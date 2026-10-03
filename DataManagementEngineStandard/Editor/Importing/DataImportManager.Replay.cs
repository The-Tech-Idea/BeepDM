using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Importing.ErrorStore;
using TheTechIdea.Beep.Editor.Importing.Quality;
using TheTechIdea.Beep.Helpers;

namespace TheTechIdea.Beep.Editor.Importing
{
    public partial class DataImportManager
    {
        public static string GetRejectContextKey(DataImportConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(config);
            return JsonFileImportErrorStore.Context(config);
        }

        /// <summary>Replays only explicitly prepared durable rejects. Legacy records are never guessed or marked.</summary>
        public async Task<IErrorsInfo> ReplayFailedRecordsAsync(string contextKey,
            IProgress<IPassedArgs> progress = null, CancellationToken token = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(contextKey);
            var result = new ImportRejectReplayResult();
            var config = _config;
            if (config?.ErrorStore is not IImportRejectRecoveryStore ||
                !string.Equals(GetRejectContextKey(config), contextKey, StringComparison.Ordinal))
            {
                result.RecordsUnsupported++;
                return FinishReplay(result);
            }
            try
            {
                var records = await config.ErrorStore.LoadAsync(contextKey, token).ConfigureAwait(false);
                if (records.Any(record => record.Recovery?.State == ImportRejectState.Claimed ||
                    record.Recovery?.State == ImportRejectState.ReconciliationRequired))
                {
                    result.RequiresReconciliation = true;
                    return FinishReplay(result);
                }
                var pending = records.Where(record => record.Recovery == null ? !record.Replayed :
                    record.Recovery.State == ImportRejectState.Pending || record.Recovery.State == ImportRejectState.Prepared).ToArray();
                foreach (var record in pending)
                {
                    token.ThrowIfCancellationRequested();
                    if (record.Recovery == null) result.RecordsUnsupported++;
                    else if (record.Recovery.State != ImportRejectState.Prepared) result.RecordsDenied++;
                    else
                    {
                        var replay = await ReplayRejectedRecordAsync(config, contextKey, record.Recovery.RejectId,
                            record.Recovery.Revision, "replay-" + Guid.NewGuid().ToString("N"), token).ConfigureAwait(false);
                        result.RecordsAttempted += replay.RecordsAttempted;
                        result.WriteAttempts += replay.WriteAttempts;
                        result.RecordsAcknowledged += replay.RecordsAcknowledged;
                        result.RecordsDenied += replay.RecordsDenied;
                        result.RecordsFailed += replay.RecordsFailed;
                        result.RecordsUnsupported += replay.RecordsUnsupported;
                        result.RecoveryPersistenceFailures += replay.RecoveryPersistenceFailures;
                        result.HasUncertainWrites |= replay.HasUncertainWrites;
                        result.RequiresReconciliation |= replay.RequiresReconciliation;
                        result.Cancelled |= replay.Cancelled;
                    }
                    try
                    {
                        progress?.Report(new PassedArgs { Messege = "Reject recovery progress.",
                            ParameterInt1 = result.RecordsAcknowledged, ParameterInt2 = pending.Length });
                    }
                    catch (Exception)
                    {
                        // Advisory observers cannot turn persisted acknowledgement into a failed write.
                        result.DiagnosticFailures++;
                    }
                    if (result.RequiresReconciliation || result.Cancelled) break;
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { result.Cancelled = true; }
            catch (Exception) { result.RecoveryPersistenceFailures++; }
            return FinishReplay(result);
        }

        /// <summary>Claims one prepared destination snapshot, evaluates current quality, then attempts one acknowledged write.</summary>
        public async Task<ImportRejectReplayResult> ReplayRejectedRecordAsync(DataImportConfiguration config,
            string contextKey, string rejectId, int expectedPreparedRevision, string ownerId, CancellationToken token = default)
        {
            var result = new ImportRejectReplayResult();
            ImportErrorRecord claim = null;
            bool claimAttempted = false, validClaim = false, writeStarted = false;
            var disposition = ImportReplayDisposition.Denied;
            var store = config?.ErrorStore as IImportRejectRecoveryStore;
            try
            {
                token.ThrowIfCancellationRequested();
                if (store == null || !string.Equals(GetRejectContextKey(config), contextKey, StringComparison.Ordinal))
                    throw new InvalidOperationException();
                var sourceName = config.SourceDataSourceName;
                var sourceEntity = config.SourceEntityName;
                var destinationName = config.DestDataSourceName;
                var destinationEntity = config.DestEntityName;
                var destination = config.DestData ?? _editor.GetDataSource(destinationName);
                var admission = ImportQualityAdmission.Capture(config, token, persistRejects: false);
                if (!admission.IsConfigured) throw new InvalidOperationException();
                await store.ValidateRecoveryAsync(contextKey, token).ConfigureAwait(false);
                var rejected = await store.LoadRejectAsync(contextKey, rejectId, token).ConfigureAwait(false);
                var identity = rejected?.Recovery;
                if (identity == null || identity.State != ImportRejectState.Prepared || identity.Revision != expectedPreparedRevision ||
                    identity.RejectId != rejectId || identity.SourceDataSourceName != sourceName || identity.SourceEntityName != sourceEntity ||
                    identity.DestinationDataSourceName != destinationName || identity.DestinationEntityName != destinationEntity ||
                    destination?.ConnectionStatus != ConnectionState.Open || destination.DatasourceName != destinationName ||
                    !string.IsNullOrWhiteSpace(identity.DestinationGuid) && identity.DestinationGuid != destination.GuidID ||
                    !destination.CheckEntityExist(destinationEntity)) throw new InvalidOperationException();

                var payload = identity.PreparedDestinationPayload;
                var values = ImportRejectPayload.Decode(payload);
                var metadata = EntityMetadataSnapshot.Capture(destination.GetEntityStructure(destinationEntity, false));
                var fields = new Dictionary<string, EntityField>(StringComparer.OrdinalIgnoreCase);
                foreach (var field in metadata.Fields)
                    if (field == null || string.IsNullOrWhiteSpace(field.FieldName) || !fields.TryAdd(field.FieldName, field))
                        throw new InvalidOperationException();
                if (fields.Count == 0 || values.Keys.Any(name => !fields.ContainsKey(name)) ||
                    fields.Values.Any(field => field.IsRequired && !field.IsAutoIncrement &&
                        (!values.TryGetValue(field.FieldName, out var value) || value == null)))
                    throw new InvalidOperationException();
                var target = DMTypeBuilder.CreateNewObject(_editor, destinationEntity, destinationEntity, metadata.Fields);
                if (target == null) throw new InvalidOperationException();
                foreach (var value in values) RecordFieldAccess.Write(target, fields[value.Key].FieldName, value.Value);
                token.ThrowIfCancellationRequested();

                // A durable CAS claim precedes evaluation/provider work. An interrupted claim is never automatically released.
                claimAttempted = true;
                claim = await store.ClaimReplayAsync(contextKey, rejectId, expectedPreparedRevision, ownerId, token).ConfigureAwait(false);
                if (claim?.Recovery?.State != ImportRejectState.Claimed || claim.Recovery.RejectId != rejectId ||
                    claim.Recovery.Revision != checked(expectedPreparedRevision + 1) || claim.Recovery.ClaimOwner != ownerId ||
                    string.IsNullOrWhiteSpace(claim.Recovery.ClaimId) || claim.Recovery.PreparedDestinationPayload != payload)
                    throw new InvalidOperationException();
                validClaim = true;
                result.RecordsAttempted = 1;
                if (!await admission.AdmitAsync(target, new ImportExecutionResult(), token).ConfigureAwait(false))
                    result.RecordsDenied = 1;
                else
                {
                    token.ThrowIfCancellationRequested();
                    var acknowledgement = await Task.Run(() =>
                    {
                        writeStarted = true;
                        result.WriteAttempts = 1;
                        return destination is IImportReplayDataSource keyed
                            ? keyed.InsertReplay(destinationEntity, target, rejectId)
                            : destination.InsertEntity(destinationEntity, target);
                    }, token).ConfigureAwait(false);
                    if (acknowledgement?.Flag == Errors.Ok)
                    {
                        result.RecordsAcknowledged = 1;
                        disposition = ImportReplayDisposition.Acknowledged;
                    }
                    else if (acknowledgement?.Flag == Errors.Failed)
                    {
                        result.RecordsFailed = 1;
                        disposition = ImportReplayDisposition.DefinitivelyFailed;
                    }
                    else
                    {
                        result.RecordsFailed = 1;
                        result.HasUncertainWrites = result.RequiresReconciliation = true;
                        disposition = ImportReplayDisposition.Uncertain;
                    }
                    result.Cancelled = token.IsCancellationRequested;
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                result.Cancelled = true;
                if (writeStarted)
                {
                    result.HasUncertainWrites = result.RequiresReconciliation = true;
                    disposition = ImportReplayDisposition.Uncertain;
                }
                else if (claimAttempted && !validClaim) result.RequiresReconciliation = true;
            }
            catch (Exception)
            {
                if (writeStarted)
                {
                    result.RecordsFailed = 1;
                    result.HasUncertainWrites = result.RequiresReconciliation = true;
                    disposition = ImportReplayDisposition.Uncertain;
                }
                else if (claimAttempted)
                {
                    result.RecoveryPersistenceFailures++;
                    result.RequiresReconciliation = true;
                    // Do not release an invalid claim snapshot or an ambiguous store transition.
                    validClaim = false;
                }
                else result.RecordsUnsupported++;
            }
            finally
            {
                if (validClaim)
                {
                    using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    try
                    {
                        await store.CompleteReplayAsync(contextKey, rejectId, claim.Recovery.Revision,
                            claim.Recovery.ClaimId, disposition, cleanup.Token).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        result.RecoveryPersistenceFailures++;
                        result.RequiresReconciliation = true;
                    }
                }
            }
            return FinishReplay(result);
        }

        private static ImportRejectReplayResult FinishReplay(ImportRejectReplayResult result)
        {
            result.Flag = result.Cancelled || result.RecordsDenied > 0 || result.RecordsFailed > 0 || result.RecordsUnsupported > 0 ||
                result.RecoveryPersistenceFailures > 0 || result.RequiresReconciliation ? Errors.Failed : Errors.Ok;
            result.Message = $"Reject recovery: acknowledged={result.RecordsAcknowledged}, denied={result.RecordsDenied}, " +
                $"failed={result.RecordsFailed}, unsupported={result.RecordsUnsupported}, reconciliation={result.RequiresReconciliation}.";
            return result;
        }
    }
}
