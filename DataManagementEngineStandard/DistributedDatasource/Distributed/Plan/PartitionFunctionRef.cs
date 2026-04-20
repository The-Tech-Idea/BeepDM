using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Distributed.Plan
{
    /// <summary>
    /// Persisted reference to a row-level partition function attached to a
    /// <see cref="EntityPlacement"/>. The actual partition function
    /// implementations land in Phase 04
    /// (<c>04-phase4-partition-functions-row-level.md</c>); the reference
    /// here is provider-neutral so that plans can be persisted, diffed,
    /// and loaded without depending on the function classes themselves.
    /// </summary>
    /// <remarks>
    /// The reference is immutable — equality / hash codes are computed
    /// from <see cref="Kind"/>, <see cref="KeyColumns"/>, and
    /// <see cref="Parameters"/> so that Phase 11 plan diffing can detect
    /// partition-function changes that require resharding.
    /// </remarks>
    public sealed class PartitionFunctionRef : IEquatable<PartitionFunctionRef>
    {
        /// <summary>Sentinel "no partition function" instance used by
        /// <see cref="DistributionMode.Routed"/>, <see cref="DistributionMode.Replicated"/>,
        /// and <see cref="DistributionMode.Broadcast"/> placements.</summary>
        public static readonly PartitionFunctionRef None
            = new PartitionFunctionRef(PartitionKind.None, Array.Empty<string>(), null);

        /// <summary>Initialises a new partition function reference.</summary>
        /// <param name="kind">Function category; see <see cref="PartitionKind"/>.</param>
        /// <param name="keyColumns">Ordered key columns. Must not be <c>null</c>; may be empty for <see cref="PartitionKind.None"/>.</param>
        /// <param name="parameters">Optional name/value parameters (e.g. bucket counts, range boundaries).</param>
        public PartitionFunctionRef(
            PartitionKind                       kind,
            IReadOnlyList<string>               keyColumns,
            IReadOnlyDictionary<string, string> parameters)
        {
            if (keyColumns == null) throw new ArgumentNullException(nameof(keyColumns));
            if (kind != PartitionKind.None && keyColumns.Count == 0)
                throw new ArgumentException("Key columns are required for non-None partition kinds.", nameof(keyColumns));

            Kind       = kind;
            KeyColumns = keyColumns;
            Parameters = parameters ?? EmptyParams;
        }

        /// <summary>Function category.</summary>
        public PartitionKind Kind { get; }

        /// <summary>Ordered key-column names used to derive the routing input.</summary>
        public IReadOnlyList<string> KeyColumns { get; }

        /// <summary>Provider-neutral parameter bag (string-typed). Persisted as JSON.</summary>
        public IReadOnlyDictionary<string, string> Parameters { get; }

        // ── Equality (value-style for plan diffs) ─────────────────────────

        /// <inheritdoc/>
        public bool Equals(PartitionFunctionRef other)
        {
            if (other == null)             return false;
            if (Kind  != other.Kind)        return false;
            if (KeyColumns.Count != other.KeyColumns.Count) return false;
            for (var i = 0; i < KeyColumns.Count; i++)
            {
                if (!string.Equals(KeyColumns[i], other.KeyColumns[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            if (Parameters.Count != other.Parameters.Count) return false;
            foreach (var kv in Parameters)
            {
                if (!other.Parameters.TryGetValue(kv.Key, out var v) ||
                    !string.Equals(v, kv.Value, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as PartitionFunctionRef);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var h = (int)Kind * 397;
                foreach (var col in KeyColumns)
                    h = (h * 31) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(col ?? string.Empty);
                foreach (var kv in Parameters.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                {
                    h = (h * 31) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(kv.Key ?? string.Empty);
                    h = (h * 31) ^ StringComparer.Ordinal.GetHashCode(kv.Value ?? string.Empty);
                }
                return h;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"{Kind}({string.Join(",", KeyColumns)};{Parameters.Count} params)";

        private static readonly IReadOnlyDictionary<string, string> EmptyParams
            = new Dictionary<string, string>(0, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Partition-function category persisted with a
    /// <see cref="PartitionFunctionRef"/>. Phase 04 attaches concrete
    /// implementations behind these tags through a registry.
    /// </summary>
    public enum PartitionKind
    {
        /// <summary>No row-level partitioning (entity-level placement).</summary>
        None      = 0,

        /// <summary>Hash-modulo / consistent-hash partitioning.</summary>
        Hash      = 1,

        /// <summary>Range partitioning (inclusive lower, exclusive upper).</summary>
        Range     = 2,

        /// <summary>Explicit-list partitioning (value → shard).</summary>
        List      = 3,

        /// <summary>Composite (multi-key) partitioning.</summary>
        Composite = 4
    }
}
