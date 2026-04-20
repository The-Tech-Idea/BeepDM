using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Distributed.Routing;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// Turns a <see cref="RoutingDecision"/> into the physical read
    /// calls against one or more <see cref="IProxyCluster"/> shards
    /// and merges the per-shard results into a single logical result
    /// for <see cref="IDataSource"/> consumers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The executor never calls the router itself. Callers
    /// (<see cref="DistributedDataSource"/>) resolve routing first,
    /// then hand the decision here. This separation makes the
    /// executor trivially testable against a fake
    /// <see cref="IShardInvoker"/> without wiring a real plan.
    /// </para>
    /// <para>
    /// Phase 06 ships only the read path. Writes live in Phase 07's
    /// <c>IDistributedWriteExecutor</c>; transactions in Phase 09.
    /// </para>
    /// </remarks>
    public interface IDistributedReadExecutor
    {
        /// <summary>
        /// Executes the supplied <paramref name="readOperation"/>
        /// against a single shard (<see cref="RoutingDecision.ShardIds"/>
        /// must contain exactly one id) and returns the raw result.
        /// Per-shard exceptions propagate.
        /// </summary>
        T ExecuteSingleShard<T>(
            RoutingDecision                decision,
            Func<IProxyCluster, T>         readOperation,
            DistributedExecutionContext    ctx);

        /// <summary>
        /// Executes the supplied <paramref name="readOperation"/>
        /// against a single shard and returns the raw result. Async
        /// variant for <c>*Async</c> <see cref="IDataSource"/> calls.
        /// </summary>
        Task<T> ExecuteSingleShardAsync<T>(
            RoutingDecision                         decision,
            Func<IProxyCluster, CancellationToken, Task<T>> readOperation,
            DistributedExecutionContext             ctx,
            CancellationToken                       cancellationToken = default);

        /// <summary>
        /// Fans <paramref name="readOperation"/> out across every shard
        /// in <paramref name="decision"/>, applies the configured
        /// <see cref="ScatterFailurePolicy"/>, and hands the per-shard
        /// results to <see cref="IResultMerger.MergeRows"/>.
        /// </summary>
        IEnumerable<object> ExecuteScatterRows(
            RoutingDecision                              decision,
            Func<IProxyCluster, IEnumerable<object>>     readOperation,
            DistributedExecutionContext                  ctx);

        /// <summary>
        /// Async row scatter; cancellation propagates to every shard
        /// call and the total deadline enforces
        /// <see cref="DistributedDataSourceOptions.DefaultReadDeadlineMs"/>.
        /// </summary>
        Task<IEnumerable<object>> ExecuteScatterRowsAsync(
            RoutingDecision                                                        decision,
            Func<IProxyCluster, CancellationToken, Task<IEnumerable<object>>>      readOperation,
            DistributedExecutionContext                                            ctx,
            CancellationToken                                                      cancellationToken = default);

        /// <summary>
        /// Fans a scalar read out and merges the results via
        /// <see cref="IResultMerger.MergeScalar"/>.
        /// </summary>
        double ExecuteScatterScalar(
            RoutingDecision                     decision,
            Func<IProxyCluster, double>         readOperation,
            DistributedExecutionContext         ctx);

        /// <summary>
        /// Async scalar scatter.
        /// </summary>
        Task<double> ExecuteScatterScalarAsync(
            RoutingDecision                                                 decision,
            Func<IProxyCluster, CancellationToken, Task<double>>            readOperation,
            DistributedExecutionContext                                     ctx,
            CancellationToken                                               cancellationToken = default);

        /// <summary>
        /// Fans a paged read out and merges the results via
        /// <see cref="IResultMerger.MergePaged"/>.
        /// </summary>
        PagedResult ExecuteScatterPaged(
            RoutingDecision                                          decision,
            Func<IProxyCluster, PagedResult>                         readOperation,
            int                                                      pageNumber,
            int                                                      pageSize,
            DistributedExecutionContext                              ctx);

        /// <summary>
        /// Executes a replicated read by picking one shard via
        /// <see cref="DistributedDataSourceOptions.ReplicatedReadPolicy"/>
        /// and falling over to the next live shard on exception.
        /// </summary>
        T ExecuteReplicatedRead<T>(
            RoutingDecision                decision,
            Func<IProxyCluster, T>         readOperation,
            DistributedExecutionContext    ctx);

        /// <summary>
        /// Async replicated read with failover.
        /// </summary>
        Task<T> ExecuteReplicatedReadAsync<T>(
            RoutingDecision                                     decision,
            Func<IProxyCluster, CancellationToken, Task<T>>     readOperation,
            DistributedExecutionContext                         ctx,
            CancellationToken                                   cancellationToken = default);
    }

    /// <summary>
    /// Thin abstraction over the shard map so the executor can be
    /// unit-tested without a real <see cref="DistributedDataSource"/>.
    /// </summary>
    public interface IShardInvoker
    {
        /// <summary>Resolves a shard id to its <see cref="IProxyCluster"/>.</summary>
        /// <returns><c>null</c> when the shard id has been unregistered.</returns>
        IProxyCluster GetShard(string shardId);

        /// <summary>
        /// Raises the <see cref="DistributedDataSource.OnShardSelected"/>
        /// event (or equivalent) so the executor reports the actual
        /// shard set used after scatter / replication decisions.
        /// </summary>
        void NotifyShardSelected(
            string                      entityName,
            string                      shardId,
            string                      operation,
            object                      partitionKey,
            string                      reason);

        /// <summary>
        /// Reports a non-fatal per-shard failure via
        /// <see cref="IDataSource.PassEvent"/> so best-effort drops
        /// are still visible to operators.
        /// </summary>
        void NotifyShardFailure(
            string                      entityName,
            string                      shardId,
            string                      operation,
            Exception                   exception);

        // ── Phase 10 resilience hooks (default no-ops) ────────────────────

        /// <summary>
        /// Phase 10 hook. Returns <c>true</c> when <paramref name="shardId"/>
        /// should be considered available for dispatch. The default
        /// implementation always returns <c>true</c>, preserving Phase
        /// 06 / 07 behaviour for callers that do not wire a resilience
        /// adapter.
        /// </summary>
        bool IsShardHealthy(string shardId) => true;

        /// <summary>
        /// Phase 10 hook. Returns the subset of
        /// <paramref name="shardIds"/> considered healthy for dispatch.
        /// The default implementation returns the input as-is so
        /// existing callers see no behaviour change.
        /// </summary>
        IReadOnlyList<string> FilterHealthyShards(IReadOnlyList<string> shardIds)
            => shardIds ?? Array.Empty<string>();

        /// <summary>
        /// Phase 10 hook. Evaluates the
        /// <see cref="DistributedDataSourceOptions.MinimumHealthyShardRatio"/>
        /// gate. Returns <c>null</c> when the scatter call may
        /// proceed; otherwise returns a ready-to-throw
        /// <see cref="Resilience.DegradedShardSetException"/>. Default
        /// always returns <c>null</c>.
        /// </summary>
        Resilience.DegradedShardSetException EvaluateScatterGate(
            IReadOnlyList<string> attemptedShardIds,
            IReadOnlyList<string> healthyShardIds)
            => null;

        /// <summary>
        /// Phase 10 hook. Records a successful per-shard call so the
        /// distribution tier's health monitor and distributed circuit
        /// breaker can flip the shard back to healthy / reset the
        /// failure counter. Default is a no-op.
        /// </summary>
        void NotifyShardSuccess(
            string shardId,
            double latencyMs) { }

        /// <summary>
        /// Phase 10 hook. Raises
        /// <see cref="DistributedDataSource.OnPartialBroadcast"/> when
        /// a broadcast / replicated fan-out excluded unhealthy shards
        /// but still satisfied its quorum. Default is a no-op.
        /// </summary>
        void NotifyPartialBroadcast(
            string                entityName,
            string                operation,
            IReadOnlyList<string> attemptedShardIds,
            IReadOnlyList<string> skippedShardIds,
            string                reason) { }

        // ── Phase 14 capacity hooks (default pass-through) ────────────────

        /// <summary>
        /// Phase 14 hook. Acquires the datasource-wide concurrency
        /// permit. The returned <see cref="IDisposable"/> must be
        /// disposed in a <c>finally</c> block to release the slot.
        /// Default returns a no-op permit. Implementations may throw
        /// <see cref="Performance.BackpressureException"/>.
        /// </summary>
        IDisposable AcquireDistributedCallPermit(System.Threading.CancellationToken cancellationToken = default)
            => NullDisposable.Instance;

        /// <summary>
        /// Phase 14 hook. Acquires a per-shard permit and consumes
        /// one rate-limiter token. Default returns a no-op permit.
        /// </summary>
        IDisposable AcquireShardCallPermit(string shardId, System.Threading.CancellationToken cancellationToken = default)
            => NullDisposable.Instance;

        /// <summary>
        /// Phase 14 hook. Drops hot shards from
        /// <paramref name="candidates"/> when the placement allows
        /// read shedding (Replicated / Broadcast). Default returns
        /// the candidates unchanged.
        /// </summary>
        IReadOnlyList<string> ShedHotShards(
            Routing.RoutingDecision decision,
            IReadOnlyList<string>   candidates)
            => candidates;

        /// <summary>
        /// Phase 14 hook. Returns an adaptive deadline (ms) for a
        /// per-shard call, blending the caller's <paramref name="fallbackMs"/>
        /// with the shard's observed p95. Default returns
        /// <paramref name="fallbackMs"/> unchanged.
        /// </summary>
        int ComputeShardDeadlineMs(string shardId, int fallbackMs) => fallbackMs;
    }

    /// <summary>
    /// Sentinel <see cref="IDisposable"/> returned by capacity hooks
    /// that are not wired; disposing it is a no-op.
    /// </summary>
    internal sealed class NullDisposable : IDisposable
    {
        internal static readonly NullDisposable Instance = new NullDisposable();
        public void Dispose() { }
    }
}
