using System.Collections.Generic;
using TheTechIdea.Beep.Distributed.Execution;
using TheTechIdea.Beep.Distributed.Routing;

namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Extends the Phase 06 <see cref="IResultMerger"/> with the
    /// <see cref="MergePlan"/> entry point that honours a
    /// <see cref="QueryPlan"/> produced by the Phase 08 planner
    /// (top-N, sort-merge, group-aggregate, …).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IResultMerger"/> remains the contract the Phase 06
    /// executor talks to. The query-aware merger implements both
    /// interfaces so callers can register a single implementation and
    /// let <see cref="DistributedDataSource"/> upgrade to the richer
    /// entry point when a plan is present.
    /// </para>
    /// <para>
    /// Implementations MUST cope with missing shard outputs (<c>null</c>
    /// entries in <paramref name="perShardResults"/>) and produce a
    /// deterministic result even when the routing decision's shard
    /// order differs from the enumerable order.
    /// </para>
    /// </remarks>
    public interface IQueryAwareResultMerger : IResultMerger
    {
        /// <summary>
        /// Merges per-shard row outputs according to
        /// <paramref name="plan"/>.
        /// </summary>
        /// <param name="plan">Plan produced by <see cref="IQueryPlanner"/>.</param>
        /// <param name="decision">Routing decision that produced the fan-out (always equal to <see cref="QueryPlan.Decision"/>; passed for convenience).</param>
        /// <param name="perShardResults">Per-shard row enumerables; <c>null</c> entries represent dropped / failed shards.</param>
        /// <returns>
        /// Merged row enumerable; never <c>null</c>. Rows are shaped
        /// as <see cref="IDictionary{TKey, TValue}"/> (with
        /// <see cref="string"/> keys) when the plan produces a
        /// group-aggregate, otherwise shards' native row objects are
        /// passed through in the requested order.
        /// </returns>
        IEnumerable<object> MergePlan(
            QueryPlan                           plan,
            RoutingDecision                     decision,
            IReadOnlyList<IEnumerable<object>>  perShardResults);
    }
}
