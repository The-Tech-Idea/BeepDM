using System.Collections.Generic;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Distributed.Routing;

namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// Strategy for combining per-shard read results produced by the
    /// Phase 06 scatter executor into a single logical result handed
    /// back to the caller.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Phase 06 ships <see cref="BasicResultMerger"/> which concatenates
    /// row sequences and sums scalar / count values. Phase 08 introduces
    /// a query-aware merger (<c>QueryAwareResultMerger</c>) that honours
    /// <c>ORDER BY</c>, aggregate functions, and <c>LIMIT</c>; callers
    /// swap mergers via <see cref="DistributedDataSource"/> construction.
    /// </para>
    /// <para>
    /// Implementations should be stateless and thread-safe: a single
    /// merger instance is shared across concurrent reads.
    /// </para>
    /// </remarks>
    public interface IResultMerger
    {
        /// <summary>
        /// Merges the row-enumerable results produced by each shard in
        /// order of <see cref="RoutingDecision.ShardIds"/>.
        /// </summary>
        /// <param name="decision">Routing decision that produced the shard fan-out.</param>
        /// <param name="perShardResults">Per-shard enumerables. Missing shards (best-effort drops) are represented by <c>null</c> entries.</param>
        /// <returns>Combined row sequence; never <c>null</c>.</returns>
        IEnumerable<object> MergeRows(
            RoutingDecision                decision,
            IReadOnlyList<IEnumerable<object>> perShardResults);

        /// <summary>
        /// Merges the paged results returned by each shard. Total
        /// record counts are summed; the caller-supplied page metadata
        /// from <paramref name="decision"/> is preserved on the merged
        /// response.
        /// </summary>
        /// <param name="decision">Routing decision that produced the shard fan-out.</param>
        /// <param name="perShardResults">Per-shard <see cref="PagedResult"/> values; <c>null</c> when the shard dropped out.</param>
        /// <param name="pageNumber">Page number originally requested by the caller.</param>
        /// <param name="pageSize">Page size originally requested by the caller.</param>
        PagedResult MergePaged(
            RoutingDecision            decision,
            IReadOnlyList<PagedResult> perShardResults,
            int                        pageNumber,
            int                        pageSize);

        /// <summary>
        /// Merges scalar values returned by <c>GetScalar</c> /
        /// <c>GetScalarAsync</c>. The default contract is "sum across
        /// shards" because those methods are overwhelmingly used for
        /// <c>COUNT</c> / <c>SUM</c> style queries. Query-aware mergers
        /// override when the originating SQL is an aggregate the
        /// basic merger cannot decompose (<c>AVG</c>, <c>MIN</c>, etc.).
        /// </summary>
        double MergeScalar(
            RoutingDecision     decision,
            IReadOnlyList<double> perShardResults);
    }
}
