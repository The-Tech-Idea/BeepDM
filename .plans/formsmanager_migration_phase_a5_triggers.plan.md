# FormsManager Migration - Phase A5: Enhanced Trigger System

## Overview

**Goal**: Enhance FormsManager's `EventManager` with async trigger support from BeepDataBlock, creating a unified trigger system.

**Current Locations**:
- FormsManager Events: `BeepDM/DataManagementEngineStandard/Editor/Forms/Helpers/EventManager.cs`
- BeepDataBlock Triggers: `Beep.Winform/TheTechIdea.Beep.Winform.Controls.Integrated/DataBlocks/BeepDataBlock.Triggers.cs`

**Target**: Enhanced `TriggerManager` replacing both systems with unified async trigger architecture.

---

## Source Analysis

### Current FormsManager EventManager (~20 events)

| Event | Type | When Fired |
|-------|------|------------|
| `OnBlockEnter` | `EventHandler<BlockEventArgs>` | Navigation into block |
| `OnBlockLeave` | `EventHandler<BlockEventArgs>` | Navigation out of block |
| `OnBlockClear` | `EventHandler<BlockEventArgs>` | Block data cleared |
| `OnBlockValidate` | `EventHandler<BlockEventArgs>` | Block validation |
| `OnRecordEnter` | `EventHandler<RecordEventArgs>` | Record receives focus |
| `OnRecordLeave` | `EventHandler<RecordEventArgs>` | Record loses focus |
| `OnRecordValidate` | `EventHandler<RecordEventArgs>` | Record validation |
| `OnPreQuery` | `EventHandler<QueryEventArgs>` | Before query execution |
| `OnPostQuery` | `EventHandler<QueryEventArgs>` | After query execution |
| `OnPreInsert` | `EventHandler<DMLEventArgs>` | Before insert |
| `OnPostInsert` | `EventHandler<DMLEventArgs>` | After insert |
| `OnPreUpdate` | `EventHandler<DMLEventArgs>` | Before update |
| `OnPostUpdate` | `EventHandler<DMLEventArgs>` | After update |
| `OnPreDelete` | `EventHandler<DMLEventArgs>` | Before delete |
| `OnPostDelete` | `EventHandler<DMLEventArgs>` | After delete |
| `OnPreCommit` | `EventHandler<CommitEventArgs>` | Before commit |
| `OnPostCommit` | `EventHandler<CommitEventArgs>` | After commit |
| `OnValidateField` | `EventHandler<FieldEventArgs>` | Field validation |
| `OnValidateRecord` | `EventHandler<RecordEventArgs>` | Record validation |
| `OnValidateForm` | `EventHandler<FormEventArgs>` | Form validation |
| `OnError` | `EventHandler<ErrorEventArgs>` | Error occurred |

**Limitations**:
- Standard C# events (no cancellation)
- No async support  
- No execution order control
- No named triggers
- No trigger statistics

### Current BeepDataBlock Trigger System (50+ types)

**Trigger Types by Category**:

| Category | Count | Trigger Types |
|----------|-------|---------------|
| Form-Level | 6 | `WhenNewFormInstance`, `PreForm`, `PostForm`, `WhenFormNavigate`, `PreFormCommit`, `PostFormCommit` |
| Block-Level | 10 | `WhenNewBlockInstance`, `PreBlock`, `PostBlock`, `WhenClearBlock`, `WhenCreateRecord`, `WhenRemoveRecord`, `PreBlockCommit`, `PostBlockCommit`, `WhenBlockNavigate`, `OnPopulateDetails` |
| Record-Level | 15 | `WhenNewRecordInstance`, `PreInsert`, `PostInsert`, `PreUpdate`, `PostUpdate`, `PreDelete`, `PostDelete`, `PreQuery`, `PostQuery`, `WhenValidateRecord`, `OnLock`, `OnCheckDeleteMaster`, `OnClearDetails`, `OnCountQuery`, `OnFetchRecords` |
| Item-Level | 12 | `WhenNewItemInstance`, `WhenValidateItem`, `PreTextItem`, `PostTextItem`, `WhenListChanged`, `KeyNextItem`, `KeyPrevItem`, `WhenItemFocus`, `WhenItemBlur`, `OnItemClick`, `OnItemDoubleClick`, `OnItemChange` |
| Navigation | 4 | `PreRecordNavigate`, `PostRecordNavigate`, `PreBlockNavigate`, `PostBlockNavigate` |
| Error/Message | 3 | `OnError`, `OnMessage`, `OnDatabaseError` |
| Additional | 5+ | `PreBlockRollback`, `PostBlockRollback`, `PreDuplicateRecord`, `PostDuplicateRecord`, etc. |

