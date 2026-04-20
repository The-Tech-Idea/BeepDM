using TheTechIdea.Beep.Distributed.Routing;

namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Converts a high-level <see cref="QueryIntent"/> + a Phase 05
    /// <see cref="RoutingDecision"/> into a
    /// <see cref="QueryPlan"/> (per-shard sub-intents + merge spec).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default implementation is <see cref="QueryPlanner"/>.
    /// Callers may swap in a richer planner via
    /// <see cref="DistributedDataSource"/> without touching the
    /// read path; the executor only needs a
    /// <see cref="QueryPlan"/>.
    /// </para>
    /// <para>
    /// The planner MUST leave the incoming
    /// <see cref="RoutingDecision"/> untouched — routing is owned by
    /// Phase 05 and should only be produced by the router. This
    /// keeps routing policy (hooks, unmapped behaviour) in one place.
    /// </para>
    /// </remarks>
    public interface IQueryPlanner
    {
        /// <summary>
        /// Produces a <see cref="QueryPlan"/> for
        /// <paramref name="intent"/> that runs on every shard listed
        /// in <paramref name="decision"/>.
        /// </summary>
        /// <param name="intent">Original intent; must not be <c>null</c>.</param>
        /// <param name="decision">Router output; must not be <c>null</c>.</param>
        /// <returns>Immutable plan ready for execution.</returns>
        QueryPlan Plan(QueryIntent intent, RoutingDecision decision);
    }
}
