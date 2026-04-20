using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Phase 11 resharding orchestrator. Exposes the four topology
    /// primitives — add shard, remove shard, move entity, repartition
    /// entity — plus a plan-apply helper that translates a
    /// <see cref="PlanDiff"/> into concrete migration calls.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every primitive follows the same Shadow → DualWrite → Cutover
    /// → Off window lifecycle:
    /// </para>
    /// <list type="number">
    ///   <item><description>Register a <see cref="DualWriteWindow"/> in <see cref="DualWriteState.Shadow"/>.</description></item>
    ///   <item><description>Transition to <see cref="DualWriteState.DualWrite"/> and copy rows via <see cref="IEntityCopyService"/>.</description></item>
    ///   <item><description>Transition to <see cref="DualWriteState.Cutover"/>, swap the plan, and unregister the window.</description></item>
    ///   <item><description>Close the window (Off) and drop any source-side state when applicable.</description></item>
    /// </list>
    /// <para>
    /// Every method is cancellable. On cancel the service forces the
    /// active window to <see cref="DualWriteState.Off"/>, leaves the
    /// source plan untouched, and returns a
    /// <see cref="ReshardOutcome"/> with <see cref="ReshardOutcome.Cancelled"/>
    /// set to <c>true</c>.
    /// </para>
    /// </remarks>
    public interface IReshardingService
    {
        /// <summary>Active dual-write coordinator; never <c>null</c>.</summary>
        IDualWriteCoordinator DualWrites { get; }

        /// <summary>Active copy service; never <c>null</c>.</summary>
        IEntityCopyService CopyService { get; }

        /// <summary>Adds a new shard to the catalog without moving data.</summary>
        /// <param name="spec">Shard specification. Required.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<ReshardOutcome> AddShardAsync(ShardSpec spec, CancellationToken cancellationToken = default);

        /// <summary>Drains a shard and removes it from the catalog.</summary>
        /// <param name="plan">Drain plan (shard id + explicit redirects or fallback targets).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<ReshardOutcome> RemoveShardAsync(RemoveShardPlan plan, CancellationToken cancellationToken = default);

        /// <summary>Moves a single <see cref="DistributionMode.Routed"/> entity from one shard to another.</summary>
        /// <param name="entityName">Entity to move.</param>
        /// <param name="fromShard">Source shard id.</param>
        /// <param name="toShard">Target shard id.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<ReshardOutcome> MoveEntityAsync(
            string            entityName,
            string            fromShard,
            string            toShard,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Repartitions a sharded entity using a new partition
        /// function and/or a new shard set. Dual-writes cover every
        /// old → new shard pair in parallel.
        /// </summary>
        Task<ReshardOutcome> RepartitionEntityAsync(
            string                entityName,
            PartitionFunctionRef  newFunction,
            IReadOnlyList<string> newShardIds,
            CancellationToken     cancellationToken = default);

        /// <summary>
        /// Computes <see cref="PlanDiff"/> between the active plan
        /// and <paramref name="targetPlan"/>, then executes each
        /// diff entry sequentially. Returns a combined outcome.
        /// </summary>
        Task<ReshardOutcome> ApplyPlanAsync(DistributionPlan targetPlan, CancellationToken cancellationToken = default);
    }
}
