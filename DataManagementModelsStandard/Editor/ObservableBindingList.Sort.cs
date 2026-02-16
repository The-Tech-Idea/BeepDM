using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Sort"
        /// <summary>Indicates that sorting is supported.</summary>
        protected override bool SupportsSortingCore => true;
        /// <summary>Gets whether the list is currently sorted.</summary>
        protected override bool IsSortedCore => isSorted;
        /// <summary>Gets the property used for the current sort.</summary>
        protected override PropertyDescriptor SortPropertyCore => sortProperty;
        /// <summary>Gets the direction of the current sort.</summary>
        protected override ListSortDirection SortDirectionCore => sortDirection;

        /// <summary>Gets the collection of sort descriptions for multi-column sorting.</summary>
        public ListSortDescriptionCollection SortDescriptions { get; }
        /// <summary>Indicates that advanced (multi-column) sorting is supported.</summary>
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
        /// <summary>Sorts the list by the named property in the specified direction.</summary>
        public void ApplySort(string propertyName, ListSortDirection direction)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("Property name cannot be null or empty.");

            PropertyDescriptor propDesc = TypeDescriptor.GetProperties(typeof(T))[propertyName];
            if (propDesc == null)
                throw new ArgumentException($"No property '{propertyName}' in type '{typeof(T).Name}'");

            // Create a comparer based on the property and direction
            var comparer = new PropertyComparer<T>(propDesc, direction);

            // BUG 2 fix: Sort a COPY of originalList, not originalList itself
            var sortedList = new List<T>(originalList);
            sortedList.Sort(comparer);

            // BUG 5 fix: Store active sort state for cross-method awareness
            _activeSortProperty = propDesc;
            _activeSortDirection = direction;
            isSorted = true;
            sortProperty = propDesc;
            sortDirection = direction;

            _currentWorkingSet = sortedList;
            ResetItems(sortedList);

            ResetBindings();
            SortApplied?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Applies a sort using a PropertyDescriptor (IBindingList implementation).</summary>
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

                    // BUG 5 fix: Store active sort state
                    _activeSortProperty = prop;
                    _activeSortDirection = direction;

                    ResetItems(items);
                   
                    ResetBindings();
                    SortApplied?.Invoke(this, EventArgs.Empty);
                }
            }
            SuppressNotification = false;
            RaiseListChangedEvents = true;
        }
        /// <summary>Removes the current sort and restores insertion order.</summary>
        public void RemoveSort()
        {
            if(isSorted)
            {
                RemoveSortCore();
            }
        }
        /// <summary>Core implementation: removes the sort and restores original insertion order.</summary>
        protected override void RemoveSortCore()
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;
            isSorted = false;
            sortProperty = null;
            sortDirection = ListSortDirection.Ascending;
            _activeSortProperty = null;
            _currentWorkingSet = null;
            // BUG 2 fix: Restore from insertion-order backup instead of (mutated) originalList
            ResetItems(_insertionOrderList.Count > 0 ? _insertionOrderList : originalList);
            SuppressNotification = false;
            RaiseListChangedEvents = true;
            SortRemoved?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>Applies a multi-column sort using ListSortDescriptionCollection.</summary>
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

            // BUG 5 fix: Store active sort state (use first sort description as primary)
            if (sorts.Count > 0)
            {
                _activeSortProperty = sorts[0].PropertyDescriptor;
                _activeSortDirection = sorts[0].SortDirection;
                isSorted = true;
                sortProperty = sorts[0].PropertyDescriptor;
                sortDirection = sorts[0].SortDirection;
                SortApplied?.Invoke(this, EventArgs.Empty);
            }

            SuppressNotification = false;
            RaiseListChangedEvents = true;
        }
        /// <summary>Gets or sets the current sort direction.</summary>
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
        /// <summary>Sorts the list by the named property using the current SortDirection.</summary>
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

            // BUG 5 fix: Store active sort state and update tracking
            var propDesc = TypeDescriptor.GetProperties(typeof(T))[propertyName];
            _activeSortProperty = propDesc;
            _activeSortDirection = SortDirection;
            isSorted = true;
            sortProperty = propDesc;

            ResetItems(((List<T>)Items));

            OnPropertyChanged("Item[]");
            SuppressNotification = false;
            RaiseListChangedEvents = true;
            SortApplied?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}
