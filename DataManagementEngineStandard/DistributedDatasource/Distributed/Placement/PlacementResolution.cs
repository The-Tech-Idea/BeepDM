using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed.Placement
{
    /// <summary>
    /// Result of resolving an entity name against the active
    /// <see cref="DistributionPlan"/> + live shard catalog. Carries
    /// everything the executors (Phase 06 / 07) need to fan out a
    /// request without re-querying the resolver.
    /// </summary>
    /// <remarks>
    /// Resolutions are immutable and safe to share across threads.
    /// They are scoped to a single resolve call — the
    /// <see cref="TargetShardIds"/> snapshot reflects the catalog at
    /// the moment of resolution, NOT the catalog at execution time.
    /// </remarks>
    public sealed class PlacementResolution
    {
        /// <summary>Initialises a fully-formed resolution.</summary>
        /// <param name="entityName">Original entity name supplied to the resolver.</param>
        /// <param name="mode">Effective distribution mode (after fallback).</param>
        /// <param name="targetShardIds">Shards the executor should target. Empty for <see cref="PlacementMatchKind.Unmapped"/> resolutions.</param>
        /// <param name="writeQuorum">Minimum acknowledged writes (0 = all shards).</param>
        /// <param name="replicationFactor">Replicas per row for <see cref="DistributionMode.Replicated"/>.</param>
        /// <param name="matchKind">How the resolution was reached; drives diagnostics.</param>
        /// <param name="source">Originating placement when one matched; <c>null</c> for fallback / unmapped resolutions.</param>
        public PlacementResolution(
            string                entityName,
            DistributionMode      mode,
            IReadOnlyList<string> targetShardIds,
            int                   writeQuorum,
            int                   replicationFactor,
            PlacementMatchKind    matchKind,
            EntityPlacement       source = null)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));

            EntityName        = entityName;
            Mode              = mode;
            TargetShardIds    = targetShardIds ?? Array.Empty<string>();
            WriteQuorum       = writeQuorum   < 0 ? 0 : writeQuorum;
            ReplicationFactor = replicationFactor < 1 ? 1 : replicationFactor;
            MatchKind         = matchKind;
            Source            = source;
        }

        /// <summary>Original entity name supplied to the resolver.</summary>
        public string EntityName { get; }

        /// <summary>Effective distribution mode after fallback.</summary>
        public DistributionMode Mode { get; }

        /// <summary>Shards the executor should target. Empty when <see cref="MatchKind"/> is <see cref="PlacementMatchKind.Unmapped"/>.</summary>
        public IReadOnlyList<string> TargetShardIds { get; }

        /// <summary>Minimum acknowledged writes (0 = all shards).</summary>
        public int WriteQuorum { get; }

        /// <summary>Replicas per row for <see cref="DistributionMode.Replicated"/>.</summary>
        public int ReplicationFactor { get; }

        /// <summary>How the resolution was reached; drives diagnostics.</summary>
        public PlacementMatchKind MatchKind { get; }

        /// <summary>Originating placement when one matched; <c>null</c> for fallback / unmapped resolutions.</summary>
        public EntityPlacement Source { get; }

        /// <summary>Convenience: <c>true</c> when the effective mode is <see cref="DistributionMode.Broadcast"/>.</summary>
        public bool IsBroadcast => Mode == DistributionMode.Broadcast;

        /// <summary>Convenience: <c>true</c> when no placement matched and no fallback was applied.</summary>
        public bool IsUnmapped => MatchKind == PlacementMatchKind.Unmapped;

        /// <inheritdoc/>
        public override string ToString()
            => $"{EntityName} -> [{string.Join(",", TargetShardIds)}] ({Mode}/{MatchKind}, quorum={WriteQuorum}, rf={ReplicationFactor})";
    }
}
