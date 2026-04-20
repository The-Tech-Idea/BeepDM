using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Services.Telemetry
{
    /// <summary>
    /// Stage application + enqueue half of <see cref="TelemetryPipeline"/>.
    /// Logging producers call <see cref="SubmitLog"/> from the synchronous
    /// hot path; audit producers call <see cref="SubmitAuditAsync"/> so
    /// <see cref="BackpressureMode.Block"/> semantics are honored.
    /// </summary>
    public sealed partial class TelemetryPipeline
    {
        /// <summary>
        /// Synchronous submit used by <see cref="TheTechIdea.Beep.Services.Logging.IBeepLog"/>.
        /// Applies enrichers, redactors, samplers (in that order) before
        /// attempting a non-blocking enqueue. Returns <c>false</c> when the
        /// envelope was dropped (filtered out or queue overflow).
        /// </summary>
        public bool SubmitLog(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return false;
            }

            ApplyEnrichers(envelope);
            ApplyRedactors(envelope);

            if (envelope.Kind == TelemetryKind.Log)
            {
                if (!PassesSamplers(envelope))
                {
                    Interlocked.Increment(ref _sampledOutCount);
                    _metrics.IncrementDroppedSampled();
                    return false;
                }
                if (_deduper is not null && !_deduper.TryAccept(envelope))
                {
                    _metrics.IncrementDroppedDeduped();
                    return false;
                }
                if (_rateLimiter is not null && !_rateLimiter.TryAcquire(envelope))
                {
                    _metrics.IncrementDroppedRateLimited();
                    return false;
                }
            }

            long droppedBefore = _queue.DroppedCount;
            bool enqueued = _queue.TryEnqueue(envelope);
            long droppedAfter = _queue.DroppedCount;
            if (droppedAfter > droppedBefore)
            {
                _metrics.IncrementDroppedQueueFull();
            }
            if (enqueued && envelope.Kind == TelemetryKind.Log)
            {
                _metrics.IncrementLogEnqueued();
            }
            return enqueued;
        }

        /// <summary>
        /// Asynchronous submit used by <see cref="TheTechIdea.Beep.Services.Audit.IBeepAudit"/>.
        /// Applies enrichers and redactors but always bypasses samplers, then
        /// awaits queue capacity per the configured backpressure mode.
        /// </summary>
        public ValueTask SubmitAuditAsync(TelemetryEnvelope envelope, CancellationToken cancellationToken)
        {
            if (envelope is null)
            {
                return default;
            }

            ApplyEnrichers(envelope);
            ApplyRedactors(envelope);

            long droppedBefore = _queue.DroppedCount;
            try
            {
                ValueTask vt = _queue.EnqueueAsync(envelope, cancellationToken);
                if (vt.IsCompletedSuccessfully)
                {
                    AccountAuditEnqueue(droppedBefore);
                    return vt;
                }
                return AwaitAuditEnqueueAsync(vt, droppedBefore);
            }
            catch
            {
                if (_queue.DroppedCount > droppedBefore)
                {
                    _metrics.IncrementDroppedQueueFull();
                }
                throw;
            }
        }

        private async ValueTask AwaitAuditEnqueueAsync(ValueTask pending, long droppedBefore)
        {
            try
            {
                await pending.ConfigureAwait(false);
            }
            catch
            {
                if (_queue.DroppedCount > droppedBefore)
                {
                    _metrics.IncrementDroppedQueueFull();
                }
                throw;
            }
            AccountAuditEnqueue(droppedBefore);
        }

        private void AccountAuditEnqueue(long droppedBefore)
        {
            if (_queue.DroppedCount > droppedBefore)
            {
                _metrics.IncrementDroppedQueueFull();
                return;
            }
            _metrics.IncrementAuditEnqueued();
        }

        private void ApplyEnrichers(TelemetryEnvelope envelope)
        {
            for (int i = 0; i < _enrichers.Count; i++)
            {
                try
                {
                    _enrichers[i].Enrich(envelope);
                }
                catch
                {
                    // Enrichers must not break the pipeline. Phase 11 will surface
                    // these failures through PipelineMetrics.
                }
            }
        }

        private void ApplyRedactors(TelemetryEnvelope envelope)
        {
            for (int i = 0; i < _redactors.Count; i++)
            {
                try
                {
                    _redactors[i].Redact(envelope);
                }
                catch
                {
                    // Same isolation rule as enrichers.
                }
            }
        }

        private bool PassesSamplers(TelemetryEnvelope envelope)
        {
            for (int i = 0; i < _samplers.Count; i++)
            {
                try
                {
                    if (!_samplers[i].ShouldSample(envelope))
                    {
                        return false;
                    }
                }
                catch
                {
                    // A throwing sampler defaults to "keep" so we never silently
                    // discard data because of a bad plugin.
                }
            }
            return true;
        }
    }
}
