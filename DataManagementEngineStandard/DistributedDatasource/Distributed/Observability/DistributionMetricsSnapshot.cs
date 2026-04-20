using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Observability
{
    /// <summary>
    /// Immutable point-in-time snapshot aggregated by
    /// <see cref="IDistributedMetricsAggregator"/>. Captures
    /// distribution-tier counters plus a merged view of every
    /// shard cluster's <see cref="Proxy.DataSourceMetrics"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Snapshots are produced on demand via
    /// <see cref="IDistributedMetricsAggregator.Snapshot"/>; each
    /// call returns a fresh instance so callers can store it for
    /// diffing, export to OpenTelemetry, or rendering in a
    /// dashboard without risk of mutation from concurrent writes.
    /// </para>
    /// <para>
    /// Per-entity and per-shard maps are capped by an LRU
    /// (<c>MaxObservedEntities</c> / <c>MaxObservedShards</c>) so
    /// metric cardinality stays bounded in multi-tenant workloads.
    /// </para>
    /// </remarks>
    public sealed class DistributionMetricsSnapshot
    {
        /// <summary>Creates a new snapshot.</summary>
        public DistributionMetricsSnapshot(
            DateTime                                        capturedAtUtc,
            long                                            totalRequests,
            long                                            succeededRequests,
            long                                            failedRequests,
            IReadOnlyDictionary<string, ShardMetrics>       perShard,
            IReadOnlyDictionary<string, EntityMetrics>      perEntity,
            IReadOnlyDictionary<string, long>               perMode)
        {
            CapturedAtUtc     = capturedAtUtc;
            TotalRequests     = totalRequests;
            SucceededRequests = succeededRequests;
            FailedRequests    = failedRequests;
            PerShard          = perShard  ?? new Dictionary<string, ShardMetrics>(StringComparer.OrdinalIgnoreCase);
            PerEntity         = perEntity ?? new Dictionary<string, EntityMetrics>(StringComparer.OrdinalIgnoreCase);
            PerMode           = perMode   ?? new Dictionary<string, long>(StringComparer.Ordinal);
        }

        /// <summary>UTC timestamp at which the snapshot was assembled.</summary>
        public DateTime CapturedAtUtc { get; }

        /// <summary>Distribution-tier total request count.</summary>
        public long TotalRequests { get; }

        /// <summary>Distribution-tier successful request count.</summary>
        public long SucceededRequests { get; }

        /// <summary>Distribution-tier failed request count.</summary>
        public long FailedRequests { get; }

        /// <summary>
        /// Per-shard merged metrics: the aggregator sums the
        /// distribution-tier counters with the shard cluster's own
        /// <c>GetClusterMetrics</c> output so operators see a single
        /// number per shard.
        /// </summary>
        public IReadOnlyDictionary<string, ShardMetrics> PerShard { get; }

        /// <summary>Per-entity counters (request/success/fail rollup).</summary>
        public IReadOnlyDictionary<string, EntityMetrics> PerEntity { get; }

        /// <summary>
        /// Per-<see cref="Plan.DistributionMode"/> counters keyed by
        /// the enum's <c>ToString()</c> so the snapshot stays JSON
        /// serialisable without a custom converter.
        /// </summary>
        public IReadOnlyDictionary<string, long> PerMode { get; }
    }

    /// <summary>
    /// Per-shard slice of a <see cref="DistributionMetricsSnapshot"/>.
    /// </summary>
    public sealed class ShardMetrics
    {
        /// <summary>Creates a new shard-metrics snapshot.</summary>
        public ShardMetrics(
            string   shardId,
            long     totalRequests,
            long     succeededRequests,
            long     failedRequests,
            double   averageLatencyMs,
            double   p95LatencyMs,
            DateTime lastRequestedUtc)
        {
            ShardId           = shardId ?? string.Empty;
            TotalRequests     = totalRequests;
            SucceededRequests = succeededRequests;
            FailedRequests    = failedRequests;
            AverageLatencyMs  = averageLatencyMs;
            P95LatencyMs      = p95LatencyMs;
            LastRequestedUtc  = lastRequestedUtc;
        }

        /// <summary>Shard identifier.</summary>
        public string   ShardId           { get; }
        /// <summary>Total requests targeted at this shard.</summary>
        public long     TotalRequests     { get; }
        /// <summary>Successful requests targeted at this shard.</summary>
        public long     SucceededRequests { get; }
        /// <summary>Failed requests targeted at this shard.</summary>
        public long     FailedRequests    { get; }
        /// <summary>Rolling-window mean latency (ms).</summary>
        public double   AverageLatencyMs  { get; }
        /// <summary>Rolling-window 95th-percentile latency (ms).</summary>
        public double   P95LatencyMs      { get; }
        /// <summary>Timestamp of the most recent request (UTC).</summary>
        public DateTime LastRequestedUtc  { get; }
    }

    /// <summary>
    /// Per-entity slice of a <see cref="DistributionMetricsSnapshot"/>.
    /// </summary>
    public sealed class EntityMetrics
    {
        /// <summary>Creates a new entity-metrics snapshot.</summary>
        public EntityMetrics(
            string entityName,
            long   totalRequests,
            long   succeededRequests,
            long   failedRequests,
            double averageLatencyMs,
            double p95LatencyMs)
        {
            EntityName        = entityName ?? string.Empty;
            TotalRequests     = totalRequests;
            SucceededRequests = succeededRequests;
            FailedRequests    = failedRequests;
            AverageLatencyMs  = averageLatencyMs;
            P95LatencyMs      = p95LatencyMs;
        }

        /// <summary>Entity name.</summary>
        public string EntityName        { get; }
        /// <summary>Total requests targeted at this entity.</summary>
        public long   TotalRequests     { get; }
        /// <summary>Successful requests targeted at this entity.</summary>
        public long   SucceededRequests { get; }
        /// <summary>Failed requests targeted at this entity.</summary>
        public long   FailedRequests    { get; }
        /// <summary>Rolling-window mean latency (ms).</summary>
        public double AverageLatencyMs  { get; }
        /// <summary>Rolling-window 95th-percentile latency (ms).</summary>
        public double P95LatencyMs      { get; }
    }
}
