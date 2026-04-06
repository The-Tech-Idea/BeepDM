# Phase 4 — FormsManager Advanced Operations

**Goal:** Add the higher-level form-orchestration features that FormsManager currently lacks:
FK-aware commit ordering, form-state persistence, cross-block validation, block-level
navigation history, and a block clone/snapshot mechanism.

**Pre-condition:** Phases 1–3 complete.

**Files touched:** `DataManagementEngineStandard/Editor/Forms/`

---

## 4-A  FK-Aware Commit Ordering in `CommitFormAsync`

**Problem:** `CommitFormAsync` iterates `_blocks.Keys` in insertion order.  With master-detail
trees this breaks FK constraints if a detail insert commits before its new master row.

**Solution:** Topological sort of blocks using the `_relationships` dependency graph before commit.

### Algorithm

1. Build a directed graph: `masterBlock → detailBlock` edge means "master must commit before detail".
2. Apply Kahn's algorithm (BFS topological sort) to get `commitOrder : List<string>`.
3. Within the commit loop, process blocks in `commitOrder`.
4. If a cycle is detected, fall back to current order and log a warning.

### Changes

- **`FormsManager.FormOperations.cs`** — replace the block iteration loop in `CommitFormAsync`
  with a call to `BuildCommitOrder()`.
- **New private method `BuildCommitOrder()`** returns `List<string>`:

```csharp
private List<string> BuildCommitOrder()
{
    // Kahn's topological sort using _relationships
    // Master blocks come first, detail blocks come after
    ...
}
```

- No interface change needed (internal implementation only).

---

## 4-B  Form State Persistence

**Goal:** Save and restore the complete form state (cursor position, current mode, filter per block,
dirty flags) so that navigating away and returning restores exactly where the user was.

### Model

**New file:** `Models/FormStateSnapshot.cs`

```csharp
public class FormStateSnapshot
{
    public string FormName      { get; set; }
    public DateTime CapturedAt  { get; set; }
    public string CurrentBlock  { get; set; }
    public Dictionary<string, BlockStateSnapshot> BlockStates { get; set; } = new();
}

public class BlockStateSnapshot
{
    public string BlockName     { get; set; }
    public int CursorPosition   { get; set; }   // CurrentIndex in OBL
    public string Mode          { get; set; }   // Normal | Query | Insert
    public string FilterExpression { get; set; }
    public bool IsDirty         { get; set; }
    public int RecordCount      { get; set; }
}
```

### API on `IUnitofWorksManager`

```csharp
/// <summary>Capture a snapshot of the current form state.</summary>
FormStateSnapshot SaveFormState();

/// <summary>
/// Restore form state from a snapshot.
/// Navigates each block to the saved cursor position, re-applies filters.
/// </summary>
Task<bool> RestoreFormStateAsync(FormStateSnapshot snapshot,
    CancellationToken ct = default);
```

### Implementation

- **`FormsManager.FormOperations.cs`**: implement `SaveFormState()` by iterating `_blocks` and
  reading `UOW.Units.CurrentIndex`, `UOW.FilterExpression`, mode from SystemVariables.
- `RestoreFormStateAsync`: call `SwitchToBlockAsync(blockName)`, navigate to
  `snapshot.CursorPosition`, re-apply filter via `ExecuteQueryAsync`.

---

## 4-C  Cross-Block Validation

**Goal:** Some business rules span two blocks (e.g. "total order lines amount ≤ customer credit limit").
These can't be expressed as per-field or per-block rules today.

### Model

**New file:** `Models/CrossBlockValidationRule.cs`

```csharp
public class CrossBlockValidationRule
{
    public string RuleName       { get; set; }
    public string BlockA         { get; set; }
    public string BlockB         { get; set; }
    /// <summary>
    /// Receives (blockAUow, blockBUow); return null/empty for pass, message for fail.
    /// </summary>
    public Func<IUnitofWork, IUnitofWork, string> Validator { get; set; }
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
}
```

### API on `IUnitofWorksManager`

