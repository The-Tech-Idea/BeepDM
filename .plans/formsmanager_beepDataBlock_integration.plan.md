# FormsManager ↔ BeepDataBlock Integration Plan

## Purpose
Define how BeepDataBlock (WinForms UI layer) integrates with FormsManager (engine layer) after the migration of business logic to FormsManager.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    FormsManager (BeepDM)                        │
│              UI-Agnostic Oracle Forms Engine                    │
├─────────────────────────────────────────────────────────────────┤
│  Manager Properties (accessed via _formsManager.*):             │
│  • SystemVariables  → ISystemVariablesManager                   │
│  • Validation       → IValidationManager                        │
│  • LOV              → ILOVManager                               │
│  • ItemProperties   → IItemPropertyManager                      │
│  • Triggers         → ITriggerManager                           │
│  • Relationships    → IRelationshipManager                      │
│  • DirtyState       → IDirtyStateManager                        │
│  • Events           → IEventManager                             │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │  Delegates business logic to
                              │
┌─────────────────────────────────────────────────────────────────┐
│                BeepDataBlock (Beep.Winform)                     │
│        Thin UI Layer + Bridge to FormsManager                   │
├─────────────────────────────────────────────────────────────────┤
│  UI-Specific (KEEP in BeepDataBlock):                           │
│  • UIComponents dictionary                                      │
│  • Control binding/unbinding                                    │
│  • Focus management (NextItem, GoToItem)                        │
│  • Keyboard handlers (F9, Tab, Enter, Arrow)                    │
│  • Visual feedback (errors, highlighting)                       │
│  • Dialog display (LOV dialogs, message boxes)                  │
│  • View rendering (Card, Grid, Mixed layouts)                   │
├─────────────────────────────────────────────────────────────────┤
│  Bridge/Adapter (NEW - maps UI models to engine models):        │
│  • BeepDataBlockLOV ↔ LOVDefinition                             │
│  • BeepDataBlockItem ↔ ItemInfo                                 │
│  • BeepDataBlockTrigger ↔ TriggerDefinition                     │
│  • ValidationRule (local) ↔ ValidationRule (engine)             │
└─────────────────────────────────────────────────────────────────┘
```

---

## Model Mapping

### System Variables

| BeepDataBlock (UI) | FormsManager (Engine) | Notes |
|--------------------|----------------------|-------|
| `SystemVariables` (local) | `SystemVariables` (engine) | Same structure, different locations |
| `block.SYSTEM.*` | `_formsManager.SystemVariables.GetSystemVariables(blockName)` | Engine provides, UI consumes |

**Integration Pattern**:
- BeepDataBlock.SYSTEM property returns local SystemVariables
- On read, sync with FormsManager.SystemVariables if coordinated
- On write, update both local and FormsManager

### Validation

| BeepDataBlock (UI) | FormsManager (Engine) | Notes |
|--------------------|----------------------|-------|
| `ValidationRule` | `ValidationRule` + `ValidationEnums` | Engine has richer model |
| `_validationRules` dictionary | `ValidationManager.RegisterFieldRule()` | Engine stores rules |
| `ValidateField()` | `ValidationManager.ValidateFieldAsync()` | Engine performs validation |
| `ShowValidationError()` | N/A (UI-only) | Keep in BeepDataBlock |

**Integration Pattern**:
```csharp
// Registration delegates to FormsManager
public void RegisterValidationRule(string fieldName, ValidationRule rule)
{
    if (IsCoordinated)
    {
        // Convert to engine rule and register
        var engineRule = ConvertToEngineRule(rule);
        _formsManager.Validation.RegisterFieldRule(this.Name, fieldName, engineRule);
    }
    else
    {
        // Fallback to local storage
        _validationRules[fieldName].Add(rule);
    }
}

