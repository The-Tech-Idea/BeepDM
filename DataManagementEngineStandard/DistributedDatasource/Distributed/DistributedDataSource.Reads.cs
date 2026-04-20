using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Distributed.Execution;
using TheTechIdea.Beep.Distributed.Placement;
using TheTechIdea.Beep.Distributed.Routing;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// <see cref="DistributedDataSource"/> partial — Phase 06 read
    /// dispatch. Each <see cref="IDataSource"/> read method builds a
    /// <see cref="RoutingDecision"/> via the Phase 05 router, hands
    /// the decision to the <see cref="IDistributedReadExecutor"/>,
    /// and returns the merged result.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dispatch rules:
    /// </para>
    /// <list type="bullet">
    ///   <item><c>GetEntity(name, filters)</c> — routes via
    ///   <see cref="IShardRouter.RouteRead(string,System.Collections.Generic.List{AppFilter},DataBase.EntityStructure,DistributedExecutionContext)"/>.
    ///   Single-shard, scatter, or replicated path is chosen from the decision flags.</item>
    ///   <item><c>RunQuery(sql)</c> — has no parsed filters in Phase 06,
    ///   so it always routes by entity metadata (broadcast / replicated /
    ///   scatter). SQL-parsed key extraction is Phase 08.</item>
    ///   <item><c>GetScalar(sql)</c> — same policy as <c>RunQuery</c>; the
    ///   basic merger sums per-shard scalars, matching the typical
    ///   <c>COUNT</c> / <c>SUM</c> use-case.</item>
    /// </list>
    /// <para>
    /// <c>RunQuery</c> / <c>GetScalar</c> currently require the caller
    /// to tag the originating entity via
    /// <see cref="DistributedDataSourceOptions"/> or via a separate
    /// routing hook. Without an entity hint the executor falls back to
    /// a broadcast across every live shard, which matches the "safe
    /// default" Phase 06 ships.
    /// </para>
    /// </remarks>
    public partial class DistributedDataSource
    {
        // ── GetEntity (non-paged) ─────────────────────────────────────────

        /// <inheritdoc/>
        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            ThrowIfDisposed();
            var ctx      = DistributedExecutionContext.New("GetEntity", EntityName, isWrite: false);
            var decision = RouteForRead(EntityName, filter, ctx);
            return DispatchRead(
                decision: decision,
                ctx:      ctx,
                readOp:   cluster => cluster.GetEntity(EntityName, filter));
        }

        // ── GetEntity (paged) ─────────────────────────────────────────────

        /// <inheritdoc/>
        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            ThrowIfDisposed();
            var ctx      = DistributedExecutionContext.New("GetEntity(paged)", EntityName, isWrite: false);
            var decision = RouteForRead(EntityName, filter, ctx);

            if (IsSingleShard(decision))
            {
                return _readExecutor.ExecuteSingleShard(
                    decision,
                    cluster => cluster.GetEntity(EntityName, filter, pageNumber, pageSize),
                    ctx);
            }

            if (IsReplicatedNonScatter(decision))
            {
                return _readExecutor.ExecuteReplicatedRead(
                    decision,
                    cluster => cluster.GetEntity(EntityName, filter, pageNumber, pageSize),
                    ctx);
            }

            return _readExecutor.ExecuteScatterPaged(
                decision,
                cluster => cluster.GetEntity(EntityName, filter, pageNumber, pageSize),
                pageNumber,
                pageSize,
                ctx);
        }

        // ── GetEntityAsync ────────────────────────────────────────────────

        /// <inheritdoc/>
        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            ThrowIfDisposed();
            var ctx      = DistributedExecutionContext.New("GetEntityAsync", EntityName, isWrite: false);
            var decision = RouteForRead(EntityName, Filter, ctx);
            return DispatchReadAsync(
                decision,
                ctx,
                (cluster, _) => cluster.GetEntityAsync(EntityName, Filter));
        }

        // ── RunQuery ──────────────────────────────────────────────────────

        /// <inheritdoc/>
        public IEnumerable<object> RunQuery(string qrystr)
        {
            ThrowIfDisposed();
            var ctx      = DistributedExecutionContext.New("RunQuery", entityName: null, isWrite: false);
            var decision = BuildBroadcastDecision(ctx.OperationName);
            return DispatchRead(
                decision: decision,
                ctx:      ctx,
                readOp:   cluster => cluster.RunQuery(qrystr));
        }

        // ── GetScalar ─────────────────────────────────────────────────────

        /// <inheritdoc/>
        public double GetScalar(string query)
        {
            ThrowIfDisposed();
            var ctx      = DistributedExecutionContext.New("GetScalar", entityName: null, isWrite: false);
            var decision = BuildBroadcastDecision(ctx.OperationName);

            if (IsSingleShard(decision))
            {
                return _readExecutor.ExecuteSingleShard(decision, c => c.GetScalar(query), ctx);
            }

            return _readExecutor.ExecuteScatterScalar(decision, c => c.GetScalar(query), ctx);
        }

        /// <inheritdoc/>
        public Task<double> GetScalarAsync(string query)
        {
            ThrowIfDisposed();
            var ctx      = DistributedExecutionContext.New("GetScalarAsync", entityName: null, isWrite: false);
            var decision = BuildBroadcastDecision(ctx.OperationName);

            if (IsSingleShard(decision))
            {
                return _readExecutor.ExecuteSingleShardAsync(
                    decision,
                    (c, ct) => c.GetScalarAsync(query),
                    ctx);
            }

            return _readExecutor.ExecuteScatterScalarAsync(
                decision,
                (c, ct) => c.GetScalarAsync(query),
                ctx);
        }

        // ── Dispatch helpers ──────────────────────────────────────────────

        /// <summary>
        /// Picks the correct executor entry-point
        /// (single-shard / scatter / replicated) for a row-returning
        /// synchronous read based on <paramref name="decision"/>.
        /// </summary>
        private IEnumerable<object> DispatchRead(
            RoutingDecision                                 decision,
            DistributedExecutionContext                     ctx,
            Func<Proxy.IProxyCluster, IEnumerable<object>>  readOp)
        {
            if (IsSingleShard(decision))
            {
                return _readExecutor.ExecuteSingleShard(decision, readOp, ctx);
            }
            if (IsReplicatedNonScatter(decision))
            {
                return _readExecutor.ExecuteReplicatedRead(decision, readOp, ctx);
            }
            return _readExecutor.ExecuteScatterRows(decision, readOp, ctx);
        }

        /// <summary>
        /// Async counterpart of <see cref="DispatchRead"/>.
        /// </summary>
        private Task<IEnumerable<object>> DispatchReadAsync(
            RoutingDecision                                                                  decision,
            DistributedExecutionContext                                                      ctx,
            Func<Proxy.IProxyCluster, System.Threading.CancellationToken, Task<IEnumerable<object>>> readOp)
        {
            if (IsSingleShard(decision))
            {
                return _readExecutor.ExecuteSingleShardAsync(decision, readOp, ctx);
            }
            if (IsReplicatedNonScatter(decision))
            {
                return _readExecutor.ExecuteReplicatedReadAsync(decision, readOp, ctx);
            }
            return _readExecutor.ExecuteScatterRowsAsync(decision, readOp, ctx);
        }

        /// <summary>
        /// Routes a read via <see cref="SnapshotRouter"/> and emits
        /// <see cref="OnShardSelected"/> for every targeted shard.
        /// </summary>
        private RoutingDecision RouteForRead(
            string                      entityName,
            List<AppFilter>             filter,
            DistributedExecutionContext ctx)
        {
            EnsureAccess(entityName, Security.DistributedAccessKind.Read, ResolvePrincipal(ctx));
            var router   = SnapshotRouter();
            var decision = router.RouteRead(entityName, filter, structure: null, context: ctx);
            EmitDecisionSelected(decision, ctx);
            return decision;
        }

        /// <summary>
        /// Builds a synthetic broadcast-style decision for read APIs
        /// that do not carry an entity name (<c>RunQuery</c>,
        /// <c>GetScalar</c>). The decision targets every live shard
        /// and is never keyed; callers opt into strict behaviour via
        /// <see cref="DistributedDataSourceOptions.ScatterFailurePolicy"/>.
        /// </summary>
        private RoutingDecision BuildBroadcastDecision(string operation)
        {
            var live = SnapshotLiveShardIds();
            if (live.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Distributed {operation} has no live shards to dispatch to.");
            }

            return new RoutingDecision(
                entityName:        "(ad-hoc)",
                mode:              DistributionMode.Broadcast,
                matchKind:         PlacementMatchKind.Broadcast,
                shardIds:          live,
                isWrite:           false,
                isScatter:         live.Count > 1,
                isFanOut:          live.Count > 1,
                writeQuorum:       0,
                replicationFactor: 1,
                keyValues:         null,
                hookOverridden:    false,
                source:            null);
        }

        private static bool IsSingleShard(RoutingDecision decision)
            => decision.ShardIds.Count == 1 && !decision.IsScatter && !decision.IsFanOut;

        private static bool IsReplicatedNonScatter(RoutingDecision decision)
            => decision.Mode == DistributionMode.Replicated &&
               decision.ShardIds.Count > 1 &&
               !decision.IsScatter;
    }
}
