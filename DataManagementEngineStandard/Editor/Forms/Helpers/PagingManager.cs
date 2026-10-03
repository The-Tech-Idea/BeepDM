using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;

namespace TheTechIdea.Beep.Editor.Forms.Helpers;

/// <summary>Bookkeeping only; neither fetch-ahead nor datasource reads are performed here.</summary>
public class PagingManager : IPagingManager, ILocalPagingPublication
{
    private sealed class State
    {
        internal int Size = 50, Page = 1, Depth = 1;
        internal long Count;
        internal bool CountKnown;
        internal object Token = new();
    }
    private readonly object _gate = new();
    private readonly Dictionary<string, State> _states = new(StringComparer.OrdinalIgnoreCase);
    private State Get(string name)
    {
        if (!_states.TryGetValue(name, out var state)) _states[name] = state = new();
        return state;
    }
    private static PageInfo Copy(State s) => new() { PageSize = s.Size, PageNumber = s.Page, TotalRecords = s.Count };
    private static int Clamp(int page, int size, long count)
    {
        var pages = size > 0 && count > 0 ? count / size + (count % size == 0 ? 0 : 1) : 1;
        return (int)Math.Max(1, Math.Min((long)page, pages));
    }
    public void SetPageSize(string blockName, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(blockName) || pageSize < 0) return;
        lock (_gate) { var s = Get(blockName); s.Size = pageSize; s.Page = Clamp(s.Page, s.Size, s.Count); s.Token = new(); }
    }
    public int GetPageSize(string blockName) { lock (_gate) return string.IsNullOrWhiteSpace(blockName) ? 50 : Get(blockName).Size; }
    public PageInfo GetCurrentPage(string blockName) { lock (_gate) return Copy(Get(blockName ?? string.Empty)); }
    public PageInfo SetCurrentPage(string blockName, int pageNumber)
    {
        if (string.IsNullOrWhiteSpace(blockName)) return new();
        lock (_gate) { var s = Get(blockName); s.Page = Clamp(pageNumber, s.Size, s.Count); s.Token = new(); return Copy(s); }
    }
    public void SetTotalRecordCount(string blockName, long count)
    {
        if (string.IsNullOrWhiteSpace(blockName)) return;
        lock (_gate) { var s = Get(blockName); s.Count = Math.Max(0, count); s.CountKnown = true; s.Page = Clamp(s.Page, s.Size, s.Count); s.Token = new(); }
    }
    public bool TryGetStoredCount(string blockName, out long count)
    {
        lock (_gate)
        {
            count = 0;
            if (string.IsNullOrWhiteSpace(blockName) || !_states.TryGetValue(blockName, out var s) || !s.CountKnown) return false;
            count = s.Count; return true;
        }
    }
    public long GetTotalRecordCount(string blockName) => TryGetStoredCount(blockName, out var count) ? count : 0;
    public void SetFetchAheadDepth(string blockName, int depth)
    {
        if (string.IsNullOrWhiteSpace(blockName)) return;
        lock (_gate) { var s = Get(blockName); s.Depth = Math.Max(0, depth); s.Token = new(); }
    }
    public int GetFetchAheadDepth(string blockName) { lock (_gate) return string.IsNullOrWhiteSpace(blockName) ? 1 : Get(blockName).Depth; }
    public void ResetPaging(string blockName) { if (!string.IsNullOrWhiteSpace(blockName)) lock (_gate) _states.Remove(blockName); }
    public LocalPagePlan PrepareLocalPage(string blockName, int pageNumber, long loadedCount)
    {
        if (string.IsNullOrWhiteSpace(blockName) || pageNumber <= 0 || loadedCount < 0) throw new ArgumentException("Invalid local page request.");
        lock (_gate)
        {
            var s = Get(blockName);
            if (s.Size == 0) throw new NotSupportedException("Local paging is disabled by page size zero.");
            return new(blockName, s.Token, s.Size, Clamp(pageNumber, s.Size, loadedCount), loadedCount);
        }
    }
    public bool TryPublishLocalPage(LocalPagePlan plan, Action publishOwnedState)
    {
        ArgumentNullException.ThrowIfNull(plan); ArgumentNullException.ThrowIfNull(publishOwnedState);
        lock (_gate)
        {
            if (!_states.TryGetValue(plan.BlockName, out var s) || !ReferenceEquals(s.Token, plan.Token)) return false;
            s.Token = new();
            publishOwnedState();
            s.Page = plan.PageNumber; s.Count = plan.TotalRecords; s.CountKnown = true;
            return true;
        }
    }
    public bool IsLocalPageCurrent(LocalPagePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        lock (_gate) return _states.TryGetValue(plan.BlockName, out var s) && ReferenceEquals(s.Token, plan.Token);
    }
}
