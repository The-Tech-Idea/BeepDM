using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>
    /// Core interface for UnitofWorksManager functionality
    /// </summary>
    public interface IUnitofWorksManager : IDisposable
    {
        #region Properties
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

        /// <summary>Gets the latest status message emitted by the manager.</summary>
        string Status { get; }

        /// <summary>Gets the number of currently registered blocks.</summary>
        int BlockCount { get; }
        #endregion

        #region Manager Properties
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
        #endregion

        #region Block Management
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
        Task SynchronizeDetailBlocksAsync(string masterBlockName);

        /// <summary>Returns the detail block names attached to a master block.</summary>
        List<string> GetDetailBlocks(string masterBlockName);

        /// <summary>Returns the master block name for a detail block.</summary>
        string GetMasterBlock(string detailBlockName);
        #endregion

        #region Form Operations
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
        #endregion

        #region Navigation
        /// <summary>Navigates to the first record in a block.</summary>
        Task<bool> FirstRecordAsync(string blockName);

        /// <summary>Navigates to the next record in a block.</summary>
        Task<bool> NextRecordAsync(string blockName);

        /// <summary>Navigates to the previous record in a block.</summary>
        Task<bool> PreviousRecordAsync(string blockName);

        /// <summary>Navigates to the last record in a block.</summary>
        Task<bool> LastRecordAsync(string blockName);
        #endregion

        #region Data Operations
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
        #endregion

        #region Validation
        /// <summary>Validates a single field value in a block.</summary>
        bool ValidateField(string blockName, string FieldName, object value);

        /// <summary>Validates the current record state of a block.</summary>
        bool ValidateBlock(string blockName);

        /// <summary>Validates the current form across its registered blocks.</summary>
        bool ValidateForm();
        #endregion

        #region Undo / Redo

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

        #endregion

        #region Change Summaries

        /// <summary>Returns the current change summary for a block.</summary>
        TheTechIdea.Beep.Editor.UOW.Models.ChangeSummary GetBlockChangeSummary(string blockName);

        /// <summary>Returns one ChangeSummary per registered block.</summary>
        IReadOnlyDictionary<string, TheTechIdea.Beep.Editor.UOW.Models.ChangeSummary> GetFormChangeSummary();

        #endregion

        #region Block Data Operations

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

        #endregion

        #region Query History

        /// <summary>Returns the recorded query history for a block.</summary>
        IReadOnlyList<TheTechIdea.Beep.Editor.UOW.Models.QueryHistoryEntry> GetBlockQueryHistory(string blockName);

        /// <summary>Clears the recorded query history for a block.</summary>
        void ClearBlockQueryHistory(string blockName);

        #endregion

        #region Block Aggregates

        /// <summary>Sum a numeric field across all loaded records in the block.</summary>
        decimal GetBlockSum(string blockName, string fieldName);

        /// <summary>Average of a numeric field.</summary>
        decimal GetBlockAverage(string blockName, string fieldName);

        /// <summary>Count of records matching optional predicate.</summary>
        int GetBlockCount(string blockName, Func<object, bool> predicate = null);

        #endregion

        #region Batch Commit

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

        #endregion

        #region Block Export / Import

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

        #endregion

        #region Block Grouping

        /// <summary>Returns grouped view of the block's current data set.</summary>
        IReadOnlyList<ItemGroup<object>> GetBlockGroups(string blockName, string fieldName);

        #endregion

        #region Phase 4 – Form State, Cross-Block Validation, Navigation History, Clone, Change Feed

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

        /// <summary>Sets a named field or property value on a record.</summary>
        bool SetFieldValue(object record, string FieldName, object value);

        // Block Change Feed
        /// <summary>Raised when a tracked field value changes within any registered block.</summary>
        event EventHandler<BlockFieldChangedEventArgs> OnBlockFieldChanged;

        /// <summary>Raised when the manager receives an inter-form message for this form.</summary>
        event EventHandler<FormMessageEventArgs> OnFormMessage;

        #endregion
    }

    /// <summary>
    /// Deprecated legacy helper for the old standalone relationship helper.
    /// Active master/detail orchestration now lives directly in FormsManager master/detail methods.
    /// </summary>
    [Obsolete("IRelationshipManager is deprecated. Use FormsManager master/detail methods instead.")]
    public interface IRelationshipManager
    {
        /// <summary>Creates a master-detail relationship between two blocks.</summary>
        void CreateMasterDetailRelation(string masterBlockName, string detailBlockName, 
            string masterKeyField, string detailForeignKeyField, RelationshipType relationshipType = RelationshipType.OneToMany);

        /// <summary>Synchronizes the detail blocks attached to a master block.</summary>
        Task SynchronizeDetailBlocksAsync(string masterBlockName);

        /// <summary>Returns the detail block names attached to a master block.</summary>
        List<string> GetDetailBlocks(string masterBlockName);

        /// <summary>Returns the master block name for a detail block.</summary>
        string GetMasterBlock(string detailBlockName);

        /// <summary>Removes all relationships involving a specific block.</summary>
        void RemoveBlockRelationships(string blockName);
    }

    /// <summary>
    /// Interface for dirty state management functionality
    /// </summary>
    public interface IDirtyStateManager
    {
        /// <summary>Checks for unsaved changes affecting a block and handles the caller decision flow.</summary>
        Task<bool> CheckAndHandleUnsavedChangesAsync(string blockName);

        /// <summary>Returns whether any registered block currently has unsaved changes.</summary>
        bool HasUnsavedChanges();

        /// <summary>Returns the names of blocks that currently have unsaved changes.</summary>
        List<string> GetDirtyBlocks();

        /// <summary>Collects dirty detail blocks related to a master block into the supplied list.</summary>
        void CollectDirtyDetailBlocks(string blockName, List<string> dirtyBlocks);

        /// <summary>Saves the specified dirty blocks.</summary>
        Task<bool> SaveDirtyBlocksAsync(List<string> dirtyBlocks);

        /// <summary>Rolls back the specified dirty blocks.</summary>
        Task<bool> RollbackDirtyBlocksAsync(List<string> dirtyBlocks);

        /// <summary>Raised when an operation encounters unsaved changes and requires a user decision.</summary>
        event EventHandler<UnsavedChangesEventArgs> OnUnsavedChanges;
    }

    /// <summary>
    /// Interface for event handling functionality
    /// </summary>
    public interface IEventManager
    {
        /// <summary>Subscribes the manager to unit-of-work events for a block.</summary>
        void SubscribeToUnitOfWorkEvents(IUnitofWork unitOfWork, string blockName);

        /// <summary>Unsubscribes the manager from unit-of-work events for a block.</summary>
        void UnsubscribeFromUnitOfWorkEvents(IUnitofWork unitOfWork, string blockName);

        /// <summary>Raises the block-enter event for a block.</summary>
        void TriggerBlockEnter(string blockName);

        /// <summary>Raises the block-leave event for a block.</summary>
        void TriggerBlockLeave(string blockName);

        /// <summary>Raises the error event for a block.</summary>
        void TriggerError(string blockName, Exception ex);

        /// <summary>Triggers field-level validation for a block field.</summary>
        bool TriggerFieldValidation(string blockName, string FieldName, object value);

        /// <summary>Triggers record-level validation for a block record.</summary>
        bool TriggerRecordValidation(string blockName, object record);
    }

    /// <summary>
    /// Interface for Oracle Forms simulation helpers
    /// </summary>
    public interface IFormsSimulationHelper
    {
        /// <summary>Sets common audit-field defaults on a record.</summary>
        void SetAuditDefaults(object record, string currentUser = null);

        /// <summary>Sets a named field or property value on a record.</summary>
        bool SetFieldValue(object record, string FieldName, object value);

        /// <summary>Gets a named field or property value from a record.</summary>
        object GetFieldValue(object record, string FieldName);

        /// <summary>Executes sequence logic for a block field.</summary>
        bool ExecuteSequence(string blockName, object record, string FieldName, string sequenceName);

        /// <summary>Gets a named property value from an object.</summary>
        object GetPropertyValue(object obj, string propertyName);
    }

    /// <summary>
    /// Interface for performance and caching functionality
    /// </summary>
    public interface IPerformanceManager : IDisposable
    {
        /// <summary>Runs cache and block-access optimization routines.</summary>
        void OptimizeBlockAccess();

        /// <summary>Caches block metadata for faster later access.</summary>
        void CacheBlockInfo(string blockName, DataBlockInfo blockInfo);

        /// <summary>Returns cached block metadata for a block when present.</summary>
        DataBlockInfo GetCachedBlockInfo(string blockName);

        /// <summary>Clears all cached block metadata.</summary>
        void ClearCache();

        /// <summary>Returns aggregate performance statistics for the manager.</summary>
        PerformanceStatistics GetPerformanceStatistics();

        // Phase 7 — cache improvements
        /// <summary>Removes a single block's entry from the cache (invalidation).</summary>
        void InvalidateBlockCache(string blockName);

        /// <summary>Sets a per-block cache TTL, overriding the global cache expiration time.</summary>
        void SetBlockCacheTtl(string blockName, TimeSpan ttl);

        /// <summary>Returns a lightweight cache hit/miss/eviction snapshot.</summary>
        CacheStats GetCacheStats();

        /// <summary>
        /// Checks estimated managed memory against <paramref name="thresholdBytes"/>;
        /// if exceeded, evicts the least-recently-used half of the cache.
        /// </summary>
        void CheckMemoryPressure(long thresholdBytes = 256 * 1024 * 1024);
    }

    /// <summary>
    /// Interface for configuration management
    /// </summary>
    public interface IConfigurationManager
    {
        /// <summary>Gets or sets the active FormsManager configuration instance.</summary>
        UnitofWorksManagerConfiguration Configuration { get; set; }

        /// <summary>Loads configuration from its backing store.</summary>
        void LoadConfiguration();

        /// <summary>Saves configuration to its backing store.</summary>
        void SaveConfiguration();

        /// <summary>Resets configuration back to defaults.</summary>
        void ResetToDefaults();

        /// <summary>Returns whether the active configuration is structurally valid.</summary>
        bool ValidateConfiguration();
    }

    #region System Variables Manager Interface

    /// <summary>
    /// Interface for managing Oracle Forms :SYSTEM.* equivalent variables
    /// Provides access to block, record, item, and form state information
    /// </summary>
    public interface ISystemVariablesManager
    {
        #region System Variables Access
        
        /// <summary>
        /// Get system variables for a specific block
        /// </summary>
        SystemVariables GetSystemVariables(string blockName);
        
        /// <summary>
        /// Get form-level system variables
        /// </summary>
        SystemVariables GetFormSystemVariables();
        
        #endregion
        
        #region Block-Level Updates
        
        /// <summary>
        /// Update system variables when block changes
        /// </summary>
        void UpdateForBlockChange(string blockName);
        
        /// <summary>
        /// Update system variables when record changes within a block
        /// </summary>
        void UpdateForRecordChange(string blockName, int recordIndex, int totalRecords);
        
        /// <summary>
        /// Update system variables when item focus changes
        /// </summary>
        void UpdateForItemChange(string blockName, string itemName, object itemValue);
        
        #endregion
        
        #region Mode Updates
        
        /// <summary>
        /// Update MODE system variable
        /// </summary>
        void SetMode(string mode);
        
        /// <summary>
        /// Update BLOCK_STATUS system variable
        /// </summary>
        void SetBlockStatus(string blockName, string status);
        
        /// <summary>
        /// Update FORM_STATUS system variable
        /// </summary>
        void SetFormStatus(string status);
        
        /// <summary>
        /// Update RECORD_STATUS system variable
        /// </summary>
        void SetRecordStatus(string blockName, string status);
        
        #endregion
        
        #region Trigger Context Updates
        
        /// <summary>
        /// Set trigger context before trigger execution
        /// </summary>
        void SetTriggerContext(string triggerType, string blockName, string itemName = null, int recordIndex = 0);
        
        /// <summary>
        /// Clear trigger context after trigger execution
        /// </summary>
        void ClearTriggerContext();
        
        #endregion
        
        #region Error Handling
        
        /// <summary>
        /// Set last error information
        /// </summary>
        void SetLastError(string errorMessage, int errorCode = 0);
        
        /// <summary>
        /// Clear last error
        /// </summary>
        void ClearLastError();
        
        #endregion
        
        #region Query Information
        
        /// <summary>
        /// Set last query string
        /// </summary>
        void SetLastQuery(string queryString);
        
        #endregion
        
        #region Form Context
        
        /// <summary>
        /// Set current form name
        /// </summary>
        void SetCurrentForm(string formName);
        
        /// <summary>
        /// Reset all system variables
        /// </summary>
        void Reset();
        
        #endregion

        #region Per-Block Snapshot (Phase 8)

        /// <summary>
        /// Update per-block variables with a full block snapshot captured by FormsManager.
        /// Stores the snapshot so BeepDataBlock can read it back via GetBlockVariables.
        /// </summary>
        void UpdateBlockVariables(
            string blockName,
            string masterBlockName,
            string mode,
            int cursorRecord,
            int lastRecord,
            int recordsDisplayed,
            bool isQueryMode,
            bool isDirty,
            string triggerItem = null,
            TriggerType? activeTrigger = null);

        /// <summary>
        /// Retrieve the per-block variable snapshot last written by UpdateBlockVariables.
        /// Returns a fresh empty instance when no snapshot exists for the block.
        /// </summary>
        SystemVariables GetBlockVariables(string blockName);

        #endregion
    }
    
    #endregion
    
    #region Validation Manager Interface
    
    /// <summary>
    /// Manages validation rules and validation execution for Oracle Forms-style validation.
    /// Supports WHEN-VALIDATE-ITEM, WHEN-VALIDATE-RECORD, and PRE-/POST- trigger validation.
    /// </summary>
    public interface IValidationManager
    {
        #region Rule Registration
        
        /// <summary>
        /// Register a validation rule for a specific block/item
        /// </summary>
        /// <param name="rule">The validation rule to register</param>
        void RegisterRule(ValidationRule rule);
        
        /// <summary>
        /// Register multiple validation rules at once
        /// </summary>
        /// <param name="rules">Collection of validation rules to register</param>
        void RegisterRules(IEnumerable<ValidationRule> rules);

        /// <summary>
        /// Start a fluent validation rule builder for a specific block+field.
        /// Usage: manager.ForField("Block", "Field").Required().MinLength(3).Register();
        /// </summary>
        ValidationRuleBuilder ForField(string blockName, string fieldName);
        
        /// <summary>
        /// Unregister a validation rule by name
        /// </summary>
        /// <param name="ruleName">Name of the rule to unregister</param>
        /// <returns>True if rule was found and removed</returns>
        bool UnregisterRule(string ruleName);
        
        /// <summary>
        /// Unregister all rules for a specific block
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        void UnregisterBlockRules(string blockName);
        
        /// <summary>
        /// Unregister all rules for a specific item in a block
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="itemName">Name of the item</param>
        void UnregisterItemRules(string blockName, string itemName);
        
        /// <summary>
        /// Clear all registered rules
        /// </summary>
        void ClearAllRules();
        
        #endregion
        
        #region Rule Retrieval
        
        /// <summary>
        /// Get a specific validation rule by name
        /// </summary>
        /// <param name="ruleName">Name of the rule</param>
        /// <returns>The validation rule or null if not found</returns>
        ValidationRule GetRule(string ruleName);
        
        /// <summary>
        /// Get all rules for a specific item
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="itemName">Name of the item</param>
        /// <returns>Collection of validation rules for the item</returns>
        IReadOnlyList<ValidationRule> GetRulesForItem(string blockName, string itemName);
        
        /// <summary>
        /// Get all rules for a specific block
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <returns>Collection of validation rules for the block</returns>
        IReadOnlyList<ValidationRule> GetRulesForBlock(string blockName);
        
        /// <summary>
        /// Get all registered rules
        /// </summary>
        /// <returns>Collection of all validation rules</returns>
        IReadOnlyList<ValidationRule> GetAllRules();
        
        /// <summary>
        /// Get rules filtered by timing
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="itemName">Name of the item (optional)</param>
        /// <param name="timing">Validation timing to filter by</param>
        /// <returns>Collection of matching validation rules</returns>
        IReadOnlyList<ValidationRule> GetRulesByTiming(string blockName, string itemName, ValidationTiming timing);
        
        #endregion
        
        #region Synchronous Validation
        
        /// <summary>
        /// Validate a single field/item value
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="itemName">Name of the item</param>
        /// <param name="value">Value to validate</param>
        /// <param name="timing">Validation timing context (default: Manual)</param>
        /// <returns>Validation result for the item</returns>
        ItemValidationResult ValidateItem(string blockName, string itemName, object value, ValidationTiming timing = ValidationTiming.Manual);
        
        /// <summary>
        /// Validate a single record
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="record">Record data as dictionary</param>
        /// <param name="timing">Validation timing context</param>
        /// <returns>Validation result for the record</returns>
        RecordValidationResult ValidateRecord(string blockName, IDictionary<string, object> record, ValidationTiming timing = ValidationTiming.Manual);
        
        /// <summary>
        /// Validate all records in a block
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="records">Collection of records</param>
        /// <param name="timing">Validation timing context</param>
        /// <returns>Validation result for the block</returns>
        BlockValidationResult ValidateBlock(string blockName, IEnumerable<IDictionary<string, object>> records, ValidationTiming timing = ValidationTiming.Manual);
        
        /// <summary>
        /// Validate entire form
        /// </summary>
        /// <param name="formData">Form data as dictionary of block name to records</param>
        /// <param name="timing">Validation timing context</param>
        /// <returns>Validation result for the form</returns>
        FormValidationResult ValidateForm(IDictionary<string, IEnumerable<IDictionary<string, object>>> formData, ValidationTiming timing = ValidationTiming.Manual);
        
        #endregion
        
        #region Asynchronous Validation
        
        /// <summary>
        /// Validate a single field/item value asynchronously
        /// </summary>
        Task<ItemValidationResult> ValidateItemAsync(string blockName, string itemName, object value, ValidationTiming timing = ValidationTiming.Manual, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Validate a single record asynchronously
        /// </summary>
        Task<RecordValidationResult> ValidateRecordAsync(string blockName, IDictionary<string, object> record, ValidationTiming timing = ValidationTiming.Manual, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Validate all records in a block asynchronously
        /// </summary>
        Task<BlockValidationResult> ValidateBlockAsync(string blockName, IEnumerable<IDictionary<string, object>> records, ValidationTiming timing = ValidationTiming.Manual, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Validate entire form asynchronously
        /// </summary>
        Task<FormValidationResult> ValidateFormAsync(IDictionary<string, IEnumerable<IDictionary<string, object>>> formData, ValidationTiming timing = ValidationTiming.Manual, CancellationToken cancellationToken = default);
        
        #endregion
        
        #region Validation Context
        
        /// <summary>
        /// Set the data source for lookup validations
        /// </summary>
        /// <param name="dataSource">Data source for lookup queries</param>
        void SetDataSource(IDataSource dataSource);
        
        /// <summary>
        /// Enable or disable all validation
        /// </summary>
        bool IsValidationEnabled { get; set; }
        
        /// <summary>
        /// Enable or disable a specific rule
        /// </summary>
        /// <param name="ruleName">Name of the rule</param>
        /// <param name="enabled">True to enable, false to disable</param>
        void SetRuleEnabled(string ruleName, bool enabled);
        
        /// <summary>
        /// Enable or disable all rules for a block
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="enabled">True to enable, false to disable</param>
        void SetBlockValidationEnabled(string blockName, bool enabled);
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Event raised when validation fails
        /// </summary>
        event EventHandler<ValidationFailedEventArgs> ValidationFailed;
        
        /// <summary>
        /// Event raised before validation starts
        /// </summary>
        event EventHandler<ValidationStartingEventArgs> ValidationStarting;
        
        /// <summary>
        /// Event raised after validation completes
        /// </summary>
        event EventHandler<ValidationCompletedEventArgs> ValidationCompleted;
        
        #endregion
    }
    
    #region Validation Event Args
    
    /// <summary>
    /// Event arguments for validation failed event
    /// </summary>
    public class ValidationFailedEventArgs : EventArgs
    {
        /// <summary>Gets or sets the block being validated.</summary>
        public string BlockName { get; set; }

        /// <summary>Gets or sets the field being validated.</summary>
        public string ItemName { get; set; }

        /// <summary>Gets or sets the value that failed validation.</summary>
        public object Value { get; set; }

        /// <summary>Gets or sets the validation rule that failed.</summary>
        public ValidationRule FailedRule { get; set; }

        /// <summary>Gets or sets the validation result payload.</summary>
        public ValidationRuleResult Result { get; set; }

        /// <summary>Gets or sets whether downstream processing should be cancelled.</summary>
        public bool Cancel { get; set; }
    }
    
    /// <summary>
    /// Event arguments for validation starting event
    /// </summary>
    public class ValidationStartingEventArgs : EventArgs
    {
        /// <summary>Gets or sets the block being validated.</summary>
        public string BlockName { get; set; }

        /// <summary>Gets or sets the field being validated.</summary>
        public string ItemName { get; set; }

        /// <summary>Gets or sets the value about to be validated.</summary>
        public object Value { get; set; }

        /// <summary>Gets or sets the rules scheduled to run.</summary>
        public IReadOnlyList<ValidationRule> Rules { get; set; }

        /// <summary>Gets or sets whether validation should be cancelled.</summary>
        public bool Cancel { get; set; }
    }
    
    /// <summary>
    /// Event arguments for validation completed event
    /// </summary>
    public class ValidationCompletedEventArgs : EventArgs
    {
        /// <summary>Gets or sets the block that was validated.</summary>
        public string BlockName { get; set; }

        /// <summary>Gets or sets the field that was validated.</summary>
        public string ItemName { get; set; }

        /// <summary>Gets or sets whether validation completed successfully.</summary>
        public bool IsValid { get; set; }

        /// <summary>Gets or sets the number of rules that were evaluated.</summary>
        public int RulesEvaluated { get; set; }

        /// <summary>Gets or sets the number of rules that failed.</summary>
        public int RulesFailed { get; set; }

        /// <summary>Gets or sets the total validation duration.</summary>
        public TimeSpan Duration { get; set; }
    }
    
    #endregion
    
    #endregion
    
    #region LOV Manager Interface
    
    /// <summary>
    /// Manages List of Values (LOV) for blocks - Oracle Forms compatible LOV engine.
    /// Handles LOV registration, data loading, caching, and validation.
    /// </summary>
    public interface ILOVManager
    {
        #region LOV Registration
        
        /// <summary>
        /// Register a LOV for a specific block/field
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="lov">LOV definition</param>
        void RegisterLOV(string blockName, string fieldName, LOVDefinition lov);
        
        /// <summary>
        /// Unregister a LOV
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="fieldName">Name of the field</param>
        void UnregisterLOV(string blockName, string fieldName);
        
        /// <summary>
        /// Check if a field has a LOV attached
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="fieldName">Name of the field</param>
        /// <returns>True if LOV exists</returns>
        bool HasLOV(string blockName, string fieldName);
        
        /// <summary>
        /// Get LOV definition for a field
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="fieldName">Name of the field</param>
        /// <returns>LOV definition or null if not found</returns>
        LOVDefinition GetLOV(string blockName, string fieldName);
        
        /// <summary>
        /// Get all LOVs for a block
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <returns>Dictionary of field name to LOV definition</returns>
        Dictionary<string, LOVDefinition> GetBlockLOVs(string blockName);
        
        /// <summary>
        /// Get all registered LOV names
        /// </summary>
        /// <returns>Collection of LOV keys (blockName:fieldName)</returns>
        IReadOnlyList<string> GetAllLOVKeys();
        
        #endregion
        
        #region LOV Data Operations
        
        /// <summary>
        /// Load LOV data from data source
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="searchText">Optional search text to filter results</param>
        /// <returns>LOV result with loaded records</returns>
        Task<LOVResult> LoadLOVDataAsync(string blockName, string fieldName, string searchText = null);
        
        /// <summary>
        /// Load LOV data using specific LOV definition
        /// </summary>
        /// <param name="lov">LOV definition</param>
        /// <param name="searchText">Optional search text to filter results</param>
        /// <returns>LOV result with loaded records</returns>
        Task<LOVResult> LoadLOVDataAsync(LOVDefinition lov, string searchText = null);
        
        /// <summary>
        /// Filter cached LOV data (client-side filtering)
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="searchText">Search text to filter by</param>
        /// <returns>Filtered LOV result</returns>
        LOVResult FilterLOVData(string blockName, string fieldName, string searchText);
        
        /// <summary>
        /// Validate a value against LOV
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="value">Value to validate</param>
        /// <returns>Validation result</returns>
        Task<LOVValidationResult> ValidateLOVValueAsync(string blockName, string fieldName, object value);
        
        /// <summary>
        /// Get related field values for a selected record
        /// </summary>
        /// <param name="lov">LOV definition</param>
        /// <param name="selectedRecord">Selected record from LOV</param>
        /// <returns>Dictionary of field name to value</returns>
        Dictionary<string, object> GetRelatedFieldValues(LOVDefinition lov, object selectedRecord);
        
        /// <summary>
        /// Find matching record by return field value
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="value">Return field value</param>
        /// <returns>Matching record or null</returns>
        Task<object> FindRecordByValueAsync(string blockName, string fieldName, object value);
        
        #endregion
        
        #region Cache Management
        
        /// <summary>
        /// Clear LOV cache for a specific field
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="fieldName">Name of the field</param>
        void ClearLOVCache(string blockName, string fieldName);
        
        /// <summary>
        /// Clear all LOV caches for a block
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        void ClearBlockLOVCache(string blockName);
        
        /// <summary>
        /// Clear all LOV caches
        /// </summary>
        void ClearAllLOVCaches();
        
        /// <summary>
        /// Refresh LOV cache for a field
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="fieldName">Name of the field</param>
        Task RefreshLOVCacheAsync(string blockName, string fieldName);
        
        /// <summary>
        /// Pre-load LOV data into cache
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        /// <param name="fieldName">Name of the field</param>
        Task PreloadLOVAsync(string blockName, string fieldName);
        
        /// <summary>
        /// Pre-load all LOVs for a block into cache
        /// </summary>
        /// <param name="blockName">Name of the block</param>
        Task PreloadBlockLOVsAsync(string blockName);
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Event raised when LOV data is loaded
        /// </summary>
        event EventHandler<LOVDataLoadedEventArgs> LOVDataLoaded;
        
        /// <summary>
        /// Event raised when LOV validation fails
        /// </summary>
        event EventHandler<LOVValidationEventArgs> LOVValidationFailed;
        
        #endregion
    }
    
    #region LOV Event Args
    
    /// <summary>
    /// Event arguments for LOV data loaded event
    /// </summary>
    public class LOVDataLoadedEventArgs : EventArgs
    {
        /// <summary>Gets or sets the block whose LOV data was loaded.</summary>
        public string BlockName { get; set; }

        /// <summary>Gets or sets the field whose LOV data was loaded.</summary>
        public string FieldName { get; set; }

        /// <summary>Gets or sets the LOV definition used for the load.</summary>
        public LOVDefinition LOV { get; set; }

        /// <summary>Gets or sets the number of LOV records returned.</summary>
        public int RecordCount { get; set; }

        /// <summary>Gets or sets whether the load was satisfied from cache.</summary>
        public bool FromCache { get; set; }

        /// <summary>Gets or sets the LOV load time in milliseconds.</summary>
        public long LoadTimeMs { get; set; }
    }
    
    /// <summary>
    /// Event arguments for LOV validation event
    /// </summary>
    public class LOVValidationEventArgs : EventArgs
    {
        /// <summary>Gets or sets the block whose LOV value was validated.</summary>
        public string BlockName { get; set; }

        /// <summary>Gets or sets the field whose LOV value was validated.</summary>
        public string FieldName { get; set; }

        /// <summary>Gets or sets the value being validated.</summary>
        public object Value { get; set; }

        /// <summary>Gets or sets the validation error message when validation fails.</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Gets or sets suggested fallback values for invalid LOV input.</summary>
        public List<object> Suggestions { get; set; }
    }
    
    #endregion
    
    #endregion
    
    #region Item Property Manager Interface
    
    /// <summary>
    /// Interface for managing item/field properties in data blocks.
    /// Oracle Forms equivalent: SET_ITEM_PROPERTY, GET_ITEM_PROPERTY built-ins.
    /// </summary>
    public interface IItemPropertyManager
    {
        #region Item Registration
        
        /// <summary>
        /// Register an item with the property manager
        /// </summary>
        /// <param name="blockName">Block name</param>
        /// <param name="itemName">Item name</param>
        /// <param name="info">Item info definition</param>
        void RegisterItem(string blockName, string itemName, ItemInfo info);
        
        /// <summary>
        /// Register items from an entity structure
        /// </summary>
        /// <param name="blockName">Block name</param>
        /// <param name="structure">Entity structure</param>
        void RegisterItemsFromEntityStructure(string blockName, IEntityStructure structure);
        
        /// <summary>
        /// Unregister an item
        /// </summary>
        /// <param name="blockName">Block name</param>
        /// <param name="itemName">Item name</param>
        void UnregisterItem(string blockName, string itemName);
        
        /// <summary>
        /// Clear all items for a block
        /// </summary>
        /// <param name="blockName">Block name</param>
        void ClearBlockItems(string blockName);
        
        #endregion
        
        #region Item Retrieval
        
        /// <summary>
        /// Get item info
        /// </summary>
        ItemInfo GetItem(string blockName, string itemName);
        
        /// <summary>
        /// Get all items for a block
        /// </summary>
        IReadOnlyList<ItemInfo> GetAllItems(string blockName);
        
        /// <summary>
        /// Check if an item exists
        /// </summary>
        bool ItemExists(string blockName, string itemName);
        
        /// <summary>
        /// Get item count for a block
        /// </summary>
        int GetItemCount(string blockName);
        
        #endregion
        
        #region SET_ITEM_PROPERTY Built-ins
        
        /// <summary>
        /// Set item property by name (Oracle Forms: SET_ITEM_PROPERTY)
        /// </summary>
        void SetItemProperty(string blockName, string itemName, string propertyName, object value);
        
        /// <summary>Set item enabled state</summary>
        void SetItemEnabled(string blockName, string itemName, bool enabled);
        
        /// <summary>Set item visible state</summary>
        void SetItemVisible(string blockName, string itemName, bool visible);
        
        /// <summary>Set item required state</summary>
        void SetItemRequired(string blockName, string itemName, bool required);
        
        /// <summary>Set item query allowed</summary>
        void SetItemQueryAllowed(string blockName, string itemName, bool allowed);
        
        /// <summary>Set item insert allowed</summary>
        void SetItemInsertAllowed(string blockName, string itemName, bool allowed);
        
        /// <summary>Set item update allowed</summary>
        void SetItemUpdateAllowed(string blockName, string itemName, bool allowed);
        
        /// <summary>Set item default value</summary>
        void SetItemDefaultValue(string blockName, string itemName, object value);
        
        /// <summary>Set item LOV name</summary>
        void SetItemLOV(string blockName, string itemName, string lovName);
        
        /// <summary>Set item format mask</summary>
        void SetItemFormatMask(string blockName, string itemName, string formatMask);
        
        /// <summary>Set item prompt/label text</summary>
        void SetItemPromptText(string blockName, string itemName, string text);
        
        /// <summary>Set item hint/tooltip text</summary>
        void SetItemHintText(string blockName, string itemName, string text);
        
        #endregion
        
        #region GET_ITEM_PROPERTY Built-ins
        
        /// <summary>
        /// Get item property by name (Oracle Forms: GET_ITEM_PROPERTY)
        /// </summary>
        object GetItemProperty(string blockName, string itemName, string propertyName);
        
        /// <summary>Check if item is enabled</summary>
        bool IsItemEnabled(string blockName, string itemName);
        
        /// <summary>Check if item is visible</summary>
        bool IsItemVisible(string blockName, string itemName);
        
        /// <summary>Check if item is required</summary>
        bool IsItemRequired(string blockName, string itemName);
        
        /// <summary>Check if item allows query</summary>
        bool IsItemQueryAllowed(string blockName, string itemName);
        
        /// <summary>Check if item allows insert</summary>
        bool IsItemInsertAllowed(string blockName, string itemName);
        
        /// <summary>Check if item allows update</summary>
        bool IsItemUpdateAllowed(string blockName, string itemName);
        
        /// <summary>Get item default value</summary>
        object GetItemDefaultValue(string blockName, string itemName);
        
        /// <summary>Get item LOV name</summary>
        string GetItemLOV(string blockName, string itemName);
        
        /// <summary>Get item format mask</summary>
        string GetItemFormatMask(string blockName, string itemName);
        
        #endregion
        
        #region Value Management
        
        /// <summary>Set item current value</summary>
        void SetItemValue(string blockName, string itemName, object value);
        
        /// <summary>Get item current value</summary>
        object GetItemValue(string blockName, string itemName);
        
        /// <summary>Apply default values to a record</summary>
        void ApplyDefaultValues(string blockName, object record);
        
        /// <summary>Clear all item values for a block</summary>
        void ClearItemValues(string blockName);
        
        /// <summary>Get all item values as dictionary</summary>
        Dictionary<string, object> GetAllItemValues(string blockName);
        
        /// <summary>Set all item values from dictionary</summary>
        void SetAllItemValues(string blockName, IDictionary<string, object> values);
        
        #endregion
        
        #region State Management
        
        /// <summary>Mark item as dirty (value changed)</summary>
        void MarkItemDirty(string blockName, string itemName, object oldValue);
        
        /// <summary>Clear item dirty flag</summary>
        void ClearItemDirty(string blockName, string itemName);
        
        /// <summary>Check if item is dirty</summary>
        bool IsItemDirty(string blockName, string itemName);
        
        /// <summary>Get all dirty items for a block</summary>
        IReadOnlyList<string> GetDirtyItems(string blockName);
        
        /// <summary>Clear all dirty flags for a block</summary>
        void ClearAllDirtyFlags(string blockName);
        
        #endregion
        
        #region Error State
        
        /// <summary>Set item error</summary>
        void SetItemError(string blockName, string itemName, string errorMessage);
        
        /// <summary>Clear item error</summary>
        void ClearItemError(string blockName, string itemName);
        
        /// <summary>Clear all item errors for a block</summary>
        void ClearAllItemErrors(string blockName);
        
        /// <summary>Check if item has error</summary>
        bool HasItemError(string blockName, string itemName);
        
        /// <summary>Get item error message</summary>
        string GetItemErrorMessage(string blockName, string itemName);
        
        /// <summary>Get all items with errors</summary>
        IReadOnlyList<ItemInfo> GetItemsWithErrors(string blockName);
        
        #endregion
        
        #region Navigation Order
        
        /// <summary>Set tab order for items</summary>
        void SetTabOrder(string blockName, IEnumerable<string> itemOrder);
        
        /// <summary>Get tab order</summary>
        IReadOnlyList<string> GetTabOrder(string blockName);
        
        /// <summary>Get next item in navigation order</summary>
        string GetNextItem(string blockName, string currentItem);
        
        /// <summary>Get previous item in navigation order</summary>
        string GetPreviousItem(string blockName, string currentItem);
        
        /// <summary>Get first item in navigation order</summary>
        string GetFirstItem(string blockName);
        
        /// <summary>Get last item in navigation order</summary>
        string GetLastItem(string blockName);
        
        #endregion
        
        #region Form Mode Support
        
        /// <summary>Get items editable in specified mode</summary>
        IReadOnlyList<ItemInfo> GetEditableItems(string blockName, FormMode mode);
        
        /// <summary>Check if item is editable in specified mode</summary>
        bool IsItemEditable(string blockName, string itemName, FormMode mode);
        
        #endregion
        
        #region Events
        
        /// <summary>Event raised when item property changes</summary>
        event EventHandler<ItemPropertyChangedEventArgs> ItemPropertyChanged;
        
        /// <summary>Event raised when item value changes</summary>
        event EventHandler<ItemValueChangedEventArgs> ItemValueChanged;
        
        /// <summary>Event raised when item error state changes</summary>
        event EventHandler<ItemErrorEventArgs> ItemErrorChanged;
        
        #endregion
    }
    
    #endregion
    
    #region Trigger Manager Interface
    
    /// <summary>
    /// Interface for managing triggers (Oracle Forms trigger equivalents).
    /// Provides registration, execution, and lifecycle management for all trigger types.
    /// </summary>
    public interface ITriggerManager : IDisposable
    {
        #region Trigger Registration
        
        /// <summary>
        /// Register a form-level trigger
        /// </summary>
        void RegisterFormTrigger(TriggerType type, string formName, Func<TriggerContext, TriggerResult> handler, TriggerPriority priority = TriggerPriority.Normal);
        
        /// <summary>
        /// Register a form-level async trigger
        /// </summary>
        void RegisterFormTriggerAsync(TriggerType type, string formName, Func<TriggerContext, CancellationToken, Task<TriggerResult>> handler, TriggerPriority priority = TriggerPriority.Normal);
        
        /// <summary>
        /// Register a block-level trigger
        /// </summary>
        void RegisterBlockTrigger(TriggerType type, string blockName, Func<TriggerContext, TriggerResult> handler, TriggerPriority priority = TriggerPriority.Normal);
        
        /// <summary>
        /// Register a block-level async trigger
        /// </summary>
        void RegisterBlockTriggerAsync(TriggerType type, string blockName, Func<TriggerContext, CancellationToken, Task<TriggerResult>> handler, TriggerPriority priority = TriggerPriority.Normal);
        
        /// <summary>
        /// Register an item-level trigger
        /// </summary>
        void RegisterItemTrigger(TriggerType type, string blockName, string itemName, Func<TriggerContext, TriggerResult> handler, TriggerPriority priority = TriggerPriority.Normal);
        
        /// <summary>
        /// Register an item-level async trigger
        /// </summary>
        void RegisterItemTriggerAsync(TriggerType type, string blockName, string itemName, Func<TriggerContext, CancellationToken, Task<TriggerResult>> handler, TriggerPriority priority = TriggerPriority.Normal);
        
        /// <summary>
        /// Register a global trigger (applies to all forms)
        /// </summary>
        void RegisterGlobalTrigger(TriggerType type, Func<TriggerContext, TriggerResult> handler, TriggerPriority priority = TriggerPriority.Normal);
        
        /// <summary>
        /// Register a trigger definition directly
        /// </summary>
        void RegisterTrigger(TriggerDefinition trigger);
        
        #endregion
        
        #region Trigger Unregistration
        
        /// <summary>
        /// Unregister a specific trigger by ID
        /// </summary>
        bool UnregisterTrigger(string triggerId);
        
        /// <summary>
        /// Unregister all triggers of a type for a block
        /// </summary>
        int UnregisterBlockTriggers(TriggerType type, string blockName);
        
        /// <summary>
        /// Unregister all triggers of a type for an item
        /// </summary>
        int UnregisterItemTriggers(TriggerType type, string blockName, string itemName);
        
        /// <summary>
        /// Unregister all triggers for a block
        /// </summary>
        int ClearBlockTriggers(string blockName);
        
        /// <summary>
        /// Unregister all triggers for an item
        /// </summary>
        int ClearItemTriggers(string blockName, string itemName);
        
        /// <summary>
        /// Unregister all triggers
        /// </summary>
        void ClearAllTriggers();
        
        #endregion
        
        #region Trigger Execution
        
        /// <summary>
        /// Fire a form-level trigger
        /// </summary>
        TriggerResult FireFormTrigger(TriggerType type, string formName, TriggerContext context = null);
        
        /// <summary>
        /// Fire a form-level trigger asynchronously
        /// </summary>
        Task<TriggerResult> FireFormTriggerAsync(TriggerType type, string formName, TriggerContext context = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Fire a block-level trigger
        /// </summary>
        TriggerResult FireBlockTrigger(TriggerType type, string blockName, TriggerContext context = null);
        
        /// <summary>
        /// Fire a block-level trigger asynchronously
        /// </summary>
        Task<TriggerResult> FireBlockTriggerAsync(TriggerType type, string blockName, TriggerContext context = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Fire an item-level trigger
        /// </summary>
        TriggerResult FireItemTrigger(TriggerType type, string blockName, string itemName, TriggerContext context = null);
        
        /// <summary>
        /// Fire an item-level trigger asynchronously
        /// </summary>
        Task<TriggerResult> FireItemTriggerAsync(TriggerType type, string blockName, string itemName, TriggerContext context = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Fire a global trigger
        /// </summary>
        TriggerResult FireGlobalTrigger(TriggerType type, TriggerContext context = null);
        
        /// <summary>
        /// Fire a global trigger asynchronously
        /// </summary>
        Task<TriggerResult> FireGlobalTriggerAsync(TriggerType type, TriggerContext context = null, CancellationToken cancellationToken = default);
        
        #endregion
        
        #region Trigger Query
        
        /// <summary>
        /// Get a trigger by ID
        /// </summary>
        TriggerDefinition GetTrigger(string triggerId);
        
        /// <summary>
        /// Get all triggers for a block
        /// </summary>
        IReadOnlyList<TriggerDefinition> GetBlockTriggers(string blockName);
        
        /// <summary>
        /// Get all triggers of a specific type for a block
        /// </summary>
        IReadOnlyList<TriggerDefinition> GetBlockTriggers(TriggerType type, string blockName);
        
        /// <summary>
        /// Get all triggers for an item
        /// </summary>
        IReadOnlyList<TriggerDefinition> GetItemTriggers(string blockName, string itemName);
        
        /// <summary>
        /// Get all form-level triggers
        /// </summary>
        IReadOnlyList<TriggerDefinition> GetFormTriggers(string formName);
        
        /// <summary>
        /// Get all global triggers
        /// </summary>
        IReadOnlyList<TriggerDefinition> GetGlobalTriggers();
        
        /// <summary>
        /// Get all triggers by category
        /// </summary>
        IReadOnlyList<TriggerDefinition> GetTriggersByCategory(TriggerCategory category);
        
        /// <summary>
        /// Check if a trigger exists for a block
        /// </summary>
        bool HasBlockTrigger(TriggerType type, string blockName);
        
        /// <summary>
        /// Check if a trigger exists for an item
        /// </summary>
        bool HasItemTrigger(TriggerType type, string blockName, string itemName);
        
        /// <summary>
        /// Get total trigger count
        /// </summary>
        int TriggerCount { get; }
        
        #endregion
        
        #region Trigger Enable/Disable
        
        /// <summary>
        /// Enable a trigger by ID
        /// </summary>
        void EnableTrigger(string triggerId);
        
        /// <summary>
        /// Disable a trigger by ID
        /// </summary>
        void DisableTrigger(string triggerId);
        
        /// <summary>
        /// Enable all triggers of a type for a block
        /// </summary>
        void EnableBlockTriggers(TriggerType type, string blockName);
        
        /// <summary>
        /// Disable all triggers of a type for a block
        /// </summary>
        void DisableBlockTriggers(TriggerType type, string blockName);
        
        /// <summary>
        /// Enable all triggers
        /// </summary>
        void EnableAllTriggers();
        
        /// <summary>
        /// Disable all triggers
        /// </summary>
        void DisableAllTriggers();
        
        /// <summary>
        /// Check if trigger execution is globally suspended
        /// </summary>
        bool IsSuspended { get; }
        
        /// <summary>
        /// Suspend all trigger execution temporarily
        /// </summary>
        void SuspendTriggers();
        
        /// <summary>
        /// Resume trigger execution
        /// </summary>
        void ResumeTriggers();
        
        #endregion
        
        #region Events
        
        /// <summary>Event raised before a trigger executes</summary>
        event EventHandler<TriggerExecutingEventArgs> TriggerExecuting;
        
        /// <summary>Event raised after a trigger executes</summary>
        event EventHandler<TriggerExecutedEventArgs> TriggerExecuted;
        
        /// <summary>Event raised when a trigger is registered</summary>
        event EventHandler<TriggerRegisteredEventArgs> TriggerRegistered;
        
        /// <summary>Event raised when a trigger is unregistered</summary>
        event EventHandler<TriggerUnregisteredEventArgs> TriggerUnregistered;
        
        /// <summary>Event raised when a trigger chain completes</summary>
        event EventHandler<TriggerChainCompletedEventArgs> TriggerChainCompleted;
        
        #endregion

        #region Statistics & Scope Helpers

        /// <summary>Get aggregate execution statistics for a block's triggers</summary>
        TriggerStatisticsInfo GetTriggerStatistics(string blockName);

        /// <summary>Get only form-scope triggers registered for a block</summary>
        IReadOnlyList<TriggerDefinition> GetFormLevelTriggers(string blockName);

        /// <summary>Get only block-scope triggers registered for a block</summary>
        IReadOnlyList<TriggerDefinition> GetBlockLevelTriggers(string blockName);

        /// <summary>Get only record-scope triggers registered for a block</summary>
        IReadOnlyList<TriggerDefinition> GetRecordLevelTriggers(string blockName);

        /// <summary>Get only item-scope triggers registered for a block</summary>
        IReadOnlyList<TriggerDefinition> GetItemLevelTriggers(string blockName);

        #endregion
    }
    
    #endregion

    #region Savepoint Manager Interface

    /// <summary>
    /// Manages named savepoints for block state snapshots and rollback.
    /// Platform-agnostic — no UI dependencies.
    /// </summary>
    public interface ISavepointManager
    {
        /// <summary>Create a named savepoint; auto-generates name if null passed</summary>
        string CreateSavepoint(string blockName, string savepointName = null);

        /// <summary>Create a savepoint with full state detail and optional record snapshot</summary>
        string CreateSavepoint(string blockName, string savepointName, int recordIndex, int recordCount, bool isDirty, Dictionary<string, object> snapshot = null);

        /// <summary>Roll back to a named savepoint; removes later savepoints</summary>
        Task<bool> RollbackToSavepointAsync(string blockName, string savepointName, CancellationToken ct = default);

        /// <summary>Release (forget) a named savepoint</summary>
        bool ReleaseSavepoint(string blockName, string savepointName);

        /// <summary>Release all savepoints for a block</summary>
        void ReleaseAllSavepoints(string blockName);

        /// <summary>List all savepoints for a block, ordered by creation time</summary>
        IReadOnlyList<SavepointInfo> ListSavepoints(string blockName);

        /// <summary>Check whether a named savepoint exists</summary>
        bool SavepointExists(string blockName, string savepointName);
    }

    #endregion

    #region Lock Manager Interface

    /// <summary>
    /// Provides client-side record locking for data blocks.
    /// Platform-agnostic — no UI dependencies.
    /// </summary>
    public interface ILockManager
    {
        /// <summary>Returns the configured lock mode for a block.</summary>
        LockMode GetLockMode(string blockName);

        /// <summary>Sets the lock mode for a block.</summary>
        void SetLockMode(string blockName, LockMode mode);

        /// <summary>Returns whether automatic lock-on-edit is enabled for a block.</summary>
        bool GetLockOnEdit(string blockName);

        /// <summary>Sets whether automatic lock-on-edit is enabled for a block.</summary>
        void SetLockOnEdit(string blockName, bool value);

        /// <summary>Set the current record index for a block (called on navigation).</summary>
        void SetCurrentRecordIndex(string blockName, int index);

        /// <summary>Locks the current record for a block.</summary>
        Task<bool> LockCurrentRecordAsync(string blockName, CancellationToken ct = default);

        /// <summary>Unlocks the current record for a block.</summary>
        bool UnlockCurrentRecord(string blockName);

        /// <summary>Unlocks all tracked records for a block.</summary>
        void UnlockAllRecords(string blockName);

        /// <summary>Returns whether a specific record is locked.</summary>
        bool IsRecordLocked(string blockName, int recordIndex);

        /// <summary>Returns whether the current record is locked.</summary>
        bool IsCurrentRecordLocked(string blockName);

        /// <summary>Returns lock metadata for a specific record.</summary>
        RecordLockInfo GetLockInfo(string blockName, int recordIndex);

        /// <summary>Returns the number of locked records for a block.</summary>
        int GetLockedRecordCount(string blockName);

        /// <summary>Returns all tracked locks for a block.</summary>
        IReadOnlyList<RecordLockInfo> GetAllLocks(string blockName);

        /// <summary>Lock automatically if LockOnEdit is true and mode is Automatic</summary>
        Task<bool> AutoLockIfNeededAsync(string blockName, CancellationToken ct = default);
    }

    #endregion

    #region Query Builder Manager Interface

    /// <summary>
    /// Builds AppFilter lists from field-value dictionaries and manages query templates.
    /// Pure logic — no UI dependencies.
    /// </summary>
    public interface IQueryBuilderManager
    {
        // Per-block per-field operator registry
        /// <summary>Registers the query operator for a block field.</summary>
        void SetQueryOperator(string blockName, string fieldName, QueryOperator op);

        /// <summary>Returns the registered query operator for a block field.</summary>
        QueryOperator GetQueryOperator(string blockName, string fieldName);

        /// <summary>Clears the registered query operators for a block.</summary>
        void ClearQueryOperators(string blockName);

        // Build AppFilter list from a value dictionary (key = fieldName)
        /// <summary>Builds filters from a block field-value dictionary.</summary>
        List<AppFilter> BuildFilters(string blockName, Dictionary<string, object> fieldValues);

        // WHERE / OrderBy clause helpers (string to AppFilter)
        /// <summary>Parses a WHERE clause into filters.</summary>
        List<AppFilter> ParseWhereClause(string whereClause);

        /// <summary>Parses an ORDER BY clause into filters.</summary>
        List<AppFilter> ParseOrderByClause(string orderByClause);

        /// <summary>Combines two filter lists using AND semantics.</summary>
        List<AppFilter> CombineFiltersAnd(List<AppFilter> a, List<AppFilter> b);

        // Query template CRUD
        /// <summary>Saves a named query template for a block.</summary>
        void SaveQueryTemplate(string blockName, string templateName, List<AppFilter> filters);

        /// <summary>Loads a named query template for a block.</summary>
        QueryTemplateInfo LoadQueryTemplate(string blockName, string templateName);

        /// <summary>Returns all query templates saved for a block.</summary>
        IReadOnlyList<QueryTemplateInfo> GetQueryTemplates(string blockName);

        /// <summary>Deletes a query template for a block.</summary>
        bool DeleteQueryTemplate(string blockName, string templateName);

        /// <summary>Deletes all query templates for a block.</summary>
        void ClearAllTemplates(string blockName);
    }

    #endregion

    #region Block Error Log Interface

    /// <summary>
    /// Per-block error log with FIFO eviction and platform-agnostic events.
    /// </summary>
    public interface IBlockErrorLog
    {
        /// <summary>Gets or sets whether error events should be suppressed.</summary>
        bool SuppressErrorEvents { get; set; }

        /// <summary>Gets or sets the maximum retained log size per block.</summary>
        int MaxLogSize { get; set; }

        /// <summary>Raised when an error entry is logged.</summary>
        event EventHandler<BlockErrorEventArgs> OnError;

        /// <summary>Raised when a warning entry is logged.</summary>
        event EventHandler<BlockErrorEventArgs> OnWarning;

        /// <summary>Logs an error or warning entry for a block.</summary>
        void LogError(string blockName, Exception ex, string context, ErrorSeverity severity = ErrorSeverity.Error);

        /// <summary>Logs a warning entry for a block.</summary>
        void LogWarning(string blockName, string message, string context);

        /// <summary>Clears the error log for a block.</summary>
        void ClearErrorLog(string blockName);

        /// <summary>Clears all block logs.</summary>
        void ClearAllLogs();

        /// <summary>Returns the full retained log for a block.</summary>
        IReadOnlyList<BlockErrorInfo> GetErrorLog(string blockName);

        /// <summary>Returns the retained log entries for a block and context.</summary>
        IReadOnlyList<BlockErrorInfo> GetErrorsForContext(string blockName, string context);

        /// <summary>Returns the retained log entries for a block and severity.</summary>
        IReadOnlyList<BlockErrorInfo> GetErrorsBySeverity(string blockName, ErrorSeverity severity);

        /// <summary>Returns the retained log entry count for a block.</summary>
        int GetErrorCount(string blockName);

        /// <summary>Returns whether the block log contains at least one error-severity entry.</summary>
        bool HasErrors(string blockName);
    }

    #endregion

    #region Message Queue Manager Interface

    /// <summary>
    /// Platform-agnostic message queue for data blocks.
    /// UI layers subscribe to OnMessage/OnMessageCleared to display messages.
    /// </summary>
    public interface IMessageQueueManager
    {
        /// <summary>Gets or sets the intended message display duration in milliseconds.</summary>
        int MessageDisplayDurationMs { get; set; }

        /// <summary>Gets or sets whether the manager should auto-advance queued messages.</summary>
        bool AutoAdvanceMessages { get; set; }

        /// <summary>Raised when a block message becomes current.</summary>
        event EventHandler<BlockMessageEventArgs> OnMessage;

        /// <summary>Raised when a block message is cleared.</summary>
        event EventHandler<BlockMessageEventArgs> OnMessageCleared;

        /// <summary>Sets the current message for a block or enqueues it.</summary>
        void SetMessage(string blockName, string text, MessageLevel level = MessageLevel.Info);

        /// <summary>Clears the current message for a block.</summary>
        void ClearMessage(string blockName);

        /// <summary>Advances to the next queued message for a block.</summary>
        void AdvanceMessage(string blockName);

        /// <summary>Queues an informational message for a block.</summary>
        void ShowInfoMessage(string blockName, string text);

        /// <summary>Queues a success message for a block.</summary>
        void ShowSuccessMessage(string blockName, string text);

        /// <summary>Queues a warning message for a block.</summary>
        void ShowWarningMessage(string blockName, string text);

        /// <summary>Queues an error message for a block.</summary>
        void ShowErrorMessage(string blockName, string text);

        /// <summary>Returns the current message text for a block.</summary>
        string GetCurrentMessage(string blockName);

        /// <summary>Returns the level of the current message for a block.</summary>
        MessageLevel GetCurrentMessageLevel(string blockName);

        /// <summary>Returns the number of queued messages waiting for a block.</summary>
        int GetQueuedMessageCount(string blockName);
    }

    #endregion

    #region Block Factory Interface

    /// <summary>
    /// Resolves IUnitofWork + IEntityStructure from connection name and entity name via IDMEEditor.
    /// </summary>
    public interface IBlockFactory
    {
        /// <summary>Create a UoW + EntityStructure pair from a connection and entity name</summary>
        Task<(IUnitofWork UoW, IEntityStructure Structure)> CreateBlockAsync(
            string connectionName, string entityName, CancellationToken ct = default);

        /// <summary>Validate that the connection + entity pair resolves correctly</summary>
        Task<bool> ValidateBlockSourceAsync(
            string connectionName, string entityName, CancellationToken ct = default);
    }

    #endregion

    #region Block Property Manager Interface

    /// <summary>
    /// Sets and gets Oracle Forms-equivalent block properties on registered blocks.
    /// Corresponds to Oracle Forms SET_BLOCK_PROPERTY / GET_BLOCK_PROPERTY built-ins.
    /// </summary>
    public interface IBlockPropertyManager
    {
        /// <summary>Set a property on the named block</summary>
        void SetBlockProperty(string blockName, Forms.Models.BlockProperty property, object value);

        /// <summary>Get a property value from the named block</summary>
        object GetBlockProperty(string blockName, Forms.Models.BlockProperty property);

        /// <summary>Typed convenience overload for GetBlockProperty</summary>
        T GetBlockProperty<T>(string blockName, Forms.Models.BlockProperty property);
    }

    #endregion

    #region Alert Provider Interface

    /// <summary>
    /// Pluggable UI provider for modal alert dialogs.
    /// Inject an implementation from the UI layer; the default no-op implementation
    /// logs to Status and returns AlertResult.Button1.
    /// </summary>
    public interface IAlertProvider
    {
        /// <summary>
        /// Display an alert dialog and return the button the user pressed.
        /// Corresponds to Oracle Forms SHOW_ALERT built-in.
        /// </summary>
        Task<Forms.Models.AlertResult> ShowAlertAsync(
            string title,
            string message,
            Forms.Models.AlertStyle style = Forms.Models.AlertStyle.None,
            string button1Text = "OK",
            string button2Text = null,
            string button3Text = null,
            CancellationToken ct = default);
    }

    #endregion

    #region Sequence Provider Interface

    /// <summary>
    /// Provides named auto-increment sequences.
    /// Corresponds to Oracle Forms :SEQUENCE.NEXTVAL usage.
    /// </summary>
    public interface ISequenceProvider
    {
        /// <summary>Increment and return the next value for the named sequence</summary>
        long GetNextSequence(string sequenceName);

        /// <summary>Peek at the next value without incrementing</summary>
        long PeekNextSequence(string sequenceName);

        /// <summary>Reset a sequence to a starting value (default 1)</summary>
        void ResetSequence(string sequenceName, long startValue = 1);

        /// <summary>Whether the named sequence has been created</summary>
        bool SequenceExists(string sequenceName);

        /// <summary>Create a new named sequence with the given starting value</summary>
        void CreateSequence(string sequenceName, long startValue = 1, long incrementBy = 1);
    }

    #endregion

    #region Timer Manager Interface

    /// <summary>
    /// Manages named form-level timers that fire WHEN-TIMER-EXPIRED triggers.
    /// Corresponds to Oracle Forms CREATE_TIMER / DELETE_TIMER / GET_TIMER built-ins.
    /// </summary>
    public interface ITimerManager : IDisposable
    {
        /// <summary>
        /// Create and start a named timer.
        /// Corresponds to Oracle Forms CREATE_TIMER.
        /// </summary>
        Forms.Models.TimerDefinition CreateTimer(string timerName, TimeSpan interval, bool repeating = false);

        /// <summary>
        /// Stop and remove a named timer.
        /// Corresponds to Oracle Forms DELETE_TIMER.
        /// Returns false if the timer was not found.
        /// </summary>
        bool DeleteTimer(string timerName);

        /// <summary>
        /// Get the definition/state of a named timer.
        /// Corresponds to Oracle Forms GET_TIMER.
        /// </summary>
        Forms.Models.TimerDefinition GetTimer(string timerName);

        /// <summary>Returns all currently registered timers (running and paused)</summary>
        IReadOnlyList<Forms.Models.TimerDefinition> GetAllTimers();

        /// <summary>Whether a timer with the given name exists</summary>
        bool TimerExists(string timerName);

        /// <summary>
        /// Event raised when a timer fires.
        /// Handlers should fire TriggerType.WhenTimerExpired for their block/form.
        /// </summary>
        event EventHandler<TimerFiredEventArgs> TimerFired;
    }

    /// <summary>
    /// Event arguments for the ITimerManager.TimerFired event.
    /// </summary>
    public class TimerFiredEventArgs : EventArgs
    {
        /// <summary>Gets the logical timer name that fired.</summary>
        public string TimerName { get; init; }

        /// <summary>Gets the number of times the timer has fired.</summary>
        public int FireCount { get; init; }

        /// <summary>Gets the timestamp when the timer fired.</summary>
        public DateTime FiredAt { get; init; } = DateTime.Now;
    }

    #endregion

    // ──────────────────────────────────────────────────────────────────────────
    // Phase 3 — Multi-Form & Cross-Form Communication
    // ──────────────────────────────────────────────────────────────────────────

    #region IFormRegistry

    /// <summary>
    /// Shared registry of all active form managers.
    /// Pass a single instance to every FormsManager so forms can discover each other.
    /// Equivalent to Oracle Forms' :GLOBAL scope and CALL_FORM / OPEN_FORM / NEW_FORM built-ins.
    /// </summary>
    public interface IFormRegistry
    {
        /// <summary>Name of the currently active (focused) form, or null.</summary>
        string ActiveFormName { get; }

        /// <summary>Register a form manager under a logical form name.</summary>
        void RegisterForm(string formName, IUnitofWorksManager form);

        /// <summary>Remove a form from the registry. Returns false if not found.</summary>
        bool UnregisterForm(string formName);

        /// <summary>Retrieve a registered form manager by name, or null.</summary>
        IUnitofWorksManager GetForm(string formName);

        /// <summary>Get all currently registered form names.</summary>
        IReadOnlyList<string> GetActiveFormNames();

        /// <summary>Returns true when a form with the given name is registered.</summary>
        bool FormExists(string formName);

        /// <summary>Mark a form as the currently active/focused form.</summary>
        void SetActiveForm(string formName);

        /// <summary>Set or overwrite a global variable (:GLOBAL.name).</summary>
        void SetGlobal(string name, object value);

        /// <summary>Read a global variable. Returns null if not set.</summary>
        object GetGlobal(string name);

        /// <summary>Returns true if a global variable with the given name exists.</summary>
        bool GlobalExists(string name);

        /// <summary>Raised whenever a form is registered, unregistered, activated or deactivated.</summary>
        event EventHandler<FormLifecycleEventArgs> FormLifecycleChanged;
    }

    #endregion

    #region IFormMessageBus

    /// <summary>
    /// Pub/sub message bus for inter-form communication.
    /// Equivalent to Oracle Forms' DO_KEY / SYNCHRONIZE and custom messaging patterns.
    /// </summary>
    public interface IFormMessageBus
    {
        /// <summary>
        /// Send a typed message payload to a specific form.
        /// Any subscribers registered for (targetForm, messageType) are invoked synchronously.
        /// </summary>
        void PostMessage(string targetForm, string messageType, object payload, string senderForm = null);

        /// <summary>Broadcast a message to all forms subscribed to the given messageType.</summary>
        void Broadcast(string messageType, object payload, string senderForm = null);

        /// <summary>Subscribe a form to receive messages of a given type.</summary>
        void Subscribe(string formName, string messageType, Action<FormMessage> handler);

        /// <summary>Unsubscribe a form from a specific message type.</summary>
        void Unsubscribe(string formName, string messageType);

        /// <summary>Remove all subscriptions registered by a form (call during cleanup).</summary>
        void UnsubscribeAll(string formName);

        /// <summary>Raised for every message posted or broadcast (global observer hook).</summary>
        event EventHandler<FormMessageEventArgs> OnFormMessage;
    }

    #endregion

    #region ISharedBlockManager

    /// <summary>
    /// Manages IUnitofWork data blocks that are shared across multiple form managers.
    /// Provides optimistic lock coordination so only one form modifies a shared block at a time.
    /// </summary>
    public interface ISharedBlockManager
    {
        /// <summary>Publish a block UoW so other forms can access it. Returns false if already exists.</summary>
        bool CreateSharedBlock(string blockName, IUnitofWork uow);

        /// <summary>Retrieve a shared block UoW by name, or null.</summary>
        IUnitofWork GetSharedBlock(string blockName);

        /// <summary>Returns true when the named shared block exists.</summary>
        bool SharedBlockExists(string blockName);

        /// <summary>Remove a shared block (releases any outstanding lock).</summary>
        bool RemoveSharedBlock(string blockName);

        /// <summary>
        /// Attempt to acquire an exclusive write lock on a shared block.
        /// Returns true when the lock is obtained within the timeout.
        /// </summary>
        bool TryLockSharedBlock(string blockName, string lockedBy, TimeSpan timeout);

        /// <summary>Release a write lock held by the named caller. No-op if not locked by that caller.</summary>
        void ReleaseSharedBlockLock(string blockName, string lockedBy);

        /// <summary>Raised when any caller notifies that a shared block's data has changed.</summary>
        event EventHandler<SharedBlockChangedEventArgs> SharedBlockChanged;
    }

    #endregion

    // ──────────────────────────────────────────────────────────────────────────
    // Phase 4 — Advanced Trigger System
    // ──────────────────────────────────────────────────────────────────────────

    #region TriggerExecutionLogEntry

    /// <summary>
    /// One recorded execution of a trigger (timing + outcome).
    /// Stored by <see cref="ITriggerExecutionLog"/>.
    /// </summary>
    public class TriggerExecutionLogEntry
    {
        /// <summary>Gets or sets the unique trigger identifier.</summary>
        public string TriggerId    { get; set; }

        /// <summary>Gets or sets the trigger display name.</summary>
        public string TriggerName  { get; set; }

        /// <summary>Gets or sets the trigger type that executed.</summary>
        public TriggerType TriggerType { get; set; }

        /// <summary>Gets or sets the block involved in the execution.</summary>
        public string BlockName    { get; set; }

        /// <summary>Gets or sets the item involved in the execution.</summary>
        public string ItemName     { get; set; }

        /// <summary>Gets or sets the trigger execution result.</summary>
        public TriggerResult Result { get; set; }

        /// <summary>Gets or sets the execution time in milliseconds.</summary>
        public long ElapsedMs      { get; set; }

        /// <summary>Gets or sets when the trigger executed.</summary>
        public DateTime ExecutedAt { get; set; } = DateTime.Now;

        /// <summary>Gets or sets the error message for failed trigger executions.</summary>
        public string ErrorMessage { get; set; }
    }

    #endregion

    #region ITriggerExecutionLog

    /// <summary>
    /// In-memory ring-buffer log of recent trigger executions with timing.
    /// </summary>
    public interface ITriggerExecutionLog
    {
        /// <summary>Maximum number of entries to retain (oldest are dropped).</summary>
        int Capacity { get; set; }

        /// <summary>Append an entry.</summary>
        void Record(TriggerExecutionLogEntry entry);

        /// <summary>All retained entries, newest last.</summary>
        IReadOnlyList<TriggerExecutionLogEntry> GetAll();

        /// <summary>Entries for a specific block.</summary>
        IReadOnlyList<TriggerExecutionLogEntry> GetByBlock(string blockName);

        /// <summary>Entries for a specific trigger type.</summary>
        IReadOnlyList<TriggerExecutionLogEntry> GetByType(TriggerType type);

        /// <summary>Remove all retained entries.</summary>
        void Clear();
    }

    #endregion

    #region ITriggerDependencyManager

    /// <summary>
    /// Builds a dependency graph over <see cref="TriggerDefinition.DependsOn"/> lists,
    /// detects cycles, and returns an execution-ordered list for a set of triggers.
    /// </summary>
    public interface ITriggerDependencyManager
    {
        /// <summary>
        /// Returns the triggers in dependency order (topological sort).
        /// Throws <see cref="InvalidOperationException"/> if a cycle is detected.
        /// </summary>
        IReadOnlyList<TriggerDefinition> OrderByDependency(IReadOnlyList<TriggerDefinition> triggers);

        /// <summary>
        /// Returns true when the supplied list contains a circular dependency.
        /// </summary>
        bool HasCircularDependency(IReadOnlyList<TriggerDefinition> triggers);

        /// <summary>
        /// Returns the names of all triggers involved in a cycle, or empty list when no cycle exists.
        /// </summary>
        IReadOnlyList<string> FindCycle(IReadOnlyList<TriggerDefinition> triggers);
    }

    #endregion

    // ── Phase 5: Audit Trail ────────────────────────────────────────────────

    #region IAuditStore

    /// <summary>Pluggable persistence back-end for audit entries.</summary>
    public interface IAuditStore
    {
        /// <summary>Persist a single audit entry.</summary>
        void Save(AuditEntry entry);

        /// <summary>Query audit entries with optional filters.</summary>
        IReadOnlyList<AuditEntry> Query(
            string blockName         = null,
            AuditOperation? operation = null,
            DateTime? from           = null,
            DateTime? to             = null);

        /// <summary>Remove entries older than <paramref name="olderThanDays"/> days.</summary>
        void Purge(int olderThanDays);

        /// <summary>Remove all entries.</summary>
        void Clear();
    }

    #endregion

    #region IAuditManager

    /// <summary>
    /// Manages field-level and commit-level audit recording for the forms engine.
    /// Accumulates pending field changes and flushes them as <see cref="AuditEntry"/>
    /// objects to the configured <see cref="IAuditStore"/> on each commit.
    /// </summary>
    public interface IAuditManager
    {
        /// <summary>Current audit configuration.</summary>
        AuditConfiguration Configuration { get; }

        /// <summary>Name of the currently logged-in user stamped on each audit entry.</summary>
        string CurrentUser { get; }

        /// <summary>The underlying store (injectable for testing or external persistence).</summary>
        IAuditStore Store { get; }

        // ── Configuration ────────────────────────────────────────────────────

        /// <summary>Sets the user name stamped on every subsequent audit entry.</summary>
        void SetAuditUser(string userName);

        /// <summary>Applies configuration changes via a delegate.</summary>
        void Configure(Action<AuditConfiguration> configure);

        // ── Accumulation ─────────────────────────────────────────────────────

        /// <summary>
        /// Records a single field change in the pending buffer.
        /// Called for every <see cref="BlockFieldChangedEventArgs"/> while audit is enabled.
        /// </summary>
        void RecordFieldChange(
            string blockName,
            string fieldName,
            object oldValue,
            object newValue,
            int recordIndex);

        /// <summary>
        /// Flushes all pending field changes as committed audit entries.
        /// Should be called after a successful <c>CommitFormAsync</c>.
        /// </summary>
        void FlushPendingToStore(string formName, AuditOperation operation);

        /// <summary>
        /// Discards all pending (uncommitted) field changes.
        /// Should be called after a rollback.
        /// </summary>
        void DiscardPending();

        // ── Query ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns audit entries from the store, with optional block/operation/date filters.
        /// </summary>
        IReadOnlyList<AuditEntry> GetAuditLog(
            string blockName         = null,
            AuditOperation? operation = null,
            DateTime? from           = null,
            DateTime? to             = null);

        /// <summary>
        /// Returns the history of changes to a specific field within a record.
        /// <paramref name="recordKey"/> is the string form of the row index or PK.
        /// </summary>
        IReadOnlyList<AuditFieldChange> GetFieldHistory(
            string blockName,
            string recordKey,
            string fieldName);

        // ── Export ────────────────────────────────────────────────────────────

        /// <summary>Writes all (or block-filtered) audit entries to a CSV file.</summary>
        System.Threading.Tasks.Task ExportToCsvAsync(string filePath, string blockName = null);

        /// <summary>Writes all (or block-filtered) audit entries to a JSON file.</summary>
        System.Threading.Tasks.Task ExportToJsonAsync(string filePath, string blockName = null);

        // ── Maintenance ───────────────────────────────────────────────────────

        /// <summary>Purges entries older than <paramref name="olderThanDays"/> days.</summary>
        void Purge(int olderThanDays);

        /// <summary>Clears all audit data from the store.</summary>
        void Clear();
    }

    #endregion

    // ── Phase 6: Security & Authorization ──────────────────────────────────

    #region IFieldMaskProvider

    /// <summary>Applies a mask pattern to a raw field value for display purposes.</summary>
    public interface IFieldMaskProvider
    {
        /// <summary>
        /// Returns a masked representation of <paramref name="rawValue"/> using
        /// <paramref name="pattern"/>.  The single character "*" means "hide everything".
        /// Other patterns use '#' as a digit placeholder and '*' for any char.
        /// </summary>
        string Mask(object rawValue, string pattern);
    }

    #endregion

    #region ISecurityManager

    /// <summary>
    /// Controls block- and field-level security for the forms engine.
    /// Integrates with <see cref="IBlockPropertyManager"/> and <see cref="IItemPropertyManager"/>
    /// to enforce permissions at runtime.
    /// </summary>
    public interface ISecurityManager
    {
        /// <summary>Raised whenever a security violation is detected.</summary>
        event EventHandler<SecurityViolationEventArgs> OnSecurityViolation;

        /// <summary>The currently active security context (user + roles).</summary>
        SecurityContext CurrentContext { get; }

        // ── Context ──────────────────────────────────────────────────────────

        /// <summary>Sets the current security context and re-evaluates all block/field permissions.</summary>
        void SetSecurityContext(SecurityContext context);

        // ── Block Security ───────────────────────────────────────────────────

        /// <summary>Registers or replaces block-level security for <paramref name="blockName"/>.</summary>
        void SetBlockSecurity(string blockName, BlockSecurity security);

        /// <summary>Returns the registered security rules for a block, or null if none.</summary>
        BlockSecurity GetBlockSecurity(string blockName);

        /// <summary>
        /// Returns true when the current user/roles may perform <paramref name="permission"/> on the block.
        /// Also applies to a specific permission flag when <c>ISecurityManager</c> is used standalone.
        /// </summary>
        bool IsBlockAllowed(string blockName, SecurityPermission permission);

        /// <summary>
        /// Evaluates all registered block securities against the current context and updates
        /// <c>DataBlockInfo.InsertAllowed</c> / <c>UpdateAllowed</c> / <c>DeleteAllowed</c> / <c>QueryAllowed</c>
        /// via the supplied <paramref name="applyBlockFlags"/> callback.
        /// </summary>
        void ApplyBlockSecurityFlags(Action<string, bool, bool, bool, bool> applyBlockFlags);

        /// <summary>
        /// Returns the effective row-filter WHERE clause for a block (or empty string if none).
        /// </summary>
        string GetBlockRowFilter(string blockName);

        // ── Field Security ───────────────────────────────────────────────────

        /// <summary>Registers or replaces field-level security for a specific item.</summary>
        void SetFieldSecurity(string blockName, string fieldName, FieldSecurity security);

        /// <summary>Returns registered field security or null.</summary>
        FieldSecurity GetFieldSecurity(string blockName, string fieldName);

        /// <summary>
        /// Evaluates all registered field securities against the current context and applies
        /// Enabled / Visible flags via the supplied callbacks (delegates into ItemPropertyManager).
        /// </summary>
        void ApplyFieldSecurityFlags(
            Action<string, string, bool> setEnabled,
            Action<string, string, bool> setVisible);

        /// <summary>
        /// Returns a masked / display-safe value for a field, applying the registered mask pattern
        /// if field security has <c>Masked = true</c>.  Returns the raw value unchanged otherwise.
        /// </summary>
        object GetMaskedValue(string blockName, string fieldName, object rawValue);

        // ── Logging ──────────────────────────────────────────────────────────

        /// <summary>Records a security violation without throwing.</summary>
        void RaiseViolation(string blockName, string fieldName, SecurityPermission permission, string message);

        /// <summary>Returns all recorded violations for the current session.</summary>
        IReadOnlyList<SecurityViolationEventArgs> GetViolationLog();
    }

    #endregion

    #region IPagingManager

    /// <summary>
    /// Manages per-block paging state for virtual scrolling / paged data loading.
    /// </summary>
    public interface IPagingManager
    {
        /// <summary>Sets the page size for a block (records per page).</summary>
        void SetPageSize(string blockName, int pageSize);

        /// <summary>Gets the current page size for a block (default 50).</summary>
        int GetPageSize(string blockName);

        /// <summary>
        /// Returns a <see cref="PageInfo"/> snapshot for the current page of the block.
        /// </summary>
        PageInfo GetCurrentPage(string blockName);

        /// <summary>
        /// Advances the paging state to the specified page number and returns the resulting
        /// <see cref="PageInfo"/>.  Does NOT load data — callers must re-execute the query.
        /// </summary>
        PageInfo SetCurrentPage(string blockName, int pageNumber);

        /// <summary>Stores the total record count for a block (from a COUNT query or post-load).</summary>
        void SetTotalRecordCount(string blockName, long count);

        /// <summary>Returns the stored total record count for a block.</summary>
        long GetTotalRecordCount(string blockName);

        /// <summary>
        /// Sets the number of pages to pre-fetch ahead of the current page (0 = disabled).
        /// </summary>
        void SetFetchAheadDepth(string blockName, int depth);

        /// <summary>Returns the configured fetch-ahead depth for a block.</summary>
        int GetFetchAheadDepth(string blockName);

        /// <summary>Resets all paging state for a block to defaults.</summary>
        void ResetPaging(string blockName);
    }

    #endregion
}