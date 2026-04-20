using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace TheTechIdea.Beep.Distributed.Query
{
    /// <summary>
    /// Reads named column values out of the loosely-typed row objects
    /// returned by shard data sources. Supports dictionary rows,
    /// <see cref="DataRow"/>, and plain POCOs (via cached reflection).
    /// </summary>
    /// <remarks>
    /// The merger deals with heterogeneous shard payloads — some
    /// shards might return strongly-typed entity classes, others
    /// might return <c>DataTable</c>s, and a few might return plain
    /// dictionaries. Wrapping field access through this helper keeps
    /// the merge path agnostic and avoids littering the grouping
    /// code with <c>as</c> chains.
    /// </remarks>
    internal static class RowValueExtractor
    {
        private static readonly IDictionary<Type, IDictionary<string, PropertyInfo>> PropertyCache
            = new Dictionary<Type, IDictionary<string, PropertyInfo>>();

        private static readonly object CacheGate = new object();

        /// <summary>
        /// Attempts to read the value stored under <paramref name="column"/>
        /// on <paramref name="row"/>. Column lookup is case-insensitive.
        /// </summary>
        /// <param name="row">Row instance; may be <c>null</c>.</param>
        /// <param name="column">Column / property name.</param>
        /// <param name="value">Extracted value; <c>null</c> when missing.</param>
        /// <returns><c>true</c> when a value (or <c>null</c>) was found.</returns>
        public static bool TryGetValue(object row, string column, out object value)
        {
            value = null;
            if (row == null || string.IsNullOrEmpty(column)) return false;

            if (row is IDictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    if (string.Equals(kvp.Key, column, StringComparison.OrdinalIgnoreCase))
                    {
                        value = kvp.Value;
                        return true;
                    }
                }
                return false;
            }

            if (row is IDictionary legacyDict)
            {
                foreach (DictionaryEntry entry in legacyDict)
                {
                    if (entry.Key != null &&
                        string.Equals(entry.Key.ToString(), column, StringComparison.OrdinalIgnoreCase))
                    {
                        value = entry.Value;
                        return true;
                    }
                }
                return false;
            }

            if (row is DataRow dataRow)
            {
                if (!dataRow.Table.Columns.Contains(column)) return false;
                value = dataRow[column];
                if (value is DBNull) value = null;
                return true;
            }

            return TryGetPropertyValue(row, column, out value);
        }

        /// <summary>
        /// Convenience wrapper around <see cref="TryGetValue"/> that
        /// returns <c>null</c> when the column is missing.
        /// </summary>
        public static object GetValueOrDefault(object row, string column)
            => TryGetValue(row, column, out var value) ? value : null;

        private static bool TryGetPropertyValue(object row, string column, out object value)
        {
            value = null;
            var map  = GetPropertyMap(row.GetType());
            if (map == null) return false;

            if (!map.TryGetValue(column, out var prop)) return false;

            try
            {
                value = prop.GetValue(row);
                return true;
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }

        private static IDictionary<string, PropertyInfo> GetPropertyMap(Type type)
        {
            if (type == null) return null;

            lock (CacheGate)
            {
                if (PropertyCache.TryGetValue(type, out var cached)) return cached;

                var map = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in props)
                    {
                        if (!prop.CanRead) continue;
                        if (prop.GetIndexParameters().Length > 0) continue;
                        map[prop.Name] = prop;
                    }
                }
                catch (Exception)
                {
                    // Ignore — we'll cache an empty map so we don't keep reflecting on failing types.
                }

                PropertyCache[type] = map;
                return map;
            }
        }
    }
}
