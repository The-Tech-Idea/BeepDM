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

}
