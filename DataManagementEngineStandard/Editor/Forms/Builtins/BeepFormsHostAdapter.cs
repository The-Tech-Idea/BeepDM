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

    public Task<bool> SaveBlockAsync(string blockName, CancellationToken ct) => _host.SaveBlockAsync(blockName);
    public Task<bool> RollbackBlockAsync(string blockName, CancellationToken ct) => _host.RollbackBlockAsync(blockName);
    public Task<bool> InsertBlockRecordAsync(string blockName, CancellationToken ct) => _host.InsertBlockRecordAsync(blockName);
    public Task<bool> DeleteBlockCurrentRecordAsync(string blockName, CancellationToken ct) => _host.DeleteBlockCurrentRecordAsync(blockName);
    public Task<bool> ExecuteQueryAsync(string blockName, CancellationToken ct) => _host.ExecuteQueryAsync(blockName, ct);
    public Task<bool> ClearBlockAsync(string blockName, CancellationToken ct) => _host.ClearBlockAsync(blockName, ct);
    public Task<bool> ClearRecordAsync(string blockName, CancellationToken ct) => _host.ClearRecordAsync(blockName, ct);

    public RecordValidationResult? ValidateBlockRecord(string blockName, IDictionary<string, object> record, ValidationTiming timing)
        => _host.ValidateBlockRecord(blockName, record, timing);

    public void PublishMessage(string message, int messageLevel, BeepBuiltinMessageSeverity severity)
    {
        _host.PublishBuiltinMessage(message, messageLevel, severity);
    }

    public void ClearMessage() => _host.ClearBuiltinMessage();

    public Task<int> ShowAlertAsync(string title, string message, BeepBuiltinAlertStyle style,
        string button1, string? button2, string? button3, CancellationToken ct)
        => Task.FromResult(1);

    public void RaiseBuiltinTriggerExecuting(TriggerExecutingEventArgs args) => _host.RaiseBuiltinTriggerExecuting(args);
    public void RaiseBuiltinTriggerExecuted(TriggerExecutedEventArgs args) => _host.RaiseBuiltinTriggerExecuted(args);

    public object? MultiFormOpenForm(string formName) => null;
    public bool MultiFormCloseForm(string formName) => false;
    public bool MultiFormGoForm(string formName) => false;
    public void MultiFormSetGlobal(string name, object? value) { }
    public object? MultiFormGetGlobal(string name) => null;

    public void SetApplicationProperty(string name, object? value) { }
    public object? GetApplicationProperty(string name) => null;
    public void SetFormProperty(string formName, string name, object? value) { }
    public object? GetFormProperty(string formName, string name) => null;

    public IReadOnlyList<object> ListLovRecords(string blockName, string fieldName) => Array.Empty<object>();
}
