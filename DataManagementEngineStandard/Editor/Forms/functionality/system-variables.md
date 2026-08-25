# FormsManager — System Variables (`:SYSTEM.*`)

This document covers the Oracle Forms `:SYSTEM.*` variables and their `FormsManager` counterparts.

## Overview

`SystemVariablesManager` (`Editor/Forms/Helpers/SystemVariablesManager.cs`, contract
`ISystemVariablesManager` in `Editor/Forms/Interfaces/ICoreHelpers.cs`) is the engine's emulation
of Oracle's `:SYSTEM.*` record. Unlike a flat set of properties on the manager itself, it hands back
a `SystemVariables` snapshot object — one **form-level** instance plus one **per block** — each
carrying 23 `UPPER_SNAKE_CASE` public fields that mirror the Oracle Forms `:SYSTEM.*` names, so
trigger authors read `sysVars.CURSOR_RECORD` the same way they would read `:SYSTEM.CURSOR_RECORD`
in real Oracle Forms.

*(This doc previously described a different, PascalCase-property shape —
`manager.SystemVariables.CursorBlock`, `.Mode`, `.Timer`, a `SetMaskSensitiveColumns(...)` method,
and a lambda-based `Callback = (ctx) => ...` trigger registration returning `TriggerResult.Ok()`.
None of that exists anywhere in this codebase; it was corrected 2026-08-25 to describe the actual
implementation below, verified directly against `SystemVariables.cs`, `SystemVariablesManager.cs`,
`ICoreHelpers.cs` and the real trigger-handler shape `DesignerHandlerScaffolder` emits.)*

## The full list

`SystemVariables` (`Editor/Forms/Models/SystemVariables.cs`) carries these fields:

| Field | Type | What it tracks |
| --- | --- | --- |
| `CURRENT_BLOCK` | string | The name of the currently focused block. |
| `CURRENT_ITEM` | string | The name of the currently focused item. |
| `CURRENT_FORM` | string | The current form's name. |
| `CURSOR_ITEM` | string | `"{block}.{item}"` for the currently focused item. |
| `CURSOR_VALUE` | object | The value of the currently focused item. |
| `CURSOR_RECORD` | int | The 1-based index of the current record in the block. |
| `LAST_RECORD` | int | The record count in the block (not a "is this the last record" flag). |
| `RECORDS_DISPLAYED` | int | Records currently displayed for the block. |
| `MODE` | string | `"NORMAL"` or a mode name the caller sets via `SetMode`. |
| `BLOCK_STATUS` | string | `"NEW"` / `"CHANGED"` / caller-set, per block. |
| `FORM_STATUS` | string | `"NEW"` / `"CHANGED"` / caller-set, form-wide. |
| `RECORD_STATUS` | string | `"NEW"` / caller-set, per block. |
| `MASTER_BLOCK` | string | This block's master block name, when it is a detail. |
| `TRIGGER_TYPE` | string | The type of the currently-firing trigger. |
| `TRIGGER_FORM` | string | The form the currently-firing trigger belongs to. |
| `TRIGGER_BLOCK` | string | The block the currently-firing trigger belongs to. |
| `TRIGGER_ITEM` | string | `"{block}.{item}"` for the currently-firing item trigger. |
| `TRIGGER_FIELD` | string | The item name alone (mirrors `TRIGGER_ITEM` in `UpdateBlockVariables`). |
| `TRIGGER_RECORD` | int | The 1-based record index the currently-firing trigger applies to. |
| `LAST_QUERY` | string | The most recent query string set via `SetLastQuery`. |
| `LAST_ERROR` | string | The most recent error message set via `SetLastError`. |
| `LAST_ERROR_CODE` | int | The most recent error code set via `SetLastError`. |
| `LAST_OPERATION_TIME` | DateTime | Timestamp of the last write to this snapshot. |

`SystemVariables.ToSnapshot()` returns all of the above (except `TRIGGER_FIELD`) as a
`IReadOnlyDictionary<string, object>`, keyed by field name.

**Not implemented** — real Oracle Forms `:SYSTEM.*` members with no counterpart here:
`MESSAGE_LEVEL`, `SUPPRESS_WORKING`, `TIMER`, `CURSOR_BLOCK` (this engine's nearest equivalent is
`CURRENT_BLOCK`), and the remainder of Oracle's ~90-variable set beyond the 23 above (coordination,
effective-date, and various display-attribute variables). Extending this is a real, scoped
future task — see the note at the end of this document.

## Access

