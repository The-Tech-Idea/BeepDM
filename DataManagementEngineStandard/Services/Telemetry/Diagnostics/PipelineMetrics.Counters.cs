using System.Threading;

namespace TheTechIdea.Beep.Services.Telemetry.Diagnostics
{
    /// <summary>
    /// Monotonic counter increments for <see cref="PipelineMetrics"/>.
    /// Every method is allocation-free and safe to call from the hot
    /// path; readers see eventually-consistent values via the matching
    /// <c>*.Read</c> properties.
    /// </summary>
    public sealed partial class PipelineMetrics
    {
        /// <summary>Counts a successfully enqueued log envelope.</summary>
        public void IncrementLogEnqueued() => Interlocked.Increment(ref _logEnqueued);

        /// <summary>Counts a successfully enqueued audit envelope.</summary>
        public void IncrementAuditEnqueued() => Interlocked.Increment(ref _auditEnqueued);

        /// <summary>Counts an envelope dropped by the sampler stage.</summary>
        public void IncrementDroppedSampled() => Interlocked.Increment(ref _droppedSampled);

        /// <summary>Counts an envelope folded into a dedup window.</summary>
        public void IncrementDroppedDeduped() => Interlocked.Increment(ref _droppedDeduped);

        /// <summary>Counts an envelope rejected by the rate limiter.</summary>
        public void IncrementDroppedRateLimited() => Interlocked.Increment(ref _droppedRateLimited);

        /// <summary>Counts an envelope dropped because the queue was full.</summary>
        public void IncrementDroppedQueueFull() => Interlocked.Increment(ref _droppedQueueFull);

        /// <summary>Counts a per-sink exception caught by the batch writer.</summary>
        public void IncrementSinkError() => Interlocked.Increment(ref _sinkErrors);

        /// <summary>Adds files-deleted to the retention sweeper counter.</summary>
        public void AddSweeperDeletes(long delta)
        {
            if (delta > 0)
            {
                Interlocked.Add(ref _sweeperDeletes, delta);
            }
        }

        /// <summary>Adds files-compressed to the budget enforcer counter.</summary>
        public void AddSweeperCompress(long delta)
        {
            if (delta > 0)
            {
                Interlocked.Add(ref _sweeperCompress, delta);
            }
        }

        /// <summary>Counts a storage-budget breach detected by the sweeper.</summary>
        public void IncrementBudgetBreach() => Interlocked.Increment(ref _budgetBreaches);

        /// <summary>Counts a successful audit chain sign.</summary>
        public void IncrementChainSigned() => Interlocked.Increment(ref _chainSigned);

        /// <summary>Counts a chain verification pass (regardless of outcome).</summary>
        public void IncrementChainVerified() => Interlocked.Increment(ref _chainVerified);

        /// <summary>Counts a chain divergence detected during verification.</summary>
        public void IncrementChainDivergence() => Interlocked.Increment(ref _chainDivergence);

        /// <summary>Counts an emitted self-observability envelope.</summary>
        public void IncrementSelfEventEmitted() => Interlocked.Increment(ref _selfEventsEmitted);

        /// <summary>Counts a self-observability envelope suppressed by the dedup window.</summary>
        public void IncrementSelfEventDeduped() => Interlocked.Increment(ref _selfEventsDeduped);
    }
}
