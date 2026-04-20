using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// <see cref="ReshardingService"/> partial —
    /// <c>RemoveShardAsync</c>. Drains the specified shard by moving
    /// every placement that references it to either an explicit
    /// redirect or the plan's fallback shard list, then unregisters
    /// the shard from the catalog.
    /// </summary>
    public sealed partial class ReshardingService
    {
        /// <inheritdoc/>
        public async Task<ReshardOutcome> RemoveShardAsync(
            RemoveShardPlan   plan,
            CancellationToken cancellationToken = default)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var reshardId   = NewReshardId("RemoveShard");
            var currentPlan = _getCurrentPlan() ?? new DistributionPlan("default");
            var affected    = currentPlan.EntityPlacements.Values.Where(e => ReferencesShard(e, plan.ShardId)).ToList();
            _raiseStarted(reshardId, $"RemoveShard '{plan.ShardId}' (draining {affected.Count} entities)", affected.Count);

            var copyResults  = new List<CopyResult>();
            var workingPlan  = currentPlan;

            try
            {
                foreach (var placement in affected)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var (legs, updatedPlan) = await DrainPlacementAsync(
                        reshardId,
                        workingPlan,
                        placement,
                        plan,
                        cancellationToken).ConfigureAwait(false);
                    copyResults.AddRange(legs);
                    workingPlan = updatedPlan;
                }

                if (!ReferenceEquals(workingPlan, currentPlan))
                {
                    _applyPlan(workingPlan);
                }

                if (plan.RequireDataDrained)
                {
                    var stillReferencing = workingPlan.EntityPlacements.Values.Where(e => ReferencesShard(e, plan.ShardId)).ToList();
                    if (stillReferencing.Count > 0)
                    {
                        var names = string.Join(",", stillReferencing.Select(e => e.EntityName));
                        throw new InvalidOperationException(
                            $"Cannot remove shard '{plan.ShardId}' — still referenced by: {names}.");
                    }
                }

                _unregisterShard(plan.ShardId);
                _raiseCompleted(reshardId, $"RemoveShard '{plan.ShardId}' completed", affected.Count, null);
                return SuccessOutcome(reshardId, "RemoveShard", workingPlan.Version, copyResults);
            }
            catch (OperationCanceledException)
            {
                _raiseCompleted(reshardId, $"RemoveShard '{plan.ShardId}' cancelled", affected.Count, null);
                return CancelledOutcome(reshardId, "RemoveShard", workingPlan.Version, copyResults);
            }
            catch (Exception ex)
            {
                _raiseCompleted(reshardId, $"RemoveShard '{plan.ShardId}' failed: {ex.Message}", affected.Count, ex);
                return FailureOutcome(reshardId, "RemoveShard", workingPlan.Version, copyResults, ex);
            }
        }

        private async Task<(IReadOnlyList<CopyResult> Legs, DistributionPlan UpdatedPlan)> DrainPlacementAsync(
            string            reshardId,
            DistributionPlan  plan,
            EntityPlacement   placement,
            RemoveShardPlan   removePlan,
            CancellationToken cancellationToken)
        {
            var targetShardId = ResolveRedirectTarget(placement, removePlan);
            if (string.IsNullOrWhiteSpace(targetShardId))
            {
                throw new InvalidOperationException(
                    $"No redirect target available for entity '{placement.EntityName}' while draining shard '{removePlan.ShardId}'.");
            }

            EnsureNoActiveWindow(placement.EntityName);
            ResolveOrThrow(removePlan.ShardId, "source");
            ResolveOrThrow(targetShardId,      "target");

            var window = new DualWriteWindow(
                reshardId:      reshardId,
                entityName:     placement.EntityName,
                sourceShardIds: new[] { removePlan.ShardId },
                targetShardIds: new[] { targetShardId });

            DualWrites.Register(window);
            try
            {
                window.TransitionTo(DualWriteState.DualWrite);
                var leg = await RunLegAsync(reshardId, placement.EntityName, removePlan.ShardId, targetShardId, cancellationToken)
                    .ConfigureAwait(false);
                if (!leg.Success)
                {
                    throw leg.Error
                        ?? new InvalidOperationException(
                               $"Drain copy failed for '{placement.EntityName}' ({removePlan.ShardId} -> {targetShardId}).");
                }

                var rewritten = ReplaceShardReference(placement, removePlan.ShardId, targetShardId);
                var updated   = plan.WithEntity(rewritten);

                window.TransitionTo(DualWriteState.Cutover);
                window.TransitionTo(DualWriteState.Off);
                return (new[] { leg }, updated);
            }
            finally
            {
                DualWrites.Unregister(placement.EntityName);
            }
        }

        private static string ResolveRedirectTarget(EntityPlacement placement, RemoveShardPlan removePlan)
        {
            if (removePlan.ExplicitRedirects != null
                && removePlan.ExplicitRedirects.TryGetValue(placement.EntityName, out var explicitTarget)
                && !string.IsNullOrWhiteSpace(explicitTarget))
            {
                return explicitTarget;
            }

            if (removePlan.FallbackTargetShardIds != null)
            {
                for (int i = 0; i < removePlan.FallbackTargetShardIds.Count; i++)
                {
                    var candidate = removePlan.FallbackTargetShardIds[i];
                    if (string.IsNullOrWhiteSpace(candidate)) continue;
                    if (string.Equals(candidate, removePlan.ShardId, StringComparison.OrdinalIgnoreCase)) continue;
                    if (placement.ShardIds.Any(s => string.Equals(s, candidate, StringComparison.OrdinalIgnoreCase))) continue;
                    return candidate;
                }
            }

            return null;
        }

        private static EntityPlacement ReplaceShardReference(
            EntityPlacement placement,
            string          removedShardId,
            string          replacementShardId)
        {
            var updatedShards = new List<string>(placement.ShardIds.Count);
            var appended      = false;
            for (int i = 0; i < placement.ShardIds.Count; i++)
            {
                var current = placement.ShardIds[i];
                if (string.Equals(current, removedShardId, StringComparison.OrdinalIgnoreCase))
                {
                    if (!updatedShards.Any(s => string.Equals(s, replacementShardId, StringComparison.OrdinalIgnoreCase)))
                    {
                        updatedShards.Add(replacementShardId);
                        appended = true;
                    }
                    continue;
                }
                updatedShards.Add(current);
            }

            if (!appended
                && !updatedShards.Any(s => string.Equals(s, replacementShardId, StringComparison.OrdinalIgnoreCase)))
            {
                updatedShards.Add(replacementShardId);
            }

            return new EntityPlacement(
                entityName:        placement.EntityName,
                mode:              placement.Mode,
                shardIds:          updatedShards,
                partitionFunction: placement.PartitionFunction,
                replicationFactor: placement.ReplicationFactor,
                writeQuorum:       placement.WriteQuorum);
        }

        private static bool ReferencesShard(EntityPlacement placement, string shardId)
        {
            if (placement?.ShardIds == null) return false;
            for (int i = 0; i < placement.ShardIds.Count; i++)
            {
                if (string.Equals(placement.ShardIds[i], shardId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
