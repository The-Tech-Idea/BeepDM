using System;
using TheTechIdea.Beep.Distributed.Placement;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Detects the narrow Phase 08 "broadcast-side join" case: a
    /// query that joins a sharded entity with a broadcast entity can
    /// run entirely inside each shard because every shard holds a
    /// complete copy of the broadcast table. The rewriter flags those
    /// intents so the planner can leave them alone instead of
    /// rejecting them as an unsupported cross-shard join.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rewriter is intentionally conservative in v1:
    /// <list type="bullet">
    ///   <item>It inspects only one "other" entity name supplied by
    ///   the caller (the typical hand-authored join has a single
    ///   driving table).</item>
    ///   <item>It rewrites nothing inside the intent text / filters;
    ///   the expectation is that each shard's data source already
    ///   executes the join locally against its broadcast replica.</item>
    ///   <item>Full distributed joins between two sharded entities
    ///   are out of scope; a future phase will add a broadcast-hash
    ///   rewriter.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class BroadcastJoinRewriter
    {
        private readonly EntityPlacementResolver _resolver;

        /// <summary>
        /// Creates a rewriter that queries <paramref name="resolver"/>
        /// to classify sibling entities as broadcast or sharded.
        /// </summary>
        public BroadcastJoinRewriter(EntityPlacementResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <summary>
        /// Returns <c>true</c> when a join between <paramref name="primaryEntityName"/>
        /// and <paramref name="joinedEntityName"/> can run locally on
        /// each shard because at least one side is broadcast.
        /// </summary>
        /// <param name="primaryEntityName">Entity driving the query (usually sharded).</param>
        /// <param name="joinedEntityName">Entity being joined in.</param>
        /// <returns>
        /// <c>true</c> when a rewrite is safe — the caller should
        /// fan out the join to each shard without any cross-shard
        /// coordination. <c>false</c> when both sides are sharded
        /// (v1 cannot plan that case) or both are broadcast (trivial,
        /// pick either shard).
        /// </returns>
        public bool CanRewrite(string primaryEntityName, string joinedEntityName)
        {
            if (string.IsNullOrWhiteSpace(primaryEntityName) ||
                string.IsNullOrWhiteSpace(joinedEntityName))
            {
                return false;
            }

            var primary = SafeResolve(primaryEntityName);
            var joined  = SafeResolve(joinedEntityName);

            if (primary == null || joined == null) return false;

            // At least one side must be Broadcast for the rewrite to be safe.
            return primary.Mode == DistributionMode.Broadcast
                || joined.Mode  == DistributionMode.Broadcast;
        }

        /// <summary>
        /// Inspects <paramref name="intent"/> against the
        /// <paramref name="joinedEntityName"/> hint and returns a
        /// <see cref="BroadcastJoinDecision"/> describing whether
        /// the caller can continue without a distributed-join
        /// rewrite.
        /// </summary>
        /// <param name="intent">Primary intent (drives the query).</param>
        /// <param name="joinedEntityName">Optional sibling joined entity.</param>
        public BroadcastJoinDecision Rewrite(QueryIntent intent, string joinedEntityName)
        {
            if (intent == null) throw new ArgumentNullException(nameof(intent));

            if (string.IsNullOrWhiteSpace(joinedEntityName))
            {
                return BroadcastJoinDecision.NotApplicable;
            }

            return CanRewrite(intent.EntityName, joinedEntityName)
                ? BroadcastJoinDecision.LocalJoin
                : BroadcastJoinDecision.RequiresDistributedJoin;
        }

        private PlacementResolution SafeResolve(string entityName)
        {
            try
            {
                return _resolver.Resolve(entityName, isWrite: false);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Outcome classes produced by <see cref="BroadcastJoinRewriter.Rewrite"/>.
    /// </summary>
    public enum BroadcastJoinDecision
    {
        /// <summary>No join hint was supplied; behave as a single-entity read.</summary>
        NotApplicable = 0,

        /// <summary>Join can run locally on every shard.</summary>
        LocalJoin = 1,

        /// <summary>Two sharded entities — v1 cannot plan this; callers should surface a descriptive error.</summary>
        RequiresDistributedJoin = 2,
    }
}
