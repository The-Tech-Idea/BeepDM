using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep.Services.Audit;
using TheTechIdea.Beep.Services.Audit.Bridges;
using TheTechIdea.Beep.Services.Audit.Export;
using TheTechIdea.Beep.Services.Audit.Integrity;
using TheTechIdea.Beep.Services.Audit.Purge;
using TheTechIdea.Beep.Services.Audit.Query;
using TheTechIdea.Beep.Services.Telemetry;
using TheTechIdea.Beep.Services.Telemetry.Diagnostics;
using TheTechIdea.Beep.Services.Telemetry.Retention;
using TheTechIdea.Beep.Services.Telemetry.Sinks;

namespace TheTechIdea.Beep.Services
{
    /// <summary>
    /// Registration helpers for the Beep audit-trail feature.
    /// </summary>
    /// <remarks>
    /// Phase 01 wired the contract and a null implementation. Phase 02
    /// constructs the real <see cref="BeepAudit"/> backed by the shared
    /// <see cref="TelemetryPipeline"/> when
    /// <see cref="BeepAuditOptions.Enabled"/> is <c>true</c>. The pipeline
    /// is registered as a keyed singleton so the DI container owns its
    /// lifetime and disposes it on shutdown (which drains the queue and
    /// disposes every sink). Audit pipelines never use samplers because
    /// audit events are lossless by policy.
    /// </remarks>
    public static class BeepServiceAuditExtensions
    {
        /// <summary>
        /// Service key used to disambiguate the audit pipeline from the
        /// logging pipeline when both are registered in the same container.
        /// </summary>
        public const string AuditPipelineKey = "Beep.Audit.Pipeline";

        /// <summary>
        /// Registers <see cref="IBeepAudit"/> in the service collection.
        /// When <see cref="BeepAuditOptions.Enabled"/> is <c>false</c> (the
        /// default) the registration resolves to <see cref="NullBeepAudit"/>
        /// and no pipeline is created.
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The same service collection for fluent chaining.</returns>
        public static IServiceCollection AddBeepAudit(
            this IServiceCollection services,
            Action<BeepAuditOptions> configure = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            BeepAuditOptions options = new BeepAuditOptions();
            configure?.Invoke(options);

            services.AddSingleton(options);

            if (!options.Enabled)
            {
                services.AddSingleton<IBeepAudit>(_ => NullBeepAudit.Instance);
                return services;
            }

            services.AddKeyedSingleton<TelemetryPipeline>(
                AuditPipelineKey,
                (_, _) => BuildPipeline(options));

            if (options.EnableRetentionSweeper)
            {
                services.AddBeepRetentionSweeper(options.SweepInterval);
            }
            else
            {
                services.AddBeepBudgetEnforcer();
            }

            EnsureChainServices(options);
            SealedLogPolicy sealedPolicy = options.SealRotatedFiles ? new SealedLogPolicy() : null;

            services.AddKeyedSingleton<SelfEventEmitter>(AuditPipelineKey, (sp, _) =>
            {
                TelemetryPipeline pipeline = sp.GetRequiredKeyedService<TelemetryPipeline>(AuditPipelineKey);
                IBudgetEnforcer enforcer = sp.GetService<IBudgetEnforcer>();
                return BeepServiceDiagnosticsExtensions.AttachDiagnostics(pipeline, enforcer);
            });

            services.AddSingleton<IBeepAudit>(sp =>
            {
                TelemetryPipeline pipeline = sp.GetRequiredKeyedService<TelemetryPipeline>(AuditPipelineKey);
                IBudgetEnforcer enforcer = sp.GetService<IBudgetEnforcer>();
                AttachFileSinks(options, enforcer, sealedPolicy);
                IHashChainSigner signer = options.HashChain ? options.Signer : null;
                BeepAudit audit = new BeepAudit(options, pipeline, signer);
                AttachComplianceServices(audit, options, signer);
                _ = sp.GetKeyedService<SelfEventEmitter>(AuditPipelineKey);
                return audit;
            });

            AuditBridgeRegistry.Register(services, options);

            RegisterComplianceFacades(services, options);

            BeepServiceDiagnosticsExtensions.RegisterMetricsSnapshot(
                services,
                pipelineKey: AuditPipelineKey,
                enabled: options.EnableMetricsSnapshot,
                interval: options.MetricsSnapshotInterval,
                outputPath: options.MetricsSnapshotFile,
                format: options.MetricsSnapshotFormat,
                emitSelfEvents: options.EmitMetricsSnapshotAsSelfEvent);

            return services;
        }

        private static void RegisterComplianceFacades(IServiceCollection services, BeepAuditOptions options)
        {
            services.AddSingleton(sp =>
            {
                IBeepAudit audit = sp.GetRequiredService<IBeepAudit>();
                IAuditQueryEngine engine = (audit as BeepAudit) is null ? null : BuildQueryEngine(options);
                return engine ?? new CompositeAuditQueryEngine(Array.Empty<IAuditQueryEngine>());
            });

            if (options.HashChain && options.KeyMaterial is not null)
            {
                services.AddSingleton(_ => new ManifestSigner(options.KeyMaterial));
                services.AddSingleton(sp =>
                {
                    IAuditQueryEngine engine = sp.GetRequiredService<IAuditQueryEngine>();
                    ManifestSigner ms = sp.GetRequiredService<ManifestSigner>();
                    return new AuditExporter(engine, ms);
                });
            }
        }

