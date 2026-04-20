using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed.Partitioning
{
    /// <summary>
    /// Consistent-hash partition function. Builds a virtual-slot ring
    /// (default 150 slots per shard) at construction and resolves
    /// each row to the shard owning the first slot ≥ <c>hash(key)</c>.
    /// Uses the shared <see cref="MurmurHash3Helper"/> so its
    /// distribution matches the proxy-tier ConsistentHashRouter
    /// bit-for-bit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ring is immutable once built; rebuild a new function when
    /// the placement shard list changes (typically driven by Phase 11
    /// resharding).
    /// </para>
    /// <para>
    /// Adding one shard to an N-shard ring re-routes only ~1/(N+1)
    /// of the keys — the property that makes this implementation
    /// suitable for online resharding.
    /// </para>
    /// </remarks>
    public sealed class HashPartitionFunction : IPartitionFunction
    {
        /// <summary>Default virtual-slot count per shard. Matches the proxy ConsistentHashRouter.</summary>
        public const int DefaultVirtualSlots = 150;

        /// <summary>Parameter key used to override <see cref="DefaultVirtualSlots"/> via <see cref="PartitionFunctionRef"/>.</summary>
        public const string VirtualSlotsParameterKey = "VirtualSlots";

        private readonly SortedDictionary<uint, string> _ring;
        private readonly uint[]                          _sortedSlots;

        /// <summary>Initialises a new function over <paramref name="shardIds"/>.</summary>
        /// <param name="keyColumns">Columns whose values combine to form the hash input. At least one is required.</param>
        /// <param name="shardIds">Shards that participate in the ring. Must contain at least one entry.</param>
        /// <param name="virtualSlots">Slots per shard; defaults to <see cref="DefaultVirtualSlots"/>.</param>
        public HashPartitionFunction(
            IReadOnlyList<string> keyColumns,
            IReadOnlyList<string> shardIds,
            int                   virtualSlots = DefaultVirtualSlots)
        {
            if (keyColumns == null || keyColumns.Count == 0)
                throw new ArgumentException("HashPartitionFunction requires at least one key column.", nameof(keyColumns));
            if (shardIds == null || shardIds.Count == 0)
                throw new ArgumentException("HashPartitionFunction requires at least one shard id.", nameof(shardIds));
            if (virtualSlots <= 0)
                virtualSlots = DefaultVirtualSlots;

            KeyColumns   = NormaliseKeyColumns(keyColumns);
            VirtualSlots = virtualSlots;

            _ring = new SortedDictionary<uint, string>();
            foreach (var shardId in shardIds)
            {
                if (string.IsNullOrWhiteSpace(shardId))
                    throw new ArgumentException("Shard ids cannot be null or whitespace.", nameof(shardIds));

                for (int v = 0; v < virtualSlots; v++)
                {
                    var slot = MurmurHash3Helper.Hash($"{shardId}:{v}");
                    _ring.TryAdd(slot, shardId);
                }
            }

            _sortedSlots = new uint[_ring.Count];
            int idx = 0;
            foreach (var kv in _ring)
                _sortedSlots[idx++] = kv.Key;
        }

        /// <inheritdoc/>
        public PartitionKind Kind => PartitionKind.Hash;

        /// <inheritdoc/>
        public IReadOnlyList<string> KeyColumns { get; }

        /// <summary>Slots-per-shard count actually used by this instance.</summary>
        public int VirtualSlots { get; }

        /// <summary>Number of slots in the active ring (≈ <see cref="VirtualSlots"/> × shard count, minus collisions).</summary>
        public int RingSize => _sortedSlots.Length;

        /// <inheritdoc/>
        public IReadOnlyList<string> Resolve(PartitionInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (_sortedSlots.Length == 0) return Array.Empty<string>();

            var hash  = MurmurHash3Helper.Hash(BuildKeyString(input));
            var slot  = FindClockwiseSlot(hash);
            return new[] { _ring[slot] };
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private string BuildKeyString(PartitionInput input)
        {
            // Concatenate key values with a separator that cannot appear in
            // PartitionKeyCoercer.Stringify outputs to guarantee uniqueness.
            var sb = new StringBuilder();
            for (int i = 0; i < KeyColumns.Count; i++)
            {
                if (i > 0) sb.Append('\u001f');     // ASCII unit separator
                sb.Append(PartitionKeyCoercer.Stringify(input.GetValue(KeyColumns[i])));
            }
            return sb.ToString();
        }

        private uint FindClockwiseSlot(uint hash)
        {
            // Binary search for the first slot ≥ hash; wrap to slot[0] when none found.
            int lo = 0, hi = _sortedSlots.Length - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                if (_sortedSlots[mid] < hash) lo = mid + 1;
                else hi = mid - 1;
            }
            return lo < _sortedSlots.Length ? _sortedSlots[lo] : _sortedSlots[0];
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
}
