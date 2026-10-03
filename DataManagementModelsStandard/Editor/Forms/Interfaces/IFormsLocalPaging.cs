using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces;

public enum LocalPageState { Completed, InvalidRequest, Unsupported, NavigationRejected, Superseded, Cancelled, Failed }

public sealed class LocalPageResult
{
    public string BlockName { get; internal set; }
    public Guid RegistrationId { get; internal set; }
    public long RequestRevision { get; internal set; }
    public LocalPageState State { get; internal set; }
    public PageInfo Page { get; internal set; }
    public bool PageStatePublished { get; internal set; }
    public bool CursorNavigationAcknowledged { get; internal set; }
    public bool NavigationEffectsPossible { get; internal set; }
    public Exception Error { get; internal set; }
    public string Message { get; internal set; }
    public System.Collections.Generic.IReadOnlyList<Exception> NotificationFailures { get; internal set; } = Array.Empty<Exception>();
}

/// <summary>Optional local cursor paging; never fetches a datasource page.</summary>
public interface IFormsLocalPaging
{
    Task<LocalPageResult> LoadLocalPageWithOutcomeAsync(string blockName, int pageNumber,
        CancellationToken cancellationToken = default);
}

/// <summary>Immutable local-page proposal. Token belongs to the supplying paging helper.</summary>
public sealed class LocalPagePlan
{
    public string BlockName { get; }
    public object Token { get; }
    public int PageSize { get; }
    public int PageNumber { get; }
    public long TotalRecords { get; }
    public PageInfo Page => new() { PageSize = PageSize, PageNumber = PageNumber, TotalRecords = TotalRecords };
    public LocalPagePlan(string blockName, object token, int pageSize, int pageNumber, long totalRecords)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (string.IsNullOrWhiteSpace(blockName) || pageSize <= 0 || pageNumber <= 0 || totalRecords < 0)
            throw new ArgumentException("Invalid local-page proposal.");
        BlockName = blockName; Token = token; PageSize = pageSize; PageNumber = pageNumber; TotalRecords = totalRecords;
    }
}

/// <summary>Optional atomic helper bookkeeping. The callback writes owned manager memory only, synchronously.</summary>
public interface ILocalPagingPublication
{
    bool TryGetStoredCount(string blockName, out long count);
    LocalPagePlan PrepareLocalPage(string blockName, int pageNumber, long loadedCount);
    bool IsLocalPageCurrent(LocalPagePlan plan);
    bool TryPublishLocalPage(LocalPagePlan plan, Action publishOwnedState);
}