// Validation delegates to FormsManager
public async Task<IErrorsInfo> ValidateField(string fieldName, object value)
{
    if (IsCoordinated)
    {
        var result = await _formsManager.Validation.ValidateFieldAsync(this.Name, fieldName, value);
        ShowValidationFeedback(fieldName, result);  // UI-only
        return ConvertToErrorsInfo(result);
    }
    // ... local fallback
}
```

### LOV (List of Values)

| BeepDataBlock (UI) | FormsManager (Engine) | Notes |
|--------------------|----------------------|-------|
| `BeepDataBlockLOV` | `LOVDefinition` | Engine has richer model |
| `_lovs` dictionary | `LOVManager.RegisterLOV()` | Engine stores definitions |
| `GetLOVData()` | `LOVManager.GetLOVDataAsync()` | Engine fetches data |
| `ShowLOV()` | N/A (UI-only) | Keep dialog display in BeepDataBlock |

**Integration Pattern**:
```csharp
// Register converts UI model to engine model
public void RegisterLOV(string itemName, BeepDataBlockLOV lov)
{
    _lovs[itemName] = lov;  // Keep local for UI binding
    
    if (IsCoordinated)
    {
        var engineLOV = ConvertToEngineLOV(lov);
        _formsManager.LOV.RegisterLOV(this.Name, itemName, engineLOV);
    }
    
    AttachLOVToComponent(itemName, lov);  // UI-only
}

// Show LOV uses FormsManager for data, local for display
public async Task<bool> ShowLOV(string itemName)
{
    IEnumerable<object> data;
    
    if (IsCoordinated)
    {
        var lovResult = await _formsManager.LOV.GetLOVDataAsync(this.Name, itemName);
        data = lovResult.Data;
    }
    else
    {
        data = await FetchLOVDataLocally(itemName);
    }
    
    return await ShowLOVDialog(itemName, data);  // UI-only
}
```

### Item Properties

| BeepDataBlock (UI) | FormsManager (Engine) | Notes |
|--------------------|----------------------|-------|
| `BeepDataBlockItem` | `ItemInfo` | Engine has FormMode |
| `_items` dictionary | `ItemPropertyManager` | Engine stores item state |
| `SetItemProperty()` | `ItemPropertyManager.SetProperty()` | Engine handles logic |
| `ApplyPropertyToControl()` | N/A (UI-only) | Keep in BeepDataBlock |

**Integration Pattern**:
```csharp
// SetItemProperty delegates to FormsManager
public void SetItemProperty(string itemName, string propertyName, object value)
{
    if (IsCoordinated)
    {
        _formsManager.ItemProperties.SetProperty(this.Name, itemName, propertyName, value);
    }
    
    // Always apply to local item for UI
    if (_items.TryGetValue(itemName, out var item))
    {
        SetLocalItemProperty(item, propertyName, value);
        ApplyPropertyToControl(item, propertyName);  // UI-only
    }
}
```

### Triggers

| BeepDataBlock (UI) | FormsManager (Engine) | Notes |
|--------------------|----------------------|-------|
| `BeepDataBlockTrigger` | `TriggerDefinition` | Both have async handlers |
| `TriggerContext` (local) | `TriggerContext` (engine) | Engine context is richer |
| `_triggers` dictionary | `TriggerManager` | Engine manages execution |
| `ExecuteTriggers()` | `TriggerManager.FireBlockTriggerAsync()` | Engine executes |

**Integration Pattern**:
```csharp
// Trigger registration with dual storage
public void RegisterTrigger(TriggerType type, Func<TriggerContext, Task<bool>> handler)
{
    // Keep local for fallback
    var localTrigger = CreateLocalTrigger(type, handler);
    RegisterLocalTrigger(localTrigger);
    
    if (IsCoordinated)
    {
        // Also register with FormsManager (convert to engine types)
        var engineType = ConvertToEngineTriggerType(type);
        _formsManager.Triggers.RegisterBlockTriggerAsync(
            this.Name, 
            engineType, 
            async ctx => await handler(ConvertToLocalContext(ctx)));
    }
}

