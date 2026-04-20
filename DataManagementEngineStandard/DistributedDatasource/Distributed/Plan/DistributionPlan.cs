using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// Versioned, immutable placement spec consumed by
    /// <see cref="DistributedDataSource"/>. Maps each entity name to a
    /// single <see cref="EntityPlacement"/>; mutations always produce a
    /// new instance with <see cref="Version"/> incremented by 1.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The plan does NOT validate that referenced shards exist in the
    /// catalog — that responsibility lives with
    /// <see cref="DistributedDataSource.ApplyDistributionPlan"/> which
    /// raises <see cref="Distributed.Events.PlacementViolationEventArgs"/>
    /// for unknown shard ids.
    /// </para>
    /// <para>
    /// Equality is value-style: two plans with the same name, version,
    /// and placement set compare equal. Phase 11 resharding diffs plans
    /// by enumerating placement deltas.
    /// </para>
    /// </remarks>
    public sealed class DistributionPlan : IEquatable<DistributionPlan>
    {
        /// <summary>Empty plan singleton — no entity placements; safe default for the constructor.</summary>
        public static DistributionPlan Empty { get; } = new DistributionPlan(
            "empty", version: 1, placements: EmptyMap, createdUtc: DateTime.UtcNow);

        /// <summary>
        /// Initialises a new empty plan with the given name. Use
        /// <see cref="DistributionPlanBuilder"/> for non-trivial plans.
        /// </summary>
        public DistributionPlan(string name = "default", int version = 1)
            : this(name, version, EmptyMap, DateTime.UtcNow) { }

        internal DistributionPlan(
            string                                       name,
            int                                          version,
            IReadOnlyDictionary<string, EntityPlacement> placements,
            DateTime                                     createdUtc)
        {
            Name            = string.IsNullOrWhiteSpace(name) ? "default" : name;
            Version         = version <= 0 ? 1 : version;
            EntityPlacements = placements ?? EmptyMap;
            CreatedUtc      = createdUtc;
        }

        /// <summary>Friendly plan name; persisted as the plan group key.</summary>
        public string Name { get; }

        /// <summary>Monotonically-increasing plan version. Phase 11 uses this to detect upgrades.</summary>
        public int Version { get; }

        /// <summary>UTC creation timestamp.</summary>
        public DateTime CreatedUtc { get; }

        /// <summary>Per-entity placements keyed by case-insensitive entity name.</summary>
        public IReadOnlyDictionary<string, EntityPlacement> EntityPlacements { get; }

        /// <summary>Returns <c>true</c> when the plan contains no placements.</summary>
        public bool IsEmpty => EntityPlacements.Count == 0;

        /// <summary>Returns the distinct shard identifiers referenced by every placement in the plan.</summary>
        public IEnumerable<string> ReferencedShardIds()
            => EntityPlacements.Values
                               .SelectMany(p => p.ShardIds)
                               .Distinct(StringComparer.OrdinalIgnoreCase);

        /// <summary>Returns <c>true</c> and the placement when an entry exists for <paramref name="entityName"/>.</summary>
        public bool TryGetPlacement(string entityName, out EntityPlacement placement)
        {
            if (string.IsNullOrWhiteSpace(entityName))
            {
                placement = null;
                return false;
            }
            return EntityPlacements.TryGetValue(entityName, out placement);
        }

        /// <summary>
        /// Returns a new plan that adds (or replaces) the given placement
        /// and increments <see cref="Version"/> by 1. The current instance
        /// is left unchanged.
        /// </summary>
        public DistributionPlan WithEntity(EntityPlacement placement)
        {
            if (placement == null) throw new ArgumentNullException(nameof(placement));

            var next = new Dictionary<string, EntityPlacement>(
                EntityPlacements, StringComparer.OrdinalIgnoreCase)
            {
                [placement.EntityName] = placement
            };
            return new DistributionPlan(Name, Version + 1, next, DateTime.UtcNow);
        }

        /// <summary>
        /// Returns a new plan with <paramref name="entityName"/> removed
        /// and <see cref="Version"/> incremented by 1. Returns the current
        /// instance untouched when the entity was not present.
        /// </summary>
        public DistributionPlan WithoutEntity(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName))                    return this;
            if (!EntityPlacements.ContainsKey(entityName))                return this;

            var next = new Dictionary<string, EntityPlacement>(
                EntityPlacements, StringComparer.OrdinalIgnoreCase);
            next.Remove(entityName);
            return new DistributionPlan(Name, Version + 1, next, DateTime.UtcNow);
        }

        // ── Equality (value-style for plan diffs) ─────────────────────────

        /// <inheritdoc/>
        public bool Equals(DistributionPlan other)
        {
            if (other == null) return false;
            if (!string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)) return false;
            if (Version != other.Version)                                              return false;
            if (EntityPlacements.Count != other.EntityPlacements.Count)                return false;
            foreach (var kv in EntityPlacements)
            {
                if (!other.EntityPlacements.TryGetValue(kv.Key, out var p) || !kv.Value.Equals(p))
                    return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as DistributionPlan);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var h = StringComparer.OrdinalIgnoreCase.GetHashCode(Name) * 397 ^ Version;
                foreach (var kv in EntityPlacements.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                    h = (h * 31) ^ kv.Value.GetHashCode();
                return h;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"DistributionPlan(name={Name}, version={Version}, entities={EntityPlacements.Count})";

        private static readonly IReadOnlyDictionary<string, EntityPlacement> EmptyMap
            = new Dictionary<string, EntityPlacement>(0, StringComparer.OrdinalIgnoreCase);
    }
}
