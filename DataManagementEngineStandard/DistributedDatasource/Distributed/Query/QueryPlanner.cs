using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Distributed.Routing;

namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Default Phase 08 <see cref="IQueryPlanner"/>. Pushes filters,
    /// columns, order-by, and top-N down to every shard, splits
    /// <see cref="AggregateKind.Avg"/> into a SUM/COUNT pair so the
    /// merger can compute <c>total_sum / total_count</c>, and
    /// produces a single <see cref="MergeSpec"/> describing how the
    /// per-shard outputs should be combined.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Planning rules:
    /// <list type="bullet">
    ///   <item>Single-shard decision → trivial plan with a pass-through
    ///   <see cref="MergeOperation.Union"/> merge and the unchanged
    ///   intent.</item>
    ///   <item>Simple filter/projection intent → push the full intent
    ///   to every shard and merge with
    ///   <see cref="MergeOperation.Union"/>.</item>
    ///   <item>Intent with order-by / top-N but no aggregates →
    ///   push order-by + (<c>offset + top</c>) cap per shard and
    ///   merge with <see cref="MergeOperation.TopN"/> /
    ///   <see cref="MergeOperation.SortMerge"/>.</item>
    ///   <item>Intent with aggregates → push group-by + partial
    ///   aggregates (with AVG split) to every shard and merge with
    ///   <see cref="MergeOperation.GroupAggregate"/>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// The planner is deliberately stateless — no caching in v1 —
    /// so plan construction stays cheap and deterministic.
    /// </para>
    /// </remarks>
    public sealed class QueryPlanner : IQueryPlanner
    {
        /// <summary>Shared, reusable default instance.</summary>
        public static readonly QueryPlanner Instance = new QueryPlanner();

        /// <inheritdoc/>
        public QueryPlan Plan(QueryIntent intent, RoutingDecision decision)
        {
            if (intent   == null) throw new ArgumentNullException(nameof(intent));
            if (decision == null) throw new ArgumentNullException(nameof(decision));

            if (decision.ShardIds.Count == 0)
            {
                throw new InvalidOperationException(
                    $"QueryPlanner: routing decision for '{intent.EntityName}' has no target shards " +
                    $"(MatchKind={decision.MatchKind}, Mode={decision.Mode}).");
            }

            // Single-shard → trivial pass-through plan.
            if (decision.ShardIds.Count == 1)
            {
                var perShard = new Dictionary<string, QueryIntent>(1, StringComparer.OrdinalIgnoreCase)
                {
                    [decision.ShardIds[0]] = intent,
                };
                return new QueryPlan(intent, decision, perShard, MergeSpec.Union());
            }

            // Fan-out → compute sub-intent + merge spec from the intent shape.
            if (intent.HasAggregates)
            {
                return PlanGroupAggregate(intent, decision);
            }

            if (intent.HasOrderBy || intent.HasTop || intent.Offset > 0)
            {
                return PlanOrderedOrTopN(intent, decision);
            }

            // Simple filter/projection read — push intent as-is to every shard, union results.
            var unionPerShard = BuildPerShardMap(decision.ShardIds, intent);
            return new QueryPlan(intent, decision, unionPerShard, MergeSpec.Union());
        }

        // ── Planning helpers ──────────────────────────────────────────────

        private static QueryPlan PlanOrderedOrTopN(QueryIntent intent, RoutingDecision decision)
        {
            // Each shard pulls enough rows to satisfy the worst case
            // (offset + top) so the merger can trim globally; when
            // offset/top are 0 this simply turns into an unbounded
            // sort-merge with identical per-shard intents.
            int perShardTop = 0;
            if (intent.Top > 0)
            {
                perShardTop = intent.Top + intent.Offset;
                if (perShardTop < intent.Top) perShardTop = intent.Top; // overflow guard
            }

            var subIntent = new QueryIntent(
                entityName: intent.EntityName,
                filters:    intent.Filters,
                columns:    intent.Columns,
                groupBy:    null,
                aggregates: null,
                orderBy:    intent.OrderBy,
                top:        perShardTop,
                offset:     0,
                rawSql:     intent.RawSql);

            var perShard = BuildPerShardMap(decision.ShardIds, subIntent);

            var merge = intent.HasTop
                ? MergeSpec.ForTopN(intent.OrderBy, intent.Top, intent.Offset)
                : MergeSpec.SortMerge(intent.OrderBy, intent.Offset);

            return new QueryPlan(intent, decision, perShard, merge);
        }

        private static QueryPlan PlanGroupAggregate(QueryIntent intent, RoutingDecision decision)
        {
            // Split AVG → (SUM, COUNT) pair; preserve the other
            // aggregates verbatim. Per-shard intent carries the
            // exploded list so each shard produces the partial pair.
            var perShardAggregates = new List<AggregateSpec>(intent.Aggregates.Count + 1);
            var partials           = new List<PartialAggregate>(intent.Aggregates.Count + 1);

            foreach (var spec in intent.Aggregates)
            {
                if (spec.Kind == AggregateKind.Avg)
                {
                    var sumAlias   = spec.Alias + "__sum";
                    var countAlias = spec.Alias + "__count";

                    perShardAggregates.Add(new AggregateSpec(AggregateKind.Sum,   spec.Column, sumAlias));
                    perShardAggregates.Add(new AggregateSpec(AggregateKind.Count, spec.Column, countAlias));

                    partials.Add(new PartialAggregate(
                        kind:         AggregateKind.Sum,
                        column:       spec.Column,
                        partialKey:   sumAlias,
                        outputAlias:  spec.Alias,
                        avgPairAlias: spec.Alias));

                    partials.Add(new PartialAggregate(
                        kind:         AggregateKind.Count,
                        column:       spec.Column,
                        partialKey:   countAlias,
                        outputAlias:  spec.Alias,
                        avgPairAlias: spec.Alias));
                }
                else
                {
                    perShardAggregates.Add(spec);
                    partials.Add(new PartialAggregate(
                        kind:        spec.Kind,
                        column:      spec.Column,
                        partialKey:  spec.Alias,
                        outputAlias: spec.Alias));
                }
            }

            // Per-shard sub-intent: same group-by + filters, exploded
            // aggregates, no top/offset (we need every group). Order-by
            // is preserved so shard engines can stream pre-sorted
            // groups when supported, but the merger re-applies it.
            var subIntent = new QueryIntent(
                entityName: intent.EntityName,
                filters:    intent.Filters,
                columns:    intent.Columns,
                groupBy:    intent.GroupBy,
                aggregates: perShardAggregates,
                orderBy:    intent.OrderBy,
                top:        0,
                offset:     0,
                rawSql:     intent.RawSql);

            var perShard = BuildPerShardMap(decision.ShardIds, subIntent);

            var merge = MergeSpec.GroupAggregate(
                groupBy:    intent.GroupBy,
                aggregates: partials,
                orderBy:    intent.OrderBy,
                topN:       intent.Top,
                offset:     intent.Offset);

            return new QueryPlan(intent, decision, perShard, merge);
        }

        private static Dictionary<string, QueryIntent> BuildPerShardMap(
            IReadOnlyList<string> shardIds,
            QueryIntent           subIntent)
        {
            var map = new Dictionary<string, QueryIntent>(shardIds.Count, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < shardIds.Count; i++)
            {
                map[shardIds[i]] = subIntent;
            }
            return map;
        }
    }
}
