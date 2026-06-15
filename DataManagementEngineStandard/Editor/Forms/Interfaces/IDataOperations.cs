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
        string CreateSavepoint(string blockName, string savepointName, int recordIndex, int recordCount, bool isDirty, IDictionary<string, object> snapshot = null);

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
