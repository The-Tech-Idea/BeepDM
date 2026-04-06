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
        /// Navigates the UoW cursor to the first record of the specified page and returns
        /// a <see cref="PageInfo"/> describing the new position.
        /// Returns <c>null</c> if the block is not found or the page number is out of range.
        /// </summary>
        public async Task<PageInfo> LoadPageAsync(string blockName, int pageNumber, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return null;

            var block = GetBlock(blockName);
            if (block?.UnitOfWork == null)
            {
                Status = $"Block '{blockName}' not found or has no unit of work";
                return null;
            }

            var pageInfo = _pagingManager.SetCurrentPage(blockName, pageNumber);

            // Sync DataBlockInfo.CurrentPage
            block.CurrentPage = pageInfo.PageNumber;

            // Navigate the UoW cursor to the first record of the page (skip = pageInfo.Skip)
            if (block.UnitOfWork.Units != null)
            {
                int skip = pageInfo.Skip;
                int total = block.UnitOfWork.TotalItemCount;
                int targetIndex = Math.Min(skip, Math.Max(0, total - 1));

                if (total > 0)
                    await NavigateToRecordAsync(blockName, targetIndex).ConfigureAwait(false);
            }

            Status = $"Block '{blockName}' loaded page {pageInfo.PageNumber} of {pageInfo.TotalPages}";
            return pageInfo;
        }

        /// <summary>
        /// Returns the total record count for the block from the paging state.
        /// Falls back to <c>UnitOfWork.TotalItemCount</c> if no count was stored.
        /// </summary>
        public long GetTotalRecordCount(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return 0;

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
        /// Configures how many pages beyond the current one should be pre-fetched.
        /// Set to 0 to disable fetch-ahead.
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
        /// Configures the maximum number of records fetched per load cycle when lazy loading
        /// or paged loading is active.
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
        /// Removes a block from the performance cache, forcing the next read to go to the data source.
        /// Use when an external process has modified the data.
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
