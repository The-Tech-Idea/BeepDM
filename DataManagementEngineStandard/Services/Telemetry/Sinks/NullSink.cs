using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// Sink that discards every envelope it receives. Useful as the
    /// default sink when the feature is enabled but the operator has not
    /// configured any persistence — the pipeline still exercises the
    /// enricher / redactor / sampler stages so misconfiguration surfaces
    /// quickly.
    /// </summary>
    public sealed class NullSink : ITelemetrySink
    {
        /// <summary>Singleton suitable for direct registration.</summary>
        public static readonly NullSink Instance = new NullSink();

        /// <inheritdoc />
        public string Name => "null";

        /// <inheritdoc />
        public bool IsHealthy => true;

        /// <inheritdoc />
        public Task WriteBatchAsync(IReadOnlyList<TelemetryEnvelope> batch, CancellationToken cancellationToken)
            => Task.CompletedTask;

        /// <inheritdoc />
        public Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <inheritdoc />
        public ValueTask DisposeAsync() => default;
    }
}
