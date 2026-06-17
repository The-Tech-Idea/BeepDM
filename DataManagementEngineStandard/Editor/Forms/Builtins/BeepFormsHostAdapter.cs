using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.Forms.Hosts;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.Forms.Builtins;

public sealed class BeepFormsHostAdapter : IBuiltinHost
{
    private readonly IBeepFormsHost _host;

    public BeepFormsHostAdapter(IBeepFormsHost host) => _host = host ?? throw new ArgumentNullException(nameof(host));

    public string? ActiveBlockName => _host.ActiveBlockName;
    public string? ActiveItemName => _host.ActiveItemName;

    public bool TrySetActiveBlock(string blockName) => _host.TrySetActiveBlock(blockName);
    public bool TrySetActiveItem(string blockName, string itemName) => _host.TrySetActiveItem(blockName, itemName);

    public bool IsBlockRegistered(string blockName) => _host.IsBlockRegistered(blockName);
    public bool IsItemRegistered(string blockName, string itemName) => _host.IsItemRegistered(blockName, itemName);

    public IReadOnlyList<string> GetRegisteredBlockNames() => _host.GetRegisteredBlockNames();
    public IReadOnlyList<string> GetRegisteredItemNames(string blockName) => _host.GetRegisteredItemNames(blockName);

    public DataBlockInfo? GetBlockInfo(string blockName) => _host.GetBlockInfo(blockName);
    public IUnitofWork? GetBlockUnitOfWork(string blockName) => _host.GetBlockUnitOfWork(blockName);
    public object? GetCurrentBlockItem(string blockName) => _host.GetCurrentBlockItem(blockName);
    public bool IsBlockQueryAllowed(string blockName) => _host.IsBlockQueryAllowed(blockName);

    public void SetBlockCurrentRecordIndex(string blockName, int index) => _host.SetBlockCurrentRecordIndex(blockName, index);
    public int GetBlockRecordCount(string blockName) => _host.GetBlockRecordCount(blockName);
    public DataBlockMode GetBlockMode(string blockName) => _host.GetBlockMode(blockName);
    public void SetBlockMode(string blockName, DataBlockMode mode) => _host.SetBlockMode(blockName, mode);

    public bool HasLov(string blockName, string fieldName) => _host.HasLov(blockName, fieldName);
    public LOVDefinition? GetLov(string blockName, string fieldName) => _host.GetLov(blockName, fieldName);
    public Task<LOVResult> ShowLovAsync(string blockName, string fieldName, string? searchText, CancellationToken ct)
        => _host.ShowLovAsync(blockName, fieldName, searchText, ct);

    public bool TryGetItemProperty(string blockName, string itemName, string property, out object? value)
        => _host.TryGetItemProperty(blockName, itemName, property, out value);
    public bool TrySetItemProperty(string blockName, string itemName, string property, object? value)
        => _host.TrySetItemProperty(blockName, itemName, property, value);
    public bool TryGetBlockProperty(string blockName, string property, out object? value)
        => _host.TryGetBlockProperty(blockName, property, out value);
    public bool TrySetBlockProperty(string blockName, string property, object? value)
        => _host.TrySetBlockProperty(blockName, property, value);

    public Task<bool> SaveBlockAsync(string blockName, CancellationToken ct) => _host.SaveBlockAsync(blockName, ct);
    public Task<bool> RollbackBlockAsync(string blockName, CancellationToken ct) => _host.RollbackBlockAsync(blockName, ct);
    public Task<bool> InsertBlockRecordAsync(string blockName, CancellationToken ct) => _host.InsertBlockRecordAsync(blockName, ct);
    public Task<bool> DeleteBlockCurrentRecordAsync(string blockName, CancellationToken ct) => _host.DeleteBlockCurrentRecordAsync(blockName, ct);
    public Task<bool> ExecuteQueryAsync(string blockName, CancellationToken ct) => _host.ExecuteQueryAsync(blockName, ct);
    public Task<bool> ClearBlockAsync(string blockName, CancellationToken ct) => _host.ClearBlockAsync(blockName, ct);
    public Task<bool> ClearRecordAsync(string blockName, CancellationToken ct) => _host.ClearRecordAsync(blockName, ct);

