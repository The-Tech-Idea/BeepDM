using System;
using System.Threading;

namespace TheTechIdea.Beep.Services.Telemetry.Diagnostics
{
    /// <summary>
    /// Self-observability counters and gauges owned by a single
    /// <see cref="TelemetryPipeline"/> instance. Designed to be free of
    /// external dependencies so it can be constructed eagerly during
    /// pipeline initialization and read concurrently from any thread.
    /// </summary>
    /// <remarks>
    /// Split into four partial files:
    /// <list type="bullet">
    ///   <item><c>.Core</c> — fields, constructor, public name, dispose-state.</item>
    ///   <item><c>.Counters</c> — <c>Increment*</c> helpers using <see cref="Interlocked"/>.</item>
    ///   <item><c>.Gauges</c> — point-in-time getters and setters.</item>
    ///   <item><c>.Snapshot</c> — frozen <see cref="MetricsSnapshot"/> assembly.</item>
    /// </list>
    /// One instance per pipeline so logging and audit metrics never
    /// collide. The <see cref="HealthAggregator"/> reference is supplied
    /// at construction so the snapshot can include per-sink health
    /// without re-walking the sink list every call.
    /// </remarks>
    public sealed partial class PipelineMetrics
    {
        private readonly HealthAggregator _healthAggregator;
        private readonly Func<int> _depthProvider;
        private readonly Func<int> _capacityProvider;
        private readonly Func<BackpressureMode> _modeProvider;

        // Producers (set on enqueue success).
        private long _logEnqueued;
        private long _auditEnqueued;

        // Drops by stage.
        private long _droppedSampled;
        private long _droppedDeduped;
        private long _droppedRateLimited;
        private long _droppedQueueFull;

        // Sinks.
        private long _sinkErrors;

        // Retention / budget.
        private long _sweeperDeletes;
        private long _sweeperCompress;
        private long _budgetBreaches;
        private int _isBlockingWrites;

        // Chain.
        private long _chainSigned;
        private long _chainVerified;
        private long _chainDivergence;

        // Self events.
        private long _selfEventsEmitted;
        private long _selfEventsDeduped;

        // Latency.
        private long _lastFlushLatencyMs;

        /// <summary>
        /// Creates a new metrics surface. <paramref name="pipelineName"/>
        /// shows up in every emitted self event so operators can tell
        /// the logging and audit pipelines apart at a glance.
        /// </summary>
        public PipelineMetrics(
            string pipelineName,
            HealthAggregator healthAggregator,
            Func<int> queueDepthProvider,
            Func<int> queueCapacityProvider,
            Func<BackpressureMode> backpressureModeProvider)
        {
            PipelineName = string.IsNullOrWhiteSpace(pipelineName) ? "pipeline" : pipelineName;
            _healthAggregator = healthAggregator ?? new HealthAggregator(Array.Empty<ITelemetrySink>());
            _depthProvider = queueDepthProvider ?? (() => 0);
            _capacityProvider = queueCapacityProvider ?? (() => 0);
            _modeProvider = backpressureModeProvider ?? (() => BackpressureMode.DropOldest);
        }

        /// <summary>Pipeline label used in self events and snapshots.</summary>
        public string PipelineName { get; }

        /// <summary>Aggregated sink health view (delegates to <see cref="HealthAggregator"/>).</summary>
        public HealthAggregator Sinks => _healthAggregator;
    }
}
