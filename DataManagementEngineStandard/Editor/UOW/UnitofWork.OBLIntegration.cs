using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOW
{
    /// <summary>
    /// OBL Integration partial class for UnitofWork.
    /// Contains all passthrough methods/properties that delegate to ObservableBindingList's
    /// enhanced capabilities (Validation, Undo/Redo, Computed, Bookmarks, ThreadSafety,
    /// Freeze, BatchUpdate, Aggregates, Navigation enhancements, Master-Detail).
    /// </summary>
    /// <typeparam name="T">The type of entity.</typeparam>
    public partial class UnitofWork<T>
    {
        #region Phase 3: Validation Passthroughs

        /// <summary>
        /// Gets or sets whether auto-validation is enabled.
        /// When true, each property change triggers validation of the changed item.
        /// </summary>
        public bool IsAutoValidateEnabled
        {
            get => Units?.IsAutoValidateEnabled ?? false;
            set { if (Units != null) Units.IsAutoValidateEnabled = value; }
        }

        /// <summary>
        /// Gets or sets whether commit is blocked when validation errors exist.
        /// When true, CommitAllAsync will skip items that have Error-severity validation failures.
        /// </summary>
        public bool BlockCommitOnValidationError
        {
            get => Units?.BlockCommitOnValidationError ?? true;
            set { if (Units != null) Units.BlockCommitOnValidationError = value; }
        }

        /// <summary>
        /// Validates a single item using OBL's validation framework
        /// (Data Annotations + EntityStructure-based CustomValidator).
        /// </summary>
        /// <param name="item">The item to validate</param>
        /// <returns>Validation result with errors/warnings</returns>
        public ValidationResult ValidateItem(T item)
        {
            if (Units == null || item == null)
                return new ValidationResult();

            return Units.Validate(item);
        }

        /// <summary>
        /// Validates all items in the collection.
        /// </summary>
        /// <returns>Aggregate validation result</returns>
        public ValidationResult ValidateAll()
        {
            if (Units == null)
                return new ValidationResult();

            return Units.ValidateAll();
        }

        /// <summary>
        /// Gets validation errors for a specific item.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>List of validation errors</returns>
        public List<ValidationError> GetErrors(T item)
        {
            if (Units == null || item == null)
                return new List<ValidationError>();

            return Units.GetErrors(item);
        }

        /// <summary>
        /// Gets all items that currently have validation errors.
        /// </summary>
        /// <returns>List of invalid items</returns>
        public List<T> GetInvalidItems()
        {
            if (Units == null)
                return new List<T>();

            return Units.GetInvalidItems();
        }

        #endregion

        #region Phase 4: Undo/Redo Passthroughs

        /// <summary>
        /// Gets or sets whether undo/redo is enabled.
        /// When enabled, property changes and add/remove operations are recorded.
        /// </summary>
        public bool IsUndoEnabled
        {
            get => Units?.IsUndoEnabled ?? false;
            set { if (Units != null) Units.IsUndoEnabled = value; }
        }

        /// <summary>
        /// Gets or sets the maximum number of undo actions to retain.
        /// </summary>
        public int MaxUndoDepth
        {
            get => Units?.MaxUndoDepth ?? 100;
            set { if (Units != null) Units.MaxUndoDepth = value; }
        }

        /// <summary>
        /// Gets whether there are actions that can be undone.
        /// </summary>
        public bool CanUndo => Units?.CanUndo ?? false;

        /// <summary>
        /// Gets whether there are actions that can be redone.
        /// </summary>
        public bool CanRedo => Units?.CanRedo ?? false;

        /// <summary>
        /// Undoes the last recorded action (property change, add, or remove).
        /// </summary>
        /// <returns>True if the undo was successful</returns>
        public bool Undo() => Units?.Undo() ?? false;

        /// <summary>
        /// Redoes the last undone action.
        /// </summary>
        /// <returns>True if the redo was successful</returns>
        public bool Redo() => Units?.Redo() ?? false;

        /// <summary>
        /// Clears all undo/redo history.
        /// </summary>
        public void ClearUndoHistory() => Units?.ClearUndoHistory();

        #endregion

        #region Phase 7: Computed Columns Passthroughs

        /// <summary>
        /// Registers a computed column that is evaluated on demand per item.
        /// </summary>
        /// <param name="name">Unique name for the computed column</param>
        /// <param name="computation">Function that computes the value from an item</param>
        public void RegisterComputed(string name, Func<T, object> computation)
            => Units?.RegisterComputed(name, computation);

        /// <summary>
        /// Unregisters a computed column.
        /// </summary>
        /// <param name="name">Name of the computed column to remove</param>
        public void UnregisterComputed(string name)
            => Units?.UnregisterComputed(name);

        /// <summary>
        /// Gets the computed value for an item and column name.
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="name">The computed column name</param>
        /// <returns>The computed value</returns>
        public object GetComputed(T item, string name)
            => Units?.GetComputed(item, name);

        /// <summary>
        /// Gets all computed values for an item.
        /// </summary>
        /// <param name="item">The item</param>
        /// <returns>Dictionary of computed column name → value</returns>
        public Dictionary<string, object> GetAllComputed(T item)
            => Units?.GetAllComputed(item) ?? new Dictionary<string, object>();

        /// <summary>
        /// Gets the registered computed column names.
        /// </summary>
        public IReadOnlyCollection<string> ComputedColumnNames
            => Units?.ComputedColumnNames ?? (IReadOnlyCollection<string>)Array.Empty<string>();

        #endregion

        #region Phase 8: Bookmarks Passthroughs

        /// <summary>
        /// Sets a named bookmark at the current position.
        /// </summary>
        /// <param name="name">Bookmark name</param>
        public void SetBookmark(string name) => Units?.SetBookmark(name);

        /// <summary>
        /// Navigates to a named bookmark position.
        /// </summary>
        /// <param name="name">Bookmark name</param>
        /// <returns>True if the bookmark was found and navigation succeeded</returns>
        public bool GoToBookmark(string name) => Units?.GoToBookmark(name) ?? false;

        /// <summary>
        /// Removes a named bookmark.
        /// </summary>
        /// <param name="name">Bookmark name</param>
        public void RemoveBookmark(string name) => Units?.RemoveBookmark(name);

        /// <summary>
        /// Clears all bookmarks.
        /// </summary>
        public void ClearBookmarks() => Units?.ClearBookmarks();

        #endregion

        #region Phase 9: Thread Safety, Freeze, Batch Update Passthroughs

        /// <summary>
        /// Gets or sets whether thread-safe (reader-writer lock) operations are enabled.
        /// </summary>
        public bool IsThreadSafe
        {
            get => Units?.IsThreadSafe ?? false;
            set { if (Units != null) Units.IsThreadSafe = value; }
        }

        /// <summary>
        /// Gets whether the collection is currently frozen (read-only).
        /// When frozen, add/remove/set operations throw InvalidOperationException.
        /// </summary>
        public bool IsFrozen => Units?.IsFrozen ?? false;

        /// <summary>
        /// Freezes the collection, making it read-only.
        /// </summary>
        public void Freeze() => Units?.Freeze();

        /// <summary>
        /// Unfreezes the collection, allowing mutations again.
        /// </summary>
        public void Unfreeze() => Units?.Unfreeze();

        /// <summary>
        /// Begins a batch update scope that suppresses change notifications
        /// until the returned IDisposable is disposed. Supports nesting.
        /// </summary>
        /// <returns>An IDisposable that ends the batch when disposed</returns>
        public IDisposable BeginBatchUpdate() => Units?.BeginBatchUpdate();

        #endregion

        #region Phase 10: Aggregates Passthroughs

        /// <summary>Computes the sum of a numeric property across all items.</summary>
        /// <param name="propertyName">Property name</param>
        /// <returns>Sum value</returns>
        public decimal Sum(string propertyName) => Units?.Sum(propertyName) ?? 0m;

        /// <summary>Computes the sum of a numeric property for items matching a predicate.</summary>
        public decimal SumWhere(string propertyName, Func<T, bool> predicate) => Units?.SumWhere(propertyName, predicate) ?? 0m;

        /// <summary>Computes the average of a numeric property across all items.</summary>
        public decimal Average(string propertyName) => Units?.Average(propertyName) ?? 0m;

        /// <summary>Computes the average of a numeric property for items matching a predicate.</summary>
        public decimal AverageWhere(string propertyName, Func<T, bool> predicate) => Units?.AverageWhere(propertyName, predicate) ?? 0m;

        /// <summary>Gets the minimum value of a property across all items.</summary>
        public object Min(string propertyName) => Units?.Min(propertyName);

        /// <summary>Gets the maximum value of a property across all items.</summary>
        public object Max(string propertyName) => Units?.Max(propertyName);

        /// <summary>Counts items matching a predicate.</summary>
        public int CountWhere(Func<T, bool> predicate) => Units?.CountWhere(predicate) ?? 0;

        /// <summary>Groups items by a property value.</summary>
        public Dictionary<object, List<T>> GroupBy(string propertyName) => Units?.GroupBy(propertyName) ?? new Dictionary<object, List<T>>();

        /// <summary>Gets distinct values of a property.</summary>
        public List<object> DistinctValues(string propertyName) => Units?.DistinctValues(propertyName) ?? new List<object>();

        #endregion

        #region Phase 11: Navigation Enhancements Passthroughs

        /// <summary>
        /// Gets whether the current position is at the beginning of the collection.
        /// </summary>
        public bool IsAtBOF => Units?.IsAtBOF ?? true;

        /// <summary>
        /// Gets whether the current position is at the end of the collection.
        /// </summary>
        public bool IsAtEOF => Units?.IsAtEOF ?? true;

        /// <summary>
        /// Gets whether the collection is empty.
        /// </summary>
        public bool IsEmpty => Units?.IsEmpty ?? true;

        /// <summary>
        /// Moves the current position to the specified item.
        /// </summary>
        /// <param name="item">The item to navigate to</param>
        /// <returns>True if the item was found and navigation succeeded</returns>
        public bool MoveToItem(T item) => Units?.MoveToItem(item) ?? false;

        #endregion

        #region Phase 5: Virtual/Lazy Loading Passthroughs

        /// <summary>
        /// Gets whether the collection is in virtual/lazy loading mode.
        /// </summary>
        public bool IsVirtualMode => Units?.IsVirtualMode ?? false;

        /// <summary>
        /// Gets or sets the number of pages to keep in cache.
        /// </summary>
        public int PageCacheSize
        {
            get => Units?.PageCacheSize ?? 3;
            set { if (Units != null) Units.PageCacheSize = value; }
        }

        /// <summary>
        /// Gets the total number of virtual pages.
        /// </summary>
        public int VirtualTotalPages => Units?.VirtualTotalPages ?? 0;

        /// <summary>
        /// Enables virtual mode by setting the total item count and configuring
        /// OBL's data provider to fetch from this UOW's DataSource.
        /// </summary>
        /// <param name="totalCount">Total number of items available</param>
        public void EnableVirtualMode(int totalCount)
        {
            if (Units == null) return;

            Units.SetTotalItemCount(totalCount);
            Units.SetDataProvider(async (pageIndex, pageSize) =>
            {
                // Use DataSource to fetch a page of data
                var filters = new List<AppFilter>
                {
                    new AppFilter { FieldName = "__offset", FilterValue = (pageIndex * pageSize).ToString() },
                    new AppFilter { FieldName = "__limit", FilterValue = pageSize.ToString() }
                };

                try
                {
                    var result = await Get(filters);
                    return result?.ToList() ?? new List<T>();
                }
                catch
                {
                    return new List<T>();
                }
            });
        }

        /// <summary>
        /// Disables virtual mode and clears the data provider.
        /// </summary>
        public void DisableVirtualMode()
        {
            Units?.ClearDataProvider();
        }

        /// <summary>
        /// Navigates to a specific page in virtual mode.
        /// </summary>
        /// <param name="pageNumber">1-based page number</param>
        public async Task GoToPageAsync(int pageNumber)
        {
            if (Units != null)
                await Units.GoToPageAsync(pageNumber);
        }

        /// <summary>
        /// Pre-fetches pages adjacent to the current page for smoother navigation.
        /// </summary>
        public async Task PrefetchAdjacentPagesAsync()
        {
            if (Units != null)
                await Units.PrefetchAdjacentPagesAsync();
        }

        /// <summary>
        /// Invalidates all cached pages, forcing a re-fetch on next access.
        /// </summary>
        public void InvalidatePageCache()
        {
            Units?.InvalidatePageCache();
        }

        #endregion

        #region Phase 6: Master-Detail Passthroughs

        /// <summary>
        /// Registers a child list for automatic master-detail synchronization.
        /// When the master's current position changes, the child list is auto-filtered
        /// to show only items matching the master's current key value.
        /// </summary>
        /// <typeparam name="TChild">Child entity type</typeparam>
        /// <param name="childList">The child ObservableBindingList</param>
        /// <param name="foreignKeyProperty">Property name on TChild that references the master's key</param>
        /// <param name="masterKeyProperty">Property name on T that is the master key</param>
        public void RegisterDetail<TChild>(ObservableBindingList<TChild> childList,
            string foreignKeyProperty, string masterKeyProperty)
            where TChild : class, INotifyPropertyChanged, new()
        {
            Units?.RegisterDetail(childList, foreignKeyProperty, masterKeyProperty);
        }

        /// <summary>
        /// Unregisters a child list from master-detail synchronization.
        /// </summary>
        public void UnregisterDetail<TChild>(ObservableBindingList<TChild> childList)
            where TChild : class, INotifyPropertyChanged, new()
        {
            Units?.UnregisterDetail(childList);
        }

        /// <summary>
        /// Unregisters all child lists from master-detail synchronization.
        /// </summary>
        public void UnregisterAllDetails()
        {
            Units?.UnregisterAllDetails();
        }

        /// <summary>
        /// Gets the registered detail lists (read-only).
        /// </summary>
        public IReadOnlyList<object> DetailLists
            => Units?.DetailLists ?? (IReadOnlyList<object>)Array.Empty<object>();

        #endregion
    }
}
