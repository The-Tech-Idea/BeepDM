# Phase A1: System Variables Migration

## Overview
Migrate the Oracle Forms `:SYSTEM.*` variables from `BeepDataBlock` to `FormsManager` in BeepDM.

## Current State

### Source Files (Beep.Winform)
- `TheTechIdea.Beep.Winform.Controls.Integrated/DataBlocks/BeepDataBlock.SystemVariables.cs`
- `TheTechIdea.Beep.Winform.Controls.Integrated/DataBlocks/Models/SystemVariables.cs`

### System Variables to Migrate (30+ Properties)

#### Record Information
| Variable | Type | Description |
|----------|------|-------------|
| `CURSOR_RECORD` | int | Current record number (1-based) |
| `LAST_RECORD` | int | Total number of records |
| `FIRST_RECORD` | int | First record (always 1) |
| `IS_FIRST_RECORD` | bool | Whether on first record |
| `IS_LAST_RECORD` | bool | Whether on last record |

#### Block Status
| Variable | Type | Description |
|----------|------|-------------|
| `BLOCK_STATUS` | string | Normal, Query, Changed, New |
| `RECORD_STATUS` | string | Query, New, Changed, Insert |
| `RECORDS_DISPLAYED` | int | Records currently displayed |
| `QUERY_HITS` | int | Records returned by last query |

#### Mode Information
| Variable | Type | Description |
|----------|------|-------------|
| `MODE` | string | Current mode (CRUD, Query) |
| `QUERY_MODE` | bool | Whether in query mode |
| `NORMAL_MODE` | bool | Whether in CRUD mode |

#### Trigger Information
| Variable | Type | Description |
|----------|------|-------------|
| `TRIGGER_RECORD` | int | Record that triggered current trigger |
| `TRIGGER_BLOCK` | string | Block that triggered current trigger |
| `TRIGGER_ITEM` | string | Item that triggered current trigger |
| `TRIGGER_FIELD` | string | Field that triggered current trigger |

#### Form/Block Information
| Variable | Type | Description |
|----------|------|-------------|
| `CURRENT_FORM` | string | Current form name |
| `CURRENT_BLOCK` | string | Current block name |
| `CURRENT_ITEM` | string | Current item with focus |
| `CURRENT_VALUE` | object | Current value of current item |

#### Message Information
| Variable | Type | Description |
|----------|------|-------------|
| `MESSAGE_LEVEL` | string | Error, Warning, Info |
| `MESSAGE_CODE` | string | Message code |
| `MESSAGE_TEXT` | string | Message text |
| `MESSAGE_SEVERITY` | int | Severity (0-25) |

#### Coordination Information
| Variable | Type | Description |
|----------|------|-------------|
| `MASTER_BLOCK` | string | Master block name |
| `COORDINATION_OPERATION` | bool | Whether in coordination operation |
| `HAS_MASTER` | bool | Whether block has parent |
| `HAS_DETAILS` | bool | Whether block has children |

#### Transaction Information
| Variable | Type | Description |
|----------|------|-------------|
| `IS_DIRTY` | bool | Has uncommitted changes |
| `IN_TRANSACTION` | bool | Whether in transaction |
| `TRANSACTION_START` | DateTime? | Transaction start time |

#### Validation State
| Variable | Type | Description |
|----------|------|-------------|
| `HAS_ERRORS` | bool | Has validation errors |
| `HAS_WARNINGS` | bool | Has validation warnings |
| `ERROR_COUNT` | int | Number of errors |
| `WARNING_COUNT` | int | Number of warnings |

#### Navigation State
| Variable | Type | Description |
|----------|------|-------------|
| `LAST_NAVIGATION` | string | Last navigation direction |
| `IS_NAVIGATING` | bool | Whether currently navigating |

#### Timestamp Information
| Variable | Type | Description |
|----------|------|-------------|
| `BLOCK_LOADED_TIME` | DateTime? | When block was loaded |
| `RECORD_LOADED_TIME` | DateTime? | When record was loaded |
| `LAST_OPERATION_TIME` | DateTime? | When last operation occurred |

---

## Target Files (BeepDM)

### File 1: Interface
**Path**: `DataManagementModelsStandard/Editor/UOWManager/Interfaces/ISystemVariablesManager.cs`

