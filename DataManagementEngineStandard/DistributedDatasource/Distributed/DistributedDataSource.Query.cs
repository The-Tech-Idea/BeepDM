using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Distributed.Query;
using TheTechIdea.Beep.Distributed.Routing;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// <see cref="DistributedDataSource"/> partial — Phase 08 query
    /// planner entry points. Exposes the richer
    /// <see cref="QueryIntent"/>-based read API without disturbing
    /// the Phase 06 <c>IDataSource</c> stubs in
    /// <c>DistributedDataSource.Reads.cs</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Callers that want cross-shard <c>ORDER BY</c>, <c>TOP</c>,
    /// <c>GROUP BY</c>, or <c>AVG</c> semantics should build a
    /// <see cref="QueryIntent"/> and call
    /// <see cref="ExecuteQueryIntent(QueryIntent, Func{IProxyCluster, QueryIntent, IEnumerable{object}})"/>.
    /// The lower-level <c>GetEntity</c> / <c>RunQuery</c> calls in
    /// <c>DistributedDataSource.Reads.cs</c> continue to use the
    /// Phase 06 union-merge path for backward compatibility.
    /// </para>
    /// <para>
    /// A caller-supplied <em>shard executor</em> delegate converts
    /// the per-shard sub-intent into an <see cref="IProxyCluster"/>
    /// call (for example, by rendering the intent to shard-native
    /// SQL). Callers that do not need aggregate pushdown can use
    /// <see cref="ExecuteQueryIntent(QueryIntent)"/>, which dispatches
    /// each shard's sub-intent via
    /// <see cref="IProxyCluster.GetEntity(string, List{Report.AppFilter})"/>.
    /// </para>
    /// </remarks>
    public partial class DistributedDataSource
    {
        /// <summary>Active query planner. <c>set</c> is exposed so tests/apps can swap the implementation.</summary>
        public IQueryPlanner QueryPlanner
        {
            get => _queryPlanner;
            set => _queryPlanner = value ?? Query.QueryPlanner.Instance;
        }

        /// <summary>Active query-aware result merger.</summary>
        public IQueryAwareResultMerger QueryMerger
        {
            get => _queryMerger;
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _queryMerger  = value;
                _resultMerger = value;
            }
        }

        /// <summary>
        /// Plans <paramref name="intent"/> against the current
        /// routing decision but does not execute it. Callers can
        /// inspect the plan to log diagnostics or customise the
        /// per-shard execution strategy.
        /// </summary>
        public QueryPlan PlanQuery(QueryIntent intent)
        {
            ThrowIfDisposed();
            if (intent == null) throw new ArgumentNullException(nameof(intent));

            var ctx      = DistributedExecutionContext.New("PlanQuery", intent.EntityName, isWrite: false);
            var router   = SnapshotRouter();
            var decision = router.RouteRead(
                entityName: intent.EntityName,
                filters:    intent.Filters?.ToList(),
                structure:  null,
                context:    ctx);

            EmitDecisionSelected(decision, ctx);
            return _queryPlanner.Plan(intent, decision);
        }

        /// <summary>
        /// Executes <paramref name="intent"/> using a default shard
        /// executor that calls
        /// <see cref="IProxyCluster.GetEntity(string, List{Report.AppFilter})"/>
        /// with the pushdown filters from the sub-intent. Aggregates
        /// are folded by the merger from the raw rows each shard
        /// returns.
        /// </summary>
        /// <remarks>
        /// The default executor is deliberately conservative: it
        /// pushes only filters to each shard, which means SUM / MAX /
        /// AVG are computed on the coordinator side from every
        /// candidate row. For large rowsets prefer the overload that
        /// accepts a custom shard executor so aggregates can run on
        /// each shard's native engine.
        /// </remarks>
        public IEnumerable<object> ExecuteQueryIntent(QueryIntent intent)
            => ExecuteQueryIntent(intent, DefaultShardExecutor);

        /// <summary>
        /// Executes <paramref name="intent"/> via a caller-supplied
        /// <paramref name="shardExecutor"/>. The executor is invoked
        /// once per target shard (in shard-id order) and must return
        /// the shard-local rows for the supplied sub-intent.
        /// </summary>
        public IEnumerable<object> ExecuteQueryIntent(
            QueryIntent                                                        intent,
            Func<IProxyCluster, QueryIntent, IEnumerable<object>>              shardExecutor)
        {
            ThrowIfDisposed();
            if (intent        == null) throw new ArgumentNullException(nameof(intent));
            if (shardExecutor == null) throw new ArgumentNullException(nameof(shardExecutor));

            var plan = PlanQuery(intent);
            return ExecutePlanInternal(plan, shardExecutor);
        }

        /// <summary>
        /// Lower-level overload that runs an already-built
        /// <paramref name="plan"/> against the current shard map.
        /// Useful when callers need to materialise / cache the plan
        /// between invocations.
        /// </summary>
        public IEnumerable<object> ExecutePlan(
            QueryPlan                                                          plan,
            Func<IProxyCluster, QueryIntent, IEnumerable<object>>              shardExecutor)
        {
            ThrowIfDisposed();
            if (plan          == null) throw new ArgumentNullException(nameof(plan));
            if (shardExecutor == null) throw new ArgumentNullException(nameof(shardExecutor));

            return ExecutePlanInternal(plan, shardExecutor);
        }

        // ── Internals ─────────────────────────────────────────────────────

        private IEnumerable<object> ExecutePlanInternal(
            QueryPlan                                                          plan,
            Func<IProxyCluster, QueryIntent, IEnumerable<object>>              shardExecutor)
        {
            // Single-shard plan → pass the full intent straight through.
            if (plan.IsSingleShard)
            {
                var shardId = plan.TargetShardIds[0];
                var cluster = ResolveCluster(shardId);
                var subIntent = plan.PerShardIntents[shardId];
                var rows      = SafeShardInvoke(cluster, subIntent, shardExecutor);
                return rows ?? Array.Empty<object>();
            }

            var perShard = new IEnumerable<object>[plan.TargetShardIds.Count];
            for (int i = 0; i < plan.TargetShardIds.Count; i++)
            {
                var shardId = plan.TargetShardIds[i];
                if (!_shards.TryGetValue(shardId, out var cluster) || cluster == null)
                {
                    perShard[i] = null;
                    continue;
                }

                plan.PerShardIntents.TryGetValue(shardId, out var subIntent);
                perShard[i] = SafeShardInvoke(cluster, subIntent ?? plan.Intent, shardExecutor);
            }

            return _queryMerger.MergePlan(plan, plan.Decision, perShard);
        }

        private IProxyCluster ResolveCluster(string shardId)
        {
            if (!_shards.TryGetValue(shardId, out var cluster) || cluster == null)
            {
                throw new InvalidOperationException(
                    $"Distributed query plan targets shard '{shardId}' but the shard is no longer live.");
            }
            return cluster;
        }

        private static IEnumerable<object> SafeShardInvoke(
            IProxyCluster                                                      cluster,
            QueryIntent                                                        subIntent,
            Func<IProxyCluster, QueryIntent, IEnumerable<object>>              shardExecutor)
        {
            try
            {
                return shardExecutor(cluster, subIntent) ?? Array.Empty<object>();
            }
            catch (Exception)
            {
                // A single shard failing should not collapse the whole
                // merge — the executor will see a null entry and the
                // merger treats missing shards as "no rows". Callers
                // that want strict semantics should use the executor
                // layer (Phase 06) which raises structured events.
                return null;
            }
        }

        private static IEnumerable<object> DefaultShardExecutor(IProxyCluster cluster, QueryIntent intent)
        {
            if (cluster == null || intent == null) return Array.Empty<object>();

            var filters = intent.Filters?.ToList();
            return cluster.GetEntity(intent.EntityName, filters) ?? Array.Empty<object>();
        }
    }
}
