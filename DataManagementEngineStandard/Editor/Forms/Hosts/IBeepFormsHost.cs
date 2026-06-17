using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Forms.Builtins;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.Forms.Hosts;

public interface IBeepFormsHost
{
    string FormName { get; set; }
    string? ActiveBlockName { get; }
    string? ActiveItemName { get; }
    IUnitofWorksManager? FormsManager { get; set; }
    IBeepBuiltins? Builtins { get; }

    IReadOnlyList<string> Blocks { get; }

    event EventHandler? ActiveBlockChanged;
    event EventHandler<TriggerExecutingEventArgs>? TriggerExecuting;
    event EventHandler<TriggerExecutedEventArgs>? TriggerExecuted;

    bool TrySetActiveBlock(string blockName);
    bool TrySetActiveItem(string blockName, string itemName);

    bool IsBlockRegistered(string blockName);
    bool IsItemRegistered(string blockName, string itemName);
    IReadOnlyList<string> GetRegisteredBlockNames();
    IReadOnlyList<string> GetRegisteredItemNames(string blockName);

    DataBlockInfo? GetBlockInfo(string blockName);
    IUnitofWork? GetBlockUnitOfWork(string blockName);
    object? GetCurrentBlockItem(string blockName);

    void SetBlockCurrentRecordIndex(string blockName, int index);
    int GetBlockRecordCount(string blockName);
    DataBlockMode GetBlockMode(string blockName);
    void SetBlockMode(string blockName, DataBlockMode mode);
    bool IsBlockQueryAllowed(string blockName);

    bool HasLov(string blockName, string fieldName);
    LOVDefinition? GetLov(string blockName, string fieldName);
    Task<LOVResult> ShowLovAsync(string blockName, string fieldName, string? searchText = null, CancellationToken ct = default);

    RecordValidationResult? ValidateBlockRecord(string blockName, IDictionary<string, object> record, ValidationTiming timing);

    bool TryGetItemProperty(string blockName, string itemName, string property, out object? value);
    bool TrySetItemProperty(string blockName, string itemName, string property, object? value);
    bool TryGetBlockProperty(string blockName, string property, out object? value);
    bool TrySetBlockProperty(string blockName, string property, object? value);

    Task<bool> SaveBlockAsync(string blockName, CancellationToken ct = default);
    Task<bool> RollbackBlockAsync(string blockName, CancellationToken ct = default);
    Task<bool> InsertBlockRecordAsync(string blockName, CancellationToken ct = default);
    Task<bool> DeleteBlockCurrentRecordAsync(string blockName, CancellationToken ct = default);
    Task<bool> ExecuteQueryAsync(string blockName, CancellationToken ct = default);
    Task<bool> ClearBlockAsync(string blockName, CancellationToken ct = default);
    Task<bool> ClearRecordAsync(string blockName, CancellationToken ct = default);

    /// <summary>Post (validate and send to DB without committing). Oracle Forms POST equivalent.</summary>
    Task<bool> PostBlockAsync(string blockName, CancellationToken ct = default);

    /// <summary>Show a modal alert dialog. Returns the 1-based button index the user clicked.</summary>
    Task<int> ShowAlertAsync(string title, string message, BeepBuiltinAlertStyle style,
        string button1Text, string? button2Text = null, string? button3Text = null, CancellationToken ct = default);

    void PublishBuiltinMessage(string message, int messageLevel, BeepBuiltinMessageSeverity severity);
    void ClearBuiltinMessage();
    void RaiseBuiltinTriggerExecuting(TriggerExecutingEventArgs args);
    void RaiseBuiltinTriggerExecuted(TriggerExecutedEventArgs args);
}
