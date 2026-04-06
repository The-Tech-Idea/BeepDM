using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Represents an item/field definition with Oracle Forms-compatible properties.
    /// UI-agnostic version that can be used by any UI framework.
    /// </summary>
    public class ItemInfo
    {
        #region Identification
        
        /// <summary>
        /// Item name (field identifier)
        /// </summary>
        public string ItemName { get; set; }
        
        /// <summary>
        /// Block this item belongs to
        /// </summary>
        public string BlockName { get; set; }
        
        /// <summary>
        /// Database column name this item maps to
        /// </summary>
        public string BoundProperty { get; set; }
        
        #endregion
        
        #region Oracle Forms Item Properties
        
        /// <summary>
        /// Oracle Forms: REQUIRED - Field must have value
        /// </summary>
        public bool Required { get; set; }
        
        /// <summary>
        /// Oracle Forms: ENABLED - Field can be edited
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Oracle Forms: VISIBLE - Field is shown
        /// </summary>
        public bool Visible { get; set; } = true;
        
        /// <summary>
        /// Oracle Forms: QUERY_ALLOWED - Can be used in query mode
        /// </summary>
        public bool QueryAllowed { get; set; } = true;
        
        /// <summary>
        /// Oracle Forms: INSERT_ALLOWED - Can be modified during new record
        /// </summary>
        public bool InsertAllowed { get; set; } = true;
        
        /// <summary>
        /// Oracle Forms: UPDATE_ALLOWED - Can be modified during edit
        /// </summary>
        public bool UpdateAllowed { get; set; } = true;
        
        /// <summary>
        /// Oracle Forms: DEFAULT_VALUE - Value for new records
        /// </summary>
        public object DefaultValue { get; set; }
        
        /// <summary>
        /// Oracle Forms: PROMPT_TEXT - Label text
        /// </summary>
        public string PromptText { get; set; }
        
        /// <summary>
        /// Oracle Forms: HINT_TEXT - Tooltip/help text  
        /// </summary>
        public string HintText { get; set; }
        
        /// <summary>
        /// Oracle Forms: LOV_NAME - Attached List of Values
        /// </summary>
        public string LOVName { get; set; }
        
        /// <summary>
        /// Oracle Forms: MAX_LENGTH - Maximum text length
        /// </summary>
        public int MaxLength { get; set; }
        
        /// <summary>
        /// Oracle Forms: FORMAT_MASK - Display format (e.g., "MM/DD/YYYY", "#,##0.00")
        /// </summary>
        public string FormatMask { get; set; }
        
        /// <summary>
        /// Validation formula/expression
        /// </summary>
        public string ValidationFormula { get; set; }
        
        /// <summary>
        /// List of validation rule names applied to this item
        /// </summary>
        public List<string> ValidationRuleNames { get; set; } = new List<string>();
        
        #endregion
        
        #region Item State
        
        /// <summary>
        /// Whether item value has changed in current record
        /// </summary>
        public bool IsDirty { get; set; }
        
        /// <summary>
        /// Previous value (before edit)
        /// </summary>
        public object OldValue { get; set; }
        
        /// <summary>
        /// Current value
        /// </summary>
        public object CurrentValue { get; set; }
        
        /// <summary>
        /// Whether item currently has focus
        /// </summary>
        public bool HasFocus { get; set; }
        
        /// <summary>
        /// Whether item has validation error
        /// </summary>
        public bool HasError { get; set; }
        
        /// <summary>
        /// Error message text
        /// </summary>
        public string ErrorMessage { get; set; }
        
        #endregion
        
        #region Navigation
        
        /// <summary>
        /// Tab order position (0 = first)
        /// </summary>
        public int TabIndex { get; set; }
        
        /// <summary>
        /// Next item in navigation order
        /// </summary>
        public string NextNavigationItem { get; set; }
        
        /// <summary>
        /// Previous item in navigation order
        /// </summary>
        public string PreviousNavigationItem { get; set; }
        
        #endregion
        
        #region Data Type
        
        /// <summary>
        /// .NET type of the item value
        /// </summary>
        public Type DataType { get; set; }
        
        /// <summary>
        /// Database type name (e.g., "VARCHAR2", "NUMBER", "DATETIME")
        /// </summary>
        public string DatabaseTypeName { get; set; }
        
        /// <summary>
        /// Precision for numeric types
        /// </summary>
        public int Precision { get; set; }
        
        /// <summary>
        /// Scale for numeric types
        /// </summary>
        public int Scale { get; set; }
        
        /// <summary>
        /// Whether the item allows null values
        /// </summary>
        public bool AllowNull { get; set; } = true;
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Check if item is editable based on current form mode
        /// </summary>
        /// <param name="mode">Current form mode</param>
        /// <returns>True if item can be edited in current mode</returns>
        public bool IsEditable(FormMode mode)
        {
            if (!Enabled) return false;
            
            return mode switch
            {
                FormMode.Query => QueryAllowed,
                FormMode.Insert => InsertAllowed,
                FormMode.Update => UpdateAllowed,
                FormMode.Normal => true,
                _ => false
            };
        }
        
        /// <summary>
        /// Reset item state (after commit or rollback)
        /// </summary>
        public void ResetState()
        {
            IsDirty = false;
            OldValue = null;
            HasError = false;
            ErrorMessage = null;
        }
        
        /// <summary>
        /// Mark item as dirty (value changed)
        /// </summary>
        /// <param name="previousValue">Value before change</param>
        public void MarkDirty(object previousValue)
        {
            if (!IsDirty)
            {
                OldValue = previousValue;
                IsDirty = true;
            }
        }
        
        /// <summary>
        /// Set error state
        /// </summary>
        /// <param name="message">Error message</param>
        public void SetError(string message)
        {
            HasError = true;
            ErrorMessage = message;
        }
        
        /// <summary>
        /// Clear error state
        /// </summary>
        public void ClearError()
        {
            HasError = false;
            ErrorMessage = null;
        }
        
        /// <summary>
        /// Clone item definition (without state)
        /// </summary>
        /// <returns>Cloned ItemInfo</returns>
        public ItemInfo Clone()
        {
            return new ItemInfo
            {
                ItemName = ItemName,
                BlockName = BlockName,
                BoundProperty = BoundProperty,
                Required = Required,
                Enabled = Enabled,
                Visible = Visible,
                QueryAllowed = QueryAllowed,
                InsertAllowed = InsertAllowed,
                UpdateAllowed = UpdateAllowed,
                DefaultValue = DefaultValue,
                PromptText = PromptText,
                HintText = HintText,
                LOVName = LOVName,
                MaxLength = MaxLength,
                FormatMask = FormatMask,
                ValidationFormula = ValidationFormula,
                ValidationRuleNames = new List<string>(ValidationRuleNames),
                TabIndex = TabIndex,
                DataType = DataType,
                DatabaseTypeName = DatabaseTypeName,
                Precision = Precision,
                Scale = Scale,
                AllowNull = AllowNull
            };
        }
        
        #endregion
        
        #region Factory Methods
        
        /// <summary>
        /// Create a basic item
        /// </summary>
        public static ItemInfo Create(string blockName, string itemName, Type dataType = null)
        {
            return new ItemInfo
            {
                BlockName = blockName,
                ItemName = itemName,
                BoundProperty = itemName,
                DataType = dataType ?? typeof(string),
                PromptText = itemName
            };
        }
        
        /// <summary>
        /// Create a required item
        /// </summary>
        public static ItemInfo CreateRequired(string blockName, string itemName, Type dataType = null)
        {
            var item = Create(blockName, itemName, dataType);
            item.Required = true;
            return item;
        }
        
        /// <summary>
        /// Create a read-only item
        /// </summary>
        public static ItemInfo CreateReadOnly(string blockName, string itemName, Type dataType = null)
        {
            var item = Create(blockName, itemName, dataType);
            item.Enabled = false;
            item.InsertAllowed = false;
            item.UpdateAllowed = false;
            return item;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Form mode enumeration - Oracle Forms compatible
    /// </summary>
    public enum FormMode
    {
        /// <summary>Normal data entry mode</summary>
        Normal = 0,
        
        /// <summary>Query criteria entry mode (Enter Query)</summary>
        Query = 1,
        
        /// <summary>New record mode (Insert)</summary>
        Insert = 2,
        
        /// <summary>Edit existing record mode (Update)</summary>
        Update = 3
    }
}
