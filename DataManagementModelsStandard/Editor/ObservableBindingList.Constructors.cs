using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Constructor"
        private void ClearAll()
        {
            originalList = new List<T>();
            _insertionOrderList = new List<T>();
            UpdateLog = new Dictionary<Guid, EntityUpdateInsertLog>();
          
            ChangedValues = new Dictionary<T, Dictionary<string, object>>();

            // Reset pipeline state
            _currentWorkingSet = null;
            _activeFilterPredicate = null;
            _activeSortProperty = null;
            _isPagingActive = false;
            _filteredCount = 0;
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
                    _insertionOrderList.Clear();
                    DeletedList.Clear();
                    UpdateLog?.Clear();
                    ChangedValues?.Clear();
                    _currentWorkingSet = null;
                    _activeFilterPredicate = null;
                    _activeSortProperty = null;
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
            _insertionOrderList = new List<T>(originalList);
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
            _insertionOrderList = new List<T>(originalList);

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
            _insertionOrderList = new List<T>(originalList);

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
                T item = GetItemFromRow(row);
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
            _insertionOrderList = new List<T>(originalList);
          
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
            _insertionOrderList = new List<T>(originalList);
            UpdateItemIndexMapping(0, true); // Update index mapping after resetting items
            SuppressNotification = false;
            RaiseListChangedEvents = true;
        }

        #endregion
    }
}
