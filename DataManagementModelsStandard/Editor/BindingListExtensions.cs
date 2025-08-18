using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;


namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Enhanced extension methods for BindingList&lt;T&gt; providing performance optimizations,
    /// bulk operations, filtering, sorting, and utility methods.
    /// All methods are fully generic with appropriate constraints.
    /// </summary>
    public static class BindingListExtensions
    {
        #region Bulk Operations

        /// <summary>
        /// Adds multiple items to a BindingList with optimized event handling
        /// </summary>
        /// <typeparam name="T">The type of elements in the BindingList</typeparam>
        /// <param name="bindingList">The target BindingList</param>
        /// <param name="items">Items to add</param>
        /// <param name="suppressEvents">Whether to suppress events during the operation</param>
        public static void AddRange<T>(this BindingList<T> bindingList, IEnumerable<T> items, bool suppressEvents = true)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (!bindingList.AllowNew)
                throw new InvalidOperationException("BindingList does not allow adding new items.");

            bool oldRaiseListChangedEvents = bindingList.RaiseListChangedEvents;
            
            if (suppressEvents)
                bindingList.RaiseListChangedEvents = false;

            try
            {
                foreach (var item in items)
                {
                    if (item != null)
                    {
                        bindingList.Add(item);
                    }
                }
            }
            finally
            {
                if (suppressEvents)
                {
                    bindingList.RaiseListChangedEvents = oldRaiseListChangedEvents;
                    if (bindingList.RaiseListChangedEvents)
                    {
                        bindingList.ResetBindings();
                    }
                }
            }
        }

        /// <summary>
        /// Removes multiple items from a BindingList with optimized event handling
        /// </summary>
        public static void RemoveRange<T>(this BindingList<T> bindingList, IEnumerable<T> items, bool suppressEvents = true)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (!bindingList.AllowRemove)
                throw new InvalidOperationException("BindingList does not allow removing items.");

            bool oldRaiseListChangedEvents = bindingList.RaiseListChangedEvents;
            
            if (suppressEvents)
                bindingList.RaiseListChangedEvents = false;

            try
            {
                var itemsToRemove = items.Where(item => bindingList.Contains(item)).ToList();
                foreach (var item in itemsToRemove)
                {
                    bindingList.Remove(item);
                }
            }
            finally
            {
                if (suppressEvents)
                {
                    bindingList.RaiseListChangedEvents = oldRaiseListChangedEvents;
                    if (bindingList.RaiseListChangedEvents)
                    {
                        bindingList.ResetBindings();
                    }
                }
            }
        }

        /// <summary>
        /// Removes items matching a predicate
        /// </summary>
        public static void RemoveWhere<T>(this BindingList<T> bindingList, Func<T, bool> predicate, bool suppressEvents = true)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var itemsToRemove = bindingList.Where(predicate).ToList();
            bindingList.RemoveRange(itemsToRemove, suppressEvents);
        }

        /// <summary>
        /// Clears and replaces all items in the BindingList
        /// </summary>
        public static void ReplaceAll<T>(this BindingList<T> bindingList, IEnumerable<T> newItems)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (newItems == null)
                throw new ArgumentNullException(nameof(newItems));

            bool oldRaiseListChangedEvents = bindingList.RaiseListChangedEvents;
            bindingList.RaiseListChangedEvents = false;

            try
            {
                bindingList.Clear();
                foreach (var item in newItems)
                {
                    if (item != null)
                    {
                        bindingList.Add(item);
                    }
                }
            }
            finally
            {
                bindingList.RaiseListChangedEvents = oldRaiseListChangedEvents;
                if (bindingList.RaiseListChangedEvents)
                {
                    bindingList.ResetBindings();
                }
            }
        }

        #endregion

        #region LINQ-Style Element Access Methods

        /// <summary>
        /// Returns the first element of the BindingList
        /// </summary>
        public static T First<T>(this BindingList<T> bindingList)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (bindingList.Count == 0)
                throw new InvalidOperationException("BindingList contains no elements");

            return bindingList[0];
        }

        /// <summary>
        /// Returns the first element that satisfies a condition
        /// </summary>
        public static T First<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            foreach (var item in bindingList)
            {
                if (predicate(item))
                    return item;
            }

            throw new InvalidOperationException("No element satisfies the condition");
        }

        /// <summary>
        /// Returns the first element of the BindingList, or default if empty
        /// </summary>
        public static T FirstOrDefault<T>(this BindingList<T> bindingList)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));

            return bindingList.Count == 0 ? default(T) : bindingList[0];
        }

        /// <summary>
        /// Returns the first element that satisfies a condition, or default if none found
        /// </summary>
        public static T FirstOrDefault<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            foreach (var item in bindingList)
            {
                if (predicate(item))
                    return item;
            }

            return default(T);
        }

        /// <summary>
        /// Returns the last element of the BindingList
        /// </summary>
        public static T Last<T>(this BindingList<T> bindingList)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (bindingList.Count == 0)
                throw new InvalidOperationException("BindingList contains no elements");

            return bindingList[bindingList.Count - 1];
        }

        /// <summary>
        /// Returns the last element that satisfies a condition
        /// </summary>
        public static T Last<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            for (int i = bindingList.Count - 1; i >= 0; i--)
            {
                if (predicate(bindingList[i]))
                    return bindingList[i];
            }

            throw new InvalidOperationException("No element satisfies the condition");
        }

        /// <summary>
        /// Returns the last element of the BindingList, or default if empty
        /// </summary>
        public static T LastOrDefault<T>(this BindingList<T> bindingList)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));

            return bindingList.Count == 0 ? default(T) : bindingList[bindingList.Count - 1];
        }

        /// <summary>
        /// Returns the last element that satisfies a condition, or default if none found
        /// </summary>
        public static T LastOrDefault<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            for (int i = bindingList.Count - 1; i >= 0; i--)
            {
                if (predicate(bindingList[i]))
                    return bindingList[i];
            }

            return default(T);
        }

        /// <summary>
        /// Returns the only element of the BindingList
        /// </summary>
        public static T Single<T>(this BindingList<T> bindingList)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (bindingList.Count == 0)
                throw new InvalidOperationException("BindingList contains no elements");
            if (bindingList.Count > 1)
                throw new InvalidOperationException("BindingList contains more than one element");

            return bindingList[0];
        }

        /// <summary>
        /// Returns the only element that satisfies a condition
        /// </summary>
        public static T Single<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            T result = default(T);
            bool found = false;

            foreach (var item in bindingList)
            {
                if (predicate(item))
                {
                    if (found)
                        throw new InvalidOperationException("More than one element satisfies the condition");
                    
                    result = item;
                    found = true;
                }
            }

            if (!found)
                throw new InvalidOperationException("No element satisfies the condition");

            return result;
        }

        /// <summary>
        /// Returns the only element of the BindingList, or default if empty
        /// </summary>
        public static T SingleOrDefault<T>(this BindingList<T> bindingList)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (bindingList.Count > 1)
                throw new InvalidOperationException("BindingList contains more than one element");

            return bindingList.Count == 0 ? default(T) : bindingList[0];
        }

        /// <summary>
        /// Returns the only element that satisfies a condition, or default if none found
        /// </summary>
        public static T SingleOrDefault<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            T result = default(T);
            bool found = false;

            foreach (var item in bindingList)
            {
                if (predicate(item))
                {
                    if (found)
                        throw new InvalidOperationException("More than one element satisfies the condition");
                    
                    result = item;
                    found = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the element at a specified index
        /// </summary>
        public static T ElementAt<T>(this BindingList<T> bindingList, int index)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (index < 0 || index >= bindingList.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return bindingList[index];
        }

        /// <summary>
        /// Returns the element at a specified index, or default if index is out of range
        /// </summary>
        public static T ElementAtOrDefault<T>(this BindingList<T> bindingList, int index)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (index < 0 || index >= bindingList.Count)
                return default(T);

            return bindingList[index];
        }

        #endregion

        #region LINQ-Style Partitioning Methods

        /// <summary>
        /// Bypasses a specified number of elements and returns the remaining elements
        /// </summary>
        public static BindingList<T> Skip<T>(this BindingList<T> bindingList, int count)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));

            var result = new List<T>();
            for (int i = count; i < bindingList.Count; i++)
            {
                result.Add(bindingList[i]);
            }

            return new BindingList<T>(result);
        }

        /// <summary>
        /// Bypasses elements while a condition is true and returns the remaining elements
        /// </summary>
        public static BindingList<T> SkipWhile<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var result = new List<T>();
            bool skipping = true;

            foreach (var item in bindingList)
            {
                if (skipping && predicate(item))
                    continue;
                
                skipping = false;
                result.Add(item);
            }

            return new BindingList<T>(result);
        }

        /// <summary>
        /// Returns a specified number of elements from the start
        /// </summary>
        public static BindingList<T> Take<T>(this BindingList<T> bindingList, int count)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));

            var result = new List<T>();
            int taken = 0;

            foreach (var item in bindingList)
            {
                if (taken >= count)
                    break;
                
                result.Add(item);
                taken++;
            }

            return new BindingList<T>(result);
        }

        /// <summary>
        /// Returns elements while a condition is true
        /// </summary>
        public static BindingList<T> TakeWhile<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var result = new List<T>();

            foreach (var item in bindingList)
            {
                if (!predicate(item))
                    break;
                
                result.Add(item);
            }

            return new BindingList<T>(result);
        }

        /// <summary>
        /// Returns a new BindingList with elements in reverse order
        /// </summary>
        public static BindingList<T> Reverse<T>(this BindingList<T> bindingList)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));

            var result = new List<T>();
            for (int i = bindingList.Count - 1; i >= 0; i--)
            {
                result.Add(bindingList[i]);
            }

            return new BindingList<T>(result);
        }

        #endregion

        #region Enhanced LINQ-Style Query Methods

        /// <summary>
        /// Determines whether all elements satisfy a condition
        /// </summary>
        public static bool All<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            foreach (var item in bindingList)
            {
                if (!predicate(item))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether any element exists or satisfies a condition
        /// </summary>
        public static bool Any<T>(this BindingList<T> bindingList)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));

            return bindingList.Count > 0;
        }

        /// <summary>
        /// Determines whether any element satisfies a condition
        /// </summary>
        public static bool Any<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            foreach (var item in bindingList)
            {
                if (predicate(item))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the BindingList contains a specific value
        /// </summary>
        public static bool Contains<T>(this BindingList<T> bindingList, T value, IEqualityComparer<T> comparer = null)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));

            comparer ??= EqualityComparer<T>.Default;

            foreach (var item in bindingList)
            {
                if (comparer.Equals(item, value))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the number of elements that satisfy a condition
        /// </summary>
        public static int Count<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            int count = 0;
            foreach (var item in bindingList)
            {
                if (predicate(item))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Finds the index of the first element that matches the predicate
        /// </summary>
        public static int FindIndex<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            for (int i = 0; i < bindingList.Count; i++)
            {
                if (predicate(bindingList[i]))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Finds the index of the last element that matches the predicate
        /// </summary>
        public static int FindLastIndex<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            for (int i = bindingList.Count - 1; i >= 0; i--)
            {
                if (predicate(bindingList[i]))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Returns the index of the first occurrence of a specific value
        /// </summary>
        public static int IndexOf<T>(this BindingList<T> bindingList, T value, IEqualityComparer<T> comparer = null)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));

            comparer ??= EqualityComparer<T>.Default;

            for (int i = 0; i < bindingList.Count; i++)
            {
                if (comparer.Equals(bindingList[i], value))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Returns the index of the last occurrence of a specific value
        /// </summary>
        public static int LastIndexOf<T>(this BindingList<T> bindingList, T value, IEqualityComparer<T> comparer = null)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));

            comparer ??= EqualityComparer<T>.Default;

            for (int i = bindingList.Count - 1; i >= 0; i--)
            {
                if (comparer.Equals(bindingList[i], value))
                    return i;
            }

            return -1;
        }

        #endregion

        #region Filtering and Searching

        /// <summary>
        /// Filters the BindingList and returns a new BindingList with matching items
        /// </summary>
        public static BindingList<T> Filter<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var filteredItems = bindingList.Where(predicate);
            return new BindingList<T>(filteredItems.ToList());
        }

        /// <summary>
        /// Generic filter by property value using expression
        /// </summary>
        public static BindingList<T> FilterByProperty<T, TProperty>(
            this BindingList<T> bindingList, 
            Expression<Func<T, TProperty>> propertySelector, 
            TProperty value,
            IEqualityComparer<TProperty> comparer = null)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (propertySelector == null)
                throw new ArgumentNullException(nameof(propertySelector));

            var compiledSelector = propertySelector.Compile();
            comparer ??= EqualityComparer<TProperty>.Default;

            return bindingList.Filter(item => comparer.Equals(compiledSelector(item), value));
        }

        /// <summary>
        /// Generic filter by string property with text search
        /// </summary>
        public static BindingList<T> FilterByText<T>(
            this BindingList<T> bindingList,
            Expression<Func<T, string>> propertySelector,
            string searchText,
            bool ignoreCase = true)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (propertySelector == null)
                throw new ArgumentNullException(nameof(propertySelector));
            if (string.IsNullOrEmpty(searchText))
                return new BindingList<T>(bindingList.ToList());

            var compiledSelector = propertySelector.Compile();
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            return bindingList.Filter(item =>
            {
                var propertyValue = compiledSelector(item);
                return !string.IsNullOrEmpty(propertyValue) && propertyValue.Contains(searchText, comparison);
            });
        }

        /// <summary>
        /// Finds the first item matching the predicate
        /// </summary>
        public static T FindFirst<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return bindingList.FirstOrDefault(predicate);
        }

        /// <summary>
        /// Finds all items matching the predicate
        /// </summary>
        public static IEnumerable<T> FindAll<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return bindingList.Where(predicate);
        }

        /// <summary>
        /// Finds item by property value
        /// </summary>
        public static T FindByProperty<T, TProperty>(
            this BindingList<T> bindingList,
            Expression<Func<T, TProperty>> propertySelector,
            TProperty value,
            IEqualityComparer<TProperty> comparer = null)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (propertySelector == null)
                throw new ArgumentNullException(nameof(propertySelector));

            var compiledSelector = propertySelector.Compile();
            comparer ??= EqualityComparer<TProperty>.Default;

            return bindingList.FirstOrDefault(item => comparer.Equals(compiledSelector(item), value));
        }

        #endregion

        #region Sorting

        /// <summary>
        /// Sorts the BindingList using a key selector
        /// </summary>
        public static void Sort<T, TKey>(this BindingList<T> bindingList, Func<T, TKey> keySelector, bool ascending = true)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            var sortedItems = ascending 
                ? bindingList.OrderBy(keySelector)
                : bindingList.OrderByDescending(keySelector);

            bindingList.ReplaceAll(sortedItems);
        }

        /// <summary>
        /// Sorts the BindingList using an expression
        /// </summary>
        public static void SortByProperty<T, TProperty>(
            this BindingList<T> bindingList,
            Expression<Func<T, TProperty>> propertySelector,
            bool ascending = true)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (propertySelector == null)
                throw new ArgumentNullException(nameof(propertySelector));

            var compiledSelector = propertySelector.Compile();
            bindingList.Sort(compiledSelector, ascending);
        }

        /// <summary>
        /// Multi-level sorting with multiple key selectors
        /// </summary>
        public static void SortBy<T>(this BindingList<T> bindingList, params (Func<T, object> keySelector, bool ascending)[] sortCriteria)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (sortCriteria == null || sortCriteria.Length == 0)
                throw new ArgumentException("At least one sort criteria must be provided");

            IOrderedEnumerable<T> orderedItems = null;

            for (int i = 0; i < sortCriteria.Length; i++)
            {
                var (keySelector, ascending) = sortCriteria[i];
                
                if (i == 0)
                {
                    orderedItems = ascending 
                        ? bindingList.OrderBy(keySelector)
                        : bindingList.OrderByDescending(keySelector);
                }
                else
                {
                    orderedItems = ascending
                        ? orderedItems.ThenBy(keySelector)
                        : orderedItems.ThenByDescending(keySelector);
                }
            }

            bindingList.ReplaceAll(orderedItems);
        }

        #endregion

        #region Conversion Methods

        /// <summary>
        /// Converts BindingList to a regular List
        /// </summary>
        public static List<T> ToList<T>(this BindingList<T> bindingList)
        {
            if (bindingList == null)
                return new List<T>();

            return bindingList.Cast<T>().ToList();
        }

        /// <summary>
        /// Converts IEnumerable to BindingList
        /// </summary>
        public static BindingList<T> ToBindingList<T>(this IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new BindingList<T>(source.ToList());
        }

        /// <summary>
        /// Creates a deep copy of the BindingList (for ICloneable items)
        /// </summary>
        public static BindingList<T> DeepCopy<T>(this BindingList<T> bindingList) where T : ICloneable
        {
            if (bindingList == null)
                return new BindingList<T>();

            var clonedItems = bindingList.Select(item => (T)item.Clone()).ToList();
            return new BindingList<T>(clonedItems);
        }

        /// <summary>
        /// Creates a shallow copy of the BindingList
        /// </summary>
        public static BindingList<T> ShallowCopy<T>(this BindingList<T> bindingList)
        {
            if (bindingList == null)
                return new BindingList<T>();

            return new BindingList<T>(bindingList.ToList());
        }

        #endregion

        #region Validation and Utility

        /// <summary>
        /// Validates all items in the BindingList using a predicate
        /// </summary>
        public static bool ValidateAll<T>(this BindingList<T> bindingList, Func<T, bool> validator)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (validator == null)
                throw new ArgumentNullException(nameof(validator));

            return bindingList.All(validator);
        }

        /// <summary>
        /// Gets invalid items based on a validator predicate
        /// </summary>
        public static IEnumerable<T> GetInvalidItems<T>(this BindingList<T> bindingList, Func<T, bool> validator)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (validator == null)
                throw new ArgumentNullException(nameof(validator));

            return bindingList.Where(item => !validator(item));
        }

        /// <summary>
        /// Removes duplicate items based on a key selector
        /// </summary>
        public static void RemoveDuplicates<T, TKey>(this BindingList<T> bindingList, Func<T, TKey> keySelector)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            var uniqueItems = bindingList.GroupBy(keySelector).Select(g => g.First());
            bindingList.ReplaceAll(uniqueItems);
        }

        /// <summary>
        /// Removes duplicates using the default equality comparer
        /// </summary>
        public static void RemoveDuplicates<T>(this BindingList<T> bindingList)
        {
            bindingList.RemoveDuplicates(item => item);
        }

        /// <summary>
        /// Checks if the BindingList is null or empty
        /// </summary>
        public static bool IsNullOrEmpty<T>(this BindingList<T> bindingList)
        {
            return bindingList == null || bindingList.Count == 0;
        }

        /// <summary>
        /// Safely gets an item by index, returning default if index is out of range
        /// </summary>
        public static T SafeGet<T>(this BindingList<T> bindingList, int index, T defaultValue = default)
        {
            if (bindingList == null || index < 0 || index >= bindingList.Count)
                return defaultValue;

            return bindingList[index];
        }

        /// <summary>
        /// Gets count of items matching predicate
        /// </summary>
        public static int CountWhere<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return bindingList.Count(predicate);
        }

        /// <summary>
        /// Checks if any item matches the predicate
        /// </summary>
        public static bool AnyMatch<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return bindingList.Any(predicate);
        }

        #endregion

        #region Async Operations

        /// <summary>
        /// Asynchronously adds items to the BindingList
        /// </summary>
        public static async Task AddRangeAsync<T>(this BindingList<T> bindingList, IEnumerable<T> items, bool suppressEvents = true)
        {
            await Task.Run(() => bindingList.AddRange(items, suppressEvents));
        }

        /// <summary>
        /// Asynchronously filters the BindingList
        /// </summary>
        public static async Task<BindingList<T>> FilterAsync<T>(this BindingList<T> bindingList, Func<T, bool> predicate)
        {
            return await Task.Run(() => bindingList.Filter(predicate));
        }

        /// <summary>
        /// Asynchronously sorts the BindingList
        /// </summary>
        public static async Task SortAsync<T, TKey>(this BindingList<T> bindingList, Func<T, TKey> keySelector, bool ascending = true)
        {
            await Task.Run(() => bindingList.Sort(keySelector, ascending));
        }

        /// <summary>
        /// Asynchronously removes duplicates
        /// </summary>
        public static async Task RemoveDuplicatesAsync<T, TKey>(this BindingList<T> bindingList, Func<T, TKey> keySelector)
        {
            await Task.Run(() => bindingList.RemoveDuplicates(keySelector));
        }

        #endregion

        #region Tree Operations (Generic with Constraints)

        /// <summary>
        /// Flattens a hierarchical BindingList into a flat list.
        /// Works with any type that has a Children property of type BindingList&lt;T&gt;
        /// </summary>
        public static BindingList<T> Flatten<T>(
            this BindingList<T> bindingList,
            Func<T, BindingList<T>> childrenSelector,
            bool includeRoot = true)
        {
            if (bindingList == null)
                return new BindingList<T>();
            if (childrenSelector == null)
                throw new ArgumentNullException(nameof(childrenSelector));

            var flatList = new List<T>();

            foreach (var item in bindingList)
            {
                if (includeRoot)
                    flatList.Add(item);

                var children = childrenSelector(item);
                if (children?.Count > 0)
                {
                    var childrenFlat = children.Flatten(childrenSelector, true);
                    flatList.AddRange(childrenFlat);
                }
            }

            return new BindingList<T>(flatList);
        }

        /// <summary>
        /// Finds items by path in a hierarchical structure
        /// </summary>
        public static T FindByPath<T>(
            this BindingList<T> bindingList,
            string path,
            Func<T, string> nameSelector,
            Func<T, BindingList<T>> childrenSelector,
            char separator = '/')
        {
            if (bindingList == null || string.IsNullOrEmpty(path))
                return default;
            if (nameSelector == null)
                throw new ArgumentNullException(nameof(nameSelector));
            if (childrenSelector == null)
                throw new ArgumentNullException(nameof(childrenSelector));

            var pathParts = path.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length == 0)
                return default;

            var currentLevel = bindingList;
            T currentItem = default;

            foreach (var part in pathParts)
            {
                currentItem = currentLevel.FirstOrDefault(item =>
                    string.Equals(nameSelector(item), part, StringComparison.OrdinalIgnoreCase));

                if (currentItem == null)
                    return default;

                currentLevel = childrenSelector(currentItem);
                if (currentLevel == null && part != pathParts.Last())
                    return default;
            }

            return currentItem;
        }

        /// <summary>
        /// Gets all leaf nodes (items without children)
        /// </summary>
        public static BindingList<T> GetLeafNodes<T>(
            this BindingList<T> bindingList,
            Func<T, BindingList<T>> childrenSelector)
        {
            if (bindingList == null)
                return new BindingList<T>();
            if (childrenSelector == null)
                throw new ArgumentNullException(nameof(childrenSelector));

            var leafNodes = new List<T>();

            foreach (var item in bindingList)
            {
                var children = childrenSelector(item);
                if (children?.Count > 0)
                {
                    leafNodes.AddRange(children.GetLeafNodes(childrenSelector));
                }
                else
                {
                    leafNodes.Add(item);
                }
            }

            return new BindingList<T>(leafNodes);
        }

        /// <summary>
        /// Gets the depth of the tree structure
        /// </summary>
        public static int GetMaxDepth<T>(
            this BindingList<T> bindingList,
            Func<T, BindingList<T>> childrenSelector)
        {
            if (bindingList.IsNullOrEmpty() || childrenSelector == null)
                return 0;

            int maxDepth = 1;
            foreach (var item in bindingList)
            {
                var children = childrenSelector(item);
                if (children?.Count > 0)
                {
                    int childDepth = children.GetMaxDepth(childrenSelector) + 1;
                    maxDepth = Math.Max(maxDepth, childDepth);
                }
            }

            return maxDepth;
        }

        #endregion

        #region Performance Monitoring

        /// <summary>
        /// Executes an action with event suppression and performance monitoring
        /// </summary>
        public static TimeSpan ExecuteWithPerformanceMonitoring<T>(
            this BindingList<T> bindingList,
            Action<BindingList<T>> action,
            bool suppressEvents = true)
        {
            if (bindingList == null)
                throw new ArgumentNullException(nameof(bindingList));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool oldRaiseListChangedEvents = bindingList.RaiseListChangedEvents;

            if (suppressEvents)
                bindingList.RaiseListChangedEvents = false;

            try
            {
                action(bindingList);
                return stopwatch.Elapsed;
            }
            finally
            {
                if (suppressEvents)
                {
                    bindingList.RaiseListChangedEvents = oldRaiseListChangedEvents;
                    if (bindingList.RaiseListChangedEvents)
                    {
                        bindingList.ResetBindings();
                    }
                }
                stopwatch.Stop();
            }
        }

        #endregion

      
    }
}
