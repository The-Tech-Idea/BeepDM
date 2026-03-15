# FormsManager Migration - Phase A4: Item Properties Manager

## Overview

**Goal**: Move Item Properties logic from `BeepDataBlock.Properties.cs` to a new `ItemPropertyManager` in FormsManager.

**Current Location**: `Beep.Winform/TheTechIdea.Beep.Winform.Controls.Integrated/DataBlocks/BeepDataBlock.Properties.cs`  
**Target Location**: `BeepDM/DataManagementEngineStandard/Editor/Forms/Helpers/ItemPropertyManager.cs`

---

## Source Analysis

### Current BeepDataBlock Item Properties (from BeepDataBlock.Properties.cs)

**Oracle Forms Properties (BeepDataBlockItem.cs)**:
| Property | Type | Description | Oracle Equivalent |
|----------|------|-------------|-------------------|
| `Required` | bool | Field mandatory | REQUIRED |
| `Enabled` | bool | Can be edited | ENABLED |
| `Visible` | bool | Is rendered | VISIBLE |
| `QueryAllowed` | bool | Used in query mode | QUERY_ALLOWED |
| `InsertAllowed` | bool | Can modify on insert | INSERT_ALLOWED |
| `UpdateAllowed` | bool | Can modify on update | UPDATE_ALLOWED |
| `DefaultValue` | object | Default for new records | DEFAULT_VALUE |
| `PromptText` | string | Label text | PROMPT_TEXT |
| `HintText` | string | Tooltip | HINT_TEXT |
| `LOVName` | string | Attached LOV | LOV_NAME |
| `MaxLength` | int | Max text length | MAX_LENGTH |
| `FormatMask` | string | Display format | FORMAT_MASK |
| `ValidationFormula` | string | Validation expression | - |

**Item State Properties**:
| Property | Type | Description |
|----------|------|-------------|
| `IsDirty` | bool | Value changed |
| `OldValue` | object | Previous value |
| `CurrentValue` | object | Current value |
| `HasFocus` | bool | Currently focused |
| `HasError` | bool | Validation error |
| `ErrorMessage` | string | Error description |

**Navigation Properties**:
| Property | Type | Description |
|----------|------|-------------|
| `TabIndex` | int | Tab order |
| `NextNavigationItem` | string | Next item name |
| `PreviousNavigationItem` | string | Previous item name |

---

## Files to Create in BeepDM

### 1. `Models/ItemInfo.cs`
```csharp
namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Represents an item/field definition with Oracle Forms-compatible properties
    /// UI-agnostic version of BeepDataBlockItem
    /// </summary>
    public class ItemInfo
    {
        // Identification
        public string ItemName { get; set; }
        public string BlockName { get; set; }
        public string BoundProperty { get; set; }  // Database column name
        
        // Oracle Forms Item Properties
        public bool Required { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Visible { get; set; } = true;
        public bool QueryAllowed { get; set; } = true;
        public bool InsertAllowed { get; set; } = true;
        public bool UpdateAllowed { get; set; } = true;
        public object DefaultValue { get; set; }
        public string PromptText { get; set; }
        public string HintText { get; set; }
        public string LOVName { get; set; }
        public int MaxLength { get; set; }
        public string FormatMask { get; set; }
        public string ValidationFormula { get; set; }
        
        // Item State (tracked by engine)
        public bool IsDirty { get; set; }
        public object OldValue { get; set; }
        public object CurrentValue { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }
        
        // Navigation
        public int TabIndex { get; set; }
        public string NextNavigationItem { get; set; }
        public string PreviousNavigationItem { get; set; }
        
        // Data Type
        public Type DataType { get; set; }
        public string DatabaseTypeName { get; set; }
    }
}
```

