using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor
{
    public enum UnitofWorkCommitOutcome { Committed, RolledBack, Unknown }

    /// <summary>Optional capability for writes inside a caller-owned provider transaction.</summary>
    public interface IEnlistedUnitofWork
    {
        bool SupportsEnlistedCommit { get; }
        Task<IUnitofWorkCommitStage> PrepareCommitAsync(IDataSource transactionOwner,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// A prepared write, not a transaction owner. Complete only after provider confirmation.
    /// Unknown preserves tracking and admission protection until explicit reconciliation.
    /// </summary>
    public interface IUnitofWorkCommitStage
    {
        IErrorsInfo Result { get; }
        bool IsResolved { get; }
        IErrorsInfo Complete(UnitofWorkCommitOutcome outcome);
    }
}
