namespace TheTechIdea.Beep.Distributed.Placement
{
    /// <summary>
    /// How a <see cref="PlacementResolution"/> was reached for a given
    /// entity name. Drives diagnostics and routing-rule audit logs.
    /// </summary>
    public enum PlacementMatchKind
    {
        /// <summary>An exact <see cref="EntityPlacement"/> matched the entity name.</summary>
        Exact        = 0,

        /// <summary>A prefix-style placement (entity name ending in <c>*</c>) matched the entity.</summary>
        Prefix       = 1,

        /// <summary>No placement matched; the resolver fell back to the configured default shard.</summary>
        DefaultRoute = 2,

        /// <summary>No placement matched; the resolver fell back to broadcast (all live shards).</summary>
        Broadcast    = 3,

        /// <summary>No placement matched and no fallback applies; the caller must reject the request.</summary>
        Unmapped     = 4
    }
}
