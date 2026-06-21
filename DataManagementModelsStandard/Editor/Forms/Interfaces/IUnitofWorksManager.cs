using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;


namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{

    public interface IUnitofWorksManager : IDisposable
    {
        /// <summary>Gets the editor instance that owns this FormsManager.</summary>
        IDMEEditor DMEEditor { get; }

        /// <summary>Gets or sets the logical name of the current form.</summary>
        string CurrentFormName { get; set; }

        /// <summary>Gets or sets the logical name of the current active block.</summary>
        string CurrentBlockName { get; set; }

        /// <summary>Gets the registered blocks keyed by block name.</summary>
        IReadOnlyDictionary<string, DataBlockInfo> Blocks { get; }

        /// <summary>Gets whether any registered block currently has unsaved changes.</summary>
        bool IsDirty { get; }

        /// <summary>Returns the names of blocks that currently have unsaved changes.</summary>
        List<string> GetDirtyBlocks();

        /// <summary>Saves all currently dirty blocks.</summary>
        Task<bool> SaveDirtyBlocksAsync();

        /// <summary>Rolls back all currently dirty blocks.</summary>
        Task<bool> RollbackDirtyBlocksAsync();

        /// <summary>Gets the latest status message emitted by the manager.</summary>
        string Status { get; }

        /// <summary>Gets the number of currently registered blocks.</summary>
        int BlockCount { get; }

        /// <summary>Gets the system variables manager</summary>
        ISystemVariablesManager SystemVariables { get; }
        
        /// <summary>Gets the validation manager</summary>
        IValidationManager Validation { get; }
        
        /// <summary>Gets the LOV manager</summary>
        ILOVManager LOV { get; }
        
        /// <summary>Gets the item property manager</summary>
        IItemPropertyManager ItemProperties { get; }
        
        /// <summary>Gets the trigger manager</summary>
        ITriggerManager Triggers { get; }

        /// <summary>Gets the savepoint manager</summary>
        ISavepointManager Savepoints { get; }

        /// <summary>Gets the record locking manager</summary>
        ILockManager Locking { get; }

        /// <summary>Gets the query builder manager</summary>
        IQueryBuilderManager QueryBuilder { get; }

        /// <summary>Gets the per-block error log</summary>
        IBlockErrorLog ErrorLog { get; }

        /// <summary>Gets the message queue manager</summary>
        IMessageQueueManager Messages { get; }

        /// <summary>Gets the block factory</summary>
        IBlockFactory BlockFactory { get; }

        /// <summary>Gets the alert provider for SHOW_ALERT-style dialogs.</summary>
        IAlertProvider AlertProvider { get; }

        /// <summary>Gets the engine-owned named sequence provider.</summary>
        ISequenceProvider Sequences { get; }

        /// <summary>Gets the engine-owned form timer manager.</summary>
        ITimerManager Timers { get; }

        /// <summary>Removes a named sequence.</summary>
        bool DropSequence(string sequenceName);

        /// <summary>Sets the user stamped on subsequent audit entries.</summary>
        void SetAuditUser(string userName);

        /// <summary>Returns audit entries with optional filters.</summary>
        IReadOnlyList<AuditEntry> GetAuditLog(
            string blockName = null,
            AuditOperation? operation = null,
            DateTime? from = null,
            DateTime? to = null);

        /// <summary>Returns field-level audit history.</summary>
        IReadOnlyList<AuditFieldChange> GetFieldHistory(
            string blockName,
            string recordKey,
            string fieldName);

        /// <summary>Exports audit entries to CSV.</summary>
        Task ExportAuditToCsvAsync(string filePath, string blockName = null);

        /// <summary>Exports audit entries to JSON.</summary>
        Task ExportAuditToJsonAsync(string filePath, string blockName = null);

        /// <summary>Purges old audit entries.</summary>
        void PurgeAudit(int olderThanDays);

        /// <summary>Clears the audit store.</summary>
        void ClearAudit();

        /// <summary>Sets the active Forms security context and reapplies effective permissions.</summary>
        void SetSecurityContext(SecurityContext context);

        /// <summary>Gets the active Forms security context.</summary>
        SecurityContext SecurityContext { get; }

        /// <summary>Registers block-level security rules.</summary>
        void SetBlockSecurity(string blockName, BlockSecurity security);

        /// <summary>Gets block-level security rules.</summary>
        BlockSecurity GetBlockSecurity(string blockName);

        /// <summary>Returns whether the active principal may perform the operation.</summary>
        bool IsBlockAllowed(string blockName, SecurityPermission permission);

        /// <summary>Registers field-level security rules.</summary>
        void SetFieldSecurity(string blockName, string fieldName, FieldSecurity security);

        /// <summary>Gets field-level security rules.</summary>
        FieldSecurity GetFieldSecurity(string blockName, string fieldName);

        /// <summary>Returns a display-safe field value after applying security masking.</summary>
        object GetMaskedFieldValue(string blockName, string fieldName, object rawValue);

        /// <summary>Returns security violations recorded for the current session.</summary>
        IReadOnlyList<SecurityViolationEventArgs> GetSecurityViolations();

        /// <summary>Registers a block and resolves its entity metadata from the unit of work when available.</summary>
        void RegisterBlock(string blockName, IUnitofWork unitOfWork,
            string dataSourceName = null, bool isMasterBlock = false);

        /// <summary>Registers a block and its backing unit of work with the manager.</summary>
        void RegisterBlock(string blockName, IUnitofWork unitOfWork, IEntityStructure entityStructure, 
            string dataSourceName = null, bool isMasterBlock = false);

        /// <summary>Unregisters a previously registered block.</summary>
        bool UnregisterBlock(string blockName);

        /// <summary>Returns the registered block metadata for a block name.</summary>
        DataBlockInfo GetBlock(string blockName);

        /// <summary>Returns the backing unit of work for a block name.</summary>
        IUnitofWork GetUnitOfWork(string blockName);

        /// <summary>Returns whether a block is currently registered.</summary>
        bool BlockExists(string blockName);

        /// <summary>
        /// Opens the named datasource, fetches EntityStructure, creates a UnitOfWork,
        /// and registers the block. All datasource work happens inside FormsManager.
        /// UI layers (BeepForms, BeepBlock) must never call IDataSource directly.
        /// </summary>
        Task<bool> SetupBlockAsync(
            string blockName,
            string connectionName,
            string entityName,
            bool isMasterBlock = false,
            CancellationToken cancellationToken = default);

        /// <summary>Synchronizes detail blocks attached to the specified master block.</summary>
        /// <param name="masterBlockName">Master block whose detail blocks should be synchronized.</param>
        /// <param name="ct">Cancellation token; observed at each relationship iteration and at the start of each recursive call.</param>
        Task SynchronizeDetailBlocksAsync(string masterBlockName, CancellationToken ct = default);

        /// <summary>Returns the detail block names attached to a master block.</summary>
        List<string> GetDetailBlocks(string masterBlockName);

        /// <summary>Returns the master block name for a detail block.</summary>
        string GetMasterBlock(string detailBlockName);

        /// <summary>Creates a master-detail relationship between two blocks using a single key field pair.</summary>
        void CreateMasterDetailRelation(string masterBlockName, string detailBlockName,
            string masterKeyField, string detailForeignKeyField,
            RelationshipType relationshipType = RelationshipType.OneToMany);

        /// <summary>Creates a master-detail relationship with composite key field mappings.</summary>
        void CreateMasterDetailRelation(string masterBlockName, string detailBlockName,
            DataBlockFieldMapping[] keyFieldMappings,
            RelationshipType relationshipType = RelationshipType.OneToMany);

        /// <summary>
        /// Returns true if a field change on the given block should trigger detail-block
        /// synchronization. A field change should synchronize if (a) the block is a master,
        /// (b) the changed field is the master key, (c) no master key is defined (sync always),
        /// or (d) the field name is blank.
        /// </summary>
        bool ShouldSynchronizeDetailOnFieldChange(string blockName, string fieldName);

        /// <summary>Opens a form and performs form-level initialization.</summary>
        Task<bool> OpenFormAsync(string formName);

        /// <summary>Closes the current form after resolving unsaved-change behavior.</summary>
        Task<bool> CloseFormAsync();

        /// <summary>Commits changes across the current form.</summary>
        Task<IErrorsInfo> CommitFormAsync();

        /// <summary>Rolls back changes across the current form.</summary>
        Task<IErrorsInfo> RollbackFormAsync();

        /// <summary>Clears all registered blocks.</summary>
        Task ClearAllBlocksAsync();

        /// <summary>Clears a single registered block.</summary>
        Task ClearBlockAsync(string blockName);

        /// <summary>Loads and returns LOV data for a block/field combination.</summary>
        Task<LOVResult> ShowLOVAsync(
            string blockName,
            string fieldName,
            string searchText = null,
            object selectedRecord = null,
            CancellationToken ct = default);

        /// <summary>Navigates to the first record in a block.</summary>
        Task<bool> FirstRecordAsync(string blockName);

        /// <summary>Navigates to the next record in a block.</summary>
        Task<bool> NextRecordAsync(string blockName);

        /// <summary>Navigates to the previous record in a block.</summary>
        Task<bool> PreviousRecordAsync(string blockName);

        /// <summary>Navigates to the last record in a block.</summary>
        Task<bool> LastRecordAsync(string blockName);

        /// <summary>Switches the current active block.</summary>
        Task<bool> SwitchToBlockAsync(string blockName);

        /// <summary>Inserts a record into a block.</summary>
        Task<bool> InsertRecordAsync(string blockName, object record = null);

        /// <summary>Deletes the current record from a block.</summary>
        Task<bool> DeleteCurrentRecordAsync(string blockName);

        /// <summary>Puts a block into query mode.</summary>
        Task<bool> EnterQueryAsync(string blockName);

        /// <summary>Executes a query for a block using the supplied filters.</summary>
        Task<bool> ExecuteQueryAsync(string blockName, List<AppFilter> filters = null);

        /// <summary>Notifies the engine that a host is entering query mode for a block.</summary>
        void EnteringQueryModeAsync(string blockName);

        /// <summary>Notifies the engine that a host has finished executing a query and is exiting query mode.</summary>
        void ExitingQueryModeAsync(string blockName);

        /// <summary>Validates a single field value in a block.</summary>
        bool ValidateField(string blockName, string FieldName, object value);

        /// <summary>Validates the current record state of a block.</summary>
        bool ValidateBlock(string blockName);

        /// <summary>Validates the current form across its registered blocks.</summary>
        bool ValidateForm();


        /// <summary>Enable or disable undo tracking for a block.</summary>
        void SetBlockUndoEnabled(string blockName, bool enable, int maxDepth = 50);

        /// <summary>Undo the last action in a block's OBL undo stack.</summary>
        bool UndoBlock(string blockName);

        /// <summary>Redo the last undone action in a block's OBL undo stack.</summary>
        bool RedoBlock(string blockName);

        /// <summary>Returns whether a block has an undo step available.</summary>
        bool CanUndoBlock(string blockName);

        /// <summary>Returns whether a block has a redo step available.</summary>
        bool CanRedoBlock(string blockName);



        /// <summary>Returns the current change summary for a block.</summary>
        TheTechIdea.Beep.Editor.UOW.Models.ChangeSummary GetBlockChangeSummary(string blockName);

        /// <summary>Returns one ChangeSummary per registered block.</summary>
        IReadOnlyDictionary<string, TheTechIdea.Beep.Editor.UOW.Models.ChangeSummary> GetFormChangeSummary();



        /// <summary>Reload block data from the data source, merging with current edits.</summary>
        Task<bool> RefreshBlockAsync(
            string blockName,
            System.Collections.Generic.List<TheTechIdea.Beep.Report.AppFilter> filters = null,
            ConflictMode conflictMode = ConflictMode.ServerWins,
            System.Threading.CancellationToken ct = default);

        /// <summary>Revert the current record to its original field values.</summary>
        bool RevertCurrentRecord(string blockName);

        /// <summary>Revert the record at a specific index.</summary>
        bool RevertRecord(string blockName, int recordIndex);



        /// <summary>Returns the recorded query history for a block.</summary>
        IReadOnlyList<TheTechIdea.Beep.Editor.UOW.Models.QueryHistoryEntry> GetBlockQueryHistory(string blockName);

        /// <summary>Clears the recorded query history for a block.</summary>
        void ClearBlockQueryHistory(string blockName);



        /// <summary>Sum a numeric field across all loaded records in the block.</summary>
        decimal GetBlockSum(string blockName, string fieldName);

        /// <summary>Average of a numeric field.</summary>
        decimal GetBlockAverage(string blockName, string fieldName);

        /// <summary>Count of records matching optional predicate.</summary>
        int GetBlockCount(string blockName, Func<object, bool> predicate = null);



        /// <summary>Commit all dirty blocks in batches across the whole form.</summary>
        Task<TheTechIdea.Beep.Editor.UOW.Models.CommitBatchResult> CommitFormBatchAsync(
            int batchSize = 200,
            IProgress<TheTechIdea.Beep.Editor.UOW.Models.CommitBatchProgress> progress = null,
            System.Threading.CancellationToken ct = default);

        /// <summary>Commit a single block in batches.</summary>
        Task<TheTechIdea.Beep.Editor.UOW.Models.CommitBatchResult> CommitBlockBatchAsync(
            string blockName,
            int batchSize = 200,
            IProgress<TheTechIdea.Beep.Editor.UOW.Models.CommitBatchProgress> progress = null,
            System.Threading.CancellationToken ct = default);



        /// <summary>Exports a block to JSON using the provided stream.</summary>
        Task ExportBlockToJsonAsync(string blockName, System.IO.Stream stream,
            System.Threading.CancellationToken ct = default);

        /// <summary>Exports a block to CSV using the provided stream.</summary>
        Task ExportBlockToCsvAsync(string blockName, System.IO.Stream stream,
            char delimiter = ',', System.Threading.CancellationToken ct = default);

        /// <summary>Returns the block data as a <see cref="System.Data.DataTable"/>.</summary>
        System.Data.DataTable GetBlockAsDataTable(string blockName);

        /// <summary>Imports JSON records into a block from a stream.</summary>
        Task<int> ImportBlockFromJsonAsync(string blockName, System.IO.Stream stream,
            bool clearFirst = true, System.Threading.CancellationToken ct = default);

        /// <summary>Imports CSV records into a block from a stream.</summary>
        Task<int> ImportBlockFromCsvAsync(string blockName, System.IO.Stream stream,
            char delimiter = ',', bool clearFirst = true, bool hasHeaderRow = true,
            System.Threading.CancellationToken ct = default);



        /// <summary>Returns grouped view of the block's current data set.</summary>
        IReadOnlyList<ItemGroup<object>> GetBlockGroups(string blockName, string fieldName);



        // Form State Persistence
        /// <summary>Captures the current form and block state snapshot.</summary>
        FormStateSnapshot SaveFormState();

        /// <summary>Restores a previously captured form-state snapshot.</summary>
        Task<bool> RestoreFormStateAsync(FormStateSnapshot snapshot, System.Threading.CancellationToken ct = default);

        // Cross-Block Validation
        /// <summary>Registers a cross-block validation rule.</summary>
        void RegisterCrossBlockRule(CrossBlockValidationRule rule);

        /// <summary>Unregisters a cross-block validation rule by name.</summary>
        bool UnregisterCrossBlockRule(string ruleName);

        /// <summary>Executes all registered cross-block validation rules.</summary>
        IReadOnlyList<string> ValidateCrossBlock();

        // Navigation History
        /// <summary>Navigates to the previous recorded position for a block.</summary>
        Task<bool> NavigateBackAsync(string blockName);

        /// <summary>Navigates to the next recorded position for a block.</summary>
        Task<bool> NavigateForwardAsync(string blockName);

        /// <summary>Returns whether a block can navigate backward through history.</summary>
        bool CanNavigateBack(string blockName);

        /// <summary>Returns whether a block can navigate forward through history.</summary>
        bool CanNavigateForward(string blockName);

        /// <summary>Returns the recorded navigation history for a block.</summary>
        IReadOnlyList<NavigationHistoryEntry> GetNavigationHistory(string blockName);

        /// <summary>Clears the navigation history for a block.</summary>
        void ClearNavigationHistory(string blockName);

        // Block Clone
        /// <summary>Clones data from one block into another block.</summary>
        Task<bool> CloneBlockDataAsync(string sourceBlockName, string destBlockName,
            System.Threading.CancellationToken ct = default);

        /// <summary>Duplicates the current record in a block.</summary>
        Task<bool> DuplicateCurrentRecordAsync(string blockName,
            System.Threading.CancellationToken ct = default);

        /// <summary>Gets a named field or property value from a record.</summary>
        object? GetFieldValue(object record, string FieldName);

        /// <summary>Sets a named field or property value on a record.</summary>
        bool SetFieldValue(object record, string FieldName, object? value);

        // Block Change Feed
        /// <summary>Raised when a tracked field value changes within any registered block.</summary>
        event EventHandler<BlockFieldChangedEventArgs> OnBlockFieldChanged;

        /// <summary>Raised when the manager receives an inter-form message for this form.</summary>
        event EventHandler<FormMessageEventArgs> OnFormMessage;

        // ── Record Groups (G1.2) ───────────────────────────────────────
        void CreateRecordGroup(string name, string dataSourceName, string entityName, List<AppFilter> filters = null);
        Task<bool> PopulateRecordGroupAsync(string name, CancellationToken ct = default);
        RecordGroup GetRecordGroup(string name);
        IReadOnlyList<RecordGroup> GetAllRecordGroups();
        bool RemoveRecordGroup(string name);
        void ClearAllRecordGroups();
        bool RecordGroupExists(string name);

        // ── Parameter Lists (G1.4) ─────────────────────────────────────
        ParameterList CreateParameterList(string name);
        bool DestroyParameterList(string name);
        void AddParameter(string listName, string paramName, object value);
        object GetParameter(string listName, string paramName);
        T GetParameter<T>(string listName, string paramName);
        bool RemoveParameter(string listName, string paramName);
        bool HasParameter(string listName, string paramName);
        ParameterList GetParameterList(string name);
        IReadOnlyList<ParameterList> GetAllParameterLists();
        bool ParameterListExists(string name);
        void ClearParameterList(string listName);

        // ── Client Info (G1.6/G1.7) ────────────────────────────────────
        ClientInfo ClientInfo { get; set; }
        void SetClientInfo(string clientInfo);
        void SetClientModule(string moduleName, string action);
        void SetClientAction(string action);
        void SetClientHost(string hostName);
        void SetClientIpAddress(string ipAddress);
        string GetClientInfo();
        string GetClientModule();
        string GetClientAction();
        string GetClientHost();
        string GetClientIpAddress();

        // ── Application Properties (G2.5) ──────────────────────────────
        void SetApplicationProperty(string name, object value);
        object GetApplicationProperty(string name);
        T GetApplicationProperty<T>(string name);
        bool HasApplicationProperty(string name);
        void RemoveApplicationProperty(string name);

        // ── TEXT_IO / Editor (G2.2) ────────────────────────────────────
        Task<string> ReadTextFileAsync(string path, CancellationToken ct = default);
        Task WriteTextFileAsync(string path, string content, CancellationToken ct = default);
        Task AppendTextFileAsync(string path, string content, CancellationToken ct = default);
        Task<string[]> ReadTextLinesAsync(string path, CancellationToken ct = default);
        string GetMultiLineText(string blockName, string fieldName);
        bool SetMultiLineText(string blockName, string fieldName, string text);

        // ── Bookmarks (G3.1) ───────────────────────────────────────────
        void SetBlockBookmark(string blockName, string bookmarkName);
        bool GoToBlockBookmark(string blockName, string bookmarkName);
        void RemoveBlockBookmark(string blockName, string bookmarkName);
        void ClearBlockBookmarks(string blockName);

        // ── Computed Columns (G3.2) ────────────────────────────────────
        void RegisterBlockComputed(string blockName, string columnName, Func<object, object> computation);
        void UnregisterBlockComputed(string blockName, string columnName);
        object GetBlockComputedValue(string blockName, string columnName);
        IReadOnlyList<string> GetBlockComputedColumnNames(string blockName);
        Dictionary<string, object> GetAllBlockComputedValues(string blockName);

        // ── Freeze / Batch Update (G3.3) ───────────────────────────────
        void FreezeBlock(string blockName);
        void UnfreezeBlock(string blockName);
        void BeginBlockBatchUpdate(string blockName);

        // ── Entity Search / Clone (G3.4) ───────────────────────────────
        Task<object> FindBlockRecordAsync(string blockName, Func<object, bool> predicate, CancellationToken ct = default);
        Task<List<object>> FindBlockRecordsAsync(string blockName, Func<object, bool> predicate, CancellationToken ct = default);
        Task<object> CloneBlockRecordAsync(string blockName, bool deepCopy = false, CancellationToken ct = default);

        // ── Change Log (G3.5) ──────────────────────────────────────────
        IReadOnlyList<object> GetBlockDetailedChangeLog(string blockName);

        // ── Virtual/Lazy Loading (G3.7) ────────────────────────────────
        void EnableBlockVirtualMode(string blockName, long totalRecordCount);
        void DisableBlockVirtualMode(string blockName);
        Task GoToBlockPageAsync(string blockName, int page, CancellationToken ct = default);
        Task PrefetchBlockAdjacentPagesAsync(string blockName, CancellationToken ct = default);

        // ── Source Aggregates (G3.10) ──────────────────────────────────
        Task<double> GetBlockAggregateScalarAsync(string blockName, string aggregateExpression, CancellationToken ct = default);

        // ── Source Transactions (G3.11) ────────────────────────────────
        bool BeginFormTransaction();
        void EndFormTransaction();
        bool CommitFormTransaction();

        // ── Post (validate + send, no commit) ──────────────────────────
        Task<bool> PostBlockAsync(string blockName, CancellationToken ct = default);

        // ── Alerts ─────────────────────────────────────────────────────
        void SetMessage(string text, MessageLevel level = MessageLevel.Info);
        void ClearMessage();
        Task<AlertResult> ShowAlertAsync(string title, string message, AlertStyle style = AlertStyle.None,
            string button1Text = "OK", string button2Text = null, string button3Text = null, CancellationToken ct = default);

        // ── Inter-Form ─────────────────────────────────────────────────
        void SetGlobalVariable(string name, object value);
        object GetGlobalVariable(string name);
        T GetGlobalVariable<T>(string name);
        bool SendParameterToForm(string targetFormName, string paramName, object value);
        void PostMessage(string targetForm, string messageType, object payload = null);
        void BroadcastMessage(string messageType, object payload = null);
        void SubscribeToMessage(string messageType, Action<FormMessage> handler);
        void UnsubscribeFromMessage(string messageType);

        // ── Key Triggers ───────────────────────────────────────────────
        void RegisterKeyTrigger(KeyTriggerType keyType, string blockName, Func<TriggerContext, TriggerResult> handler);
        void RegisterKeyTriggerAsync(KeyTriggerType keyType, string blockName, Func<TriggerContext, CancellationToken, Task<TriggerResult>> asyncHandler);
        Task<TriggerResult> FireKeyTriggerAsync(KeyTriggerType keyType, string blockName);

        // ── Multi-Form Navigation ──────────────────────────────────────
        Task<bool> CallFormAsync(string formName, Dictionary<string, object> parameters = null, FormCallMode callMode = FormCallMode.Modal, CancellationToken ct = default);
        Task<bool> OpenFormModelessAsync(string formName, Dictionary<string, object> parameters = null);
        Task<bool> NewFormAsync(string formName, Dictionary<string, object> parameters = null);
        Task<bool> ReturnToCallerAsync(object returnData = null);
        object GetFormParameter(string paramName);

        // ── Form Trigger Raise ─────────────────────────────────────────
        Task<TriggerResult> RaiseFormTriggerAsync(string triggerName, string blockName = null);

        // ── Block Management ───────────────────────────────────────────
        /// <summary>Removes all master-detail relationships where the given block is master or detail.</summary>
        void ClearBlockRelationships(string blockName);
        /// <summary>Removes all security rules (block- and field-level) for a block.</summary>
        void ClearBlockSecurity(string blockName);

        // ── IDE / Navigator Surface ─────────────────────────────────────
        /// <summary>Blocks not attached to any form (standalone registration).</summary>
        IReadOnlyList<DataBlockInfo> StandaloneBlocks { get; }
        /// <summary>Runtime status snapshot for a block (record count, query mode, etc.).</summary>
        BlockStatus GetBlockStatus(string blockName);
        /// <summary>Register a form discovered by the IDE scanner.</summary>
        void RegisterDiscoveredForm(string formName, string codeFilePath, string designerFilePath, string hostName = null);

    }
}
