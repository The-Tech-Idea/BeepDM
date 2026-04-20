using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks.Test
{
    /// <summary>
    /// In-memory test sink with rich query helpers. Differs from
    /// <see cref="MemorySink"/> in two ways: there is no eviction (tests
    /// keep every envelope until <see cref="Clear"/> is called) and the
    /// sink exposes typed query helpers for logs, audit events, and
    /// self-observability events through the <c>.Query</c> partial.
    /// </summary>
    /// <remarks>
    /// Split into two partial files:
    /// <list type="bullet">
    ///   <item><c>.Core</c> — fields, constructor, write path, lifecycle.</item>
    ///   <item><c>.Query</c> — typed lookup helpers consumed by tests.</item>
    /// </list>
    /// The buffer uses a <see cref="ConcurrentQueue{T}"/> + atomic counter
    /// so tests can call <see cref="Snapshot"/> from any thread without
    /// a lock and without freezing the producer drain task.
    /// </remarks>
    public sealed partial class RecordingSink : ITelemetrySink
    {
        private readonly ConcurrentQueue<TelemetryEnvelope> _buffer = new ConcurrentQueue<TelemetryEnvelope>();
        private long _writtenCount;
        private int _disposed;

        /// <summary>Creates a recording sink with the supplied display name.</summary>
        public RecordingSink(string name = "recording")
        {
            Name = string.IsNullOrWhiteSpace(name) ? "recording" : name;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public bool IsHealthy => Volatile.Read(ref _disposed) == 0;

        /// <summary>Total envelopes written since startup.</summary>
        public long WrittenCount => Interlocked.Read(ref _writtenCount);

        /// <summary>Approximate live count (eventually consistent).</summary>
        public int Count => _buffer.Count;

        /// <inheritdoc />
        public Task WriteBatchAsync(IReadOnlyList<TelemetryEnvelope> batch, CancellationToken cancellationToken)
        {
            if (batch is null || batch.Count == 0 || Volatile.Read(ref _disposed) != 0)
            {
                return Task.CompletedTask;
            }

            for (int i = 0; i < batch.Count; i++)
            {
                TelemetryEnvelope env = batch[i];
                if (env is null) { continue; }
                _buffer.Enqueue(env);
                Interlocked.Increment(ref _writtenCount);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Returns a point-in-time snapshot of the buffer contents.
        /// Order is producer-time order (oldest first).
        /// </summary>
        public IReadOnlyList<TelemetryEnvelope> Snapshot() => _buffer.ToArray();

        /// <summary>
        /// Convenience accessor returning every recorded envelope. Equivalent
        /// to <see cref="Snapshot"/> but reads better in test assertions.
        /// </summary>
        public IReadOnlyList<TelemetryEnvelope> All => _buffer.ToArray();

        /// <summary>
        /// Empties the buffer (preserves <see cref="WrittenCount"/>).
        /// </summary>
        public void Clear()
        {
            while (_buffer.TryDequeue(out _))
            {
                // drain
            }
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref _disposed, 1);
            Clear();
            return default;
        }
    }
}