**Trigger Features**:
- `Func<TriggerContext, Task<bool>>` handlers (async with cancellation)
- Execution order control
- Named trigger registration
- Statistics tracking (ExecutionCount, AverageExecutionMs, ErrorCount)
- ON-ERROR trigger escalation

---

## Migration Strategy

### Approach: **Hybrid Enhancement**

1. **Keep EventManager** for backward compatibility (existing subscribers)
2. **Add TriggerManager** with advanced features
3. **Bridge events to triggers**: EventManager fires events → TriggerManager executes triggers
4. **UI-specific triggers** remain in BeepDataBlock (KEY_*, mouse events)

---

## Files to Create in BeepDM

### 1. `Models/TriggerInfo.cs` (Enhanced Trigger Definition)

```csharp
namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Represents a registered trigger with Oracle Forms-compatible semantics
    /// UI-agnostic version of BeepDataBlockTrigger
    /// </summary>
    public class TriggerInfo
    {
        // Identification
        public string TriggerName { get; set; }
        public TriggerType TriggerType { get; set; }
        public string BlockName { get; set; }  // null = form-level
        public string ItemName { get; set; }   // null = block/form-level
        
        // Handler
        public Func<TriggerContext, Task<TriggerResult>> Handler { get; set; }
        
        // Execution Control
        public int ExecutionOrder { get; set; }  // Lower = executes first
        public bool IsEnabled { get; set; } = true;
        public TriggerTiming Timing { get; set; }
        public TriggerScope Scope { get; set; }
        
        // Metadata
        public string Description { get; set; }
        public DateTime RegisteredDate { get; set; }
        public string RegisteredBy { get; set; }
        
        // Statistics
        public int ExecutionCount { get; set; }
        public DateTime? LastExecutionTime { get; set; }
        public double AverageExecutionMs { get; set; }
        public int CancellationCount { get; set; }
        public int ErrorCount { get; set; }
    }
}
```

### 2. `Models/TriggerContext.cs` (Shared Context)

```csharp
namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Context passed to trigger handlers - UI-agnostic
    /// </summary>
    public class TriggerContext
    {
        // Form/Block/Item Identification
        public string FormName { get; set; }
        public string BlockName { get; set; }
        public string ItemName { get; set; }
        public string FieldName { get; set; }
        
        // Trigger Information
        public TriggerType TriggerType { get; set; }
        public string TriggerName { get; set; }
        public DateTime TriggerTime { get; set; } = DateTime.Now;
        
        // Values
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public object CurrentRecord { get; set; }
        public Dictionary<string, object> RecordValues { get; set; }
        
        // Control Flow
        public bool Cancel { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> InfoMessages { get; set; } = new();
        
        // Data Passing
        public Dictionary<string, object> Parameters { get; set; } = new();
        public Dictionary<string, object> ContextData { get; set; } = new();
        
        // System Variables Access
        public ISystemVariablesManager SystemVariables { get; set; }
        
        // Helper Methods
        public void AddWarning(string msg) { Warnings.Add(msg); }
        public void SetError(string msg) { ErrorMessage = msg; Cancel = true; }
    }
}
```

### 3. `Models/TriggerResult.cs`

```csharp
namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Result returned from trigger execution
    /// </summary>
    public class TriggerResult
    {
        public bool Success { get; set; } = true;
        public bool Cancel { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> Warnings { get; set; } = new();
        public double ExecutionTimeMs { get; set; }
        
        // Factory methods
        public static TriggerResult OK() => new() { Success = true };
        public static TriggerResult Cancelled(string msg = null) => new() { Success = false, Cancel = true, ErrorMessage = msg };
        public static TriggerResult Error(string msg) => new() { Success = false, ErrorMessage = msg };
    }
}
```

