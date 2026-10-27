using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>Optional explicit transaction ownership. A caller-created opaque token owns one
    /// begin/commit/rollback lifetime. Refusals must not finish or poison another owner's outcome.
    /// An advertised but unsupported capability must not silently fall back to legacy transactions.</summary>
    public interface IOwnedTransactionDataSource
    {
        bool SupportsOwnedTransactions { get; }
        bool OwnsTransaction(object owner);
        IErrorsInfo BeginOwnedTransaction(object owner);
        IErrorsInfo CommitOwnedTransaction(object owner);
        IErrorsInfo RollbackOwnedTransaction(object owner);
    }

    /// <summary>This call was refused before native transactional dispatch or resource changes.
    /// Another owner's scope may exist; this is never an unknown/partially allocated begin result.</summary>
    public sealed class TransactionOwnershipRefusal : ErrorsInfo
    {
        public TransactionOwnershipRefusal(string message) { Flag = TheTechIdea.Beep.ConfigUtil.Errors.Failed; Message = message; }
    }
}
