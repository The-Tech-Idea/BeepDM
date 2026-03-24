using System;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Checkpoint artifact persisted after each successful batch during a sync run.
    /// Enables partial-resume and idempotent replay without re-processing records already written.
    /// </summary>
    public class SyncCheckpoint
    {
        /// <summary>Unique run identifier for this sync execution.</summary>
        public string RunId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Schema Id this checkpoint belongs to.</summary>
        public string SchemaId { get; set; }

        /// <summary>Number of records successfully processed at the time this checkpoint was saved.</summary>
        public int ProcessedOffset { get; set; }

        /// <summary>Total number of records expected in this run (0 = unknown).</summary>
        public int TotalExpected { get; set; }

        /// <summary>Completion percentage derived from offset / total. 0 when total is unknown.</summary>
        public double ProgressPercent => TotalExpected > 0 ? 100.0 * ProcessedOffset / TotalExpected : 0;

        /// <summary>UTC timestamp when this checkpoint was saved.</summary>
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;

        /// <summary>User or service that saved this checkpoint.</summary>
        public string SavedBy { get; set; }

        /// <summary>
        /// Lifecycle state of this checkpoint.
        /// Values: <c>"InProgress"</c>, <c>"Completed"</c>, <c>"Failed"</c>, <c>"Stale"</c>.
        /// </summary>
        public string Status { get; set; } = "InProgress";

        /// <summary>Total number of run attempts (including current).</summary>
        public int AttemptCount { get; set; } = 1;

        /// <summary>Error category from the last failure, as classified by the retry rule engine.</summary>
        public string LastErrorCategory { get; set; }

        /// <summary>
        /// Compiled mapping plan identifier cached at run start for consistent field-level
        /// behaviour across retried batches.
        /// </summary>
        public string CompiledMappingPlanId { get; set; }

        /// <summary>Governance version of the mapping plan at checkpoint time, for drift detection on resume.</summary>
        public string MappingVersion { get; set; }

        /// <summary>
        /// Primary-key value of the last successfully written record in this run.
        /// Used as the idempotency anchor when resuming from a partial batch.
        /// </summary>
        public object LastProcessedKeyValue { get; set; }
    }
}
