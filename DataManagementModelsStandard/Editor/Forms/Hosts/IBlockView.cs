using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.Forms.Hosts;

public interface IBlockView
{
    string BlockName { get; set; }
    string ManagerBlockName { get; }
    string EntityName { get; set; }
    string ConnectionName { get; set; }
    bool IsBound { get; }
    bool IsQueryMode { get; }
    bool IsMaster { get; set; }

    IBeepFormsHost? FormsHost { get; }
    object? View { get; }
    IBlockNavigationBar? NavigationBar { get; set; }
    object? Definition { get; set; }
    object? ViewState { get; }

    int RecordCount { get; }
    int CurrentRecordIndex { get; }
    object? CurrentRecord { get; }

    IReadOnlyList<IFieldPresenter> FieldPresenters { get; }
    IFieldPresenter? FindFieldPresenter(string fieldName);
    void AddFieldPresenter(IFieldPresenter presenter);
    void RemoveFieldPresenter(string fieldName);

    event EventHandler<TriggerExecutingEventArgs>? TriggerExecuting;
    event EventHandler<TriggerExecutedEventArgs>? TriggerExecuted;
    event EventHandler<TriggerRegisteredEventArgs>? TriggerRegistered;
    event EventHandler<TriggerUnregisteredEventArgs>? TriggerUnregistered;
    event EventHandler<BeepUnitOfWorkEventArgs>? UnitOfWorkActivity;

    void Bind(IBeepFormsHost formsHost);
    void Unbind();
    void SyncFromManager();

    Task<bool> ExecuteQueryAsync(CancellationToken cancellationToken = default);
    Task<bool> SaveAsync();
    Task<bool> RollbackAsync();
    Task<bool> InsertRecordAsync();
    Task<bool> DeleteCurrentRecordAsync();
    Task<bool> ClearAsync(CancellationToken cancellationToken = default);
    Task<bool> ClearRecordAsync(CancellationToken cancellationToken = default);

    Task<bool> NavigateFirstAsync();
    Task<bool> NavigateLastAsync();
    Task<bool> NavigateNextAsync();
    Task<bool> NavigatePreviousAsync();
    Task<bool> NavigateToRecordAsync(int index);

    void EnterQueryMode();
    void ExitQueryMode();
    void RefreshPresenters();
    void RaiseTriggerExecuting(TriggerExecutingEventArgs args);
    void RaiseTriggerExecuted(TriggerExecutedEventArgs args);
    void RaiseTriggerRegistered(TriggerRegisteredEventArgs args);
    void RaiseTriggerUnregistered(TriggerUnregisteredEventArgs args);
}