### 4. `Enums/TriggerType.cs` (Complete Enum)

```csharp
namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// Oracle Forms-compatible trigger types
    /// </summary>
    public enum TriggerType
    {
        // Form-Level (1-99)
        WhenNewFormInstance = 1,
        PreForm = 2,
        PostForm = 3,
        WhenFormNavigate = 4,
        PreFormCommit = 5,
        PostFormCommit = 6,
        
        // Block-Level (100-199)
        WhenNewBlockInstance = 100,
        PreBlock = 101,
        PostBlock = 102,
        WhenClearBlock = 103,
        WhenCreateRecord = 104,
        WhenRemoveRecord = 105,
        PreBlockCommit = 106,
        PostBlockCommit = 107,
        WhenBlockNavigate = 108,
        OnPopulateDetails = 109,
        
        // Record-Level (200-299)
        WhenNewRecordInstance = 200,
        PreInsert = 201,
        PostInsert = 202,
        PreUpdate = 203,
        PostUpdate = 204,
        PreDelete = 205,
        PostDelete = 206,
        PreQuery = 207,
        PostQuery = 208,
        WhenValidateRecord = 209,
        OnLock = 210,
        OnCheckDeleteMaster = 211,
        OnClearDetails = 212,
        OnCountQuery = 213,
        OnFetchRecords = 214,
        
        // Item-Level (300-399)
        WhenNewItemInstance = 300,
        WhenValidateItem = 301,
        PreTextItem = 302,
        PostTextItem = 303,
        WhenListChanged = 304,
        // NOTE: KEY-* triggers remain UI-only
        
        // Navigation (400-499)
        PreRecordNavigate = 400,
        PostRecordNavigate = 401,
        PreBlockNavigate = 402,
        PostBlockNavigate = 403,
        
        // Error/Message (500-599)
        OnError = 500,
        OnMessage = 501,
        OnDatabaseError = 502,
        
        // Additional (600+)
        PreBlockRollback = 600,
        PostBlockRollback = 601,
        PreDuplicateRecord = 602,
        PostDuplicateRecord = 603
    }
    
    public enum TriggerTiming
    {
        Before,   // Pre-* triggers
        After,    // Post-* triggers
        On,       // On-* triggers
        When      // When-* triggers
    }
    
    public enum TriggerScope
    {
        Form,
        Block,
        Record,
        Item,
        Navigation,
        System
    }
}
```

### 5. `Interfaces/ITriggerManager.cs`

```csharp
namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>
    /// Interface for advanced trigger management
    /// </summary>
    public interface ITriggerManager
    {
        #region Trigger Registration
        
        /// <summary>Register a trigger with automatic naming</summary>
        void RegisterTrigger(TriggerType type, Func<TriggerContext, Task<TriggerResult>> handler, 
            int executionOrder = 0, string blockName = null, string itemName = null);
        
        /// <summary>Register a named trigger</summary>
        void RegisterTrigger(string triggerName, TriggerType type, 
            Func<TriggerContext, Task<TriggerResult>> handler,
            string description = null, string blockName = null, string itemName = null);
        
        /// <summary>Unregister a named trigger</summary>
        bool UnregisterTrigger(string triggerName);
        
        /// <summary>Unregister all triggers of a type</summary>
        void UnregisterTriggers(TriggerType type, string blockName = null);
        
        /// <summary>Clear all triggers</summary>
        void ClearAllTriggers();
        
        #endregion
        
        #region Trigger Retrieval
        
        TriggerInfo GetTrigger(string triggerName);
        IEnumerable<TriggerInfo> GetTriggers(TriggerType type, string blockName = null);
        IEnumerable<TriggerInfo> GetAllTriggers();
        bool HasTriggers(TriggerType type, string blockName = null);
        
        #endregion
        
        #region Trigger Execution
        
        /// <summary>Execute all triggers of a type</summary>
        Task<TriggerResult> ExecuteTriggersAsync(TriggerType type, TriggerContext context, 
            CancellationToken cancellationToken = default);
        
        /// <summary>Execute a specific named trigger</summary>
        Task<TriggerResult> ExecuteTriggerAsync(string triggerName, TriggerContext context,
            CancellationToken cancellationToken = default);
        
        #endregion
        
        #region Trigger Control
        
        void EnableTrigger(string triggerName);
        void DisableTrigger(string triggerName);
        void SuppressTriggers(TriggerType type);
        void UnsuppressTriggers(TriggerType type);
        void SuppressAllTriggers();
        void UnsuppressAllTriggers();
        bool AreTriggersSuppressed { get; }
        
        #endregion
        
        #region Statistics
        
        TriggerStatistics GetTriggerStatistics(string triggerName);
        IEnumerable<TriggerStatistics> GetAllStatistics();
        void ResetStatistics();
        
        #endregion
        
        #region Events
        
        event EventHandler<TriggerExecutingEventArgs> TriggerExecuting;
        event EventHandler<TriggerExecutedEventArgs> TriggerExecuted;
        event EventHandler<TriggerErrorEventArgs> TriggerError;
        
        #endregion
    }
}
```

