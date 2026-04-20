using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Distributed.Performance
{
    /// <summary>
    /// Semaphore-backed concurrency cap that throws
    /// <see cref="BackpressureException"/> when a permit cannot be
    /// acquired within the caller's wait budget. Holds both a
    /// global permit (all distributed calls) and per-shard permits
    /// so a single slow shard does not drain the datasource-wide
    /// budget.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Acquired permits implement <see cref="IDisposable"/>; the
    /// caller <em>must</em> dispose the permit in a <c>finally</c>
    /// block to avoid permanently leaking capacity. Permits are
    /// safe to dispose from any thread.
    /// </para>
    /// <para>
    /// The gate is allocation-light on the hot path — the only
    /// allocations are the permit struct boxed as
    /// <see cref="IDisposable"/> and the per-shard
    /// <see cref="SemaphoreSlim"/> lazy-created once per shard id.
    /// </para>
    /// </remarks>
    public sealed class DistributedConcurrencyGate : IDisposable
    {
        private readonly SemaphoreSlim _global;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _perShard;
        private int _maxGlobal;
        private int _maxPerShard;
        private bool _disposed;

        /// <summary>Creates a gate with the supplied caps.</summary>
        /// <param name="maxConcurrentDistributedCalls">Global cap; <c>0</c> disables the global semaphore.</param>
        /// <param name="maxConcurrentCallsPerShard">Per-shard cap; <c>0</c> disables per-shard gating.</param>
        public DistributedConcurrencyGate(
            int maxConcurrentDistributedCalls,
            int maxConcurrentCallsPerShard)
        {
            _maxGlobal    = maxConcurrentDistributedCalls;
            _maxPerShard  = maxConcurrentCallsPerShard;
            _global       = _maxGlobal > 0 ? new SemaphoreSlim(_maxGlobal, _maxGlobal) : null;
            _perShard     = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Current global cap (read-only at runtime).</summary>
        public int MaxConcurrentDistributedCalls => _maxGlobal;

        /// <summary>Current per-shard cap (read-only at runtime).</summary>
        public int MaxConcurrentCallsPerShard => _maxPerShard;

        /// <summary>Approximate count of currently-held global permits.</summary>
        public int GlobalInFlight
            => _global == null ? 0 : _maxGlobal - _global.CurrentCount;

        /// <summary>
        /// Acquires the global distributed permit. Throws
        /// <see cref="BackpressureException"/> when the cap is full
        /// longer than <paramref name="wait"/>.
        /// </summary>
        public IDisposable AcquireDistributed(
            TimeSpan           wait,
            CancellationToken  cancellationToken = default)
        {
            ThrowIfDisposed();
            if (_global == null) return NullPermit.Instance;

            try
            {
                if (!_global.Wait(wait, cancellationToken))
                {
                    throw new BackpressureException(
                        gateName:   "DistributedCall",
                        retryAfter: TimeSpan.FromMilliseconds(Math.Max(50, wait.TotalMilliseconds)),
                        message:    $"Distributed call gate exhausted (cap={_maxGlobal}).");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            return new Permit(_global);
        }

        /// <summary>Asynchronous variant of <see cref="AcquireDistributed"/>.</summary>
        public async Task<IDisposable> AcquireDistributedAsync(
            TimeSpan           wait,
            CancellationToken  cancellationToken = default)
        {
            ThrowIfDisposed();
            if (_global == null) return NullPermit.Instance;

            if (!await _global.WaitAsync(wait, cancellationToken).ConfigureAwait(false))
            {
                throw new BackpressureException(
                    gateName:   "DistributedCall",
                    retryAfter: TimeSpan.FromMilliseconds(Math.Max(50, wait.TotalMilliseconds)),
                    message:    $"Distributed call gate exhausted (cap={_maxGlobal}).");
            }
            return new Permit(_global);
        }

        /// <summary>
        /// Acquires a per-shard permit for <paramref name="shardId"/>.
        /// Returns a no-op <see cref="IDisposable"/> when per-shard
        /// gating is disabled.
        /// </summary>
        public IDisposable AcquireShard(
            string             shardId,
            TimeSpan           wait,
            CancellationToken  cancellationToken = default)
        {
            ThrowIfDisposed();
            if (_maxPerShard <= 0 || string.IsNullOrWhiteSpace(shardId)) return NullPermit.Instance;

            var sem = _perShard.GetOrAdd(shardId, _ => new SemaphoreSlim(_maxPerShard, _maxPerShard));
            if (!sem.Wait(wait, cancellationToken))
            {
                throw new BackpressureException(
                    gateName:   "Shard:" + shardId,
                    retryAfter: TimeSpan.FromMilliseconds(Math.Max(50, wait.TotalMilliseconds)),
                    message:    $"Shard '{shardId}' concurrency cap reached (cap={_maxPerShard}).");
            }
            return new Permit(sem);
        }

        /// <summary>Asynchronous variant of <see cref="AcquireShard"/>.</summary>
        public async Task<IDisposable> AcquireShardAsync(
            string             shardId,
            TimeSpan           wait,
            CancellationToken  cancellationToken = default)
        {
            ThrowIfDisposed();
            if (_maxPerShard <= 0 || string.IsNullOrWhiteSpace(shardId)) return NullPermit.Instance;

            var sem = _perShard.GetOrAdd(shardId, _ => new SemaphoreSlim(_maxPerShard, _maxPerShard));
            if (!await sem.WaitAsync(wait, cancellationToken).ConfigureAwait(false))
            {
                throw new BackpressureException(
                    gateName:   "Shard:" + shardId,
                    retryAfter: TimeSpan.FromMilliseconds(Math.Max(50, wait.TotalMilliseconds)),
                    message:    $"Shard '{shardId}' concurrency cap reached (cap={_maxPerShard}).");
            }
            return new Permit(sem);
        }

        /// <summary>Disposes the gate and every per-shard semaphore.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _global?.Dispose();
            foreach (var kv in _perShard)
            {
                kv.Value.Dispose();
            }
            _perShard.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DistributedConcurrencyGate));
        }

        private sealed class Permit : IDisposable
        {
            private SemaphoreSlim _semaphore;

            internal Permit(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                var sem = Interlocked.Exchange(ref _semaphore, null);
                try { sem?.Release(); }
                catch (ObjectDisposedException) { /* gate tore down underneath us */ }
                catch (SemaphoreFullException)   { /* double dispose — swallow */ }
            }
        }

        private sealed class NullPermit : IDisposable
        {
            internal static readonly NullPermit Instance = new NullPermit();
            public void Dispose() { }
        }
    }
}
