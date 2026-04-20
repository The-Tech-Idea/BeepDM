using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed.Placement
{
    /// <summary>
    /// Runtime, fast-lookup view of a <see cref="DistributionPlan"/>.
    /// Splits placements into <i>exact</i> and <i>prefix</i> buckets so
    /// resolution is O(1) for exact matches and O(prefix-count) for the
    /// longest-prefix scan. Inspired by the
    /// <c>Proxy/EntityAffinityMap</c> resolver.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Prefix patterns are encoded by an entity name ending in
    /// <c>*</c> (case-insensitive). For example, an
    /// <see cref="EntityPlacement"/> with <c>EntityName = "Audit_*"</c>
    /// matches every entity whose name starts with <c>"Audit_"</c>; the
    /// stored prefix key is <c>"Audit_"</c>.
    /// </para>
    /// <para>
    /// The map is immutable once <see cref="FromPlan"/> returns; the
    /// dictionaries are concurrent only to allow safe enumeration from
    /// multiple readers without re-allocating a snapshot per resolve.
    /// </para>
    /// </remarks>
    public sealed class EntityPlacementMap
    {
        /// <summary>Prefix sentinel: an entity name ending in this character is treated as a prefix pattern.</summary>
        public const char PrefixWildcard = '*';

        private readonly ConcurrentDictionary<string, EntityPlacement> _exact;
        private readonly ConcurrentDictionary<string, EntityPlacement> _prefix;

        private EntityPlacementMap(
            ConcurrentDictionary<string, EntityPlacement> exact,
            ConcurrentDictionary<string, EntityPlacement> prefix,
            DistributionPlan                              source)
        {
            _exact = exact;
            _prefix = prefix;
            SourcePlan = source;
        }

        /// <summary>Plan this map was built from. Useful for diagnostics; do not mutate.</summary>
        public DistributionPlan SourcePlan { get; }

        /// <summary>Number of exact-match placements registered.</summary>
        public int ExactCount => _exact.Count;

        /// <summary>Number of prefix-match placements registered.</summary>
        public int PrefixCount => _prefix.Count;

        /// <summary>An empty map (no placements). Always returns <see cref="PlacementMatchKind.Unmapped"/>.</summary>
        public static EntityPlacementMap Empty { get; } = new EntityPlacementMap(
            new ConcurrentDictionary<string, EntityPlacement>(StringComparer.OrdinalIgnoreCase),
            new ConcurrentDictionary<string, EntityPlacement>(StringComparer.OrdinalIgnoreCase),
            DistributionPlan.Empty);

        /// <summary>
        /// Builds a map from <paramref name="plan"/>. Placements whose
        /// <see cref="EntityPlacement.EntityName"/> ends in <c>*</c> are
        /// stored as prefix entries (with the <c>*</c> stripped); all
        /// others are stored as exact entries.
        /// </summary>
        public static EntityPlacementMap FromPlan(DistributionPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var exact  = new ConcurrentDictionary<string, EntityPlacement>(StringComparer.OrdinalIgnoreCase);
            var prefix = new ConcurrentDictionary<string, EntityPlacement>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in plan.EntityPlacements)
            {
                var name = kv.Value.EntityName;
                if (name.Length > 1 && name[name.Length - 1] == PrefixWildcard)
                {
                    var key = name.Substring(0, name.Length - 1);
                    if (key.Length > 0)
                        prefix[key] = kv.Value;
                }
                else
                {
                    exact[name] = kv.Value;
                }
            }

            return new EntityPlacementMap(exact, prefix, plan);
        }

        /// <summary>
        /// Looks up the placement for <paramref name="entityName"/>.
        /// Returns <see cref="PlacementMatchKind.Exact"/> first, then
        /// <see cref="PlacementMatchKind.Prefix"/> (longest prefix
        /// wins), and finally <see cref="PlacementMatchKind.Unmapped"/>
        /// when nothing matched.
        /// </summary>
        /// <param name="entityName">Entity to resolve.</param>
        /// <param name="placement">Matched placement; <c>null</c> when unmapped.</param>
        /// <returns>How the match was made (or <see cref="PlacementMatchKind.Unmapped"/>).</returns>
        public PlacementMatchKind Match(string entityName, out EntityPlacement placement)
        {
            placement = null;
            if (string.IsNullOrWhiteSpace(entityName)) return PlacementMatchKind.Unmapped;

            if (_exact.TryGetValue(entityName, out placement))
                return PlacementMatchKind.Exact;

            EntityPlacement bestMatch    = null;
            string          bestPrefix   = null;
            foreach (var kv in _prefix)
            {
                if (entityName.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase) &&
                    (bestPrefix == null || kv.Key.Length > bestPrefix.Length))
                {
                    bestPrefix = kv.Key;
                    bestMatch  = kv.Value;
                }
            }

            if (bestMatch != null)
            {
                placement = bestMatch;
                return PlacementMatchKind.Prefix;
            }

            return PlacementMatchKind.Unmapped;
        }

        /// <summary>Returns every placement currently registered (exact + prefix), ordered by entity name.</summary>
        public IReadOnlyList<EntityPlacement> Snapshot()
            => _exact.Values
                     .Concat(_prefix.Values)
                     .OrderBy(p => p.EntityName, StringComparer.OrdinalIgnoreCase)
                     .ToList();
    }
}
