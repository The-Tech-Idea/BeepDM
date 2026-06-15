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

        /// <summary>Sets a named field or property value on a record.</summary>
        bool SetFieldValue(object record, string FieldName, object value);

        // Block Change Feed
        /// <summary>Raised when a tracked field value changes within any registered block.</summary>
        event EventHandler<BlockFieldChangedEventArgs> OnBlockFieldChanged;

        /// <summary>Raised when the manager receives an inter-form message for this form.</summary>
        event EventHandler<FormMessageEventArgs> OnFormMessage;

    }

}
