namespace TheTechIdea.Beep.Distributed.Placement
{
    /// <summary>
    /// Policy that controls how
    /// <see cref="EntityPlacementResolver.Resolve(string, bool)"/>
    /// behaves when no <see cref="EntityPlacement"/> matches the entity
    /// name. Mirrors the <c>EntityAffinityFallback</c> knobs from the
    /// <c>Proxy/EntityAffinityMap</c> tier.
    /// </summary>
    public enum UnmappedEntityPolicy
    {
        /// <summary>Reject the request — the resolver returns an <see cref="PlacementMatchKind.Unmapped"/> resolution and the caller throws.</summary>
        RejectUnmapped    = 0,

        /// <summary>Route the request to the configured default shard (Routed mode).</summary>
        DefaultShardId    = 1,

        /// <summary>Broadcast the request to every live shard in the catalog.</summary>
        BroadcastUnmapped = 2
    }
}