```csharp
// Form-level snapshot
var formVars = manager.SystemVariables.GetFormSystemVariables();
var currentBlock = formVars.CURRENT_BLOCK;      // :SYSTEM.CURRENT_BLOCK-equivalent

// Per-block snapshot (created lazily on first access)
var orderVars = manager.SystemVariables.GetSystemVariables("Orders");
var cursorRecord = orderVars.CURSOR_RECORD;     // :SYSTEM.CURSOR_RECORD-equivalent
```

`IUnitofWorksManager.SystemVariables` (implemented by `FormsManager.SystemVariables`) exposes the
`ISystemVariablesManager` interface. There is no flat per-variable property on the manager itself —
always go through `GetFormSystemVariables()` or `GetSystemVariables(blockName)` first.

## When each is updated

| Field | When updated |
| --- | --- |
| `CURRENT_BLOCK` | `UpdateForBlockChange(blockName)` — form-level and the block's own snapshot. |
| `CURSOR_RECORD`, `LAST_RECORD`, `RECORDS_DISPLAYED` | `UpdateForRecordChange(blockName, recordIndex, totalRecords)`; also opportunistically inside `UpdateForBlockChange` from the block's live `IUnitofWork`. |
| `CURRENT_ITEM`, `CURSOR_ITEM`, `CURSOR_VALUE` | `UpdateForItemChange(blockName, itemName, itemValue)`. |
| `MASTER_BLOCK` | `UpdateForBlockChange`, when the block has a registered master. |
| `MODE` | `SetMode(mode)` — form-level, and the current block's snapshot. |
| `BLOCK_STATUS` | `SetBlockStatus(blockName, status)`; a `"CHANGED"` status also sets `FORM_STATUS`. |
| `FORM_STATUS` | `SetFormStatus(status)`, or implicitly via `SetBlockStatus("CHANGED")`. |
| `RECORD_STATUS` | `SetRecordStatus(blockName, status)` — form-level and the block's snapshot. |
| `TRIGGER_TYPE`, `TRIGGER_FORM`, `TRIGGER_BLOCK`, `TRIGGER_ITEM`, `TRIGGER_RECORD` | ✅ **live** — `TriggerManager.ExecuteTriggerChain`/`ExecuteTriggerChainAsync` call `SetTriggerContext(...)` before every trigger chain runs and `ClearTriggerContext()` after, for all ten `Fire*Trigger(Async)` variants (Form/Block/Item/Global × sync/async). Also populates `context.SystemVariables` itself, previously always null. |
| `LAST_ERROR`, `LAST_ERROR_CODE` | `SetLastError(message, code)` / cleared by `ClearLastError()`. **Not yet called anywhere.** |
| `LAST_QUERY` | `SetLastQuery(queryString)`. **Not yet called anywhere.** |
| `CURRENT_FORM` | `SetCurrentForm(formName)`. **Not yet called anywhere.** |
| everything | `Reset()` returns the form-level snapshot and the per-block cache to their construction-time defaults. |

**Only `SetTriggerContext`/`ClearTriggerContext` have a caller today (wired 2026-08-25) — the other
eight `Set*`/`UpdateFor*` methods still have none anywhere in `Editor/Forms`,** confirmed by grepping
`manager.SystemVariables.` / `SystemVariables.` for each method name. Trigger-firing had one natural
choke point (`TriggerManager`'s two internal chain-execution methods, which every `Fire*Trigger(Async)`
variant funnels through) — the rest do not: `UpdateForBlockChange`/`UpdateForRecordChange`/
`UpdateForItemChange`/`SetMode`/`SetBlockStatus`/`SetFormStatus`/`SetRecordStatus` each need their own
call site scattered across `FormsManager`'s block-switch, record-navigation, item-focus,
mode-transition, DML and query-execution code — nothing in that code calls into
`SystemVariablesManager` for those eight yet. `CURRENT_BLOCK`, `CURSOR_RECORD`, `MODE`,
`BLOCK_STATUS`/`FORM_STATUS`/`RECORD_STATUS`, `LAST_QUERY`, `LAST_ERROR`(`_CODE`), `CURRENT_FORM` are
all still permanently whatever `SystemVariables`'s constructor set, regardless of what the form does —
only the five `TRIGGER_*` fields are genuinely live now. Wiring the rest is real, valuable,
scoped-per-call-site work — genuinely separate from this documentation correction, and not
attempted here. Check current call sites with `grep` before relying on any specific field being live.

## A dedicated per-block snapshot, separate from the lazy one

`UpdateBlockVariables(blockName, masterBlockName, mode, cursorRecord, lastRecord, recordsDisplayed,
isQueryMode, isDirty, triggerItem, activeTrigger)` writes into a **second**, independent per-block
store (`_blockVars`, read back via `GetBlockVariables(blockName)`) — separate from the
`GetSystemVariables`-backed one above, which is lazily created on any access. `BLOCK_STATUS` here is
derived (`"Query"` / `"Changed"` / `"Normal"`) rather than caller-supplied. This exists so a runtime
host (`BeepDataBlock` or similar) can read a rich block snapshot without going back through
`FormsManager` directly. `GetBlockVariables` on a block with no snapshot yet returns a fresh, empty
`SystemVariables()`, not null.

