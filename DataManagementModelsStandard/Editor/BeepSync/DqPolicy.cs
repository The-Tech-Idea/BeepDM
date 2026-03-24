using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Policy that controls which Data Quality gate rules are evaluated on each batch
    /// before records are written to the destination.
    /// Stored on <see cref="DataSyncSchema.DqPolicy"/>.
    /// </summary>
    public class DqPolicy
    {
        /// <summary>
        /// When <c>false</c> the entire DQ gate is skipped (default: <c>true</c>).
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Ordered list of Rule Engine keys to evaluate per record, e.g.
        /// <c>sync.dq.required-fields</c>, <c>sync.dq.type-validity</c>.
        /// Rules are applied in order; the first failure routes the record to the reject channel.
        /// </summary>
        public List<string> RuleKeys { get; set; } = new List<string>();

        /// <summary>
        /// Rule key evaluated at the end of each batch to decide whether
        /// the run should be aborted due to a high reject rate.
        /// Defaults to <c>sync.dq.batch-threshold</c>.
        /// </summary>
        public string BatchThresholdRuleKey { get; set; } = "sync.dq.batch-threshold";

        /// <summary>
        /// Maximum percentage of records that may be rejected before the batch-threshold
        /// rule triggers an <c>AbortRun</c> action (0–100; 0 = unlimited).
        /// Only used as a fallback when the rule engine is unavailable.
        /// </summary>
        public double MaxRejectRatePercent { get; set; } = 5.0;

        /// <summary>
        /// Optional name of the reject-channel data source.
        /// Rejected records are written here when set.
        /// </summary>
        public string RejectChannelDataSourceName { get; set; }

        /// <summary>
        /// Optional entity (table) name in the reject-channel data source.
        /// </summary>
        public string RejectChannelEntityName { get; set; }

        /// <summary>
        /// When <c>true</c>, the <see cref="SyncIntegrationContext.DefaultsManager"/> is
        /// invoked to fill missing destination fields <em>before</em> DQ rules are evaluated.
        /// Requires <see cref="SyncDefaultsPolicy.ApplyOnInsert"/> to also be set.
        /// </summary>
        public bool FillDefaultsBeforeEval { get; set; } = true;
    }
}
