using System;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// The result of evaluating a single Data Quality gate rule against a record or field.
    /// Captured in <see cref="SyncReconciliationReport.DqFailures"/> for every record
    /// that is routed to the reject channel during a sync run.
    /// </summary>
    public class DqGateResult
    {
        /// <summary>
        /// The rule-engine key that was evaluated, e.g. <c>sync.dq.required-fields</c>.
        /// </summary>
        public string RuleKey { get; set; }

        /// <summary>
        /// <c>true</c> when the DQ check passed; <c>false</c> when the record was rejected.
        /// </summary>
        public bool Passed { get; set; }

        /// <summary>
        /// Short machine-readable code returned from the rule, e.g. <c>DQ-FAIL</c>,
        /// <c>NULL-REQUIRED</c>, <c>TYPE-MISMATCH</c>.
        /// </summary>
        public string ReasonCode { get; set; }

        /// <summary>
        /// Optional field name that caused the failure (when available from the rule outputs).
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// Human-readable message describing why the DQ check failed.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The name of the entity that was being validated.
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// UTC timestamp when this DQ result was captured.
        /// </summary>
        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
    }
}