## Reading inside triggers

A trigger handler is a real C# method the IDE scaffolds onto the form's own partial class
(`DesignerHandlerScaffolder.AddTriggerHandler`), with the signature the engine's
`Func<TriggerContext, TriggerResult>` requires:

```csharp
private TriggerResult OnValidateQty(TriggerContext context)
{
    // Live: TriggerManager sets these on every trigger fire (see "When each
    // is updated" above) before this handler runs.
    var triggerBlock = context.SystemVariables.GetFormSystemVariables().TRIGGER_BLOCK;
    var triggerRecord = context.SystemVariables.GetFormSystemVariables().TRIGGER_RECORD;

    // Not yet live: CURRENT_BLOCK/CURSOR_RECORD/etc. are still whatever
    // SystemVariables's constructor set — nothing updates them yet (see the
    // "When each is updated" table). Reading them today will not throw, but
    // will not reflect the actual current block/record either.
    var currentBlock = context.SystemVariables.GetFormSystemVariables().CURRENT_BLOCK;

    // ... do something with the current state ...
    return TriggerResult.Success;
}
```

`TriggerContext.SystemVariables` (`Editor/Forms/Models/TriggerContext.cs`) is the same
`ISystemVariablesManager` the form's own `manager.SystemVariables` is — the context just hands it
through so a handler does not need a separate reference to the manager (`TriggerManager` populates
this field itself now, right before the handler runs; it used to always be null). `TriggerResult` is
an enum (`Success`/`Failure`/… — check `TriggerResult.cs` for the full set), not a factory method:
return the member directly, never `TriggerResult.Ok()`.

## `BLOCK_STATUS` values (as set by `SetBlockStatus`)

| Value | Meaning |
| --- | --- |
| `CHANGED` | Passed explicitly by the caller (also forces `FORM_STATUS = "CHANGED"`). |
| `NEW` | The default/reset value. |

`SystemVariablesManager` does not itself compute `BLOCK_STATUS` from `IUnitofWork.IsDirty` — this is
caller-supplied state (via `SetBlockStatus`) or, in the separate `UpdateBlockVariables` snapshot, a
three-way `isQueryMode`/`isDirty` derivation (`"Query"`/`"Changed"`/`"Normal"`).

## Concurrency

All read/write access in `SystemVariablesManager` goes through a single `lock (_lockObject)`, not
`volatile`/`Interlocked` fields — a caller reading `SystemVariables` fields directly (they are plain
auto-properties) outside that lock can observe a torn read across multiple fields set by the same
`Update*`/`Set*` call. For a consistent multi-field snapshot, prefer `ToSnapshot()` (still not called
under the manager's lock, so pair it with your own locking if that matters for your use case) or
accept the fields individually as eventually-consistent.

## Notes for callers

- `manager.SystemVariables` is the one access path. There is no separate direct-construction route —
  `SystemVariablesManager`'s constructor takes the engine's own block dictionary, so it is only ever
  built by `FormsManager` itself.
- `GetSystemVariables(blockName)` creates and caches a new `SystemVariables` for a block name it has
  not seen before (seeded with just `CURRENT_BLOCK`) — it never returns null.
- The `TRIGGER_*` fields only reflect the *last* `SetTriggerContext`/`ClearTriggerContext` call, not a
  live "am I inside a trigger right now" flag — read them during trigger execution, not after.

## Extending this to a larger subset of Oracle's `:SYSTEM.*` set

Real Oracle Forms defines roughly 90 `:SYSTEM.*` variables; the 23 fields above cover the ones a
block/record/item/trigger-context emulation most directly needs, and were not chosen against the
full Oracle list. If picking this up as further work, treat it as a genuinely separate,
scoped-per-variable task — each addition needs real live-state wiring from a `FormsManager`
operation, the same way `CURSOR_RECORD` is wired from `UpdateForRecordChange`, not just a new field
that nothing ever sets.

## See also

- [`architecture.md`](../architecture.md) — where `SystemVariablesManager` sits in the helper layer.
- [`alerts-timers-sequences.md`](alerts-timers-sequences.md) — the subsystems that update these variables.
- [`triggers.md`](triggers.md) — how trigger bodies use these variables.
- [`ORACLE-FORMS-MAPPING.md`](../ORACLE-FORMS-MAPPING.md) section 17 — the system variables mapping.