### 6. `Helpers/TriggerManager.cs` (Main Implementation)

```csharp
namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Advanced trigger management with async support and statistics
    /// </summary>
    public class TriggerManager : ITriggerManager
    {
        private readonly ConcurrentDictionary<string, TriggerInfo> _namedTriggers;
        private readonly ConcurrentDictionary<TriggerType, List<TriggerInfo>> _triggersByType;
        private readonly HashSet<TriggerType> _suppressedTypes;
        private readonly IDMEEditor _dmeEditor;
        private bool _allSuppressed;
        
        public bool AreTriggersSuppressed => _allSuppressed;
        
        // Events
        public event EventHandler<TriggerExecutingEventArgs> TriggerExecuting;
        public event EventHandler<TriggerExecutedEventArgs> TriggerExecuted;
        public event EventHandler<TriggerErrorEventArgs> TriggerError;
        
        public TriggerManager(IDMEEditor dmeEditor)
        {
            _dmeEditor = dmeEditor;
            _namedTriggers = new ConcurrentDictionary<string, TriggerInfo>(StringComparer.OrdinalIgnoreCase);
            _triggersByType = new ConcurrentDictionary<TriggerType, List<TriggerInfo>>();
            _suppressedTypes = new HashSet<TriggerType>();
        }
        
        #region Registration
        
        public void RegisterTrigger(TriggerType type, 
            Func<TriggerContext, Task<TriggerResult>> handler, 
            int executionOrder = 0, 
            string blockName = null, 
            string itemName = null)
        {
            var trigger = new TriggerInfo
            {
                TriggerName = $"{type}_{Guid.NewGuid():N}".Substring(0, 20),
                TriggerType = type,
                Handler = handler,
                ExecutionOrder = executionOrder,
                BlockName = blockName,
                ItemName = itemName,
                Timing = GetTimingFromType(type),
                Scope = GetScopeFromType(type),
                IsEnabled = true,
                RegisteredDate = DateTime.Now
            };
            
            RegisterTriggerInternal(trigger);
        }
        
        public void RegisterTrigger(string triggerName, TriggerType type,
            Func<TriggerContext, Task<TriggerResult>> handler,
            string description = null, string blockName = null, string itemName = null)
        {
            if (string.IsNullOrEmpty(triggerName))
                throw new ArgumentException("Trigger name required", nameof(triggerName));
            
            if (_namedTriggers.ContainsKey(triggerName))
                throw new InvalidOperationException($"Trigger '{triggerName}' already registered");
            
            var trigger = new TriggerInfo
            {
                TriggerName = triggerName,
                TriggerType = type,
                Handler = handler,
                Description = description,
                BlockName = blockName,
                ItemName = itemName,
                Timing = GetTimingFromType(type),
                Scope = GetScopeFromType(type),
                IsEnabled = true,
                RegisteredDate = DateTime.Now
            };
            
            RegisterTriggerInternal(trigger);
            _namedTriggers[triggerName] = trigger;
        }
        
        private void RegisterTriggerInternal(TriggerInfo trigger)
        {
            if (!_triggersByType.ContainsKey(trigger.TriggerType))
            {
                _triggersByType[trigger.TriggerType] = new List<TriggerInfo>();
            }
            
            _triggersByType[trigger.TriggerType].Add(trigger);
            
            // Sort by execution order
            _triggersByType[trigger.TriggerType] = _triggersByType[trigger.TriggerType]
                .OrderBy(t => t.ExecutionOrder)
                .ThenBy(t => t.RegisteredDate)
                .ToList();
        }
        
        #endregion
        
        #region Execution
        
        public async Task<TriggerResult> ExecuteTriggersAsync(TriggerType type, 
            TriggerContext context, CancellationToken cancellationToken = default)
        {
            if (_allSuppressed || _suppressedTypes.Contains(type))
                return TriggerResult.OK();
            
            if (!_triggersByType.TryGetValue(type, out var triggers) || triggers.Count == 0)
                return TriggerResult.OK();
            
            var enabledTriggers = triggers.Where(t => t.IsEnabled).ToList();
            if (enabledTriggers.Count == 0)
                return TriggerResult.OK();
            
            context.TriggerType = type;
            
            foreach (var trigger in enabledTriggers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Fire executing event
                TriggerExecuting?.Invoke(this, new TriggerExecutingEventArgs
                {
                    TriggerName = trigger.TriggerName,
                    TriggerType = type,
                    Context = context
                });
                
                var startTime = DateTime.Now;
                trigger.ExecutionCount++;
                
                try
                {
                    var result = await trigger.Handler(context);
                    
                    // Update statistics
                    trigger.LastExecutionTime = DateTime.Now;
                    var durationMs = (DateTime.Now - startTime).TotalMilliseconds;
                    trigger.AverageExecutionMs = 
                        ((trigger.AverageExecutionMs * (trigger.ExecutionCount - 1)) + durationMs) 
                        / trigger.ExecutionCount;
                    
                    // Fire executed event
                    TriggerExecuted?.Invoke(this, new TriggerExecutedEventArgs
                    {
                        TriggerName = trigger.TriggerName,
                        TriggerType = type,
                        Result = result,
                        ExecutionTimeMs = durationMs
                    });
                    
                    if (!result.Success || result.Cancel || context.Cancel)
                    {
                        trigger.CancellationCount++;
                        return TriggerResult.Cancelled(result.ErrorMessage ?? context.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    trigger.ErrorCount++;
                    
                    // Fire error event
                    TriggerError?.Invoke(this, new TriggerErrorEventArgs
                    {
                        TriggerName = trigger.TriggerName,
                        TriggerType = type,
                        Exception = ex
                    });
                    
                    // Execute ON-ERROR trigger if not already in error handling
                    if (type != TriggerType.OnError)
                    {
                        var errorContext = new TriggerContext
                        {
                            FormName = context.FormName,
                            BlockName = context.BlockName,
                            TriggerType = TriggerType.OnError,
                            ErrorMessage = ex.Message,
                            Parameters = new Dictionary<string, object>
                            {
                                ["Exception"] = ex,
                                ["OriginalTriggerType"] = type,
                                ["OriginalTriggerName"] = trigger.TriggerName
                            }
                        };
                        
                        await ExecuteTriggersAsync(TriggerType.OnError, errorContext, cancellationToken);
                    }
                    
                    return TriggerResult.Error($"Trigger '{trigger.TriggerName}' error: {ex.Message}");
                }
            }
            
            return TriggerResult.OK();
        }
        
        #endregion
        
        #region Helper Methods
        
        private static TriggerTiming GetTimingFromType(TriggerType type)
        {
            var name = type.ToString();
            if (name.StartsWith("Pre")) return TriggerTiming.Before;
            if (name.StartsWith("Post")) return TriggerTiming.After;
            if (name.StartsWith("On")) return TriggerTiming.On;
            return TriggerTiming.When;
        }
        
        private static TriggerScope GetScopeFromType(TriggerType type)
        {
            var value = (int)type;
            return value switch
            {
                < 100 => TriggerScope.Form,
                < 200 => TriggerScope.Block,
                < 300 => TriggerScope.Record,
                < 400 => TriggerScope.Item,
                < 500 => TriggerScope.Navigation,
                _ => TriggerScope.System
            };
        }
        
        #endregion
    }
}
```

