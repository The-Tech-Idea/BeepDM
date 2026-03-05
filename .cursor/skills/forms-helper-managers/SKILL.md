---
name: forms-helper-managers
description: Detailed guidance for FormsManager helper architecture including RelationshipManager, DirtyStateManager, EventManager, and FormsSimulationHelper. Use when extending helper logic, wiring triggers, or debugging master-detail, dirty-state, and field simulation behavior.
---

# Forms Helper Managers

Use this skill when touching helper classes under `Editor/Forms/Helpers`.

## Additional Resources
- End-to-end scenarios: [reference.md](reference.md)

## Helper Responsibilities
- `RelationshipManager`: stores and applies master-detail relationships; synchronizes detail blocks.
- `DirtyStateManager`: analyzes unsaved changes and handles save/rollback decisions across related blocks.
- `EventManager`: wraps trigger/event pipeline for block, record, DML, validation, and errors.
- `FormsSimulationHelper`: reflection-based field access, audit defaults, sequence emulation, and system variable helpers.

## RelationshipManager Guidance
- Register blocks before creating relationships.
- Use `CreateMasterDetailRelation(master, detail, masterKey, detailForeignKey, type)`.
- Call `SynchronizeDetailBlocksAsync(master)` whenever master cursor changes.
- Use `RemoveBlockRelationships(block)` during unregister/dispose workflows.

## DirtyStateManager Guidance
- Use `CheckAndHandleUnsavedChangesAsync(block)` before navigation and mode transitions.
- `GetDirtyBlocks()` gives fast names; `GetDirtyBlocksWithDetails()` provides diagnostics.
- Save flow uses dependency ordering and optional validation/retry logic.
- Rollback flow runs per dirty block and honors rollback options.

## EventManager Guidance
- Subscribe per block using `SubscribeToUnitOfWorkEvents(unitOfWork, blockName)`.
- Trigger APIs should be centralized:
  - `TriggerBlockEnter`, `TriggerBlockLeave`
  - `TriggerFieldValidation`, `TriggerRecordValidation`
  - `TriggerError`
- `UnsubscribeFromUnitOfWorkEvents` currently notes simplified handler cleanup; avoid duplicate subscriptions.

## FormsSimulationHelper Guidance
- Use `SetAuditDefaults` for insert-time defaults.
- Use `SetFieldValue`/`GetFieldValue` instead of repeated ad-hoc reflection.
- Use `ExecuteSequence` when sequence semantics are needed from `UnitOfWork.GetSeq`.
- Use `SetSystemVariables` for Oracle-forms-like system field population.

## Integration Pattern
```csharp
forms.CreateMasterDetailRelation("CUSTOMERS", "ORDERS", "Id", "CustomerId");

var canContinue = await forms.CheckAndHandleUnsavedChangesAsync("CUSTOMERS");
if (!canContinue)
{
    return;
}

await forms.SynchronizeDetailBlocksAsync("CUSTOMERS");
```

## Trigger Pattern
```csharp
forms.OnValidateField += (s, e) =>
{
    if (e.FieldName == "Email" && string.IsNullOrWhiteSpace(e.Value?.ToString()))
    {
        e.IsValid = false;
        e.ErrorMessage = "Email is required";
    }
};
```

## Pitfalls
- Creating relationships with unregistered blocks throws and leaves inconsistent setup.
- Skipping dirty-state checks can drop user edits in parent/child blocks.
- Double subscription can duplicate DML side effects.
- Reflection-based field names must match runtime model names (or known aliases).

## Verification Checklist
- Block registration order is deterministic.
- Relationship key fields are correct and active.
- Dirty-state event path is wired and tested.
- Event subscriptions are paired with unsubscribe on teardown.
- Audit and sequence behavior is validated for inserts.

