---
name: forms-enhanced-data-operations
description: Detailed guidance for FormsManager enhanced data operations such as CreateNewRecord, InsertRecordEnhancedAsync, UpdateCurrentRecordAsync, and ExecuteQueryEnhancedAsync. Use when implementing robust CRUD with validation, reflection-based method resolution, and audit-field defaults.
---

# Forms Enhanced Data Operations

Use this skill when operating on data through `FormsManager.EnhancedOperations`.

## Additional Resources
- End-to-end scenarios: [reference.md](reference.md)

## API Surface
- `CreateNewRecord(string blockName)`
- `InsertRecordEnhancedAsync(string blockName, object record = null)`
- `UpdateCurrentRecordAsync(string blockName)`
- `ExecuteQueryEnhancedAsync(string blockName, List<AppFilter> filters = null)`
- `GetCurrentRecord(string blockName)`
- `GetRecordCount(string blockName)`
- `CopyFields(object sourceRecord, object targetRecord, params string[] fields)`
- `ApplyAuditDefaults(object record, string currentUser = null)`

## Behavior Notes
- Insert/update are mode-aware and may trigger mode transition handling.
- Insert path can auto-create record when `record == null`.
- Validation is performed before DML operations.
- Reflection-based insert/update method resolution is used to support different UnitOfWork shapes.
- Audit defaults and modified stamps are applied via `FormsSimulationHelper`.

## Recommended Insert Flow
```csharp
var modeResult = await forms.EnterCrudModeForNewRecordAsync("ORDERS");
if (modeResult.Flag != Errors.Ok)
{
    return;
}

var insertResult = await forms.InsertRecordEnhancedAsync("ORDERS");
if (insertResult.Flag != Errors.Ok)
{
    // inspect insertResult.Message and insertResult.Ex
}
```

## Recommended Update Flow
```csharp
var updateResult = await forms.UpdateCurrentRecordAsync("ORDERS");
if (updateResult.Flag != Errors.Ok)
{
    // handle update failure
}
```

## Rules
1. Prefer enhanced async methods over ad-hoc direct UnitOfWork reflection in callers.
2. Keep records in mode-appropriate flow (CRUD for insert/update).
3. Run through `FormsManager` so relationship synchronization and status logging are preserved.
4. Treat `Errors.Warning` distinctly from `Errors.Failed` when querying.
5. Keep field copy lists explicit when using `CopyFields`.

## Pitfalls
- Calling update when no current record is selected.
- Ignoring unsaved-change checks before inserts in related blocks.
- Assuming type resolution always succeeds for dynamic entity names.
- Bypassing helper methods and duplicating reflection logic in external code.

## Verification Checklist
- Block exists and `UnitOfWork` is not null.
- Mode is valid for target operation.
- Validation hooks pass (`ValidateField` / `ValidateRecord` path).
- Relationship sync runs after successful DML on master blocks.
- `Status` and log messages are present for success/failure diagnostics.

