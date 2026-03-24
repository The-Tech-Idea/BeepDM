using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Post-run reconciliation report produced by <see cref="BeepSyncManager"/> after a sync
    /// run completes.  Summarises row counts, DQ failures, reject channel stats, and mapping
    /// quality information for the run.
    /// </summary>
    public class SyncReconciliationReport
    {
        // ── Run identity ─────────────────────────────────────────────────────────

        /// <summary>Schema identifier this report belongs to.</summary>
        public string SchemaId { get; set; }

        /// <summary>Unique identifier for this specific run (matches the checkpoint RunId when available).</summary>
        public string RunId { get; set; }

        /// <summary>UTC timestamp when this report was generated.</summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>User or process that initiated the run.</summary>
        public string GeneratedBy { get; set; }

        // ── Row counts ───────────────────────────────────────────────────────────

        /// <summary>Total rows read from the source entity.</summary>
        public int SourceRowsScanned { get; set; }

        /// <summary>Total rows successfully written to the destination entity.</summary>
        public int DestRowsWritten { get; set; }

        /// <summary>Subset of <see cref="DestRowsWritten"/> that were inserts.</summary>
        public int DestRowsInserted { get; set; }

        /// <summary>Subset of <see cref="DestRowsWritten"/> that were updates.</summary>
        public int DestRowsUpdated { get; set; }

        /// <summary>Rows skipped (e.g. no change detected in delta mode).</summary>
        public int DestRowsSkipped { get; set; }

        /// <summary>Rows routed to the reject channel due to DQ failures.</summary>
        public int RejectCount { get; set; }

        /// <summary>Rows quarantined due to conflict resolution policy.</summary>
        public int QuarantineCount { get; set; }

        /// <summary>Rows where defaults were applied to fill missing destination fields.</summary>
        public int DefaultsFillCount { get; set; }

        /// <summary>Rows where a conflict was detected during bidirectional sync.</summary>
        public int ConflictCount { get; set; }

        // ── DQ summary ───────────────────────────────────────────────────────────

        /// <summary>Reject rate as a fraction 0–1 (<see cref="RejectCount"/> / <see cref="SourceRowsScanned"/>).</summary>
        public double RejectRate { get; set; }

        /// <summary>
        /// <c>true</c> when the batch-threshold rule triggered an AbortRun action
        /// and the run was halted before all source rows were processed.
        /// </summary>
        public bool RunAbortedByThreshold { get; set; }

        /// <summary>All DQ gate failures captured during the run (one entry per field/rule combination).</summary>
        public List<DqGateResult> DqFailures { get; set; } = new List<DqGateResult>();

        // ── Mapping quality ──────────────────────────────────────────────────────

        /// <summary>
        /// Mapping quality score evaluated by the Mapping Manager before the run (0–100).
        /// -1 indicates the quality check was skipped.
        /// </summary>
        public int MappingQualityScore { get; set; } = -1;

        /// <summary>Qualitative band returned by the Mapping Manager, e.g. "Good", "Fair", "Poor".</summary>
        public string MappingQualityBand { get; set; }

        /// <summary>
        /// Destination fields that received neither a source mapping value nor a defaults fill.
        /// These fields will contain null/default in the written record.
        /// </summary>
        public List<string> UnmappedRequiredFields { get; set; } = new List<string>();
    }
}
