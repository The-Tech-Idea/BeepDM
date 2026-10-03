using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Core;
using TheTechIdea.Beep.Editor.SchemaMigration;

namespace TheTechIdea.Beep.Editor.Migration
{
    public partial class MigrationManager
    {
        private readonly object _bindingGate = new object();
        private readonly AsyncLocal<ExecutionScope> _executionScope = new AsyncLocal<ExecutionScope>();
        private bool _operationActive;
        private IDataSource _migrationSource;
        private string _executionTargetIdentity;

        /// <summary>Host-owned canonical physical target identity, shared across aliases and credentials.</summary>
        public string ExecutionTargetIdentity
        {
            get { lock (_bindingGate) return _executionTargetIdentity; }
            set { lock (_bindingGate) { RejectBindingChange(); _executionTargetIdentity = value; } }
        }

        private void RejectBindingChange()
        {
            if (_operationActive) throw new InvalidOperationException("Migration bindings cannot change during an active operation.");
        }

        private sealed class ExecutionScope
        {
            public IDataSource Source;
            public string TargetIdentity, TargetKey, DataSourceName;
            public Utilities.DataSourceType DataSourceType;
            public IMigrationExecutionStorage Storage;
            public IMigrationExecutionLease Lease;
            public MigrationExecutionCheckpoint Checkpoint;
            public ISchemaMigrationProvider Provider;
            public bool ProviderCaptured, ProviderInvoked, ClaimValidated, AdmissionAttempted;
            public int AcknowledgedCount;
            public string TargetFingerprint;
            public MigrationClaimDisposition? FinishDisposition;
        }

        private bool BeginOperation(out ExecutionScope scope)
        {
            lock (_bindingGate)
            {
                scope = null;
                if (_operationActive) return false;
                _operationActive = true;
                scope = new ExecutionScope { Source = _migrationSource, TargetIdentity = _executionTargetIdentity };
                return true;
            }
        }

