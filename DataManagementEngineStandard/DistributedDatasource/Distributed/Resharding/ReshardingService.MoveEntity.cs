using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// <see cref="ReshardingService"/> partial — <c>MoveEntityAsync</c>.
    /// Moves a single <see cref="DistributionMode.Routed"/> entity
    /// from one shard to another using the Shadow → DualWrite →
    /// Cutover window protocol.
    /// </summary>
    public sealed partial class ReshardingService
    {
        /// <inheritdoc/>
        public async Task<ReshardOutcome> MoveEntityAsync(
            string            entityName,
            string            fromShard,
            string            toShard,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(entityName)) throw new ArgumentException("Entity name required.", nameof(entityName));
            if (string.IsNullOrWhiteSpace(fromShard))  throw new ArgumentException("Source shard required.", nameof(fromShard));
            if (string.IsNullOrWhiteSpace(toShard))    throw new ArgumentException("Target shard required.", nameof(toShard));
            if (string.Equals(fromShard, toShard, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Source and target shards must differ.", nameof(toShard));

            EnsureNoActiveWindow(entityName);

            var reshardId   = NewReshardId("MoveEntity");
            var currentPlan = _getCurrentPlan();
            _raiseStarted(reshardId, $"MoveEntity '{entityName}' {fromShard} -> {toShard}", 1);

            var copyResults = new List<CopyResult>();

            try
            {
                var legs = await MoveEntityInternalAsync(reshardId, entityName, fromShard, toShard, cancellationToken).ConfigureAwait(false);
                copyResults.AddRange(legs);

                var newPlan = BuildMovedPlan(currentPlan, entityName, fromShard, toShard);
                _applyPlan(newPlan);
                _raiseCompleted(reshardId, $"MoveEntity '{entityName}' completed", 1, null);
                return SuccessOutcome(reshardId, "MoveEntity", newPlan.Version, copyResults);
            }
            catch (OperationCanceledException)
            {
                var window = DualWrites.TryGetWindow(entityName);
                window?.ForceOff();
                DualWrites.Unregister(entityName);
                _raiseCompleted(reshardId, $"MoveEntity '{entityName}' cancelled", 1, null);
                return CancelledOutcome(reshardId, "MoveEntity", currentPlan?.Version ?? 0, copyResults);
            }
            catch (Exception ex)
            {
                var window = DualWrites.TryGetWindow(entityName);
                window?.ForceOff();
                DualWrites.Unregister(entityName);
                _raiseCompleted(reshardId, $"MoveEntity '{entityName}' failed: {ex.Message}", 1, ex);
                return FailureOutcome(reshardId, "MoveEntity", currentPlan?.Version ?? 0, copyResults, ex);
            }
        }

        private async Task<IReadOnlyList<CopyResult>> MoveEntityInternalAsync(
            string            reshardId,
            string            entityName,
            string            fromShard,
            string            toShard,
            CancellationToken cancellationToken)
        {
            // Ensure source/target shards are resolvable before touching window state.
            ResolveOrThrow(fromShard, "source");
            ResolveOrThrow(toShard,   "target");

            var window = new DualWriteWindow(
                reshardId:      reshardId,
                entityName:     entityName,
                sourceShardIds: new[] { fromShard },
                targetShardIds: new[] { toShard });

            // Shadow → DualWrite → (copy) → Cutover → Off
            DualWrites.Register(window);
            try
            {
                window.TransitionTo(DualWriteState.DualWrite);

                var leg = await RunLegAsync(reshardId, entityName, fromShard, toShard, cancellationToken).ConfigureAwait(false);
                if (!leg.Success)
                {
                    throw leg.Error
                        ?? new InvalidOperationException($"Copy leg failed for entity '{entityName}' ({fromShard} -> {toShard}).");
                }

                window.TransitionTo(DualWriteState.Cutover);
                // Plan swap happens in the public wrapper; this helper only touches window state.
                window.TransitionTo(DualWriteState.Off);
                return new[] { leg };
            }
            finally
            {
                DualWrites.Unregister(entityName);
            }
        }

        private static DistributionPlan BuildMovedPlan(
            DistributionPlan currentPlan,
            string           entityName,
            string           fromShard,
            string           toShard)
        {
            currentPlan = currentPlan ?? new DistributionPlan("default");
            var placement = new EntityPlacement(
                entityName:        entityName,
                mode:              DistributionMode.Routed,
                shardIds:          new[] { toShard },
                partitionFunction: PartitionFunctionRef.None);
            return currentPlan.WithEntity(placement);
        }
    }
}
