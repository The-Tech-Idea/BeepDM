namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Shape of the cross-shard merge the Phase 08 planner attached to
    /// a <see cref="QueryPlan"/>. The value decides which code path in
    /// <see cref="QueryAwareResultMerger"/> runs against the per-shard
    /// results.
    /// </summary>
    /// <remarks>
    /// The planner picks exactly one operation per plan. Complex
    /// intents (e.g. <c>GROUP BY</c> + <c>ORDER BY</c> + <c>TOP</c>)
    /// are expressed as a <see cref="GroupAggregate"/> merge that
    /// keeps sort/top information on the <see cref="MergeSpec"/> and
    /// applies them after the group merge finishes.
    /// </remarks>
    public enum MergeOperation
    {
        /// <summary>
        /// Concatenate per-shard rows in the order shards returned
        /// them. No sort, no dedupe. Matches the v1 Phase 06 behaviour
        /// for filter-only reads.
        /// </summary>
        Union = 0,

        /// <summary>
        /// K-way merge the per-shard outputs by
        /// <see cref="MergeSpec.OrderBy"/> and stop after
        /// <see cref="MergeSpec.TopN"/> rows. Each shard is expected
        /// to return at most <c>TopN</c> pre-sorted rows.
        /// </summary>
        TopN = 1,

        /// <summary>
        /// K-way merge the per-shard outputs by
        /// <see cref="MergeSpec.OrderBy"/> with no row limit. Used
        /// for <c>ORDER BY</c> without <c>TOP</c> / <c>LIMIT</c>.
        /// </summary>
        SortMerge = 2,

        /// <summary>
        /// Group per-shard partial aggregates by
        /// <see cref="MergeSpec.GroupBy"/> and fold their
        /// <see cref="MergeSpec.Aggregates"/> across shards. AVG is
        /// rewritten upstream to a SUM/COUNT pair and divided after
        /// merging to avoid an "average of averages" error.
        /// </summary>
        GroupAggregate = 3,
    }
}
