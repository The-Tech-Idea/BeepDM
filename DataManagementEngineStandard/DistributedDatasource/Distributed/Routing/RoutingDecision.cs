using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Distributed.Placement;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed.Routing
{
    /// <summary>
    /// Concrete instruction produced by <see cref="ShardRouter"/>:
    /// "given this entity and these key values, send the operation
    /// to <em>these</em> shards with <em>this</em> quorum and
    /// fan-out flag." Phase 06+ executors consume the decision; the
    /// router never executes anything itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Decisions are immutable so they can be safely captured into
    /// closures by the read / write executors. Construction is
    /// validated: <see cref="ShardIds"/> may be empty only when
    /// <see cref="MatchKind"/> indicates an explicit failure mode the
    /// caller is expected to handle.
    /// </para>
    /// <para>
    /// <see cref="KeyValues"/> records the partition-key values that
    /// were actually consulted (or <c>null</c>/empty for scatter
    /// reads / unkeyed broadcasts). The dictionary is exposed for
    /// observability — Phase 13 audit attaches it to the trace event
    /// so a slow read can be correlated with the key it was issued
    /// for.
    /// </para>
    /// </remarks>
    public sealed class RoutingDecision
    {
        private static readonly IReadOnlyDictionary<string, object> EmptyKeyValues
            = new Dictionary<string, object>(0, StringComparer.OrdinalIgnoreCase);

        /// <summary>Initialises a new decision.</summary>
        /// <param name="entityName">Logical entity the decision applies to. Required.</param>
        /// <param name="mode">Effective distribution mode for this call.</param>
        /// <param name="matchKind">How the upstream placement was matched (exact / prefix / default / broadcast / unmapped).</param>
        /// <param name="shardIds">Shards the executor must contact. May be empty when <paramref name="isScatter"/> is <c>true</c> and no live shards exist.</param>
        /// <param name="isWrite"><c>true</c> when this decision was produced for a write call.</param>
        /// <param name="isScatter"><c>true</c> when no partition key was supplied for a sharded read and the executor must fan out across <paramref name="shardIds"/>.</param>
        /// <param name="isFanOut"><c>true</c> when the operation must touch more than one shard (replicated / broadcast write or sharded write resolved to multiple shards).</param>
        /// <param name="writeQuorum">Quorum carried over from <see cref="EntityPlacement.WriteQuorum"/>; <c>0</c> means "all listed shards must ack".</param>
        /// <param name="replicationFactor">Replication factor carried over from <see cref="EntityPlacement.ReplicationFactor"/>.</param>
        /// <param name="keyValues">Partition-key columns used for routing; <c>null</c> is normalised to empty.</param>
        /// <param name="hookOverridden"><c>true</c> when an <see cref="IShardRoutingHook"/> replaced the baseline decision.</param>
        /// <param name="source">Original placement resolution; useful for diagnostics. May be <c>null</c> for hook-only decisions.</param>
        public RoutingDecision(
            string                              entityName,
            DistributionMode                    mode,
            PlacementMatchKind                  matchKind,
            IReadOnlyList<string>               shardIds,
            bool                                isWrite,
            bool                                isScatter,
            bool                                isFanOut,
            int                                 writeQuorum,
            int                                 replicationFactor,
            IReadOnlyDictionary<string, object> keyValues       = null,
            bool                                hookOverridden  = false,
            PlacementResolution                 source          = null)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));
            if (writeQuorum < 0)
                throw new ArgumentOutOfRangeException(nameof(writeQuorum), "Write quorum must be >= 0.");
            if (replicationFactor < 1)
                throw new ArgumentOutOfRangeException(nameof(replicationFactor), "Replication factor must be >= 1.");

            EntityName        = entityName;
            Mode              = mode;
            MatchKind         = matchKind;
            ShardIds          = shardIds == null
                                ? Array.Empty<string>()
                                : shardIds.ToArray();
            IsWrite           = isWrite;
            IsScatter         = isScatter;
            IsFanOut          = isFanOut;
            WriteQuorum       = writeQuorum;
            ReplicationFactor = replicationFactor;
            KeyValues         = keyValues ?? EmptyKeyValues;
            HookOverridden    = hookOverridden;
            Source            = source;
        }

        /// <summary>Logical entity the decision applies to.</summary>
        public string EntityName { get; }

        /// <summary>Effective distribution mode resolved for this call.</summary>
        public DistributionMode Mode { get; }

        /// <summary>How the placement was matched (exact / prefix / default / broadcast / unmapped).</summary>
        public PlacementMatchKind MatchKind { get; }

        /// <summary>Shards the executor must contact. Never <c>null</c>; may be empty when no live shard is available.</summary>
        public IReadOnlyList<string> ShardIds { get; }

        /// <summary><c>true</c> when this decision was produced for a write call.</summary>
        public bool IsWrite { get; }

        /// <summary><c>true</c> when the executor must fan out a sharded read because no partition key was supplied.</summary>
        public bool IsScatter { get; }

        /// <summary><c>true</c> when the operation must touch more than one shard (replicated / broadcast / multi-shard sharded).</summary>
        public bool IsFanOut { get; }

        /// <summary>Minimum acknowledged writes; <c>0</c> means "all listed shards must ack".</summary>
        public int WriteQuorum { get; }

        /// <summary>Replicas per row (carried from <see cref="EntityPlacement.ReplicationFactor"/>).</summary>
        public int ReplicationFactor { get; }

        /// <summary>Partition-key columns and values used for routing. Never <c>null</c>; empty for scatter / broadcast.</summary>
        public IReadOnlyDictionary<string, object> KeyValues { get; }

        /// <summary><c>true</c> when an <see cref="IShardRoutingHook"/> replaced the baseline decision.</summary>
        public bool HookOverridden { get; }

        /// <summary>Original placement resolution for diagnostics; may be <c>null</c> for hook-only decisions.</summary>
        public PlacementResolution Source { get; }

        /// <inheritdoc/>
        public override string ToString()
            => $"RoutingDecision({EntityName}, {Mode}, match={MatchKind}, shards=[{string.Join(",", ShardIds)}]" +
               $", scatter={IsScatter}, fanOut={IsFanOut}, quorum={WriteQuorum}, hook={HookOverridden})";
    }
}
