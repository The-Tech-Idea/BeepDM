﻿using System.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor
{
    public class ObservableBindingList<T> : BindingList<T>, IBindingListView, INotifyCollectionChanged where T : class, INotifyPropertyChanged, new()
    {

        public int PageSize { get; private set; } = 20;
        public int CurrentPage { get; private set; } = 1;
        public int TotalPages => (int)Math.Ceiling((double)originalList.Count / PageSize);
        public event EventHandler<ItemAddedEventArgs<T>> ItemAdded;
        public event EventHandler<ItemRemovedEventArgs<T>> ItemRemoved;
        public event EventHandler<ItemChangedEventArgs<T>> ItemChanged;

        public event EventHandler<ItemValidatingEventArgs<T>> ItemValidating;
        public event EventHandler<ItemValidatingEventArgs<T>> ItemDeleteing;
      
        public List<Tracking> Trackings { get; set; } = new List<Tracking>();
        public bool SuppressNotification { get; set; } = false;
        public bool IsSorted => false;
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
        // public T Current => (_currentIndex >= 0 && _currentIndex < Items.Count) ? Items[_currentIndex] : default;
        //private T _current;
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
                var property = typeof(T).GetProperty(prop.Name);
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
                var property = typeof(T).GetProperty(sortDesc.PropertyDescriptor.Name);
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
        private ListSortDirection _sortDirection;
        public ListSortDirection SortDirection
        {
            get => _sortDirection;
            set
            {
                if (_sortDirection != value)
                {
                    _sortDirection = value;
                    OnPropertyChanged("SortDirection");
                }
            }
        }
        public void Sort(string propertyName)
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;
            var prop = typeof(T).GetProperty(propertyName);
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
            var prop = typeof(T).GetProperty(propertyName);
            if (prop == null)
            {
                throw new ArgumentException($"'{propertyName}' is not a valid property of type '{typeof(T).Name}'.");
            }

            return this.FirstOrDefault(item => object.Equals(prop.GetValue(item), value));
        }
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
            Expression expression = null;

            // Simple tokenization of filter string
            // Example filter: "Name LIKE '%John%' AND Age > 30"
            var filters = filter.Split(new string[] { " AND " }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var f in filters)
            {
                var parts = f.Trim().Split(new[] { ' ' }, 3);
                if (parts.Length < 3)
                    continue;

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

                    comparison = Expression.AndAlso(nullCheck, containsExpression);
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
                        case ">":
                            comparison = Expression.GreaterThan(property, valueExpression);
                            break;
                        case "<":
                            comparison = Expression.LessThan(property, valueExpression);
                            break;
                            // Add other cases as needed
                    }
                }

                if (comparison != null)
                {
                    expression = expression == null ? comparison : Expression.AndAlso(expression, comparison);
                }
            }

            return expression != null ? Expression.Lambda<Func<T, bool>>(expression, parameter) : null;
        }
        private void ResetItems(List<T> items)
        {
          
            // Create a copy of the list for safe iteration
            var itemsCopy = new List<T>(items);

            bool raiseEvent = itemsCopy.Count != this.Count;
            // Use Batch update to minimize events
           
            // Clear the current items
            ClearItems();
          //  Trackings=new List<Tracking>();
            // Use the copy for adding items to avoid modification issues
            foreach (var item in itemsCopy)
            {
                this.Add(item);
            }
          
            //if (raiseEvent)
            //{
            //    OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            //}
           
            UpdateIndexTrackingAfterFilterorSort(); // Update index mapping after resetting items
          
        }
        public new void ResetBindings()
        {
           // RaiseListChangedEvents = false;
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
          //  RaiseListChangedEvents = true;
        }
        #endregion
        #region "Constructor"
        private void ClearAll()
        {
            originalList = new List<T>();
            UpdateLog = new Dictionary<DateTime, EntityUpdateInsertLog>();
          
            ChangedValues = new Dictionary<T, Dictionary<string, object>>();
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
                this.Add(item); // Adds the item to the list and hooks up PropertyChanged event
            }
            AddingNew += ObservableBindingList_AddingNew;
            
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
         //       this.Add(item); // Adds the item to the list and hooks up PropertyChanged event
                originalList.Add(item);
            }
               

            //  HookupCollectionChangedEvent();

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
                this.Add(item); // Adds the item to the list and hooks up PropertyChanged event
                originalList.Add(item);
            }


            //  HookupCollectionChangedEvent();

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
            //if (dataTable == null)
            //{
            //    throw new ArgumentNullException(nameof(dataTable));
            //}
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
        public bool IsLoggin { get; set; } = false;
        public Dictionary<DateTime, EntityUpdateInsertLog> UpdateLog { get; set; }
        private Dictionary<string, object> GetChangedFields(T oldItem, T newItem)
        {
            var changedFields = new Dictionary<string, object>();

            foreach (var property in typeof(T).GetProperties())
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
            ItemDeleteing?.Invoke(this, args);
            if (args.Cancel)
            {
                // Revert change or handle as needed
                throw new InvalidOperationException(args.ErrorMessage);
            }
            // Continue with change notification
            int trackingindex = -1;
            Tracking tracking = null;
            if (Trackings.Count > 0)
            {
                tracking = GetTrackingITem(removedItem);
            }
            if (removedItem != null)
            {
                removedItem.PropertyChanged -= Item_PropertyChanged;
                DeletedList.Add(removedItem);
            }
     //       int deletedindex = DeletedList.IndexOf(removedItem);

            if (IsLoggin)
            {
                CreateLogEntry(removedItem, LogAction.Delete, tracking);
            }
           
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, index));
            Items.RemoveAt(index);
            originalList.RemoveAt(tracking.OriginalIndex);
            ItemRemoved?.Invoke(this, new ItemRemovedEventArgs<T>(removedItem));
            if (tracking != null)
            {
              Trackings.Remove(tracking);
            }
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
                    Trackings.Add(tr);
                    if (IsLoggin)
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
                originalList[index] = item;
                if (Trackings.Count > 0)
                {
                    Tracking tr = Trackings.Where(p => p.Equals(index)).FirstOrDefault();
                    if (tr != null)
                    {
                        index = tr.OriginalIndex;
                    }

                    if (index == -1)
                    {
                        tr = new Tracking(Guid.NewGuid(), index, index);
                        tr.EntityState = EntityState.Modified;
                        Trackings.Add(tr);
                    }
                    if (IsLoggin)
                    {
                        CreateLogEntry(item, LogAction.Update, tr, changedFields);
                    }

                }
            }
            else
            {
                Tracking tracking = Trackings.Where(p => p.CurrentIndex == index).FirstOrDefault();
                if (tracking != null)
                {
                    tracking.EntityState = EntityState.Modified;
                    if (IsLoggin)
                    {

                        CreateLogEntry(item, LogAction.Update, tracking, changedFields);
                    }
                }
                originalList[tracking.OriginalIndex] = item;

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
            for (int i = 0; i < Items.Count; i++)
            {
                int originallistidx = originalList.IndexOf(Items[i]);
                int newlistidx = i;
                if (Trackings.Count > 0)
                {
                    if (originallistidx != -1)
                    {
                        int idx = Trackings.FindIndex(p => p.OriginalIndex == originallistidx);
                        if(idx != -1)
                        {
                            Trackings[idx].CurrentIndex = newlistidx;
                            UpdateLogEntries(Trackings[idx], newlistidx);
                        }
                        else
                        {
                            // Create a new tracking record if one does not exist
                            Tracking newTracking = new Tracking(Guid.NewGuid(), originallistidx, newlistidx)
                            {
                                EntityState = EntityState.Unchanged
                            };
                            Trackings.Add(newTracking);
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
                var tracking = Trackings.FirstOrDefault(t => t.CurrentIndex == i);

                if (tracking != null)
                {
                    tracking.OriginalIndex = originalIndex;
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
                    Trackings.Add(tr);

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
        public T GetItemFroCurrentList(int index)
        {
            if(index>=0 && index < Items.Count)
            {
                return this[index];
            }else
                { return null; }
            
        }
        public Tracking GetTrackingITem(T item)
        {
            Tracking retval = null;
            int index = -1;
            if (DeletedList.Count > 0)
                {
                    index = DeletedList.IndexOf(item);
                    retval = Trackings.Where(p => p.CurrentIndex == index).FirstOrDefault();
                }

            index = GetOriginalIndex(item);
            if (index>-1)
            {
                
                retval = Trackings.Where(p => p.OriginalIndex == index).FirstOrDefault();
            }
            else
            {
                retval= Trackings.Where(p => p.CurrentIndex == index).FirstOrDefault();
            }
            
                
            return retval;
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
            foreach (var item in items)
            {
                Add(item);
            }
        }

        #endregion "CRUD"
    }
    public class Tracking
    {
        public Guid UniqueId { get; set; }
        public int OriginalIndex { get; set; }
        public int CurrentIndex { get; set; }
        public EntityState EntityState { get; set; } = EntityState.Unchanged;
        public bool IsSaved { get; set; } = false;
        public bool IsNew { get; set; } = false;
        public string EntityName { get; set; }
        public string PKFieldName { get; set; }
        public string PKFieldValue { get; set; }
        public string PKFieldNameType { get; set; } // Can Int or string or Guid
        public Tracking(Guid uniqueId, int originalIndex)
        {
            UniqueId = uniqueId;
            OriginalIndex = originalIndex;
            CurrentIndex = originalIndex;
        }
        public Tracking(Guid uniqueId, int originalIndex,int currentindex)
        {
            UniqueId = uniqueId;
            OriginalIndex = originalIndex;
            CurrentIndex = currentindex;
        }

    }
    public enum EntityState
    {
        Added,
        Modified,
        Deleted,
        Unchanged
    }
    public class EntityUpdateInsertLog
    {
        public int ID { get; set; }
        public int RecordID { get; set; }
        public string RecordGuidKey { get; set; }
        public string GuidKey { get; set; } = Guid.NewGuid().ToString();
        public Dictionary<string, object> UpdatedFields { get; set; }
        public DateTime LogDateandTime { get; set; }
        public string LogUser { get; set; }
        public LogAction LogAction { get; set; }
        public string LogEntity { get; set; }
        public Tracking TrackingRecord { get; set; }
        public EntityUpdateInsertLog()
        {

        }
        public EntityUpdateInsertLog(Dictionary<string, object> updatedFields, DateTime logDateandTime, string logUser, LogAction logAction, string logEntity, string recordGuidKey)
        {
            UpdatedFields = updatedFields;
            LogDateandTime = logDateandTime;
            LogUser = logUser;
            LogAction = logAction;
            LogEntity = logEntity;
            RecordGuidKey = recordGuidKey;
        }

        public EntityUpdateInsertLog(Dictionary<string, object> updatedFields, DateTime logDateandTime, string logUser, LogAction logAction, string logEntity)
        {
            UpdatedFields = updatedFields;
            LogDateandTime = logDateandTime;
            LogUser = logUser;
            LogAction = logAction;
            LogEntity = logEntity;
        }
        public EntityUpdateInsertLog(Dictionary<string, object> updatedFields, DateTime logDateandTime, string logUser, LogAction logAction)
        {
            UpdatedFields = updatedFields;
            LogDateandTime = logDateandTime;
            LogUser = logUser;
            LogAction = logAction;
        }
        public EntityUpdateInsertLog(Dictionary<string, object> updatedFields, DateTime logDateandTime, string logUser)
        {
            UpdatedFields = updatedFields;
            LogDateandTime = logDateandTime;
            LogUser = logUser;
        }
        public EntityUpdateInsertLog(Dictionary<string, object> updatedFields, DateTime logDateandTime)
        {
            UpdatedFields = updatedFields;
            LogDateandTime = logDateandTime;
        }
        public EntityUpdateInsertLog(Dictionary<string, object> updatedFields)
        {
            UpdatedFields = updatedFields;
        }



    }
    public class PropertyComparer<T> : IComparer<T>
    {
        private readonly PropertyDescriptor _property;
        private readonly ListSortDirection _direction;

        public PropertyComparer(PropertyDescriptor property, ListSortDirection direction)
        {
            _property = property;
            _direction = direction;
        }

        public int Compare(T x, T y)
        {
            var valueX = _property.GetValue(x);
            var valueY = _property.GetValue(y);

            int result = Comparer.Default.Compare(valueX, valueY);

            return _direction == ListSortDirection.Ascending ? result : -result;
        }
    }
    public class ItemAddedEventArgs<T> : EventArgs
    {
        public T Item { get; }
        public ItemAddedEventArgs(T item) => Item = item;
    }

    public class ItemRemovedEventArgs<T> : EventArgs
    {
        public T Item { get; }
        public ItemRemovedEventArgs(T item) => Item = item;
    }

    public class ItemChangedEventArgs<T> : EventArgs
    {
        public T Item { get; }
        public string PropertyName { get; }
        public ItemChangedEventArgs(T item, string propertyName)
        {
            Item = item;
            PropertyName = propertyName;
        }
    }
    public class ItemValidatingEventArgs<T> : EventArgs
    {
        public T Item { get; }
        public bool Cancel { get; set; } = false;
        public string ErrorMessage { get; set; }

        public ItemValidatingEventArgs(T item)
        {
            Item = item;
        }
    }


}