```csharp
void RegisterCrossBlockRule(CrossBlockValidationRule rule);
bool UnregisterCrossBlockRule(string ruleName);

/// <summary>
/// Run all registered cross-block rules.
/// Returns a list of ValidationResult for any failures.
/// </summary>
List<ValidationResult> ValidateCrossBlock();
```

### Implementation

- **New file:** `Helpers/CrossBlockValidationManager.cs`
- Store rules in `Dictionary<string, CrossBlockValidationRule>`.
- `ValidateCrossBlock()` resolves Block A + B UOWs, invokes each rule, collects any non-null failure messages.
- Wire into `FormsManager.cs` field `_crossBlockValidation` + property `CrossBlockValidation`.
- Call `ValidateCrossBlock()` inside `CommitFormAsync` before committing — block commit if any
  Error-severity rule fails.

---

## 4-D  Block-Level Navigation History

**Goal:** Forward/back navigation stack per block — mirrors a browser's Back/Forward for record navigation.

### Model

**New file:** `Models/NavigationHistoryEntry.cs`

```csharp
public class NavigationHistoryEntry
{
    public int RecordIndex   { get; set; }
    public DateTime VisitedAt { get; set; }
}
```

### API on `IUnitofWorksManager`

```csharp
/// <summary>Go back to the previously visited record in the block.</summary>
Task<bool> NavigateBackAsync(string blockName);

/// <summary>Go forward after a back navigation.</summary>
Task<bool> NavigateForwardAsync(string blockName);

bool CanNavigateBack(string blockName);
bool CanNavigateForward(string blockName);

/// <summary>Get the full navigation history for diagnostic display.</summary>
IReadOnlyList<NavigationHistoryEntry> GetNavigationHistory(string blockName);
void ClearNavigationHistory(string blockName);
```

### Implementation

- **New helper:** `Helpers/NavigationHistoryManager.cs`
  - Per-block stacks: `_backStack : Stack<int>`, `_forwardStack : Stack<int>`.
  - `Push(blockName, index)`: push to back stack, clear forward stack.
  - `Back(blockName)`: pop from back stack → navigate, push to forward.
  - `Forward(blockName)`: pop from forward stack → navigate, push to back.
- Wire into `FormsManager.Navigation.cs`: call `_navHistory.Push(blockName, oldIndex)` whenever
  `NavigateTo(blockName, index)` moves the cursor.
- Add `_navHistoryManager` field to `FormsManager.cs`.

---

## 4-E  Block Clone / Snapshot

**Goal:** "Duplicate current record" or "clone block state to a scratch area" for comparison or
what-if scenarios.

### API on `IUnitofWorksManager`

```csharp
/// <summary>
/// Clone all loaded data from sourceBlock into destBlock (which must already be registered).
/// destBlock's UOW is populated with copies of all source records (all states = Added).
/// </summary>
Task<bool> CloneBlockDataAsync(
    string sourceBlockName,
    string destBlockName,
    CancellationToken ct = default);

/// <summary>Clone the current record into a new record (Insert) in the same block.</summary>
Task<bool> DuplicateCurrentRecordAsync(
    string blockName,
    CancellationToken ct = default);
```

### Implementation

- `CloneBlockDataAsync`: call `sourceUow.Units.ToList()` → for each item call `sourceUow.CloneItem(item)`
  → bulk add to `destUow.Units.LoadBatch(clones)`.
- `DuplicateCurrentRecordAsync`: call `CloneItem(Units.Current)` → `InsertRecordAsync(blockName, clone)`.
- Add to `FormsManager.DataOperations.cs`.

---

## 4-F  Block Change Feed (Real-Time Event)

**Goal:** FormsManager fires a single event whenever ANY field in ANY block changes,
with enough context for the UI to update status bars / dirty indicators without polling.

### Model

**New file:** `Models/BlockFieldChangedEventArgs.cs`

