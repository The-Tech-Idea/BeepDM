using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Services.Audit.Integrity;
using TheTechIdea.Beep.Services.Telemetry;
using TheTechIdea.Beep.Services.Telemetry.Diagnostics;
using TheTechIdea.Beep.Services.Telemetry.Retention;

namespace TheTechIdea.Beep.Services.Audit
{
    /// <summary>
    /// Operator-facing configuration for the Beep audit-trail feature.
    /// Audit is lossless by policy so defaults differ from logging:
    /// <see cref="BackpressureMode.Block"/>, hash chain on, no samplers,
    /// budget breaches default to <see cref="BudgetBreachAction.BlockNewWrites"/>
    /// rather than deleting old records.
    /// </summary>
    /// <remarks>
    /// Phase 01 consumed only the toggle and budget fields. Phase 02 introduced
    /// the typed sink / redactor / enricher collections. Phase 04 adds
    /// <see cref="Rotation"/>, <see cref="Retention"/>, and <see cref="Budget"/>
    /// for the budget enforcer / sweeper integration. Audit deliberately
    /// exposes no sampler collection because audit events are lossless.
    /// </remarks>
    public sealed class BeepAuditOptions
    {
        /// <summary>
        /// When <c>false</c> (the default) the registration extension binds
        /// the null audit so the feature has zero runtime cost.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>Maximum envelopes that may sit in the bounded queue.</summary>
        public int QueueCapacity { get; set; } = TelemetryFeature.DefaultQueueCapacity;

        /// <summary>
        /// Behavior when the queue is full. Audit defaults to
        /// <see cref="BackpressureMode.Block"/> because events are lossless.
        /// </summary>
        public BackpressureMode BackpressureMode { get; set; } = BackpressureMode.Block;

        /// <summary>How often the batch writer drains the queue.</summary>
        public TimeSpan FlushInterval { get; set; } = TelemetryFeature.DefaultFlushInterval;

        /// <summary>How long <c>FlushAsync</c> waits during clean shutdown.</summary>
        public TimeSpan ShutdownTimeout { get; set; } = TelemetryFeature.DefaultShutdownTimeout;

        /// <summary>
        /// Hard cap on total bytes the audit feature may consume on disk.
        /// Audit defaults to a larger cap than logs because retention is
        /// usually longer.
        /// </summary>
        public long StorageBudgetBytes { get; set; } = TelemetryFeature.DefaultDesktopAuditBudgetBytes;

        /// <summary>Days to retain audit events before sweeping. 0 = forever.</summary>
        public int RetentionDays { get; set; } = TelemetryFeature.DefaultAuditRetentionDays;

        /// <summary>
        /// Enables the tamper-evident HMAC-SHA256 hash chain (Phase 08).
        /// Strongly recommended; turn off only in non-compliance environments.
        /// </summary>
        public bool HashChain { get; set; } = true;

        /// <summary>
        /// Forwards entries from the existing forms-level audit manager
        /// into the unified pipeline (Phase 09).
        /// </summary>
        public bool BridgeForms { get; set; } = true;

        /// <summary>
        /// Forwards entries from the proxy audit sink into the unified
        /// pipeline (Phase 09).
        /// </summary>
        public bool BridgeProxy { get; set; } = true;

        /// <summary>
        /// Forwards entries from the distributed audit sink into the
        /// unified pipeline (Phase 09 / distributed Phase 13).
        /// </summary>
        public bool BridgeDistributed { get; set; } = false;

        /// <summary>Pluggable telemetry sinks. Populated by extension methods.</summary>
        public IList<ITelemetrySink> Sinks { get; } = new List<ITelemetrySink>();

        /// <summary>Pluggable redactors. Populated by extension methods.</summary>
        public IList<IRedactor> Redactors { get; } = new List<IRedactor>();

        /// <summary>Pluggable enrichers. Populated by extension methods.</summary>
        public IList<IEnricher> Enrichers { get; } = new List<IEnricher>();

        /// <summary>
        /// Rotation policy applied to file-based audit sinks. Audit
        /// rotates more aggressively than logs so a single corrupt file
        /// loses fewer events.
        /// </summary>
        public RotationPolicy Rotation { get; set; } = new RotationPolicy
        {
            MaxFileBytes = 10L * 1024 * 1024,
            RollInterval = TimeSpan.FromHours(6)
        };

        /// <summary>Retention policy applied by the budget sweeper.</summary>
        public RetentionPolicy Retention { get; set; } = new RetentionPolicy
        {
            MaxAgeDays = TelemetryFeature.DefaultAuditRetentionDays,
            MaxFiles = 720
        };

        /// <summary>
        /// Storage budget. Defaults to
        /// <see cref="BudgetBreachAction.BlockNewWrites"/>: when audit
        /// records exceed the configured cap producers begin failing
        /// fast rather than deleting prior audit history. Operators must
        /// explicitly opt into <see cref="BudgetBreachAction.DeleteOldest"/>
        /// because doing so may breach compliance.
        /// </summary>
        public StorageBudget Budget { get; set; } = new StorageBudget
        {
            OnBreach = BudgetBreachAction.BlockNewWrites
        };

        /// <summary>
        /// When <c>true</c>, the registration helper schedules
        /// <see cref="RetentionSweeperHostedService"/> on the host's
        /// <c>IHostedService</c> pipeline.
        /// </summary>
        public bool EnableRetentionSweeper { get; set; } = false;

