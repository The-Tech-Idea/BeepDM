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
    public partial class ObservableBindingList<T> : BindingList<T>, IBindingListView, INotifyCollectionChanged, IDisposable where T : class, INotifyPropertyChanged, new()
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
        public int TotalPages => (int)Math.Ceiling((double)(_currentWorkingSet?.Count ?? originalList.Count) / PageSize);
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
        private bool _isPositionChanging = false;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region "Shared Fields - relocated from partial files for cross-boundary access"

        // Sort fields (from ObservableBindingList.Sort.cs)
        private bool isSorted;
        private PropertyDescriptor sortProperty;
        private ListSortDirection sortDirection;

        // Filter fields (from ObservableBindingList.Filter.cs)
        private string filterString;
        private List<T> originalList = new List<T>();
        private List<T> DeletedList = new List<T>();

        // Current position (from ObservableBindingList.CurrentAndMovement.cs)
        private int _currentIndex = -1;

        // Logging fields
        private Dictionary<T, Dictionary<string, object>> ChangedValues = new Dictionary<T, Dictionary<string, object>>();
        public bool IsLogging { get; set; } = false;
        public Dictionary<Guid, EntityUpdateInsertLog> UpdateLog { get; set; }

        /// <summary>
        /// Configurable identity of the current user, used for Tracking.ModifiedBy.
        /// Defaults to Environment.UserName.
        /// </summary>
        public string CurrentUser { get; set; } = Environment.UserName;

        #endregion

        #region "Phase 2 — Pipeline State Fields"

        /// <summary>
        /// Snapshot of items in their original insertion order.
        /// Used by RemoveSortCore to restore pre-sort order.
        /// Kept in sync by InsertItem, RemoveItem, AddRange, RemoveRange.
        /// </summary>
        private List<T> _insertionOrderList = new List<T>();

        /// <summary>
        /// The predicate-based filter that is currently active (from ApplyFilter(Func&lt;T,bool&gt;)).
        /// Null when no predicate filter is active.
        /// </summary>
        private Func<T, bool> _activeFilterPredicate;

        /// <summary>
        /// The PropertyDescriptor used by the active sort. Null when no sort is active.
        /// </summary>
        private PropertyDescriptor _activeSortProperty;

        /// <summary>
        /// The direction of the active sort.
        /// </summary>
        private ListSortDirection _activeSortDirection;

        /// <summary>
        /// True when pagination is active (SetPageSize has been called).
        /// </summary>
        private bool _isPagingActive;

        /// <summary>
        /// Count of items after filtering but before pagination.
        /// </summary>
        private int _filteredCount;

        /// <summary>
        /// The filtered+sorted working set used as the source for pagination.
        /// Null when no filter or sort has been applied.
        /// </summary>
        private List<T> _currentWorkingSet;

        // --- Public read-only properties ---

        /// <summary>True when a filter predicate or filter string is active.</summary>
        public bool IsFiltered => _activeFilterPredicate != null || !string.IsNullOrWhiteSpace(filterString);

        /// <summary>True when pagination is active.</summary>
        public bool IsPaged => _isPagingActive;

        /// <summary>Total count of items in the master list (before any filtering).</summary>
        public int TotalCount => originalList.Count;

        /// <summary>Count of items after filtering (before pagination). If no filter, equals TotalCount.</summary>
        public int FilteredCount => IsFiltered ? _filteredCount : originalList.Count;

        // --- Pipeline events ---

        /// <summary>Raised after a filter has been applied.</summary>
        public event EventHandler FilterApplied;
        /// <summary>Raised after a filter has been removed.</summary>
        public event EventHandler FilterRemoved;
        /// <summary>Raised after a sort has been applied.</summary>
        public event EventHandler SortApplied;
        /// <summary>Raised after a sort has been removed.</summary>
        public event EventHandler SortRemoved;

        #endregion

        #region "Phase 3 — Batch / Commit Events"

        /// <summary>Raised when a batch operation (AddRange, RemoveRange) starts.</summary>
        public event EventHandler<BatchOperationEventArgs> BatchOperationStarted;

        /// <summary>Raised when a batch operation (AddRange, RemoveRange) completes.</summary>
        public event EventHandler<BatchOperationEventArgs> BatchOperationCompleted;

        /// <summary>
        /// Raised BEFORE CommitItemAsync persists an item. Set Cancel = true to abort the commit.
        /// </summary>
        public event EventHandler<CommitEventArgs<T>> BeforeSave;

        /// <summary>
        /// Raised AFTER CommitItemAsync has successfully persisted an item.
        /// </summary>
        public event EventHandler<CommitEventArgs<T>> AfterSave;

        #endregion
    }
    // Supporting classes have been extracted to separate files:
    // Tracking.cs, EntityState.cs, EntityUpdateInsertLog.cs,
    // PropertyComparer.cs, ObservableChanges.cs, ObservableBindingListEventArgs.cs
}
