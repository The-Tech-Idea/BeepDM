using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Telemetry.Diagnostics
{
    /// <summary>
    /// Snapshot assembly for <see cref="PipelineMetrics"/>. The snapshot
    /// is computed under no lock — every value is read via
    /// <see cref="System.Threading.Interlocked"/> or via a
    /// <see cref="System.Func{TResult}"/> provider, so callers see an
    /// eventually-consistent view that never tears.
    /// </summary>
    public sealed partial class PipelineMetrics
    {
        /// <summary>
        /// Captures the current counter / gauge values plus a per-sink
        /// health probe into a frozen <see cref="MetricsSnapshot"/>.
        /// </summary>
        public MetricsSnapshot Snapshot()
        {
            IReadOnlyList<SinkHealth> sinks = _healthAggregator.Probe();
            bool allHealthy = _healthAggregator.IsHealthy;

            return new MetricsSnapshot
            {
                PipelineName = PipelineName,
                CapturedUtc = DateTime.UtcNow,
                LogEnqueuedTotal = LogEnqueuedTotal,
                AuditEnqueuedTotal = AuditEnqueuedTotal,
                DroppedSampledTotal = DroppedSampledTotal,
                DroppedDedupedTotal = DroppedDedupedTotal,
                DroppedRateLimitedTotal = DroppedRateLimitedTotal,
                DroppedQueueFullTotal = DroppedQueueFullTotal,
                SinkErrorsTotal = SinkErrorsTotal,
                Sinks = sinks,
                AllSinksHealthy = allHealthy,
                SweeperDeletesTotal = SweeperDeletesTotal,
                SweeperCompressTotal = SweeperCompressTotal,
                BudgetBreachesTotal = BudgetBreachesTotal,
                IsBlockingWrites = IsBlockingWrites,
                ChainSignedTotal = ChainSignedTotal,
                ChainVerifiedTotal = ChainVerifiedTotal,
                ChainDivergenceTotal = ChainDivergenceTotal,
                SelfEventsEmittedTotal = SelfEventsEmittedTotal,
                SelfEventsDedupedTotal = SelfEventsDedupedTotal,
                QueueDepthCurrent = QueueDepth,
                QueueCapacity = QueueCapacity,
                LastFlushLatencyMs = LastFlushLatencyMs,
                BackpressureMode = BackpressureMode
            };
        }
    }
}