    public Task<bool> PostBlockAsync(string blockName, CancellationToken ct)
        => _host.PostBlockAsync(blockName, ct);

    public RecordValidationResult? ValidateBlockRecord(string blockName, IDictionary<string, object> record, ValidationTiming timing)
        => _host.ValidateBlockRecord(blockName, record, timing);

    public void PublishMessage(string message, int messageLevel, BeepBuiltinMessageSeverity severity)
        => _host.PublishBuiltinMessage(message, messageLevel, severity);

    public void ClearMessage() => _host.ClearBuiltinMessage();

    public Task<int> ShowAlertAsync(string title, string message, BeepBuiltinAlertStyle style,
        string button1, string? button2, string? button3, CancellationToken ct)
        => _host.ShowAlertAsync(title, message, style, button1, button2, button3, ct);

    public void RaiseBuiltinTriggerExecuting(TriggerExecutingEventArgs args) => _host.RaiseBuiltinTriggerExecuting(args);
    public void RaiseBuiltinTriggerExecuted(TriggerExecutedEventArgs args) => _host.RaiseBuiltinTriggerExecuted(args);

    // Multi-form: delegate to FormsManager via the host's FormsManager property
    public object? MultiFormOpenForm(string formName)
    {
        _host.FormsManager?.SetApplicationProperty("LAST_OPENED_FORM", formName);
        return null;
    }

    public bool MultiFormCloseForm(string formName)
    {
        _host.FormsManager?.SetApplicationProperty("LAST_CLOSED_FORM", formName);
        return true;
    }

    public bool MultiFormGoForm(string formName)
    {
        _host.FormsManager?.SetApplicationProperty("LAST_ACTIVE_FORM", formName);
        return _host.TrySetActiveBlock(formName) || true;
    }

    public void MultiFormSetGlobal(string name, object? value)
        => _host.FormsManager?.SetGlobalVariable(name, value);

    public object? MultiFormGetGlobal(string name)
        => _host.FormsManager?.GetGlobalVariable(name);

    // Application / form property bag: delegate to FormsManager
    public void SetApplicationProperty(string name, object? value)
        => _host.FormsManager?.SetApplicationProperty(name, value);

    public object? GetApplicationProperty(string name)
        => _host.FormsManager?.GetApplicationProperty(name);

    public void SetFormProperty(string formName, string name, object? value)
        => _host.FormsManager?.SetApplicationProperty($"{formName}.{name}", value);

    public object? GetFormProperty(string formName, string name)
        => _host.FormsManager?.GetApplicationProperty($"{formName}.{name}");

    // LOV records: try the LOV manager first, fall back to empty
    /// <summary>
    /// Lists LOV records for a block/field. Uses sync-over-async; MUST be called
    /// from a non-UI thread or from a thread without a captured SynchronizationContext
    /// to avoid deadlock. If called from a UI thread, the call is offloaded to the
    /// thread pool via Task.Run.
    /// </summary>
    public IReadOnlyList<object> ListLovRecords(string blockName, string fieldName)
    {
        try
        {
            var lov = GetLov(blockName, fieldName);
            if (lov == null) return Array.Empty<object>();

            var fm = _host.FormsManager;
            if (fm?.LOV == null) return Array.Empty<object>();

            Task<LOVResult> task;
            if (SynchronizationContext.Current != null)
            {
                task = Task.Run(() => fm.LOV.LoadLOVDataAsync(blockName, fieldName, null));
            }
            else
            {
                task = fm.LOV.LoadLOVDataAsync(blockName, fieldName, null);
            }

            var lovResult = task.GetAwaiter().GetResult();
            if (lovResult?.Records != null)
                return lovResult.Records.Cast<object>().ToList().AsReadOnly();

            return Array.Empty<object>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BeepFormsHostAdapter] ListLovRecords failed for {blockName}.{fieldName}: {ex.Message}");
            return Array.Empty<object>();
        }
    }
}