### 2. `Interfaces/IItemPropertyManager.cs`
```csharp
namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>
    /// Interface for managing item/field properties in a block
    /// Oracle Forms equivalent: SET_ITEM_PROPERTY, GET_ITEM_PROPERTY built-ins
    /// </summary>
    public interface IItemPropertyManager
    {
        // Item Registration
        void RegisterItem(string blockName, string itemName, ItemInfo info);
        void RegisterItemsFromEntityStructure(string blockName, EntityStructure structure);
        void UnregisterItem(string blockName, string itemName);
        
        // Item Retrieval
        ItemInfo GetItem(string blockName, string itemName);
        IEnumerable<ItemInfo> GetAllItems(string blockName);
        bool ItemExists(string blockName, string itemName);
        
        // Oracle Forms Built-ins: SET_ITEM_PROPERTY
        void SetItemProperty(string blockName, string itemName, string propertyName, object value);
        void SetItemEnabled(string blockName, string itemName, bool enabled);
        void SetItemVisible(string blockName, string itemName, bool visible);
        void SetItemRequired(string blockName, string itemName, bool required);
        void SetItemQueryAllowed(string blockName, string itemName, bool allowed);
        void SetItemInsertAllowed(string blockName, string itemName, bool allowed);
        void SetItemUpdateAllowed(string blockName, string itemName, bool allowed);
        void SetItemDefaultValue(string blockName, string itemName, object value);
        void SetItemLOV(string blockName, string itemName, string lovName);
        void SetItemFormatMask(string blockName, string itemName, string formatMask);
        
        // Oracle Forms Built-ins: GET_ITEM_PROPERTY
        object GetItemProperty(string blockName, string itemName, string propertyName);
        bool GetItemEnabled(string blockName, string itemName);
        bool GetItemVisible(string blockName, string itemName);
        bool GetItemRequired(string blockName, string itemName);
        object GetItemDefaultValue(string blockName, string itemName);
        
        // Value Management
        void SetItemValue(string blockName, string itemName, object value);
        object GetItemValue(string blockName, string itemName);
        void ApplyDefaultValues(string blockName, object record);
        void ClearItemValues(string blockName);
        
        // State Management
        void MarkItemDirty(string blockName, string itemName, object oldValue);
        void ClearItemDirty(string blockName, string itemName);
        bool IsItemDirty(string blockName, string itemName);
        IEnumerable<string> GetDirtyItems(string blockName);
        
        // Error State
        void SetItemError(string blockName, string itemName, string errorMessage);
        void ClearItemError(string blockName, string itemName);
        void ClearAllItemErrors(string blockName);
        
        // Navigation Order
        void SetTabOrder(string blockName, IEnumerable<string> itemOrder);
        IEnumerable<string> GetTabOrder(string blockName);
        string GetNextItem(string blockName, string currentItem);
        string GetPreviousItem(string blockName, string currentItem);
        
        // Events
        event EventHandler<ItemPropertyChangedEventArgs> ItemPropertyChanged;
        event EventHandler<ItemValueChangedEventArgs> ItemValueChanged;
        event EventHandler<ItemErrorEventArgs> ItemErrorChanged;
    }
}
```

### 3. `Events/ItemPropertyEventArgs.cs`
```csharp
namespace TheTechIdea.Beep.Editor.UOWManager.Events
{
    public class ItemPropertyChangedEventArgs : EventArgs
    {
        public string BlockName { get; set; }
        public string ItemName { get; set; }
        public string PropertyName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }
    
    public class ItemValueChangedEventArgs : EventArgs
    {
        public string BlockName { get; set; }
        public string ItemName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public bool IsDirty { get; set; }
    }
    
    public class ItemErrorEventArgs : EventArgs
    {
        public string BlockName { get; set; }
        public string ItemName { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }
    }
}
```

