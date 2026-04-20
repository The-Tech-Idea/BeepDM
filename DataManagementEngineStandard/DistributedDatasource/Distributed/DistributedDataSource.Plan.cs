using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// <see cref="DistributedDataSource"/> partial — distribution plan
    /// management and validation against the live shard catalog.
    /// Extracts the plan-related concerns out of the constructor partial
    /// so Phase 11 (resharding) can extend behaviour here without
    /// touching identity, lifecycle, or the <see cref="IDataSource"/>
    /// surface.
    /// </summary>
    public partial class DistributedDataSource
    {
        /// <summary>
        /// Returns the currently active plan. Equivalent to
        /// <see cref="DistributionPlan"/> but exposed as a method to
        /// match Phase 02 wording in the design doc.
        /// </summary>
        public DistributionPlan GetCurrentPlan() => DistributionPlan;

        /// <inheritdoc cref="IDistributedDataSource.ApplyDistributionPlan(DistributionPlan)" />
        /// <remarks>
        /// <para>
        /// Synchronous plan swap: validates the target plan against
        /// the live shard catalog and replaces the active plan
        /// reference without moving any data. Use this overload when
        /// the caller knows the delta requires no data movement
        /// (pure version bumps, new empty entities, or additions to
        /// broadcast placements).
        /// </para>
        /// <para>
        /// For live topology changes that require data migration
        /// (moves, repartitions, shard drains), call
        /// <see cref="ApplyDistributionPlanAsync(DistributionPlan, System.Threading.CancellationToken)"/>
        /// instead — it routes the diff through the Phase 11
        /// resharding pipeline.
        /// </para>
        /// </remarks>
        public void ApplyDistributionPlan(DistributionPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            ThrowIfDisposed();

            ValidatePlanAgainstShards(plan);

            lock (_planSwapLock)
            {
                _plan = plan;
                RebuildPlacementResolver(plan);
            }
            AttachDualWriteCoordinatorToRouter();
        }

        // ── Validation ────────────────────────────────────────────────────

        /// <summary>
        /// Validates the supplied plan against the live shard map and
        /// raises <see cref="OnPlacementViolation"/> for every detected
        /// problem. Validation never throws — violations are surfaced
        /// via events so callers can decide whether to proceed with a
        /// degraded plan or roll back.
        /// </summary>
        /// <param name="plan">Plan to validate; <see cref="DistributionPlan.Empty"/> is treated as valid.</param>
        private void ValidatePlanAgainstShards(DistributionPlan plan)
        {
            if (plan == null || plan.IsEmpty) return;

            // 1. Plan-level: any referenced shard ID that is not in the live shard map.
            var unknownShards = plan
                .ReferencedShardIds()
                .Where(id => !_shards.ContainsKey(id))
                .ToList();

            foreach (var shardId in unknownShards)
            {
                RaisePlacementViolation(
                    entityName: "(plan)",
                    shardId:    shardId,
                    reason:     $"Plan '{plan.Name}' v{plan.Version} references unknown shard '{shardId}'.");
            }

            // 2. Per-placement: each placement must have at least one resolvable shard.
            foreach (var placement in plan.EntityPlacements.Values)
            {
                var resolvable = placement.ShardIds.Count(id => _shards.ContainsKey(id));
                if (resolvable == 0)
                {
                    RaisePlacementViolation(
                        entityName: placement.EntityName,
                        shardId:    placement.ShardIds.FirstOrDefault() ?? "(none)",
                        reason:     $"Placement for '{placement.EntityName}' references no live shards.");
                    continue;
                }

                // Mode-specific consistency checks.
                switch (placement.Mode)
                {
                    case DistributionMode.Routed:
                        if (placement.ShardIds.Count != 1)
                        {
                            RaisePlacementViolation(
                                placement.EntityName,
                                placement.ShardIds[0],
                                $"Routed placement for '{placement.EntityName}' must reference exactly one shard (got {placement.ShardIds.Count}).");
                        }
                        break;

                    case DistributionMode.Sharded:
                        if (placement.PartitionFunction == null ||
                            placement.PartitionFunction.Kind == PartitionKind.None)
                        {
                            RaisePlacementViolation(
                                placement.EntityName,
                                placement.ShardIds[0],
                                $"Sharded placement for '{placement.EntityName}' is missing a partition function.");
                        }
                        break;

                    case DistributionMode.Replicated:
                        if (placement.ReplicationFactor > placement.ShardIds.Count)
                        {
                            RaisePlacementViolation(
                                placement.EntityName,
                                placement.ShardIds[0],
                                $"Replicated placement for '{placement.EntityName}' requests RF={placement.ReplicationFactor} but only {placement.ShardIds.Count} shard(s) declared.");
                        }
                        break;

                    case DistributionMode.Broadcast:
                        // Broadcast accepts any shard count; no extra checks at Phase 02.
                        break;
                }

                // Quorum sanity: write quorum must not exceed declared shard count.
                if (placement.WriteQuorum > placement.ShardIds.Count)
                {
                    RaisePlacementViolation(
                        placement.EntityName,
                        placement.ShardIds[0],
                        $"Write quorum {placement.WriteQuorum} exceeds shard count {placement.ShardIds.Count} for '{placement.EntityName}'.");
                }
            }
        }
    }
}
