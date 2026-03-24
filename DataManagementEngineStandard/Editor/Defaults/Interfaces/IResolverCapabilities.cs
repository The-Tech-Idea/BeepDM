namespace TheTechIdea.Beep.Editor.Defaults.Interfaces
{
    /// <summary>
    /// Describes the runtime capabilities and scheduling metadata of a resolver.
    /// Implemented by resolvers that want to declare priority or caching hints.
    /// Base class <c>BaseDefaultValueResolver</c> supplies safe defaults when not implemented.
    /// </summary>
    public interface IResolverCapabilities
    {
        /// <summary>Lower numbers are tried first when multiple resolvers claim a rule. Default: 100.</summary>
        int Priority { get; }

        /// <summary>
        /// True when the same rule + context always produces the same value.
        /// Non-deterministic resolvers (RANDOM, volatile NOW) must return false.
        /// Cache (Phase 6) skips storing results when this is false.
        /// </summary>
        bool IsDeterministic { get; }

        /// <summary>True when the resolver's results are safe to cache across calls.</summary>
        bool SupportsCaching { get; }

        /// <summary>Future-ready flag — true when the resolver can execute asynchronously.</summary>
        bool SupportsAsync { get; }
    }
}
