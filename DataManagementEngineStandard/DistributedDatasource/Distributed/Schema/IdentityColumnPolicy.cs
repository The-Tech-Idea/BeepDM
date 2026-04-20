namespace TheTechIdea.Beep.Distributed.Schema
{
    /// <summary>
    /// Policy applied by <see cref="IDistributedSchemaService.CreateEntityAsync"/>
    /// when an <see cref="DataBase.EntityStructure"/> marked for
    /// <see cref="Plan.DistributionMode.Sharded"/> placement contains a
    /// database-generated identity column.
    /// </summary>
    /// <remarks>
    /// Per-shard identity sequences collide on a distributed insert path
    /// because nothing coordinates the generator across shards. Phase 12
    /// therefore surfaces this mismatch explicitly: either log a warning
    /// and let the operator accept the risk, or fail loudly and force
    /// the caller to switch to a distributed sequence provider
    /// (Snowflake / HiLo) or remove the identity column.
    /// </remarks>
    public enum IdentityColumnPolicy
    {
        /// <summary>
        /// Log a warning via <c>OnPlacementViolation</c> but proceed
        /// with the create. Intended for dev / test flows that rely on
        /// per-shard identity for local-only tests.
        /// </summary>
        WarnOnly = 0,

        /// <summary>
        /// Reject the create with a descriptive exception when a
        /// Sharded entity carries an identity column. Default for
        /// production-grade deployments.
        /// </summary>
        RejectShardedIdentity = 1
    }
}
