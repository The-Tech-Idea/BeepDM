using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Studio;

namespace TheTechIdea.Beep.Studio.Migration.Ledger;

/// <summary>
/// Unified, queryable ledger for schema and data migrations.
/// Records every Apply, DryRun, Rollback, and data-sync operation.
/// </summary>
public interface IMigrationLedger
{
    /// <summary>Record a new migration event. Returns the persisted entry.</summary>
    Task<StudioResult<MigrationLedgerEntry>> RecordAsync(MigrationLedgerEntry entry, CancellationToken ct = default);

    /// <summary>Update the status of an existing entry (e.g. Pending → Succeeded).</summary>
    Task<StudioResult<MigrationLedgerEntry>> UpdateStatusAsync(string entryId, MigrationLedgerStatus status, string? errorMessage = null, CancellationToken ct = default);

    /// <summary>Query ledger entries with optional filters.</summary>
    Task<StudioResult<IReadOnlyList<MigrationLedgerEntry>>> QueryAsync(MigrationLedgerQuery query, CancellationToken ct = default);

    /// <summary>Return true if a migration with the given PlanHash has already been applied (idempotency gate).</summary>
    Task<StudioResult<bool>> IsAppliedAsync(string planHash, string? datasourceName = null, CancellationToken ct = default);

    /// <summary>Get the rollback chain for a given entry (parent → children).</summary>
    Task<StudioResult<IReadOnlyList<MigrationLedgerEntry>>> GetRollbackChainAsync(string entryId, CancellationToken ct = default);

    /// <summary>Count ledger entries by status for a scope.</summary>
    Task<StudioResult<int>> CountAsync(MigrationLedgerQuery? filter = null, CancellationToken ct = default);
}
