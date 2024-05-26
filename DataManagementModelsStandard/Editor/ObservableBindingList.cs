using System.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;
using System.Linq.Expressions;

namespace DataManagementModels.Editor
{
    public class ObservableBindingList<T> : BindingList<T>, IBindingListView, INotifyCollectionChanged where T : class,INotifyPropertyChanged
    {
     
        protected override object AddNewCore()
        {
            var newItem = Activator.CreateInstance<T>();
            Add(newItem);
            return newItem;
        }
        public List<Tracking> Trackings { get; set; } = new List<Tracking>();
        public bool SuppressNotification { get; set; } = false;
        public bool IsSorted => false;
        public bool IsSynchronized => false;
      
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #region "Current and Movement"
        private int _currentIndex = -1;
        public int CurrentIndex { get { return _currentIndex; } }
        // public T Current => (_currentIndex >= 0 && _currentIndex < Items.Count) ? Items[_currentIndex] : default;
        private T _current;
        public T Current
        {
            get => _current;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_current, value))
                {
                    _current = value;
                    OnPropertyChanged("Current");
                }
            }
        }
        public event EventHandler CurrentChanged;
        protected virtual void OnCurrentChanged()
        {
            CurrentChanged?.Invoke(this, EventArgs.Empty);
        }
        protected override void OnListChanged(ListChangedEventArgs e)
        {
                base.OnListChanged(e);
                if (e.ListChangedType == ListChangedType.ItemChanged && e.NewIndex >= 0 && e.NewIndex < Count)
                {
                    Current = this[e.NewIndex];
                    OnCurrentChanged();
                }
        }
        public bool MoveNext()
        {
            if (_currentIndex < Items.Count - 1)
            {
                _currentIndex++;
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
        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            SuppressNotification = true;
            var items = Items as List<T>;
            if (items != null)
            {
                var property = typeof(T).GetProperty(prop.Name);
                if (property != null)
                {
                    items.Sort((x, y) =>
                    {
                        var valueX = property.GetValue(x);
                        var valueY = property.GetValue(y);
                        return direction == ListSortDirection.Ascending ?
                            Comparer<object>.Default.Compare(valueX, valueY) :
                            Comparer<object>.Default.Compare(valueY, valueX);
                    });

                    isSorted = true;
                    sortProperty = prop;
                    sortDirection = direction;

                    ResetItems(items);
                    SuppressNotification = false;
                    ResetBindings();
                }
            }
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
            isSorted = false;
            sortProperty = null;
            sortDirection = ListSortDirection.Ascending;
            ResetItems(originalList);
        }
        public void ApplySort(ListSortDescriptionCollection sorts)
        {
            SuppressNotification = true;
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
            SuppressNotification = false;
            ResetBindings();
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
        }
        #endregion 
        #region "Find"
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
        public bool SupportsFiltering => true;
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
            SuppressNotification = false;
            ResetBindings();
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
            RaiseListChangedEvents = false;
            // Clear the current items
            ClearItems();
          //  Trackings=new List<Tracking>();
            // Use the copy for adding items to avoid modification issues
            foreach (var item in itemsCopy)
            {
                this.Add(item);
            }
            RaiseListChangedEvents = true;
            if (raiseEvent)
            {
                OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            }

            UpdateIndexTrackingAfterFilterorSort(); // Update index mapping after resetting items

        }
        public new void ResetBindings()
        {
            RaiseListChangedEvents = false;
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            RaiseListChangedEvents = true;
        }
        #endregion
        #region "Constructor"
        public ObservableBindingList() : base()
        {
            // Initialize the list with no items and subscribe to AddingNew event.
            AddingNew += ObservableBindingList_AddingNew;
            this.AllowNew = true;
            this.AllowEdit = true;
            this.AllowRemove = true;
            originalList = new List<T>();
        }
        public ObservableBindingList(IEnumerable<T> enumerable) : base(new List<T>(enumerable))
        {
            foreach (T item in this.Items)
            {
                item.PropertyChanged += Item_PropertyChanged;
                this.Add(item); // Adds the item to the list and hooks up PropertyChanged event
            }
            AddingNew += ObservableBindingList_AddingNew;
            originalList = new List<T>(this.Items);
        }
        public ObservableBindingList(IList<T> list) : base(list)
        {
            foreach (T item in list)
            {

                item.PropertyChanged += Item_PropertyChanged;
                //this.Add(item); // Adds the item to the list and hooks up PropertyChanged event
            }
               

            //  HookupCollectionChangedEvent();

            AddingNew += ObservableBindingList_AddingNew;
            this.AllowNew = true;
            this.AllowEdit = true;
            this.AllowRemove = true;
            originalList = this.Items.ToList();
            UpdateItemIndexMapping(0, true); // Update index mapping after resetting items
        }
        public ObservableBindingList(IBindingListView bindinglist) : base()
        {
            foreach (T item in bindinglist)
            {

                item.PropertyChanged += Item_PropertyChanged;
                this.Add(item); // Adds the item to the list and hooks up PropertyChanged event
            }


            //  HookupCollectionChangedEvent();

            AddingNew += ObservableBindingList_AddingNew;
            this.AllowNew = true;
            this.AllowEdit = true;
            this.AllowRemove = true;
            originalList = this.Items.ToList();
            UpdateItemIndexMapping(0, true); // Update index mapping after resetting items
        }
        public ObservableBindingList(DataTable dataTable) : base()
        {
            //if (dataTable == null)
            //{
            //    throw new ArgumentNullException(nameof(dataTable));
            //}

            foreach (DataRow row in dataTable.Rows)
            {
                T item = GetItem<T>(row);
                if (item != null)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                    this.Items.Add(item); // Adds the item to the list and hooks up PropertyChanged event
                }
               
            }

            AddingNew += ObservableBindingList_AddingNew;
            this.AllowNew = true;
            this.AllowEdit = true;
            this.AllowRemove = true;
            originalList = new List<T>(this.Items);
            UpdateItemIndexMapping(0, true); // Update index mapping after resetting items
        }
        public ObservableBindingList(List<object> objects) : base()
        {
            if (objects == null)
            {
                throw new ArgumentNullException(nameof(objects));
            }

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
        }

        #endregion
        #region "Util Methods"
        private T GetItem<T>(DataRow dr)
        {
            Type temp = typeof(T);
            T obj = Activator.CreateInstance<T>();

            foreach (DataColumn column in dr.Table.Columns)
            {
                foreach (PropertyInfo pro in temp.GetProperties())
                {
                    if (pro.Name == column.ColumnName)
                    {
                        var value = dr[column.ColumnName];

                        if (value == DBNull.Value)
                        {
                            value = pro.PropertyType.IsValueType ? Activator.CreateInstance(pro.PropertyType) : null;
                        }
                        else if (pro.PropertyType == typeof(char) && value is string str && str.Length == 1)
                        {
                            value = str[0];
                        }
                        else if (pro.PropertyType.IsEnum && value is string enumString)
                        {
                            value = Enum.Parse(pro.PropertyType, enumString);
                        }
                        else if (IsNumericType(pro.PropertyType) && value != null)
                        {
                            try
                            {
                                // Convert the value to the property type if it's a numeric type
                                value = Convert.ChangeType(value, pro.PropertyType);
                            }
                            catch (InvalidCastException ex)
                            {
                                // Handle the exception here (e.g., log it or throw a custom exception)
                                throw new InvalidCastException($"Cannot convert value to {pro.PropertyType}: {ex.Message}");
                            }
                        }

                        pro.SetValue(obj, value, null);
                    }
                }
            }
            return obj;
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
            
            // Notify that the entire item has changed, not just a single property
            int index = IndexOf(item);
            
            if (index >= 0)
            {   

                OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
            }

        }
        protected override void RemoveItem(int index)
        {
            T removedItem = this[index];
            base.RemoveItem(index);
            int trackingindex = -1;
            Tracking tracking = null;
            if (Trackings.Count > 0)
            {
                trackingindex = Trackings.FindIndex(p=>p.CurrentIndex==index);
            }
            if(trackingindex != -1)
            {
                tracking = Trackings[trackingindex];
                originalList.RemoveAt(tracking.OriginalIndex);
                if (!string.IsNullOrEmpty(filterString))
                {
                    Items.RemoveAt(index);

                }
            }
            if (Trackings.Count > 0)
            {

                if (trackingindex >= 0)
                {
                    Trackings[trackingindex].EntityState = EntityState.Deleted;
                }

            }


            if (!SuppressNotification)
            {
                removedItem.PropertyChanged -= Item_PropertyChanged;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, index));
            }
           

        }
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);

            if (!SuppressNotification)
            {
                if (RaiseListChangedEvents)
                {
                    Tracking tr = new Tracking(Guid.NewGuid(), index, index);
                    tr.EntityState = EntityState.Added;

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
                    item.PropertyChanged += Item_PropertyChanged;
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
                }

            }

        }
        protected override void SetItem(int index, T item)
        {
            T replacedItem = this[index];
            replacedItem.PropertyChanged -= Item_PropertyChanged;
           
         
            base.SetItem(index, item);
            if (string.IsNullOrEmpty(filterString))
            {
                originalList[index] = item;
                if (Trackings.Count > 0)
                {
                    index = Trackings.Where(p => p.Equals(index)).FirstOrDefault().OriginalIndex;
                    if (index == -1)
                    {
                        Tracking tr=new Tracking(Guid.NewGuid(), index, index);
                        tr.EntityState = EntityState.Modified;
                        Trackings.Add(tr);
                    }

                }
            }
            else
            {
                Tracking tracking =  Trackings.Where(p => p.CurrentIndex==index).FirstOrDefault();
                if (tracking != null)
                {
                    tracking.EntityState = EntityState.Modified;
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
                        }
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
        public int GetItemsIndex(T item)
        {

            return base.Items.IndexOf(item);
        }
        public Tracking GetTrackingITem(T item)
        {
            Tracking retval = null;
            int index = GetItemsIndex(item);
            if (index < 0)
            {
                index = GetOriginalIndex(item);
                retval = Trackings.Where(p => p.OriginalIndex == index).FirstOrDefault();
            }
            else
            {
                retval= Trackings.Where(p => p.CurrentIndex == index).FirstOrDefault();
            }
            return retval;
        }

        #endregion
    }
    public class Tracking
    {
        public Guid UniqueId { get; set; }
        public int OriginalIndex { get; set; }
        public int CurrentIndex { get; set; }
        public EntityState EntityState { get; set; } = EntityState.Unchanged;
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
}
