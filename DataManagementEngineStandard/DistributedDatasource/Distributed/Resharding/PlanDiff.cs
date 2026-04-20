using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Plan-to-plan diff calculator. Produces the minimal ordered
    /// list of <see cref="PlanDiffEntry"/> steps required to migrate
    /// <paramref name="oldPlan"/> into <paramref name="newPlan"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Step ordering: removes run first (to free capacity), then
    /// adds, then repartition/move steps. The actual apply order is
    /// the <see cref="ReshardingService"/>'s responsibility; the
    /// diff merely classifies and presents the work.
    /// </para>
    /// <para>
    /// Equality is Phase-02 value-style: two placements compare
    /// equal when their mode, shard list, partition function, RF,
    /// and quorum match exactly. A <see cref="PlanDiffKind.NoOp"/>
    /// entry is never added for an unchanged placement; the result
    /// list only contains actionable work.
    /// </para>
    /// </remarks>
    public static class PlanDiff
    {
        /// <summary>
        /// Computes the diff from <paramref name="oldPlan"/> to
        /// <paramref name="newPlan"/>. Either side may be
        /// <c>null</c> or <see cref="DistributionPlan.Empty"/>.
        /// </summary>
        public static IReadOnlyList<PlanDiffEntry> Compute(DistributionPlan oldPlan, DistributionPlan newPlan)
        {
            var oldMap = AsMap(oldPlan);
            var newMap = AsMap(newPlan);

            var removes       = new List<PlanDiffEntry>();
            var adds          = new List<PlanDiffEntry>();
            var modifications = new List<PlanDiffEntry>();

            foreach (var kv in oldMap)
            {
                if (!newMap.ContainsKey(kv.Key))
                {
                    removes.Add(new PlanDiffEntry(kv.Key, PlanDiffKind.RemoveEntity, kv.Value, newPlacement: null));
                }
            }

            foreach (var kv in newMap)
            {
                if (!oldMap.TryGetValue(kv.Key, out var oldPlacement))
                {
                    adds.Add(new PlanDiffEntry(kv.Key, PlanDiffKind.AddEntity, oldPlacement: null, newPlacement: kv.Value));
                    continue;
                }

                if (oldPlacement.Equals(kv.Value)) continue; // unchanged — skip

                var kind = ClassifyChange(oldPlacement, kv.Value);
                modifications.Add(new PlanDiffEntry(kv.Key, kind, oldPlacement, kv.Value));
            }

            // Removes → Adds → Modifications gives operators a predictable order.
            var result = new List<PlanDiffEntry>(removes.Count + adds.Count + modifications.Count);
            result.AddRange(removes);
            result.AddRange(adds);
            result.AddRange(modifications);
            return result;
        }

        /// <summary>
        /// Convenience helper that returns <c>true</c> when two plans
        /// differ in at least one actionable way.
        /// </summary>
        public static bool HasChanges(DistributionPlan oldPlan, DistributionPlan newPlan)
            => Compute(oldPlan, newPlan).Count > 0;

        // ── Internal ──────────────────────────────────────────────────────

        private static IReadOnlyDictionary<string, EntityPlacement> AsMap(DistributionPlan plan)
        {
            if (plan == null || plan.IsEmpty)
                return new Dictionary<string, EntityPlacement>(0, StringComparer.OrdinalIgnoreCase);

            return plan.EntityPlacements;
        }

        private static PlanDiffKind ClassifyChange(EntityPlacement oldP, EntityPlacement newP)
        {
            if (oldP.Mode != newP.Mode)                       return PlanDiffKind.ModeChange;
            if (PartitionFunctionChanged(oldP, newP))         return PlanDiffKind.ModeChange;

            if (IsSingleShardMove(oldP, newP))                return PlanDiffKind.MoveEntity;
            return PlanDiffKind.Repartition;
        }

        private static bool PartitionFunctionChanged(EntityPlacement oldP, EntityPlacement newP)
        {
            var a = oldP.PartitionFunction;
            var b = newP.PartitionFunction;
            if (a == null && b == null) return false;
            if (a == null || b == null) return true;
            if (a.Kind != b.Kind)       return true;

            // Key-column set change counts as a partition-function change;
            // shard-list churn with the same kind + columns is a Repartition.
            if (a.KeyColumns.Count != b.KeyColumns.Count) return true;
            for (int i = 0; i < a.KeyColumns.Count; i++)
            {
                if (!string.Equals(a.KeyColumns[i], b.KeyColumns[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool IsSingleShardMove(EntityPlacement oldP, EntityPlacement newP)
        {
            if (oldP.Mode != DistributionMode.Routed)   return false;
            if (newP.Mode != DistributionMode.Routed)   return false;
            if (oldP.ShardIds.Count != 1)               return false;
            if (newP.ShardIds.Count != 1)               return false;
            return !string.Equals(oldP.ShardIds[0], newP.ShardIds[0], StringComparison.OrdinalIgnoreCase);
        }
    }
}
