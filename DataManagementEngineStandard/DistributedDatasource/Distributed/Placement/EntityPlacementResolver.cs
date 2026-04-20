using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed.Placement
{
    /// <summary>
    /// Combines an <see cref="EntityPlacementMap"/>, a live-shard
    /// supplier, and an <see cref="UnmappedEntityPolicy"/> into a
    /// single <see cref="Resolve(string, bool)"/> call that returns a
    /// <see cref="PlacementResolution"/> ready for the executors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The resolver always queries the live-shard supplier when
    /// expanding <see cref="DistributionMode.Broadcast"/> placements
    /// (and when applying the <see cref="UnmappedEntityPolicy.BroadcastUnmapped"/>
    /// fallback) so that shards added after the plan was built are
    /// included automatically — see Phase 03 risk note.
    /// </para>
    /// <para>
    /// Shards listed by a <see cref="EntityPlacement"/> but missing
    /// from the live snapshot are filtered out. When that filtering
    /// removes every shard the resolver still returns a non-null
    /// resolution but with an empty <see cref="PlacementResolution.TargetShardIds"/> —
    /// callers must check <see cref="PlacementResolution.IsUnmapped"/>
    /// or the <c>TargetShardIds.Count == 0</c> case before dispatching.
    /// </para>
    /// </remarks>
    public sealed class EntityPlacementResolver
    {
        private readonly EntityPlacementMap          _map;
        private readonly Func<IReadOnlyList<string>> _liveShardSupplier;
        private readonly UnmappedEntityPolicy        _unmappedPolicy;
        private readonly string                      _defaultShardId;

        /// <summary>Initialises a new resolver.</summary>
        /// <param name="map">Pre-built placement lookup; supply <see cref="EntityPlacementMap.Empty"/> when no plan is active.</param>
        /// <param name="liveShardSupplier">Returns the current live shard ids. Called per resolve for Broadcast / fallback expansion.</param>
        /// <param name="unmappedPolicy">Behaviour when no placement matches.</param>
        /// <param name="defaultShardId">Shard id used by <see cref="UnmappedEntityPolicy.DefaultShardId"/>; ignored otherwise.</param>
        public EntityPlacementResolver(
            EntityPlacementMap          map,
            Func<IReadOnlyList<string>> liveShardSupplier,
            UnmappedEntityPolicy        unmappedPolicy = UnmappedEntityPolicy.RejectUnmapped,
            string                      defaultShardId = null)
        {
            _map               = map               ?? throw new ArgumentNullException(nameof(map));
            _liveShardSupplier = liveShardSupplier ?? throw new ArgumentNullException(nameof(liveShardSupplier));
            _unmappedPolicy    = unmappedPolicy;
            _defaultShardId    = defaultShardId;

            if (_unmappedPolicy == UnmappedEntityPolicy.DefaultShardId &&
                string.IsNullOrWhiteSpace(_defaultShardId))
            {
                throw new ArgumentException(
                    "DefaultShardId policy requires a non-empty defaultShardId.",
                    nameof(defaultShardId));
            }
        }

        /// <summary>Active placement lookup. Exposed for diagnostics; do not mutate.</summary>
        public EntityPlacementMap Map => _map;

        /// <summary>Active unmapped-entity policy.</summary>
        public UnmappedEntityPolicy UnmappedPolicy => _unmappedPolicy;

        /// <summary>Default shard id used by <see cref="UnmappedEntityPolicy.DefaultShardId"/>.</summary>
        public string DefaultShardId => _defaultShardId;

        /// <summary>
        /// Resolves <paramref name="entityName"/> against the active map
        /// and the live shard supplier. <paramref name="isWrite"/> is
        /// reserved for future write-only routing rules (Phase 05+);
        /// in Phase 03 it is recorded on the resolution but does not
        /// alter the result.
        /// </summary>
        public PlacementResolution Resolve(string entityName, bool isWrite = false)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));

            var matchKind = _map.Match(entityName, out var placement);

            if (matchKind == PlacementMatchKind.Unmapped)
                return ApplyUnmappedPolicy(entityName);

            return BuildResolutionFromPlacement(entityName, placement, matchKind);
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private PlacementResolution BuildResolutionFromPlacement(
            string             entityName,
            EntityPlacement    placement,
            PlacementMatchKind matchKind)
        {
            var liveShards = SafeLiveShards();

            // Broadcast: ignore the persisted shard list and use every live shard.
            // Late-added shards are picked up automatically (Phase 03 risk note).
            if (placement.Mode == DistributionMode.Broadcast)
            {
                return new PlacementResolution(
                    entityName,
                    DistributionMode.Broadcast,
                    liveShards,
                    placement.WriteQuorum,
                    placement.ReplicationFactor,
                    matchKind,
                    placement);
            }

            // For all other modes, intersect the placement shard list with the live snapshot.
            var live = new HashSet<string>(liveShards, StringComparer.OrdinalIgnoreCase);
            var targets = placement.ShardIds.Where(live.Contains).ToList();

            return new PlacementResolution(
                entityName,
                placement.Mode,
                targets,
                placement.WriteQuorum,
                placement.ReplicationFactor,
                matchKind,
                placement);
        }

        private PlacementResolution ApplyUnmappedPolicy(string entityName)
        {
            var liveShards = SafeLiveShards();

            switch (_unmappedPolicy)
            {
                case UnmappedEntityPolicy.DefaultShardId:
                    {
                        var live = new HashSet<string>(liveShards, StringComparer.OrdinalIgnoreCase);
                        var target = live.Contains(_defaultShardId)
                            ? new List<string> { _defaultShardId }
                            : new List<string>();
                        return new PlacementResolution(
                            entityName,
                            DistributionMode.Routed,
                            target,
                            writeQuorum:       0,
                            replicationFactor: 1,
                            matchKind:         PlacementMatchKind.DefaultRoute);
                    }

                case UnmappedEntityPolicy.BroadcastUnmapped:
                    return new PlacementResolution(
                        entityName,
                        DistributionMode.Broadcast,
                        liveShards,
                        writeQuorum:       0,
                        replicationFactor: 1,
                        matchKind:         PlacementMatchKind.Broadcast);

                case UnmappedEntityPolicy.RejectUnmapped:
                default:
                    return new PlacementResolution(
                        entityName,
                        DistributionMode.Routed,
                        Array.Empty<string>(),
                        writeQuorum:       0,
                        replicationFactor: 1,
                        matchKind:         PlacementMatchKind.Unmapped);
            }
        }

        private IReadOnlyList<string> SafeLiveShards()
        {
            try
            {
                return _liveShardSupplier() ?? Array.Empty<string>();
            }
            catch
            {
                // Defensive: a misbehaving supplier must never crash the resolver.
                return Array.Empty<string>();
            }
        }
    }
}
