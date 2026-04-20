using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Distributed.Placement;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Distributed.Routing
{
    /// <summary>
    /// <see cref="ShardRouter"/> partial — partition-key extractors
    /// for the three call shapes used across the
    /// <see cref="DataBase.IDataSource"/> surface:
    /// <c>List&lt;AppFilter&gt;</c> (query filters), positional PK
    /// arrays (<c>GetEntity(string, object[])</c>), and entity
    /// instance writes
    /// (<c>InsertEntity</c>/<c>UpdateEntity</c>/<c>DeleteEntity</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// All extractors return <c>null</c> when no key column has a
    /// usable value — the router treats <c>null</c> as "no key" and
    /// either scatters or rejects the call depending on direction
    /// and the <c>AllowScatterWrite</c> option.
    /// </para>
    /// <para>
    /// Reflection accessors are cached per <see cref="Type"/> via
    /// <see cref="_propertyCache"/> so per-call cost is a dictionary
    /// lookup once the first record of a type has been routed.
    /// </para>
    /// </remarks>
    public sealed partial class ShardRouter
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>>
            _propertyCache = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>>();

        // ── 1. AppFilter list (query reads) ───────────────────────────────

        /// <summary>
        /// Scans <paramref name="filters"/> for entries that target
        /// any of the placement's partition-key columns. Supports
        /// <c>=</c> (single value) and <c>IN</c> (comma-separated)
        /// operators; other operators are ignored.
        /// </summary>
        internal IReadOnlyDictionary<string, object> TryExtractFromFilters(
            PlacementResolution placement,
            List<AppFilter>     filters,
            EntityStructure     structure)
        {
            var keyColumns = placement?.Source?.PartitionFunction?.KeyColumns;
            if (keyColumns == null || keyColumns.Count == 0) return null;
            if (filters == null || filters.Count == 0)       return null;

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var keyColumn in keyColumns)
            {
                var match = filters.FirstOrDefault(f => f != null
                    && string.Equals(f.FieldName, keyColumn, StringComparison.OrdinalIgnoreCase));
                if (match == null) continue;

                if (TryParseFilterValue(match, structure, keyColumn, out var value))
                    result[keyColumn] = value;
            }

            return result.Count == 0 ? null : result;
        }

        private static bool TryParseFilterValue(
            AppFilter        filter,
            EntityStructure  structure,
            string           keyColumn,
            out object       value)
        {
            value = null;
            if (string.IsNullOrEmpty(filter.FilterValue)) return false;

            var op = (filter.Operator ?? string.Empty).Trim();

            // IN-list: comma-separated values; emit a List<object> so the router
            // recognises it as a multi-value match in TryGetMultiValues.
            if (string.Equals(op, "IN", StringComparison.OrdinalIgnoreCase))
            {
                var parts = filter.FilterValue
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .Select(s => (object)s)
                    .ToList();
                if (parts.Count == 0) return false;
                value = parts;
                return true;
            }

            // Equality (the default for `=`, `==`, or empty operators).
            if (op.Length == 0
                || op == "="
                || op == "=="
                || string.Equals(op, "EQ", StringComparison.OrdinalIgnoreCase))
            {
                value = CoerceFilterValue(filter, structure, keyColumn);
                return value != null || !string.IsNullOrEmpty(filter.FilterValue);
            }

            // Other operators (>, <, BETWEEN, LIKE...) cannot pin a single shard;
            // treat as "no key" so the router scatters or rejects.
            return false;
        }

        private static object CoerceFilterValue(
            AppFilter       filter,
            EntityStructure structure,
            string          keyColumn)
        {
            // Prefer the explicit field type when supplied on the filter; otherwise
            // fall back to the entity structure's column metadata.
            var targetType = filter.FieldType
                          ?? LookupColumnType(structure, keyColumn);
            if (targetType == null || targetType == typeof(string))
                return filter.FilterValue;

            try
            {
                var nonNull = Nullable.GetUnderlyingType(targetType) ?? targetType;
                return Convert.ChangeType(filter.FilterValue, nonNull, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                // Coercion failure → fall back to the raw string;
                // PartitionKeyCoercer will best-effort compare it.
                return filter.FilterValue;
            }
        }

        private static Type LookupColumnType(EntityStructure structure, string columnName)
        {
            if (structure?.Fields == null || structure.Fields.Count == 0) return null;

            var field = structure.Fields.FirstOrDefault(f => f != null
                && string.Equals(f.FieldName, columnName, StringComparison.OrdinalIgnoreCase));
            if (field == null || string.IsNullOrEmpty(field.Fieldtype)) return null;

            return Type.GetType(field.Fieldtype, throwOnError: false, ignoreCase: true);
        }

        // ── 2. Positional PK arrays (GetEntity(name, object[] keys)) ─────

        /// <summary>
        /// Maps a positional key array onto the entity's primary-key
        /// columns (in declaration order) and then onto the
        /// placement's partition-key columns. Returns <c>null</c>
        /// when no PK metadata is available or the array is empty.
        /// </summary>
        internal IReadOnlyDictionary<string, object> TryExtractFromPositionalKeys(
            PlacementResolution placement,
            object[]            positionalKeys,
            EntityStructure     structure)
        {
            var keyColumns = placement?.Source?.PartitionFunction?.KeyColumns;
            if (keyColumns == null || keyColumns.Count == 0)            return null;
            if (positionalKeys == null || positionalKeys.Length == 0)   return null;
            if (structure?.PrimaryKeys == null || structure.PrimaryKeys.Count == 0) return null;

            // Map PK ordinal → field name in declaration order.
            var pkByPosition = new Dictionary<int, string>(structure.PrimaryKeys.Count);
            for (int i = 0; i < structure.PrimaryKeys.Count; i++)
            {
                var pkName = structure.PrimaryKeys[i]?.FieldName;
                if (!string.IsNullOrEmpty(pkName)) pkByPosition[i] = pkName;
            }
            if (pkByPosition.Count == 0) return null;

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < positionalKeys.Length && i < structure.PrimaryKeys.Count; i++)
            {
                if (!pkByPosition.TryGetValue(i, out var fieldName)) continue;
                if (!keyColumns.Any(c => string.Equals(c, fieldName, StringComparison.OrdinalIgnoreCase))) continue;
                result[fieldName] = positionalKeys[i];
            }

            return result.Count == 0 ? null : result;
        }

        // ── 3. Entity instance writes (Insert/Update/Delete) ──────────────

        /// <summary>
        /// Reads the partition-key columns from <paramref name="record"/>
        /// using a cached reflection accessor (or a dictionary cast
        /// when the record implements
        /// <see cref="IDictionary{TKey,TValue}"/>).
        /// </summary>
        internal IReadOnlyDictionary<string, object> TryExtractFromEntityInstance(
            PlacementResolution placement,
            object              record)
        {
            var keyColumns = placement?.Source?.PartitionFunction?.KeyColumns;
            if (keyColumns == null || keyColumns.Count == 0) return null;
            if (record == null) return null;

            // Fast paths for the two most common shapes in BeepDM.
            if (record is IDictionary<string, object> typed)
                return ProjectFromDictionary(typed, keyColumns);

            if (record is IDictionary nonGeneric)
                return ProjectFromNonGenericDictionary(nonGeneric, keyColumns);

            // POCO / anonymous → cached reflection accessor.
            var accessor = _propertyCache.GetOrAdd(record.GetType(), BuildAccessor);
            var result   = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var keyColumn in keyColumns)
            {
                if (!accessor.TryGetValue(keyColumn, out var prop)) continue;
                try
                {
                    var value = prop.GetValue(record);
                    if (value != null) result[keyColumn] = value;
                }
                catch
                {
                    // Defensive: a buggy property accessor should not crash routing.
                }
            }

            return result.Count == 0 ? null : result;
        }

        private static IReadOnlyDictionary<string, object> ProjectFromDictionary(
            IDictionary<string, object> source,
            IReadOnlyList<string>       keyColumns)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in keyColumns)
            {
                if (source.TryGetValue(col, out var value) && value != null)
                {
                    result[col] = value;
                    continue;
                }
                // Case-insensitive fallback when the source dictionary uses a different comparer.
                foreach (var kv in source)
                {
                    if (string.Equals(kv.Key, col, StringComparison.OrdinalIgnoreCase) && kv.Value != null)
                    {
                        result[col] = kv.Value;
                        break;
                    }
                }
            }
            return result.Count == 0 ? null : result;
        }

        private static IReadOnlyDictionary<string, object> ProjectFromNonGenericDictionary(
            IDictionary             source,
            IReadOnlyList<string>   keyColumns)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in keyColumns)
            {
                foreach (DictionaryEntry entry in source)
                {
                    if (entry.Key is string key
                        && string.Equals(key, col, StringComparison.OrdinalIgnoreCase)
                        && entry.Value != null)
                    {
                        result[col] = entry.Value;
                        break;
                    }
                }
            }
            return result.Count == 0 ? null : result;
        }

        private static IReadOnlyDictionary<string, PropertyInfo> BuildAccessor(Type type)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var dict  = new Dictionary<string, PropertyInfo>(props.Length, StringComparer.OrdinalIgnoreCase);
            foreach (var p in props)
            {
                if (p.GetIndexParameters().Length > 0) continue;     // skip indexers
                if (!p.CanRead)                        continue;
                if (!dict.ContainsKey(p.Name))         dict[p.Name] = p;
            }
            return dict;
        }
    }
}