### 4. `Helpers/ItemPropertyManager.cs` (Main Implementation)
```csharp
namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Manages item properties for all blocks in FormsManager
    /// UI-agnostic implementation of Oracle Forms item property system
    /// </summary>
    public class ItemPropertyManager : IItemPropertyManager
    {
        // Block -> ItemName -> ItemInfo
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ItemInfo>> _blockItems;
        private readonly IDMEEditor _dmeEditor;
        
        // Events
        public event EventHandler<ItemPropertyChangedEventArgs> ItemPropertyChanged;
        public event EventHandler<ItemValueChangedEventArgs> ItemValueChanged;
        public event EventHandler<ItemErrorEventArgs> ItemErrorChanged;
        
        public ItemPropertyManager(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor;
            _blockItems = new ConcurrentDictionary<string, ConcurrentDictionary<string, ItemInfo>>(
                StringComparer.OrdinalIgnoreCase);
        }
        
        // Implementation methods...
    }
}
```

---

## File-by-File Implementation

### Step 1: Create ItemInfo Model

**File**: `DataManagementEngineStandard/Editor/Forms/Models/ItemInfo.cs`

```csharp
using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Represents an item/field definition with Oracle Forms-compatible properties
    /// UI-agnostic version of BeepDataBlockItem
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
        /// Oracle Forms: FORMAT_MASK - Display format
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
        /// Database type name (e.g., "VARCHAR2", "NUMBER")
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
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Check if item is editable based on current mode
        /// </summary>
        public bool IsEditable(FormMode mode)
        {
            if (!Enabled) return false;
            
            return mode switch
            {
                FormMode.Query => QueryAllowed,
                FormMode.Insert => InsertAllowed,
                FormMode.Update => UpdateAllowed,
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
        /// Clone item definition (without state)
        /// </summary>
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
                Scale = Scale
            };
        }
        
        #endregion
    }
    
    /// <summary>
    /// Form mode enumeration
    /// </summary>
    public enum FormMode
    {
        /// <summary>Normal data entry mode</summary>
        Normal = 0,
        /// <summary>Query criteria entry mode</summary>
        Query = 1,
        /// <summary>New record mode</summary>
        Insert = 2,
        /// <summary>Edit existing record mode</summary>
        Update = 3
    }
}
```

### Step 2: Create Event Args

**File**: `DataManagementEngineStandard/Editor/Forms/Events/ItemPropertyEventArgs.cs`

```csharp
using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Events
{
    /// <summary>
    /// Event args for item property changes
    /// </summary>
    public class ItemPropertyChangedEventArgs : EventArgs
    {
        public string BlockName { get; set; }
        public string ItemName { get; set; }
        public string PropertyName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// Event args for item value changes
    /// </summary>
    public class ItemValueChangedEventArgs : EventArgs
    {
        public string BlockName { get; set; }
        public string ItemName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public bool IsDirty { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// Event args for item error state changes
    /// </summary>
    public class ItemErrorEventArgs : EventArgs
    {
        public string BlockName { get; set; }
        public string ItemName { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
```

### Step 3: Create Interface

**File**: `DataManagementModelsStandard/Editor/UOWManager/Interfaces/IItemPropertyManager.cs`

