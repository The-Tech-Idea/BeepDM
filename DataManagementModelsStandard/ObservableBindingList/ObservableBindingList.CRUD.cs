using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region"CRUD"
        /// <summary>Creates and adds a new default instance of T to the list.</summary>
        protected override object AddNewCore()
        {
            var newItem = Activator.CreateInstance<T>();
            Add(newItem);
            return newItem;
        }
        /// <summary>Adds an existing item to the list.</summary>
        public void AddNew(T item)
        {
            Add(item);
        }
        /// <summary>Adds a new default item to the list (hides base BindingList.AddNew).</summary>
        public new void AddNew()
        {
            AddNewCore();
        }
        /// <summary>Adds a range of items with a single notification event.</summary>
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) return;

            var itemsList = items as IList<T> ?? items.ToList();
            BatchOperationStarted?.Invoke(this, new BatchOperationEventArgs("AddRange", itemsList.Count));

            var savedSuppress = SuppressNotification;
            var savedRaiseEvents = RaiseListChangedEvents;
            
            SuppressNotification = true;
            RaiseListChangedEvents = false;
            try
            {
                foreach (var item in itemsList)
                {
                    // Use base InsertItem to avoid per-item events
                    int idx = Items.Count;
                    base.InsertItem(idx, item);
                    
                    // Track the item
                    originalList.Add(item);
                    _insertionOrderList.Add(item);
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

            BatchOperationCompleted?.Invoke(this, new BatchOperationEventArgs("AddRange", itemsList.Count));
        }

        /// <summary>
        /// Removes a range of items with a single notification event.
        /// </summary>
        public void RemoveRange(IEnumerable<T> items)
        {
            if (items == null) return;

            var itemsToRemove = items.ToList(); // Materialize to avoid modification during enumeration
            if (itemsToRemove.Count == 0) return;

            BatchOperationStarted?.Invoke(this, new BatchOperationEventArgs("RemoveRange", itemsToRemove.Count));

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
                            // BUG 9 fix: Use reference-based removal
                            int removedOriginalIndex = tracking.OriginalIndex;
                            int actualIndex = originalList.IndexOf(item);
                            if (actualIndex >= 0)
                            {
                                removedOriginalIndex = actualIndex;
                                originalList.RemoveAt(actualIndex);
                            }
                            _insertionOrderList.Remove(item);
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
                                _insertionOrderList.Remove(item);
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

            BatchOperationCompleted?.Invoke(this, new BatchOperationEventArgs("RemoveRange", itemsToRemove.Count));
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
}
