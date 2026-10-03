using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Editor.Importing;

namespace TheTechIdea.Beep.Editor
{
    public partial class BeepSyncManager
    {
        /// <summary>Replays one prepared reject with current direction-specific record policy; does not complete a sync run or advance cursors.</summary>
        public async Task<ImportRejectReplayResult> ReplayRejectedRecordAsync(DataSyncSchema schema, IImportErrorStore errorStore,
            string contextKey, string rejectId, int expectedPreparedRevision, string ownerId, CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (schema == null || errorStore is not IImportRejectRecoveryStore) throw new InvalidOperationException();
                var forward = SyncSchemaTranslator.ToImportConfiguration(schema, errorStore);
                DataImportConfiguration config;
                if (DataImportManager.GetRejectContextKey(forward) == contextKey) config = forward;
                else
                {
                    if (!string.Equals(schema.SyncDirection, "Bidirectional", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException();
                    config = SyncSchemaTranslator.ToReverseImportConfiguration(schema, errorStore);
                    if (DataImportManager.GetRejectContextKey(config) != contextKey) throw new InvalidOperationException();
                }
                var gate = SyncRecordQualityAdmission.Capture(schema, IntegrationContext?.RuleEngine, token);
                if (gate == null) throw new InvalidOperationException();
                config.RecordAdmission = gate.ForDirection(config.DestEntityName);
                config.QualityFailureMode = gate.FailureMode;
                config.QualityRuleTimeoutMs = gate.TimeoutMs;
                config.DestData = _editor.GetDataSource(config.DestDataSourceName);
                using var manager = new DataImportManager(_editor);
                return await manager.ReplayRejectedRecordAsync(config, contextKey, rejectId, expectedPreparedRevision, ownerId, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return new ImportRejectReplayResult { Flag = Errors.Failed, Cancelled = true, Message = "Sync reject recovery was cancelled before admission." };
            }
            catch (Exception)
            {
                return new ImportRejectReplayResult { Flag = Errors.Failed, RecordsUnsupported = 1, Message = "Sync reject recovery policy or direction is unavailable." };
            }
        }
    }
}
