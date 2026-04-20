using System;
using System.Collections.Generic;
using System.Diagnostics;
using TheTechIdea.Beep.Distributed.Observability;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// <see cref="DistributedDataSource"/> partial — Phase 13
    /// observability surface: metrics aggregator, hot-shard/hot-entity
    /// events, and <see cref="Activity"/> scoping.
    /// </summary>
    /// <remarks>
    /// The aggregator is lazy-built on first access so tests that opt
    /// out of metrics allocate nothing. All calls are resilient to
    /// subscriber exceptions — the hot path is never broken by a
    /// faulty listener.
    /// </remarks>
    public partial class DistributedDataSource
    {
        private IDistributedMetricsAggregator _metricsAggregator;
        private readonly object _metricsLock = new object();

        /// <summary>
        /// Raised when a shard's p95 latency breaches the
        /// configured hot-shard threshold for the configured number
        /// of consecutive windows.
        /// </summary>
        public event EventHandler<HotShardEventArgs> OnHotShardDetected;

        /// <summary>
        /// Raised when an entity's request rate breaches the
        /// configured hot-entity threshold.
        /// </summary>
        public event EventHandler<HotEntityEventArgs> OnHotEntityDetected;

        /// <summary>
        /// Returns the active aggregator. Built on demand so tests
        /// can short-circuit by stubbing
        /// <see cref="DistributedDataSourceOptions.MetricsAggregator"/>.
        /// </summary>
        public IDistributedMetricsAggregator MetricsAggregator
        {
            get
            {
                ThrowIfDisposed();
                EnsureMetricsAggregator();
                return _metricsAggregator;
            }
        }

        /// <summary>
        /// Captures a metrics snapshot safe to export or render.
        /// </summary>
        public DistributionMetricsSnapshot GetMetricsSnapshot()
        {
            ThrowIfDisposed();
            EnsureMetricsAggregator();
            return _metricsAggregator?.Snapshot();
        }

        /// <summary>
        /// Records a completed request at the distribution tier.
        /// Called by the read / write / DDL executors via the
        /// partials below; public so tests can drive it.
        /// </summary>
        internal void RecordDistributedRequest(
            string shardId,
            string entityName,
            string mode,
            double latencyMs,
            bool   succeeded)
        {
            if (!_options.EnableDistributedMetrics) return;
            EnsureMetricsAggregator();
            _metricsAggregator?.Record(shardId, entityName, mode, latencyMs, succeeded);
        }

        /// <summary>
        /// Starts an <see cref="Activity"/> scoped to a
        /// distribution-tier operation. Returns <c>null</c> when no
        /// listener is attached (zero overhead).
        /// </summary>
        internal Activity StartDistributedActivity(
            string operation,
            string entity        = null,
            string mode          = null,
            string matchKind     = null,
            string partitionKey  = null,
            IEnumerable<string> shardIds = null,
            string correlationId = null)
        {
            var activity = DistributedActivitySource.StartActivity(operation);
            if (activity == null) return null;

            string joined = shardIds == null ? null : string.Join(",", shardIds);
            return activity.SetDistributedTags(
                entity:        entity,
                mode:          mode,
                matchKind:     matchKind,
                partitionKey:  partitionKey,
                shardIds:      joined,
                correlationId: correlationId);
        }

        private void EnsureMetricsAggregator()
        {
            if (_metricsAggregator != null) return;
            if (!_options.EnableDistributedMetrics) return;

            lock (_metricsLock)
            {
                if (_metricsAggregator != null) return;

                if (_options.MetricsAggregator != null)
                {
                    _metricsAggregator = _options.MetricsAggregator;
                }
                else
                {
                    var detector = new HotShardDetector(
                        windowSize:         256,
                        p95ThresholdMs:     _options.HotShardP95ThresholdMs > 0 ? _options.HotShardP95ThresholdMs : 500.0,
                        consecutiveWindows: _options.HotShardConsecutiveWindows > 0 ? _options.HotShardConsecutiveWindows : 3);

                    _metricsAggregator = new DistributedMetricsAggregator(
                        shardsResolver:        () => _shards,
                        detector:              detector,
                        hotEntityThresholdRps: _options.HotEntityThresholdRps > 0 ? _options.HotEntityThresholdRps : 500.0);
                }

                _metricsAggregator.OnHotShardDetected += (_, args) =>
                {
                    try { OnHotShardDetected?.Invoke(this, args); }
                    catch (Exception ex)
                    {
                        RaisePassEventSafe("OnHotShardDetected handler failed: " + ex.Message);
                    }
                    RaiseAuditEvent(
                        kind:      Audit.DistributedAuditEventKind.HotShardDetected,
                        operation: "HotShard",
                        message:   $"Shard {args.ShardId} p95={args.P95LatencyMs:F1}ms (threshold {args.ThresholdMs:F1}ms, windows {args.ConsecutiveWindows})",
                        shardIds:  new[] { args.ShardId });
                };

                _metricsAggregator.OnHotEntityDetected += (_, args) =>
                {
                    try { OnHotEntityDetected?.Invoke(this, args); }
                    catch (Exception ex)
                    {
                        RaisePassEventSafe("OnHotEntityDetected handler failed: " + ex.Message);
                    }
                    RaiseAuditEvent(
                        kind:       Audit.DistributedAuditEventKind.HotEntityDetected,
                        operation:  "HotEntity",
                        entityName: args.EntityName,
                        message:    $"Entity {args.EntityName} rps={args.RequestsPerSecond:F1} (threshold {args.ThresholdRps:F1})");
                };
            }
        }
    }
}
