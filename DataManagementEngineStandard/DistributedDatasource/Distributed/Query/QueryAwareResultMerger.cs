using System;
using System.Collections.Generic;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Distributed.Execution;
using TheTechIdea.Beep.Distributed.Routing;

namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Phase 08 <see cref="IQueryAwareResultMerger"/> implementation.
    /// Dispatches on <see cref="MergeSpec.Operation"/> to Union,
    /// Top-N k-way merge, SortMerge, or GroupAggregate paths. Falls
    /// back to <see cref="BasicResultMerger"/> behaviour when asked
    /// through the plain <see cref="IResultMerger"/> surface (no
    /// plan present).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The merger is stateless; one instance can serve all concurrent
    /// reads on the distributed data source. Worker helpers live in
    /// partial class files
    /// (<c>QueryAwareResultMerger.Sorting.cs</c>,
    /// <c>QueryAwareResultMerger.Grouping.cs</c>) so each file does
    /// one job.
    /// </para>
    /// <para>
    /// When a shard's enumerable is <c>null</c> the merger logs
    /// nothing — the Phase 06 executor is responsible for partial
    /// failure reporting via
    /// <see cref="BasicResultMerger"/> / shard-level events. The
    /// merger merely skips the entry.
    /// </para>
    /// </remarks>
    public sealed partial class QueryAwareResultMerger : IQueryAwareResultMerger
    {
        private readonly IResultMerger _basicFallback;

        /// <summary>Creates a merger that falls back to <see cref="BasicResultMerger"/>.</summary>
        public QueryAwareResultMerger()
            : this(new BasicResultMerger())
        {
        }

        /// <summary>Creates a merger with a custom fallback (primarily for tests).</summary>
        public QueryAwareResultMerger(IResultMerger basicFallback)
        {
            _basicFallback = basicFallback ?? new BasicResultMerger();
        }

        // ── IResultMerger pass-through ─────────────────────────────────────

        /// <inheritdoc/>
        public IEnumerable<object> MergeRows(
            RoutingDecision                      decision,
            IReadOnlyList<IEnumerable<object>>   perShardResults)
            => _basicFallback.MergeRows(decision, perShardResults);

        /// <inheritdoc/>
        public PagedResult MergePaged(
            RoutingDecision            decision,
            IReadOnlyList<PagedResult> perShardResults,
            int                        pageNumber,
            int                        pageSize)
            => _basicFallback.MergePaged(decision, perShardResults, pageNumber, pageSize);

        /// <inheritdoc/>
        public double MergeScalar(
            RoutingDecision       decision,
            IReadOnlyList<double> perShardResults)
            => _basicFallback.MergeScalar(decision, perShardResults);

        // ── IQueryAwareResultMerger ────────────────────────────────────────

        /// <inheritdoc/>
        public IEnumerable<object> MergePlan(
            QueryPlan                           plan,
            RoutingDecision                     decision,
            IReadOnlyList<IEnumerable<object>>  perShardResults)
        {
            if (plan            == null) throw new ArgumentNullException(nameof(plan));
            if (decision        == null) throw new ArgumentNullException(nameof(decision));
            if (perShardResults == null) perShardResults = Array.Empty<IEnumerable<object>>();

            switch (plan.Merge.Operation)
            {
                case MergeOperation.Union:
                    return MergeUnion(decision, perShardResults);

                case MergeOperation.TopN:
                    return MergeTopN(plan.Merge, perShardResults);

                case MergeOperation.SortMerge:
                    return MergeSortMerge(plan.Merge, perShardResults);

                case MergeOperation.GroupAggregate:
                    return MergeGroupAggregate(plan.Merge, perShardResults);

                default:
                    // Forward-compat: treat unknown ops as a union so callers see something sensible.
                    return MergeUnion(decision, perShardResults);
            }
        }

        private IEnumerable<object> MergeUnion(
            RoutingDecision                     decision,
            IReadOnlyList<IEnumerable<object>>  perShardResults)
            => _basicFallback.MergeRows(decision, perShardResults);
    }
}
