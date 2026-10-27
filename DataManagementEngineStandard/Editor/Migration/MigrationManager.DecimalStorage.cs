using System;
using System.Text.Json;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        public bool SupportsGuardedDecimalWidening => ResolveProvider() is IDecimalColumnMigrationProvider;

        /// <summary>Refuses unresolved native decimal work even when live columns already reached their target.</summary>
        public IErrorsInfo CheckDecimalWideningReadiness()
        {
            try
            {
                if (ResolveProvider() is not IDecimalColumnMigrationProvider provider ||
                    _editor?.ConfigEditor is not IMigrationExecutionStorageProvider stores)
                    return CreateErrorsInfo(Errors.Failed, "Guarded decimal widening requires native execution and captured migration storage.");
                var store = stores.CaptureMigrationExecutionStorage();
                var claim = store.ReadMigrationExecutionClaim(provider.DecimalMigrationTargetIdentity);
                return claim == null || claim.State == MigrationClaimState.Released && claim.Disposition != MigrationClaimDisposition.RequiresReconciliation
                    ? CreateErrorsInfo(Errors.Ok, "No unresolved decimal migration claim.")
                    : CreateErrorsInfo(Errors.Failed, "A migration owner or unresolved outcome requires explicit reconciliation before decimal boot.");
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("MigrationManager", "Decimal readiness failed: " + ex, DateTime.Now, 0, null, Errors.Failed);
                return CreateErrorsInfo(Errors.Failed, "Decimal migration recovery state is unavailable.", ex);
            }
        }

        /// <summary>Native guarded decimal widening with acknowledged intent/outcome and existing migration ownership.</summary>
        public IErrorsInfo WidenDecimalColumn(string entityName, EntityField expectedColumn, EntityField desiredColumn)
        {
            if (MigrateDataSource == null || string.IsNullOrWhiteSpace(entityName) || expectedColumn == null || desiredColumn == null)
                return CreateErrorsInfo(Errors.Failed, "Datasource, entity and exact decimal column observations are required.");
            if (!BeginOperation(out var scope)) return CreateErrorsInfo(Errors.Failed, "This migration manager owns another active operation.");
            var previous = _executionScope.Value;
            _executionScope.Value = scope;
            try
            {
                if (ResolveProvider() is not IDecimalColumnMigrationProvider provider)
                    return CreateErrorsInfo(Errors.Failed, "The migration provider does not support guarded decimal widening.");
                // Snapshot inputs before the first persistence callback.
                expectedColumn = EntityMetadataSnapshot.Capture(new EntityStructure { Fields = new System.Collections.Generic.List<EntityField> { expectedColumn } }).Fields[0];
                desiredColumn = EntityMetadataSnapshot.Capture(new EntityStructure { Fields = new System.Collections.Generic.List<EntityField> { desiredColumn } }).Fields[0];
                scope.TargetIdentity = provider.DecimalMigrationTargetIdentity;
                CaptureStorage(scope);
                var token = Guid.NewGuid().ToString("N");
                var intent = JsonSerializer.Serialize(new { Version = 1, scope.TargetIdentity, Entity = entityName, Expected = expectedColumn, Desired = desiredColumn });
                var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(intent)));
                var admission = Acquire(scope, token, hash, default);
                if (admission.Status != MigrationAdmissionStatus.Acquired)
                    return CreateErrorsInfo(Errors.Failed, "Decimal migration admission refused: " + admission.Status);
                scope.Storage.AppendMigrationRecordAcknowledged(scope.DataSourceName, scope.DataSourceType,
                    new MigrationRecord { MigrationId = token, Name = "DecimalWidening.Intent.v1", Notes = intent, Success = false }).ThrowIfNotSaved();
                scope.ProviderInvoked = true;
                var result = provider.WidenDecimalColumn(entityName, expectedColumn, desiredColumn, scope.TargetIdentity);
                scope.Storage.AppendMigrationRecordAcknowledged(scope.DataSourceName, scope.DataSourceType,
                    new MigrationRecord { MigrationId = token, Name = "DecimalWidening.Outcome.v1", Notes = intent, Success = result.Flag == Errors.Ok,
                        Steps = new System.Collections.Generic.List<MigrationStep> { new MigrationStep { Operation = "WidenDecimalColumn", EntityName = entityName,
                            ColumnName = expectedColumn.FieldName, Success = result.Flag == Errors.Ok, Message = result.Message } } }).ThrowIfNotSaved();
                scope.Lease.Finish(result.Flag == Errors.Ok ? MigrationClaimDisposition.Completed : MigrationClaimDisposition.RequiresReconciliation).ThrowIfNotSaved();
                scope.FinishDisposition = result.Flag == Errors.Ok ? MigrationClaimDisposition.Completed : MigrationClaimDisposition.RequiresReconciliation;
                EmitDdlEvidence("WidenDecimalColumn", entityName, expectedColumn.FieldName, null, null,
                    OutcomeFor(result), DdlHelperSource.Direct, result.Flag == Errors.Ok ? "DDL-CONFIRMED" : "DDL-EXEC-FAILED");
                return result;
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("MigrationManager", "Guarded decimal widening failed; reconcile live metadata and migration history before retrying: " + ex,
                    DateTime.Now, 0, null, Errors.Failed);
                return CreateErrorsInfo(Errors.Failed, "Guarded decimal widening or its history acknowledgement failed; reconcile before retrying.", ex);
            }
            finally
            {
                try
                {
                    if (scope.Lease != null && !scope.FinishDisposition.HasValue)
                    {
                        var disposition = scope.ProviderInvoked ? MigrationClaimDisposition.RequiresReconciliation : MigrationClaimDisposition.SafeToRetry;
                        var saved = scope.Lease.Finish(disposition);
                        if (saved?.IsSaved != true) _editor?.AddLogMessage("MigrationManager", "Decimal migration claim finish was not acknowledged; preserve it for reconciliation.", DateTime.Now, 0, null, Errors.Failed);
                    }
                }
                catch (Exception ex) { _editor?.AddLogMessage("MigrationManager", "Decimal claim finish failed: " + ex, DateTime.Now, 0, null, Errors.Failed); }
                finally
                {
                    try { scope.Lease?.Dispose(); }
                    catch (Exception ex) { _editor?.AddLogMessage("MigrationManager", "Decimal lease disposal failed: " + ex, DateTime.Now, 0, null, Errors.Failed); }
                    finally
                    {
                        _executionScope.Value = previous;
                        lock (_bindingGate) { _operationActive = false; }
                    }
                }
            }
        }
    }
}
