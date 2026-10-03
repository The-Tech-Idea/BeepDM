using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

internal sealed class MigrationStorageProbe(IMigrationExecutionStorage inner) : IMigrationExecutionStorage
{
    public Func<MigrationRecord, PersistenceWriteResult?>? AppendOverride { get; set; }
    public Func<MigrationExecutionClaim, MigrationExecutionClaim>? ClaimOverride { get; set; }
    public Func<MigrationClaimDisposition, PersistenceWriteResult>? FinishOverride { get; set; }
    public int FinishCalls { get; private set; }
    public string ScopeIdentity => inner.ScopeIdentity;
    public MigrationHistory LoadMigrationHistory(string name) => inner.LoadMigrationHistory(name);
    public PersistenceWriteResult SaveMigrationHistoryAcknowledged(MigrationHistory history, CancellationToken token = default)
        => inner.SaveMigrationHistoryAcknowledged(history, token);
    public PersistenceWriteResult AppendMigrationRecordAcknowledged(string name, DataSourceType type,
        MigrationRecord record, CancellationToken token = default)
        => AppendOverride?.Invoke(record) ?? inner.AppendMigrationRecordAcknowledged(name, type, record, token);
    public MigrationExecutionAdmission TryAcquireMigrationExecution(string target, string token, string hash, CancellationToken cancellation = default)
    {
        var admission = inner.TryAcquireMigrationExecution(target, token, hash, cancellation);
        return admission.Status != MigrationAdmissionStatus.Acquired ? admission :
            new(MigrationAdmissionStatus.Acquired, new ProbeLease(this, admission.Lease));
    }
    public MigrationExecutionClaim ReadMigrationExecutionClaim(string target) => inner.ReadMigrationExecutionClaim(target);
    public PersistenceWriteResult ReconcileMigrationExecution(string target, string id, string actor, string evidence, CancellationToken token = default)
        => inner.ReconcileMigrationExecution(target, id, actor, evidence, token);
    private sealed class ProbeLease(MigrationStorageProbe probe, IMigrationExecutionLease lease) : IMigrationExecutionLease
    {
        public MigrationExecutionClaim Claim => probe.ClaimOverride?.Invoke(lease.Claim) ?? lease.Claim;
        public PersistenceWriteResult Finish(MigrationClaimDisposition disposition, CancellationToken token = default)
        {
            probe.FinishCalls++;
            return probe.FinishOverride != null ? probe.FinishOverride(disposition) : lease.Finish(disposition, token);
        }
        public void Dispose() => lease.Dispose();
    }
}
