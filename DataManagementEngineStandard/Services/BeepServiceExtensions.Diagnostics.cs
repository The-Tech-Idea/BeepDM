using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheTechIdea.Beep.Services.Telemetry;
using TheTechIdea.Beep.Services.Telemetry.Diagnostics;
using TheTechIdea.Beep.Services.Telemetry.Retention;

namespace TheTechIdea.Beep.Services
{
    /// <summary>
    /// Phase 11 diagnostic registration helpers. Builds the
    /// <see cref="SelfEventEmitter"/>, attaches it (plus
    /// <see cref="PipelineDiagnosticsHooks"/>) to a
    /// <see cref="TelemetryPipeline"/>, and optionally schedules the
    /// periodic metrics snapshot service. Kept in its own file so the
    /// logging / audit registration helpers stay focused on their
    /// primary concerns.
    /// </summary>
    internal static class BeepServiceDiagnosticsExtensions
    {
        /// <summary>
        /// Wires the supplied pipeline with a self-event emitter and a
        /// diagnostics-hook subscriber. Returns the emitter so the
        /// caller can register the optional snapshot service against
        /// the same instance.
        /// </summary>
        internal static SelfEventEmitter AttachDiagnostics(
            TelemetryPipeline pipeline,
            IBudgetEnforcer enforcer = null)
        {
            if (pipeline is null)
            {
                throw new ArgumentNullException(nameof(pipeline));
            }

            SelfEventEmitter emitter = new SelfEventEmitter(
                pipelineName: pipeline.Name,
                emit: pipeline.EnqueueSelfEvent,
                metrics: pipeline.Metrics);

            // Hooks subscribe to pipeline + enforcer events for the
            // lifetime of the host. They have no dispose semantics
            // beyond unsubscribing; the pipeline itself is the lifetime
            // anchor so we deliberately do not return the IDisposable.
            _ = new PipelineDiagnosticsHooks(
                metrics: pipeline.Metrics,
                selfEvents: emitter,
                pipeline: pipeline,
                enforcer: enforcer);

            return emitter;
        }

        /// <summary>
        /// Schedules the periodic snapshot service against
        /// <paramref name="services"/> when the supplied options
        /// request it. The service is only registered when the host
        /// has wired <see cref="IHostedService"/> support.
        /// </summary>
        internal static void RegisterMetricsSnapshot(
            IServiceCollection services,
            string pipelineKey,
            bool enabled,
            TimeSpan interval,
            string outputPath,
            MetricsSnapshotFormat format,
            bool emitSelfEvents)
        {
            if (services is null || string.IsNullOrEmpty(pipelineKey) || !enabled)
            {
                return;
            }

            services.AddSingleton<IHostedService>(sp =>
            {
                TelemetryPipeline pipeline = sp.GetRequiredKeyedService<TelemetryPipeline>(pipelineKey);
                SelfEventEmitter emitter = sp.GetKeyedService<SelfEventEmitter>(pipelineKey);
                return new PeriodicMetricsSnapshotHostedService(
                    pipeline: pipeline,
                    selfEvents: emitter,
                    interval: interval,
                    emitSelfEvents: emitSelfEvents,
                    outputPath: outputPath,
                    format: format);
            });
        }
    }
}
