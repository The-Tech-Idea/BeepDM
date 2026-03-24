using System.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Data;
using System.Reflection;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;


namespace TheTechIdea.Beep.Editor
{
    public class ObservableBindingList<T> : BindingList<T>, IBindingListView, INotifyCollectionChanged, IDisposable where T : class, INotifyPropertyChanged, new()
    {
        private bool _isDisposed = false;

        /// <summary>
        /// Static cache for PropertyInfo lookups to avoid repeated reflection calls.
        /// Key is "TypeFullName.PropertyName".
        /// </summary>
        private static readonly ConcurrentDictionary<string, PropertyInfo> _propertyInfoCache
            = new ConcurrentDictionary<string, PropertyInfo>();

        /// <summary>
        /// Gets a cached PropertyInfo for the given property name on type T.
        /// Returns null if the property does not exist.
        /// </summary>
        private static PropertyInfo GetCachedProperty(string propertyName)
        {
            string key = $"{typeof(T).FullName}.{propertyName}";
            return _propertyInfoCache.GetOrAdd(key, _ => typeof(T).GetProperty(propertyName));
        }

        /// <summary>
        /// Gets all cached PropertyInfo for type T.
        /// </summary>
        private static PropertyInfo[] GetCachedProperties()
        {
            // Use a sentinel key for "all properties"
            string key = $"{typeof(T).FullName}.*";
            if (_propertyInfoCache.TryGetValue(key, out _))
            {
                // We stored the "all" sentinel; now return from per-property cache
            }
            var props = typeof(T).GetProperties();
            // Cache each property individually
            foreach (var p in props)
            {
                string propKey = $"{typeof(T).FullName}.{p.Name}";
                _propertyInfoCache.TryAdd(propKey, p);
            }
            return props;
        }

        public int PageSize { get; private set; } = 20;
        public int CurrentPage { get; private set; } = 1;
        public int TotalPages => (int)Math.Ceiling((double)originalList.Count / PageSize);
        public event EventHandler<ItemAddedEventArgs<T>> ItemAdded;
        public event EventHandler<ItemRemovedEventArgs<T>> ItemRemoved;
        public event EventHandler<ItemChangedEventArgs<T>> ItemChanged;

        public event EventHandler<ItemValidatingEventArgs<T>> ItemValidating;
        public event EventHandler<ItemValidatingEventArgs<T>> ItemDeleting;
      
        // Primary dictionary keyed by UniqueId for O(1) lookup by Guid
        private Dictionary<Guid, Tracking> _trackingsByGuid = new Dictionary<Guid, Tracking>();
        // Secondary index for O(1) lookup by OriginalIndex
        private Dictionary<int, Tracking> _trackingsByOriginalIndex = new Dictionary<int, Tracking>();

        /// <summary>
        /// Backward-compatible list view of all tracking records.
        /// Prefer internal dictionary lookups for performance.
        /// </summary>
        public List<Tracking> Trackings
        {
            get => _trackingsByGuid.Values.ToList();
            set
            {
                ClearTrackings();
                if (value != null)
                {
                    foreach (var tr in value)
                        AddTracking(tr);
                }
            }
        }

        private void AddTracking(Tracking tr)
        {
            _trackingsByGuid[tr.UniqueId] = tr;
            // Only index non-deleted items by OriginalIndex (deleted ones may collide)
            if (tr.EntityState != EntityState.Deleted)
                _trackingsByOriginalIndex[tr.OriginalIndex] = tr;
        }

        private void RemoveTracking(Tracking tr)
        {
            _trackingsByGuid.Remove(tr.UniqueId);
            if (_trackingsByOriginalIndex.TryGetValue(tr.OriginalIndex, out var existing) && existing.UniqueId == tr.UniqueId)
                _trackingsByOriginalIndex.Remove(tr.OriginalIndex);
        }

        private void ClearTrackings()
        {
            _trackingsByGuid.Clear();
            _trackingsByOriginalIndex.Clear();
        }

        private Tracking FindTrackingByOriginalIndex(int originalIndex)
        {
            _trackingsByOriginalIndex.TryGetValue(originalIndex, out var tr);
            return tr;
        }

        private int TrackingsCount => _trackingsByGuid.Count;
        public bool SuppressNotification { get; set; } = false;
        public bool IsSorted => isSorted;
        public bool IsSynchronized => false;
        private bool _isPositionChanging = false; // Add this flag

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #region "Current and Movement"
        private int _currentIndex = -1;
        public int CurrentIndex { get { return _currentIndex; } }
        public T Current
        {
            get 
            { 
                if (_currentIndex >= 0 && _currentIndex < Items.Count)
                {
                    return Items[_currentIndex];
                }
                else
                {
                    return default;
                }
            }
          
        }
        public event EventHandler CurrentChanged;
        protected virtual void OnCurrentChanged()
        {
            SuppressNotification = true;
            _isPositionChanging = true; // Set the flag before changing the position
            CurrentChanged?.Invoke(this, EventArgs.Empty);
            _isPositionChanging = false; // Reset the flag after changing the position
            SuppressNotification = false;
        }
    

        public bool MoveNext()
        {
            if (_currentIndex < Items.Count - 1)
            {
                _currentIndex++;
              
                OnCurrentChanged(); // Manually call OnCurrentChanged if needed
                OnPropertyChanged("Current");
                return true;
            }

            return false;
        }
        public bool MovePrevious()
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                OnCurrentChanged(); // Manually call OnCurrentChanged if needed
                OnPropertyChanged("Current");
                return true;
            }