---

## Step 5: Wire Up to FormsManager

### Modification: `FormsManager.cs`

```csharp
// Add fields
private readonly ITriggerManager _triggerManager;
private readonly IEventManager _eventManager;  // Keep for backward compat

// Add to constructor
public FormsManager(
    IDMEEditor dmeEditor,
    // ... existing managers ...
    ITriggerManager triggerManager,
    IEventManager eventManager)
{
    _triggerManager = triggerManager ?? throw new ArgumentNullException(nameof(triggerManager));
    _eventManager = eventManager;
    
    // Bridge events to triggers
    WireEventManagerToTriggers();
}

// Bridge old events to new triggers
private void WireEventManagerToTriggers()
{
    _eventManager.OnPreInsert += async (s, e) =>
    {
        var ctx = new TriggerContext { BlockName = e.BlockName, CurrentRecord = e.Record };
        await _triggerManager.ExecuteTriggersAsync(TriggerType.PreInsert, ctx);
    };
    
    _eventManager.OnPostInsert += async (s, e) =>
    {
        var ctx = new TriggerContext { BlockName = e.BlockName, CurrentRecord = e.Record };
        await _triggerManager.ExecuteTriggersAsync(TriggerType.PostInsert, ctx);
    };
    
    // ... wire other events ...
}

// Public property
public ITriggerManager Triggers => _triggerManager;
```

