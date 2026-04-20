using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks.Test
{
    /// <summary>
    /// Deterministic fault-injection sink. Throws an
    /// <see cref="InvalidOperationException"/> on every Nth call to
    /// <see cref="WriteBatchAsync"/>; all other calls succeed and forward
    /// the batch to an inner sink (or simply count it when no inner sink
    /// is supplied).
    /// </summary>
    /// <remarks>
    /// Use this sink to verify pipeline robustness:
    /// <list type="bullet">
    ///   <item>Logs: producer never observes the failure (drop is silent).</item>
    ///   <item>Audit: failure surfaces through <see cref="ITelemetrySink.IsHealthy"/>
    ///         and the sink-error self event from Phase 11.</item>
    /// </list>
    /// The failure cadence is fully deterministic (<c>FailEvery</c>) so
    /// tests do not depend on RNG state.
    /// </remarks>
    public sealed class FlakySink : ITelemetrySink
    {
        private readonly ITelemetrySink _inner;
        private readonly int _failEvery;
        private long _callCount;
        private long _failureCount;
        private long _writtenCount;

        /// <summary>
        /// Creates a flaky sink that fails on every <paramref name="failEvery"/>th
        /// call. <paramref name="inner"/> is optional; when supplied the
        /// successful batches are forwarded to it (typically a
        /// <see cref="RecordingSink"/> or <see cref="MemorySink"/>).
        /// </summary>
        public FlakySink(int failEvery = 7, ITelemetrySink inner = null, string name = "flaky")
        {
            if (failEvery <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(failEvery), "failEvery must be positive.");
            }
            _failEvery = failEvery;
            _inner = inner;
            Name = string.IsNullOrWhiteSpace(name) ? "flaky" : name;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public bool IsHealthy => Interlocked.Read(ref _failureCount) == 0
            || _inner is null
            || _inner.IsHealthy;

        /// <summary>Total batches the sink has been asked to write.</summary>
        public long CallCount => Interlocked.Read(ref _callCount);

        /// <summary>Total batches the sink intentionally failed.</summary>
        public long FailureCount => Interlocked.Read(ref _failureCount);

        /// <summary>Total envelopes successfully forwarded to the inner sink.</summary>
        public long WrittenCount => Interlocked.Read(ref _writtenCount);

        /// <inheritdoc />
        public Task WriteBatchAsync(IReadOnlyList<TelemetryEnvelope> batch, CancellationToken cancellationToken)
        {
            long call = Interlocked.Increment(ref _callCount);
            if (call % _failEvery == 0)
            {
                Interlocked.Increment(ref _failureCount);
                throw new InvalidOperationException(
                    $"FlakySink '{Name}' intentional failure on call #{call} (every {_failEvery}).");
            }

            if (batch is not null && batch.Count > 0)
            {
                Interlocked.Add(ref _writtenCount, batch.Count);
            }

            if (_inner is not null && batch is not null && batch.Count > 0)
            {
                return _inner.WriteBatchAsync(batch, cancellationToken);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task FlushAsync(CancellationToken cancellationToken)
            => _inner is null ? Task.CompletedTask : _inner.FlushAsync(cancellationToken);

        /// <inheritdoc />
        public ValueTask DisposeAsync()
            => _inner is null ? default : _inner.DisposeAsync();
    }
}