            return false;
        }
        public bool MoveFirst()
        {
            if (Items.Count > 0)
            {
                _currentIndex = 0;
                OnCurrentChanged(); // Manually call OnCurrentChanged if needed
                OnPropertyChanged("Current");
                return true;
            }

            return false;
        }
        public bool MoveLast()
        {
            if (Items.Count > 0)
            {
                _currentIndex = Items.Count - 1;
                OnCurrentChanged(); // Manually call OnCurrentChanged if needed
                OnPropertyChanged("Current");
                return true;
            }

            return false;
        }
        public bool MoveTo(int index)
        {
            if (index >= 0 && index < Items.Count)
            {
              
                _currentIndex = index;
                OnCurrentChanged(); // Manually call OnCurrentChanged if needed
                OnPropertyChanged("Current");

                return true;
            }

            return false;
        }

        #endregion
        #region "Sort"
        private bool isSorted;
        private PropertyDescriptor sortProperty;
        private ListSortDirection sortDirection;
        protected override bool SupportsSortingCore => true;
        protected override bool IsSortedCore => isSorted;
        protected override PropertyDescriptor SortPropertyCore => sortProperty;
        protected override ListSortDirection SortDirectionCore => sortDirection;

        public ListSortDescriptionCollection SortDescriptions { get; }
        public bool SupportsAdvancedSorting => true;
        private void InsertionSort(List<T> list, int left, int right, PropertyInfo property, ListSortDirection direction)
        {
            for (int i = left + 1; i <= right; i++)
            {
                T temp = list[i];
                int j = i - 1;

                while (j >= left && Compare(list[j], temp, property, direction) > 0)
                {
                    list[j + 1] = list[j];
                    j--;
                }

                list[j + 1] = temp;
            }
        }
        private int Compare(T x, T y, PropertyInfo property, ListSortDirection direction)
        {
            var valueX = property.GetValue(x);
            var valueY = property.GetValue(y);
            int result = Comparer<object>.Default.Compare(valueX, valueY);

            return direction == ListSortDirection.Ascending ? result : -result;
        }
        private void ParallelQuickSort(List<T> list, int left, int right, PropertyInfo property, ListSortDirection direction)
        {
            if (left < right)
            {
                int pivotIndex = Partition(list, left, right, property, direction);

                if (right - left < 1000) // Threshold for switching to insertion sort
                {
                    InsertionSort(list, left, pivotIndex - 1, property, direction);
                    InsertionSort(list, pivotIndex + 1, right, property, direction);
                }
                else
                {
                    // Parallelize the recursive calls
                    Parallel.Invoke(
                        () => ParallelQuickSort(list, left, pivotIndex - 1, property, direction),
                        () => ParallelQuickSort(list, pivotIndex + 1, right, property, direction)
                    );
                }
            }
        }

        private int Partition(List<T> list, int left, int right, PropertyInfo property, ListSortDirection direction)
        {
            T pivot = list[right];
            int i = left;

            for (int j = left; j < right; j++)
            {
                if (Compare(list[j], pivot, property, direction) <= 0)
                {
                    Swap(list, i, j);
                    i++;
                }
            }

            Swap(list, i, right);
            return i;
        }

        private void Swap(List<T> list, int i, int j)
        {
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
        public void ApplySort(string propertyName, ListSortDirection direction)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("Property name cannot be null or empty.");

            PropertyDescriptor propDesc = TypeDescriptor.GetProperties(typeof(T))[propertyName];
            if (propDesc == null)
                throw new ArgumentException($"No property '{propertyName}' in type '{typeof(T).Name}'");

            // Create a comparer based on the property and direction
            var comparer = new PropertyComparer<T>(propDesc, direction);

            // Sort the original list
            originalList.Sort(comparer);

            // Apply the same sort to the current list
            var sortedList = new List<T>(originalList);
            ResetItems(sortedList);

            ResetBindings();
        }

        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;
            var items = Items as List<T>;
            if (items != null)
            {
                var property = GetCachedProperty(prop.Name);
                if (property != null)
                {
                    // Parallel quicksort with insertion sort for small subarrays
                    ParallelQuickSort(items, 0, items.Count - 1, property, direction);

                    isSorted = true;
                    sortProperty = prop;
                    sortDirection = direction;

                    ResetItems(items);
                   
                    ResetBindings();
                }
            }
            SuppressNotification = false;
            RaiseListChangedEvents = true;
        }
        public void RemoveSort()
        {
            if(isSorted)
            {
                RemoveSortCore();
            }
        }
        protected override void RemoveSortCore()
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;
            isSorted = false;
            sortProperty = null;
            sortDirection = ListSortDirection.Ascending;
            ResetItems(originalList);
            SuppressNotification = false;
            RaiseListChangedEvents = true;
        }
        public void ApplySort(ListSortDescriptionCollection sorts)
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;
            var paramExpr = Expression.Parameter(typeof(T), "x");
            IQueryable<T> queryableList = originalList.ToList().AsQueryable(); 

            IOrderedQueryable<T> orderedQuery = null;

            foreach (ListSortDescription sortDesc in sorts)
            {
                var property = GetCachedProperty(sortDesc.PropertyDescriptor.Name);
                if (property == null)
                    throw new InvalidOperationException($"No property '{sortDesc.PropertyDescriptor.Name}' on type '{typeof(T)}'");

                var propertyAccess = Expression.MakeMemberAccess(paramExpr, property);
                var orderByExp = Expression.Lambda(propertyAccess, paramExpr);

                string methodName = null;

                if (sortDesc.SortDirection == ListSortDirection.Ascending)
                    methodName = orderedQuery == null ? "OrderBy" : "ThenBy";
                else
                    methodName = orderedQuery == null ? "OrderByDescending" : "ThenByDescending";

                MethodCallExpression resultExp = Expression.Call(
                    typeof(Queryable),
                    methodName,
                    new Type[] { typeof(T), property.PropertyType },
                    queryableList.Expression,
                    Expression.Quote(orderByExp));

                queryableList = queryableList.Provider.CreateQuery<T>(resultExp);
                orderedQuery = (IOrderedQueryable<T>)queryableList;
            }

            ResetItems(orderedQuery.ToList());
          
            ResetBindings();
            SuppressNotification = false;
            RaiseListChangedEvents = true;
        }
        public ListSortDirection SortDirection
        {
            get => sortDirection;
            set
            {
                if (sortDirection != value)
                {
                    sortDirection = value;
                    OnPropertyChanged("SortDirection");
                }
            }
        }
        public void Sort(string propertyName)
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;
            var prop = GetCachedProperty(propertyName);
            if (prop == null)
            {
                throw new ArgumentException($"'{propertyName}' is not a valid property of type '{typeof(T).Name}'.");
            }

            if (SortDirection == ListSortDirection.Ascending)
            {
                ((List<T>)Items).Sort((x, y) => Comparer.Default.Compare(prop.GetValue(x), prop.GetValue(y)));
            }
            else
            {
                ((List<T>)Items).Sort((x, y) => Comparer.Default.Compare(prop.GetValue(y), prop.GetValue(x)));
            }

            OnPropertyChanged("Item[]");
            SuppressNotification = false;
            RaiseListChangedEvents = true;
        }
        #endregion
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
            public List<TItem> Results { get; }
            public int Count => Results.Count;

            public SearchCompletedEventArgs(List<TItem> results)
            {
                Results = results;
            }
        }

        public int FindIndex(Predicate<T> match)
        {
            for (int i = 0; i < this.Count; i++)
            {
                if (match(this[i]))
                    return i;
            }
            return -1;
        }

        public List<T> Search(Func<T, bool> predicate)
        {
            return originalList.Where(predicate).ToList();
        }

        public T Find(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return this.Items.AsQueryable().FirstOrDefault(predicate);
        }
        public T Find(string propertyName, object value)
        {
            var prop = GetCachedProperty(propertyName);
            if (prop == null)
            {
                throw new ArgumentException($"'{propertyName}' is not a valid property of type '{typeof(T).Name}'.");
            }

            return this.FirstOrDefault(item => object.Equals(prop.GetValue(item), value));
        }
        #region "Enhanced Search Functionality"

        /// <summary>
        /// Searches for items matching multiple property conditions with a specific operator between conditions.
        /// </summary>
        /// <param name="propertyConditions">Dictionary of property names and their values to match</param>
        /// <param name="matchOperator">Logical operator to apply between conditions ("AND" or "OR")</param>
        /// <returns>List of matching items</returns>
        public List<T> SearchByProperties(Dictionary<string, object> propertyConditions, string matchOperator = "AND")
        {
            if (propertyConditions == null || propertyConditions.Count == 0)
                return originalList.ToList();

            var parameter = Expression.Parameter(typeof(T), "x");
            Expression expression = null;

            foreach (var condition in propertyConditions)
            {
                var property = Expression.Property(parameter, condition.Key);
                var constant = Expression.Constant(condition.Value);
                var comparison = Expression.Equal(property, constant);

                if (expression == null)
                {
                    expression = comparison;
                }
                else
                {
                    expression = matchOperator.ToUpper() == "AND"
                        ? Expression.AndAlso(expression, comparison)
                        : Expression.OrElse(expression, comparison);
                }
            }

            if (expression == null)
                return originalList.ToList();

            var lambda = Expression.Lambda<Func<T, bool>>(expression, parameter);
            return originalList.Where(lambda.Compile()).ToList();
        }

        /// <summary>
        /// Finds all items with the specified property value matching the search string using 
        /// contains, starts with, or ends with matching.
        /// </summary>
        /// <param name="propertyName">Name of the property to search</param>
        /// <param name="searchText">Text to search for</param>
        /// <param name="matchType">Type of match: "Contains", "StartsWith", "EndsWith", or "Exact"</param>
        /// <param name="ignoreCase">Whether to ignore case when matching</param>
        /// <returns>List of matching items</returns>
        public List<T> SearchByText(string propertyName, string searchText,
                                   string matchType = "Contains", bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(searchText))
                return originalList.ToList();

            var property = GetCachedProperty(propertyName);
            if (property == null)
                throw new ArgumentException($"Property '{propertyName}' not found on type {typeof(T).Name}");

            if (property.PropertyType != typeof(string))
                throw new ArgumentException($"Property '{propertyName}' is not a string property");

            StringComparison comparison = ignoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            return originalList.Where(item =>
            {
                string propertyValue = property.GetValue(item) as string;
                if (propertyValue == null)
                    return false;

                switch (matchType.ToLower())
                {
                    case "startswith":
                        return propertyValue.StartsWith(searchText, comparison);
                    case "endswith":
                        return propertyValue.EndsWith(searchText, comparison);
                    case "exact":
                        return string.Equals(propertyValue, searchText, comparison);
                    case "contains":
                    default:
                        return propertyValue.IndexOf(searchText, comparison) >= 0;
                }
            }).ToList();
        }

        /// <summary>
        /// Searches across multiple string properties for a search term
        /// </summary>
        /// <param name="searchText">Text to search for</param>
        /// <param name="propertyNames">Optional specific properties to search (searches all string properties if null)</param>
        /// <param name="ignoreCase">Whether to ignore case when matching</param>
        /// <returns>List of matching items</returns>
        public List<T> SearchAllProperties(string searchText, IEnumerable<string> propertyNames = null, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(searchText))
                return originalList.ToList();

            StringComparison comparison = ignoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            // Get all string properties if propertyNames is null (using cached reflection)
            var allProps = GetCachedProperties();
            var properties = propertyNames == null
                ? allProps.Where(p => p.PropertyType == typeof(string)).ToList()
                : allProps.Where(p =>
                    propertyNames.Contains(p.Name) && p.PropertyType == typeof(string)).ToList();

            if (!properties.Any())
                return originalList.ToList();

            return originalList.Where(item =>
            {
                foreach (var prop in properties)
                {
                    string value = prop.GetValue(item) as string;
                    if (value != null && value.IndexOf(searchText, comparison) >= 0)
                        return true;
                }
                return false;
            }).ToList();
        }

        /// <summary>
        /// Finds items based on a complex predicate that can include nested property paths
        /// </summary>
        /// <param name="filter">Filter string in format "Property[.NestedProperty] Operator Value"</param>
        /// <param name="separator">Character separating multiple filter conditions</param>
        /// <param name="logicalOperator">Logical operator to use between conditions ("AND" or "OR")</param>
        /// <returns>List of items matching the complex filter</returns>
        public List<T> AdvancedSearch(string filter, char separator = ';', string logicalOperator = "AND")
        {
            if (string.IsNullOrWhiteSpace(filter))
                return originalList.ToList();

            var filters = filter.Split(separator);
            Func<T, bool> combinedPredicate = null;

            foreach (var filterPart in filters)
            {
                var parts = filterPart.Trim().Split(new[] { ' ' }, 3);
                if (parts.Length < 3)
                    continue;

                string propPath = parts[0];
                string op = parts[1];
                string value = parts[2].Trim('\'');

                // Create predicate for this filter part
                Func<T, bool> partPredicate = item => EvaluateCondition(item, propPath, op, value);

                // Combine with existing predicate
                if (combinedPredicate == null)
                    combinedPredicate = partPredicate;
                else if (logicalOperator.ToUpper() == "AND")
                    combinedPredicate = x => combinedPredicate(x) && partPredicate(x);
                else
                    combinedPredicate = x => combinedPredicate(x) || partPredicate(x);
            }

            return combinedPredicate == null
                ? originalList.ToList()
                : originalList.Where(combinedPredicate).ToList();
        }

        private bool EvaluateCondition(T item, string propertyPath, string op, string value)
        {
            // Split property path for nested properties
            var pathParts = propertyPath.Split('.');
            object propValue = item;

            // Navigate the property path
            foreach (var part in pathParts)
            {
                if (propValue == null)
                    return false;

                var propInfo = propValue.GetType().GetProperty(part);
                if (propInfo == null)
                    return false;

                propValue = propInfo.GetValue(propValue);
            }

            if (propValue == null)
                return op.ToUpper() == "IS" && value.ToUpper() == "NULL";

            // Handle different operators
            switch (op.ToUpper())
            {
                case "=":
                case "==":
                    return CompareValues(propValue, value, (a, b) => a.Equals(b));
                case "!=":
                    return CompareValues(propValue, value, (a, b) => !a.Equals(b));
                case ">":
                    return CompareValues(propValue, value, (a, b) => Comparer<IComparable>.Default.Compare((IComparable)a, (IComparable)b) > 0);
                case "<":
                    return CompareValues(propValue, value, (a, b) => Comparer<IComparable>.Default.Compare((IComparable)a, (IComparable)b) < 0);
                case ">=":
                    return CompareValues(propValue, value, (a, b) => Comparer<IComparable>.Default.Compare((IComparable)a, (IComparable)b) >= 0);
                case "<=":
                    return CompareValues(propValue, value, (a, b) => Comparer<IComparable>.Default.Compare((IComparable)a, (IComparable)b) <= 0);
                case "LIKE":
                    return propValue.ToString().Contains(value.Replace("%", ""));
                case "IN":
                    var values = value.Split(',').Select(v => v.Trim());
                    return values.Contains(propValue.ToString());
                default:
                    return false;
            }
        }

        private bool CompareValues(object propValue, string stringValue, Func<object, object, bool> comparison)
        {
            try
            {
                var propType = propValue.GetType();
                object convertedValue;

                if (propType == typeof(string))
                {
                    convertedValue = stringValue;
                }
                else if (propType.IsEnum)
                {
                    convertedValue = Enum.Parse(propType, stringValue);
                }
                else if (propType == typeof(DateTime))
                {
                    convertedValue = DateTime.Parse(stringValue);
                }
                else if (IsNumericType(propType))
                {
                    convertedValue = Convert.ChangeType(stringValue, propType);
                }
                else
                {
                    convertedValue = Convert.ChangeType(stringValue, propType);
                }

                return comparison(propValue, convertedValue);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Applies the search based on a predicate and updates the current view
        /// </summary>
        /// <param name="predicate">Search predicate function</param>
        /// <returns>Number of items found</returns>
        public int FindAndFilter(Func<T, bool> predicate)
        {
            if (predicate == null)
            {
                ResetItems(originalList);
                return Count;
            }

            var results = originalList.Where(predicate).ToList();
            ResetItems(results);
            ResetBindings();
            return results.Count;
        }

        #endregion

        #endregion
        #region "Filter"
        private string filterString;
        private List<T> originalList = new List<T>();
        private List<T> DeletedList = new List<T>();
        public bool SupportsFiltering => true;

        // New ApplyFilter method using predicate
        public void ApplyFilter(Func<T, bool> predicate)
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;

            if (predicate == null)
            {
                // Remove filter by resetting items to the original list
                ResetItems(originalList);
            }
            else
            {
                // Apply the predicate to filter the items
                var filteredItems = originalList.Where(predicate).ToList();
                ResetItems(filteredItems);
            }

            ResetBindings();
            SuppressNotification = false;
            RaiseListChangedEvents = true;
        }
        public void ApplyFilter(string propertyName, object value, string comparisonOperator = "==")
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyName);
            var constant = Expression.Constant(value);

            BinaryExpression comparison;
            switch (comparisonOperator)
            {
                case "==":
                    comparison = Expression.Equal(property, constant);
                    break;
                case "!=":
                    comparison = Expression.NotEqual(property, constant);
                    break;
                case ">":
                    comparison = Expression.GreaterThan(property, constant);
                    break;
                case "<":
                    comparison = Expression.LessThan(property, constant);
                    break;
                case ">=":
                    comparison = Expression.GreaterThanOrEqual(property, constant);
                    break;
                case "<=":
                    comparison = Expression.LessThanOrEqual(property, constant);
                    break;
                default:
                    throw new ArgumentException("Invalid comparison operator");
            }

            var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
            ApplyFilter(lambda.Compile());
        }

        public string Filter
        {
            get => filterString;
            set
            {
                if (filterString != value)
                {
                    filterString = value;
                    ApplyFilter();
                }
            }
        }
        public void RemoveFilter()
        {
            Filter = null;
        }
        private void ApplyFilter()
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;
            if (string.IsNullOrWhiteSpace(filterString))
            {
                ResetItems(originalList);
            }
            else
            {
                var fil = ParseFilter(filterString);
                if(fil == null)
                {
                    return;
                }
                var filteredItems = originalList.AsQueryable().Where(fil).ToList();
                ResetItems(filteredItems);
            }
            ResetBindings();
            SuppressNotification = false;
            RaiseListChangedEvents = true;
           
        }
        private Expression<Func<T, bool>> ParseFilter(string filter)
        {
            var parameter = Expression.Parameter(typeof(T), "x");

            // Split by OR first, then each OR-group by AND
            // Example: "Name LIKE '%John%' AND Age > 30 OR City = 'NY'"
            var orGroups = filter.Split(new string[] { " OR " }, StringSplitOptions.RemoveEmptyEntries);
            Expression orExpression = null;

            foreach (var orGroup in orGroups)
            {
                var andFilters = orGroup.Split(new string[] { " AND " }, StringSplitOptions.RemoveEmptyEntries);
                Expression andExpression = null;

                foreach (var f in andFilters)
                {
                    var comparison = ParseSingleCondition(f.Trim(), parameter);
                    if (comparison != null)
                    {
                        andExpression = andExpression == null ? comparison : Expression.AndAlso(andExpression, comparison);
                    }
                }

                if (andExpression != null)
                {
                    orExpression = orExpression == null ? andExpression : Expression.OrElse(orExpression, andExpression);
                }
            }

            return orExpression != null ? Expression.Lambda<Func<T, bool>>(orExpression, parameter) : null;
        }

        private Expression ParseSingleCondition(string condition, ParameterExpression parameter)
        {
            var parts = condition.Split(new[] { ' ' }, 3);
            if (parts.Length < 3)
                return null;

            string propName = parts[0];
            string op = parts[1];
            string value = parts[2].Trim('\'');

            var property = Expression.Property(parameter, propName);
            var propertyType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;

            Expression comparison = null;
            bool treatAsString = value.Contains("%");

            if ((op.ToUpper() == "LIKE") && (propertyType == typeof(string) || treatAsString))
            {
                var nullCheck = Expression.NotEqual(property, Expression.Constant(null, property.Type));
                var propertyAsString = Expression.Call(property, typeof(object).GetMethod("ToString", Type.EmptyTypes));

                Expression containsExpression = null;
                if (value.StartsWith("%") && value.EndsWith("%"))
                {
                    value = value.Trim('%');
                    containsExpression = Expression.Call(propertyAsString, typeof(string).GetMethod("Contains", new[] { typeof(string) }), Expression.Constant(value));
                }
                else if (value.StartsWith("%"))
                {
                    value = value.TrimStart('%');
                    containsExpression = Expression.Call(propertyAsString, typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), Expression.Constant(value));
                }
                else if (value.EndsWith("%"))
                {
                    value = value.TrimEnd('%');
                    containsExpression = Expression.Call(propertyAsString, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), Expression.Constant(value));
                }

                comparison = containsExpression != null ? Expression.AndAlso(nullCheck, containsExpression) : null;
            }
            else
            {
                // Ensure the value is converted to the correct type
                object convertedValue = null;
                try
                {
                    if (propertyType == typeof(string) || treatAsString)
                    {
                        convertedValue = value;
                    }
                    else if (propertyType.IsEnum)
                    {
                        convertedValue = Enum.Parse(propertyType, value);
                    }
                    else if (propertyType == typeof(DateTime))
                    {
                        convertedValue = DateTime.Parse(value);
                    }
                    else if (IsNumericType(propertyType))
                    {
                        convertedValue = Convert.ChangeType(value, propertyType);
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(value, propertyType);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidCastException($"Failed to convert value '{value}' to type '{propertyType}'", ex);
                }

                var valueExpression = Expression.Constant(convertedValue, treatAsString ? typeof(string) : property.Type);

                switch (op.ToUpper())
                {
                    case "=":
                        comparison = Expression.Equal(property, valueExpression);
                        break;
                    case "!=":
                        comparison = Expression.NotEqual(property, valueExpression);
                        break;
                    case ">":
                        comparison = Expression.GreaterThan(property, valueExpression);
                        break;
                    case "<":
                        comparison = Expression.LessThan(property, valueExpression);
                        break;
                    case ">=":
                        comparison = Expression.GreaterThanOrEqual(property, valueExpression);
                        break;
                    case "<=":
                        comparison = Expression.LessThanOrEqual(property, valueExpression);
                        break;
                }
            }

            return comparison;
        }
        private void ResetItems(List<T> items)
        {
            // Create a copy of the list for safe iteration
            var itemsCopy = new List<T>(items);

            // Bypass InsertItem override by using Items directly.
            // This avoids re-adding to originalList and corrupting Trackings.
            // Unhook PropertyChanged from current items first
            foreach (var item in Items)
            {
                if (item is INotifyPropertyChanged npc)
                    npc.PropertyChanged -= Item_PropertyChanged;
            }
            Items.Clear();

            // Add items directly to the underlying list (no InsertItem override)
            foreach (var item in itemsCopy)
            {
                Items.Add(item);
                if (item is INotifyPropertyChanged npc)
                    npc.PropertyChanged += Item_PropertyChanged;
            }

            UpdateIndexTrackingAfterFilterorSort(); // Update index mapping after resetting items
        }
        public new void ResetBindings()
        {
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }
        #endregion
        #region "Constructor"
        private void ClearAll()
        {
            originalList = new List<T>();
            UpdateLog = new Dictionary<DateTime, EntityUpdateInsertLog>();
          
            ChangedValues = new Dictionary<T, Dictionary<string, object>>();
        }

        /// <summary>
        /// Override ClearItems to unhook PropertyChanged handlers before clearing,
        /// and reset all tracking state.
        /// </summary>
        protected override void ClearItems()
        {
            // Unhook PropertyChanged from all items to prevent memory leaks
            foreach (var item in Items)
            {
                if (item is INotifyPropertyChanged npc)
                    npc.PropertyChanged -= Item_PropertyChanged;
            }
            base.ClearItems();
        }

        /// <summary>
        /// Disposes the ObservableBindingList, unhooking all event handlers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Unhook all PropertyChanged handlers from active items
                    foreach (var item in Items)
                    {
                        if (item is INotifyPropertyChanged npc)
                            npc.PropertyChanged -= Item_PropertyChanged;
                    }
                    // Unhook from deleted items too
                    foreach (var item in DeletedList)
                    {
                        if (item is INotifyPropertyChanged npc)
                            npc.PropertyChanged -= Item_PropertyChanged;
                    }

                    ClearTrackings();
                    originalList.Clear();
                    DeletedList.Clear();
                    UpdateLog?.Clear();
                    ChangedValues?.Clear();
                }
                _isDisposed = true;
            }
        }

        public ObservableBindingList() : base()
        {
            // Initialize the list with no items and subscribe to AddingNew event.
            AddingNew += ObservableBindingList_AddingNew;
            this.AllowNew = true;
            this.AllowEdit = true;
            this.AllowRemove = true;
            ClearAll();
        }
        public ObservableBindingList(IEnumerable<T> enumerable) : base(new List<T>(enumerable))
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;
            ClearAll();
            foreach (T item in this.Items)
            {
                item.PropertyChanged += Item_PropertyChanged;
                originalList.Add(item); // Add to originalList, don't add again to Items (already in base constructor)
            }
            AddingNew += ObservableBindingList_AddingNew;
            UpdateItemIndexMapping(0, true); // Update index mapping after resetting items
            SuppressNotification = false;
            RaiseListChangedEvents = true;
        }
        public ObservableBindingList(IList<T> list) : base(list)
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;
            ClearAll();
            foreach (T item in list)
            {
                item.PropertyChanged += Item_PropertyChanged;
                originalList.Add(item);
            }

            AddingNew += ObservableBindingList_AddingNew;
            this.AllowNew = true;
            this.AllowEdit = true;
            this.AllowRemove = true;
          
            UpdateItemIndexMapping(0, true); // Update index mapping after resetting items
            SuppressNotification = false;
            RaiseListChangedEvents = true;
        }
        public ObservableBindingList(IBindingListView bindinglist) : base()
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;
            ClearAll();
            foreach (T item in bindinglist)
            {
                item.PropertyChanged += Item_PropertyChanged;
                this.Add(item);
                originalList.Add(item);
            }

            AddingNew += ObservableBindingList_AddingNew;
            this.AllowNew = true;
            this.AllowEdit = true;
            this.AllowRemove = true;
          
            UpdateItemIndexMapping(0, true); // Update index mapping after resetting items
            SuppressNotification = false;
            RaiseListChangedEvents = true;
        }
        public ObservableBindingList(DataTable dataTable) : base()
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;
            ClearAll();
            foreach (DataRow row in dataTable.Rows)
            {
                T item = GetItem<T>(row);
                if (item != null)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                    this.Items.Add(item); // Adds the item to the list and hooks up PropertyChanged event
                    originalList.Add(item);
                }
               
            }

            AddingNew += ObservableBindingList_AddingNew;
            this.AllowNew = true;
            this.AllowEdit = true;
            this.AllowRemove = true;
          
            UpdateItemIndexMapping(0, true); // Update index mapping after resetting items
            SuppressNotification = false;
            RaiseListChangedEvents = true;
        }
        public ObservableBindingList(List<object> objects) : base()
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;
            if (objects == null)
            {
                throw new ArgumentNullException(nameof(objects));
            }
            ClearAll();
            foreach (var obj in objects)
            {
                T item = obj as T;
                if (item != null)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                    this.Items.Add(item); // Adds the item to the list and hooks up PropertyChanged event
                }
                else
                {
                    // Optionally handle the case where the object is not of type T
                    // For example, you might throw an exception or ignore the item
                    throw new InvalidCastException($"Object of type {obj.GetType().Name} cannot be cast to type {typeof(T).Name}.");
                }
            }

            AddingNew += ObservableBindingList_AddingNew;
            this.AllowNew = true;
            this.AllowEdit = true;
            this.AllowRemove = true;
            originalList = new List<T>(this.Items);
            UpdateItemIndexMapping(0, true); // Update index mapping after resetting items
            SuppressNotification = false;
            RaiseListChangedEvents = true;
        }

        #endregion
        #region "Util Methods"
        private T GetItem<T>(DataRow dr) where T : class, new()
        {
            Type temp = typeof(T);
            T obj = new T();
            var properties = temp.GetProperties();

            foreach (var property in properties)
            {
                if (dr.Table.Columns.Contains(property.Name))
                {
                    try
                    {
                        var value = dr[property.Name];
                        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                        if (value == DBNull.Value)
                        {
                            value = property.PropertyType.IsValueType ? Activator.CreateInstance(propertyType) : null;
                        }
                        else if (propertyType == typeof(char) && value is string str && str.Length == 1)
                        {
                            value = str[0];
                        }
                        else if (propertyType.IsEnum && value is string enumString)
                        {
                            value = Enum.Parse(propertyType, enumString);
                        }
                        else if (IsNumericType(propertyType))
                        {
                            value = ConvertToNumericType(value, propertyType);
                        }
                        else
                        {
                            value = Convert.ChangeType(value, propertyType);
                        }

                        property.SetValue(obj, value);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidCastException($"Cannot convert column '{property.Name}' value to property '{property.Name}' of type '{property.PropertyType}': {ex.Message}", ex);
                    }
                }
            }

            return obj;
        }

        private object ConvertToNumericType(object value, Type targetType)
        {
            try
            {
                if (targetType == typeof(byte)) return Convert.ToByte(value);
                if (targetType == typeof(sbyte)) return Convert.ToSByte(value);
                if (targetType == typeof(short)) return Convert.ToInt16(value);
                if (targetType == typeof(ushort)) return Convert.ToUInt16(value);
                if (targetType == typeof(int)) return Convert.ToInt32(value);
                if (targetType == typeof(uint)) return Convert.ToUInt32(value);
                if (targetType == typeof(long)) return Convert.ToInt64(value);
                if (targetType == typeof(ulong)) return Convert.ToUInt64(value);
                if (targetType == typeof(float)) return Convert.ToSingle(value);
                if (targetType == typeof(double)) return Convert.ToDouble(value);
                if (targetType == typeof(decimal)) return Convert.ToDecimal(value);
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Cannot convert value '{value}' to numeric type '{targetType}': {ex.Message}", ex);
            }
            return value;
        }
        private static bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }
        #endregion
        #region "Logging"

        private Dictionary<string, object> TrackChanges(T original, T current)
        {
            var changedFields = new Dictionary<string, object>();

            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(current))
            {
                var originalValue = prop.GetValue(original);
                var currentValue = prop.GetValue(current);
                if (!Equals(originalValue, currentValue))
                {
                    if (!ChangedValues.ContainsKey(current))
                    {
                        ChangedValues[current] = new Dictionary<string, object>();
                    }
                    changedFields[prop.Name] = currentValue;
                    ChangedValues[current][prop.Name] = originalValue;
                }
            }

            return changedFields;
        }
        private void CreateLogEntry(T item, LogAction action, Tracking tracking, Dictionary<string, object> changedFields = null)
        {
            if (tracking == null)
            {
                // Create a temporary tracking for logging purposes
                int originalIndex = originalList.IndexOf(item);
                tracking = new Tracking(Guid.NewGuid(), originalIndex >= 0 ? originalIndex : 0, Items.IndexOf(item))
                {
                    EntityState = action == LogAction.Insert ? EntityState.Added : 
                                 action == LogAction.Delete ? EntityState.Deleted : EntityState.Modified
                };
            }
            
            // Check if an entry for this tracking record already exists
            var existingLogEntry = UpdateLog.Values.FirstOrDefault(log =>
                log.TrackingRecord != null && log.TrackingRecord.UniqueId == tracking.UniqueId);

            if (existingLogEntry != null)
            {
                // Update the existing log entry
                existingLogEntry.LogDateandTime = DateTime.Now;
                existingLogEntry.LogAction = action;
                existingLogEntry.UpdatedFields = changedFields ?? new Dictionary<string, object>();
            }
            else
            {
                // Create a new log entry
                var logEntry = new EntityUpdateInsertLog
                {
                    LogDateandTime = DateTime.Now,
                    LogUser = "CurrentUser", // Replace with actual user if available
                    LogAction = action,
                    LogEntity = typeof(T).Name,
                    UpdatedFields = changedFields ?? new Dictionary<string, object>(),
                    TrackingRecord = tracking
                };

                UpdateLog[logEntry.LogDateandTime] = logEntry;
            }
        }

        private Dictionary<T, Dictionary<string, object>> ChangedValues = new Dictionary<T, Dictionary<string, object>>();
        [Obsolete("Use IsLogging instead. This property name contains a typo.")]
        public bool IsLoggin { get => IsLogging; set => IsLogging = value; }
        public bool IsLogging { get; set; } = false;
        public Dictionary<DateTime, EntityUpdateInsertLog> UpdateLog { get; set; }
        private Dictionary<string, object> GetChangedFields(T oldItem, T newItem)
        {
            var changedFields = new Dictionary<string, object>();

            foreach (var property in GetCachedProperties())
            {
                var oldValue = property.GetValue(oldItem);
                var newValue = property.GetValue(newItem);

                if (!Equals(oldValue, newValue))
                {
                    changedFields[property.Name] = newValue;
                }
            }

            return changedFields;
        }
        #endregion
        #region "List and Item Change"
        protected override void OnListChanged(ListChangedEventArgs e)
        {
            if(SuppressNotification || _isPositionChanging)
            {
                return;
            }
        
            if (!_isPositionChanging) // Check the flag before executing the base method
            {
                if (e.ListChangedType == ListChangedType.ItemChanged && e.NewIndex >= 0 && e.NewIndex < Count)
                {
                    _currentIndex = e.NewIndex;
                   
                    OnCurrentChanged();
                }
                base.OnListChanged(e); // Ensure base method is called conditionally
            }
           
        }
        void ObservableBindingList_AddingNew(object sender, AddingNewEventArgs e)
        {
            if (e.NewObject is T item)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }
        }
        void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            T item = (T)sender;
            var args = new ItemValidatingEventArgs<T>(item);
            ItemValidating?.Invoke(this, args);
            if (args.Cancel)
            {
                // Revert change or handle as needed
                throw new InvalidOperationException(args.ErrorMessage);
            }
            // Continue with change notification
            // Notify that the entire item has changed, not just a single property
            if (!SuppressNotification)
            {
                int index = IndexOf(item);
                if (index >= 0)
                {

                    OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
                    ItemChanged?.Invoke(this, new ItemChangedEventArgs<T>((T)sender, e.PropertyName));
                }

            }

        }
        protected override void RemoveItem(int index)
        {
            T removedItem = this[index];
            var args = new ItemValidatingEventArgs<T>(removedItem);
            ItemDeleting?.Invoke(this, args);
            if (args.Cancel)
            {
                // Revert change or handle as needed
                throw new InvalidOperationException(args.ErrorMessage);
            }
            // Continue with change notification
            int trackingindex = -1;
            Tracking tracking = null;
            if (TrackingsCount > 0)
            {
                tracking = GetTrackingItem(removedItem);
            }
            if (removedItem != null)
            {
                removedItem.PropertyChanged -= Item_PropertyChanged;
                DeletedList.Add(removedItem);
            }

            if (IsLogging)
            {
                CreateLogEntry(removedItem, LogAction.Delete, tracking);
            }
           
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, index));
            Items.RemoveAt(index);
            
            int removedOriginalIndex = -1;
            if (tracking != null)
            {
                removedOriginalIndex = tracking.OriginalIndex;
                originalList.RemoveAt(removedOriginalIndex);
                tracking.EntityState = EntityState.Deleted;
                tracking.IsSaved = false; // Optional, mark as pending
            }
            else
            {
                // Create a tracking record if it doesn't exist yet
                int originalIndex = originalList.IndexOf(removedItem);
                if (originalIndex >= 0)
                {
                    removedOriginalIndex = originalIndex;
                    originalList.RemoveAt(originalIndex);
                    var newTracking = new Tracking(Guid.NewGuid(), originalIndex, index)
                    {
                        EntityState = EntityState.Deleted,
                        IsSaved = false
                    };
                    AddTracking(newTracking);
                }
            }

            // Fix index-shift: decrement OriginalIndex for all tracking entries
            // that pointed to an originalList position after the removed one
            if (removedOriginalIndex >= 0)
            {
                // Rebuild the OriginalIndex secondary index after shifting
                var affectedTrackings = _trackingsByGuid.Values
                    .Where(tr => tr.OriginalIndex > removedOriginalIndex && tr.EntityState != EntityState.Deleted)
                    .ToList();
                foreach (var tr in affectedTrackings)
                {
                    // Remove old index entry
                    if (_trackingsByOriginalIndex.TryGetValue(tr.OriginalIndex, out var existing) && existing.UniqueId == tr.UniqueId)
                        _trackingsByOriginalIndex.Remove(tr.OriginalIndex);
                    tr.OriginalIndex--;
                    // Re-add with new index
                    _trackingsByOriginalIndex[tr.OriginalIndex] = tr;
                }
            }
            
            ItemRemoved?.Invoke(this, new ItemRemovedEventArgs<T>(removedItem));

        }
        protected override void InsertItem(int index, T item)
        {
            var args = new ItemValidatingEventArgs<T>(item);
            ItemValidating?.Invoke(this, args);
            if (args.Cancel)
            {
                throw new InvalidOperationException(args.ErrorMessage);
            }
            base.InsertItem(index, item);

            if (!SuppressNotification)
            {
                if (RaiseListChangedEvents)
                {
                    ItemAdded?.Invoke(this, new ItemAddedEventArgs<T>(item));
                    Tracking tr = new Tracking(Guid.NewGuid(), index, index);
                    tr.EntityState = EntityState.Added;
                    tr.IsNew = true;
                    tr.IsSaved=false;
                    if (string.IsNullOrEmpty(filterString) || !isSorted)
                    {
                        originalList.Insert(index, item);

                    }
                    else
                    {
                        originalList.Add(item);
                        tr.OriginalIndex = originalList.Count - 1;

                    }
                    AddTracking(tr);
                    if (IsLogging)
                    {
                        CreateLogEntry(item, LogAction.Insert, tr);
                    }
                    item.PropertyChanged += Item_PropertyChanged;
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
                    // Set current index to the new item
                    SetPosition(index);
                }

            }

        }
        public void SetPosition(int newPosition)
        {
            if (newPosition >= 0 && newPosition < Count)
            {
                SuppressNotification = true;
                _isPositionChanging = true; // Set the flag before changing the position
                try
                {
                    _currentIndex = newPosition;
                    OnCurrentChanged();
                }
                finally
                {
                    SuppressNotification = false;
                    _isPositionChanging = false; // Reset the flag after changing the position
                }
            }
        }

        protected override void SetItem(int index, T item)
        {
            T replacedItem = this[index];
            replacedItem.PropertyChanged -= Item_PropertyChanged;

            var changedFields = TrackChanges(replacedItem, item);

            base.SetItem(index, item);
            if (string.IsNullOrEmpty(filterString))
            {
                if (TrackingsCount > 0)
                {
                    // Try lookup by OriginalIndex first, then scan for CurrentIndex match
                    Tracking tr = FindTrackingByOriginalIndex(index);
                    if (tr == null)
                    {
                        tr = _trackingsByGuid.Values.FirstOrDefault(p => p.CurrentIndex == index);
                    }
                    if (tr != null)
                    {
                        originalList[tr.OriginalIndex] = item;
                        if (IsLogging)
                        {
                            CreateLogEntry(item, LogAction.Update, tr, changedFields);
                        }
                    }
                    else
                    {
                        // Create a new tracking record if it doesn't exist
                        int originalIndex = originalList.IndexOf(replacedItem);
                        if (originalIndex >= 0)
                        {
                            originalList[originalIndex] = item;
                            tr = new Tracking(Guid.NewGuid(), originalIndex, index)
                            {
                                EntityState = EntityState.Modified
                            };
                            AddTracking(tr);
                            if (IsLogging)
                            {
                                CreateLogEntry(item, LogAction.Update, tr, changedFields);
                            }
                        }
                    }
                }
                else
                {
                    // No tracking, update by index directly
                    if (index >= 0 && index < originalList.Count)
                    {
                        originalList[index] = item;
                    }
                }
            }
            else
            {
                Tracking tracking = _trackingsByGuid.Values.FirstOrDefault(p => p.CurrentIndex == index);
                if (tracking != null)
                {
                    originalList[tracking.OriginalIndex] = item;
                    tracking.EntityState = EntityState.Modified;
                    if (IsLogging)
                    {
                        CreateLogEntry(item, LogAction.Update, tracking, changedFields);
                    }
                }
            }

            if (!SuppressNotification)
            {
                item.PropertyChanged += Item_PropertyChanged;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, replacedItem, index));
            }

        }
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
        #endregion "List and Item Change"
        #region "ID Generations"
        private void UpdateIndexTrackingAfterFilterorSort()
        {
            // Optimize by creating a dictionary for O(1) lookups instead of O(n) IndexOf calls
            var itemToOriginalIndex = new Dictionary<T, int>(originalList.Count);
            for (int i = 0; i < originalList.Count; i++)
            {
                itemToOriginalIndex[originalList[i]] = i;
            }
            
            for (int i = 0; i < Items.Count; i++)
            {
                T currentItem = Items[i];
                int newlistidx = i;
                
                if (itemToOriginalIndex.TryGetValue(currentItem, out int originallistidx))
                {
                    if (TrackingsCount > 0)
                    {
                        var existingTr = FindTrackingByOriginalIndex(originallistidx);
                        if (existingTr != null)
                        {
                            existingTr.CurrentIndex = newlistidx;
                            UpdateLogEntries(existingTr, newlistidx);
                        }
                        else
                        {
                            // Create a new tracking record if one does not exist
                            Tracking newTracking = new Tracking(Guid.NewGuid(), originallistidx, newlistidx)
                            {
                                EntityState = EntityState.Unchanged
                            };
                            AddTracking(newTracking);
                        }
                    }
                }
            }
        }
        private void EnsureTrackingConsistency()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                var originalIndex = originalList.IndexOf(item);
                var tracking = _trackingsByGuid.Values.FirstOrDefault(t => t.CurrentIndex == i);

                if (tracking != null)
                {
                    // Update OriginalIndex and rebuild secondary index
                    if (_trackingsByOriginalIndex.TryGetValue(tracking.OriginalIndex, out var existing) && existing.UniqueId == tracking.UniqueId)
                        _trackingsByOriginalIndex.Remove(tracking.OriginalIndex);
                    tracking.OriginalIndex = originalIndex;
                    if (originalIndex >= 0 && tracking.EntityState != EntityState.Deleted)
                        _trackingsByOriginalIndex[originalIndex] = tracking;
                }
            }
        }

        private void UpdateLogEntries(Tracking tracking, int newlistidx)
        {
            if (UpdateLog != null && UpdateLog.Count > 0)
            {
                foreach (var logEntry in UpdateLog.Values)
                {
                    if (logEntry.TrackingRecord != null && logEntry.TrackingRecord.UniqueId == tracking.UniqueId)
                    {
                        logEntry.TrackingRecord.CurrentIndex = newlistidx;
                    }
                }
            }
        }
        private void ResettoOriginal(List<T> items)
        {
            // Reset items to the original list state
            // This method can be used to restore the list to its original state after filtering/sorting
            if (items != null && items.Count > 0)
            {
                ResetItems(items);
                ResetBindings();
            }
        }
        private void UpdateItemIndexMapping(int startIndex, bool isInsert)
        {

            for (int i = startIndex; i < originalList.Count; i++)
            {
                T item = originalList[i];
                if (isInsert)
                {
                    Tracking tr = new Tracking(Guid.NewGuid(), i,i);
                    tr.EntityState = EntityState.Unchanged;
                    AddTracking(tr);

                }
            }
        }

        public int GetOriginalIndex(T item)
        {
            return originalList.IndexOf(item);
        }
        public T GetItem()
        {
            return this.Current;
        }
        public T GetItemFromOriginalList(int index)
        {
            if (index >= 0 && index < originalList.Count)
            {
                return originalList[index];
            }
            else
            { return null; }
        }
        public T GetItemFromCurrentList(int index)
        {
            if(index>=0 && index < Items.Count)
            {
                return this[index];
            }else
                { return null; }
            
        }
        
        [Obsolete("Use GetItemFromCurrentList instead. This method name contains a typo.")]
        public T GetItemFroCurrentList(int index)
        {
            return GetItemFromCurrentList(index);
        }
        public Tracking GetTrackingItem(T item)
        {
            if (item == null)
                return null;
                
            Tracking retval = null;
            
            // First check if item is in deleted list
            if (DeletedList.Count > 0 && DeletedList.Contains(item))
            {
                int originalIndex = originalList.IndexOf(item);
                if (originalIndex >= 0)
                {
                    retval = FindTrackingByOriginalIndex(originalIndex);
                }
                if (retval != null)
                    return retval;
            }

            // Check by original index (O(1) via dictionary)
            int index = GetOriginalIndex(item);
            if (index >= 0)
            {
                retval = FindTrackingByOriginalIndex(index);
                if (retval != null)
                    return retval;
            }
            
            // Check by current index as fallback (linear scan needed - no secondary index for CurrentIndex)
            int currentIndex = Items.IndexOf(item);
            if (currentIndex >= 0)
            {
                retval = _trackingsByGuid.Values.FirstOrDefault(p => p.CurrentIndex == currentIndex);
            }
            
            return retval;
        }
        public void MarkAsCommitted(T item)
        {
            var tracking = GetTrackingItem(item);
            if (tracking != null)
            {
                tracking.IsSaved = true;
                tracking.IsNew = false;
                tracking.EntityState = EntityState.Unchanged;

                // Clean up logs and changed values
                ChangedValues.Remove(item);

                var log = UpdateLog.Values.FirstOrDefault(x => x.TrackingRecord?.UniqueId == tracking.UniqueId);
                if (log != null)
                {
                    UpdateLog.Remove(log.LogDateandTime);
                }

                // Optionally remove from lists if it was deleted
                if (DeletedList.Contains(item))
                {
                    DeletedList.Remove(item);
                    originalList.Remove(item);
                }

                ResetBindings();
            }
        }

        public void ResetAfterCommit()
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;

            // 1. Remove deleted items from the actual list
            if (DeletedList.Count > 0)
            {
                foreach (var deletedItem in DeletedList)
                {
                    if (Items.Contains(deletedItem))
                    {
                        Items.Remove(deletedItem);
                    }

                    // Also remove from originalList if you wish to clear memory
                    originalList.Remove(deletedItem);
                }
                DeletedList.Clear();
            }

            // 2. Clear update log (optional: only for entries that are saved)
            if (UpdateLog != null)
            {
                var keysToRemove = UpdateLog
                    .Where(kvp => kvp.Value.TrackingRecord != null && kvp.Value.TrackingRecord.IsSaved)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    UpdateLog.Remove(key);
                }
            }

            // 3. Reset tracking state
            foreach (var track in _trackingsByGuid.Values)
            {
                track.IsSaved = false;
                track.IsNew = false;
                track.EntityState = EntityState.Unchanged;
            }

            // 4. Reset ChangedValues tracking
            ChangedValues.Clear();

            // 5. Raise bindings and refresh tracking
            UpdateIndexTrackingAfterFilterorSort();
            ResetBindings();

            SuppressNotification = false;
            RaiseListChangedEvents = true;
        }
        public async Task<IErrorsInfo> CommitItemAsync(
    T item,
    Func<T, Task<IErrorsInfo>> insertAsync,
    Func<T, Task<IErrorsInfo>> updateAsync,
    Func<T, Task<IErrorsInfo>> deleteAsync)
        {
            var errors = new ErrorsInfo();
            var tracking = GetTrackingItem(item);

            if (tracking == null)
            {
                errors.Flag = Errors.Failed;
                errors.Message = "Tracking info not found for the item.";
                return errors;
            }

            try
            {
                switch (tracking.EntityState)
                {
                    case EntityState.Added:
                        var insertResult = await insertAsync(item);
                        if (insertResult.Flag == Errors.Ok)
                        {
                            tracking.EntityState = EntityState.Unchanged;
                            tracking.IsSaved = true;
                        }
                        else
                        {
                            return insertResult;
                        }
                        break;

                    case EntityState.Modified:
                        var updateResult = await updateAsync(item);
                        if (updateResult.Flag == Errors.Ok)
                        {
                            tracking.EntityState = EntityState.Unchanged;
                            tracking.IsSaved = true;
                        }
                        else
                        {
                            return updateResult;
                        }
                        break;

                    case EntityState.Deleted:
                        var deleteResult = await deleteAsync(item);
                        if (deleteResult.Flag == Errors.Ok)
                        {
                            DeletedList.Remove(item);
                            originalList.Remove(item);
                            Items.Remove(item);
                            tracking.IsSaved = true;
                        }
                        else
                        {
                            return deleteResult;
                        }
                        break;

                    case EntityState.Unchanged:
                    default:
                        // Nothing to do
                        break;
                }

                // Cleanup: remove update log entry if exists
                var log = UpdateLog.Values.FirstOrDefault(x => x.TrackingRecord?.UniqueId == tracking.UniqueId);
                if (log != null)
                {
                    UpdateLog.Remove(log.LogDateandTime);
                }

                // Clear changes
                ChangedValues.Remove(item);
            }
            catch (Exception ex)
            {
                errors.Flag = Errors.Failed;
                errors.Message = $"CommitItem failed: {ex.Message}";
                errors.Ex = ex;
            }

            return errors;
        }
        public ObservableChanges<T> GetPendingChanges()
        {
            var changes = new ObservableChanges<T>();

            // Build O(1) lookup dictionaries to avoid O(n^2) scanning
            // Map: item -> tracking Guid for items in the active list
            var itemTrackingMap = new Dictionary<Guid, T>();
            foreach (var item in Items)
            {
                var tr = GetTrackingItem(item);
                if (tr != null)
                    itemTrackingMap[tr.UniqueId] = item;
            }
            // Map: item -> tracking Guid for deleted items
            var deletedTrackingMap = new Dictionary<Guid, T>();
            foreach (var item in DeletedList)
            {
                var tr = GetTrackingItem(item);
                if (tr != null)
                    deletedTrackingMap[tr.UniqueId] = item;
            }

            foreach (var tracking in _trackingsByGuid.Values)
            {
                if (!tracking.IsSaved)
                {
                    T item = default;
                    if (tracking.EntityState == EntityState.Deleted)
                        deletedTrackingMap.TryGetValue(tracking.UniqueId, out item);
                    else
                        itemTrackingMap.TryGetValue(tracking.UniqueId, out item);

                    if (item == null) continue;

                    switch (tracking.EntityState)
                    {
                        case EntityState.Added:
                            changes.Added.Add(item);
                            break;
                        case EntityState.Modified:
                            changes.Modified.Add(item);
                            break;
                        case EntityState.Deleted:
                            changes.Deleted.Add(item);
                            break;
                    }
                }
            }

            return changes;
        }
        #endregion
        #region "Export"
        /// <summary>
        /// Exports the current items to a DataTable. Mirror of the DataTable import constructor.
        /// </summary>
        /// <param name="tableName">Optional name for the DataTable. Defaults to the type name.</param>
        /// <returns>A DataTable containing all items and their property values.</returns>
        public DataTable ToDataTable(string tableName = null)
        {
            var dt = new DataTable(tableName ?? typeof(T).Name);
            var properties = GetCachedProperties();

            // Create columns
            foreach (var prop in properties)
            {
                var colType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                // DataTable doesn't support generic types - use string as fallback
                if (colType.IsGenericType || colType.IsArray || !colType.IsValueType && colType != typeof(string))
                    colType = typeof(string);
                dt.Columns.Add(prop.Name, colType);
            }

            // Populate rows
            foreach (var item in Items)
            {
                var row = dt.NewRow();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(item);
                    var colType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    if (colType.IsGenericType || colType.IsArray || !colType.IsValueType && colType != typeof(string))
                    {
                        // Convert complex types to string representation
                        row[prop.Name] = value?.ToString() ?? (object)DBNull.Value;
                    }
                    else
                    {
                        row[prop.Name] = value ?? DBNull.Value;
                    }
                }
                dt.Rows.Add(row);
            }

            return dt;
        }
        #endregion
        #region "Pagination"
        public void SetPageSize(int pageSize)
        {
            if (pageSize <= 0)
                throw new ArgumentException("Page size must be greater than zero.");

            PageSize = pageSize;
            CurrentPage = 1;
            ApplyPaging();
        }

        public void GoToPage(int pageNumber)
        {
            if (pageNumber < 1 || pageNumber > TotalPages)
                throw new ArgumentOutOfRangeException("Invalid page number.");

            CurrentPage = pageNumber;
            ApplyPaging();
        }

        private void ApplyPaging()
        {
            var pagedItems = originalList.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            ResetItems(pagedItems);
            ResetBindings();
        }

        #endregion "Pagination"
        #region"CRUD"
        protected override object AddNewCore()
        {
            var newItem = Activator.CreateInstance<T>();
            Add(newItem);
            return newItem;
        }
        public void AddNew(T item)
        {
            Add(item);
        }
        public void AddNew()
        {
            AddNewCore();
        }
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) return;
            
            var savedSuppress = SuppressNotification;
            var savedRaiseEvents = RaiseListChangedEvents;
            
            SuppressNotification = true;
            RaiseListChangedEvents = false;
            try
            {
                foreach (var item in items)
                {
                    // Use base InsertItem to avoid per-item events
                    int idx = Items.Count;
                    base.InsertItem(idx, item);
                    
                    // Track the item
                    originalList.Add(item);
                    var tr = new Tracking(Guid.NewGuid(), originalList.Count - 1, idx)
                    {
                        EntityState = EntityState.Added,
                        IsNew = true,
                        IsSaved = false
                    };
                    AddTracking(tr);
                    
                    // Hook PropertyChanged
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }
            finally
            {
                SuppressNotification = savedSuppress;
                RaiseListChangedEvents = savedRaiseEvents;
            }
            
            // Fire a single Reset event instead of per-item events
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Removes a range of items with a single notification event.
        /// </summary>
        public void RemoveRange(IEnumerable<T> items)
        {
            if (items == null) return;

            var itemsToRemove = items.ToList(); // Materialize to avoid modification during enumeration
            if (itemsToRemove.Count == 0) return;

            var savedSuppress = SuppressNotification;
            var savedRaiseEvents = RaiseListChangedEvents;

            SuppressNotification = true;
            RaiseListChangedEvents = false;
            try
            {
                foreach (var item in itemsToRemove)
                {
                    int index = Items.IndexOf(item);
                    if (index >= 0)
                    {
                        // Unhook event
                        item.PropertyChanged -= Item_PropertyChanged;
                        DeletedList.Add(item);

                        // Track deletion
                        var tracking = GetTrackingItem(item);
                        if (tracking != null)
                        {
                            int removedOriginalIndex = tracking.OriginalIndex;
                            originalList.RemoveAt(removedOriginalIndex);
                            tracking.EntityState = EntityState.Deleted;
                            tracking.IsSaved = false;

                            // Fix index-shift for remaining trackings
                            foreach (var tr in _trackingsByGuid.Values)
                            {
                                if (tr.OriginalIndex > removedOriginalIndex && tr.EntityState != EntityState.Deleted)
                                    tr.OriginalIndex--;
                            }
                        }
                        else
                        {
                            int originalIndex = originalList.IndexOf(item);
                            if (originalIndex >= 0)
                            {
                                originalList.RemoveAt(originalIndex);
                                var newTr = new Tracking(Guid.NewGuid(), originalIndex, index)
                                {
                                    EntityState = EntityState.Deleted,
                                    IsSaved = false
                                };
                                AddTracking(newTr);
                            }
                        }

                        Items.RemoveAt(index);
                    }
                }
            }
            finally
            {
                SuppressNotification = savedSuppress;
                RaiseListChangedEvents = savedRaiseEvents;
            }

            // Rebuild secondary index after batch removal
            _trackingsByOriginalIndex.Clear();
            foreach (var tr in _trackingsByGuid.Values)
            {
                if (tr.EntityState != EntityState.Deleted)
                    _trackingsByOriginalIndex[tr.OriginalIndex] = tr;
            }

            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Removes all items matching the predicate with a single notification event.
        /// </summary>
        public void RemoveAll(Func<T, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            var itemsToRemove = Items.Where(predicate).ToList();
            RemoveRange(itemsToRemove);
        }

        #endregion "CRUD"
    }
    // Supporting classes have been extracted to separate files:
    // Tracking.cs, EntityState.cs, EntityUpdateInsertLog.cs,
    // PropertyComparer.cs, ObservableChanges.cs, ObservableBindingListEventArgs.cs
}