### New Partial: `FormsManager.TriggerOperations.cs`

```csharp
namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        /// <summary>
        /// Register a trigger (Oracle Forms equivalent)
        /// </summary>
        public void RegisterTrigger(TriggerType type, 
            Func<TriggerContext, Task<TriggerResult>> handler,
            string blockName = null)
        {
            _triggerManager.RegisterTrigger(type, handler, blockName: blockName);
        }
        
        /// <summary>
        /// Register a named trigger
        /// </summary>
        public void RegisterTrigger(string name, TriggerType type,
            Func<TriggerContext, Task<TriggerResult>> handler,
            string description = null)
        {
            _triggerManager.RegisterTrigger(name, type, handler, description);
        }
        
        /// <summary>
        /// Fire triggers for DML operations
        /// </summary>
        protected async Task<bool> FireDMLTriggersAsync(TriggerType preType, 
            TriggerType postType, string blockName, object record,
            Func<Task<bool>> operation)
        {
            var context = new TriggerContext
            {
                FormName = CurrentFormName,
                BlockName = blockName,
                CurrentRecord = record
            };
            
            // PRE trigger
            var preResult = await _triggerManager.ExecuteTriggersAsync(preType, context);
            if (!preResult.Success) return false;
            
            // Execute operation
            var success = await operation();
            if (!success) return false;
            
            // POST trigger
            context.TriggerType = postType;
            var postResult = await _triggerManager.ExecuteTriggersAsync(postType, context);
            
            return postResult.Success;
        }
    }
}
```

---

## Step 6: Update BeepDataBlock (UI Layer)

### Modification: `BeepDataBlock.Triggers.cs`

Keep UI-specific triggers, delegate business triggers to FormsManager:

