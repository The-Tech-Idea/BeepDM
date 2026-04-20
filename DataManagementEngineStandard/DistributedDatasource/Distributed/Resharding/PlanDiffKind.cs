namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Classification of a single <see cref="PlanDiffEntry"/>
    /// produced by <see cref="PlanDiff.Compute"/>. Each kind maps to
    /// exactly one migration primitive on
    /// <see cref="IReshardingService"/>.
    /// </summary>
    public enum PlanDiffKind
    {
        /// <summary>Entity is present in both plans with identical placement — no action required.</summary>
        NoOp             = 0,

        /// <summary>Entity is new in the target plan; insert it on the declared shards.</summary>
        AddEntity        = 1,

        /// <summary>Entity was removed in the target plan; drain its placement.</summary>
        RemoveEntity     = 2,

        /// <summary>
        /// Placement shard list changed (add/drop shards) while the
        /// partition function is compatible — drives
        /// <see cref="IReshardingService.RepartitionEntityAsync"/>.
        /// </summary>
        Repartition      = 3,

        /// <summary>
        /// Placement moved to a single, different shard while the
        /// distribution mode stays <c>Routed</c> — drives
        /// <see cref="IReshardingService.MoveEntityAsync"/>.
        /// </summary>
        MoveEntity       = 4,

        /// <summary>
        /// Distribution mode or partition-function kind changed —
        /// treated as a full repartition. The target plan's placement
        /// replaces the source; data is copied per new placement.
        /// </summary>
        ModeChange       = 5
    }
}
