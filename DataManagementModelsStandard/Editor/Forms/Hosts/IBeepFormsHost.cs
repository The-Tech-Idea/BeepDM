using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.UOW.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.Forms.Hosts;

/// <summary>
/// Single host contract. Only what Forms, Blocks, and Fields need.
/// Engine-level operations (SetMessage, ShowAlert, Multi-form, etc.)
/// are accessed through <c>FormsManager</c> directly.
/// </summary>
public interface IBeepFormsHost
{
    string? ActiveBlockName { get; }
    IUnitofWorksManager? FormsManager { get; set; }

    event EventHandler? ActiveBlockChanged;
    event EventHandler<FormsHostMessageEventArgs>? MessageRaised;
    event EventHandler<FormsHostMessageEventArgs>? MessageCleared;
    event EventHandler<FormsHostTimerEventArgs>? TimerFired;
    event EventHandler<FormsHostFormMessageEventArgs>? FormMessageReceived;

    // ── Block lifecycle ───────────────────────────────────────────────────────
    bool RegisterBlock(object blockView);
    bool UnregisterBlock(string blockName);
    bool TrySetActiveBlock(string blockName);
    Task<bool> SwitchToBlockAsync(
        string blockName,
        CancellationToken ct = default);
    bool IsBlockRegistered(string blockName);
    DataBlockInfo? GetBlockInfo(string blockName);

    // ── Data ──────────────────────────────────────────────────────────────────
    IReadOnlyList<EntityField>? GetBlockFields(string blockName);
    System.Collections.IEnumerable? GetBlockData(string blockName);
    object? GetCurrentBlockItem(string blockName);
    int GetCurrentBlockRecordIndex(string blockName);
    object? GetFieldValue(string blockName, string fieldName);
    bool SetFieldValue(string blockName, string fieldName, object? value);
    IEnumerable<string> GetDetailBlockNames(string blockName);

    // ── State ─────────────────────────────────────────────────────────────────
    int GetBlockRecordCount(string blockName);
    DataBlockMode GetBlockMode(string blockName);
    bool IsBlockQueryAllowed(string blockName);
    bool IsFieldQueryAllowed(string blockName, string fieldName);
    bool IsBlockDirty(string blockName);
    ItemInfo? GetItemInfo(string blockName, string fieldName);

    // ── Security ──────────────────────────────────────────────────────────────
    void SetSecurityContext(SecurityContext context);
    SecurityContext? GetSecurityContext();
    void SetBlockSecurity(string blockName, BlockSecurity security);
    BlockSecurity? GetBlockSecurity(string blockName);
    bool IsBlockAllowed(string blockName, SecurityPermission permission);
    void SetFieldSecurity(string blockName, string fieldName, FieldSecurity security);
    FieldSecurity? GetFieldSecurity(string blockName, string fieldName);
    object? GetMaskedFieldValue(string blockName, string fieldName, object? rawValue);
    IReadOnlyList<SecurityViolationEventArgs> GetSecurityViolations();
    void ClearBlockSecurity(string blockName);

    // ── Audit ─────────────────────────────────────────────────────────────────
    void SetAuditUser(string userName);
    IReadOnlyList<AuditEntry> GetAuditLog(
        string? blockName = null,
        AuditOperation? operation = null,
        DateTime? from = null,
        DateTime? to = null);
    IReadOnlyList<AuditFieldChange> GetFieldHistory(
        string blockName,
        string recordKey,
        string fieldName);
    Task ExportAuditToCsvAsync(
        string filePath,
        string? blockName = null);
    Task ExportAuditToJsonAsync(
        string filePath,
        string? blockName = null);
    void PurgeAudit(int olderThanDays);
    void ClearAudit();

    // ── Undo, validation, and dirty state ─────────────────────────────────────
    void SetBlockUndoEnabled(string blockName, bool enabled, int maxDepth = 50);
    bool UndoBlock(string blockName);
    bool RedoBlock(string blockName);
    bool CanUndoBlock(string blockName);
    bool CanRedoBlock(string blockName);
    IReadOnlyDictionary<string, ChangeSummary> GetFormChangeSummary();
    void RegisterCrossBlockRule(CrossBlockValidationRule rule);
    bool UnregisterCrossBlockRule(string ruleName);
    IReadOnlyList<string> ValidateCrossBlock();
    IReadOnlyList<string> GetDirtyBlocks();
    Task<bool> SaveDirtyBlocksAsync(CancellationToken ct = default);
    Task<bool> RollbackDirtyBlocksAsync(CancellationToken ct = default);

