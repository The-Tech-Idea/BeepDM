using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Represents a group of items sharing the same key value.
    /// </summary>
    public class ItemGroup<TItem>
    {
        /// <summary>The group key value.</summary>
        public object Key { get; set; }

        /// <summary>String representation of the key (Key.ToString() by default).</summary>
        public string KeyDisplay { get; set; }

        /// <summary>Items belonging to this group.</summary>
        public IReadOnlyList<TItem> Items { get; set; }

        /// <summary>Number of items in the group.</summary>
        public int Count => Items?.Count ?? 0;
    }

    public partial class ObservableBindingList<T>
    {
        #region "Grouping Support (1-F)"

        /// <summary>
        /// Groups the current working set by <paramref name="keySelector"/> and returns
        /// groups in key order.  Does NOT modify the underlying list.
        /// </summary>
        /// <typeparam name="TKey">The type of the grouping key.</typeparam>
        /// <param name="keySelector">Function that extracts the key from an item.</param>
        /// <param name="ascending">True to sort groups ascending (default); false for descending.</param>
        public IReadOnlyList<ItemGroup<T>> GetGroups<TKey>(
            Func<T, TKey> keySelector,
            bool ascending = true)
        {
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            var source = (_currentWorkingSet != null && _currentWorkingSet.Count > 0)
                ? _currentWorkingSet
                : originalList;

            var grouped = source
                .GroupBy(keySelector)
                .Select(g => new ItemGroup<T>
                {
                    Key = g.Key,
                    KeyDisplay = g.Key?.ToString() ?? string.Empty,
                    Items = g.ToList()
                });

            var ordered = ascending
                ? grouped.OrderBy(g => g.Key as IComparable ?? g.KeyDisplay)
                : grouped.OrderByDescending(g => g.Key as IComparable ?? g.KeyDisplay);

            return ordered.ToList();
        }

        #endregion "Grouping Support (1-F)"
    }
}