        private static void AttachComplianceServices(
            BeepAudit audit,
            BeepAuditOptions options,
            IHashChainSigner signer)
        {
            IAuditQueryEngine engine = BuildQueryEngine(options);
            IAuditPurgeStore[] purgeStores = BuildPurgeStores(options);

            GdprPurgeService purgeService = null;
            if (purgeStores.Length > 0)
            {
                IPurgePolicy policy = options.PurgePolicy
                    ?? (string.IsNullOrEmpty(options.PurgeConfirmationToken)
                        ? null
                        : new ConfirmTokenPurgePolicy(options.PurgeConfirmationToken));
                if (policy is not null)
                {
                    purgeService = new GdprPurgeService(purgeStores, signer, policy, audit);
                }
            }

            IntegrityVerifier verifier = null;
            if (signer is not null && options.AnchorStore is not null)
            {
                verifier = new IntegrityVerifier(signer, options.AnchorStore);
            }

            audit.AttachComplianceServices(engine, purgeService, verifier);
        }

        private static IAuditQueryEngine BuildQueryEngine(BeepAuditOptions options)
        {
            var engines = new List<IAuditQueryEngine>();

            foreach (ITelemetrySink sink in options.Sinks)
            {
                if (sink is SqliteSink sqlite)
                {
                    engines.Add(new SqliteAuditQueryEngine(sqlite));
                }
            }

            if (options.EnableFileScanQuery)
            {
                string scanDir =
                    !string.IsNullOrWhiteSpace(options.FileScanDirectory) ? options.FileScanDirectory :
                    !string.IsNullOrWhiteSpace(options.AnchorStoreDirectory) ? options.AnchorStoreDirectory :
                    PlatformPaths.AuditDir();
                string pattern = string.IsNullOrWhiteSpace(options.FileScanPrefix)
                    ? "audit*.ndjson*"
                    : options.FileScanPrefix + "*.ndjson*";
                engines.Add(new FileScanAuditQueryEngine(scanDir, pattern));
            }
            else
            {
                foreach (ITelemetrySink sink in options.Sinks)
                {
                    if (sink is FileRollingSink file)
                    {
                        engines.Add(new FileScanAuditQueryEngine(file));
                    }
                }
            }

            if (engines.Count == 0)
            {
                return null;
            }
            if (engines.Count == 1)
            {
                return engines[0];
            }
            return new CompositeAuditQueryEngine(engines);
        }

        private static IAuditPurgeStore[] BuildPurgeStores(BeepAuditOptions options)
        {
            var stores = new List<IAuditPurgeStore>();
            foreach (ITelemetrySink sink in options.Sinks)
            {
                if (sink is IAuditPurgeStore store)
                {
                    stores.Add(store);
                }
            }
            return stores.ToArray();
        }

        private static void EnsureChainServices(BeepAuditOptions options)
        {
            if (!options.HashChain)
            {
                return;
            }
            if (options.KeyMaterial is null)
            {
                options.KeyMaterial = new EnvironmentKeyMaterialProvider();
            }
            if (options.AnchorStore is null)
            {
                string dir = string.IsNullOrWhiteSpace(options.AnchorStoreDirectory)
                    ? PlatformPaths.AuditDir()
                    : options.AnchorStoreDirectory;
                options.AnchorStore = new JsonChainAnchorStore(dir);
            }
            if (options.Signer is null)
            {
                options.Signer = new HashChainSigner(options.KeyMaterial, options.AnchorStore);
            }
        }

        private static void AttachFileSinks(BeepAuditOptions options, IBudgetEnforcer enforcer, SealedLogPolicy sealedPolicy)
        {
            if (options.Sinks.Count == 0)
            {
                return;
            }
            foreach (ITelemetrySink sink in options.Sinks)
            {
                if (sink is not FileRollingSink fileSink)
                {
                    continue;
                }
                if (enforcer is not null)
                {
                    EnforcerScope scope = new EnforcerScope
                    {
                        Name = string.Concat("audit:", fileSink.Name),
                        Directory = fileSink.Directory,
                        FilePattern = fileSink.FilePattern,
                        Rotation = options.Rotation?.Clone() ?? new RotationPolicy(),
                        Retention = options.Retention?.Clone() ?? new RetentionPolicy(),
                        Budget = options.Budget?.Clone() ?? new StorageBudget
                        {
                            OnBreach = BudgetBreachAction.BlockNewWrites
                        }
                    };
                    enforcer.AttachSink(fileSink, scope);
                }
                if (sealedPolicy is not null)
                {
                    fileSink.Rolled += rolled => sealedPolicy.Seal(rolled?.Path);
                }
            }
        }

        private static TelemetryPipeline BuildPipeline(BeepAuditOptions options)
        {
            IReadOnlyList<ITelemetrySink> sinks = ToReadOnly(options.Sinks);
            IReadOnlyList<IEnricher> enrichers = ToReadOnly(options.Enrichers);
            IReadOnlyList<IRedactor> redactors = ToReadOnly(options.Redactors);

            return new TelemetryPipeline(
                queueCapacity: options.QueueCapacity,
                backpressureMode: options.BackpressureMode,
                flushInterval: options.FlushInterval,
                sinks: sinks,
                enrichers: enrichers,
                redactors: redactors,
                samplers: null,
                name: "audit");
        }

        private static IReadOnlyList<T> ToReadOnly<T>(IList<T> source)
        {
            if (source is null || source.Count == 0)
            {
                return Array.Empty<T>();
            }
            T[] copy = new T[source.Count];
            source.CopyTo(copy, 0);
            return copy;
        }
    }
}
