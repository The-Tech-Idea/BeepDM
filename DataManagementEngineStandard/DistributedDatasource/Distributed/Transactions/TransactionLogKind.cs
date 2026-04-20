namespace TheTechIdea.Beep.Distributed.Transactions
{
    /// <summary>
    /// Kind of entry captured by an
    /// <see cref="IDistributedTransactionLog"/>. Used for observability
    /// (v1, in-memory) and for crash-recovery reconciliation in
    /// Phase 13 (durable log).
    /// </summary>
    public enum TransactionLogKind
    {
        /// <summary>Scope opened.</summary>
        Begin = 0,

        /// <summary>2PC prepare sent to one shard.</summary>
        PrepareSent = 1,

        /// <summary>2PC prepare acknowledged by one shard.</summary>
        PrepareAck = 2,

        /// <summary>2PC prepare failed / rejected by one shard.</summary>
        PrepareNack = 3,

        /// <summary>Coordinator reached the global commit decision.</summary>
        GlobalCommit = 4,

        /// <summary>Coordinator reached the global abort decision.</summary>
        GlobalAbort = 5,

        /// <summary>Commit round succeeded on one shard.</summary>
        CommitAck = 6,

        /// <summary>Commit round failed on one shard (in-doubt trigger).</summary>
        CommitFailed = 7,

        /// <summary>Rollback round succeeded on one shard.</summary>
        RollbackAck = 8,

        /// <summary>Rollback round failed on one shard.</summary>
        RollbackFailed = 9,

        /// <summary>Saga forward step succeeded.</summary>
        SagaForwardAck = 10,

        /// <summary>Saga forward step failed — triggers compensation.</summary>
        SagaForwardFailed = 11,

        /// <summary>Saga compensation step succeeded.</summary>
        SagaCompensationAck = 12,

        /// <summary>Saga compensation step failed — operator attention required.</summary>
        SagaCompensationFailed = 13,

        /// <summary>Coordinator entered the <see cref="DistributedTransactionStatus.InDoubt"/> state.</summary>
        InDoubt = 14,

        /// <summary>Scope closed (terminal state reached).</summary>
        Closed = 15,
    }
}