        private void CaptureStorage(ExecutionScope scope)
        {
            if (_editor?.ConfigEditor is not IMigrationExecutionStorageProvider provider)
                throw new NotSupportedException("Governed migration requires captured history and ownership storage.");
            scope.Storage = provider.CaptureMigrationExecutionStorage()
                ?? throw new InvalidOperationException("Migration storage capture returned no store.");
            ValidateOwnershipIdentity(scope.Storage.ScopeIdentity, 256);
            ValidateOwnershipIdentity(scope.TargetIdentity, 1024);
            scope.TargetKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scope.TargetIdentity)));
            scope.DataSourceName = scope.Source?.DatasourceName;
            scope.DataSourceType = scope.Source?.DatasourceType ?? Utilities.DataSourceType.Unknown;
        }

        private static void ValidateOwnershipIdentity(string value, int limit)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > limit || value != value.Trim() ||
                value.Any(char.IsControl) || value.Any(char.IsSurrogate))
                throw new NotSupportedException("A bounded canonical migration target and store identity are required.");
        }

        private static MigrationExecutionAdmission Acquire(ExecutionScope scope, string token, string hash, CancellationToken cancellation)
        {
            scope.AdmissionAttempted = true;
            var result = scope.Storage.TryAcquireMigrationExecution(scope.TargetIdentity, token, hash, cancellation);
            if (result == null || !Enum.IsDefined(result.Status) ||
                result.Status == MigrationAdmissionStatus.Acquired && result.Lease == null ||
                result.Status != MigrationAdmissionStatus.Acquired && result.Lease != null)
            {
                result?.Lease?.Dispose();
                throw new InvalidOperationException("Migration ownership returned an invalid admission.");
            }
            if (result.Status == MigrationAdmissionStatus.Acquired)
            {
                scope.Lease = result.Lease;
                var claim = scope.Lease.Claim;
                if (claim == null || claim.TargetKey != scope.TargetKey || claim.ExecutionToken != token || claim.PlanHash != hash ||
                    claim.State != MigrationClaimState.Owned || claim.Revision <= 0 || !Guid.TryParseExact(claim.ClaimId, "N", out _))
                    throw new InvalidOperationException("Migration ownership claim does not match captured admission.");
                scope.ClaimValidated = true;
            }
            return result;
        }

        public async Task<MigrationExecutionResult> ExecuteMigrationPlanAsync(MigrationPlanArtifact plan,
            MigrationExecutionPolicy policy = null, string executionToken = null, IProgress<PassedArgs> progress = null,
            CancellationToken token = default, MigrationPolicyOptions policyOptions = null)
            => await ExecuteMigrationPlanWithOwnershipAsync(plan, policy, executionToken, progress, token, policyOptions, false).ConfigureAwait(false);

        private async Task<MigrationExecutionResult> ExecuteMigrationPlanWithOwnershipAsync(MigrationPlanArtifact plan,
            MigrationExecutionPolicy policy, string executionToken, IProgress<PassedArgs> progress,
            CancellationToken token, MigrationPolicyOptions policyOptions, bool resume)
        {
            var result = new MigrationExecutionResult { ResumedFromCheckpoint = resume };
            if (!BeginOperation(out var scope))
                return new MigrationExecutionResult { OwnershipAdmissionStatus = MigrationAdmissionStatus.Busy,
                    Message = "This migration manager already owns an active operation." };
            var previous = _executionScope.Value;
            _executionScope.Value = scope;
            try
            {
                token.ThrowIfCancellationRequested();
                if (scope.Source == null) throw new InvalidOperationException("A migration datasource is required.");
                policy = CopySnapshot(policy);
                policyOptions = CopySnapshot(policyOptions);
                CaptureStorage(scope);
                executionToken = string.IsNullOrWhiteSpace(executionToken) ? Guid.NewGuid().ToString("N") : executionToken.Trim();
                result.ExecutionToken = executionToken;
                if (resume)
                {
                    var stored = TryLoadPersistedCheckpoint(executionToken);
                    if (stored == null) { result.Message = "No persisted execution checkpoint was found."; return result; }
                    plan = stored.ApprovedPlan;
                    if (plan == null || !ValidateCheckpointIntent(stored, plan))
                    { result.Message = "Legacy, changed or incompatible checkpoint intent. Rebuild and re-approve before resume."; return result; }
                }
                if (plan == null) throw new InvalidOperationException("A migration plan is required.");
                plan = CopyExecutionPlan(plan);
                if (!ValidatePlanIntent(plan, out var intentError)) { result.Message = intentError; return result; }
                if (policy != null && !JTokenEquals(policy, plan.ExecutionPolicy))
                { result.Message = "Execution policy differs from captured migration intent."; return result; }
                if (policyOptions != null && !GovernanceMatches(policyOptions, plan.GovernancePolicy))
                { result.Message = "Governance policy differs from captured migration intent."; return result; }
                if (!string.IsNullOrWhiteSpace(policyOptions?.Approver) && policyOptions.ApprovedPlanHash != plan.PlanHash)
                { result.Message = "Approval differs from captured migration intent."; return result; }
                if (ExecutionPlans.TryGetValue(executionToken, out var reserved) && reserved.PlanHash != plan.PlanHash)
                { result.Message = "Execution token belongs to a different migration plan hash."; return result; }
                scope.TargetFingerprint = plan.TargetFingerprint;
                var admission = Acquire(scope, executionToken, plan.PlanHash, token);
                result.OwnershipAdmissionStatus = admission.Status;
                if (admission.Status != MigrationAdmissionStatus.Acquired)
                {
                    result.RequiresReconciliation = admission.Status == MigrationAdmissionStatus.RequiresReconciliation;
                    result.RequiresOperatorIntervention = result.RequiresReconciliation;
                    result.Message = "Migration target admission was denied (" + admission.Status + ").";
                    return result;
                }
                scope.Provider = _editor.GetMigrationProvider(scope.Source);
                scope.ProviderCaptured = true;
                result = await ExecuteMigrationPlanCoreAsync(plan, policy, executionToken, progress, token, policyOptions).ConfigureAwait(false);
                result.ResumedFromCheckpoint = resume;
            }
            catch (MigrationCheckpointPersistenceException ex)
            {
                result.Checkpoint = CopySnapshot(ex.Checkpoint);
                result.CheckpointPersistenceStatus = ex.Outcome.Status;
                result.RequiresReconciliation = result.RequiresOperatorIntervention = true;
                result.Message = "Stored checkpoint is unreadable or incompatible; preserve it and reconcile before replay.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Migration execution stopped (" + ex.GetType().Name + ").";
                result.ExecutionToken = executionToken ?? string.Empty;
                result.Checkpoint = scope.Checkpoint ?? result.Checkpoint;
                result.AppliedCount = scope.AcknowledgedCount;
                result.RequiresReconciliation |= scope.ProviderInvoked || scope.AdmissionAttempted && scope.Lease == null;
                result.RequiresOperatorIntervention |= result.RequiresReconciliation;
                if (ex is NotSupportedException) result.CheckpointPersistenceStatus = PersistenceWriteStatus.Unsupported;
                if (scope.Lease == null) result.OwnershipAdmissionStatus = ex is NotSupportedException
                    ? MigrationAdmissionStatus.Unsupported : MigrationAdmissionStatus.Failed;
                if (ex is OperationCanceledException && scope.Lease == null) result.OwnershipAdmissionStatus = MigrationAdmissionStatus.Cancelled;
            }
            finally
            {
                if (scope.Lease != null) CompleteExecutionOwnership(scope, result);
                _executionScope.Value = previous;
                lock (_bindingGate) _operationActive = false;
            }
            return result;
        }

        private void CompleteExecutionOwnership(ExecutionScope scope, MigrationExecutionResult result)
        {
            try
            {
                result.OwnershipAdmissionStatus = MigrationAdmissionStatus.Acquired;
                if (!scope.ClaimValidated)
                {
                    result.Success = false;
                    result.RequiresReconciliation = result.RequiresOperatorIntervention = true;
                    result.OwnershipPersistenceStatus = PersistenceWriteStatus.Failed;
                    return; // An invalid receipt is not authority to finish somebody else's claim.
                }
                result.OwnershipClaimId = scope.Lease.Claim.ClaimId;
                result.AppliedCount = Math.Max(result.AppliedCount, scope.AcknowledgedCount);
                result.RequiresReconciliation |= !result.Success && scope.ProviderInvoked;
                if (scope.Checkpoint != null && !result.Success)
                {
                    scope.Checkpoint.RequiresReconciliation |= result.RequiresReconciliation;
                    if (!scope.ProviderInvoked)
                        foreach (var step in scope.Checkpoint.Steps.Where(s => s.Status == MigrationExecutionStepStatus.Running))
                            step.Status = MigrationExecutionStepStatus.Pending;
                    if (result.RequiresReconciliation || result.CheckpointPersisted)
                    {
                        try { PersistExecutionCheckpoint(scope.Checkpoint); }
                        catch (MigrationCheckpointPersistenceException ex)
                        { result.CheckpointPersisted = false; result.CheckpointPersistenceStatus = ex.Outcome.Status; }
                    }
                }
                result.Checkpoint = CopySnapshot(result.Checkpoint);
                var disposition = result.RequiresReconciliation ? MigrationClaimDisposition.RequiresReconciliation
                    : scope.FinishDisposition ?? (result.Success ? MigrationClaimDisposition.Completed : MigrationClaimDisposition.SafeToRetry);
                var saved = scope.Lease.Finish(disposition) ?? new PersistenceWriteResult(PersistenceWriteStatus.Failed);
                if (!Enum.IsDefined(saved.Status)) saved = new PersistenceWriteResult(PersistenceWriteStatus.Failed);
                result.OwnershipPersistenceStatus = saved.Status;
                result.OwnershipFinished = saved.IsSaved;
                if (!saved.IsSaved)
                {
                    result.Success = false;
                    result.RequiresReconciliation = true;
                    result.Message += " Migration ownership completion was not acknowledged; reconcile before replay.";
                }
                result.RequiresOperatorIntervention |= result.RequiresReconciliation;
            }
            catch
            {
                result.Success = false;
                result.RequiresReconciliation = result.RequiresOperatorIntervention = true;
                result.OwnershipPersistenceStatus = PersistenceWriteStatus.Failed;
                result.Message += " Migration ownership completion failed; reconcile before replay.";
            }
            finally { try { scope.Lease.Dispose(); } catch { result.Success = false; result.RequiresReconciliation = result.RequiresOperatorIntervention = true; } }
        }

        private bool CheckpointScopeMatches(MigrationExecutionCheckpoint checkpoint) =>
            checkpoint != null && checkpoint.OwnershipStoreIdentity == _executionScope.Value?.Storage?.ScopeIdentity &&
            checkpoint.OwnershipTargetKey == _executionScope.Value?.TargetKey && !string.IsNullOrWhiteSpace(checkpoint.OwnershipTargetKey);
    }
}
