using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Services.Telemetry.Diagnostics
{
    /// <summary>
    /// Frozen point-in-time snapshot returned by
    /// <see cref="PipelineMetrics.Snapshot"/>. The shape is intentionally
    /// flat and JSON-friendly so the
    /// <see cref="PeriodicMetricsSnapshotHostedService"/> can serialize it
    /// without bespoke conversions.
    /// </summary>
    /// <remarks>
    /// Counters are monotonic since startup; gauges are point-in-time.
    /// Drop counters are split by stage (sampler / dedup / rate-limit /
    /// queue-full / sink-error) so operators can see which stage is
    /// shedding load.
    /// </remarks>
    public sealed class MetricsSnapshot
    {
        /// <summary>Pipeline name (e.g. <c>logging</c> or <c>audit</c>).</summary>
        public string PipelineName { get; init; }

        /// <summary>UTC time the snapshot was taken.</summary>
        public DateTime CapturedUtc { get; init; } = DateTime.UtcNow;

        // Producers
        /// <summary>Total log envelopes accepted by the producer.</summary>
        public long LogEnqueuedTotal { get; init; }

        /// <summary>Total audit envelopes accepted by the producer.</summary>
        public long AuditEnqueuedTotal { get; init; }

        // Drops by stage
        /// <summary>Total log envelopes filtered by the sampler stage.</summary>
        public long DroppedSampledTotal { get; init; }

        /// <summary>Total log envelopes folded into a dedup window.</summary>
        public long DroppedDedupedTotal { get; init; }

        /// <summary>Total log envelopes rejected by the rate limiter.</summary>
        public long DroppedRateLimitedTotal { get; init; }

        /// <summary>Total envelopes dropped due to queue overflow.</summary>
        public long DroppedQueueFullTotal { get; init; }

        // Sinks
        /// <summary>Total per-sink exceptions caught by the batch writer.</summary>
        public long SinkErrorsTotal { get; init; }

        /// <summary>Per-sink health snapshots produced by the aggregator.</summary>
        public IReadOnlyList<SinkHealth> Sinks { get; init; }

        /// <summary>Pipeline-wide rollup of <see cref="Sinks"/>.</summary>
        public bool AllSinksHealthy { get; init; }

        // Retention / budget
        /// <summary>Total files deleted by the retention sweeper.</summary>
        public long SweeperDeletesTotal { get; init; }

        /// <summary>Total files compressed by the budget enforcer.</summary>
        public long SweeperCompressTotal { get; init; }

        /// <summary>Total storage budget breaches detected.</summary>
        public long BudgetBreachesTotal { get; init; }

        /// <summary>True if any scope is currently in <c>BlockNewWrites</c> state.</summary>
        public bool IsBlockingWrites { get; init; }

        // Audit chain
        /// <summary>Total audit events successfully signed.</summary>
        public long ChainSignedTotal { get; init; }

        /// <summary>Total chain verification runs completed.</summary>
        public long ChainVerifiedTotal { get; init; }

        /// <summary>Total chain divergences detected during verification.</summary>
        public long ChainDivergenceTotal { get; init; }

        // Self events
        /// <summary>Total self-observability envelopes emitted.</summary>
        public long SelfEventsEmittedTotal { get; init; }

        /// <summary>Total self events suppressed by the dedup window.</summary>
        public long SelfEventsDedupedTotal { get; init; }

        // Gauges
        /// <summary>Approximate current depth of the bounded queue.</summary>
        public int QueueDepthCurrent { get; init; }

        /// <summary>Configured queue capacity.</summary>
        public int QueueCapacity { get; init; }

        /// <summary>Most recent flush latency observed by the writer (ms).</summary>
        public long LastFlushLatencyMs { get; init; }

        /// <summary>Configured backpressure mode.</summary>
        public BackpressureMode BackpressureMode { get; init; }
    }
}