```csharp
using System;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>
    /// Manages Oracle Forms :SYSTEM.* equivalent variables for blocks
    /// </summary>
    public interface ISystemVariablesManager
    {
        /// <summary>
        /// Gets the system variables for a specific block
        /// </summary>
        SystemVariables GetSystemVariables(string blockName);
        
        /// <summary>
        /// Updates system variables after an operation
        /// </summary>
        void UpdateSystemVariables(string blockName);
        
        /// <summary>
        /// Updates system variables for trigger execution
        /// </summary>
        void UpdateForTrigger(string blockName, string triggerType, string itemName = null);
        
        /// <summary>
        /// Sets the current form name
        /// </summary>
        void SetCurrentForm(string formName);
        
        /// <summary>
        /// Gets the current form name
        /// </summary>
        string GetCurrentForm();
        
        /// <summary>
        /// Sets a message in system variables
        /// </summary>
        void SetMessage(string blockName, string level, string code, string text, int severity = 0);
        
        /// <summary>
        /// Clears the current message
        /// </summary>
        void ClearMessage(string blockName);
    }
}
```

### File 2: Model Class
**Path**: `DataManagementModelsStandard/Editor/UOWManager/Models/SystemVariables.cs`

```csharp
using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// System variables equivalent to Oracle Forms :SYSTEM.* variables
    /// UI-agnostic model for runtime block/record/form state
    /// </summary>
    public class SystemVariables
    {
        #region Record Information
        
        /// <summary>Oracle Forms: :SYSTEM.CURSOR_RECORD - Current record number (1-based)</summary>
        public int CURSOR_RECORD { get; set; }
        
        /// <summary>Oracle Forms: :SYSTEM.LAST_RECORD - Total number of records</summary>
        public int LAST_RECORD { get; set; }
        
        /// <summary>First record in block (1-based)</summary>
        public int FIRST_RECORD => 1;
        
        /// <summary>Whether current record is the first record</summary>
        public bool IS_FIRST_RECORD => CURSOR_RECORD == FIRST_RECORD;
        
        /// <summary>Whether current record is the last record</summary>
        public bool IS_LAST_RECORD => CURSOR_RECORD == LAST_RECORD;
        
        #endregion
        
        #region Block Status
        
        /// <summary>Oracle Forms: :SYSTEM.BLOCK_STATUS - Block status</summary>
        public string BLOCK_STATUS { get; set; }
        
        /// <summary>Oracle Forms: :SYSTEM.RECORD_STATUS - Record status</summary>
        public string RECORD_STATUS { get; set; }
        
        /// <summary>Number of records displayed</summary>
        public int RECORDS_DISPLAYED { get; set; }
        
        /// <summary>Number of records from last query</summary>
        public int QUERY_HITS { get; set; }
        
        #endregion
        
        #region Mode Information
        
        /// <summary>Oracle Forms: :SYSTEM.MODE - Current mode</summary>
        public string MODE { get; set; }
        
        /// <summary>Whether in query mode</summary>
        public bool QUERY_MODE => MODE == "Query";
        
        /// <summary>Whether in CRUD mode</summary>
        public bool NORMAL_MODE => MODE == "CRUD" || MODE == "Normal";
        
        #endregion
        
        #region Trigger Information
        
        /// <summary>Oracle Forms: :SYSTEM.TRIGGER_RECORD</summary>
        public int TRIGGER_RECORD { get; set; }
        
        /// <summary>Oracle Forms: :SYSTEM.TRIGGER_BLOCK</summary>
        public string TRIGGER_BLOCK { get; set; }
        
        /// <summary>Oracle Forms: :SYSTEM.TRIGGER_ITEM</summary>
        public string TRIGGER_ITEM { get; set; }
        
        /// <summary>Oracle Forms: :SYSTEM.TRIGGER_FIELD</summary>
        public string TRIGGER_FIELD { get; set; }
        
        #endregion
        
        #region Form/Block Information
        
        /// <summary>Oracle Forms: :SYSTEM.CURRENT_FORM</summary>
        public string CURRENT_FORM { get; set; }
        
        /// <summary>Oracle Forms: :SYSTEM.CURRENT_BLOCK</summary>
        public string CURRENT_BLOCK { get; set; }
        
        /// <summary>Oracle Forms: :SYSTEM.CURRENT_ITEM</summary>
        public string CURRENT_ITEM { get; set; }
        
        /// <summary>Oracle Forms: :SYSTEM.CURRENT_VALUE</summary>
        public object CURRENT_VALUE { get; set; }
        
        #endregion
        
        #region Message Information
        
        /// <summary>Oracle Forms: :SYSTEM.MESSAGE_LEVEL</summary>
        public string MESSAGE_LEVEL { get; set; }
        
        /// <summary>Oracle Forms: :SYSTEM.MESSAGE_CODE</summary>
        public string MESSAGE_CODE { get; set; }
        
        /// <summary>Oracle Forms: :SYSTEM.MESSAGE_TEXT</summary>
        public string MESSAGE_TEXT { get; set; }
        
        /// <summary>Oracle Forms: :SYSTEM.MESSAGE_SEVERITY</summary>
        public int MESSAGE_SEVERITY { get; set; }
        
        #endregion
        
        #region Coordination Information
        
        /// <summary>Oracle Forms: :SYSTEM.MASTER_BLOCK</summary>
        public string MASTER_BLOCK { get; set; }
        
        /// <summary>Oracle Forms: :SYSTEM.COORDINATION_OPERATION</summary>
        public bool COORDINATION_OPERATION { get; set; }
        
        /// <summary>Whether block has a master</summary>
        public bool HAS_MASTER { get; set; }
        
        /// <summary>Whether block has details</summary>
        public bool HAS_DETAILS { get; set; }
        
        #endregion
        
        #region Transaction Information
        
        /// <summary>Has uncommitted changes</summary>
        public bool IS_DIRTY { get; set; }
        
        /// <summary>Whether in transaction</summary>
        public bool IN_TRANSACTION { get; set; }
        
        /// <summary>Transaction start time</summary>
        public DateTime? TRANSACTION_START { get; set; }
        
        #endregion
        
        #region Validation State
        
        /// <summary>Has validation errors</summary>
        public bool HAS_ERRORS { get; set; }
        
        /// <summary>Has validation warnings</summary>
        public bool HAS_WARNINGS { get; set; }
        
        /// <summary>Number of errors</summary>
        public int ERROR_COUNT { get; set; }
        
        /// <summary>Number of warnings</summary>
        public int WARNING_COUNT { get; set; }
        
        #endregion
        
        #region Navigation State
        
        /// <summary>Last navigation direction</summary>
        public string LAST_NAVIGATION { get; set; }
        
        /// <summary>Whether navigating</summary>
        public bool IS_NAVIGATING { get; set; }
        
        #endregion
        
        #region Timestamp Information
        
        /// <summary>When block was loaded</summary>
        public DateTime? BLOCK_LOADED_TIME { get; set; }
        
        /// <summary>When record was loaded</summary>
        public DateTime? RECORD_LOADED_TIME { get; set; }
        
        /// <summary>When last operation occurred</summary>
        public DateTime? LAST_OPERATION_TIME { get; set; }
        
        #endregion
    }
}
```

