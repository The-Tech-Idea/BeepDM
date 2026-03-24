using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Dirty-State Aggregate Properties"

        /// <summary>True if any tracked entity has a state other than Unchanged.</summary>
        public bool HasChanges => _trackingsByGuid.Values.Any(t => t.EntityState != EntityState.Unchanged);

        /// <summary>All items with Modified or Added state.</summary>
        public List<T> DirtyItems
        {
            get
            {
                var result = new List<T>();
                foreach (var item in Items)
                {
                    var tr = GetTrackingItem(item);
                    if (tr != null && (tr.EntityState == EntityState.Modified || tr.EntityState == EntityState.Added))
                        result.Add(item);
                }
                return result;
            }
        }

        /// <summary>Count of items with Modified or Added state.</summary>
        public int DirtyCount => _trackingsByGuid.Values.Count(t => t.EntityState == EntityState.Modified || t.EntityState == EntityState.Added);

        /// <summary>Count of items with Added state.</summary>
        public int AddedCount => _trackingsByGuid.Values.Count(t => t.EntityState == EntityState.Added);

        /// <summary>Count of items with Modified state.</summary>
        public int ModifiedCount => _trackingsByGuid.Values.Count(t => t.EntityState == EntityState.Modified);

        /// <summary>Count of items with Deleted state.</summary>
        public int DeletedCount => _trackingsByGuid.Values.Count(t => t.EntityState == EntityState.Deleted);

        /// <summary>Read-only list of items with Added state.</summary>
        public IReadOnlyList<T> AddedItems
        {
            get
            {
                var result = new List<T>();
                foreach (var item in Items)
                {
                    var tr = GetTrackingItem(item);
                    if (tr != null && tr.EntityState == EntityState.Added)
                        result.Add(item);
                }
                return result;
            }
        }

        /// <summary>Read-only list of items that have been deleted (in DeletedList).</summary>
        public IReadOnlyList<T> DeletedItems => DeletedList.AsReadOnly();

        #endregion "Dirty-State Aggregate Properties"

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

        /// <summary>Gets the original index of the specified item in the master list.</summary>
        public int GetOriginalIndex(T item)
        {
            return originalList.IndexOf(item);
        }
        /// <summary>Gets the item at the current cursor position.</summary>
        public T GetItem()
        {
            return this.Current;
        }
        /// <summary>Gets an item from the original (unfiltered) list by index.</summary>
        public T GetItemFromOriginalList(int index)
        {
            if (index >= 0 && index < originalList.Count)
            {
                return originalList[index];
            }
            else
            { return null; }
        }
        /// <summary>Gets an item from the current (visible) list by index.</summary>
        public T GetItemFromCurrentList(int index)
        {
            if(index>=0 && index < Items.Count)
            {
                return this[index];
            }else
                { return null; }
            
        }
        
        /// <summary>Obsolete. Use GetItemFromCurrentList instead.</summary>
        [Obsolete("Use GetItemFromCurrentList instead. This method name contains a typo.")]
        public T GetItemFroCurrentList(int index)
        {
            return GetItemFromCurrentList(index);
        }
        /// <summary>Gets the tracking record associated with the specified item.</summary>
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
        /// <summary>Marks an item as committed (saved), resets its tracking state to Unchanged.</summary>
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
                    UpdateLog.Remove(log.LogId);
                }

                // Optionally remove from lists if it was deleted
                if (DeletedList.Contains(item))
                {
                    DeletedList.Remove(item);
                    originalList.Remove(item);
                    _insertionOrderList.Remove(item);
                }

                ResetBindings();
            }
        }

        /// <summary>Resets all tracking state after a bulk commit. Removes deleted items and clears logs.</summary>
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
                    _insertionOrderList.Remove(deletedItem);
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
        /// <summary>Commits a single item asynchronously using the provided insert/update/delete delegates.</summary>
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

            // --- BeforeSave: allow cancellation ---
            var beforeArgs = new CommitEventArgs<T>(item, tracking.EntityState);
            BeforeSave?.Invoke(this, beforeArgs);
            if (beforeArgs.Cancel)
            {
                errors.Flag = Errors.Failed;
                errors.Message = "Save cancelled by BeforeSave handler.";
                return errors;
            }

            // --- Phase 4: Block commit if validation errors ---
            if (BlockCommitOnValidationError && tracking.EntityState != EntityState.Deleted)
            {
                var validationResult = Validate(item);
                if (!validationResult.IsValid)
                {
                    errors.Flag = Errors.Failed;
                    errors.Message = $"Validation failed: {string.Join("; ", validationResult.Errors.Where(e => e.Severity == ValidationSeverity.Error).Select(e => e.ToString()))}";
                    return errors;
                }
            }
            // --- End Phase 4 ---

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
                            _insertionOrderList.Remove(item);
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
                    UpdateLog.Remove(log.LogId);
                }

                // Clear changes
                ChangedValues.Remove(item);

                // --- AfterSave ---
                AfterSave?.Invoke(this, new CommitEventArgs<T>(item, tracking.EntityState));
            }
            catch (Exception ex)
            {
                errors.Flag = Errors.Failed;
                errors.Message = $"CommitItem failed: {ex.Message}";
                errors.Ex = ex;
            }

            return errors;
        }

        #region "Batch Commit — Phase 5A"

        /// <summary>
        /// Commits all pending changes in the default order (Deletes → Updates → Inserts).
        /// Fires BeforeSave/AfterSave per item, calls AcceptChanges for succeeded items,
        /// and rebuilds tracking indexes once at the end.
        /// </summary>
        public Task<CommitResult> CommitAllAsync(
            Func<T, Task<IErrorsInfo>> insertAsync,
            Func<T, Task<IErrorsInfo>> updateAsync,
            Func<T, Task<IErrorsInfo>> deleteAsync)
        {
            return CommitAllAsync(insertAsync, updateAsync, deleteAsync, CommitOrder.DeletesFirst);
        }

        /// <summary>
        /// Commits all pending changes in the specified order.
        /// Returns a CommitResult with per-item success/failure details.
        /// </summary>
        public async Task<CommitResult> CommitAllAsync(
            Func<T, Task<IErrorsInfo>> insertAsync,
            Func<T, Task<IErrorsInfo>> updateAsync,
            Func<T, Task<IErrorsInfo>> deleteAsync,
            CommitOrder order)
        {
            var commitResult = new CommitResult();
            var pending = GetPendingChanges();

            // Build the ordered work list
            var workItems = new List<(T Item, EntityState State)>();

            switch (order)
            {
                case CommitOrder.DeletesFirst:
                    foreach (var item in pending.Deleted) workItems.Add((item, EntityState.Deleted));
                    foreach (var item in pending.Modified) workItems.Add((item, EntityState.Modified));
                    foreach (var item in pending.Added) workItems.Add((item, EntityState.Added));
                    break;

                case CommitOrder.InsertsFirst:
                    foreach (var item in pending.Added) workItems.Add((item, EntityState.Added));
                    foreach (var item in pending.Modified) workItems.Add((item, EntityState.Modified));
                    foreach (var item in pending.Deleted) workItems.Add((item, EntityState.Deleted));
                    break;

                case CommitOrder.AsTracked:
                    // Combine all in the order tracking finds them
                    foreach (var item in pending.Added) workItems.Add((item, EntityState.Added));
                    foreach (var item in pending.Modified) workItems.Add((item, EntityState.Modified));
                    foreach (var item in pending.Deleted) workItems.Add((item, EntityState.Deleted));
                    break;
            }

            if (workItems.Count == 0)
                return commitResult;

            // Suppress notifications during batch
            var savedSuppress = SuppressNotification;
            var savedRaiseEvents = RaiseListChangedEvents;
            SuppressNotification = true;
            RaiseListChangedEvents = false;

            try
            {
                foreach (var (item, state) in workItems)
                {
                    var itemResult = new CommitItemResult
                    {
                        Item = item,
                        EntityState = state
                    };

                    var tracking = GetTrackingItem(item);
                    if (tracking == null)
                    {
                        itemResult.Success = false;
                        itemResult.Errors = new ErrorsInfo { Flag = Errors.Failed, Message = "Tracking info not found." };
                        commitResult.Results.Add(itemResult);
                        continue;
                    }

                    // BeforeSave with cancel
                    var beforeArgs = new CommitEventArgs<T>(item, state);
                    BeforeSave?.Invoke(this, beforeArgs);
                    if (beforeArgs.Cancel)
                    {
                        itemResult.Success = false;
                        itemResult.Errors = new ErrorsInfo { Flag = Errors.Failed, Message = "Cancelled by BeforeSave handler." };
                        commitResult.Results.Add(itemResult);
                        continue;
                    }

                    // Phase 4 validation block (skip for deletes)
                    if (BlockCommitOnValidationError && state != EntityState.Deleted)
                    {
                        var vr = Validate(item);
                        if (!vr.IsValid)
                        {
                            itemResult.Success = false;
                            itemResult.Errors = new ErrorsInfo
                            {
                                Flag = Errors.Failed,
                                Message = $"Validation failed: {string.Join("; ", vr.Errors.Where(e => e.Severity == ValidationSeverity.Error).Select(e => e.ToString()))}"
                            };
                            commitResult.Results.Add(itemResult);
                            continue;
                        }
                    }

                    try
                    {
                        IErrorsInfo opResult;
                        switch (state)
                        {
                            case EntityState.Added:
                                opResult = await insertAsync(item);
                                break;
                            case EntityState.Modified:
                                opResult = await updateAsync(item);
                                break;
                            case EntityState.Deleted:
                                opResult = await deleteAsync(item);
                                break;
                            default:
                                continue;
                        }

                        if (opResult.Flag == Errors.Ok)
                        {
                            itemResult.Success = true;

                            if (state == EntityState.Deleted)
                            {
                                // Defer physical removal — mark tracking
                                DeletedList.Remove(item);
                                originalList.Remove(item);
                                _insertionOrderList.Remove(item);
                                Items.Remove(item);
                                tracking.IsSaved = true;
                                tracking.EntityState = EntityState.Deleted; // keep for cleanup
                            }
                            else
                            {
                                tracking.EntityState = EntityState.Unchanged;
                                tracking.IsSaved = true;
                                tracking.OriginalValues = null;
                                tracking.ModifiedProperties.Clear();
                                tracking.ModifiedAt = null;
                                tracking.ModifiedBy = null;
                                tracking.Version = 0;
                                tracking.IsNew = false;
                            }

                            // Cleanup log/changes for this item
                            var log = UpdateLog?.Values.FirstOrDefault(x => x.TrackingRecord?.UniqueId == tracking.UniqueId);
                            if (log != null) UpdateLog.Remove(log.LogId);
                            ChangedValues?.Remove(item);

                            // AfterSave
                            AfterSave?.Invoke(this, new CommitEventArgs<T>(item, state));
                        }
                        else
                        {
                            itemResult.Success = false;
                            itemResult.Errors = opResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        itemResult.Success = false;
                        itemResult.Errors = new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message, Ex = ex };
                    }

                    commitResult.Results.Add(itemResult);
                }
            }
            finally
            {
                SuppressNotification = savedSuppress;
                RaiseListChangedEvents = savedRaiseEvents;
            }

            // Rebuild tracking indexes ONCE at the end
            _trackingsByOriginalIndex.Clear();
            var toRemove = new List<Guid>();
            foreach (var tr in _trackingsByGuid.Values)
            {
                if (tr.EntityState == EntityState.Deleted && tr.IsSaved)
                {
                    toRemove.Add(tr.UniqueId);
                }
                else if (tr.EntityState != EntityState.Deleted)
                {
                    _trackingsByOriginalIndex[tr.OriginalIndex] = tr;
                }
            }
            foreach (var id in toRemove) _trackingsByGuid.Remove(id);

            // Clamp current index
            if (_currentIndex >= Items.Count)
                _currentIndex = Items.Count - 1;

            // Single reset notification
            OnListChanged(new System.ComponentModel.ListChangedEventArgs(System.ComponentModel.ListChangedType.Reset, -1));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));

            return commitResult;
        }

        #endregion "Batch Commit — Phase 5A"

        /// <summary>Returns all pending (unsaved) changes grouped by Added, Modified, and Deleted.</summary>
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

        #region "Snapshot and Dirty-State Helpers"

        /// <summary>
        /// Creates a snapshot of all property values for the given item.
        /// Uses the static PropertyInfo cache for performance.
        /// </summary>
        private Dictionary<string, object> SnapshotValues(T item)
        {
            var snapshot = new Dictionary<string, object>();
            foreach (var prop in GetCachedProperties())
            {
                if (prop.CanRead)
                {
                    snapshot[prop.Name] = prop.GetValue(item);
                }
            }
            return snapshot;
        }

        /// <summary>
        /// Returns the original (pre-modification) value of a single property.
        /// Returns null if no snapshot exists or the property was not captured.
        /// </summary>
        public object GetOriginalValue(T item, string propertyName)
        {
            var tracking = GetTrackingItem(item);
            if (tracking?.OriginalValues != null && tracking.OriginalValues.TryGetValue(propertyName, out var value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// Reconstructs a new T instance populated with the original (pre-modification) values.
        /// Returns null if no snapshot exists.
        /// </summary>
        public T GetOriginalItem(T item)
        {
            var tracking = GetTrackingItem(item);
            if (tracking?.OriginalValues == null)
                return default;

            var original = new T();
            foreach (var prop in GetCachedProperties())
            {
                if (prop.CanWrite && tracking.OriginalValues.TryGetValue(prop.Name, out var value))
                {
                    prop.SetValue(original, value);
                }
            }
            return original;
        }

        /// <summary>
        /// Returns true if the item's tracking state is anything other than Unchanged.
        /// Returns false if the item is not tracked.
        /// </summary>
        public bool IsDirty(T item)
        {
            var tracking = GetTrackingItem(item);
            return tracking != null && tracking.EntityState != EntityState.Unchanged;
        }

        /// <summary>
        /// Returns a per-property diff between the original snapshot and the current values.
        /// Only includes properties that have actually changed.
        /// Returns an empty dictionary if no snapshot exists.
        /// </summary>
        public Dictionary<string, (object Original, object Current)> GetChanges(T item)
        {
            var result = new Dictionary<string, (object Original, object Current)>();
            var tracking = GetTrackingItem(item);
            if (tracking?.OriginalValues == null)
                return result;

            foreach (var prop in GetCachedProperties())
            {
                if (!prop.CanRead) continue;
                if (!tracking.OriginalValues.TryGetValue(prop.Name, out var originalValue)) continue;

                var currentValue = prop.GetValue(item);
                if (!Equals(originalValue, currentValue))
                {
                    result[prop.Name] = (originalValue, currentValue);
                }
            }
            return result;
        }

        #endregion "Snapshot and Dirty-State Helpers"

        #region "AcceptChanges / RejectChanges"

        /// <summary>
        /// Marks ALL tracked items as Unchanged, clears original-value snapshots,
        /// modified-property lists, DeletedList, UpdateLog, and ChangedValues.
        /// Call after a successful bulk save/commit.
        /// </summary>
        public void AcceptChanges()
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;

            try
            {
                // Remove deleted items permanently
                foreach (var deletedItem in DeletedList.ToList())
                {
                    if (deletedItem is INotifyPropertyChanged npc)
                        npc.PropertyChanged -= Item_PropertyChanged;
                    originalList.Remove(deletedItem);
                    _insertionOrderList.Remove(deletedItem);
                }
                DeletedList.Clear();

                // Reset all tracking records to Unchanged
                foreach (var tr in _trackingsByGuid.Values.ToList())
                {
                    if (tr.EntityState == EntityState.Deleted)
                    {
                        // Tracking for a deleted item that's gone — remove it
                        _trackingsByGuid.Remove(tr.UniqueId);
                        continue;
                    }
                    tr.EntityState = EntityState.Unchanged;
                    tr.OriginalValues = null;
                    tr.ModifiedProperties.Clear();
                    tr.ModifiedAt = null;
                    tr.ModifiedBy = null;
                    tr.Version = 0;
                    tr.IsSaved = false;
                    tr.IsNew = false;
                }

                // Rebuild secondary index
                _trackingsByOriginalIndex.Clear();
                foreach (var tr in _trackingsByGuid.Values)
                {
                    if (tr.EntityState != EntityState.Deleted)
                        _trackingsByOriginalIndex[tr.OriginalIndex] = tr;
                }

                // Clear logging artifacts
                UpdateLog?.Clear();
                ChangedValues?.Clear();
            }
            finally
            {
                SuppressNotification = false;
                RaiseListChangedEvents = true;
            }

            ResetBindings();
        }

        /// <summary>
        /// Marks a single item as Unchanged and clears its original-value snapshot.
        /// </summary>
        public void AcceptChanges(T item)
        {
            var tracking = GetTrackingItem(item);
            if (tracking == null) return;

            if (tracking.EntityState == EntityState.Deleted)
            {
                // Item was deleted — remove permanently
                if (item is INotifyPropertyChanged npc)
                    npc.PropertyChanged -= Item_PropertyChanged;
                DeletedList.Remove(item);
                originalList.Remove(item);
                _insertionOrderList.Remove(item);
                _trackingsByGuid.Remove(tracking.UniqueId);
                if (_trackingsByOriginalIndex.TryGetValue(tracking.OriginalIndex, out var existing) && existing.UniqueId == tracking.UniqueId)
                    _trackingsByOriginalIndex.Remove(tracking.OriginalIndex);
            }
            else
            {
                tracking.EntityState = EntityState.Unchanged;
                tracking.OriginalValues = null;
                tracking.ModifiedProperties.Clear();
                tracking.ModifiedAt = null;
                tracking.ModifiedBy = null;
                tracking.Version = 0;
                tracking.IsSaved = false;
                tracking.IsNew = false;
            }

            // Clean up logging artifacts for this item
            ChangedValues?.Remove(item);
            if (UpdateLog != null)
            {
                var logKey = UpdateLog.FirstOrDefault(kvp => kvp.Value.TrackingRecord?.UniqueId == tracking.UniqueId).Key;
                if (logKey != default)
                    UpdateLog.Remove(logKey);
            }
        }

        /// <summary>
        /// Reverts ALL pending changes: restores Modified items from snapshots,
        /// removes Added items, restores Deleted items. Resets all states to Unchanged.
        /// </summary>
        public void RejectChanges()
        {
            SuppressNotification = true;
            RaiseListChangedEvents = false;

            try
            {
                var trackingsCopy = _trackingsByGuid.Values.ToList();

                foreach (var tr in trackingsCopy)
                {
                    switch (tr.EntityState)
                    {
                        case EntityState.Modified:
                            // Restore original property values
                            if (tr.OriginalValues != null)
                            {
                                // Find the item by current index
                                T item = (tr.CurrentIndex >= 0 && tr.CurrentIndex < Items.Count) ? Items[tr.CurrentIndex] : default;
                                if (item == null && tr.OriginalIndex >= 0 && tr.OriginalIndex < originalList.Count)
                                    item = originalList[tr.OriginalIndex];

                                if (item != null)
                                {
                                    // Temporarily unhook to avoid re-triggering change tracking
                                    item.PropertyChanged -= Item_PropertyChanged;
                                    foreach (var prop in GetCachedProperties())
                                    {
                                        if (prop.CanWrite && tr.OriginalValues.TryGetValue(prop.Name, out var origVal))
                                        {
                                            prop.SetValue(item, origVal);
                                        }
                                    }
                                    item.PropertyChanged += Item_PropertyChanged;

                                    // Update originalList copy too
                                    if (tr.OriginalIndex >= 0 && tr.OriginalIndex < originalList.Count)
                                        originalList[tr.OriginalIndex] = item;
                                }
                            }
                            tr.EntityState = EntityState.Unchanged;
                            tr.OriginalValues = null;
                            tr.ModifiedProperties.Clear();
                            tr.ModifiedAt = null;
                            tr.ModifiedBy = null;
                            tr.Version = 0;
                            break;

                        case EntityState.Added:
                            // Remove added items from all lists
                            if (tr.CurrentIndex >= 0 && tr.CurrentIndex < Items.Count)
                            {
                                T addedItem = Items[tr.CurrentIndex];
                                if (addedItem is INotifyPropertyChanged npc)
                                    npc.PropertyChanged -= Item_PropertyChanged;
                                Items.RemoveAt(tr.CurrentIndex);
                                originalList.Remove(addedItem);
                                _insertionOrderList.Remove(addedItem);
                            }
                            _trackingsByGuid.Remove(tr.UniqueId);
                            break;

                        case EntityState.Deleted:
                            // Restore deleted items back into the list
                            T deletedItem = DeletedList.FirstOrDefault(d =>
                            {
                                var dTr = GetTrackingItem(d);
                                return dTr != null && dTr.UniqueId == tr.UniqueId;
                            });
                            if (deletedItem != null)
                            {
                                DeletedList.Remove(deletedItem);
                                int insertAt = Math.Min(tr.OriginalIndex, Items.Count);
                                Items.Insert(insertAt, deletedItem);
                                originalList.Insert(tr.OriginalIndex, deletedItem);
                                deletedItem.PropertyChanged += Item_PropertyChanged;

                                tr.EntityState = EntityState.Unchanged;
                                tr.CurrentIndex = insertAt;
                                tr.OriginalValues = null;
                                tr.ModifiedProperties.Clear();
                                tr.ModifiedAt = null;
                                tr.ModifiedBy = null;
                                tr.Version = 0;
                            }
                            break;
                    }
                }

                // Rebuild secondary index
                _trackingsByOriginalIndex.Clear();
                foreach (var tr in _trackingsByGuid.Values)
                {
                    if (tr.EntityState != EntityState.Deleted)
                        _trackingsByOriginalIndex[tr.OriginalIndex] = tr;
                }

                UpdateLog?.Clear();
                ChangedValues?.Clear();
            }
            finally
            {
                SuppressNotification = false;
                RaiseListChangedEvents = true;
            }

            ResetBindings();
        }

        /// <summary>
        /// Reverts a single item's changes. Restores Modified items from snapshot,
        /// removes Added items, restores Deleted items.
        /// </summary>
        public void RejectChanges(T item)
        {
            var tracking = GetTrackingItem(item);
            if (tracking == null) return;

            SuppressNotification = true;
            RaiseListChangedEvents = false;

            try
            {
                switch (tracking.EntityState)
                {
                    case EntityState.Modified:
                        if (tracking.OriginalValues != null)
                        {
                            item.PropertyChanged -= Item_PropertyChanged;
                            foreach (var prop in GetCachedProperties())
                            {
                                if (prop.CanWrite && tracking.OriginalValues.TryGetValue(prop.Name, out var origVal))
                                {
                                    prop.SetValue(item, origVal);
                                }
                            }
                            item.PropertyChanged += Item_PropertyChanged;

                            if (tracking.OriginalIndex >= 0 && tracking.OriginalIndex < originalList.Count)
                                originalList[tracking.OriginalIndex] = item;
                        }
                        tracking.EntityState = EntityState.Unchanged;
                        tracking.OriginalValues = null;
                        tracking.ModifiedProperties.Clear();
                        tracking.ModifiedAt = null;
                        tracking.ModifiedBy = null;
                        tracking.Version = 0;
                        break;

                    case EntityState.Added:
                        if (item is INotifyPropertyChanged npc)
                            npc.PropertyChanged -= Item_PropertyChanged;
                        Items.Remove(item);
                        originalList.Remove(item);
                        _insertionOrderList.Remove(item);
                        _trackingsByGuid.Remove(tracking.UniqueId);
                        if (_trackingsByOriginalIndex.TryGetValue(tracking.OriginalIndex, out var existing) && existing.UniqueId == tracking.UniqueId)
                            _trackingsByOriginalIndex.Remove(tracking.OriginalIndex);
                        break;

                    case EntityState.Deleted:
                        DeletedList.Remove(item);
                        int insertAt = Math.Min(tracking.OriginalIndex, Items.Count);
                        Items.Insert(insertAt, item);
                        originalList.Insert(tracking.OriginalIndex, item);
                        item.PropertyChanged += Item_PropertyChanged;
                        tracking.EntityState = EntityState.Unchanged;
                        tracking.CurrentIndex = insertAt;
                        tracking.OriginalValues = null;
                        tracking.ModifiedProperties.Clear();
                        tracking.ModifiedAt = null;
                        tracking.ModifiedBy = null;
                        tracking.Version = 0;
                        break;
                }

                // Clean up logging artifacts
                ChangedValues?.Remove(item);
                if (UpdateLog != null)
                {
                    var logKey = UpdateLog.FirstOrDefault(kvp => kvp.Value.TrackingRecord?.UniqueId == tracking.UniqueId).Key;
                    if (logKey != default)
                        UpdateLog.Remove(logKey);
                }
            }
            finally
            {
                SuppressNotification = false;
                RaiseListChangedEvents = true;
            }

            ResetBindings();
        }

        #endregion "AcceptChanges / RejectChanges"

        #endregion
    }
}