        /// <summary>Cadence of the retention sweeper when enabled.</summary>
        public TimeSpan SweepInterval { get; set; } = RetentionSweeperHostedService.DefaultSweepInterval;

        /// <summary>
        /// Optional override for the HMAC secret source. <c>null</c>
        /// means the DI extension will install
        /// <see cref="EnvironmentKeyMaterialProvider"/> reading from
        /// <c>BEEP_AUDIT_HMAC_SECRET</c>. Tests may inject a
        /// <see cref="StaticKeyMaterialProvider"/> here.
        /// </summary>
        public IKeyMaterialProvider KeyMaterial { get; set; }

        /// <summary>
        /// Optional override for the chain anchor store. <c>null</c>
        /// means the DI extension will install a
        /// <see cref="JsonChainAnchorStore"/> rooted at the audit
        /// directory.
        /// </summary>
        public IChainAnchorStore AnchorStore { get; set; }

        /// <summary>
        /// Optional override for the hash-chain signer. <c>null</c>
        /// means the DI extension will compose
        /// <see cref="HashChainSigner"/> from
        /// <see cref="KeyMaterial"/> + <see cref="AnchorStore"/>.
        /// Set to a custom implementation only for tests.
        /// </summary>
        public IHashChainSigner Signer { get; set; }

        /// <summary>
        /// Folder used by the default <see cref="JsonChainAnchorStore"/>.
        /// When <c>null</c> the registration extension defaults to the
        /// platform's audit directory (<c>PlatformPaths.AuditDir</c>).
        /// </summary>
        public string AnchorStoreDirectory { get; set; }

        /// <summary>
        /// When <c>true</c>, every rotated audit file is marked
        /// read-only (and chmod 0440 on Unix) by
        /// <see cref="SealedLogPolicy"/>. Defaults to <c>true</c>
        /// because the operational cost is negligible and the
        /// compliance value is high.
        /// </summary>
        public bool SealRotatedFiles { get; set; } = true;

        /// <summary>
        /// When set, the DI extension installs a
        /// <see cref="Purge.ConfirmTokenPurgePolicy"/> keyed by this
        /// value. Callers must pass the same token to
        /// <see cref="IBeepAudit.PurgeByUserAsync"/> and
        /// <see cref="IBeepAudit.PurgeByEntityAsync"/> for the purge
        /// to be authorized. Leaving this <c>null</c> disables the
        /// default policy; in that case the host must register a
        /// custom <see cref="Purge.IPurgePolicy"/>.
        /// </summary>
        public string PurgeConfirmationToken { get; set; }

        /// <summary>
        /// Optional override for the GDPR purge policy. <c>null</c>
        /// means the DI extension will derive a policy from
        /// <see cref="PurgeConfirmationToken"/>.
        /// </summary>
        public Purge.IPurgePolicy PurgePolicy { get; set; }

        /// <summary>
        /// When <c>true</c>, the DI extension scans the audit
        /// directory (<see cref="AnchorStoreDirectory"/> when set,
        /// otherwise <c>PlatformPaths.AuditDir</c>) and installs a
        /// <see cref="Query.FileScanAuditQueryEngine"/> alongside
        /// any database-backed engines. Useful when audit is
        /// configured to write NDJSON files instead of (or in
        /// addition to) SQLite.
        /// </summary>
        public bool EnableFileScanQuery { get; set; } = false;

        /// <summary>
        /// Optional folder used by the default
        /// <see cref="Query.FileScanAuditQueryEngine"/> when
        /// <see cref="EnableFileScanQuery"/> is <c>true</c>. Falls
        /// back to <see cref="AnchorStoreDirectory"/> and finally
        /// <c>PlatformPaths.AuditDir</c>.
        /// </summary>
        public string FileScanDirectory { get; set; }

        /// <summary>
        /// File-name prefix used by the default
        /// <see cref="Query.FileScanAuditQueryEngine"/>. Defaults to
        /// <c>"audit"</c> to match the recommended file sink prefix.
        /// </summary>
        public string FileScanPrefix { get; set; } = "audit";

        // ----- Phase 11 -- self-observability -------------------------------

        /// <summary>
        /// When <c>true</c>, the registration helper schedules
        /// <see cref="PeriodicMetricsSnapshotHostedService"/> for the
        /// audit pipeline. Off by default.
        /// </summary>
        public bool EnableMetricsSnapshot { get; set; } = false;

        /// <summary>Cadence used by the optional metrics snapshot service.</summary>
        public TimeSpan MetricsSnapshotInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// When set, periodic snapshots are written to this file path.
        /// Leave <c>null</c> to disable file output.
        /// </summary>
        public string MetricsSnapshotFile { get; set; }

        /// <summary>Format used when writing snapshots to disk.</summary>
        public MetricsSnapshotFormat MetricsSnapshotFormat { get; set; } = MetricsSnapshotFormat.Text;

        /// <summary>
        /// When <c>true</c>, periodic snapshots are emitted as
        /// <c>BeepTelemetry.Self.Snapshot</c> events through the
        /// audit pipeline.
        /// </summary>
        public bool EmitMetricsSnapshotAsSelfEvent { get; set; } = false;
    }
}
