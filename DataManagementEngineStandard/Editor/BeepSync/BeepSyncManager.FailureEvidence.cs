using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.Importing;

namespace TheTechIdea.Beep.Editor
{
    public partial class BeepSyncManager
    {
        private static RetryPolicy CaptureRetryPolicy(RetryPolicy source) => source == null ? null : new RetryPolicy
        {
            MaxAttempts = source.MaxAttempts, BaseDelayMs = source.BaseDelayMs, BackoffMode = source.BackoffMode,
            CheckpointEnabled = source.CheckpointEnabled, ErrorCategoryRuleKey = source.ErrorCategoryRuleKey,
            MaxResumeWindowHours = source.MaxResumeWindowHours,
            NonRetryableCategories = source.NonRetryableCategories == null ? null : new System.Collections.Generic.List<string>(source.NonRetryableCategories)
        };

        private static SyncCheckpoint CheckpointFromIntent(SyncCheckpoint intent, string status, int attempt, int count) => new SyncCheckpoint
        {
            SchemaId = intent.SchemaId, RunId = intent.RunId, SchemaFingerprint = intent.SchemaFingerprint,
            MappingVersion = intent.MappingVersion, CompiledMappingPlanId = intent.CompiledMappingPlanId,
            Status = status, AttemptCount = attempt, ProcessedOffset = count
        };

        private async Task<IErrorsInfo> PublishFailureAsync(DataSyncSchema schema, RetryPolicy policy, SyncCheckpoint intent,
            int attempt, ImportExecutionResult records, IErrorsInfo original, SyncRunFailureKind kind)
        {
            if (policy?.CheckpointEnabled != true || intent == null) return original;
            var evidence = new SyncFailureEvidence
            {
                Kind = kind, RecordsAttempted = records?.RecordsAttempted ?? 0,
                RecordsAcknowledged = records?.RecordsSucceeded ?? 0, RecordsFailed = records?.RecordsFailed ?? 0,
                RecordsSkipped = records?.RecordsSkipped ?? 0, WriteAttempts = records?.WriteAttempts ?? 0,
                HasUncertainWrites = records?.HasUncertainWrites ?? false,
                RecordsTransformationFailed = records?.RecordsTransformationFailed ?? 0,
                RecordsQualityEvaluated = records?.RecordsQualityEvaluated ?? 0,
                RecordsQualityRejected = records?.RecordsQualityRejected ?? 0,
                RecordsQualityEvaluationFailed = records?.RecordsQualityEvaluationFailed ?? 0,
                QualityAdmissionFailed = records?.QualityAdmissionFailed ?? false,
                TransformationAdmissionFailed = records?.TransformationAdmissionFailed ?? false, RecordsBlocked = records?.RecordsBlocked ?? 0,
                RecordsQuarantined = records?.RecordsQuarantined ?? 0, RecordsWarned = records?.RecordsWarned ?? 0,
                RejectStoreFailures = records?.RejectStoreFailures ?? 0, Threshold = LastRunBatchThresholdResult
            };
            var checkpoint = CheckpointFromIntent(intent, "Failed", Math.Max(1, attempt), evidence.RecordsAcknowledged);
            checkpoint.TotalExpected = evidence.RecordsAttempted;
            checkpoint.RequiresReconciliation = true;
            checkpoint.FailureEvidence = evidence;
            PersistenceWriteResult saved;
            // Caller cancellation must not discard acknowledged work. Storage still has a bounded cleanup token.
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                saved = _persistenceHelper is ISyncPersistenceAcknowledgement acknowledgement
                    ? await acknowledgement.SaveCheckpointAcknowledgedAsync(checkpoint, cleanup.Token).ConfigureAwait(false)
                    : new PersistenceWriteResult(PersistenceWriteStatus.Unsupported);
                saved ??= new PersistenceWriteResult(PersistenceWriteStatus.Failed);
            }
            catch (OperationCanceledException) { saved = new PersistenceWriteResult(PersistenceWriteStatus.Cancelled); }
            catch (NotSupportedException) { saved = new PersistenceWriteResult(PersistenceWriteStatus.Unsupported); }
            catch (Exception) { saved = new PersistenceWriteResult(PersistenceWriteStatus.Failed); }
            LastRunFailureCheckpointStatus = saved.Status;
            if (!saved.IsSaved)
                return new SyncCheckpointFailureResult(intent.RunId, evidence.RecordsAcknowledged,
                    evidence.HasUncertainWrites, saved.Status, true, records);
            LastRunCheckpoint = checkpoint;
            RunDiagnostic("CheckpointNotification", () => schema.ActiveCheckpoint = checkpoint);
            return original;
        }
    }
}