    // ── Item properties ───────────────────────────────────────────────────────
    IReadOnlyList<ItemInfo> GetItems(string blockName);
    void SetItemProperty(
        string blockName,
        string fieldName,
        string propertyName,
        object? value);
    object? GetItemProperty(
        string blockName,
        string fieldName,
        string propertyName);
    void SetItemValue(string blockName, string fieldName, object? value);
    object? GetItemValue(string blockName, string fieldName);
    IReadOnlyDictionary<string, object> GetAllItemValues(string blockName);
    void SetAllItemValues(
        string blockName,
        IReadOnlyDictionary<string, object> values);
    IReadOnlyList<string> GetDirtyItems(string blockName);
    void ClearItemDirty(string blockName, string fieldName);
    void ClearAllItemDirtyFlags(string blockName);
    void SetItemError(string blockName, string fieldName, string message);
    void ClearItemError(string blockName, string fieldName);
    IReadOnlyList<ItemInfo> GetItemsWithErrors(string blockName);
    void SetTabOrder(string blockName, IReadOnlyList<string> fieldNames);
    IReadOnlyList<string> GetTabOrder(string blockName);
    string? GetNextItem(string blockName, string currentFieldName);
    string? GetPreviousItem(string blockName, string currentFieldName);
    IReadOnlyList<ItemInfo> GetEditableItems(string blockName, FormMode mode);

    // ── Navigation ────────────────────────────────────────────────────────────
    Task<bool> MoveFirstAsync(string blockName);
    Task<bool> MovePreviousAsync(string blockName);
    Task<bool> MoveNextAsync(string blockName);
    Task<bool> MoveLastAsync(string blockName);
    Task<bool> MoveToRecordAsync(string blockName, int index);
    Task<bool> GoToItemAsync(
        string blockName,
        string fieldName,
        CancellationToken ct = default);

    // ── CRUD ──────────────────────────────────────────────────────────────────
    Task<bool> SaveBlockAsync(string blockName, CancellationToken ct = default);
    Task<bool> RollbackBlockAsync(string blockName, CancellationToken ct = default);
    Task<bool> InsertBlockRecordAsync(string blockName, CancellationToken ct = default);
    Task<bool> DeleteBlockCurrentRecordAsync(string blockName, CancellationToken ct = default);
    Task<bool> ExecuteQueryAsync(string blockName, CancellationToken ct = default);
    Task<bool> ClearBlockAsync(string blockName, CancellationToken ct = default);
    Task<bool> ClearRecordAsync(string blockName, CancellationToken ct = default);
    Task<bool> DuplicateCurrentRecordAsync(string blockName, CancellationToken ct = default);

    // ── Query mode ────────────────────────────────────────────────────────────
    Task<bool> EnterQueryModeAsync(string blockName);
    Task<bool> ExitQueryModeAsync(string blockName);
    Task<bool> ExecuteQueryByExampleAsync(
        string blockName,
        IReadOnlyDictionary<string, QueryCriterion> criteria,
        CancellationToken ct = default);
    void SaveQueryTemplate(
        string blockName,
        string templateName,
        IReadOnlyDictionary<string, QueryCriterion> criteria);
    QueryTemplateInfo? LoadQueryTemplate(string blockName, string templateName);
    IReadOnlyList<QueryTemplateInfo> GetQueryTemplates(string blockName);
    bool DeleteQueryTemplate(string blockName, string templateName);
    IReadOnlyList<QueryHistoryEntry> GetQueryHistory(string blockName);
    void ClearQueryHistory(string blockName);

    // ── Triggers ──────────────────────────────────────────────────────────────
    IReadOnlyList<TriggerDefinition> GetBlockTriggers(string blockName);
    TriggerStatisticsInfo GetTriggerStatistics(string blockName);
    Task<TriggerResult> FireBlockTriggerAsync(
        TriggerType type,
        string blockName,
        TriggerContext? context = null,
        CancellationToken ct = default);
    Task<TriggerResult> FireKeyTriggerAsync(
        KeyTriggerType keyType,
        string blockName);
    void EnableTrigger(string triggerId);
    void DisableTrigger(string triggerId);
    void SuspendTriggers();
    void ResumeTriggers();

