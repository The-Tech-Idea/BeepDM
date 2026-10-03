using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.Importing;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>Provider acknowledgements retained when mandatory run completion cannot be persisted.</summary>
    public sealed class SyncCheckpointFailureResult : ErrorsInfo
    {
        public string RunId { get; }
        public int RecordsAcknowledged { get; }
        public bool HasUncertainWrites { get; }
        public bool RequiresReconciliation => true;
        public PersistenceWriteStatus CheckpointPersistenceStatus { get; }
        public string CheckpointStage { get; }
        public ImportExecutionResult ImportResult { get; }

        public SyncCheckpointFailureResult(string runId, int recordsAcknowledged, bool hasUncertainWrites,
            PersistenceWriteStatus status) : this(runId, recordsAcknowledged, hasUncertainWrites, status, false, null)
        { }

        public SyncCheckpointFailureResult(string runId, int recordsAcknowledged, bool hasUncertainWrites,
            PersistenceWriteStatus status, bool failedRunPublication, ImportExecutionResult importResult)
        {
            RunId = runId;
            RecordsAcknowledged = recordsAcknowledged;
            HasUncertainWrites = hasUncertainWrites;
            CheckpointPersistenceStatus = status;
            CheckpointStage = failedRunPublication ? "Failure" : "Completion";
            ImportResult = importResult;
            Flag = ConfigUtil.Errors.Failed;
            Message = $"Mandatory sync {CheckpointStage.ToLowerInvariant()} checkpoint was not acknowledged ({status}); " +
                $"{recordsAcknowledged} provider writes were acknowledged. Preserve evidence and reconcile before replay.";
        }
    }
}
