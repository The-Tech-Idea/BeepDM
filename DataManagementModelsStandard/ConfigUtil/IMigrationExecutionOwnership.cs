using System;
using System.Threading;

namespace TheTechIdea.Beep.ConfigUtil
{
    public enum MigrationAdmissionStatus { Acquired, Busy, RequiresReconciliation, Failed, Cancelled, Unsupported }
    public enum MigrationClaimState { Owned, Released, RequiresReconciliation }
    public enum MigrationClaimDisposition { Completed, SafeToRetry, RequiresReconciliation, OperatorReconciled }

    /// <summary>An immutable observation, not authority to change execution progress.</summary>
    public sealed class MigrationExecutionClaim
    {
        public string TargetKey { get; }
        public string ClaimId { get; }
        public string ExecutionToken { get; }
        public string PlanHash { get; }
        public long Revision { get; }
        public MigrationClaimState State { get; }
        public MigrationClaimDisposition? Disposition { get; }
        public DateTimeOffset UpdatedOnUtc { get; }

        public MigrationExecutionClaim(string targetKey, string claimId, string executionToken, string planHash,
            long revision, MigrationClaimState state, MigrationClaimDisposition? disposition, DateTimeOffset updatedOnUtc)
        {
            TargetKey = targetKey; ClaimId = claimId; ExecutionToken = executionToken; PlanHash = planHash;
            Revision = revision; State = state; Disposition = disposition; UpdatedOnUtc = updatedOnUtc;
        }
    }

    /// <summary>Disposal releases the owner handle, never silently clears durable unfinished work.</summary>
    public interface IMigrationExecutionLease : IDisposable
    {
        MigrationExecutionClaim Claim { get; }
        PersistenceWriteResult Finish(MigrationClaimDisposition disposition, CancellationToken token = default);
    }

    public sealed class MigrationExecutionAdmission
    {
        public MigrationAdmissionStatus Status { get; }
        public IMigrationExecutionLease Lease { get; }
        public MigrationExecutionClaim ExistingClaim { get; }
        public string ErrorCode { get; }

        public MigrationExecutionAdmission(MigrationAdmissionStatus status, IMigrationExecutionLease lease = null,
            MigrationExecutionClaim existingClaim = null, string errorCode = null)
        { Status = status; Lease = lease; ExistingClaim = existingClaim; ErrorCode = errorCode; }
    }

    /// <summary>
    /// Optional durable ownership for cooperating executors sharing one store. The host supplies
    /// a canonical physical-target identity, not a credential, connection alias or runtime GUID.
    /// This capability alone does not cause MigrationManager to acquire a claim.
    /// </summary>
    public interface IMigrationExecutionOwnership
    {
        MigrationExecutionAdmission TryAcquireMigrationExecution(string targetIdentity, string executionToken,
            string planHash, CancellationToken token = default);
        MigrationExecutionClaim ReadMigrationExecutionClaim(string targetIdentity);
        PersistenceWriteResult ReconcileMigrationExecution(string targetIdentity, string expectedClaimId,
            string actor, string evidenceReference, CancellationToken token = default);
    }

    /// <summary>A captured store must retain its identity, loader and root for the entire operation.</summary>
    public interface IMigrationExecutionStorage : IMigrationExecutionOwnership, IMigrationHistoryPersistence
    {
        string ScopeIdentity { get; }
        MigrationHistory LoadMigrationHistory(string dataSourceName);
    }

    /// <summary>Optional configuration capability for immutable migration storage capture.</summary>
    public interface IMigrationExecutionStorageProvider
    {
        IMigrationExecutionStorage CaptureMigrationExecutionStorage();
    }
}
