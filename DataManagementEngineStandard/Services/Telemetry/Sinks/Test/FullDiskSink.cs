using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks.Test
{
    /// <summary>
    /// Storage-exhaustion fault injection sink. Tracks an estimated byte
    /// count from <see cref="TelemetryEnvelope.Message"/> length plus a
    /// fixed per-envelope overhead, and once <see cref="MaxBytes"/> is
    /// reached every subsequent <see cref="WriteBatchAsync"/> throws an
    /// <see cref="IOException"/> with a "disk full" message.
    /// </summary>
    /// <remarks>
    /// Use this sink to verify:
    /// <list type="bullet">
    ///   <item>The pipeline marks the sink unhealthy and emits the
    ///         <c>BeepTelemetry.Self.Sink</c> error event.</item>
    ///   <item>The retention sweeper / budget enforcer recovers space and
    ///         clears the unhealthy state once <see cref="ResetUsage"/>
    ///         is called.</item>
    /// </list>
    /// The byte estimate intentionally over-counts so that small test
    /// budgets trigger predictably.
    /// </remarks>
    public sealed class FullDiskSink : ITelemetrySink
    {
        /// <summary>Default per-envelope overhead used by the byte estimator.</summary>
        public const int DefaultPerEnvelopeOverheadBytes = 128;

        private readonly ITelemetrySink _inner;
        private readonly int _perEnvelopeOverhead;
        private long _usedBytes;
        private long _writtenCount;
        private bool _full;

        /// <summary>
        /// Creates a sink that throws once <paramref name="maxBytes"/>
        /// of estimated payload is exceeded.
        /// </summary>
        public FullDiskSink(
            long maxBytes = 1L * 1024 * 1024,
            ITelemetrySink inner = null,
            int perEnvelopeOverheadBytes = DefaultPerEnvelopeOverheadBytes,
            string name = "full-disk")
        {
            if (maxBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxBytes), "maxBytes must be positive.");
            }
            if (perEnvelopeOverheadBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(perEnvelopeOverheadBytes));
            }
            MaxBytes = maxBytes;
            _perEnvelopeOverhead = perEnvelopeOverheadBytes;
            _inner = inner;
            Name = string.IsNullOrWhiteSpace(name) ? "full-disk" : name;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public bool IsHealthy => !Volatile.Read(ref _full)
            && (_inner is null || _inner.IsHealthy);

        /// <summary>Configured byte ceiling.</summary>
        public long MaxBytes { get; }

        /// <summary>Estimated bytes written so far.</summary>
        public long UsedBytes => Interlocked.Read(ref _usedBytes);

        /// <summary>Total envelopes successfully forwarded.</summary>
        public long WrittenCount => Interlocked.Read(ref _writtenCount);

        /// <summary><c>true</c> once the sink has tripped the disk-full state.</summary>
        public bool IsFull => Volatile.Read(ref _full);

        /// <summary>
        /// Resets the byte counter and clears the disk-full state. Call this
        /// from a test that validates recovery (for example after a manual
        /// retention sweep).
        /// </summary>
        public void ResetUsage()
        {
            Interlocked.Exchange(ref _usedBytes, 0);
            Volatile.Write(ref _full, false);
        }

        /// <inheritdoc />
        public Task WriteBatchAsync(IReadOnlyList<TelemetryEnvelope> batch, CancellationToken cancellationToken)
        {
            if (batch is null || batch.Count == 0)
            {
                return Task.CompletedTask;
            }

            long estimate = 0;
            for (int i = 0; i < batch.Count; i++)
            {
                TelemetryEnvelope env = batch[i];
                if (env is null) { continue; }
                int messageBytes = string.IsNullOrEmpty(env.Message) ? 0 : Encoding.UTF8.GetByteCount(env.Message);
                estimate += messageBytes + _perEnvelopeOverhead;
            }

            long used = Interlocked.Add(ref _usedBytes, estimate);
            if (used > MaxBytes)
            {
                Volatile.Write(ref _full, true);
                throw new IOException(
                    $"FullDiskSink '{Name}' simulated disk-full: used={used} bytes > maxBytes={MaxBytes}.");
            }

            Interlocked.Add(ref _writtenCount, batch.Count);
            if (_inner is not null)
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
