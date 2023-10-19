using System.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;

namespace DataManagementModels.Editor
{
    public class ObservableBindingList<T> : BindingList<T>, INotifyCollectionChanged where T : INotifyPropertyChanged
    {
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
        private Func<T, bool> _filter = null;
        private IList<T> _originalCollection;
        public Func<T, bool> Filter
        {
            get => _filter;
            set
            {
                _filter = value;
                ApplyFilter();
                OnPropertyChanged("Filter");
            }
        }
        private void ApplyFilter()
        {
            if (_originalCollection == null)
            {
                _originalCollection = new List<T>(this);
            }

            if (_filter == null)
            {
                if (Items.Count != _originalCollection.Count)
                {
                    ClearItems();

                    foreach (var item in _originalCollection)
                    {
                        Add(item);
                    }
                }
            }
            else
            {
                var results = _originalCollection.Where(_filter).ToList();

                ClearItems();

                foreach (var item in results)
                {
                    Add(item);
                }
            }

            // Now we can raise the ListChanged event
            ResetBindings();
        }
        public new void ResetBindings()
        {
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }
        #endregion
        private void HookupCollectionChangedEvent()
        {
            
        }
        public ObservableBindingList() : base()
        {
            HookupCollectionChangedEvent();
            // Initialize the list with no items and subscribe to AddingNew event.
            AddingNew += ObservableBindingList_AddingNew;
        }
        public ObservableBindingList(IEnumerable<T> enumerable) : base()
        {

            foreach (T item in enumerable)
                item.PropertyChanged += Item_PropertyChanged;
            AddingNew += ObservableBindingList_AddingNew;
        }
        public ObservableBindingList(IList<T> list) : base(list)
        {
            foreach (T item in list)
                item.PropertyChanged += Item_PropertyChanged;

            HookupCollectionChangedEvent();

            AddingNew += ObservableBindingList_AddingNew;
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
            int index = IndexOf((T)sender);
            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender, index));
        }
        protected override void RemoveItem(int index)
        {
            T removedItem = this[index];
            base.RemoveItem(index);
            removedItem.PropertyChanged -= Item_PropertyChanged;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, index));
        }
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            item.PropertyChanged += Item_PropertyChanged;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }
        protected override void SetItem(int index, T item)
        {
            T replacedItem = this[index];
            replacedItem.PropertyChanged -= Item_PropertyChanged;
            base.SetItem(index, item);
            item.PropertyChanged += Item_PropertyChanged;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, replacedItem, index));
        }
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
    }
}
