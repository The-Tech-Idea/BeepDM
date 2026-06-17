using System;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Forms.Models;

public enum BeepUnitOfWorkEventKind
{
    CurrentChanged,
    ItemChanged,
    PreCreate,
    PostCreate,
    PreQuery,
    PostQuery,
    PreInsert,
    PostInsert,
    PreUpdate,
    PostUpdate,
    PostEdit,
    PreDelete,
    PostDelete,
    PreCommit,
    PostCommit
}

public class BeepUnitOfWorkEventArgs : EventArgs
{
    public string BlockName { get; init; } = string.Empty;
    public BeepUnitOfWorkEventKind EventKind { get; init; }
    public IUnitofWork? UnitOfWork { get; init; }
    public UnitofWorkParams? Parameters { get; init; }
    public object? Item { get; init; }
    public string? PropertyName { get; init; }
    public object? CurrentItem { get; init; }

    public bool IsPreEvent => EventKind is
        BeepUnitOfWorkEventKind.PreCreate or
        BeepUnitOfWorkEventKind.PreQuery or
        BeepUnitOfWorkEventKind.PreInsert or
        BeepUnitOfWorkEventKind.PreUpdate or
        BeepUnitOfWorkEventKind.PreDelete or
        BeepUnitOfWorkEventKind.PreCommit;

    public bool IsPostEvent => EventKind is
        BeepUnitOfWorkEventKind.PostCreate or
        BeepUnitOfWorkEventKind.PostQuery or
        BeepUnitOfWorkEventKind.PostInsert or
        BeepUnitOfWorkEventKind.PostUpdate or
        BeepUnitOfWorkEventKind.PostEdit or
        BeepUnitOfWorkEventKind.PostDelete or
        BeepUnitOfWorkEventKind.PostCommit;

    public string ActivityText => EventKind switch
    {
        BeepUnitOfWorkEventKind.CurrentChanged => "Current record changed",
        BeepUnitOfWorkEventKind.ItemChanged => string.IsNullOrWhiteSpace(PropertyName)
            ? "Field changed"
            : $"Field changed: {PropertyName}",
        _ => EventKind.ToString()
    };
}
