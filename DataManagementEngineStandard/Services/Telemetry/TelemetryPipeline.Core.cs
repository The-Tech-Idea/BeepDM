using System;
using System.Collections.Generic;
using System.Threading;
using TheTechIdea.Beep.Services.Telemetry.Dedup;
using TheTechIdea.Beep.Services.Telemetry.Diagnostics;
using TheTechIdea.Beep.Services.Telemetry.RateLimit;

namespace TheTechIdea.Beep.Services.Telemetry
{
    /// <summary>
    /// Shared, opt-in telemetry orchestrator that backs both
    /// <see cref="TheTechIdea.Beep.Services.Logging.IBeepLog"/> and
    /// <see cref="TheTechIdea.Beep.Services.Audit.IBeepAudit"/>. The pipeline
    /// holds the bounded queue, the enricher / redactor / sampler stages, the
    /// fan-out batch writer, and the lifetime management.
    /// </summary>
    /// <remarks>
    /// The class is split across three partial files:
    /// <list type="bullet">
    ///   <item><c>.Core</c> — fields, ctor, stage wiring, Submit entry points.</item>
    ///   <item><c>.Drain</c> — stage application before enqueue.</item>
    ///   <item><c>.Flush</c> — graceful shutdown / FlushAsync.</item>
    /// </list>
    /// Construction allocates exactly one queue and one drain task. When no
    /// sinks are registered the pipeline still accepts envelopes (they are
    /// dropped at dispatch) so callers can pre-wire before sinks land.
    /// </remarks>
    public sealed partial class TelemetryPipeline : IAsyncDisposable
    {
        private readonly BoundedChannelQueue _queue;
        private readonly BatchWriter _writer;
        private readonly IReadOnlyList<ITelemetrySink> _sinks;
        private readonly IReadOnlyList<IEnricher> _enrichers;
        private readonly IReadOnlyList<IRedactor> _redactors;
        private readonly IReadOnlyList<ISampler> _samplers;
        private readonly IMessageDeduper _deduper;
        private readonly IRateLimiter _rateLimiter;
        private readonly PipelineMetrics _metrics;
        private long _sampledOutCount;

        /// <summary>
        /// Raised once per per-sink exception caught by the batch writer.
        /// Phase 11 hooks subscribe here to feed
        /// <see cref="PipelineMetrics"/> and <see cref="SelfEventEmitter"/>.
        /// </summary>
        public event Action<string, Exception> SinkErrored;

        /// <summary>
        /// Creates a new pipeline. Caller owns the lifetime; dispose during
        /// host shutdown via <see cref="DisposeAsync"/>.
        /// </summary>
        public TelemetryPipeline(
            int queueCapacity,
            BackpressureMode backpressureMode,
            TimeSpan flushInterval,
            IReadOnlyList<ITelemetrySink> sinks,
            IReadOnlyList<IEnricher> enrichers = null,
            IReadOnlyList<IRedactor> redactors = null,
            IReadOnlyList<ISampler> samplers = null,
            IMessageDeduper deduper = null,
            IRateLimiter rateLimiter = null,
            string name = "pipeline")
        {
            Name = string.IsNullOrWhiteSpace(name) ? "pipeline" : name;
            _queue = new BoundedChannelQueue(queueCapacity, backpressureMode);
            _enrichers = enrichers ?? Array.Empty<IEnricher>();
            _redactors = redactors ?? Array.Empty<IRedactor>();
            _samplers = samplers ?? Array.Empty<ISampler>();
            _deduper = deduper;
            _rateLimiter = rateLimiter;
            _sinks = sinks ?? Array.Empty<ITelemetrySink>();
            _writer = new BatchWriter(_queue, _sinks, flushInterval, onSinkError: RaiseSinkErrored, onFlushLatency: RecordFlushLatency);
            _metrics = new PipelineMetrics(
                pipelineName: Name,
                healthAggregator: new HealthAggregator(_sinks),
                queueDepthProvider: () => _queue.CurrentDepth,
                queueCapacityProvider: () => _queue.Capacity,
                backpressureModeProvider: () => _queue.Mode);

            // Synthetic envelopes (dedup/rate-limit summaries) re-enter the
            // queue directly so they cannot themselves be deduped or
            // throttled — that would defeat the diagnostic.
            _deduper?.Bind(EnqueueSynthetic);
            _rateLimiter?.Bind(EnqueueSynthetic);
        }

        /// <summary>Pipeline name (e.g. <c>logging</c>, <c>audit</c>).</summary>
        public string Name { get; }

        /// <summary>Self-observability metrics owned by this pipeline.</summary>
        public PipelineMetrics Metrics => _metrics;

        /// <summary>
        /// Submits a self-observability envelope without re-running
        /// pre-queue stages (samplers / dedup / rate-limit). Used by
        /// <see cref="SelfEventEmitter"/>.
        /// </summary>
        public void EnqueueSelfEvent(TelemetryEnvelope envelope) => EnqueueSynthetic(envelope);

        private void RaiseSinkErrored(string sinkName, Exception ex)
        {
            Action<string, Exception> handler = SinkErrored;
            if (handler is null)
            {
                return;
            }
            try
            {
                handler(sinkName, ex);
            }
            catch
            {
                // Subscribers are best-effort; never let an observer break the writer.
            }
        }

        private void RecordFlushLatency(long milliseconds)
        {
            _metrics.RecordFlushLatency(milliseconds);
        }

        /// <summary>Total envelopes filtered by the sampler stage.</summary>
        public long SampledOutCount => Interlocked.Read(ref _sampledOutCount);

        /// <summary>Total envelopes folded into a dedup window.</summary>
        public long DedupSuppressedCount => _deduper?.SuppressedCount ?? 0;

        /// <summary>Total envelopes rejected by the rate limiter.</summary>
        public long RateLimitedCount => _rateLimiter?.DroppedCount ?? 0;

        private void EnqueueSynthetic(TelemetryEnvelope envelope)
        {
            if (envelope is null)
            {
                return;
            }
            _queue.TryEnqueue(envelope);
        }

        /// <summary>Configured queue capacity.</summary>
        public int QueueCapacity => _queue.Capacity;

        /// <summary>Configured backpressure mode.</summary>
        public BackpressureMode BackpressureMode => _queue.Mode;

        /// <summary>Total envelopes dropped due to queue overflow since startup.</summary>
        public long DroppedCount => _queue.DroppedCount;

        /// <summary>Total per-sink exceptions caught by the batch writer.</summary>
        public long SinkErrorCount => _writer.SinkErrorCount;

        /// <summary>Approximate current queue depth (sampling hint only).</summary>
        public int CurrentDepth => _queue.CurrentDepth;
    }
}
