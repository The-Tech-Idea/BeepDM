using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Pipelines.Scheduling
{
    /// <summary>
    /// Limits the number of concurrent runs for a given pipeline using per-key semaphores.
    /// Callers acquire a slot before starting a run and release it (via <see cref="IDisposable"/>) when done.
    /// Tracks contention metrics (total waits, cumulative wait duration, active slots).
    /// </summary>
    public sealed class ConcurrencyGate
    {
        private readonly Dictionary<string, SemaphoreSlim> _semaphores
            = new(StringComparer.Ordinal);
        private readonly object _lock = new();

        // Contention metrics per key
        private readonly ConcurrentDictionary<string, GateMetrics> _metrics
            = new(StringComparer.Ordinal);

        /// <summary>
        /// Acquires a concurrency slot for <paramref name="key"/>.
        /// Waits if <paramref name="maxConcurrency"/> slots are already taken.
        /// Dispose the returned handle to release the slot.
        /// </summary>
        public async Task<IDisposable> AcquireAsync(string key, int maxConcurrency, CancellationToken token)
        {
            if (maxConcurrency < 1) maxConcurrency = 1;

            SemaphoreSlim sem;
            lock (_lock)
            {
                if (!_semaphores.TryGetValue(key, out sem!))
                {
                    sem = new SemaphoreSlim(maxConcurrency, maxConcurrency);
                    _semaphores[key] = sem;
                }
            }

            var metrics = _metrics.GetOrAdd(key, _ => new GateMetrics());
            var sw = Stopwatch.StartNew();

            await sem.WaitAsync(token).ConfigureAwait(false);

            sw.Stop();
            Interlocked.Increment(ref metrics.TotalAcquires);
            Interlocked.Add(ref metrics.TotalWaitMs, sw.ElapsedMilliseconds);
            if (sw.ElapsedMilliseconds > 0)
                Interlocked.Increment(ref metrics.ContentionCount);
            Interlocked.Increment(ref metrics.ActiveCount);

            return new ReleaseHandle(sem, metrics);
        }

        /// <summary>Returns the number of active (acquired) slots for <paramref name="key"/>.</summary>
        public int GetActiveCount(string key)
            => _metrics.TryGetValue(key, out var m) ? (int)Interlocked.Read(ref m.ActiveCount) : 0;

        /// <summary>Returns contention metrics for all tracked keys.</summary>
        public IReadOnlyDictionary<string, GateSnapshot> GetAllMetrics()
        {
            var result = new Dictionary<string, GateSnapshot>(StringComparer.Ordinal);
            foreach (var kv in _metrics)
            {
                var m = kv.Value;
                result[kv.Key] = new GateSnapshot(
                    Interlocked.Read(ref m.TotalAcquires),
                    Interlocked.Read(ref m.ContentionCount),
                    Interlocked.Read(ref m.TotalWaitMs),
                    (int)Interlocked.Read(ref m.ActiveCount));
            }
            return result;
        }

        // ── Snapshot DTO ──────────────────────────────────────────────────────

        public readonly struct GateSnapshot
        {
            public long TotalAcquires   { get; }
            public long ContentionCount { get; }
            public long TotalWaitMs     { get; }
            public int  ActiveSlots     { get; }

            public GateSnapshot(long acquires, long contentions, long waitMs, int active)
            {
                TotalAcquires   = acquires;
                ContentionCount = contentions;
                TotalWaitMs     = waitMs;
                ActiveSlots     = active;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private sealed class GateMetrics
        {
            public long TotalAcquires;
            public long ContentionCount;
            public long TotalWaitMs;
            public long ActiveCount;
        }

        private sealed class ReleaseHandle : IDisposable
        {
            private readonly SemaphoreSlim _sem;
            private readonly GateMetrics _metrics;
            private int _disposed;

            public ReleaseHandle(SemaphoreSlim sem, GateMetrics metrics)
            {
                _sem = sem;
                _metrics = metrics;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    Interlocked.Decrement(ref _metrics.ActiveCount);
                    _sem.Release();
                }
            }
        }
    }
}
