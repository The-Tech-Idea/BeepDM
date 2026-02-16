using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Find"


        /// <summary>
        /// Finds items where a string property contains the search text
        /// </summary>
        public List<T> WhereContains(string propertyName, string searchText, bool ignoreCase = true)
        {
            return SearchByText(propertyName, searchText, "Contains", ignoreCase);
        }

        /// <summary>
        /// Finds the first item matching the predicate
        /// </summary>
        public T FirstOrDefault(Func<T, bool> predicate)
        {
            foreach (var item in this)
            {
                if (predicate(item))
                    return item;
            }
            return default;
        }

        /// <summary>
        /// Gets whether any item matches the predicate
        /// </summary>
        public bool Any(Func<T, bool> predicate)
        {
            foreach (var item in this)
            {
                if (predicate(item))
                    return true;
            }
            return false;
        }



        /// <summary>
        /// Event triggered when search or filter operations complete
        /// </summary>
        public event EventHandler<SearchCompletedEventArgs<T>> SearchCompleted;

        /// <summary>
        /// Searches with progress reporting
        /// </summary>
        /// <param name="predicate">Search criteria</param>
        /// <param name="progress">Progress reporter</param>
        /// <returns>List of matching items</returns>
        public List<T> SearchWithProgress(Func<T, bool> predicate, IProgress<int> progress = null)
        {
            var results = new List<T>();
            int total = originalList.Count;
            int processed = 0;

            foreach (var item in originalList)
            {
                if (predicate(item))
                    results.Add(item);

                processed++;
                progress?.Report((int)((float)processed / total * 100));
            }

            // Raise the search completed event
            SearchCompleted?.Invoke(this, new SearchCompletedEventArgs<T>(results));

            return results;
        }

        /// <summary>
        /// Event arguments for search completion
        /// </summary>
        public class SearchCompletedEventArgs<TItem> : EventArgs
        {
            /// <summary>Gets the list of search results.</summary>
            public List<TItem> Results { get; }
            /// <summary>Gets the number of results found.</summary>
            public int Count => Results.Count;

            /// <summary>Initializes a new SearchCompletedEventArgs with the given results.</summary>
            public SearchCompletedEventArgs(List<TItem> results)
            {
                Results = results;
            }
        }

        /// <summary>Finds the index of the first item matching the predicate.</summary>
        public int FindIndex(Predicate<T> match)
        {
            for (int i = 0; i < this.Count; i++)
            {
                if (match(this[i]))
                    return i;
            }
            return -1;
        }

        /// <summary>Searches the original list and returns all items matching the predicate.</summary>
        public List<T> Search(Func<T, bool> predicate)
        {
            return originalList.Where(predicate).ToList();
        }

        /// <summary>Finds the first item matching the expression predicate.</summary>
        public T Find(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return this.Items.AsQueryable().FirstOrDefault(predicate);
        }
        /// <summary>Finds the first item where the named property equals the given value.</summary>
        public T Find(string propertyName, object value)
        {
            var prop = GetCachedProperty(propertyName);
            if (prop == null)
            {
                throw new ArgumentException($"'{propertyName}' is not a valid property of type '{typeof(T).Name}'.");
            }

            return this.FirstOrDefault(item => object.Equals(prop.GetValue(item), value));
        }

        #endregion
    }
}
