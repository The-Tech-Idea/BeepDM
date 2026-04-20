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
    /// <c>RepartitionEntityAsync</c>. Repartitions a sharded entity
    /// using a new partition function and/or a new shard set by
    /// dual-writing across every old → new shard pair until the
    /// target placement is fully populated.
    /// </summary>
    public sealed partial class ReshardingService
    {
        /// <inheritdoc/>
        public async Task<ReshardOutcome> RepartitionEntityAsync(
            string                entityName,
            PartitionFunctionRef  newFunction,
            IReadOnlyList<string> newShardIds,
            CancellationToken     cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(entityName))  throw new ArgumentException("Entity name required.", nameof(entityName));
            if (newFunction == null)                    throw new ArgumentNullException(nameof(newFunction));
            if (newShardIds == null || newShardIds.Count == 0)
                throw new ArgumentException("Target shard set must not be empty.", nameof(newShardIds));

            EnsureNoActiveWindow(entityName);

            var currentPlan = _getCurrentPlan();
            if (currentPlan == null || !currentPlan.TryGetPlacement(entityName, out var oldPlacement))
            {
                throw new InvalidOperationException(
                    $"Cannot repartition '{entityName}' — entity has no current placement.");
            }

            var reshardId = NewReshardId("Repartition");
            _raiseStarted(reshardId, $"Repartition '{entityName}' -> [{string.Join(",", newShardIds)}]", 1);

            var copyResults = new List<CopyResult>();
            try
            {
                var legs = await RepartitionEntityInternalAsync(
                    reshardId,
                    entityName,
                    oldPlacement.ShardIds,
                    newShardIds,
                    cancellationToken).ConfigureAwait(false);
                copyResults.AddRange(legs);

                var newPlacement = new EntityPlacement(
                    entityName:        entityName,
                    mode:              oldPlacement.Mode == DistributionMode.Routed
                                           ? DistributionMode.Sharded
                                           : oldPlacement.Mode,
                    shardIds:          newShardIds,
                    partitionFunction: newFunction,
                    replicationFactor: oldPlacement.ReplicationFactor,
                    writeQuorum:       oldPlacement.WriteQuorum);
                var newPlan = currentPlan.WithEntity(newPlacement);
                _applyPlan(newPlan);
                _raiseCompleted(reshardId, $"Repartition '{entityName}' completed", 1, null);
                return SuccessOutcome(reshardId, "Repartition", newPlan.Version, copyResults);
            }
            catch (OperationCanceledException)
            {
                var window = DualWrites.TryGetWindow(entityName);
                window?.ForceOff();
                DualWrites.Unregister(entityName);
                _raiseCompleted(reshardId, $"Repartition '{entityName}' cancelled", 1, null);
                return CancelledOutcome(reshardId, "Repartition", currentPlan.Version, copyResults);
            }
            catch (Exception ex)
            {
                var window = DualWrites.TryGetWindow(entityName);
                window?.ForceOff();
                DualWrites.Unregister(entityName);
                _raiseCompleted(reshardId, $"Repartition '{entityName}' failed: {ex.Message}", 1, ex);
                return FailureOutcome(reshardId, "Repartition", currentPlan.Version, copyResults, ex);
            }
        }

        private async Task<IReadOnlyList<CopyResult>> RepartitionEntityInternalAsync(
            string                reshardId,
            string                entityName,
            IReadOnlyList<string> oldShardIds,
            IReadOnlyList<string> newShardIds,
            CancellationToken     cancellationToken)
        {
            foreach (var id in oldShardIds) ResolveOrThrow(id, "source");
            foreach (var id in newShardIds) ResolveOrThrow(id, "target");

            var window = new DualWriteWindow(
                reshardId:      reshardId,
                entityName:     entityName,
                sourceShardIds: oldShardIds,
                targetShardIds: newShardIds);

            DualWrites.Register(window);
            try
            {
                window.TransitionTo(DualWriteState.DualWrite);

                var pairs   = BuildCopyPairs(oldShardIds, newShardIds);
                var results = new List<CopyResult>(pairs.Count);
                foreach (var (from, to) in pairs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var leg = await RunLegAsync(reshardId, entityName, from, to, cancellationToken).ConfigureAwait(false);
                    results.Add(leg);
                    if (!leg.Success)
                    {
                        throw leg.Error
                            ?? new InvalidOperationException(
                                   $"Copy leg failed for entity '{entityName}' ({from} -> {to}).");
                    }
                }

                window.TransitionTo(DualWriteState.Cutover);
                window.TransitionTo(DualWriteState.Off);
                return results;
            }
            finally
            {
                DualWrites.Unregister(entityName);
            }
        }

        private static IReadOnlyList<(string From, string To)> BuildCopyPairs(
            IReadOnlyList<string> oldShards,
            IReadOnlyList<string> newShards)
        {
            var pairs   = new List<(string From, string To)>();
            var newSet  = new HashSet<string>(newShards, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < oldShards.Count; i++)
            {
                var src = oldShards[i];
                // Shards that survive in the new set act as anchors — nothing to copy from them.
                if (newSet.Contains(src)) continue;

                for (int j = 0; j < newShards.Count; j++)
                {
                    var dst = newShards[j];
                    if (string.Equals(src, dst, StringComparison.OrdinalIgnoreCase)) continue;
                    pairs.Add((src, dst));
                }
            }
            return pairs;
        }
    }
}
