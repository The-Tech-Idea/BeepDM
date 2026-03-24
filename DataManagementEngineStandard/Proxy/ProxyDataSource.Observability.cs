using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// Phase 6 — per-source latency tracking for SLO computation and adaptive routing.
    /// Maintains a rolling window of the most recent latency samples and exposes
    /// p50/p95/p99 percentile summaries via <see cref="GetSloSnapshot"/>.
    /// </summary>
    public partial class ProxyDataSource
    {
        // Bounded circular buffer of latency samples per data source (last N calls)
        private const int LatencySampleCapacity = 500;

        private readonly ConcurrentDictionary<string, BoundedLatencyBuffer> _latencyBuffers = new();

        // ─────────────────────────────────────────────────────────────
        //  Partial method implementation (called from ExecutionHelpers)
        // ─────────────────────────────────────────────────────────────

        partial void RecordLatency(string dsName, long elapsedMs)
        {
            var buffer = _latencyBuffers.GetOrAdd(dsName, _ => new BoundedLatencyBuffer(LatencySampleCapacity));
            buffer.Add(elapsedMs);
        }

        // ─────────────────────────────────────────────────────────────
        //  SLO snapshot per data source  (Phase 6)
        // ─────────────────────────────────────────────────────────────

        public ProxySloSnapshot GetSloSnapshot(string dsName)
        {
            _metrics.TryGetValue(dsName, out var m);
            _latencyBuffers.TryGetValue(dsName, out var buf);

            long   total    = m?.TotalRequests    ?? 0L;
            long   failed   = m?.FailedRequests   ?? 0L;
            double errRate  = total > 0 ? (double)failed / total * 100.0 : 0d;

            var (p50, p95, p99) = buf != null ? buf.Percentiles() : (0d, 0d, 0d);

            var stats         = _cacheScope?.Statistics;
            double cacheHitRatio = stats != null && (stats.Hits + stats.Misses) > 0
                ? (double)stats.Hits / (stats.Hits + stats.Misses)
                : 0d;

            return new ProxySloSnapshot
            {
                DataSourceName   = dsName,
                P50LatencyMs     = p50,
                P95LatencyMs     = p95,
                P99LatencyMs     = p99,
                ErrorRatePercent = errRate,
                TotalRequests    = total,
                CacheHitRatio    = cacheHitRatio,
                SnapshotTime     = DateTime.UtcNow
            };
        }

        /// <summary>Returns an SLO snapshot for every registered data source.</summary>
        public IReadOnlyList<ProxySloSnapshot> GetAllSloSnapshots()
            => _dataSourceNames.Select(GetSloSnapshot).ToList();

        // ─────────────────────────────────────────────────────────────
        //  Apply / re-apply a policy at runtime
        // ─────────────────────────────────────────────────────────────

        public void ApplyPolicy(ProxyPolicy newPolicy)
        {
            if (newPolicy == null) throw new ArgumentNullException(nameof(newPolicy));

            // ── P0 fix: stop old health-check timer BEFORE swapping policy ────
            // Without this, the old timer keeps firing at the old interval even after
            // the policy changes, creating a second timer that leaks resources.
            _healthCheckTimer.Stop();

            // Swap policy reference
            _policy = newPolicy;

            // Sync backward-compat public properties
            MaxRetries                      = newPolicy.Resilience.MaxRetries;
            RetryDelayMilliseconds          = newPolicy.Resilience.RetryBaseDelayMs;
            HealthCheckIntervalMilliseconds = newPolicy.HealthCheckIntervalMs;

            // Update timer interval and restart (reuse the same instance — it's readonly)
            _healthCheckTimer.Interval = newPolicy.HealthCheckIntervalMs;
            _healthCheckTimer.Start();

            // Reinitialise circuit state for all datasources with the new thresholds
            foreach (var dsName in _dataSourceNames)
            {
                _circuitStateStore.Initialize(
                    dsName,
                    newPolicy.Resilience.FailureThreshold,
                    newPolicy.Resilience.CircuitResetTimeout,
                    newPolicy.Resilience.ConsecutiveSuccessesToClose);
            }

            // Re-init cache scope if cache profile changed
            if (newPolicy.Cache.Enabled)
                InitializeCacheProvider();

            _dmeEditor.AddLogMessage(
                $"ProxyDataSource policy updated: {newPolicy.Name} v{newPolicy.Version} ({newPolicy.Resilience.ProfileType}).");
        }

        // ─────────────────────────────────────────────────────────────
        //  Bounded latency buffer (fixed-capacity circular array)
        // ─────────────────────────────────────────────────────────────

        private sealed class BoundedLatencyBuffer
        {
            private readonly long[] _samples;
            private readonly int    _capacity;
            private int             _index;
            private int             _count;
            private readonly object _lock = new object();

            public BoundedLatencyBuffer(int capacity)
            {
                _capacity = capacity;
                _samples  = new long[capacity];
            }

            public void Add(long ms)
            {
                lock (_lock)
                {
                    _samples[_index % _capacity] = ms;
                    _index++;
                    if (_count < _capacity) _count++;
                }
            }

            public (double P50, double P95, double P99) Percentiles()
            {
                long[] snapshot;
                int count;
                lock (_lock)
                {
                    count    = _count;
                    snapshot = new long[count];
                    // Reconstruct in insertion order from the circular buffer
                    int start = _count < _capacity ? 0 : _index % _capacity;
                    for (int i = 0; i < count; i++)
                        snapshot[i] = _samples[(start + i) % _capacity];
                }

                if (count == 0) return (0d, 0d, 0d);

                Array.Sort(snapshot);
                return (
                    PercentileValue(snapshot, 50),
                    PercentileValue(snapshot, 95),
                    PercentileValue(snapshot, 99)
                );
            }

            private static double PercentileValue(long[] sorted, int p)
            {
                if (sorted.Length == 0) return 0d;
                double index = (p / 100.0) * (sorted.Length - 1);
                int    lo    = (int)Math.Floor(index);
                int    hi    = Math.Min(lo + 1, sorted.Length - 1);
                double frac  = index - lo;
                return sorted[lo] * (1 - frac) + sorted[hi] * frac;
            }
        }
    }
}