```csharp
public partial class BeepDataBlock
{
    private ITriggerManager _triggerManager;  // From FormsManager
    
    // UI-only triggers (keep local)
    private Dictionary<TriggerType, List<BeepDataBlockTrigger>> _uiTriggers = new();
    
    // UI-only trigger types (not delegated)
    private static readonly HashSet<TriggerType> _uiOnlyTriggers = new()
    {
        TriggerType.KeyNextItem,
        TriggerType.KeyPrevItem,
        TriggerType.WhenItemFocus,
        TriggerType.WhenItemBlur,
        TriggerType.OnItemClick,
        TriggerType.OnItemDoubleClick
    };
    
    /// <summary>
    /// Register trigger - delegates to FormsManager unless UI-only
    /// </summary>
    public void RegisterTrigger(TriggerType type, Func<TriggerContext, Task<bool>> handler)
    {
        if (_uiOnlyTriggers.Contains(type))
        {
            // Keep UI-specific triggers local
            RegisterUITrigger(type, handler);
        }
        else
        {
            // Delegate to FormsManager
            _triggerManager?.RegisterTrigger(type, 
                async ctx => 
                {
                    var result = await handler(ConvertContext(ctx));
                    return result ? TriggerResult.OK() : TriggerResult.Cancelled();
                },
                blockName: Name);
        }
    }
    
    /// <summary>
    /// Execute triggers - calls FormsManager for business, local for UI
    /// </summary>
    protected async Task<bool> ExecuteTriggers(TriggerType type, TriggerContext context)
    {
        if (_uiOnlyTriggers.Contains(type))
        {
            return await ExecuteUITriggers(type, context);
        }
        
        // Delegate to FormsManager
        var ctx = ConvertToFormsContext(context);
        var result = await _triggerManager.ExecuteTriggersAsync(type, ctx);
        return result.Success && !result.Cancel;
    }
}
```

---

## Migration Checklist

### Phase A5 Tasks:

- [ ] **A5.1**: Create `TriggerType.cs` enum in BeepDM (copy from BeepDataBlock)
- [ ] **A5.2**: Create `TriggerTiming.cs`, `TriggerScope.cs` enums in BeepDM
- [ ] **A5.3**: Create `TriggerInfo.cs` model in BeepDM
- [ ] **A5.4**: Create `TriggerContext.cs` model in BeepDM
- [ ] **A5.5**: Create `TriggerResult.cs` model in BeepDM
- [ ] **A5.6**: Create `TriggerEventArgs.cs` event args in BeepDM
- [ ] **A5.7**: Create `ITriggerManager.cs` interface in DataManagementModelsStandard
- [ ] **A5.8**: Create `TriggerManager.cs` implementation in BeepDM
- [ ] **A5.9**: Create `FormsManager.TriggerOperations.cs` partial
- [ ] **A5.10**: Wire TriggerManager into `FormsManager.cs`
- [ ] **A5.11**: Update `BeepDataBlock.Triggers.cs` to delegate
- [ ] **A5.12**: Build and test

---

## UI-Only Triggers (Stay in BeepDataBlock)

These triggers are inherently UI-specific and should NOT move to FormsManager:

| Trigger | Reason |
|---------|--------|
| `KeyNextItem` | Keyboard event (Tab key) |
| `KeyPrevItem` | Keyboard event (Shift+Tab) |
| `WhenItemFocus` | Control.GotFocus |
| `WhenItemBlur` | Control.LostFocus |
| `OnItemClick` | Mouse Click event |
| `OnItemDoubleClick` | Mouse DoubleClick event |
| `KeyF9` (if added) | F9 for LOV |

---

## Dependencies

- **Requires**: Phase A1 (SystemVariables) — TriggerContext accesses SystemVariables
- **Requires**: Phase A2 (Validation) — WhenValidate* triggers use validation
- **Requires**: Phase A4 (ItemProperties) — Item triggers need item info
- **Required by**: All FormsManager operations use triggers

---

## Estimated Effort

| Task | Files | Lines | Time |
|------|-------|-------|------|
| A5.1-A5.6 Models/Enums | 6 | ~400 | 60 min |
| A5.7 Interface | 1 | ~100 | 20 min |
| A5.8 Implementation | 1 | ~600 | 120 min |
| A5.9-A5.10 Wire-up | 2 | ~200 | 45 min |
| A5.11 BeepDataBlock | 1 | ~150 | 30 min |
| Testing | - | - | 60 min |
| **Total** | **11** | **~1450** | **~5.5 hrs** |

---

## Testing Strategy

1. **Unit Tests**:
   - Trigger registration (anonymous and named)
   - Execution order (lower numbers execute first)
   - Cancellation (trigger returns Cancel → operation stops)
   - ON-ERROR escalation (exception → OnError trigger fires)
   - Statistics tracking

2. **Integration Tests**:
   - PRE/POST triggers for Insert/Update/Delete
   - Trigger suppression
   - BeepDataBlock delegation to TriggerManager
   - UI-only triggers stay local
