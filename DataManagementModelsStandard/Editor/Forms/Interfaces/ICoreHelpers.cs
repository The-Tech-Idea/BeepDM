using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor.UOWManager.Configuration;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;


namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{

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

        /// <summary>Fires the host-defined custom item event (WHEN-CUSTOM-ITEM-EVENT) with an event type identifier and optional payload.</summary>
        bool TriggerCustomItemEvent(string eventType, string blockName, string itemName, object payload = null);

        /// <summary>Raised when a host-defined custom item event is triggered via <see cref="TriggerCustomItemEvent"/>.</summary>
        event EventHandler<CustomItemEventArgs> OnCustomItemEvent;
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

        // PR 17 (rev 2): the next two methods were referenced from
        // FormsManager.FormsSimulation.cs wrappers but were missing from
        // the interface. FormsSimulationHelper (the implementation) had
        // them all along, so this is just a contract gap. Adding them here
        // lets the engine compile.

        /// <summary>Sets a named system variable (e.g. <c>SYSTEM.RECORD_STATUS</c>) on a record.</summary>
        void SetSystemVariables(object record, SystemVariableType variableType, object value = null);

        /// <summary>Validates a field's value against a set of field constraints. Returns the validation result.</summary>
        ValidationResult ValidateField(object record, string FieldName, object value, FieldConstraints constraints = null);
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

}
