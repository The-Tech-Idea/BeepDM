using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Computed / Virtual Columns — Phase 6A"

        /// <summary>
        /// Registered computed column definitions.
        /// Key: computed column name, Value: computation function.
        /// </summary>
        private readonly Dictionary<string, Func<T, object>> _computedColumns
            = new Dictionary<string, Func<T, object>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Cached computed values per item.
        /// Outer key: item reference, Inner key: computed column name.
        /// Invalidated per-item on property change.
        /// </summary>
        private readonly Dictionary<T, Dictionary<string, object>> _computedCache
            = new Dictionary<T, Dictionary<string, object>>();

        /// <summary>
        /// Registers a computed column that auto-recalculates on property change.
        /// </summary>
        /// <param name="name">Logical name of the computed column (e.g. "FullName").</param>
        /// <param name="computation">Function that computes the value from an item.</param>
        public void RegisterComputed(string name, Func<T, object> computation)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (computation == null) throw new ArgumentNullException(nameof(computation));

            _computedColumns[name] = computation;

            // Invalidate any cached values for this column
            foreach (var kvp in _computedCache)
            {
                kvp.Value.Remove(name);
            }
        }

        /// <summary>
        /// Removes a registered computed column.
        /// </summary>
        public void UnregisterComputed(string name)
        {
            if (string.IsNullOrEmpty(name)) return;

            _computedColumns.Remove(name);

            // Clean up cache entries
            foreach (var kvp in _computedCache)
            {
                kvp.Value.Remove(name);
            }
        }

        /// <summary>
        /// Gets all registered computed column names.
        /// </summary>
        public IReadOnlyCollection<string> ComputedColumnNames => _computedColumns.Keys.ToList().AsReadOnly();

        /// <summary>
        /// Gets the computed value for a specific column on a specific item.
        /// Uses cache if available; otherwise computes and caches.
        /// </summary>
        public object GetComputed(T item, string name)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (!_computedColumns.TryGetValue(name, out var computation))
                throw new ArgumentException($"No computed column '{name}' is registered.");

            // Check cache
            if (_computedCache.TryGetValue(item, out var itemCache) && itemCache.TryGetValue(name, out var cached))
                return cached;

            // Compute
            var value = computation(item);

            // Cache
            if (!_computedCache.TryGetValue(item, out itemCache))
            {
                itemCache = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                _computedCache[item] = itemCache;
            }
            itemCache[name] = value;

            return value;
        }

        /// <summary>
        /// Gets all computed values for a specific item.
        /// </summary>
        public Dictionary<string, object> GetAllComputed(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in _computedColumns)
            {
                result[kvp.Key] = GetComputed(item, kvp.Key);
            }
            return result;
        }

        /// <summary>
        /// Clears the computed cache for a specific item.
        /// Called internally on property change to trigger recalculation.
        /// </summary>
        internal void InvalidateComputedCache(T item)
        {
            if (_computedColumns.Count == 0) return;

            _computedCache.Remove(item);

            // Raise PropertyChanged for each computed column so UI bindings refresh
            if (!SuppressNotification)
            {
                foreach (var name in _computedColumns.Keys)
                {
                    OnPropertyChanged(name);
                }
            }
        }

        /// <summary>
        /// Clears all computed caches.
        /// </summary>
        public void ClearComputedCache()
        {
            _computedCache.Clear();
        }

        #endregion
    }
}