    // ── Locking ───────────────────────────────────────────────────────────────
    Task<bool> LockCurrentRecordAsync(
        string blockName,
        CancellationToken ct = default);
    bool UnlockCurrentRecord(string blockName);
    void UnlockAllRecords(string blockName);
    bool IsCurrentRecordLocked(string blockName);
    RecordLockInfo? GetCurrentRecordLockInfo(string blockName);
    IReadOnlyList<RecordLockInfo> GetAllLocks(string blockName);
    LockMode GetLockMode(string blockName);
    void SetLockMode(string blockName, LockMode mode);
    bool GetLockOnEdit(string blockName);
    void SetLockOnEdit(string blockName, bool value);

    // ── Savepoints ────────────────────────────────────────────────────────────
    string CreateSavepoint(string blockName, string? savepointName = null);
    Task<bool> RollbackToSavepointAsync(
        string blockName,
        string savepointName,
        CancellationToken ct = default);
    bool ReleaseSavepoint(string blockName, string savepointName);
    void ReleaseAllSavepoints(string blockName);
    IReadOnlyList<SavepointInfo> GetSavepoints(string blockName);

    // ── History and bookmarks ─────────────────────────────────────────────────
    Task<bool> NavigateBackAsync(string blockName);
    Task<bool> NavigateForwardAsync(string blockName);
    bool CanNavigateBack(string blockName);
    bool CanNavigateForward(string blockName);
    IReadOnlyList<NavigationHistoryEntry> GetNavigationHistory(string blockName);
    void ClearNavigationHistory(string blockName);
    void SetBookmark(string blockName, string bookmarkName);
    bool GoToBookmark(string blockName, string bookmarkName);
    void RemoveBookmark(string blockName, string bookmarkName);
    void ClearBookmarks(string blockName);

    // ── Runtime objects ───────────────────────────────────────────────────────
    TimerDefinition CreateTimer(
        string timerName,
        TimeSpan interval,
        bool repeating = false);
    bool DeleteTimer(string timerName);
    TimerDefinition? GetTimer(string timerName);
    IReadOnlyList<TimerDefinition> GetTimers();
    long GetNextSequence(string sequenceName);
    long PeekNextSequence(string sequenceName);
    void CreateSequence(
        string sequenceName,
        long startValue = 1,
        long incrementBy = 1);
    void ResetSequence(string sequenceName, long startValue = 1);
    bool DropSequence(string sequenceName);
    void CreateRecordGroup(
        string name,
        string dataSourceName,
        string entityName,
        List<AppFilter>? filters = null);
    Task<bool> PopulateRecordGroupAsync(
        string name,
        CancellationToken ct = default);
    RecordGroup? GetRecordGroup(string name);
    IReadOnlyList<RecordGroup> GetRecordGroups();
    bool RemoveRecordGroup(string name);
    void ClearRecordGroups();
    ParameterList CreateParameterList(string name);
    bool DestroyParameterList(string name);
    void SetParameter(string listName, string parameterName, object? value);
    object? GetParameter(string listName, string parameterName);
    bool RemoveParameter(string listName, string parameterName);
    IReadOnlyList<ParameterList> GetParameterLists();
    void ClearParameterList(string listName);

    // ── Multi-form ────────────────────────────────────────────────────────────
    Task<bool> CallFormAsync(
        string formName,
        Dictionary<string, object>? parameters = null,
        FormCallMode mode = FormCallMode.Modal,
        CancellationToken ct = default);
    Task<bool> OpenFormModelessAsync(
        string formName,
        Dictionary<string, object>? parameters = null,
        CancellationToken ct = default);
    Task<bool> NewFormAsync(
        string formName,
        Dictionary<string, object>? parameters = null,
        CancellationToken ct = default);
    Task<bool> ReturnToCallerAsync(
        object? returnData = null,
        CancellationToken ct = default);
    void SetGlobalVariable(string name, object? value);
    object? GetGlobalVariable(string name);
    object? GetFormParameter(string name);
    bool SendParameterToForm(
        string targetFormName,
        string parameterName,
        object? value);
    void PostMessage(
        string targetForm,
        string messageType,
        object? payload = null);
    void BroadcastMessage(
        string messageType,
        object? payload = null);

