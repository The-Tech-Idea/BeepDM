using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed.Partitioning
{
    /// <summary>
    /// Combines two or more inner <see cref="IPartitionFunction"/>
    /// instances. Each inner function resolves the input
    /// independently; the composite returns the deduplicated union
    /// of every inner result. Useful for tenant + key sharding where
    /// a row should land on every shard chosen by either function.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Output ordering is intentionally undefined; documented as
    /// "union, deduplicated, no order guarantee" so callers must not
    /// depend on the order in which shard ids are returned.
    /// </para>
    /// <para>
    /// The composite's <see cref="KeyColumns"/> is the deduplicated
    /// union of the inner functions' columns, in the order they were
    /// first observed.
    /// </para>
    /// </remarks>
    public sealed class CompositePartitionFunction : IPartitionFunction
    {
        private readonly IPartitionFunction[] _inner;

        /// <summary>Initialises a new composite from <paramref name="inner"/> functions.</summary>
        /// <param name="inner">Two or more inner functions; null entries are rejected.</param>
        public CompositePartitionFunction(IReadOnlyList<IPartitionFunction> inner)
        {
            if (inner == null || inner.Count < 2)
                throw new ArgumentException(
                    "CompositePartitionFunction requires at least two inner functions.",
                    nameof(inner));

            _inner = new IPartitionFunction[inner.Count];
            var keyColumns = new List<string>();
            var seen       = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < inner.Count; i++)
            {
                _inner[i] = inner[i] ?? throw new ArgumentException(
                    $"Inner function at index {i} is null.", nameof(inner));

                foreach (var col in _inner[i].KeyColumns)
                {
                    if (seen.Add(col)) keyColumns.Add(col);
                }
            }

            KeyColumns = keyColumns;
        }

        /// <inheritdoc/>
        public PartitionKind Kind => PartitionKind.Composite;

        /// <inheritdoc/>
        public IReadOnlyList<string> KeyColumns { get; }

        /// <summary>Active inner functions. Exposed for diagnostics; do not mutate.</summary>
        public IReadOnlyList<IPartitionFunction> InnerFunctions => _inner;

        /// <inheritdoc/>
        public IReadOnlyList<string> Resolve(PartitionInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            // Preserve insertion order for stable diagnostics while still deduplicating.
            var seen     = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var combined = new List<string>();

            for (int i = 0; i < _inner.Length; i++)
            {
                var partial = _inner[i].Resolve(input) ?? Array.Empty<string>();
                for (int j = 0; j < partial.Count; j++)
                {
                    var shardId = partial[j];
                    if (!string.IsNullOrEmpty(shardId) && seen.Add(shardId))
                        combined.Add(shardId);
                }
            }

            return combined;
        }
    }
}
