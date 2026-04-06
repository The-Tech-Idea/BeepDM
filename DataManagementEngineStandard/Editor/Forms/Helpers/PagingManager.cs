using System;
using System.Collections.Concurrent;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.Forms.Helpers
{
    /// <summary>
    /// Manages per-block paging state for virtual scrolling — Phase 7.
    /// Does not load data itself; callers re-execute their query after calling <see cref="SetCurrentPage"/>.
    /// </summary>
    public class PagingManager : IPagingManager
    {
        private sealed class BlockPageState
        {
            public int PageSize { get; set; } = 50;
            public int CurrentPageNumber { get; set; } = 1;
            public long TotalRecords { get; set; }
            public int FetchAheadDepth { get; set; } = 1;
        }

        private readonly ConcurrentDictionary<string, BlockPageState> _states = new();

        // ── helpers ──────────────────────────────────────────────────────────

        private BlockPageState GetOrCreate(string blockName)
            => _states.GetOrAdd(blockName, _ => new BlockPageState());

        // ── IPagingManager ───────────────────────────────────────────────────

        public void SetPageSize(string blockName, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(blockName) || pageSize <= 0) return;
            GetOrCreate(blockName).PageSize = pageSize;
        }

        public int GetPageSize(string blockName)
            => string.IsNullOrWhiteSpace(blockName) ? 50 : GetOrCreate(blockName).PageSize;

        public PageInfo GetCurrentPage(string blockName)
        {
            var state = GetOrCreate(blockName ?? string.Empty);
            return BuildPageInfo(state);
        }

        public PageInfo SetCurrentPage(string blockName, int pageNumber)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return new PageInfo();
            var state = GetOrCreate(blockName);
            int totalPages = state.PageSize > 0
                ? (int)Math.Ceiling((double)state.TotalRecords / state.PageSize)
                : 1;
            state.CurrentPageNumber = Math.Max(1, Math.Min(pageNumber, Math.Max(1, totalPages)));
            return BuildPageInfo(state);
        }

        public void SetTotalRecordCount(string blockName, long count)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return;
            GetOrCreate(blockName).TotalRecords = Math.Max(0, count);
        }

        public long GetTotalRecordCount(string blockName)
            => string.IsNullOrWhiteSpace(blockName) ? 0 : GetOrCreate(blockName).TotalRecords;

        public void SetFetchAheadDepth(string blockName, int depth)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return;
            GetOrCreate(blockName).FetchAheadDepth = Math.Max(0, depth);
        }

        public int GetFetchAheadDepth(string blockName)
            => string.IsNullOrWhiteSpace(blockName) ? 1 : GetOrCreate(blockName).FetchAheadDepth;

        public void ResetPaging(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return;
            _states.TryRemove(blockName, out _);
        }

        // ── private ──────────────────────────────────────────────────────────

        private static PageInfo BuildPageInfo(BlockPageState state)
            => new PageInfo
            {
                PageNumber = state.CurrentPageNumber,
                PageSize = state.PageSize,
                TotalRecords = state.TotalRecords
            };
    }
}
