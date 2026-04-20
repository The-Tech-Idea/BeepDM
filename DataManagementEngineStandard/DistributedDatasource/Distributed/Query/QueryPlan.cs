using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Distributed.Routing;

namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Immutable plan produced by the Phase 08 query planner: the
    /// per-shard sub-intent each shard should execute, the
    /// cross-shard <see cref="MergeSpec"/>, and the
    /// <see cref="RoutingDecision"/> that pinned down the target
    /// shards.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The plan is a read-only snapshot — running the same logical
    /// read twice yields two fresh plans because the shard set and
    /// placement may have changed. Callers MUST NOT mutate the
    /// returned dictionary; the planner builds a defensive copy.
    /// </para>
    /// <para>
    /// When <see cref="RoutingDecision.ShardIds"/> has exactly one
    /// id, <see cref="IsSingleShard"/> is <c>true</c> and the merge
    /// spec is always <see cref="MergeOperation.Union"/>. The
    /// executor short-circuits single-shard plans to a straight
    /// pass-through call with no merge overhead.
    /// </para>
    /// </remarks>
    public sealed class QueryPlan
    {
        /// <summary>Creates a new query plan.</summary>
        /// <param name="intent">The original high-level intent.</param>
        /// <param name="decision">Routing decision produced by the Phase 05 router.</param>
        /// <param name="perShardIntents">Per-shard sub-intent (shardId → sub-intent). Must cover every id in <paramref name="decision"/>.</param>
        /// <param name="merge">Merge specification applied after per-shard execution.</param>
        /// <param name="broadcastJoinApplied"><c>true</c> when the plan rewrote a broadcast-join; diagnostic only.</param>
        public QueryPlan(
            QueryIntent                                 intent,
            RoutingDecision                             decision,
            IReadOnlyDictionary<string, QueryIntent>    perShardIntents,
            MergeSpec                                   merge,
            bool                                        broadcastJoinApplied = false)
        {
            Intent          = intent   ?? throw new ArgumentNullException(nameof(intent));
            Decision        = decision ?? throw new ArgumentNullException(nameof(decision));
            PerShardIntents = perShardIntents ?? throw new ArgumentNullException(nameof(perShardIntents));
            Merge           = merge    ?? throw new ArgumentNullException(nameof(merge));

            BroadcastJoinApplied = broadcastJoinApplied;
        }

        /// <summary>Original intent the plan was built from.</summary>
        public QueryIntent Intent { get; }

        /// <summary>Routing decision the planner reused; authoritative source for <see cref="TargetShardIds"/>.</summary>
        public RoutingDecision Decision { get; }

        /// <summary>Per-shard sub-intent (shardId → sub-intent).</summary>
        public IReadOnlyDictionary<string, QueryIntent> PerShardIntents { get; }

        /// <summary>Cross-shard merge step the executor runs after every shard returns.</summary>
        public MergeSpec Merge { get; }

        /// <summary><c>true</c> when the broadcast-join rewriter fired for this plan.</summary>
        public bool BroadcastJoinApplied { get; }

        /// <summary>Convenience accessor over <see cref="Decision"/>.</summary>
        public IReadOnlyList<string> TargetShardIds => Decision.ShardIds;

        /// <summary><c>true</c> when the plan targets exactly one shard (no merge required).</summary>
        public bool IsSingleShard => Decision.ShardIds.Count == 1;

        /// <summary><c>true</c> when the plan fans out across more than one shard.</summary>
        public bool IsFanOut => Decision.ShardIds.Count > 1;

        /// <inheritdoc/>
        public override string ToString()
            => $"QueryPlan(entity={Intent.EntityName}; shards={Decision.ShardIds.Count}" +
               $"; merge={Merge.Operation}; broadcastJoin={BroadcastJoinApplied})";
    }
}
