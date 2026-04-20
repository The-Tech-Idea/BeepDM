using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Transactions
{
    /// <summary>
    /// Append-only log consumed by the Phase 09
    /// <see cref="IDistributedTransactionCoordinator"/>. V1 ships an
    /// in-memory implementation (<see cref="InMemoryTransactionLog"/>)
    /// that is suitable for diagnostics but not for crash recovery;
    /// Phase 13 will introduce a durable file-based log that swaps in
    /// behind the same contract.
    /// </summary>
    /// <remarks>
    /// Implementations MUST be safe for concurrent appends: the
    /// coordinator writes entries from the prepare / commit /
    /// rollback fan-outs without serialising on its own.
    /// </remarks>
    public interface IDistributedTransactionLog
    {
        /// <summary>Appends one entry.</summary>
        void Append(TransactionLogEntry entry);

        /// <summary>
        /// Returns a snapshot of every entry recorded for the given
        /// correlation id, ordered by
        /// <see cref="TransactionLogEntry.TimestampUtc"/>.
        /// </summary>
        IReadOnlyList<TransactionLogEntry> Read(string correlationId);

        /// <summary>
        /// Drops the log for the given correlation id once the scope
        /// has reached a terminal state. Implementations may keep the
        /// tail for a short retention window (useful for support /
        /// audit) or discard immediately.
        /// </summary>
        void Close(string correlationId);

        /// <summary>
        /// Returns a snapshot of every scope currently in a non-terminal
        /// state — used by recovery to spot scopes that survived a
        /// crash. V1 returns only scopes still live in memory.
        /// </summary>
        IReadOnlyList<string> OpenCorrelationIds();
    }
}
