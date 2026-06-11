# FormsManager — System Variables (`:SYSTEM.*`)

This document covers the Oracle Forms `:SYSTEM.*` variables and their `FormsManager` counterparts.

## Overview

`SystemVariablesManager` is the engine's emulation of Oracle's `:SYSTEM.*` record. There are ~15 standard variables, each accessible as a property on the manager.

## The full list

| System variable | Type | What it tracks |
| --- | --- | --- |
| `:SYSTEM.CURSOR_BLOCK` | string | The name of the currently focused block. |
| `:SYSTEM.CURSOR_RECORD` | int | The 0-based index of the current record in the current block. |
| `:SYSTEM.CURSOR_ITEM` | string | The name of the currently focused item. |
| `:SYSTEM.CURSOR_VALUE` | object | The value of the currently focused item. |
| `:SYSTEM.MODE` | string | "ENTER-QUERY" or "NORMAL" (CRUD). |
| `:SYSTEM.BLOCK_STATUS` | string | "CHANGED", "NEW", or "QUERY" — the current block's dirty state. |
| `:SYSTEM.FORM_STATUS` | string | "CHANGED", "NEW", or "QUERY" — the current form's overall dirty state. |
| `:SYSTEM.LAST_RECORD` | bool | Whether the current record is the last in the block. |
| `:SYSTEM.LAST_QUERY` | string | The SQL of the most recent query. |
| `:SYSTEM.MESSAGE_LEVEL` | int | 0-25, the message severity. |
| `:SYSTEM.SUPPRESS_WORKING` | bool | True if "Working..." messages are suppressed. |
| `:SYSTEM.TIMER` | string | The name of the currently-firing timer. |
| `:SYSTEM.TRIGGER_BLOCK` | string | The block that owns the currently-firing trigger. |
| `:SYSTEM.TRIGGER_RECORD` | int | The record index in the trigger-owning block. |
| `:SYSTEM.TRIGGER_ITEM` | string | The item that owns the currently-firing trigger. |

## Access

```csharp
var cursorBlock = manager.SystemVariables.CursorBlock;     // :SYSTEM.CURSOR_BLOCK
var mode = manager.SystemVariables.Mode;                  // :SYSTEM.MODE
var timer = manager.SystemVariables.Timer;                 // :SYSTEM.TIMER
```

The `FormsManager.SystemVariables` property exposes the `ISystemVariablesManager` interface. All the variables above are accessible as properties on that interface.

## When each is updated

