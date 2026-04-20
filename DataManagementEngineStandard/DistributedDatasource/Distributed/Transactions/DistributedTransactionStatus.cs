namespace TheTechIdea.Beep.Distributed.Transactions
{
    /// <summary>
    /// Lifecycle state of a <see cref="DistributedTransactionScope"/>
    /// tracked by the Phase 09 coordinator.
    /// </summary>
    /// <remarks>
    /// State transitions are linear:
    /// <c>Active → Preparing → Prepared → Committing → Committed</c>
    /// (2PC success) or
    /// <c>Active → Preparing → Aborting → Aborted</c>
    /// (2PC prepare failure). Single-shard uses
    /// <c>Active → Committing → Committed</c>. Sagas use
    /// <c>Active → Committing → Committed</c> on success and
    /// <c>Active → Compensating → Aborted</c> on failure. A
    /// <see cref="InDoubt"/> state is reached only when a commit
    /// round produced mixed acks after the prepare round succeeded.
    /// </remarks>
    public enum DistributedTransactionStatus
    {
        /// <summary>The scope is open and accepting work.</summary>
        Active = 0,

        /// <summary>2PC prepare round is in progress.</summary>
        Preparing = 1,

        /// <summary>2PC prepare round completed successfully.</summary>
        Prepared = 2,

        /// <summary>Single-shard or 2PC commit round is in progress.</summary>
        Committing = 3,

        /// <summary>Saga compensation sequence is in progress.</summary>
        Compensating = 4,

        /// <summary>2PC abort round is in progress (prepare failure).</summary>
        Aborting = 5,

        /// <summary>Terminal: the scope committed successfully.</summary>
        Committed = 6,

        /// <summary>Terminal: the scope rolled back.</summary>
        Aborted = 7,

        /// <summary>
        /// Terminal: at least one shard acked prepare but failed the
        /// commit phase; resolution requires operator intervention
        /// (or the Phase 13 recovery log). The coordinator raises
        /// <c>OnTransactionInDoubt</c> when it enters this state.
        /// </summary>
        InDoubt = 8,
    }
}
