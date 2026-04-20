using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Distributed.Plan;

namespace TheTechIdea.Beep.Distributed.Partitioning
{
    /// <summary>
    /// Discrete-value partition function. Maps each enumerated key
    /// value to an explicit shard id (e.g. <c>Region = "EU" → "shard-eu"</c>).
    /// Falls back to <see cref="DefaultShardId"/> for values not in
    /// the map; returns an empty list when no default is set.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lookups use <see cref="PartitionKeyCoercer.AreEqual"/> so a
    /// stored string <c>"42"</c> can match an int <c>42</c> coming
    /// from a row. The original mapping is preserved verbatim for
    /// diagnostics via <see cref="ValueMap"/>.
    /// </para>
    /// </remarks>
    public sealed class ListPartitionFunction : IPartitionFunction
    {
        private readonly KeyValuePair<object, string>[] _entries;

        /// <summary>Initialises a new list function.</summary>
        /// <param name="keyColumns">Key columns (only the first is consulted).</param>
        /// <param name="valueMap">Value → shard id mapping; required and non-empty unless <paramref name="defaultShardId"/> is supplied.</param>
        /// <param name="defaultShardId">Fallback shard for values not present in <paramref name="valueMap"/>; <c>null</c> disables the fallback.</param>
        public ListPartitionFunction(
            IReadOnlyList<string>            keyColumns,
            IReadOnlyDictionary<object, string> valueMap,
            string                            defaultShardId = null)
        {
            if (keyColumns == null || keyColumns.Count == 0)
                throw new ArgumentException("ListPartitionFunction requires at least one key column.", nameof(keyColumns));
            if ((valueMap == null || valueMap.Count == 0) && string.IsNullOrWhiteSpace(defaultShardId))
                throw new ArgumentException("ListPartitionFunction requires either a non-empty value map or a default shard id.", nameof(valueMap));

            KeyColumns     = NormaliseKeyColumns(keyColumns);
            DefaultShardId = string.IsNullOrWhiteSpace(defaultShardId) ? null : defaultShardId;

            if (valueMap == null)
            {
                _entries = Array.Empty<KeyValuePair<object, string>>();
                ValueMap = new Dictionary<object, string>(0);
            }
            else
            {
                _entries = new KeyValuePair<object, string>[valueMap.Count];
                int idx = 0;
                foreach (var kv in valueMap)
                {
                    if (string.IsNullOrWhiteSpace(kv.Value))
                        throw new ArgumentException(
                            $"List value '{kv.Key}' maps to a null/empty shard id.", nameof(valueMap));
                    _entries[idx++] = new KeyValuePair<object, string>(kv.Key, kv.Value);
                }
                ValueMap = valueMap;
            }
        }

        /// <inheritdoc/>
        public PartitionKind Kind => PartitionKind.List;

        /// <inheritdoc/>
        public IReadOnlyList<string> KeyColumns { get; }

        /// <summary>Original value-to-shard map. Exposed for diagnostics; do not mutate.</summary>
        public IReadOnlyDictionary<object, string> ValueMap { get; }

        /// <summary>Fallback shard id used when no value matches; <c>null</c> when no fallback is configured.</summary>
        public string DefaultShardId { get; }

        /// <inheritdoc/>
        public IReadOnlyList<string> Resolve(PartitionInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var key = input.GetValue(KeyColumns[0]);

            for (int i = 0; i < _entries.Length; i++)
            {
                if (PartitionKeyCoercer.AreEqual(_entries[i].Key, key))
                    return new[] { _entries[i].Value };
            }

            return DefaultShardId != null
                ? new[] { DefaultShardId }
                : Array.Empty<string>();
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
