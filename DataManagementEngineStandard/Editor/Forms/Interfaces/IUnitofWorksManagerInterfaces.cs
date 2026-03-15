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
    /// Core interface for UnitofWorksManager functionality
    /// </summary>
    public interface IUnitofWorksManager : IDisposable
    {
        #region Properties
        IDMEEditor DMEEditor { get; }
        string CurrentFormName { get; set; }
        string CurrentBlockName { get; set; }
        IReadOnlyDictionary<string, DataBlockInfo> Blocks { get; }
        bool IsDirty { get; }
        string Status { get; }
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
        #endregion

        #region Block Management
        void RegisterBlock(string blockName, IUnitofWork unitOfWork, IEntityStructure entityStructure, 
            string dataSourceName = null, bool isMasterBlock = false);
        bool UnregisterBlock(string blockName);
        DataBlockInfo GetBlock(string blockName);
        IUnitofWork GetUnitOfWork(string blockName);
        bool BlockExists(string blockName);
        #endregion

        #region Form Operations
        Task<bool> OpenFormAsync(string formName);
        Task<bool> CloseFormAsync();
        Task<IErrorsInfo> CommitFormAsync();
        Task<IErrorsInfo> RollbackFormAsync();
        Task ClearAllBlocksAsync();
        Task ClearBlockAsync(string blockName);
        #endregion

        #region Navigation
        Task<bool> FirstRecordAsync(string blockName);
        Task<bool> NextRecordAsync(string blockName);
        Task<bool> PreviousRecordAsync(string blockName);
        Task<bool> LastRecordAsync(string blockName);
        #endregion

        #region Data Operations
        Task<bool> SwitchToBlockAsync(string blockName);
        Task<bool> InsertRecordAsync(string blockName, object record = null);
        Task<bool> DeleteCurrentRecordAsync(string blockName);
        Task<bool> EnterQueryAsync(string blockName);
        Task<bool> ExecuteQueryAsync(string blockName, List<AppFilter> filters = null);
        #endregion

        #region Validation
        bool ValidateField(string blockName, string FieldName, object value);
        bool ValidateBlock(string blockName);
        bool ValidateForm();
        #endregion
    }

    /// <summary>
    /// Interface for relationship management functionality
    /// </summary>
    public interface IRelationshipManager
    {
        void CreateMasterDetailRelation(string masterBlockName, string detailBlockName, 
            string masterKeyField, string detailForeignKeyField, RelationshipType relationshipType = RelationshipType.OneToMany);
        Task SynchronizeDetailBlocksAsync(string masterBlockName);
        List<string> GetDetailBlocks(string masterBlockName);
        string GetMasterBlock(string detailBlockName);
        void RemoveBlockRelationships(string blockName);
    }

    /// <summary>
    /// Interface for dirty state management functionality
    /// </summary>
    public interface IDirtyStateManager
    {
        Task<bool> CheckAndHandleUnsavedChangesAsync(string blockName);
        bool HasUnsavedChanges();
        List<string> GetDirtyBlocks();
        void CollectDirtyDetailBlocks(string blockName, List<string> dirtyBlocks);
        Task<bool> SaveDirtyBlocksAsync(List<string> dirtyBlocks);
        Task<bool> RollbackDirtyBlocksAsync(List<string> dirtyBlocks);
        event EventHandler<UnsavedChangesEventArgs> OnUnsavedChanges;
    }

    /// <summary>
    /// Interface for event handling functionality
    /// </summary>
    public interface IEventManager
    {
        void SubscribeToUnitOfWorkEvents(IUnitofWork unitOfWork, string blockName);
        void UnsubscribeFromUnitOfWorkEvents(IUnitofWork unitOfWork, string blockName);
        void TriggerBlockEnter(string blockName);
        void TriggerBlockLeave(string blockName);
        void TriggerError(string blockName, Exception ex);
        bool TriggerFieldValidation(string blockName, string FieldName, object value);
        bool TriggerRecordValidation(string blockName, object record);
    }

    /// <summary>
    /// Interface for Oracle Forms simulation helpers
    /// </summary>
    public interface IFormsSimulationHelper
    {
        void SetAuditDefaults(object record, string currentUser = null);
        bool SetFieldValue(object record, string FieldName, object value);
        object GetFieldValue(object record, string FieldName);
        bool ExecuteSequence(string blockName, object record, string FieldName, string sequenceName);
        object GetPropertyValue(object obj, string propertyName);
    }

    /// <summary>
    /// Interface for performance and caching functionality
    /// </summary>
    public interface IPerformanceManager : IDisposable
    {
        void OptimizeBlockAccess();
        void CacheBlockInfo(string blockName, DataBlockInfo blockInfo);
        DataBlockInfo GetCachedBlockInfo(string blockName);
        void ClearCache();
        PerformanceStatistics GetPerformanceStatistics();
    }

    /// <summary>
    /// Interface for configuration management
    /// </summary>
    public interface IConfigurationManager
    {
        UnitofWorksManagerConfiguration Configuration { get; set; }
        void LoadConfiguration();
        void SaveConfiguration();
        void ResetToDefaults();
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
        public string BlockName { get; set; }
        public string ItemName { get; set; }
        public object Value { get; set; }
        public ValidationRule FailedRule { get; set; }
        public ValidationRuleResult Result { get; set; }
        public bool Cancel { get; set; }
    }
    
    /// <summary>
    /// Event arguments for validation starting event
    /// </summary>
    public class ValidationStartingEventArgs : EventArgs
    {
        public string BlockName { get; set; }
        public string ItemName { get; set; }
        public object Value { get; set; }
        public IReadOnlyList<ValidationRule> Rules { get; set; }
        public bool Cancel { get; set; }
    }
    
    /// <summary>
    /// Event arguments for validation completed event
    /// </summary>
    public class ValidationCompletedEventArgs : EventArgs
    {
        public string BlockName { get; set; }
        public string ItemName { get; set; }
        public bool IsValid { get; set; }
        public int RulesEvaluated { get; set; }
        public int RulesFailed { get; set; }
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
        public string BlockName { get; set; }
        public string FieldName { get; set; }
        public LOVDefinition LOV { get; set; }
        public int RecordCount { get; set; }
        public bool FromCache { get; set; }
        public long LoadTimeMs { get; set; }
    }
    
    /// <summary>
    /// Event arguments for LOV validation event
    /// </summary>
    public class LOVValidationEventArgs : EventArgs
    {
        public string BlockName { get; set; }
        public string FieldName { get; set; }
        public object Value { get; set; }
        public string ErrorMessage { get; set; }
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
    }
    
    #endregion
}