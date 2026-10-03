using System;
using TheTechIdea.Beep.Editor.Importing;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    public enum SyncBatchThresholdOutcome { Passed, Rejected, EvaluationFailed }

    /// <summary>Captured, payload-free attempt threshold evidence, not provider rollback or replay authority.</summary>
    public sealed class SyncBatchThresholdResult
    {
        public SyncBatchThresholdOutcome Outcome { get; }
        public QualityFailureMode FailureMode { get; }
        public int RecordsAttempted { get; }
        public int RecordsRejected { get; }
        public double MaxRejectRate { get; }
        public double RejectRate => RecordsAttempted == 0 ? 0 : (double)RecordsRejected / RecordsAttempted;
        public bool BlocksCompletion => FailureMode == QualityFailureMode.Required && Outcome != SyncBatchThresholdOutcome.Passed;
        public bool HasWarning => FailureMode == QualityFailureMode.Advisory && Outcome != SyncBatchThresholdOutcome.Passed;

        public SyncBatchThresholdResult(SyncBatchThresholdOutcome outcome, QualityFailureMode failureMode,
            int recordsAttempted, int recordsRejected, double maxRejectRate)
        {
            if (!Enum.IsDefined(typeof(SyncBatchThresholdOutcome), outcome) ||
                !Enum.IsDefined(typeof(QualityFailureMode), failureMode) || recordsAttempted < 0 ||
                recordsRejected < 0 || recordsRejected > recordsAttempted || double.IsNaN(maxRejectRate) ||
                double.IsInfinity(maxRejectRate) || maxRejectRate < 0 || maxRejectRate > 1)
                throw new ArgumentException("Invalid threshold evidence.");
            Outcome = outcome; FailureMode = failureMode; RecordsAttempted = recordsAttempted;
            RecordsRejected = recordsRejected; MaxRejectRate = maxRejectRate;
        }
    }
}
