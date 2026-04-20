using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed.Partitioning
{
    /// <summary>
    /// Range-based partition function. Routes a row to the shard
    /// whose half-open boundary <c>[lo, hi)</c> contains the key
    /// value. Boundaries are sorted at construction; resolution uses
    /// binary search.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The boundary list MUST be supplied in ascending order of
    /// <see cref="RangePartitionBoundary.MaxExclusive"/>. The constructor
    /// validates the ordering and contiguousness so callers cannot
    /// accidentally introduce gaps.
    /// </para>
    /// <para>
    /// A boundary with <c>MaxExclusive = null</c> represents the
    /// open-ended upper segment (matches every key &gt;= the previous
    /// boundary's max). Exactly one such boundary is allowed and it
    /// must be the last entry.
    /// </para>
    /// </remarks>
    public sealed class RangePartitionFunction : IPartitionFunction
    {
        private readonly RangePartitionBoundary[] _boundaries;

        /// <summary>Initialises a new range function.</summary>
        /// <param name="keyColumns">Key columns whose values are compared against the boundaries.</param>
        /// <param name="boundaries">Sorted, contiguous boundaries. The last entry may have <c>MaxExclusive = null</c> for the open-ended segment.</param>
        public RangePartitionFunction(
            IReadOnlyList<string>            keyColumns,
            IReadOnlyList<RangePartitionBoundary> boundaries)
        {
            if (keyColumns == null || keyColumns.Count == 0)
                throw new ArgumentException("RangePartitionFunction requires at least one key column.", nameof(keyColumns));
            if (boundaries == null || boundaries.Count == 0)
                throw new ArgumentException("RangePartitionFunction requires at least one boundary.", nameof(boundaries));

            KeyColumns  = NormaliseKeyColumns(keyColumns);
            _boundaries = ValidateAndCopy(boundaries);
        }

        /// <inheritdoc/>
        public PartitionKind Kind => PartitionKind.Range;

        /// <inheritdoc/>
        public IReadOnlyList<string> KeyColumns { get; }

        /// <summary>Active sorted boundary list. Exposed for diagnostics; do not mutate.</summary>
        public IReadOnlyList<RangePartitionBoundary> Boundaries => _boundaries;

        /// <inheritdoc/>
        public IReadOnlyList<string> Resolve(PartitionInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            // Range functions use the first key column only.
            var key = input.GetValue(KeyColumns[0]);

            // Binary search for the first boundary whose MaxExclusive > key.
            // Open-ended boundaries (MaxExclusive == null) compare as +infinity
            // so they only become the answer when no other boundary matches.
            int lo = 0, hi = _boundaries.Length - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                if (CompareKeyToMax(key, _boundaries[mid]) < 0)
                    hi = mid - 1;
                else
                    lo = mid + 1;
            }

            if (lo >= _boundaries.Length) return Array.Empty<string>();
            return new[] { _boundaries[lo].ShardId };
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static int CompareKeyToMax(object key, RangePartitionBoundary boundary)
        {
            if (boundary.MaxExclusive == null) return -1; // open-ended → key always < +∞
            return PartitionKeyCoercer.Compare(key, boundary.MaxExclusive);
        }

        private static RangePartitionBoundary[] ValidateAndCopy(IReadOnlyList<RangePartitionBoundary> boundaries)
        {
            var copy = new RangePartitionBoundary[boundaries.Count];
            object previousMax = null;

            for (int i = 0; i < boundaries.Count; i++)
            {
                var b = boundaries[i] ?? throw new ArgumentException(
                    $"Boundary at index {i} is null.", nameof(boundaries));

                if (string.IsNullOrWhiteSpace(b.ShardId))
                    throw new ArgumentException(
                        $"Boundary at index {i} has a null/empty ShardId.", nameof(boundaries));

                bool isOpenEnded = b.MaxExclusive == null;
                if (isOpenEnded && i != boundaries.Count - 1)
                    throw new ArgumentException(
                        "Only the last boundary may have MaxExclusive = null.", nameof(boundaries));

                if (i > 0 && !isOpenEnded && previousMax != null &&
                    PartitionKeyCoercer.Compare(b.MaxExclusive, previousMax) <= 0)
                {
                    throw new ArgumentException(
                        $"Boundary at index {i} (MaxExclusive='{b.MaxExclusive}') is not strictly greater than the previous boundary.",
                        nameof(boundaries));
                }

                copy[i]     = b;
                previousMax = b.MaxExclusive;
            }
            return copy;
        }

        private static IReadOnlyList<string> NormaliseKeyColumns(IReadOnlyList<string> keyColumns)
        {
            var copy = new string[keyColumns.Count];
            for (int i = 0; i < keyColumns.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(keyColumns[i]))
                    throw new ArgumentException("Key column names cannot be null or whitespace.", nameof(keyColumns));
                copy[i] = keyColumns[i].Trim();
            }
            return copy;
        }
    }

    /// <summary>
    /// One half-open range boundary used by
    /// <see cref="RangePartitionFunction"/>. A boundary owns every
    /// key strictly less than <see cref="MaxExclusive"/> (and greater
    /// than or equal to the previous boundary's max).
    /// </summary>
    public sealed class RangePartitionBoundary
    {
        /// <summary>Initialises a new boundary.</summary>
        /// <param name="maxExclusive">Upper bound (exclusive). <c>null</c> denotes the open-ended segment.</param>
        /// <param name="shardId">Target shard id; required.</param>
        public RangePartitionBoundary(object maxExclusive, string shardId)
        {
            if (string.IsNullOrWhiteSpace(shardId))
                throw new ArgumentException("ShardId cannot be null or whitespace.", nameof(shardId));
            MaxExclusive = maxExclusive;
            ShardId      = shardId;
        }

        /// <summary>Upper bound (exclusive); <c>null</c> means +∞.</summary>
        public object MaxExclusive { get; }

        /// <summary>Target shard for keys in this boundary.</summary>
        public string ShardId { get; }

        /// <inheritdoc/>
        public override string ToString()
            => $"[..,{MaxExclusive ?? "+∞"}) -> {ShardId}";
    }
}
