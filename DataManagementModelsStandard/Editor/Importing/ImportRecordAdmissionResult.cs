using System;
using System.Threading;

namespace TheTechIdea.Beep.Editor.Importing
{
    public enum QualityFailureMode { Required, Advisory }
    public enum ImportRecordAdmissionOutcome { Allowed, Rejected, EvaluationFailed }

    /// <summary>Closed, payload-free record admission outcome. Rejection is never an uncertain provider write.</summary>
    public sealed class ImportRecordAdmissionResult
    {
        private ImportRecordAdmissionResult(ImportRecordAdmissionOutcome outcome, DataQualityAction action,
            bool warning, bool evaluationFailed)
        {
            Outcome = outcome;
            Action = action;
            HasWarning = warning;
            HasEvaluationFailure = evaluationFailed;
        }

        public ImportRecordAdmissionOutcome Outcome { get; }
        public DataQualityAction Action { get; }
        public bool HasWarning { get; }
        public bool HasEvaluationFailure { get; }
        public bool ShouldWrite => Outcome == ImportRecordAdmissionOutcome.Allowed;
        public static ImportRecordAdmissionResult Pass() => new(ImportRecordAdmissionOutcome.Allowed, DataQualityAction.Warn, false, false);
        public static ImportRecordAdmissionResult Warn(bool evaluationFailed = false) => new(ImportRecordAdmissionOutcome.Allowed, DataQualityAction.Warn, true, evaluationFailed);
        public static ImportRecordAdmissionResult Fail() => new(ImportRecordAdmissionOutcome.EvaluationFailed, DataQualityAction.Block, false, true);
        public static ImportRecordAdmissionResult Reject(DataQualityAction action)
        {
            if (action == DataQualityAction.Warn) return Warn();
            if (action != DataQualityAction.Block && action != DataQualityAction.Quarantine)
                throw new ArgumentOutOfRangeException(nameof(action));
            return new(ImportRecordAdmissionOutcome.Rejected, action, false, false);
        }
    }

    /// <summary>Optional dedicated post-transform/pre-write gate, not a transformation callback.</summary>
    public interface IImportRecordAdmission
    {
        bool RequiresRejectStore { get; }
        void Validate(CancellationToken token);
        ImportRecordAdmissionResult Evaluate(object record, CancellationToken token);
    }

    public sealed class ImportQualityAdmissionException : Exception
    {
        public ImportQualityAdmissionException() : base("Required record-quality admission configuration failed.") { }
    }
}