```csharp
using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Editor.UOWManager.Events;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>
    /// Interface for managing item/field properties in data blocks
    /// Oracle Forms equivalent: SET_ITEM_PROPERTY, GET_ITEM_PROPERTY built-ins
    /// </summary>
    public interface IItemPropertyManager
    {
        #region Item Registration
        
        void RegisterItem(string blockName, string itemName, ItemInfo info);
        void RegisterItemsFromEntityStructure(string blockName, EntityStructure structure);
        void UnregisterItem(string blockName, string itemName);
        void ClearBlockItems(string blockName);
        
        #endregion
        
        #region Item Retrieval
        
        ItemInfo GetItem(string blockName, string itemName);
        IEnumerable<ItemInfo> GetAllItems(string blockName);
        bool ItemExists(string blockName, string itemName);
        int GetItemCount(string blockName);
        
        #endregion
        
        #region SET_ITEM_PROPERTY Built-in
        
        void SetItemProperty(string blockName, string itemName, string propertyName, object value);
        void SetItemEnabled(string blockName, string itemName, bool enabled);
        void SetItemVisible(string blockName, string itemName, bool visible);
        void SetItemRequired(string blockName, string itemName, bool required);
        void SetItemQueryAllowed(string blockName, string itemName, bool allowed);
        void SetItemInsertAllowed(string blockName, string itemName, bool allowed);
        void SetItemUpdateAllowed(string blockName, string itemName, bool allowed);
        void SetItemDefaultValue(string blockName, string itemName, object value);
        void SetItemLOV(string blockName, string itemName, string lovName);
        void SetItemFormatMask(string blockName, string itemName, string formatMask);
        void SetItemPromptText(string blockName, string itemName, string text);
        void SetItemHintText(string blockName, string itemName, string text);
        
        #endregion
        
        #region GET_ITEM_PROPERTY Built-in
        
        object GetItemProperty(string blockName, string itemName, string propertyName);
        bool IsItemEnabled(string blockName, string itemName);
        bool IsItemVisible(string blockName, string itemName);
        bool IsItemRequired(string blockName, string itemName);
        bool IsItemQueryAllowed(string blockName, string itemName);
        bool IsItemInsertAllowed(string blockName, string itemName);
        bool IsItemUpdateAllowed(string blockName, string itemName);
        object GetItemDefaultValue(string blockName, string itemName);
        string GetItemLOV(string blockName, string itemName);
        string GetItemFormatMask(string blockName, string itemName);
        
        #endregion
        
        #region Value Management
        
        void SetItemValue(string blockName, string itemName, object value);
        object GetItemValue(string blockName, string itemName);
        void ApplyDefaultValues(string blockName, object record);
        void ClearItemValues(string blockName);
        Dictionary<string, object> GetAllItemValues(string blockName);
        void SetAllItemValues(string blockName, Dictionary<string, object> values);
        
        #endregion
        
        #region State Management
        
        void MarkItemDirty(string blockName, string itemName, object oldValue);
        void ClearItemDirty(string blockName, string itemName);
        bool IsItemDirty(string blockName, string itemName);
        IEnumerable<string> GetDirtyItems(string blockName);
        void ClearAllDirtyFlags(string blockName);
        
        #endregion
        
        #region Error State
        
        void SetItemError(string blockName, string itemName, string errorMessage);
        void ClearItemError(string blockName, string itemName);
        void ClearAllItemErrors(string blockName);
        bool HasItemError(string blockName, string itemName);
        string GetItemErrorMessage(string blockName, string itemName);
        IEnumerable<ItemInfo> GetItemsWithErrors(string blockName);
        
        #endregion
        
        #region Navigation Order
        
        void SetTabOrder(string blockName, IEnumerable<string> itemOrder);
        IEnumerable<string> GetTabOrder(string blockName);
        string GetNextItem(string blockName, string currentItem);
        string GetPreviousItem(string blockName, string currentItem);
        string GetFirstItem(string blockName);
        string GetLastItem(string blockName);
        
        #endregion
        
        #region Events
        
        event EventHandler<ItemPropertyChangedEventArgs> ItemPropertyChanged;
        event EventHandler<ItemValueChangedEventArgs> ItemValueChanged;
        event EventHandler<ItemErrorEventArgs> ItemErrorChanged;
        
        #endregion
    }
}
```

### Step 4: Create Implementation

**File**: `DataManagementEngineStandard/Editor/Forms/Helpers/ItemPropertyManager.cs`

Full implementation with all methods.

---

## Step 5: Wire Up to FormsManager

### Modification: `FormsManager.cs`