```csharp
public class BlockFieldChangedEventArgs : EventArgs
{
    public string BlockName   { get; set; }
    public string FieldName   { get; set; }
    public object OldValue    { get; set; }
    public object NewValue    { get; set; }
    public int    RecordIndex { get; set; }
}
```

### API on `IUnitofWorksManager`

```csharp
/// <summary>
/// Fires when any tracked field in any registered block changes.
/// Subscribe to drive dirty-flag indicators, computed fields, etc.
/// </summary>
event EventHandler<BlockFieldChangedEventArgs> OnBlockFieldChanged;
```

### Implementation

- Subscribe to each UOW's `Units.ItemChanged` event in `RegisterBlock`.
- In the handler: read `BlockFieldChangedEventArgs` from the OBL event args, then re-fire
  `OnBlockFieldChanged`.
- Unsubscribe in `UnregisterBlock`.
- Add event field to `FormsManager.cs`; raise in `FormsManager.EnhancedOperations.cs`.

---

## Checklist

| # | Task | File | Status |
|---|---|---|---|
| 4-A.1 | Implement `BuildCommitOrder()` using topological sort | `FormsManager.FormOperations.cs` | [ ] |
| 4-A.2 | Replace block iteration loop in `CommitFormAsync` | `FormsManager.FormOperations.cs` | [ ] |
| 4-B.1 | Add `FormStateSnapshot` + `BlockStateSnapshot` POCOs | `Models/FormStateSnapshot.cs` (new) | [ ] |
| 4-B.2 | Add `SaveFormState` + `RestoreFormStateAsync` to `IUnitofWorksManager` | `IUnitofWorksManagerInterfaces.cs` | [ ] |
| 4-B.3 | Implement both methods | `FormsManager.FormOperations.cs` | [ ] |
| 4-C.1 | Add `CrossBlockValidationRule` + `ValidationSeverity` POCO | `Models/CrossBlockValidationRule.cs` (new) | [ ] |
| 4-C.2 | Add `RegisterCrossBlockRule/Unregister/ValidateCrossBlock` to `IUnitofWorksManager` | `IUnitofWorksManagerInterfaces.cs` | [ ] |
| 4-C.3 | Implement `CrossBlockValidationManager` | `Helpers/CrossBlockValidationManager.cs` (new) | [ ] |
| 4-C.4 | Wire into `FormsManager.cs` + call in `CommitFormAsync` | `FormsManager.cs` + `FormsManager.FormOperations.cs` | [ ] |
| 4-D.1 | Add `NavigationHistoryEntry` POCO | `Models/NavigationHistoryEntry.cs` (new) | [ ] |
| 4-D.2 | Add navigation-history API to `IUnitofWorksManager` | `IUnitofWorksManagerInterfaces.cs` | [ ] |
| 4-D.3 | Implement `NavigationHistoryManager` | `Helpers/NavigationHistoryManager.cs` (new) | [ ] |
| 4-D.4 | Wire push into `FormsManager.Navigation.cs` | `FormsManager.Navigation.cs` | [ ] |
| 4-E.1 | Add `CloneBlockDataAsync` + `DuplicateCurrentRecordAsync` to `IUnitofWorksManager` | `IUnitofWorksManagerInterfaces.cs` | [ ] |
| 4-E.2 | Implement in `FormsManager.DataOperations.cs` | `FormsManager.DataOperations.cs` | [ ] |
| 4-F.1 | Add `BlockFieldChangedEventArgs` POCO | `Models/BlockFieldChangedEventArgs.cs` (new) | [ ] |
| 4-F.2 | Add `OnBlockFieldChanged` event to `IUnitofWorksManager` | `IUnitofWorksManagerInterfaces.cs` | [ ] |
| 4-F.3 | Subscribe/unsubscribe in `RegisterBlock` / `UnregisterBlock` | `FormsManager.cs` | [ ] |
| 4-F.4 | Fire `OnBlockFieldChanged` from OBL `ItemChanged` handler | `FormsManager.cs` or `FormsManager.EnhancedOperations.cs` | [ ] |