### File 3: Manager Implementation
**Path**: `DataManagementEngineStandard/Editor/Forms/Helpers/SystemVariablesManager.cs`

```csharp
using System;
using System.Collections.Concurrent;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Manages Oracle Forms :SYSTEM.* equivalent variables
    /// </summary>
    public class SystemVariablesManager : ISystemVariablesManager
    {
        #region Fields
        
        private readonly IDMEEditor _dmeEditor;
        private readonly ConcurrentDictionary<string, DataBlockInfo> _blocks;
        private readonly ConcurrentDictionary<string, SystemVariables> _systemVariables = new();
        private string _currentFormName;
        
        #endregion
        
        #region Constructor
        
        public SystemVariablesManager(
            IDMEEditor dmeEditor,
            ConcurrentDictionary<string, DataBlockInfo> blocks)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        }
        
        #endregion
        
        #region ISystemVariablesManager Implementation
        
        public SystemVariables GetSystemVariables(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                return null;
                
            return _systemVariables.GetOrAdd(blockName, _ => CreateSystemVariables(blockName));
        }
        
        public void UpdateSystemVariables(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                return;
                
            if (!_blocks.TryGetValue(blockName, out var blockInfo))
                return;
                
            var sysVars = GetSystemVariables(blockName);
            if (sysVars == null)
                return;
                
            // Update block information
            sysVars.CURRENT_BLOCK = blockName;
            sysVars.CURRENT_FORM = _currentFormName;
            
            // Update mode
            sysVars.MODE = blockInfo.Status.ToString();
            
            // Update record information from UnitOfWork
            if (blockInfo.UnitOfWork != null)
            {
                var units = blockInfo.UnitOfWork.Units;
                if (units != null)
                {
                    sysVars.CURSOR_RECORD = units.CurrentIndex + 1;  // 1-based
                    sysVars.LAST_RECORD = units.Count;
                    sysVars.RECORDS_DISPLAYED = units.Count;
                    sysVars.IS_DIRTY = blockInfo.UnitOfWork.IsDirty;
                }
            }
            
            // Update status
            sysVars.BLOCK_STATUS = blockInfo.IsDirty ? "Changed" : 
                                   (blockInfo.Status == BlockMode.Query ? "Query" : "Normal");
            
            // Update coordination
            sysVars.MASTER_BLOCK = blockInfo.MasterBlockName;
            sysVars.HAS_MASTER = !string.IsNullOrEmpty(blockInfo.MasterBlockName);
            sysVars.HAS_DETAILS = blockInfo.DetailBlockNames?.Count > 0;
            
            // Update timestamp
            sysVars.LAST_OPERATION_TIME = DateTime.Now;
        }
        
        public void UpdateForTrigger(string blockName, string triggerType, string itemName = null)
        {
            var sysVars = GetSystemVariables(blockName);
            if (sysVars == null)
                return;
                
            sysVars.TRIGGER_BLOCK = blockName;
            sysVars.TRIGGER_RECORD = sysVars.CURSOR_RECORD;
            
            if (!string.IsNullOrEmpty(itemName))
            {
                sysVars.TRIGGER_ITEM = itemName;
                sysVars.TRIGGER_FIELD = itemName;
            }
            
            UpdateSystemVariables(blockName);
        }
        
        public void SetCurrentForm(string formName)
        {
            _currentFormName = formName;
            
            // Update all blocks with new form name
            foreach (var blockName in _systemVariables.Keys)
            {
                var sysVars = _systemVariables[blockName];
                if (sysVars != null)
                {
                    sysVars.CURRENT_FORM = formName;
                }
            }
        }
        
        public string GetCurrentForm() => _currentFormName;
        
        public void SetMessage(string blockName, string level, string code, string text, int severity = 0)
        {
            var sysVars = GetSystemVariables(blockName);
            if (sysVars == null)
                return;
                
            sysVars.MESSAGE_LEVEL = level;
            sysVars.MESSAGE_CODE = code;
            sysVars.MESSAGE_TEXT = text;
            sysVars.MESSAGE_SEVERITY = severity;
        }
        
        public void ClearMessage(string blockName)
        {
            var sysVars = GetSystemVariables(blockName);
            if (sysVars == null)
                return;
                
            sysVars.MESSAGE_LEVEL = null;
            sysVars.MESSAGE_CODE = null;
            sysVars.MESSAGE_TEXT = null;
            sysVars.MESSAGE_SEVERITY = 0;
        }
        
        #endregion
        
        #region Private Methods
        
        private SystemVariables CreateSystemVariables(string blockName)
        {
            var sysVars = new SystemVariables
            {
                CURRENT_BLOCK = blockName,
                CURRENT_FORM = _currentFormName,
                MODE = "Normal",
                BLOCK_STATUS = "Normal",
                BLOCK_LOADED_TIME = DateTime.Now
            };
            
            return sysVars;
        }
        
        #endregion
    }
}
```