// Trigger execution through FormsManager
protected async Task<bool> ExecuteTriggers(TriggerType type, TriggerContext context)
{
    if (_suppressTriggers)
        return true;
    
    if (IsCoordinated)
    {
        var engineType = ConvertToEngineTriggerType(type);
        var engineContext = ConvertToEngineContext(context);
        return await _formsManager.Triggers.FireBlockTriggerAsync(this.Name, engineType, engineContext);
    }
    
    // Fallback to local execution
    return await ExecuteTriggersLocally(type, context);
}
```

---

## Implementation Phases

### Phase B1: Adapter Classes
Create converter/adapter classes to map between UI and engine models.

**Files to Create**:
```
TheTechIdea.Beep.Winform.Controls.Integrated/DataBlocks/Adapters/
├── ValidationRuleAdapter.cs     - Maps ValidationRule ↔ engine ValidationRule
├── LOVAdapter.cs               - Maps BeepDataBlockLOV ↔ LOVDefinition
├── ItemPropertyAdapter.cs      - Maps BeepDataBlockItem ↔ ItemInfo
├── TriggerAdapter.cs           - Maps BeepDataBlockTrigger ↔ TriggerDefinition
└── TriggerTypeMapper.cs        - Maps local TriggerType ↔ engine TriggerType
```

### Phase B2: Update SystemVariables Integration
- Add sync logic between local SYSTEM and FormsManager.SystemVariables
- Update `UpdateSystemVariables()` to push to FormsManager when coordinated

### Phase B3: Update Validation Integration
- Update `RegisterValidationRule()` to delegate to FormsManager
- Update `ValidateField()` to use FormsManager.Validation
- Keep `ShowValidationError()` local for UI feedback

### Phase B4: Update LOV Integration
- Update `RegisterLOV()` to register with FormsManager
- Update `ShowLOV()` to get data from FormsManager.LOV
- Keep dialog display local

### Phase B5: Update ItemProperties Integration
- Update `RegisterItem()` to sync with FormsManager.ItemProperties
- Update `SetItemProperty()` to delegate to FormsManager
- Keep `ApplyPropertyToControl()` local for UI

### Phase B6: Update Triggers Integration
- Update `RegisterTrigger()` to register with FormsManager.Triggers
- Update `ExecuteTriggers()` to delegate to FormsManager
- Map TriggerType enums between UI and engine

---

## Backward Compatibility

All changes must maintain backward compatibility:

1. **IsCoordinated Check**: All delegated methods check `IsCoordinated` before using FormsManager
2. **Local Fallback**: If not coordinated, use local storage/execution
3. **Dual Registration**: Register with both local dictionaries and FormsManager

```csharp
// Pattern used throughout
if (IsCoordinated && _formsManager.Validation != null)
{
    // Use FormsManager
    await _formsManager.Validation.ValidateFieldAsync(...)
}
else
{
    // Fallback to local implementation
    await ValidateFieldLocally(...)
}
```

---

## Testing Strategy

1. **Unit Tests**: Test adapters convert correctly between model types
2. **Integration Tests**: Test BeepDataBlock with and without FormsManager
3. **UI Tests**: Verify visual feedback still works (error display, LOV dialogs)

---

## Dependencies

- BeepDM must be built first with all Phase A1-A5 managers
- Beep.Winform references BeepDM
- No new NuGet packages required

---

## File Changes Summary

| File | Change Type | Description |
|------|-------------|-------------|
| `Adapters/*.cs` (5 files) | CREATE | Model converter/adapters |
| `BeepDataBlock.SystemVariables.cs` | UPDATE | Add FormsManager sync |
| `BeepDataBlock.Validation.cs` | UPDATE | Delegate to FormsManager |
| `BeepDataBlock.LOV.cs` | UPDATE | Delegate to FormsManager |
| `BeepDataBlock.Properties.cs` | UPDATE | Delegate to FormsManager |
| `BeepDataBlock.Triggers.cs` | UPDATE | Delegate to FormsManager |

---

## Success Criteria

1. BeepDataBlock works without FormsManager (backward compatible)
2. When FormsManager is attached, business logic flows through engine
3. UI-specific code (dialogs, controls, visual feedback) stays in BeepDataBlock
4. No compile errors
5. Existing tests pass