| Variable | When updated |
| --- | --- |
| `CURSOR_BLOCK` | `SwitchToBlockAsync` (and any block navigation). |
| `CURSOR_RECORD` | `NavigateWithValidationAsync` (and any record navigation). |
| `CURSOR_ITEM` | `GoItemAsync` (and any item navigation). |
| `CURSOR_VALUE` | `GoItemAsync` (after the new item is selected). |
| `MODE` | `EnterQueryModeAsync` / `ExecuteQueryAndEnterCrudModeAsync`. |
| `BLOCK_STATUS` | any DML (set to "CHANGED" on insert/update/delete). |
| `FORM_STATUS` | any DML on any block (set to "CHANGED" on the form's status). |
| `LAST_RECORD` | `NavigateToRecordAsync` (computed from `unitOfWork.Units.Count`). |
| `LAST_QUERY` | `ExecuteQueryAsync` (set to the executed SQL). |
| `MESSAGE_LEVEL` | `SetMessage` (and the equivalent built-ins). |
| `SUPPRESS_WORKING` | (currently no caller; reserved for future use). |
| `TIMER` | `TimerFired` event (set to the firing timer's name). |
| `TRIGGER_*` | any trigger fire (set to the trigger's context). |

## Reading inside triggers

A trigger body (registered via `TriggerManager.RegisterTrigger`) can read `:SYSTEM.*` via `manager.SystemVariables.X`. The variables reflect the **current state at trigger time**.

```csharp
manager.Triggers.RegisterTrigger(new TriggerDefinition
{
    Name = "WHEN-NEW-RECORD-INSTANCE",
    BlockName = "ORDERS",
    Callback = (ctx) =>
    {
        var currentBlock = ctx.Manager.SystemVariables.CursorBlock;
        var currentIndex = ctx.Manager.SystemVariables.CursorRecord;
        // ... do something with the current state ...
        return TriggerResult.Ok();
    }
});
```

## `:SYSTEM.BLOCK_STATUS` values

| Value | Meaning |
| --- | --- |
| `CHANGED` | The block has dirty records (one or more inserts/updates/deletes not yet committed). |
| `NEW` | The block is in "new record" mode (a record is being created, not yet committed). |
| `QUERY` | The block has no dirty state; the records are clean (or the block is in `EnterQuery`/`Query` mode with no records yet). |

The status is computed from the block's `IUnitofWork.IsDirty` plus the `Mode` and the presence of any uncommitted "new" record.

## `:SYSTEM.FORM_STATUS`

Aggregates the `BLOCK_STATUS` across all blocks. The form status is the **most permissive** of the block statuses:

- If any block is `NEW`, the form is `NEW`.
- Else if any block is `CHANGED`, the form is `CHANGED`.
- Else, the form is `QUERY`.

## `:SYSTEM.MODE` values

| Value | Meaning |
| --- | --- |
| `ENTER-QUERY` | At least one block is in `EnterQuery` mode. |
| `NORMAL` | All blocks are in `Crud`, `Query`, or `New` mode (i.e. normal operation). |

`:SYSTEM.MODE = "NORMAL"` covers the three "normal" modes; it's not specific to CRUD-only. (Oracle Forms treats CRUD as "Normal" too.)

## `:SYSTEM.LAST_QUERY`

The exact SQL string that was last executed by `ExecuteQueryAsync`. Useful for debugging ("what query did the engine run for this?") and for audit ("which query produced these records?").

The string is the **composed** query, with the actual parameter values substituted. For sensitive parameters, the engine masks the value in the query string (e.g. `WHERE Password = '***'`). This is configurable via `ISystemVariablesManager.SetMaskSensitiveColumns(true)`.

## `:SYSTEM.TRIGGER_*`

The three trigger-related variables are set **before** a trigger body runs. They describe the context the trigger is firing in. Useful for triggers that need to know "where am I?" before deciding what to do.

```csharp
WHEN-NEW-RECORD-INSTANCE:
    :SYSTEM.TRIGGER_BLOCK = the block that just got a new record
    :SYSTEM.TRIGGER_RECORD = the index of the new record
    :SYSTEM.TRIGGER_ITEM = null (item triggers are separate)
```

## Updates that are NOT immediate

Some `:SYSTEM.*` variables are updated **on a delay** (after the operation completes, not at the start). This is because the engine computes them from the post-operation state. The most common case:

- `:SYSTEM.BLOCK_STATUS` and `:SYSTEM.FORM_STATUS` are set after a DML operation completes successfully. If the operation fails, the status is NOT updated.
- `:SYSTEM.MODE` is set after a mode transition completes. If the transition is cancelled (e.g. `OnPreQuery` cancels), the mode is unchanged.

If a trigger body reads `:SYSTEM.BLOCK_STATUS` during a `OnPreInsert` handler, it sees the **pre-operation** status. If it reads it during `OnPostInsert`, it sees the **post-operation** status.

## Reading from the host

The host UI typically reads the variables through the helper property:

```csharp
// WinForms
private void UpdateStatusBar()
{
    statusLabel.Text = $"{manager.SystemVariables.CursorBlock} / " +
                       $"record {manager.SystemVariables.CursorRecord}";
}
```

For frequent updates (e.g. a status bar that updates on every navigation), subscribe to the events that cause the variable change and re-read on each event. The variables themselves are getters; they're not observable.

## Concurrency

`SystemVariablesManager` is thread-safe for reads (the values are stored in `volatile` or `Interlocked` fields). Writes happen on the caller's thread and are immediately visible to other threads (memory barrier on the writer side).

A multi-threaded caller that needs a consistent snapshot of multiple variables should call a snapshot method (if available) or use a lock around the read.

## Notes for callers

- The `FormsManager.SystemVariables` property is the public surface. Direct access to the `SystemVariablesManager` is also fine.
- The `SYSTEM.*` variables are **not** a substitute for proper state management. If you need to know "is this block dirty?", call `FormsManager.GetDirtyBlocks()` — don't infer it from `:SYSTEM.BLOCK_STATUS`.
- The `SYSTEM.LAST_QUERY` value is the engine's composed SQL. It may differ from what your datasource would render (e.g. case differences, parameter substitution order). Use it for debugging, not for SQL parsing.
- The `TRIGGER_*` variables are only set during trigger execution. Outside a trigger body, they hold the last-trigger value, not the current state.

## See also

- [`architecture.md`](../architecture.md) — where `SystemVariablesManager` sits in the helper layer.
- [`alerts-timers-sequences.md`](alerts-timers-sequences.md) — the subsystems that update these variables.
- [`triggers.md`](triggers.md) — how trigger bodies use these variables.
- [`ORACLE-FORMS-MAPPING.md`](../ORACLE-FORMS-MAPPING.md) section 17 — the system variables mapping.
