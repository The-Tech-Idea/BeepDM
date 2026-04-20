using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Observability
{
    /// <summary>
    /// Thread-safe implementation of
    /// <see cref="IDistributedMetricsAggregator"/>. Records each
    /// request via <see cref="Record"/> and, on
    /// <see cref="Snapshot"/>, merges distribution-tier counters
    /// with the shard clusters'
    /// <see cref="IProxyCluster.GetClusterMetrics"/> output.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cardinality guard: <see cref="MaxObservedEntities"/> bounds
    /// the per-entity map with a simple LRU eviction so a runaway
    /// workload cannot blow up memory. The same bound is applied
    /// to the per-shard map via <see cref="MaxObservedShards"/>.
    /// </para>
    /// <para>
    /// Latency rolling windows are delegated to
    /// <see cref="HotShardDetector"/>; the detector's event is
    /// forwarded as <see cref="OnHotShardDetected"/>.
    /// </para>
    /// </remarks>
    public sealed class DistributedMetricsAggregator : IDistributedMetricsAggregator
    {
        private readonly Func<IReadOnlyDictionary<string, IProxyCluster>> _shardsResolver;
        private readonly HotShardDetector _detector;

        private long _total;
        private long _succeeded;
        private long _failed;

        private readonly ConcurrentDictionary<string, ShardState>  _perShard;
        private readonly ConcurrentDictionary<string, EntityState> _perEntity;
        private readonly ConcurrentDictionary<string, long>        _perMode;

        private readonly int _maxEntities;
        private readonly int _maxShards;

        private readonly object _hotEntityLock = new object();
        private DateTime _hotEntityWindowStartUtc = DateTime.UtcNow;
        private readonly ConcurrentDictionary<string, long> _entityWindowCount
            = new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Ceiling on per-entity tracked keys (default 1024).</summary>
        public int MaxObservedEntities => _maxEntities;

        /// <summary>Ceiling on per-shard tracked keys (default 256).</summary>
        public int MaxObservedShards => _maxShards;

        /// <summary>Request-rate threshold (RPS) for hot-entity detection.</summary>
        public double HotEntityThresholdRps { get; }

        /// <summary>Detection window size in seconds (default 10).</summary>
        public int HotEntityWindowSeconds { get; }

        /// <inheritdoc/>
        public event EventHandler<HotShardEventArgs>  OnHotShardDetected;

        /// <inheritdoc/>
        public event EventHandler<HotEntityEventArgs> OnHotEntityDetected;

        /// <summary>Creates a new aggregator.</summary>
        /// <param name="shardsResolver">
        /// Delegate returning the live shard map. Invoked on
        /// <see cref="Snapshot"/> so the aggregator always sees the
        /// latest topology after an online plan swap.
        /// </param>
        /// <param name="detector">
        /// Optional custom hot-shard detector; a default-configured
        /// instance is created when <c>null</c>.
        /// </param>
        /// <param name="maxObservedEntities">Max tracked entities.</param>
        /// <param name="maxObservedShards">Max tracked shards.</param>
        /// <param name="hotEntityThresholdRps">Hot-entity RPS threshold.</param>
        /// <param name="hotEntityWindowSeconds">Hot-entity window seconds.</param>
        public DistributedMetricsAggregator(
            Func<IReadOnlyDictionary<string, IProxyCluster>> shardsResolver,
            HotShardDetector                                 detector               = null,
            int                                              maxObservedEntities    = 1024,
            int                                              maxObservedShards      = 256,
            double                                           hotEntityThresholdRps  = 500.0,
            int                                              hotEntityWindowSeconds = 10)
        {
            _shardsResolver = shardsResolver ?? (() => EmptyShardMap);
            _detector       = detector ?? new HotShardDetector();
            _maxEntities    = Math.Max(16,  maxObservedEntities);
            _maxShards      = Math.Max(8,   maxObservedShards);

            HotEntityThresholdRps   = hotEntityThresholdRps > 0 ? hotEntityThresholdRps : 500.0;
            HotEntityWindowSeconds  = hotEntityWindowSeconds > 0 ? hotEntityWindowSeconds : 10;

            _perShard  = new ConcurrentDictionary<string, ShardState>(StringComparer.OrdinalIgnoreCase);
            _perEntity = new ConcurrentDictionary<string, EntityState>(StringComparer.OrdinalIgnoreCase);
            _perMode   = new ConcurrentDictionary<string, long>(StringComparer.Ordinal);

            _detector.OnHotShardDetected += (_, args) =>
            {
                try { OnHotShardDetected?.Invoke(this, args); }
                catch { /* never throw from event forwarding */ }
            };
        }

        private static readonly IReadOnlyDictionary<string, IProxyCluster> EmptyShardMap
            = new Dictionary<string, IProxyCluster>();

        /// <inheritdoc/>
        public void Record(
            string shardId,
            string entityName,
            string mode,
            double latencyMs,
            bool   succeeded)
        {
            try
            {
                Interlocked.Increment(ref _total);
                if (succeeded) Interlocked.Increment(ref _succeeded);
                else           Interlocked.Increment(ref _failed);

                if (!string.IsNullOrWhiteSpace(shardId))
                {
                    var shard = GetOrCreateShard(shardId);
                    shard.Record(latencyMs, succeeded);
                    _detector.RecordLatency(shardId, latencyMs);
                }

                if (!string.IsNullOrWhiteSpace(entityName))
                {
                    var entity = GetOrCreateEntity(entityName);
                    entity.Record(latencyMs, succeeded);
                    TrackEntityRate(entityName);
                }

                if (!string.IsNullOrWhiteSpace(mode))
                {
                    _perMode.AddOrUpdate(mode, 1L, (_, prev) => prev + 1L);
                }
            }
            catch
            {
                // Guard hot path: aggregator never throws to callers.
            }
        }

        /// <inheritdoc/>
        public DistributionMetricsSnapshot Snapshot()
        {
            // Merge distribution-tier counters with per-cluster metrics.
            IReadOnlyDictionary<string, IProxyCluster> shards;
            try { shards = _shardsResolver() ?? EmptyShardMap; }
            catch { shards = EmptyShardMap; }

            var perShardResult  = new Dictionary<string, ShardMetrics>(StringComparer.OrdinalIgnoreCase);
            var perEntityResult = new Dictionary<string, EntityMetrics>(StringComparer.OrdinalIgnoreCase);
            var perModeResult   = new Dictionary<string, long>(StringComparer.Ordinal);

            foreach (var kv in _perShard)
            {
                var s = kv.Value;
                double avg = s.Average;
                double p95 = _detector.GetP95(kv.Key);
                long shardTotal = s.Total;
                long shardOk    = s.Succeeded;
                long shardFail  = s.Failed;

                if (shards.TryGetValue(kv.Key, out var cluster) && cluster != null)
                {
                    try
                    {
                        var clusterMetrics = cluster.GetClusterMetrics();
                        if (clusterMetrics != null)
                        {
                            foreach (var cm in clusterMetrics.Values)
                            {
                                if (cm == null) continue;
                                shardTotal += cm.TotalRequests;
                                shardOk    += cm.SuccessfulRequests;
                                shardFail  += cm.FailedRequests;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore shard-side metric failures.
                    }
                }

                perShardResult[kv.Key] = new ShardMetrics(
                    shardId:           kv.Key,
                    totalRequests:     shardTotal,
                    succeededRequests: shardOk,
                    failedRequests:    shardFail,
                    averageLatencyMs:  avg,
                    p95LatencyMs:      p95,
                    lastRequestedUtc:  s.LastRequestedUtc);
            }

            foreach (var kv in _perEntity)
            {
                var e = kv.Value;
                perEntityResult[kv.Key] = new EntityMetrics(
                    entityName:        kv.Key,
                    totalRequests:     e.Total,
                    succeededRequests: e.Succeeded,
                    failedRequests:    e.Failed,
                    averageLatencyMs:  e.Average,
                    p95LatencyMs:      0.0); // per-entity p95 deferred to Phase 14
            }

            foreach (var kv in _perMode) perModeResult[kv.Key] = kv.Value;

            return new DistributionMetricsSnapshot(
                capturedAtUtc:     DateTime.UtcNow,
                totalRequests:     Interlocked.Read(ref _total),
                succeededRequests: Interlocked.Read(ref _succeeded),
                failedRequests:    Interlocked.Read(ref _failed),
                perShard:          perShardResult,
                perEntity:         perEntityResult,
                perMode:           perModeResult);
        }

        private ShardState GetOrCreateShard(string shardId)
        {
            if (_perShard.TryGetValue(shardId, out var existing)) return existing;

            if (_perShard.Count >= _maxShards)
            {
                EvictOldestShard();
            }
            return _perShard.GetOrAdd(shardId, _ => new ShardState());
        }

        private EntityState GetOrCreateEntity(string entityName)
        {
            if (_perEntity.TryGetValue(entityName, out var existing)) return existing;

            if (_perEntity.Count >= _maxEntities)
            {
                EvictOldestEntity();
            }
            return _perEntity.GetOrAdd(entityName, _ => new EntityState());
        }

        private void EvictOldestShard()
        {
            string oldest = null;
            DateTime oldestTs = DateTime.MaxValue;
            foreach (var kv in _perShard)
            {
                if (kv.Value.LastRequestedUtc < oldestTs)
                {
                    oldestTs = kv.Value.LastRequestedUtc;
                    oldest = kv.Key;
                }
            }
            if (oldest != null) _perShard.TryRemove(oldest, out _);
        }

        private void EvictOldestEntity()
        {
            string oldest = null;
            DateTime oldestTs = DateTime.MaxValue;
            foreach (var kv in _perEntity)
            {
                if (kv.Value.LastRequestedUtc < oldestTs)
                {
                    oldestTs = kv.Value.LastRequestedUtc;
                    oldest = kv.Key;
                }
            }
            if (oldest != null) _perEntity.TryRemove(oldest, out _);
        }

        private void TrackEntityRate(string entityName)
        {
            _entityWindowCount.AddOrUpdate(entityName, 1L, (_, prev) => prev + 1L);

            // Roll the window if enough time has passed.
            DateTime now = DateTime.UtcNow;
            TimeSpan elapsed;
            lock (_hotEntityLock) elapsed = now - _hotEntityWindowStartUtc;
            if (elapsed.TotalSeconds < HotEntityWindowSeconds) return;

            Dictionary<string, long> snapshot;
            lock (_hotEntityLock)
            {
                if ((DateTime.UtcNow - _hotEntityWindowStartUtc).TotalSeconds < HotEntityWindowSeconds)
                    return;

                snapshot = _entityWindowCount.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value,
                    StringComparer.OrdinalIgnoreCase);
                _entityWindowCount.Clear();
                _hotEntityWindowStartUtc = DateTime.UtcNow;
            }

            double windowSeconds = Math.Max(1.0, HotEntityWindowSeconds);
            foreach (var kv in snapshot)
            {
                double rps = kv.Value / windowSeconds;
                if (rps >= HotEntityThresholdRps)
                {
                    RaiseHotEntity(kv.Key, rps);
                }
            }
        }

        private void RaiseHotEntity(string entityName, double rps)
        {
            var handler = OnHotEntityDetected;
            if (handler == null) return;
            try
            {
                handler(this, new HotEntityEventArgs(
                    entityName:        entityName,
                    requestsPerSecond: rps,
                    thresholdRps:      HotEntityThresholdRps,
                    observedAtUtc:     DateTime.UtcNow));
            }
            catch
            {
                // Never throw from aggregation.
            }
        }

        private sealed class ShardState
        {
            private long _total;
            private long _succeeded;
            private long _failed;
            private double _latencySum;
            private DateTime _lastRequestedUtc = DateTime.MinValue;
            private readonly object _lock = new object();

            public long Total => Interlocked.Read(ref _total);
            public long Succeeded => Interlocked.Read(ref _succeeded);
            public long Failed => Interlocked.Read(ref _failed);

            public double Average
            {
                get
                {
                    lock (_lock)
                    {
                        long t = _total;
                        return t == 0 ? 0.0 : _latencySum / t;
                    }
                }
            }

            public DateTime LastRequestedUtc
            {
                get { lock (_lock) return _lastRequestedUtc; }
            }

            public void Record(double latencyMs, bool succeeded)
            {
                Interlocked.Increment(ref _total);
                if (succeeded) Interlocked.Increment(ref _succeeded);
                else           Interlocked.Increment(ref _failed);
                lock (_lock)
                {
                    _latencySum += latencyMs;
                    _lastRequestedUtc = DateTime.UtcNow;
                }
            }
        }

        private sealed class EntityState
        {
            private long _total;
            private long _succeeded;
            private long _failed;
            private double _latencySum;
            private DateTime _lastRequestedUtc = DateTime.MinValue;
            private readonly object _lock = new object();

            public long Total => Interlocked.Read(ref _total);
            public long Succeeded => Interlocked.Read(ref _succeeded);
            public long Failed => Interlocked.Read(ref _failed);

            public double Average
            {
                get
                {
                    lock (_lock)
                    {
                        long t = _total;
                        return t == 0 ? 0.0 : _latencySum / t;
                    }
                }
            }

            public DateTime LastRequestedUtc
            {
                get { lock (_lock) return _lastRequestedUtc; }
            }

            public void Record(double latencyMs, bool succeeded)
            {
                Interlocked.Increment(ref _total);
                if (succeeded) Interlocked.Increment(ref _succeeded);
                else           Interlocked.Increment(ref _failed);
                lock (_lock)
                {
                    _latencySum += latencyMs;
                    _lastRequestedUtc = DateTime.UtcNow;
                }
            }
        }
    }
}
