using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// ProxyCluster — fault injection + per-node rate limiting partition (Phase 11.10).
    /// </summary>
    public partial class ProxyCluster
    {
        // ── RNG for fault injection probability rolls ─────────────────────
        private static readonly ThreadLocal<Random> _faultRng
            = new(() => new Random(Guid.NewGuid().GetHashCode()));

        // ── Per-node rate limiters (keyed by NodeId) ──────────────────────
        private readonly ConcurrentDictionary<string, NodeRateLimiter> _rateLimiters = new();

        // ─────────────────────────────────────────────────────────────────
        //  Fault injection
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies fault injection policy if configured.
        /// Must be called at the start of every operation delegating to a node.
        /// No-ops when <see cref="ProxyPolicy.FaultInjection"/> is null.
        /// </summary>
        internal async Task ApplyFaultInjectionAsync(
            string            operationName,
            string?           entityHint,
            ProxyNode         node,
            CancellationToken ct)
        {
            var fi = _clusterPolicy.FaultInjection;
            if (fi is null) return;

            if (fi.TargetNodeId is not null && fi.TargetNodeId != node.NodeId) return;
            if (fi.TargetEntity is not null && fi.TargetEntity != entityHint)  return;

            var rng = _faultRng.Value!;

            // Artificial latency
            if (fi.DelayRate > 0 && rng.NextDouble() < fi.DelayRate)
                await Task.Delay(fi.DelayMs, ct).ConfigureAwait(false);

            // Artificial error
            if (fi.ErrorRate > 0 && rng.NextDouble() < fi.ErrorRate)
                throw new ProxyFaultInjectionException(
                    $"[FaultInjection] Artificial error on node '{node.NodeId}'" +
                    (entityHint is not null ? $" entity '{entityHint}'" : string.Empty));
        }

        // ─────────────────────────────────────────────────────────────────
        //  Per-node rate limiting
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to acquire a rate-limit slot for <paramref name="node"/>.
        /// Returns true if the caller may proceed.
        /// When the limit is exceeded, applies the configured <see cref="RateLimitAction"/>.
        /// </summary>
        internal async Task<bool> TryAcquireNodeSlotAsync(
            ProxyNode         node,
            CancellationToken ct)
        {
            var limitCfg = _clusterPolicy.NodeRateLimits
                .FirstOrDefault(r => r.NodeId == node.NodeId);

            if (limitCfg is null) return true;   // no rate limit for this node

            var limiter = _rateLimiters.GetOrAdd(
                node.NodeId,
                id => new NodeRateLimiter(limitCfg.MaxRps));

            bool acquired = limiter.TryAcquire();

            if (acquired) return true;

            switch (limitCfg.Action)
            {
                case RateLimitAction.RouteElsewhere:
                    return false;   // caller should pick another node

                case RateLimitAction.Reject:
                    throw new InvalidOperationException(
                        $"ProxyCluster: rate limit exceeded for node '{node.NodeId}' " +
                        $"(max {limitCfg.MaxRps} rps).");

                case RateLimitAction.Queue:
                    // Block up to the cluster queue timeout
                    int timeoutMs = _clusterPolicy.ClusterQueueTimeoutMs;
                    bool waited   = await limiter.WaitAsync(timeoutMs, ct).ConfigureAwait(false);
                    if (!waited)
                        throw new TimeoutException(
                            $"ProxyCluster: queued request timed out waiting for rate-limit " +
                            $"slot on node '{node.NodeId}'.");
                    return true;
            }

            return true;
        }

        /// <summary>
        /// Rebuilds per-node rate limiters after a policy change.
        /// </summary>
        private void RebuildRateLimiters()
        {
            // Remove limiters for nodes no longer in the rate-limit config
            var configuredIds = _clusterPolicy.NodeRateLimits
                .Select(r => r.NodeId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var key in _rateLimiters.Keys.ToList())
                if (!configuredIds.Contains(key))
                    if (_rateLimiters.TryRemove(key, out var old))
                        old.Dispose();

            // Create/replace limiters for re-configured nodes
            foreach (var cfg in _clusterPolicy.NodeRateLimits)
                _rateLimiters.AddOrUpdate(
                    cfg.NodeId,
                    _ => new NodeRateLimiter(cfg.MaxRps),
                    (_, existing) =>
                    {
                        existing.Dispose();
                        return new NodeRateLimiter(cfg.MaxRps);
                    });
        }

        // ─────────────────────────────────────────────────────────────────
        //  NodeRateLimiter  — token-bucket using SemaphoreSlim
        // ─────────────────────────────────────────────────────────────────

        private sealed class NodeRateLimiter : IDisposable
        {
            // Token-bucket: max MaxRps tokens, refilled every second via a timer
            private readonly SemaphoreSlim   _semaphore;
            private readonly Timer           _refillTimer;
            private readonly int             _maxRps;
            private          int             _disposed;

            public NodeRateLimiter(int maxRps)
            {
                _maxRps    = Math.Max(1, maxRps);
                _semaphore = new SemaphoreSlim(_maxRps, _maxRps);

                // Refill one token-slot per (1000 / MaxRps) ms interval
                int intervalMs = Math.Max(1, 1000 / _maxRps);
                _refillTimer = new Timer(
                    _ => Refill(),
                    null,
                    TimeSpan.FromMilliseconds(intervalMs),
                    TimeSpan.FromMilliseconds(intervalMs));
            }

            private void Refill()
            {
                // Add back up to _maxRps permits without exceeding the ceiling
                int current = _semaphore.CurrentCount;
                int missing  = _maxRps - current;
                if (missing > 0) _semaphore.Release(missing);
            }

            public bool TryAcquire() => _semaphore.Wait(0);

            public Task<bool> WaitAsync(int timeoutMs, CancellationToken ct)
                => _semaphore.WaitAsync(timeoutMs, ct);

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
                _refillTimer.Dispose();
                _semaphore.Dispose();
            }
        }
    }
}
