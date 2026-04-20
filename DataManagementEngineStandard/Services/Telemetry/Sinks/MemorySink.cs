using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry.Sinks
{
    /// <summary>
    /// In-process ring buffer sink. Stores up to
    /// <see cref="MaxItems"/> envelopes; oldest entries are evicted when
    /// the cap is exceeded. Useful for unit tests and as a low-storage
    /// fallback on hosts where disk is unavailable.
    /// </summary>
    /// <remarks>
    /// The buffer is backed by a <see cref="ConcurrentQueue{T}"/> + an
    /// atomic counter so producers (the pipeline drain task) and readers
    /// (test code calling <see cref="Snapshot"/>) never lock. The trade-off
    /// is that a transient over-shoot of one or two items above
    /// <see cref="MaxItems"/> is possible during heavy concurrent writes;
    /// the cap is treated as a soft target.
    /// </remarks>
    public sealed class MemorySink : ITelemetrySink
    {
        /// <summary>Default ring buffer size when the caller does not specify one.</summary>
        public const int DefaultMaxItems = 4096;

        private readonly ConcurrentQueue<TelemetryEnvelope> _buffer = new();
        private int _count;
        private long _writtenCount;

        /// <summary>Creates a sink with the supplied capacity.</summary>
        public MemorySink(string name = "memory", int maxItems = DefaultMaxItems)
        {
            if (maxItems <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxItems), "maxItems must be positive.");
            }
            Name = string.IsNullOrWhiteSpace(name) ? "memory" : name;
            MaxItems = maxItems;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public bool IsHealthy => true;

        /// <summary>Configured ring buffer cap.</summary>
        public int MaxItems { get; }

        /// <summary>Total envelopes accepted since startup (pre-eviction).</summary>
        public long WrittenCount => Interlocked.Read(ref _writtenCount);

        /// <summary>Approximate live count (eventually consistent).</summary>
        public int Count => Volatile.Read(ref _count);

        /// <inheritdoc />
        public Task WriteBatchAsync(IReadOnlyList<TelemetryEnvelope> batch, CancellationToken cancellationToken)
        {
            if (batch is null || batch.Count == 0)
            {
                return Task.CompletedTask;
            }

            for (int i = 0; i < batch.Count; i++)
            {
                _buffer.Enqueue(batch[i]);
                Interlocked.Increment(ref _writtenCount);
                int now = Interlocked.Increment(ref _count);

                while (now > MaxItems && _buffer.TryDequeue(out _))
                {
                    now = Interlocked.Decrement(ref _count);
                }
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

        /// <summary>Empties the buffer (preserves <see cref="WrittenCount"/>).</summary>
        public void Clear()
        {
            while (_buffer.TryDequeue(out _))
            {
                Interlocked.Decrement(ref _count);
            }
            Interlocked.Exchange(ref _count, 0);
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            Clear();
            return default;
        }
    }
}
