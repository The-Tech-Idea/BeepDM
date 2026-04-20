using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks.Test
{
    /// <summary>
    /// Latency-injection sink. Awaits <see cref="Latency"/> before each
    /// successful <see cref="WriteBatchAsync"/> call. Useful to stress
    /// the bounded queue: with a slow sink, the producer side will hit
    /// the back-pressure path (drop or block) before the writer drains.
    /// </summary>
    /// <remarks>
    /// The sink uses <see cref="Task.Delay(TimeSpan, CancellationToken)"/>
    /// rather than <see cref="Thread.Sleep(TimeSpan)"/> so it does not
    /// hold the underlying writer thread. Tests can stop the simulated
    /// latency early by cancelling the supplied
    /// <see cref="CancellationToken"/>.
    /// </remarks>
    public sealed class SlowSink : ITelemetrySink
    {
        private readonly ITelemetrySink _inner;
        private long _writtenCount;
        private long _callCount;

        /// <summary>
        /// Creates a slow sink that adds <paramref name="latency"/> per
        /// batch. <paramref name="inner"/> is optional; when supplied the
        /// batch is forwarded after the wait.
        /// </summary>
        public SlowSink(TimeSpan latency, ITelemetrySink inner = null, string name = "slow")
        {
            if (latency < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(latency), "latency must be non-negative.");
            }
            Latency = latency;
            _inner = inner;
            Name = string.IsNullOrWhiteSpace(name) ? "slow" : name;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public bool IsHealthy => _inner is null ? true : _inner.IsHealthy;

        /// <summary>Configured latency injected before each batch.</summary>
        public TimeSpan Latency { get; }

        /// <summary>Total batches the sink processed.</summary>
        public long CallCount => Interlocked.Read(ref _callCount);

        /// <summary>Total envelopes processed.</summary>
        public long WrittenCount => Interlocked.Read(ref _writtenCount);

        /// <inheritdoc />
        public async Task WriteBatchAsync(IReadOnlyList<TelemetryEnvelope> batch, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            if (Latency > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(Latency, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
            if (batch is not null && batch.Count > 0)
            {
                Interlocked.Add(ref _writtenCount, batch.Count);
            }
            if (_inner is not null && batch is not null && batch.Count > 0)
            {
                await _inner.WriteBatchAsync(batch, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public Task FlushAsync(CancellationToken cancellationToken)
            => _inner is null ? Task.CompletedTask : _inner.FlushAsync(cancellationToken);

        /// <inheritdoc />
        public ValueTask DisposeAsync()
            => _inner is null ? default : _inner.DisposeAsync();
    }
}