### File 4: FormsManager Partial
**Path**: `DataManagementEngineStandard/Editor/Forms/FormsManager.SystemVariables.cs`

```csharp
using System;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// FormsManager partial - System Variables support
    /// </summary>
    public partial class FormsManager
    {
        #region Fields
        
        private ISystemVariablesManager _systemVariablesManager;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// System variables manager
        /// </summary>
        public ISystemVariablesManager SystemVariablesManager => _systemVariablesManager;
        
        #endregion
        
        #region System Variables Methods
        
        /// <summary>
        /// Gets system variables for the current block
        /// </summary>
        public SystemVariables GetCurrentBlockSystemVariables()
        {
            return _systemVariablesManager?.GetSystemVariables(CurrentBlockName);
        }
        
        /// <summary>
        /// Gets system variables for a specific block
        /// </summary>
        public SystemVariables GetSystemVariables(string blockName)
        {
            return _systemVariablesManager?.GetSystemVariables(blockName);
        }
        
        /// <summary>
        /// Updates system variables after an operation
        /// </summary>
        public void UpdateSystemVariables(string blockName)
        {
            _systemVariablesManager?.UpdateSystemVariables(blockName);
        }
        
        /// <summary>
        /// Updates system variables for trigger execution
        /// </summary>
        public void UpdateSystemVariablesForTrigger(string blockName, string triggerType, string itemName = null)
        {
            _systemVariablesManager?.UpdateForTrigger(blockName, triggerType, itemName);
        }
        
        /// <summary>
        /// Sets a message in system variables (Oracle Forms MESSAGE built-in)
        /// </summary>
        public void SetMessage(string text, string level = "Info", int severity = 0)
        {
            _systemVariablesManager?.SetMessage(
                CurrentBlockName, 
                level, 
                null, 
                text, 
                severity);
        }
        
        /// <summary>
        /// Clears the current message
        /// </summary>
        public void ClearMessage()
        {
            _systemVariablesManager?.ClearMessage(CurrentBlockName);
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initializes the system variables manager
        /// Called from main FormsManager constructor
        /// </summary>
        private void InitializeSystemVariablesManager()
        {
            _systemVariablesManager = new SystemVariablesManager(_dmeEditor, _blocks);
        }
        
        #endregion
    }
}
```

