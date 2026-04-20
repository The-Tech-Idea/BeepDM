using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Partitioning
{
    /// <summary>
    /// Input bundle passed to <see cref="IPartitionFunction.Resolve(PartitionInput)"/>.
    /// Carries the entity name being routed and the values for the
    /// declared partition-key columns. Phase 04 deliberately keeps
    /// this surface small — the Phase 05 router is responsible for
    /// extracting these values from a request payload.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instances are immutable and safe to share across threads. Use
    /// the <see cref="GetValue"/> accessor instead of indexing
    /// <see cref="KeyValues"/> directly so that case-insensitive
    /// lookups remain consistent regardless of how the dictionary was
    /// constructed by the caller.
    /// </para>
    /// </remarks>
    public sealed class PartitionInput
    {
        private static readonly IReadOnlyDictionary<string, object> EmptyValues
            = new Dictionary<string, object>(0, StringComparer.OrdinalIgnoreCase);

        /// <summary>Initialises a new input.</summary>
        /// <param name="entityName">Logical entity (table) being routed; required.</param>
        /// <param name="keyValues">Key column → value map. Case-insensitive lookups recommended.</param>
        public PartitionInput(
            string                              entityName,
            IReadOnlyDictionary<string, object> keyValues)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));

            EntityName = entityName;
            KeyValues  = keyValues ?? EmptyValues;
        }

        /// <summary>Logical entity (table) the partition function is routing for.</summary>
        public string EntityName { get; }

        /// <summary>Key column → value map supplied by the caller.</summary>
        public IReadOnlyDictionary<string, object> KeyValues { get; }

        /// <summary>
        /// Returns the value for <paramref name="column"/> or <c>null</c>
        /// when the column is missing. Lookups are case-insensitive
        /// regardless of the underlying dictionary's comparer.
        /// </summary>
        public object GetValue(string column)
        {
            if (string.IsNullOrEmpty(column) || KeyValues.Count == 0)
                return null;

            if (KeyValues.TryGetValue(column, out var direct))
                return direct;

            // Fallback: scan the dictionary case-insensitively when the supplied
            // dictionary uses a case-sensitive comparer.
            foreach (var kv in KeyValues)
            {
                if (string.Equals(kv.Key, column, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }
            return null;
        }
    }
}
