using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Distributed.Plan
{
    /// <summary>
    /// Per-entity placement record carried by a
    /// <see cref="DistributionPlan"/>. Tells the router and executors
    /// which shards own this entity, which mode applies, and (for
    /// <see cref="DistributionMode.Sharded"/> placements) which
    /// partition function to consult.
    /// </summary>
    /// <remarks>
    /// Placement instances are immutable. Equality is value-style on
    /// every member so Phase 11 plan diffing can detect placement
    /// changes that require resharding without depending on object
    /// identity.
    /// </remarks>
    public sealed class EntityPlacement : IEquatable<EntityPlacement>
    {
        /// <summary>Initialises a new placement.</summary>
        /// <param name="entityName">Logical entity name (table). Must not be null/whitespace.</param>
        /// <param name="mode">Distribution mode for this entity.</param>
        /// <param name="shardIds">Shard identifiers that own (or replicate) this entity. Must contain at least one id.</param>
        /// <param name="partitionFunction">Row-level partition function reference. Required when <paramref name="mode"/> is <see cref="DistributionMode.Sharded"/>; defaults to <see cref="PartitionFunctionRef.None"/> otherwise.</param>
        /// <param name="replicationFactor">Replicas per row for <see cref="DistributionMode.Replicated"/>; ignored otherwise. Must be &gt;= 1.</param>
        /// <param name="writeQuorum">Minimum acknowledged writes for replicated/broadcast modes. <c>0</c> means "all shards must ack".</param>
        public EntityPlacement(
            string                  entityName,
            DistributionMode        mode,
            IReadOnlyList<string>   shardIds,
            PartitionFunctionRef    partitionFunction = null,
            int                     replicationFactor = 1,
            int                     writeQuorum       = 0)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));
            if (shardIds == null || shardIds.Count == 0)
                throw new ArgumentException("At least one shard ID is required.", nameof(shardIds));
            if (replicationFactor < 1)
                throw new ArgumentOutOfRangeException(nameof(replicationFactor), "Replication factor must be >= 1.");
            if (writeQuorum < 0)
                throw new ArgumentOutOfRangeException(nameof(writeQuorum), "Write quorum must be >= 0.");

            partitionFunction ??= PartitionFunctionRef.None;
            if (mode == DistributionMode.Sharded && partitionFunction.Kind == PartitionKind.None)
                throw new ArgumentException(
                    $"Sharded entity '{entityName}' requires a non-None partition function.",
                    nameof(partitionFunction));

            EntityName        = entityName;
            Mode              = mode;
            ShardIds          = NormaliseShardIds(shardIds);
            PartitionFunction = partitionFunction;
            ReplicationFactor = replicationFactor;
            WriteQuorum       = writeQuorum;
        }

        /// <summary>Logical entity name (table).</summary>
        public string EntityName { get; }

        /// <summary>Distribution mode for this entity.</summary>
        public DistributionMode Mode { get; }

        /// <summary>Shard identifiers that own (or replicate) this entity. Always non-empty.</summary>
        public IReadOnlyList<string> ShardIds { get; }

        /// <summary>Row-level partition function reference; <see cref="PartitionFunctionRef.None"/> for non-Sharded modes.</summary>
        public PartitionFunctionRef PartitionFunction { get; }

        /// <summary>Replicas per row for <see cref="DistributionMode.Replicated"/> placements.</summary>
        public int ReplicationFactor { get; }

        /// <summary>Minimum acknowledged writes; <c>0</c> means "all shards must ack".</summary>
        public int WriteQuorum { get; }

        // ── Equality ──────────────────────────────────────────────────────

        /// <inheritdoc/>
        public bool Equals(EntityPlacement other)
        {
            if (other == null) return false;
            if (!string.Equals(EntityName, other.EntityName, StringComparison.OrdinalIgnoreCase)) return false;
            if (Mode              != other.Mode)              return false;
            if (ReplicationFactor != other.ReplicationFactor) return false;
            if (WriteQuorum       != other.WriteQuorum)       return false;
            if (ShardIds.Count    != other.ShardIds.Count)    return false;
            for (var i = 0; i < ShardIds.Count; i++)
            {
                if (!string.Equals(ShardIds[i], other.ShardIds[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return Equals(PartitionFunction, other.PartitionFunction);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as EntityPlacement);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var h = StringComparer.OrdinalIgnoreCase.GetHashCode(EntityName);
                h = (h * 397) ^ (int)Mode;
                h = (h * 397) ^ ReplicationFactor;
                h = (h * 397) ^ WriteQuorum;
                foreach (var id in ShardIds)
                    h = (h * 31) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(id ?? string.Empty);
                h = (h * 397) ^ (PartitionFunction?.GetHashCode() ?? 0);
                return h;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"{EntityName} [{Mode}] -> {string.Join(",", ShardIds)} (rf={ReplicationFactor}, quorum={WriteQuorum})";

        private static IReadOnlyList<string> NormaliseShardIds(IReadOnlyList<string> input)
            => input
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }
}
