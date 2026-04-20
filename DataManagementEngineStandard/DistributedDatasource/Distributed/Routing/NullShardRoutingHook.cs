namespace TheTechIdea.Beep.Distributed.Routing
{
    /// <summary>
    /// Default no-op implementation of <see cref="IShardRoutingHook"/>.
    /// Returns the baseline decision unchanged so the router can
    /// always invoke a hook without null-checks.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Instance"/> rather than allocating new
    /// instances; the type is stateless.
    /// </remarks>
    public sealed class NullShardRoutingHook : IShardRoutingHook
    {
        /// <summary>Shared singleton.</summary>
        public static NullShardRoutingHook Instance { get; } = new NullShardRoutingHook();

        private NullShardRoutingHook() { }

        /// <inheritdoc/>
        public RoutingDecision OnRouteResolved(RoutingDecision baseline, ShardRoutingHookContext context)
            => baseline;
    }
}
