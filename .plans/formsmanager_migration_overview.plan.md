# FormsManager Migration Plan - Overview

## Purpose
Migrate business logic from `BeepDataBlock` (WinForms UI control) to `FormsManager` (UI-agnostic engine in BeepDM) to achieve proper separation of concerns and enable multi-platform support.

## Goal
- **FormsManager**: Complete Oracle Forms emulation engine (UI-agnostic)
- **BeepDataBlock**: Thin UI layer only (WinForms-specific rendering and input)

---

## Current State Analysis

### FormsManager (BeepDM)
**Location**: `DataManagementEngineStandard/Editor/Forms/`

**Existing Components**:
| File | Purpose |
|------|---------|
| `FormsManager.cs` | Main coordinator, block registration |
| `FormsManager.EnhancedOperations.cs` | Type-safe CRUD operations |
| `FormsManager.FormOperations.cs` | COMMIT_FORM, ROLLBACK_FORM |
| `FormsManager.Navigation.cs` | Record navigation (First/Next/Previous/Last) |
| `FormsManager.ModeTransitions.cs` | Query ↔ CRUD mode transitions |

**Existing Helpers**:
| File | Purpose |
|------|---------|
| `RelationshipManager.cs` | Master-detail coordination |
| `DirtyStateManager.cs` | Unsaved changes tracking |
| `EventManager.cs` | Block/Record/DML events (~20 types) |
| `FormsSimulationHelper.cs` | Audit defaults, sequences, reflection |
| `PerformanceManager.cs` | Caching and optimization |
| `ConfigurationManager.cs` | JSON-based configuration |

### BeepDataBlock (Beep.Winform)
**Location**: `TheTechIdea.Beep.Winform.Controls.Integrated/DataBlocks/`

**Components to Migrate**:
| File | Feature | Migration Target |
|------|---------|------------------|
| `BeepDataBlock.SystemVariables.cs` | `:SYSTEM.*` variables (30+) | **Phase A1** |
| `BeepDataBlock.Validation.cs` | Validation rules (10+ types) | **Phase A2** |
| `BeepDataBlock.LOV.cs` | List of Values system | **Phase A3** |
| `BeepDataBlock.Properties.cs` | Item properties (18+) | **Phase A4** |
| `BeepDataBlock.Triggers.cs` | Async trigger system (50+ types) | **Phase A5** |

**Components to Keep (UI-Only)**:
| File | Reason |
|------|--------|
| `BeepDataBlock.Navigation.cs` | Focus management, keyboard |
| `BeepDataBlock.cs` | Control binding, ViewMode |
| `BeepDataBlock.UnitOfWork.cs` | Delegates to FormsManager |
| `BeepDataBlock.Coordination.cs` | UI update on master change |

---

## Migration Phases

### Phase A1: System Variables Migration
**Files**: See `formsmanager_migration_phase_a1_systemvariables.plan.md`
- Create `ISystemVariablesManager` interface
- Create `SystemVariablesManager.cs` helper
- Create `SystemVariables` model class
- Add `FormsManager.SystemVariables.cs` partial

### Phase A2: Validation Manager Migration
**Files**: See `formsmanager_migration_phase_a2_validation.plan.md`
- Create `IValidationManager` interface
- Create `ValidationManager.cs` helper
- Move `ValidationRule`, `ValidationType` models
- Add `FormsManager.Validation.cs` partial

### Phase A3: LOV Manager Migration
**Files**: See `formsmanager_migration_phase_a3_lov.plan.md`
- Create `ILOVManager` interface
- Create `LOVManager.cs` helper
- Move `LOVDefinition` model
- Add `FormsManager.LOV.cs` partial

### Phase A4: Item Properties Migration
**Files**: See `formsmanager_migration_phase_a4_itemproperties.plan.md`
- Create `IItemPropertyManager` interface
- Create `ItemPropertyManager.cs` helper
- Move `ItemProperties` model
- Add `FormsManager.ItemProperties.cs` partial

### Phase A5: Trigger System Enhancement
**Files**: See `formsmanager_migration_phase_a5_triggers.plan.md`
- Enhance `IEventManager` with async triggers
- Add `TriggerContext` model
- Add `TriggerType` enum (50+ values)
- Add `FormsManager.Triggers.cs` partial

---

