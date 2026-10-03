using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Editor.Importing;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Policy for per-record post-transform/pre-write Data Quality gates.
    /// Threshold admission is captured separately; default integration retains legacy compatibility.
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
        /// <summary>Required lookup/evaluation failures deny a row; Advisory failures produce warnings.</summary>
        public QualityFailureMode RecordFailureMode { get; set; } = QualityFailureMode.Required;
        public DataQualityAction OnRecordFailure { get; set; } = DataQualityAction.Block;

        /// <summary>
        /// Rule key evaluated once after an attempt's admitted imports (both directions),
        /// including partial failures. Outputs must contain action: ContinueRun or AbortRun.
        /// Defaults to <c>sync.dq.batch-threshold</c>.
        /// </summary>
        public string BatchThresholdRuleKey { get; set; } = "sync.dq.batch-threshold";

        /// <summary>Explicit opt-out for record-only policies; enabled thresholds require a rule by default.</summary>
        public bool BatchThresholdEnabled { get; set; } = true;

        /// <summary>Required decisions/errors block completion; Advisory decisions/errors produce counted evidence.</summary>
        public QualityFailureMode ThresholdFailureMode { get; set; } = QualityFailureMode.Required;

        /// <summary>
        /// Finite 0-100 limit. Reject rate is quality-rejected rows / attempted import rows,
        /// combined across admitted directions. Empty attempts have rate zero.
        /// Exceeding this limit or an AbortRun action rejects the threshold decision.
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
        /// Retained compatibility property, not an additional default stage.
        /// Record admission observes the result of the configured import transformation/default pipeline.
        /// </summary>
        public bool FillDefaultsBeforeEval { get; set; } = true;
    }
}
