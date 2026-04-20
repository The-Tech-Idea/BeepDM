using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Services.Logging;
using TheTechIdea.Beep.Services.Telemetry;
using TheTechIdea.Beep.Services.Telemetry.Dedup;
using TheTechIdea.Beep.Services.Telemetry.Diagnostics;
using TheTechIdea.Beep.Services.Telemetry.RateLimit;
using TheTechIdea.Beep.Services.Telemetry.Retention;

namespace TheTechIdea.Beep.Services.Logging
{
    /// <summary>
    /// Operator-facing configuration for the Beep logging feature.
    /// All disk-bound options carry a hard <see cref="StorageBudgetBytes"/>
    /// cap so the feature is safe to ship in storage-constrained environments.
    /// </summary>
    /// <remarks>
    /// Phase 01 consumed only the toggle and budget fields. Phase 02 introduced
    /// the typed sink / redactor / enricher / sampler collections that the
    /// shared telemetry pipeline reads on construction. Phase 04 adds the
    /// per-directory <see cref="Rotation"/>, <see cref="Retention"/>, and
    /// <see cref="Budget"/> POCOs consumed by the
    /// <see cref="DefaultBudgetEnforcer"/> and the optional
    /// <see cref="RetentionSweeperHostedService"/>.
    /// </remarks>
    public sealed class BeepLoggingOptions
    {
        /// <summary>
        /// When <c>false</c> (the default) the registration extension binds
        /// the null logger so the feature has zero runtime cost.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>Minimum severity processed by the pipeline.</summary>
        public BeepLogLevel MinLevel { get; set; } = BeepLogLevel.Information;

        /// <summary>Maximum envelopes that may sit in the bounded queue.</summary>
        public int QueueCapacity { get; set; } = TelemetryFeature.DefaultQueueCapacity;

        /// <summary>
        /// Behavior when the queue is full. Logs default to
        /// <see cref="BackpressureMode.DropOldest"/> so a noisy code path
        /// never blocks the producer.
        /// </summary>
        public BackpressureMode BackpressureMode { get; set; } = BackpressureMode.DropOldest;

        /// <summary>How often the batch writer drains the queue.</summary>
        public TimeSpan FlushInterval { get; set; } = TelemetryFeature.DefaultFlushInterval;

        /// <summary>How long <c>FlushAsync</c> waits during clean shutdown.</summary>
        public TimeSpan ShutdownTimeout { get; set; } = TelemetryFeature.DefaultShutdownTimeout;

        /// <summary>
        /// Hard cap on the total bytes the logging feature may consume on disk.
        /// Honored by the budget enforcer (Phase 04). Set to 0 to disable the
        /// cap (not recommended on mobile or browser hosts).
        /// </summary>
        public long StorageBudgetBytes { get; set; } = TelemetryFeature.DefaultDesktopStorageBudgetBytes;

        /// <summary>Pluggable telemetry sinks. Populated by extension methods.</summary>
        public IList<ITelemetrySink> Sinks { get; } = new List<ITelemetrySink>();

        /// <summary>Pluggable redactors. Populated by extension methods.</summary>
        public IList<IRedactor> Redactors { get; } = new List<IRedactor>();

        /// <summary>Pluggable enrichers. Populated by extension methods.</summary>
        public IList<IEnricher> Enrichers { get; } = new List<IEnricher>();

        /// <summary>Pluggable samplers. Populated by extension methods.</summary>
        public IList<ISampler> Samplers { get; } = new List<ISampler>();

        /// <summary>
        /// Optional message deduper. <c>null</c> disables the dedup stage so
        /// every envelope continues straight to the rate limiter / queue.
        /// </summary>
        public IMessageDeduper Deduper { get; set; }

        /// <summary>
        /// Optional rate limiter. <c>null</c> disables the rate-limit stage
        /// so every envelope continues straight to the queue.
        /// </summary>
        public IRateLimiter RateLimiter { get; set; }

        /// <summary>Rotation policy applied to file-based sinks.</summary>
        public RotationPolicy Rotation { get; set; } = new RotationPolicy();

        /// <summary>Retention policy applied by the budget sweeper.</summary>
        public RetentionPolicy Retention { get; set; } = new RetentionPolicy();

        /// <summary>
        /// Storage budget applied by the sweeper. Defaults to
        /// <see cref="BudgetBreachAction.DeleteOldest"/> because logs
        /// are disposable.
        /// </summary>
        public StorageBudget Budget { get; set; } = new StorageBudget
        {
            OnBreach = BudgetBreachAction.DeleteOldest
        };

        /// <summary>
        /// When <c>true</c>, the registration helper schedules
        /// <see cref="RetentionSweeperHostedService"/> on the host's
        /// <c>IHostedService</c> pipeline. Off by default so non-hosted
        /// callers (console tools, ad-hoc tests) do not pay for a timer
        /// they do not need.
        /// </summary>
        public bool EnableRetentionSweeper { get; set; } = false;

        /// <summary>Cadence of the retention sweeper when enabled.</summary>
        public TimeSpan SweepInterval { get; set; } = RetentionSweeperHostedService.DefaultSweepInterval;

        /// <summary>
        /// When <c>true</c> (the default once the feature is enabled) the
        /// registration helper replaces any existing <c>IDMLogger</c>
        /// registration with a bridge that forwards every legacy
        /// <c>WriteLog</c>/<c>LogError</c>/<c>LogWarning</c> call into the
        /// unified pipeline. Set to <c>false</c> to keep the original
        /// <c>DMLogger</c> alongside <see cref="IBeepLog"/>.
        /// </summary>
        public bool ReplaceDMLogger { get; set; } = true;

        /// <summary>
        /// When <c>true</c> the registration helper installs
        /// <c>MicrosoftLoggerProvider</c> as an
        /// <see cref="Microsoft.Extensions.Logging.ILoggerProvider"/>
        /// so frameworks already using <c>Microsoft.Extensions.Logging</c>
        /// (ASP.NET Core, EF, gRPC, hosted services) flow into the unified
        /// pipeline without duplicate plumbing.
        /// </summary>
        public bool RegisterMicrosoftLoggerProvider { get; set; } = true;

        // ----- Phase 11 -- self-observability -------------------------------
        // The metrics counter struct is allocated on every pipeline regardless
        // of these flags so callers can probe it from ad-hoc diagnostics. The
        // toggles below only control automatic reporting (snapshot file or
        // self-event emission).

        /// <summary>
        /// When <c>true</c>, the registration helper schedules
        /// <see cref="PeriodicMetricsSnapshotHostedService"/> on the host's
        /// <c>IHostedService</c> pipeline. Off by default so non-hosted
        /// callers (console tools, ad-hoc tests) do not pay for a timer.
        /// </summary>
        public bool EnableMetricsSnapshot { get; set; } = false;

        /// <summary>Cadence used by the optional metrics snapshot service.</summary>
        public TimeSpan MetricsSnapshotInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// When set, periodic snapshots are written to this file path
        /// (rendered as <see cref="MetricsSnapshotFormat"/>). Leave
        /// <c>null</c> to disable file output.
        /// </summary>
        public string MetricsSnapshotFile { get; set; }

        /// <summary>Format used when writing snapshots to disk.</summary>
        public MetricsSnapshotFormat MetricsSnapshotFormat { get; set; } = MetricsSnapshotFormat.Text;

        /// <summary>
        /// When <c>true</c>, periodic snapshots are also emitted as
        /// <c>BeepTelemetry.Self.Snapshot</c> events through the pipeline.
        /// </summary>
        public bool EmitMetricsSnapshotAsSelfEvent { get; set; } = false;
    }
}
