using System;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Distributed.Routing;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// Turns a write-side <see cref="RoutingDecision"/> into the
    /// physical <see cref="IProxyCluster"/> write calls, applies the
    /// quorum policy, and produces a <see cref="WriteOutcome"/> the
    /// datasource can convert into <see cref="IErrorsInfo"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike the Phase 06 read executor the write executor does not
    /// throw on per-shard failure — every outcome is collected and
    /// reported via <see cref="WriteOutcome"/>. The datasource inspects
    /// <see cref="WriteOutcome.QuorumSatisfied"/> and
    /// <see cref="WriteOutcome.IsPartial"/> to decide between
    /// <c>Errors.Ok</c>, partial success (event-only), and
    /// <c>Errors.Failed</c>.
    /// </para>
    /// <para>
    /// The executor intentionally does NOT know about
    /// <see cref="IDataSource"/> write methods directly. Callers pass
    /// a <see cref="Func{IProxyCluster, IErrorsInfo}"/> describing the
    /// per-shard operation; this keeps the executor usable from
    /// future Phase 12 DDL broadcast paths without API churn.
    /// </para>
    /// </remarks>
    public interface IDistributedWriteExecutor
    {
        /// <summary>
        /// Executes the supplied <paramref name="writeOperation"/>
        /// against the exactly one shard named in
        /// <paramref name="decision"/>. Quorum is implicitly
        /// <c>AtLeastN=1</c>; the returned outcome carries one leg.
        /// </summary>
        WriteOutcome ExecuteSingleShard(
            RoutingDecision                     decision,
            Func<IProxyCluster, IErrorsInfo>    writeOperation,
            DistributedExecutionContext         ctx);

        /// <summary>
        /// Fans <paramref name="writeOperation"/> out across every
        /// shard in <paramref name="decision"/> using
        /// <see cref="DistributedDataSourceOptions.MaxFanOutParallelism"/>.
        /// Applies the effective quorum policy (derived from the
        /// decision + options + caller override) to produce a
        /// <see cref="WriteOutcome"/>. Typical caller: replicated or
        /// broadcast writes.
        /// </summary>
        WriteOutcome ExecuteFanOut(
            RoutingDecision                     decision,
            Func<IProxyCluster, IErrorsInfo>    writeOperation,
            DistributedExecutionContext         ctx,
            DistributedWriteOptions             options = null);

        /// <summary>
        /// Executes a sharded-scatter write (e.g. delete-by-filter)
        /// across every shard in <paramref name="decision"/>. Requires
        /// <see cref="DistributedWriteOptions.AllowScatterWrite"/> (or
        /// the datasource-wide flag) to be <c>true</c>; otherwise
        /// throws <see cref="ShardRoutingException"/>. Quorum is
        /// always <c>All</c> for scatter writes.
        /// </summary>
        WriteOutcome ExecuteScatter(
            RoutingDecision                     decision,
            Func<IProxyCluster, IErrorsInfo>    writeOperation,
            DistributedExecutionContext         ctx,
            DistributedWriteOptions             options);
    }
}
