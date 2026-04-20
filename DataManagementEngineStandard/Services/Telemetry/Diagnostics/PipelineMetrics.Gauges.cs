using System.Threading;

namespace TheTechIdea.Beep.Services.Telemetry.Diagnostics
{
    /// <summary>
    /// Point-in-time getters and write-ables for
    /// <see cref="PipelineMetrics"/>. Distinguished from counters because
    /// gauges are not monotonic — the queue depth oscillates and the
    /// flush-latency value reflects the most recent batch.
    /// </summary>
    public sealed partial class PipelineMetrics
    {
        /// <summary>Approximate live queue depth.</summary>
        public int QueueDepth => _depthProvider();

        /// <summary>Configured queue capacity.</summary>
        public int QueueCapacity => _capacityProvider();

        /// <summary>Configured backpressure mode.</summary>
        public BackpressureMode BackpressureMode => _modeProvider();

        /// <summary>Most recent flush latency in milliseconds.</summary>
        public long LastFlushLatencyMs => Interlocked.Read(ref _lastFlushLatencyMs);

        /// <summary>True when the budget enforcer is currently blocking new writes for any scope.</summary>
        public bool IsBlockingWrites => Volatile.Read(ref _isBlockingWrites) != 0;

        /// <summary>Records the most recent flush latency reported by the writer.</summary>
        public void RecordFlushLatency(long milliseconds)
        {
            if (milliseconds < 0)
            {
                milliseconds = 0;
            }
            Interlocked.Exchange(ref _lastFlushLatencyMs, milliseconds);
        }

        /// <summary>Updates the blocking-writes flag from the budget enforcer.</summary>
        public void SetBlockingWrites(bool isBlocking)
        {
            Interlocked.Exchange(ref _isBlockingWrites, isBlocking ? 1 : 0);
        }

        // Read-only counter accessors (mirror IncrementXxx side of the API).

        /// <summary>Total log envelopes accepted by the producer.</summary>
        public long LogEnqueuedTotal => Interlocked.Read(ref _logEnqueued);

        /// <summary>Total audit envelopes accepted by the producer.</summary>
        public long AuditEnqueuedTotal => Interlocked.Read(ref _auditEnqueued);

        /// <summary>Total envelopes filtered by the sampler stage.</summary>
        public long DroppedSampledTotal => Interlocked.Read(ref _droppedSampled);

        /// <summary>Total envelopes folded into a dedup window.</summary>
        public long DroppedDedupedTotal => Interlocked.Read(ref _droppedDeduped);

        /// <summary>Total envelopes rejected by the rate limiter.</summary>
        public long DroppedRateLimitedTotal => Interlocked.Read(ref _droppedRateLimited);

        /// <summary>Total envelopes dropped due to queue overflow.</summary>
        public long DroppedQueueFullTotal => Interlocked.Read(ref _droppedQueueFull);

        /// <summary>Total per-sink exceptions caught by the batch writer.</summary>
        public long SinkErrorsTotal => Interlocked.Read(ref _sinkErrors);

        /// <summary>Total files deleted by the retention sweeper.</summary>
        public long SweeperDeletesTotal => Interlocked.Read(ref _sweeperDeletes);

        /// <summary>Total files compressed by the budget enforcer.</summary>
        public long SweeperCompressTotal => Interlocked.Read(ref _sweeperCompress);

        /// <summary>Total budget breaches detected by the sweeper.</summary>
        public long BudgetBreachesTotal => Interlocked.Read(ref _budgetBreaches);

        /// <summary>Total successful audit chain signs.</summary>
        public long ChainSignedTotal => Interlocked.Read(ref _chainSigned);

        /// <summary>Total audit chain verification passes.</summary>
        public long ChainVerifiedTotal => Interlocked.Read(ref _chainVerified);

        /// <summary>Total chain divergences detected during verification.</summary>
        public long ChainDivergenceTotal => Interlocked.Read(ref _chainDivergence);

        /// <summary>Total self-observability envelopes emitted.</summary>
        public long SelfEventsEmittedTotal => Interlocked.Read(ref _selfEventsEmitted);

        /// <summary>Total self-observability envelopes suppressed by the dedup window.</summary>
        public long SelfEventsDedupedTotal => Interlocked.Read(ref _selfEventsDeduped);
    }
}
