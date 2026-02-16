using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Virtual / Lazy Loading — Phase 6C"

        /// <summary>
        /// The async data provider callback: (pageIndex, pageSize) → page items.
        /// Null when not in virtual mode.
        /// </summary>
        private Func<int, int, Task<List<T>>> _dataProvider;

        /// <summary>
        /// Total item count available server-side (used for TotalPages in virtual mode).
        /// </summary>
        private int _virtualTotalCount;

        /// <summary>
        /// Number of pages to cache in memory (current + neighbors). Default: 3.
        /// </summary>
        private int _pageCacheSize = 3;

        /// <summary>
        /// LRU page cache. Key: zero-based page index, Value: page items.
        /// </summary>
        private readonly Dictionary<int, List<T>> _pageCache = new Dictionary<int, List<T>>();

        /// <summary>
        /// Ordered list of cached page indices for LRU eviction.
        /// </summary>
        private readonly List<int> _pageCacheOrder = new List<int>();

        /// <summary>
        /// True when a data provider is set (virtual/lazy loading mode).
        /// </summary>
        public bool IsVirtualMode => _dataProvider != null;

        /// <summary>
        /// Gets or sets the number of pages to keep cached. Default: 3.
        /// </summary>
        public int PageCacheSize
        {
            get => _pageCacheSize;
            set
            {
                if (value < 1) throw new ArgumentException("Page cache size must be at least 1.");
                _pageCacheSize = value;
                TrimPageCache();
            }
        }

        /// <summary>
        /// Sets the async data provider for virtual/lazy loading.
        /// When set, GoToPage will call the provider instead of reading from originalList.
        /// </summary>
        /// <param name="provider">Async function: (pageIndex, pageSize) → List&lt;T&gt;.</param>
        public void SetDataProvider(Func<int, int, Task<List<T>>> provider)
        {
            _dataProvider = provider;
            _pageCache.Clear();
            _pageCacheOrder.Clear();
        }

        /// <summary>
        /// Clears the data provider, returning to normal (non-virtual) mode.
        /// </summary>
        public void ClearDataProvider()
        {
            _dataProvider = null;
            _pageCache.Clear();
            _pageCacheOrder.Clear();
            _virtualTotalCount = 0;
        }

        /// <summary>
        /// Sets the total number of items available server-side.
        /// Used to calculate TotalPages in virtual mode.
        /// </summary>
        public void SetTotalItemCount(int count)
        {
            if (count < 0) throw new ArgumentException("Total count cannot be negative.");
            _virtualTotalCount = count;
        }

        /// <summary>
        /// Gets the total pages in virtual mode (uses _virtualTotalCount instead of originalList.Count).
        /// </summary>
        public int VirtualTotalPages => PageSize > 0
            ? (int)Math.Ceiling((double)_virtualTotalCount / PageSize)
            : 0;

        /// <summary>
        /// Navigates to a page in virtual mode, loading data from the provider.
        /// Falls back to normal GoToPage if not in virtual mode.
        /// </summary>
        public async Task GoToPageAsync(int pageNumber)
        {
            if (!IsVirtualMode)
            {
                // Fall back to normal pagination
                GoToPage(pageNumber);
                return;
            }

            int totalPages = VirtualTotalPages;
            if (pageNumber < 1 || (totalPages > 0 && pageNumber > totalPages))
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Invalid page number.");

            int zeroBasedPage = pageNumber - 1;

            // Check cache first
            List<T> pageItems;
            if (_pageCache.TryGetValue(zeroBasedPage, out pageItems))
            {
                // Move to front of LRU
                _pageCacheOrder.Remove(zeroBasedPage);
                _pageCacheOrder.Add(zeroBasedPage);
            }
            else
            {
                // Load from provider
                pageItems = await _dataProvider(zeroBasedPage, PageSize);
                if (pageItems == null) pageItems = new List<T>();

                // Cache the page
                _pageCache[zeroBasedPage] = pageItems;
                _pageCacheOrder.Add(zeroBasedPage);
                TrimPageCache();
            }

            // Update state
            CurrentPage = pageNumber;
            _isPagingActive = true;

            // Replace visible items
            SuppressNotification = true;
            RaiseListChangedEvents = false;
            try
            {
                Items.Clear();
                foreach (var item in pageItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                    Items.Add(item);
                }
            }
            finally
            {
                SuppressNotification = false;
                RaiseListChangedEvents = true;
            }

            // Clamp cursor
            if (_currentIndex >= Items.Count)
                _currentIndex = Items.Count > 0 ? 0 : -1;

            ResetBindings();
        }

        /// <summary>
        /// Pre-fetches adjacent pages into the cache for smooth navigation.
        /// </summary>
        public async Task PrefetchAdjacentPagesAsync()
        {
            if (!IsVirtualMode || _dataProvider == null) return;

            int zeroBasedCurrent = CurrentPage - 1;
            int totalPages = VirtualTotalPages;

            // Prefetch previous and next pages
            var pagesToFetch = new List<int>();
            if (zeroBasedCurrent > 0 && !_pageCache.ContainsKey(zeroBasedCurrent - 1))
                pagesToFetch.Add(zeroBasedCurrent - 1);
            if (zeroBasedCurrent < totalPages - 1 && !_pageCache.ContainsKey(zeroBasedCurrent + 1))
                pagesToFetch.Add(zeroBasedCurrent + 1);

            foreach (var pageIdx in pagesToFetch)
            {
                var items = await _dataProvider(pageIdx, PageSize);
                if (items != null)
                {
                    _pageCache[pageIdx] = items;
                    _pageCacheOrder.Add(pageIdx);
                }
            }

            TrimPageCache();
        }

        /// <summary>
        /// Invalidates the page cache, forcing reload on next navigation.
        /// </summary>
        public void InvalidatePageCache()
        {
            _pageCache.Clear();
            _pageCacheOrder.Clear();
        }

        /// <summary>
        /// Evicts oldest pages from cache when it exceeds PageCacheSize.
        /// </summary>
        private void TrimPageCache()
        {
            while (_pageCacheOrder.Count > _pageCacheSize)
            {
                int oldest = _pageCacheOrder[0];
                _pageCacheOrder.RemoveAt(0);
                _pageCache.Remove(oldest);
            }
        }

        #endregion
    }
}
