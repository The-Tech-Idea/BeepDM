using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "List and Item Change"
        /// <summary>Handles list change notifications. Suppressed during position changes.</summary>
        protected override void OnListChanged(ListChangedEventArgs e)
        {
            if(SuppressNotification || _isPositionChanging)
            {
                return;
            }
        
            if (!_isPositionChanging) // Check the flag before executing the base method
            {
                // BUG 10 fix: REMOVED _currentIndex = e.NewIndex and OnCurrentChanged()
                // Property edits must NOT silently move the cursor
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

            // --- Phase 1B: Update tracking state on property change ---
            var tracking = GetTrackingItem(item);
            if (tracking != null && tracking.EntityState == EntityState.Unchanged)
            {
                // First modification — snapshot original values BEFORE changing state
                tracking.OriginalValues = SnapshotValues(item);
                tracking.EntityState = EntityState.Modified;
                tracking.ModifiedAt = DateTime.UtcNow;
                tracking.ModifiedBy = CurrentUser;
            }
            if (tracking != null)
            {
                if (!string.IsNullOrEmpty(e.PropertyName) && !tracking.ModifiedProperties.Contains(e.PropertyName))
                    tracking.ModifiedProperties.Add(e.PropertyName);
                tracking.Version++;
            }
            // --- End Phase 1B ---

            // --- Phase 7A: Record property change for undo ---
            if (_isUndoEnabled && !_isUndoing && !string.IsNullOrEmpty(e.PropertyName))
            {
                // Use OriginalValues snapshot if available, otherwise skip
                object oldVal = null;
                if (tracking?.OriginalValues != null && tracking.OriginalValues.ContainsKey(e.PropertyName))
                    oldVal = tracking.OriginalValues[e.PropertyName];
                var prop = typeof(T).GetProperty(e.PropertyName);
                object newVal = prop?.GetValue(item);
                RecordPropertyChangeForUndo(item, e.PropertyName, oldVal, newVal);
            }
            // --- End Phase 7A ---

            // --- Phase 6A: Invalidate computed cache for this item ---
            InvalidateComputedCache(item);
            // --- End Phase 6A ---

            // --- Phase 4: Auto-validate on property change ---
            AutoValidateProperty(item, e.PropertyName);
            // --- End Phase 4 ---

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
        /// <summary>Removes an item at the specified index, tracks deletion, and fires events.</summary>
        protected override void RemoveItem(int index)
        {
            ThrowIfFrozen();
            T removedItem = this[index];

            // --- Phase 7A: Record removal for undo ---
            RecordRemoveForUndo(removedItem, index);
            // --- End Phase 7A ---

            var args = new ItemValidatingEventArgs<T>(removedItem);
            ItemDeleting?.Invoke(this, args);
            if (args.Cancel)
            {
                // Revert change or handle as needed
                throw new InvalidOperationException(args.ErrorMessage);
            }
            // Continue with change notification
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
                // BUG 9 fix: Use reference-based lookup instead of stale OriginalIndex
                int actualIndex = originalList.IndexOf(removedItem);
                if (actualIndex >= 0)
                {
                    removedOriginalIndex = actualIndex;
                    originalList.RemoveAt(actualIndex);
                }
                _insertionOrderList.Remove(removedItem);
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
                    _insertionOrderList.Remove(removedItem);
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
        /// <summary>Inserts an item at the specified index, creates tracking, and fires events.</summary>
        protected override void InsertItem(int index, T item)
        {
            ThrowIfFrozen();

            var args = new ItemValidatingEventArgs<T>(item);
            ItemValidating?.Invoke(this, args);
            if (args.Cancel)
            {
                throw new InvalidOperationException(args.ErrorMessage);
            }
            base.InsertItem(index, item);

            // --- Phase 7A: Record insertion for undo ---
            RecordInsertForUndo(item, index);
            // --- End Phase 7A ---

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
                        // BUG 8 fix: Only insert at view index when no filter/sort is active
                        if (!IsFiltered && !isSorted)
                        {
                            originalList.Insert(index, item);
                            _insertionOrderList.Insert(index, item);
                        }
                        else
                        {
                            // Filter or sort active: append to end of master lists
                            originalList.Add(item);
                            _insertionOrderList.Add(item);
                            tr.OriginalIndex = originalList.Count - 1;
                        }
                    }
                    else
                    {
                        originalList.Add(item);
                        _insertionOrderList.Add(item);
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

        /// <summary>Replaces an item at the specified index, tracks changes, and fires events.</summary>
        protected override void SetItem(int index, T item)
        {
            ThrowIfFrozen();

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
        /// <summary>Occurs when the collection changes (INotifyCollectionChanged implementation).</summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        /// <summary>Raises the CollectionChanged event.</summary>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
        #endregion "List and Item Change"
    }
}
