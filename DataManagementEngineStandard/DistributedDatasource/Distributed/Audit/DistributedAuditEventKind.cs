namespace TheTechIdea.Beep.Distributed.Audit
{
    /// <summary>
    /// Classification of a <see cref="DistributedAuditEvent"/>.
    /// The distribution tier emits one event per decision point;
    /// callers filter or route by this enum. Keep additive: persisted
    /// event logs must remain readable by older readers.
    /// </summary>
    public enum DistributedAuditEventKind
    {
        /// <summary>The placement resolver picked a target set for an entity.</summary>
        PlacementDecided    = 0,

        /// <summary>A scatter read was issued across multiple shards.</summary>
        Scattered           = 1,

        /// <summary>A write was fanned out to multiple shards (replicated / broadcast).</summary>
        FannedOut           = 2,

        /// <summary>An online resharding run was started.</summary>
        ReshardStarted      = 3,

        /// <summary>An online resharding run completed.</summary>
        ReshardCompleted    = 4,

        /// <summary>A DDL operation (create/alter/drop) was broadcast to shards.</summary>
        DDLBroadcast        = 5,

        /// <summary>A distributed transaction scope opened.</summary>
        TransactionBegan    = 6,

        /// <summary>A distributed transaction scope committed.</summary>
        TransactionCommit   = 7,

        /// <summary>A distributed transaction scope rolled back.</summary>
        TransactionRollback = 8,

        /// <summary>A request was denied by the configured access policy.</summary>
        AccessDenied        = 9,

        /// <summary>A shard was flagged hot and may be quarantined.</summary>
        HotShardDetected    = 10,

        /// <summary>An entity was flagged as experiencing unusually high RPS.</summary>
        HotEntityDetected   = 11,
    }
}
