using System;
using System.Threading;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Editor.Importing;

namespace TheTechIdea.Beep.Editor
{
    public partial class BeepSyncManager
    {
        private IErrorsInfo ApplyBatchQuality(SyncBatchQualityAdmission admission, ImportExecutionResult records,
            IErrorsInfo original, CancellationToken token, ref bool blocked)
        {
            if (admission == null) return original;
            LastRunBatchThresholdResult = admission.Evaluate(records, token);
            blocked = LastRunBatchThresholdResult.BlocksCompletion;
            if (!blocked) return original;
            records ??= new ImportExecutionResult();
            records.Complete(records.RecordsSucceeded > 0 ? ImportOutcome.Partial : ImportOutcome.Failed,
                "Required sync threshold rejected completion; preserve acknowledgement evidence and reconcile before replay.");
            return records;
        }

        private static ImportExecutionResult CombineRecordOutcomes(ImportExecutionResult first, ImportExecutionResult second)
        {
            first ??= new ImportExecutionResult();
            var result = new ImportExecutionResult
            {
                RunId = second.RunId,
                RecordsAttempted = checked(first.RecordsAttempted + second.RecordsAttempted),
                WriteAttempts = checked(first.WriteAttempts + second.WriteAttempts),
                RecordsSucceeded = checked(first.RecordsSucceeded + second.RecordsSucceeded),
                RecordsFailed = checked(first.RecordsFailed + second.RecordsFailed),
                RecordsTransformationFailed = checked(first.RecordsTransformationFailed + second.RecordsTransformationFailed),
                RecordsQualityEvaluated = checked(first.RecordsQualityEvaluated + second.RecordsQualityEvaluated),
                RecordsQualityRejected = checked(first.RecordsQualityRejected + second.RecordsQualityRejected),
                RecordsQualityEvaluationFailed = checked(first.RecordsQualityEvaluationFailed + second.RecordsQualityEvaluationFailed),
                RecordsBlocked = checked(first.RecordsBlocked + second.RecordsBlocked),
                RecordsQuarantined = checked(first.RecordsQuarantined + second.RecordsQuarantined),
                RecordsWarned = checked(first.RecordsWarned + second.RecordsWarned),
                RejectStoreFailures = checked(first.RejectStoreFailures + second.RejectStoreFailures),
                RecordsSkipped = checked(first.RecordsSkipped + second.RecordsSkipped),
                HasUncertainWrites = first.HasUncertainWrites || second.HasUncertainWrites,
                QualityAdmissionFailed = first.QualityAdmissionFailed || second.QualityAdmissionFailed,
                TransformationAdmissionFailed = first.TransformationAdmissionFailed || second.TransformationAdmissionFailed
            };
            result.Errors.AddRange(first.Errors);
            result.Errors.AddRange(second.Errors);
            var outcome = second.Outcome == ImportOutcome.Cancelled ? ImportOutcome.Cancelled :
                result.RecordsFailed == 0 && !result.QualityAdmissionFailed && !result.TransformationAdmissionFailed && !result.HasUncertainWrites ? second.Outcome :
                result.RecordsSucceeded > 0 ? ImportOutcome.Partial : ImportOutcome.Failed;
            result.Complete(outcome, $"Sync import {outcome}: {result.RecordsSucceeded} acknowledged writes, {result.RecordsFailed} failed records.");
            return result;
        }
    }
}
