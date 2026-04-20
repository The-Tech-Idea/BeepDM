using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Services.Logging;
using TheTechIdea.Beep.Services.Logging.Bridges;
using TheTechIdea.Beep.Services.Logging.Query;
using TheTechIdea.Beep.Services.Telemetry;
using TheTechIdea.Beep.Services.Telemetry.Diagnostics;
using TheTechIdea.Beep.Services.Telemetry.Retention;
using TheTechIdea.Beep.Services.Telemetry.Sinks;

namespace TheTechIdea.Beep.Services
{
    /// <summary>
    /// Registration helpers for the Beep logging feature.
    /// </summary>
    /// <remarks>
    /// Phase 01 wired the contract and a null implementation. Phase 02
    /// constructs the real <see cref="BeepLog"/> backed by the shared
    /// <see cref="TelemetryPipeline"/> when
    /// <see cref="BeepLoggingOptions.Enabled"/> is <c>true</c>. The pipeline
    /// is registered as a keyed singleton so the DI container owns its
    /// lifetime and disposes it on shutdown (which drains the queue and
    /// disposes every sink).
    /// </remarks>
    public static class BeepServiceLoggingExtensions
    {
        /// <summary>
        /// Service key used to disambiguate the logging pipeline from the
        /// audit pipeline when both are registered in the same container.
        /// </summary>
        public const string LoggingPipelineKey = "Beep.Logging.Pipeline";

        /// <summary>
        /// Registers <see cref="IBeepLog"/> in the service collection.
        /// When <see cref="BeepLoggingOptions.Enabled"/> is <c>false</c> (the
        /// default) the registration resolves to <see cref="NullBeepLog"/>
        /// and no pipeline is created.
        /// </summary>
        /// <param name="services">The service collection to extend.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The same service collection for fluent chaining.</returns>
        public static IServiceCollection AddBeepLogging(
            this IServiceCollection services,
            Action<BeepLoggingOptions> configure = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            BeepLoggingOptions options = new BeepLoggingOptions();
            configure?.Invoke(options);

            services.AddSingleton(options);

            if (!options.Enabled)
            {
                services.AddSingleton<IBeepLog>(_ => NullBeepLog.Instance);
                return services;
            }

            services.AddKeyedSingleton<TelemetryPipeline>(
                LoggingPipelineKey,
                (_, _) => BuildPipeline(options));

            if (options.EnableRetentionSweeper)
            {
                services.AddBeepRetentionSweeper(options.SweepInterval);
            }
            else
            {
                services.AddBeepBudgetEnforcer();
            }

            services.AddKeyedSingleton<SelfEventEmitter>(LoggingPipelineKey, (sp, _) =>
            {
                TelemetryPipeline pipeline = sp.GetRequiredKeyedService<TelemetryPipeline>(LoggingPipelineKey);
                IBudgetEnforcer enforcer = sp.GetService<IBudgetEnforcer>();
                return BeepServiceDiagnosticsExtensions.AttachDiagnostics(pipeline, enforcer);
            });

            services.AddSingleton<IBeepLog>(sp =>
            {
                TelemetryPipeline pipeline = sp.GetRequiredKeyedService<TelemetryPipeline>(LoggingPipelineKey);
                IBudgetEnforcer enforcer = sp.GetService<IBudgetEnforcer>();
                AttachFileSinks(options, enforcer);
                // Force-construct the emitter so diagnostics start
                // observing the pipeline as soon as the log is built.
                _ = sp.GetKeyedService<SelfEventEmitter>(LoggingPipelineKey);
                return new BeepLog(options, pipeline);
            });

            RegisterBridges(services, options);

            RegisterLogQueryEngine(services, options);

            BeepServiceDiagnosticsExtensions.RegisterMetricsSnapshot(
                services,
                pipelineKey: LoggingPipelineKey,
                enabled: options.EnableMetricsSnapshot,
                interval: options.MetricsSnapshotInterval,
                outputPath: options.MetricsSnapshotFile,
                format: options.MetricsSnapshotFormat,
                emitSelfEvents: options.EmitMetricsSnapshotAsSelfEvent);

            return services;
        }

        private static void RegisterLogQueryEngine(IServiceCollection services, BeepLoggingOptions options)
        {
            services.AddSingleton<ILogQueryEngine>(_ =>
            {
                var engines = new List<ILogQueryEngine>();
                foreach (ITelemetrySink sink in options.Sinks)
                {
                    if (sink is SqliteSink sqlite)
                    {
                        engines.Add(new SqliteLogQueryEngine(sqlite));
                    }
                    else if (sink is FileRollingSink file)
                    {
                        engines.Add(new FileScanLogQueryEngine(file));
                    }
                }
                if (engines.Count == 1)
                {
                    return engines[0];
                }
                return new CompositeLogQueryEngine(engines);
            });
        }

        private static void RegisterBridges(IServiceCollection services, BeepLoggingOptions options)
        {
            if (options.ReplaceDMLogger)
            {
                services.RemoveAll<IDMLogger>();
                services.AddSingleton<IDMLogger>(sp =>
                    new DMLoggerToBeepLogBridge(sp.GetRequiredService<IBeepLog>()));
            }

            if (options.RegisterMicrosoftLoggerProvider)
            {
                services.AddSingleton<ILoggerProvider>(sp =>
                    new MicrosoftLoggerProvider(sp.GetRequiredService<IBeepLog>()));
            }
        }

        private static void AttachFileSinks(BeepLoggingOptions options, IBudgetEnforcer enforcer)
        {
            if (enforcer is null || options.Sinks.Count == 0)
            {
                return;
            }
            foreach (ITelemetrySink sink in options.Sinks)
            {
                if (sink is not FileRollingSink fileSink)
                {
                    continue;
                }
                EnforcerScope scope = new EnforcerScope
                {
                    Name = string.Concat("logging:", fileSink.Name),
                    Directory = fileSink.Directory,
                    FilePattern = fileSink.FilePattern,
                    Rotation = options.Rotation?.Clone() ?? new RotationPolicy(),
                    Retention = options.Retention?.Clone() ?? new RetentionPolicy(),
                    Budget = options.Budget?.Clone() ?? new StorageBudget()
                };
                enforcer.AttachSink(fileSink, scope);
            }
        }

        private static TelemetryPipeline BuildPipeline(BeepLoggingOptions options)
        {
            IReadOnlyList<ITelemetrySink> sinks = ToReadOnly(options.Sinks);
            IReadOnlyList<IEnricher> enrichers = ToReadOnly(options.Enrichers);
            IReadOnlyList<IRedactor> redactors = ToReadOnly(options.Redactors);
            IReadOnlyList<ISampler> samplers = ToReadOnly(options.Samplers);

            return new TelemetryPipeline(
                queueCapacity: options.QueueCapacity,
                backpressureMode: options.BackpressureMode,
                flushInterval: options.FlushInterval,
                sinks: sinks,
                enrichers: enrichers,
                redactors: redactors,
                samplers: samplers,
                deduper: options.Deduper,
                rateLimiter: options.RateLimiter,
                name: "logging");
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
