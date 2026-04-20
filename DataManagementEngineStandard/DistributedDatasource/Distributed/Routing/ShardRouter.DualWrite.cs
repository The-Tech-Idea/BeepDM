using System.Collections.Generic;
using TheTechIdea.Beep.Distributed.Resharding;

namespace TheTechIdea.Beep.Distributed.Routing
{
    /// <summary>
    /// <see cref="ShardRouter"/> partial that augments write-path
    /// routing decisions with the additional target shards demanded
    /// by active Phase 11 dual-write windows.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reads are never rerouted by the dual-write coordinator — the
    /// reshard source is still canonical for query traffic. Writes
    /// are augmented only when a window is in
    /// <see cref="DualWriteState.DualWrite"/> and the routed shard
    /// set intersects the window's source shard list; in that case
    /// the target shards from the window are union-merged into the
    /// decision so every INSERT/UPDATE/DELETE lands on both the old
    /// and the new placement.
    /// </para>
    /// <para>
    /// The coordinator is optional. When <c>null</c> (default), the
    /// router behaves exactly like it did before Phase 11 — no
    /// additional shards are added. When provided, dual-write fan-out
    /// is layered on top of the baseline routing decision without
    /// altering any other routing semantics.
    /// </para>
    /// </remarks>
    public sealed partial class ShardRouter
    {
        private IDualWriteCoordinator _dualWrites;

        /// <summary>
        /// Optional dual-write coordinator. When set, writes against
        /// entities with an active <see cref="DualWriteWindow"/> (in
        /// <see cref="DualWriteState.DualWrite"/>) are augmented with
        /// the window's target shards so each write reaches both the
        /// old and new placements atomically.
        /// </summary>
        public IDualWriteCoordinator DualWrites
        {
            get => _dualWrites;
            set => _dualWrites = value;
        }

        private RoutingDecision ApplyDualWriteFanOut(
            string          entityName,
            bool            isWrite,
            RoutingDecision decision)
        {
            if (!isWrite)                     return decision;
            if (decision == null)             return decision;
            var coordinator = _dualWrites;
            if (coordinator == null)          return decision;

            var window = coordinator.TryGetWindow(entityName);
            if (window == null)               return decision;
            if (!window.IsWriteDualHit)       return decision;

            var union = UnionTargets(decision.ShardIds, window.TargetShardIds);
            if (union.Count == decision.ShardIds.Count) return decision;

            return new RoutingDecision(
                entityName:        decision.EntityName,
                mode:              decision.Mode,
                matchKind:         decision.MatchKind,
                shardIds:          union,
                isWrite:           decision.IsWrite,
                isScatter:         decision.IsScatter,
                isFanOut:          union.Count > 1,
                writeQuorum:       decision.WriteQuorum,
                replicationFactor: decision.ReplicationFactor,
                keyValues:         decision.KeyValues,
                hookOverridden:    decision.HookOverridden,
                source:            decision.Source);
        }

        private static IReadOnlyList<string> UnionTargets(
            IReadOnlyList<string> baseline,
            IReadOnlyList<string> additions)
        {
            var seen   = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            var merged = new List<string>(baseline.Count + (additions?.Count ?? 0));
            for (int i = 0; i < baseline.Count; i++)
            {
                var shard = baseline[i];
                if (string.IsNullOrEmpty(shard)) continue;
                if (seen.Add(shard)) merged.Add(shard);
            }
            if (additions != null)
            {
                for (int i = 0; i < additions.Count; i++)
                {
                    var shard = additions[i];
                    if (string.IsNullOrEmpty(shard)) continue;
                    if (seen.Add(shard)) merged.Add(shard);
                }
            }
            return merged;
        }
    }
}
