namespace TheTechIdea.Beep.Editor.BeepSync
{
    public enum SyncRunFailureKind { ImportFailure, QualityThreshold, Cancelled, ExecutionFailure }

    /// <summary>Versioned acknowledgement evidence; not ordered replay offsets or transaction commits.</summary>
    public sealed class SyncFailureEvidence
    {
        public int FormatVersion { get; set; } = 1;
        public SyncRunFailureKind Kind { get; set; }
        public int RecordsAttempted { get; set; }
        public int RecordsAcknowledged { get; set; }
        public int RecordsFailed { get; set; }
        public int RecordsSkipped { get; set; }
        public int WriteAttempts { get; set; }
        public bool HasUncertainWrites { get; set; }
        public int RecordsTransformationFailed { get; set; }
        public int RecordsQualityEvaluated { get; set; }
        public int RecordsQualityRejected { get; set; }
        public int RecordsQualityEvaluationFailed { get; set; }
        public bool QualityAdmissionFailed { get; set; }
        public bool TransformationAdmissionFailed { get; set; }
        public int RecordsBlocked { get; set; }
        public int RecordsQuarantined { get; set; }
        public int RecordsWarned { get; set; }
        public int RejectStoreFailures { get; set; }
        public SyncBatchThresholdResult Threshold { get; set; }
    }
}
