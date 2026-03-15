using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Pipelines.Scheduling
{
    /// <summary>
    /// Limits the number of concurrent runs for a given pipeline using per-key semaphores.
    /// Callers acquire a slot before starting a run and release it (via <see cref="IDisposable"/>) when done.
    /// </summary>
    public sealed class ConcurrencyGate
    {
        private readonly Dictionary<string, SemaphoreSlim> _semaphores
            = new(StringComparer.Ordinal);
        private readonly object _lock = new();

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

            await sem.WaitAsync(token).ConfigureAwait(false);
            return new ReleaseHandle(sem);
        }

        // ── Private helper ────────────────────────────────────────────────────

        private sealed class ReleaseHandle : IDisposable
        {
            private readonly SemaphoreSlim _sem;
            private int _disposed;

            public ReleaseHandle(SemaphoreSlim sem) => _sem = sem;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                    _sem.Release();
            }
        }
    }
}
