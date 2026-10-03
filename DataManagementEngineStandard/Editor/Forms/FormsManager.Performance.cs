using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// FormsManager partial — Phase 7 Performance &amp; Scalability.
    /// Provides paging, lazy-load configuration, and cache management APIs.
    /// </summary>
    public partial class FormsManager
    {
        // ── field (declared in FormsManager.cs via Phase 7 wiring) ──────────
        // private IPagingManager _pagingManager;

        #region Initialization

        private void InitializePerformance()
        {
            // Nothing to subscribe; PagingManager is stateless setup-only.
        }

        #endregion

        #region 7.1 — Paging

        /// <summary>
        /// Sets the page size for a block. When <paramref name="pageSize"/> &gt; 0, paging is active
        /// and callers should use <see cref="LoadPageAsync"/> to navigate pages.
        /// </summary>
        public void SetBlockPageSize(string blockName, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(blockName) || pageSize < 0) return;
            _pagingManager.SetPageSize(blockName, pageSize);

            var block = GetBlock(blockName);
            if (block != null)
            {
                block.Configuration.PageSize = pageSize;
            }
        }

        /// <summary>
        /// Local cursor paging over already loaded, authorized rows; never fetches a provider page.
        /// Returns null unless page state is acknowledged. Use typed local outcomes for partial effects.
        /// </summary>
        public async Task<PageInfo> LoadPageAsync(string blockName, int pageNumber, CancellationToken ct = default)
        {
            var result = await LoadLocalPageWithOutcomeAsync(blockName, pageNumber, ct).ConfigureAwait(false);
            return result.PageStatePublished ? result.Page : null;
        }

        /// <summary>
        /// Returns the total record count for the block from the paging state.
        /// Falls back to <c>UnitOfWork.TotalItemCount</c> if no count was stored.
        /// </summary>
        public long GetTotalRecordCount(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return 0;

            if (_pagingManager is ILocalPagingPublication state && state.TryGetStoredCount(blockName, out var known))
                return known;

            var stored = _pagingManager.GetTotalRecordCount(blockName);
            if (stored > 0) return stored;

            var block = GetBlock(blockName);
            return block?.UnitOfWork?.TotalItemCount ?? 0;
        }

        /// <summary>
        /// Stores the total record count for a block (e.g., from a COUNT(*) query) so that
        /// <see cref="PageInfo.TotalPages"/> can be calculated correctly.
        /// </summary>
        public void SetTotalRecordCount(string blockName, long count)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return;
            _pagingManager.SetTotalRecordCount(blockName, count);
        }

        /// <summary>
        /// Stores intended fetch-ahead depth. This facade does not fetch or cache provider pages.
        /// </summary>
        public void SetFetchAheadDepth(string blockName, int depth)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return;
            _pagingManager.SetFetchAheadDepth(blockName, depth);
            var block = GetBlock(blockName);
            if (block != null) block.Configuration.FetchAheadDepth = depth;
        }

        /// <summary>Exposes the underlying <see cref="IPagingManager"/> for advanced use.</summary>
        public IPagingManager Paging => _pagingManager;

        #endregion

        #region 7.2 — Lazy Loading

        /// <summary>Sets the lazy-load strategy for a data block.</summary>
        public void SetLazyLoadMode(string blockName, LazyLoadMode mode)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return;
            var block = GetBlock(blockName);
            if (block == null) return;

            block.LazyLoadMode = mode;
            block.Configuration.EnableLazyLoad = mode != LazyLoadMode.None;
        }

        /// <summary>Returns the current lazy-load mode for a block.</summary>
        public LazyLoadMode GetLazyLoadMode(string blockName)
        {
            var block = GetBlock(blockName);
            return block?.LazyLoadMode ?? LazyLoadMode.None;
        }

        /// <summary>
        /// Stores an intended fetch limit; this setting alone does not bound managed/provider reads.
        /// </summary>
        public void SetMaxRecordsPerFetch(string blockName, int max)
        {
            if (string.IsNullOrWhiteSpace(blockName) || max <= 0) return;
            var block = GetBlock(blockName);
            if (block != null) block.Configuration.MaxRecordsPerFetch = max;
        }

        #endregion

        #region 7.3 — Cache Management

        /// <summary>
        /// Removes cached block metadata. This does not itself re-query records or refresh a view.
        /// </summary>
        public void InvalidateBlockCache(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return;
            _performanceManager.InvalidateBlockCache(blockName);
        }

        /// <summary>
        /// Overrides the cache TTL for a specific block.
        /// Pass <see cref="TimeSpan.Zero"/> to revert to the global default.
        /// </summary>
        public void SetBlockCacheTtl(string blockName, TimeSpan ttl)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return;
            _performanceManager.SetBlockCacheTtl(blockName, ttl);

            var block = GetBlock(blockName);
            if (block != null) block.Configuration.CacheTtlMinutes = (int)ttl.TotalMinutes;
        }

        /// <summary>Returns a snapshot of cache hit/miss/eviction statistics.</summary>
        public CacheStats GetCacheStats() => _performanceManager.GetCacheStats();

        /// <summary>
        /// Manually triggers a memory-pressure check; evicts LRU entries if managed memory
        /// exceeds <paramref name="thresholdMb"/> megabytes (default 256 MB).
        /// </summary>
        public void CheckCacheMemoryPressure(long thresholdMb = 256)
            => _performanceManager.CheckMemoryPressure(thresholdMb * 1024 * 1024);

        #endregion
    }
}
