using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.Importing
{
    public enum ImportOutcome { Completed, Partial, Failed, Cancelled }

    /// <summary>
    /// Write acknowledgements, not transaction guarantees. Legacy callers still receive
    /// IErrorsInfo; only a fully completed run maps to Errors.Ok.
    /// </summary>
    public sealed class ImportExecutionResult : ErrorsInfo
    {
        public string? RunId { get; set; }
        public ImportOutcome Outcome { get; internal set; } = ImportOutcome.Failed;
        public int RecordsAttempted { get; set; }
        public int WriteAttempts { get; set; }
        public int RecordsSucceeded { get; set; }
        public int RecordsFailed { get; set; }
        /// <summary>Pre-write transformation failures included in RecordsFailed; never provider write attempts.</summary>
        public int RecordsTransformationFailed { get; set; }
        public int RecordsQualityEvaluated { get; set; }
        public int RecordsQualityRejected { get; set; }
        public int RecordsQualityEvaluationFailed { get; set; }
        public int RecordsBlocked { get; set; }
        /// <summary>Rejected rows whose quarantine store acknowledged SaveAsync, not proof of a database commit.</summary>
        public int RecordsQuarantined { get; set; }
        public int RecordsWarned { get; set; }
        public int RejectStoreFailures { get; set; }
        public bool QualityAdmissionFailed { get; set; }
        /// <summary>Required transformation configuration failed before rows were admitted; not a fabricated row failure count.</summary>
        public bool TransformationAdmissionFailed { get; set; }
        public int RecordsSkipped { get; set; }
        public bool HasUncertainWrites { get; set; }
        public bool RequiresReconciliation => Outcome != ImportOutcome.Completed && (RecordsSucceeded > 0 || HasUncertainWrites);

        public void Complete(ImportOutcome outcome, string message)
        {
            Outcome = outcome;
            Flag = outcome == ImportOutcome.Completed ? ConfigUtil.Errors.Ok : ConfigUtil.Errors.Failed;
            Message = message;
        }
    }
}
