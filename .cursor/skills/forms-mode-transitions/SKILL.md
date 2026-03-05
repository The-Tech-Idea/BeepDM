---
name: forms-mode-transitions
description: Detailed guidance for FormsManager mode transitions between Query and CRUD states, including validation, master-detail coordination, and safe record creation. Use when working with EnterQueryModeAsync, ExecuteQueryAndEnterCrudModeAsync, EnterCrudModeForNewRecordAsync, and related transition checks.
---

# Forms Mode Transitions

Use this skill when implementing or debugging mode-aware behavior in `FormsManager`.

## Additional Resources
- End-to-end scenarios: [reference.md](reference.md)

## Transition Surface
- `EnterQueryModeAsync(string blockName)`
- `ExecuteQueryAndEnterCrudModeAsync(string blockName, List<AppFilter> filters = null)`
- `EnterCrudModeForNewRecordAsync(string blockName)`
- `CreateNewRecordInMasterBlockAsync(string masterBlockName)`
- `ValidateAllBlocksForModeTransitionAsync()`
- `GetBlockMode(string blockName)`
- `GetAllBlockModeInfo()`
- `IsFormReadyForModeTransitionAsync()`

## Runtime Behavior
- Query entry validates unsaved changes and related blocks, clears block state for transition, then sets `DataBlockMode.Query`.
- Query execution requires Query mode first, executes query, validates results, and sets `DataBlockMode.CRUD`.
- New-record CRUD entry validates master-detail readiness and unsaved changes before record instantiation.
- Transition flow updates `Status`, writes operation logs, and triggers block/error events.

## Implementation Rules
1. Never force mode changes by directly mutating block fields from outside `FormsManager`.
2. Always call transition APIs and inspect `IErrorsInfo.Flag`.
3. For query execution, enter Query mode before `ExecuteQueryAndEnterCrudModeAsync`.
4. For inserts/updates, ensure block mode is CRUD or call transition helper first.
5. In master-detail forms, resolve parent context before detail record creation.

## Recommended Flow
```csharp
var enterResult = await forms.EnterQueryModeAsync("CUSTOMERS");
if (enterResult.Flag != Errors.Ok)
{
    return;
}

var queryResult = await forms.ExecuteQueryAndEnterCrudModeAsync(
    "CUSTOMERS",
    filters: customerFilters);
if (queryResult.Flag != Errors.Ok && queryResult.Flag != Errors.Warning)
{
    return;
}
```

## New Record Flow
```csharp
var crudResult = await forms.EnterCrudModeForNewRecordAsync("ORDERS");
if (crudResult.Flag != Errors.Ok)
{
    return;
}

var insertResult = await forms.InsertRecordEnhancedAsync("ORDERS");
if (insertResult.Flag != Errors.Ok)
{
    // inspect insertResult.Message and insertResult.Ex
}
```

## Failure Patterns To Watch
- Calling execute-query transition while block is still in CRUD mode.
- Entering CRUD for detail block without a valid current master record.
- Ignoring warning states (`Errors.Warning`) after query result validation.
- Creating records when unresolved unsaved changes exist in related blocks.

## Debug Checklist
- Confirm block exists and has `UnitOfWork`.
- Verify current mode with `GetBlockMode`.
- Check unsaved blocks list before transition.
- Validate master-detail relation is active and key fields are correct.
- Inspect logs for transition cancellation by events/triggers.