    // ── State and utilities ───────────────────────────────────────────────────
    FormStateSnapshot SaveFormState();
    Task<bool> RestoreFormStateAsync(
        FormStateSnapshot snapshot,
        CancellationToken ct = default);
    IReadOnlyDictionary<string, object> GetComputedValues(string blockName);
    void FreezeBlock(string blockName);
    void UnfreezeBlock(string blockName);
    void BeginBlockBatchUpdate(string blockName);
    bool RevertCurrentRecord(string blockName);
    Task<bool> RefreshBlockAsync(
        string blockName,
        ConflictMode mode = ConflictMode.ServerWins,
        CancellationToken ct = default);
    ChangeSummary GetBlockChangeSummary(string blockName);
    IReadOnlyList<object> GetDetailedChangeLog(string blockName);
    decimal GetBlockSum(string blockName, string fieldName);
    decimal GetBlockAverage(string blockName, string fieldName);
    Task<double> GetBlockAggregateScalarAsync(
        string blockName,
        string aggregateExpression,
        CancellationToken ct = default);
    Task ExportBlockToJsonAsync(
        string blockName,
        Stream stream,
        CancellationToken ct = default);
    Task ExportBlockToCsvAsync(
        string blockName,
        Stream stream,
        char delimiter = ',',
        CancellationToken ct = default);
    Task<int> ImportBlockFromJsonAsync(
        string blockName,
        Stream stream,
        bool clearFirst = true,
        CancellationToken ct = default);
    Task<int> ImportBlockFromCsvAsync(
        string blockName,
        Stream stream,
        char delimiter = ',',
        bool clearFirst = true,
        bool hasHeaderRow = true,
        CancellationToken ct = default);
    Task GoToPageAsync(
        string blockName,
        int page,
        CancellationToken ct = default);
    Task PrefetchAdjacentPagesAsync(
        string blockName,
        CancellationToken ct = default);
    Task<string> ReadTextFileAsync(
        string path,
        CancellationToken ct = default);
    Task WriteTextFileAsync(
        string path,
        string content,
        CancellationToken ct = default);
    Task AppendTextFileAsync(
        string path,
        string content,
        CancellationToken ct = default);
    void SetClientInfo(string clientInfo);
    string GetClientInfo();
    void SetApplicationProperty(string name, object? value);
    object? GetApplicationProperty(string name);
    bool BeginFormTransaction();
    bool CommitFormTransaction();
    void EndFormTransaction();
    Task<bool> PostBlockAsync(
        string blockName,
        CancellationToken ct = default);
    BlockStatus GetBlockStatus(string blockName);

    // ── LOV ───────────────────────────────────────────────────────────────────
    bool HasLov(string blockName, string fieldName);
    LOVDefinition? GetLov(string blockName, string fieldName);
    Task<LOVResult> LoadLovDataAsync(string blockName, string fieldName, string? searchText = null);
    Task<LOVResult> ShowLovAsync(string blockName, string fieldName, string? searchText = null, CancellationToken ct = default);
    Task<LOVValidationResult> ValidateLovValueAsync(
        string blockName,
        string fieldName,
        object value);
    Dictionary<string, object>? GetLovRelatedFieldValues(LOVDefinition lov, object? selectedItem);

    // ── Validation ────────────────────────────────────────────────────────────
    ItemValidationResult ValidateItem(
        string blockName,
        string fieldName,
        object? value,
        ValidationTiming timing = ValidationTiming.Manual);
    RecordValidationResult? ValidateBlockRecord(string blockName, IDictionary<string, object> record, ValidationTiming timing);

    // ── Messaging ─────────────────────────────────────────────────────────────
    void SetMessage(string message, MessageLevel level = MessageLevel.Info);
    void ClearMessage();
    Task<AlertResult> ShowAlertAsync(
        string title,
        string message,
        AlertStyle style = AlertStyle.None,
        string button1Text = "OK",
        string? button2Text = null,
        string? button3Text = null,
        CancellationToken ct = default);
    void ShowInfo(string message);
    void ShowWarning(string message);
    void ShowError(string message);
}
