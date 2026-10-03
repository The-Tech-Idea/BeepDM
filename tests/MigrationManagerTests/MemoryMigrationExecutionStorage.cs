using System.Security.Cryptography;
using System.Text;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.JsonLoaderService;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

// Recording-fixture ownership, not evidence of filesystem/process durability.
internal sealed class MemoryMigrationExecutionStorage(MigrationTestHarness harness) : IMigrationExecutionStorage
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Lease> _claims = new(StringComparer.Ordinal);
    public string ScopeIdentity { get; } = Guid.NewGuid().ToString("N");
    public MigrationHistory LoadMigrationHistory(string name)
    {
        lock (_gate)
        {
            var codec = new JsonLoader();
            return codec.DeserializeSnapshot<MigrationHistory>(codec.SerializeSnapshot(harness.History));
        }
    }
    public PersistenceWriteResult SaveMigrationHistoryAcknowledged(MigrationHistory history, CancellationToken token = default)
        => new(PersistenceWriteStatus.Unsupported);
    public PersistenceWriteResult AppendMigrationRecordAcknowledged(string name, DataSourceType type,
        MigrationRecord record, CancellationToken token = default)
    {
        lock (_gate)
        {
            token.ThrowIfCancellationRequested();
            var result = harness.HistoryWrite?.Invoke(record) ?? new(PersistenceWriteStatus.Saved);
            if (result.IsSaved) harness.History.Migrations.Add(record);
            return result;
        }
    }
    public MigrationExecutionAdmission TryAcquireMigrationExecution(string target, string token, string hash, CancellationToken cancellation = default)
    {
        lock (_gate)
        {
            if (cancellation.IsCancellationRequested) return new(MigrationAdmissionStatus.Cancelled);
            if (_claims.TryGetValue(target, out var previous))
            {
                if (previous.Held) return new(MigrationAdmissionStatus.Busy, existingClaim: previous.Claim);
                if (previous.Claim.State != MigrationClaimState.Released)
                    return new(MigrationAdmissionStatus.RequiresReconciliation, existingClaim: previous.Claim);
            }
            var claim = new MigrationExecutionClaim(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(target))),
                Guid.NewGuid().ToString("N"), token, hash, (previous?.Claim.Revision ?? 0) + 1,
                MigrationClaimState.Owned, null, DateTimeOffset.UtcNow);
            var lease = new Lease(this, claim);
            _claims[target] = lease;
            return new(MigrationAdmissionStatus.Acquired, lease);
        }
    }
    public MigrationExecutionClaim? ReadMigrationExecutionClaim(string target)
    { lock (_gate) return _claims.TryGetValue(target, out var entry) ? entry.Claim : null; }
    public PersistenceWriteResult ReconcileMigrationExecution(string target, string id, string actor, string evidence, CancellationToken token = default)
    {
        lock (_gate)
        {
            if (!_claims.TryGetValue(target, out var entry) || entry.Held || entry.Claim.ClaimId != id ||
                entry.Claim.State == MigrationClaimState.Released || string.IsNullOrWhiteSpace(actor) || string.IsNullOrWhiteSpace(evidence))
                return new(PersistenceWriteStatus.Failed);
            entry.Transition(MigrationClaimDisposition.OperatorReconciled);
            return new(PersistenceWriteStatus.Saved);
        }
    }
    private sealed class Lease(MemoryMigrationExecutionStorage owner, MigrationExecutionClaim claim) : IMigrationExecutionLease
    {
        public bool Held = true;
        public MigrationExecutionClaim Claim { get; private set; } = claim;
        public PersistenceWriteResult Finish(MigrationClaimDisposition disposition, CancellationToken token = default)
        {
            lock (owner._gate)
            {
                if (!Held || token.IsCancellationRequested || Claim.State != MigrationClaimState.Owned)
                    return new(PersistenceWriteStatus.Failed);
                Transition(disposition);
                return new(PersistenceWriteStatus.Saved);
            }
        }
        public void Transition(MigrationClaimDisposition disposition) => Claim = new(Claim.TargetKey, Claim.ClaimId,
            Claim.ExecutionToken, Claim.PlanHash, Claim.Revision + 1,
            disposition == MigrationClaimDisposition.RequiresReconciliation ? MigrationClaimState.RequiresReconciliation : MigrationClaimState.Released,
            disposition, DateTimeOffset.UtcNow);
        public void Dispose() { lock (owner._gate) Held = false; }
    }
}