```csharp
// Add field
private readonly IItemPropertyManager _itemPropertyManager;

// Update constructor
public FormsManager(
    IDMEEditor dmeEditor,
    IRelationshipManager relationshipManager,
    IDirtyStateManager dirtyStateManager,
    IEventManager eventManager,
    IFormsSimulationHelper formsSimulationHelper,
    IPerformanceManager performanceManager,
    IConfigurationManager configurationManager,
    IItemPropertyManager itemPropertyManager)  // NEW
{
    // ... existing code ...
    _itemPropertyManager = itemPropertyManager ?? throw new ArgumentNullException(nameof(itemPropertyManager));
}

// Add property
public IItemPropertyManager ItemProperties => _itemPropertyManager;
```

---

## Step 6: Update BeepDataBlock (UI Layer)

### Modification: `BeepDataBlock.Properties.cs`

Replace business logic with delegation:

```csharp
public partial class BeepDataBlock
{
    // Reference to FormsManager's ItemPropertyManager
    private IItemPropertyManager _itemPropertyManager;
    
    /// <summary>
    /// Initialize item properties from FormsManager (called during setup)
    /// </summary>
    public void InitializeItemPropertyManager(IItemPropertyManager manager)
    {
        _itemPropertyManager = manager;
    }
    
    // Delegate to FormsManager
    public void SetItemProperty(string itemName, string propertyName, object value)
    {
        _itemPropertyManager?.SetItemProperty(Name, itemName, propertyName, value);
        
        // Apply visual changes only
        if (_items.TryGetValue(itemName, out var item))
        {
            ApplyItemPropertyToControl(item, propertyName);
        }
    }
    
    public object GetItemProperty(string itemName, string propertyName)
    {
        return _itemPropertyManager?.GetItemProperty(Name, itemName, propertyName);
    }
    
    // UI-ONLY: Apply property to control
    private void ApplyItemPropertyToControl(BeepDataBlockItem item, string propertyName)
    {
        if (item?.Component is Control control)
        {
            switch (propertyName.ToUpperInvariant())
            {
                case "ENABLED":
                    control.Enabled = item.Enabled;
                    break;
                case "VISIBLE":
                    control.Visible = item.Visible;
                    break;
                // ... other visual properties
            }
        }
    }
}
```

---

## Migration Checklist

### Phase A4 Tasks:

- [ ] **A4.1**: Create `ItemInfo.cs` model in BeepDM
- [ ] **A4.2**: Create `ItemPropertyEventArgs.cs` event args in BeepDM
- [ ] **A4.3**: Create `IItemPropertyManager.cs` interface in DataManagementModelsStandard
- [ ] **A4.4**: Create `ItemPropertyManager.cs` implementation in BeepDM
- [ ] **A4.5**: Wire up to `FormsManager.cs`
- [ ] **A4.6**: Update `BeepDataBlock.Properties.cs` to delegate
- [ ] **A4.7**: Build and test

---

## Dependencies

- **Requires**: Phase A1 (SystemVariables) — FormMode enum may be shared
- **Requires**: Phase A2 (Validation) — Item errors may be set by validation
- **Required by**: Phase A5 (Triggers) — Triggers use item properties

---

## Estimated Effort

| Task | Files | Lines | Time |
|------|-------|-------|------|
| A4.1-A4.2 Models | 2 | ~150 | 30 min |
| A4.3 Interface | 1 | ~100 | 20 min |
| A4.4 Implementation | 1 | ~500 | 90 min |
| A4.5-A4.6 Wire-up | 2 | ~100 | 30 min |
| Testing | - | - | 30 min |
| **Total** | **6** | **~850** | **~3.5 hrs** |

---

## Testing Strategy

1. **Unit Tests**:
   - `RegisterItem()` / `UnregisterItem()`
   - `SetItemProperty()` / `GetItemProperty()` for all properties
   - Navigation order (Next/Previous item)
   - Dirty state tracking
   - Error state management

2. **Integration Tests**:
   - Create block → Register items → Set properties → Apply defaults
   - BeepDataBlock uses ItemPropertyManager for all property operations
