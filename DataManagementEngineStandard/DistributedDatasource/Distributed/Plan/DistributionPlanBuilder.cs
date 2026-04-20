using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Distributed.Plan
{
    /// <summary>
    /// Fluent builder for <see cref="DistributionPlan"/>. Mutations
    /// accumulate in the builder; <see cref="Build"/> produces an
    /// immutable plan whose <see cref="DistributionPlan.Version"/> is
    /// the supplied <c>baseVersion + 1</c> (or <c>1</c> when no base
    /// version was provided). Use the <see cref="From(DistributionPlan)"/>
    /// helper to derive a new plan from an existing one.
    /// </summary>
    /// <remarks>
    /// The builder is NOT thread-safe — construct it on one thread,
    /// configure, build, and then publish the resulting plan.
    /// </remarks>
    public sealed class DistributionPlanBuilder
    {
        private readonly Dictionary<string, EntityPlacement> _placements
            = new Dictionary<string, EntityPlacement>(StringComparer.OrdinalIgnoreCase);

        private string _name;
        private int    _baseVersion;

        /// <summary>Creates a new empty builder for a plan with the given name.</summary>
        public DistributionPlanBuilder(string name = "default")
        {
            _name = string.IsNullOrWhiteSpace(name) ? "default" : name;
            _baseVersion = 0;
        }

        /// <summary>Returns a builder seeded from an existing plan; <see cref="Build"/> bumps the version.</summary>
        public static DistributionPlanBuilder From(DistributionPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var b = new DistributionPlanBuilder(plan.Name)
            {
                _baseVersion = plan.Version
            };
            foreach (var kv in plan.EntityPlacements)
                b._placements[kv.Key] = kv.Value;
            return b;
        }

        /// <summary>Overrides the plan name.</summary>
        public DistributionPlanBuilder Named(string name)
        {
            if (!string.IsNullOrWhiteSpace(name)) _name = name;
            return this;
        }

        /// <summary>Routes <paramref name="entityName"/> to a single shard (Routed mode).</summary>
        public DistributionPlanBuilder RouteEntity(string entityName, string shardId)
        {
            return Place(new EntityPlacement(
                entityName,
                DistributionMode.Routed,
                new[] { shardId },
                PartitionFunctionRef.None));
        }

        /// <summary>Shards <paramref name="entityName"/> across the given shards using a partition function.</summary>
        public DistributionPlanBuilder ShardEntity(
            string                entityName,
            IEnumerable<string>   shardIds,
            PartitionFunctionRef  partitionFunction,
            int                   replicationFactor = 1,
            int                   writeQuorum       = 0)
        {
            if (partitionFunction == null) throw new ArgumentNullException(nameof(partitionFunction));
            return Place(new EntityPlacement(
                entityName,
                DistributionMode.Sharded,
                shardIds?.ToArray() ?? Array.Empty<string>(),
                partitionFunction,
                replicationFactor,
                writeQuorum));
        }

        /// <summary>Replicates <paramref name="entityName"/> across the given shards.</summary>
        public DistributionPlanBuilder ReplicateEntity(
            string              entityName,
            IEnumerable<string> shardIds,
            int                 replicationFactor = 0,
            int                 writeQuorum       = 0)
        {
            var ids = shardIds?.ToArray() ?? Array.Empty<string>();
            var rf  = replicationFactor <= 0 ? Math.Max(1, ids.Length) : replicationFactor;
            return Place(new EntityPlacement(
                entityName,
                DistributionMode.Replicated,
                ids,
                PartitionFunctionRef.None,
                rf,
                writeQuorum));
        }

        /// <summary>Broadcasts <paramref name="entityName"/> to every supplied shard.</summary>
        public DistributionPlanBuilder BroadcastEntity(
            string              entityName,
            IEnumerable<string> shardIds,
            int                 writeQuorum = 0)
        {
            return Place(new EntityPlacement(
                entityName,
                DistributionMode.Broadcast,
                shardIds?.ToArray() ?? Array.Empty<string>(),
                PartitionFunctionRef.None,
                replicationFactor: 1,
                writeQuorum: writeQuorum));
        }

        /// <summary>Removes the placement for <paramref name="entityName"/> if present.</summary>
        public DistributionPlanBuilder RemoveEntity(string entityName)
        {
            if (!string.IsNullOrWhiteSpace(entityName)) _placements.Remove(entityName);
            return this;
        }

        /// <summary>Adds (or replaces) a fully-formed <see cref="EntityPlacement"/>.</summary>
        public DistributionPlanBuilder Place(EntityPlacement placement)
        {
            if (placement == null) throw new ArgumentNullException(nameof(placement));
            _placements[placement.EntityName] = placement;
            return this;
        }

        /// <summary>Materialises the immutable plan; sets <see cref="DistributionPlan.Version"/> to <c>baseVersion + 1</c>.</summary>
        public DistributionPlan Build()
        {
            var snapshot = new Dictionary<string, EntityPlacement>(_placements, StringComparer.OrdinalIgnoreCase);
            return new DistributionPlan(_name, _baseVersion + 1, snapshot, DateTime.UtcNow);
        }
    }
}