## Target Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    FormsManager (BeepDM)                        │
│              UI-Agnostic Oracle Forms Engine                    │
├─────────────────────────────────────────────────────────────────┤
│  EXISTING Helper Managers:                                      │
│  • IRelationshipManager      • IDirtyStateManager               │
│  • IEventManager             • IPerformanceManager              │
│  • IFormsSimulationHelper    • IConfigurationManager            │
├─────────────────────────────────────────────────────────────────┤
│  NEW Managers (From BeepDataBlock):                             │
│  • ISystemVariablesManager   • IValidationManager               │
│  • ILOVManager               • IItemPropertyManager             │
│  • ITriggerManager (enhanced IEventManager)                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │  IFormsManager / IUnitofWorksManager
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                BeepDataBlock (Beep.Winform)                     │
│                 Thin UI Layer Only                              │
├─────────────────────────────────────────────────────────────────┤
│  UI-Only Responsibilities:                                      │
│  • Control binding (UIComponents dictionary)                    │
│  • Focus/navigation (NextItem, GoToItem)                        │
│  • Keyboard handlers (F9, Tab, Enter, Arrow keys)               │
│  • Visual feedback (borders, error icons, highlighting)         │
│  • Dialog display (LOV dialog, message boxes)                   │
│  • View rendering (Card, Grid, Mixed layouts)                   │
├─────────────────────────────────────────────────────────────────┤
│  DELEGATES TO FormsManager:                                     │
│  • SYSTEM vars: _formsManager.GetSystemVariables(blockName)     │
│  • Validation: _formsManager.ValidateField(blockName, field)    │
│  • LOV data: _formsManager.GetLOVData(blockName, itemName)      │
│  • Properties: _formsManager.GetItemProperties(blockName, item) │
│  • Triggers: await _formsManager.ExecuteTriggerAsync(...)       │
└─────────────────────────────────────────────────────────────────┘
```

---

## File Structure After Migration

### BeepDM - New Files

```
DataManagementEngineStandard/Editor/Forms/
├── FormsManager.cs                      (existing - update)
├── FormsManager.EnhancedOperations.cs   (existing)
├── FormsManager.FormOperations.cs       (existing)
├── FormsManager.Navigation.cs           (existing)
├── FormsManager.ModeTransitions.cs      (existing)
├── FormsManager.SystemVariables.cs      (NEW - Phase A1)
├── FormsManager.Validation.cs           (NEW - Phase A2)
├── FormsManager.LOV.cs                  (NEW - Phase A3)
├── FormsManager.ItemProperties.cs       (NEW - Phase A4)
├── FormsManager.Triggers.cs             (NEW - Phase A5)
└── Helpers/
    ├── RelationshipManager.cs           (existing)
    ├── DirtyStateManager.cs             (existing)
    ├── EventManager.cs                  (existing - update Phase A5)
    ├── FormsSimulationHelper.cs         (existing)
    ├── PerformanceManager.cs            (existing)
    ├── ConfigurationManager.cs          (existing)
    ├── SystemVariablesManager.cs        (NEW - Phase A1)
    ├── ValidationManager.cs             (NEW - Phase A2)
    ├── LOVManager.cs                    (NEW - Phase A3)
    └── ItemPropertyManager.cs           (NEW - Phase A4)

DataManagementModelsStandard/Editor/UOWManager/
├── Interfaces/
│   ├── ISystemVariablesManager.cs       (NEW - Phase A1)
│   ├── IValidationManager.cs            (NEW - Phase A2)
│   ├── ILOVManager.cs                   (NEW - Phase A3)
│   ├── IItemPropertyManager.cs          (NEW - Phase A4)
│   └── ITriggerManager.cs               (NEW - Phase A5)
└── Models/
    ├── SystemVariables.cs               (NEW - Phase A1)
    ├── ValidationRule.cs                (NEW - Phase A2)
    ├── ValidationType.cs                (NEW - Phase A2)
    ├── LOVDefinition.cs                 (NEW - Phase A3)
    ├── ItemProperties.cs                (NEW - Phase A4)
    ├── TriggerType.cs                   (NEW - Phase A5)
    └── TriggerContext.cs                (NEW - Phase A5)
```

---

## Dependencies

### Phase Dependencies
```
Phase A1 (System Variables) ─── No dependencies
Phase A2 (Validation) ──────── No dependencies
Phase A3 (LOV) ─────────────── No dependencies
Phase A4 (Item Properties) ─── Phase A2 (for REQUIRED validation)
Phase A5 (Triggers) ────────── Phases A1-A4 (trigger context uses all)
```

### Package Dependencies
- All new code in `DataManagementEngineStandard` and `DataManagementModelsStandard`
- No new NuGet packages required
- Existing `ConcurrentDictionary`, `System.Threading.Tasks` already available

---

## BeepDataBlock Refactoring (After All Phases)

After migration, `BeepDataBlock` partial files will be simplified:

| Original File | After Migration |
|---------------|-----------------|
| `BeepDataBlock.SystemVariables.cs` | Thin wrapper: `SYSTEM => _formsManager.GetSystemVariables(Name)` |
| `BeepDataBlock.Validation.cs` | UI feedback only: `ShowValidationError()`, `ClearValidationError()` |
| `BeepDataBlock.LOV.cs` | Dialog display only: `ShowLOVDialog()`, F9 handler |
| `BeepDataBlock.Properties.cs` | Control binding only: `ApplyPropertyToControl()` |
| `BeepDataBlock.Triggers.cs` | UI triggers only: `WHEN-MOUSE-CLICK`, `KEY-*`, delegates rest |

---

## Success Criteria

1. **All business logic in FormsManager** - No data/rule logic in UI code
2. **BeepDataBlock compiles with FormsManager reference** - Clean dependency
3. **Unit tests pass** - FormsManager testable without UI
4. **Existing functionality preserved** - No regression in BeepDataBlock
5. **Clean interfaces** - All managers implement interfaces from Models project

---

## Estimated Effort

| Phase | New Files | Modified Files | Complexity |
|-------|-----------|----------------|------------|
| A1 - System Variables | 4 | 2 | Low |
| A2 - Validation | 5 | 2 | Medium |
| A3 - LOV | 4 | 2 | Medium |
| A4 - Item Properties | 4 | 2 | Low |
| A5 - Triggers | 5 | 3 | High |
| **Total** | **22** | **11** | - |

---

## Related Plans
- `formsmanager_migration_phase_a1_systemvariables.plan.md`
- `formsmanager_migration_phase_a2_validation.plan.md`
- `formsmanager_migration_phase_a3_lov.plan.md`
- `formsmanager_migration_phase_a4_itemproperties.plan.md`
- `formsmanager_migration_phase_a5_triggers.plan.md`