---

## Modifications to Existing Files

### File 5: Update FormsManager.cs Constructor
**Path**: `DataManagementEngineStandard/Editor/Forms/FormsManager.cs`

**Add to constructor** (after existing helper initializations):
```csharp
// Initialize system variables manager
InitializeSystemVariablesManager();
```

**Add to helper manager interface property section**:
```csharp
/// <summary>System variables manager for Oracle Forms :SYSTEM.* emulation</summary>
public ISystemVariablesManager SystemVariablesManager { get; }
```

---

## BeepDataBlock Refactoring (Beep.Winform)

After migration, update `BeepDataBlock.SystemVariables.cs` to delegate to FormsManager:

### Updated BeepDataBlock.SystemVariables.cs

```csharp
using System;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Winform.Controls
{
    /// <summary>
    /// BeepDataBlock partial - System Variables (thin UI wrapper)
    /// Delegates to FormsManager for actual system variable management
    /// </summary>
    public partial class BeepDataBlock
    {
        #region System Variables Access
        
        /// <summary>
        /// Oracle Forms :SYSTEM.* equivalent
        /// Delegates to FormsManager for state management
        /// </summary>
        public SystemVariables SYSTEM
        {
            get
            {
                // Get from FormsManager
                return _formsManager?.GetSystemVariables(Name);
            }
        }
        
        #endregion
        
        #region UI-Specific Updates
        
        /// <summary>
        /// Update system variables for UI-specific changes (focus, current item)
        /// Delegates business logic to FormsManager, keeps UI state local
        /// </summary>
        public void UpdateSystemVariablesForUI()
        {
            var sysVars = SYSTEM;
            if (sysVars == null)
                return;
                
            // UI-specific updates only
            sysVars.CURRENT_ITEM = CurrentItemName;
            sysVars.CURRENT_VALUE = GetCurrentItemValue();
        }
        
        #endregion
        
        #region Property Helpers (unchanged)
        
        public int CurrentRecord => SYSTEM?.CURSOR_RECORD ?? 0;
        public int RecordsDisplayed => SYSTEM?.RECORDS_DISPLAYED ?? 0;
        public int QueryHits => SYSTEM?.QUERY_HITS ?? 0;
        
        #endregion
    }
}
```

---

## Verification Steps

1. **Build BeepDM** - Verify no compile errors
2. **Check interface** - Ensure `ISystemVariablesManager` is accessible from BeepDM reference
3. **Update BeepDataBlock** - Reference new `SystemVariables` model class
4. **Test system variables** - Verify variables update correctly during operations

---

## Dependencies

- **None** - This phase has no dependencies on other phases
- **Can be implemented independently**

---

## Files Summary

| File | Action | Location |
|------|--------|----------|
| `ISystemVariablesManager.cs` | CREATE | `DataManagementModelsStandard/Editor/UOWManager/Interfaces/` |
| `SystemVariables.cs` | CREATE | `DataManagementModelsStandard/Editor/UOWManager/Models/` |
| `SystemVariablesManager.cs` | CREATE | `DataManagementEngineStandard/Editor/Forms/Helpers/` |
| `FormsManager.SystemVariables.cs` | CREATE | `DataManagementEngineStandard/Editor/Forms/` |
| `FormsManager.cs` | MODIFY | Add initialization call |
| `BeepDataBlock.SystemVariables.cs` | MODIFY | Thin wrapper delegation (Beep.Winform) |
