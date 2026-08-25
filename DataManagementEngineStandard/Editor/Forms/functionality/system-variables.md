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
| `CURRENT_BLOCK` | ✅ **live** — `UpdateForBlockChange(blockName)` is called from `SwitchToBlockAsync` (`FormsManager.Navigation.cs`, wired 2026-08-25) on every block switch, including through `GoBlockAsync`'s delegation. |
| `CURSOR_RECORD`, `LAST_RECORD`, `RECORDS_DISPLAYED` | ⚠️ **partially live** — `UpdateForRecordChange(blockName, recordIndex, totalRecords)` is called from `TryUpdateSavepointSystemVariables` (`FormsManager.BlockRegistration.cs:648`) after a savepoint rollback, but **not** from ordinary record navigation (`NextRecordAsync`/`PreviousRecordAsync`/etc. do not call it) — these fields go stale between rollbacks. Also opportunistically refreshed by `UpdateForBlockChange` from the block's live `IUnitofWork` on block entry. |
| `CURRENT_ITEM`, `CURSOR_ITEM`, `CURSOR_VALUE` | ✅ **live** — `UpdateForItemChange(blockName, itemName, itemValue)` is called from `GoItemAsync` (`FormsManager.Navigation.cs:406`) on every item-focus change. |
| `MASTER_BLOCK` | ✅ **live** — same `UpdateForBlockChange` call as `CURRENT_BLOCK` above, when the block has a registered master. |
| `MODE` | ✅ **live** — `SetMode(mode)` is called at all four sites that assign `blockInfo.Mode` directly (`EnterQueryModeAsync`, `EnterCrudModeForNewRecordAsync`, `CoordinateChildBlocksForNewMasterRecord` in `FormsManager.ModeTransitions.cs`; `ExecuteQueryEnhancedAsync` in `FormsManager.EnhancedOperations.cs`, wired 2026-08-25), mapped through `ToSystemVariableMode(DataBlockMode)` onto Oracle's real two-value vocabulary (`NORMAL`/`ENTER-QUERY`). |
| `BLOCK_STATUS` | ✅ **live for `"CHANGED"`, `"QUERY"`, and `"NEW"`** — `SetBlockStatus(blockName, "CHANGED")` is called from the block-registration `ItemChanged` handler (`FormsManager.BlockRegistration.cs`, wired 2026-08-25), confirmed never fired by query population, only real edits; a `"CHANGED"` status also cascades `FORM_STATUS`. `"QUERY"` is called from `ExecuteQueryEnhancedAsync` right after a successful `Get`/`Get(filters)` (wired 2026-08-25, unconditional — whether or not the query found rows, the same simplification `SetMode` already makes at that site). `"NEW"` is called from `EnterCrudModeForNewRecordAsync` right after `CreateNewRecord` succeeds (wired 2026-08-25). `"INSERT"` — Oracle's distinct status for a `"NEW"` record that has since been edited (as opposed to `"CHANGED"`, which Oracle reserves for an edited *queried* record) — is **not** wired: the current per-block `SystemVariables` snapshot has no per-record "was this row ever queried" state to key that distinction on, a genuinely bigger design question than the other three values. |
| `FORM_STATUS` | ✅ **live, implicitly** — every `SetBlockStatus(_, "CHANGED")` call cascades `FORM_STATUS = "CHANGED"` (see above). `SetFormStatus(status)` itself, for any other value, still has **no direct call site**. |
| `RECORD_STATUS` | ✅ **live for `"CHANGED"`, `"QUERY"`, and `"NEW"`** — same choke points as `BLOCK_STATUS` above (`ItemChanged` for `"CHANGED"`, `ExecuteQueryEnhancedAsync` for `"QUERY"`, `EnterCrudModeForNewRecordAsync` for `"NEW"`), all wired 2026-08-25. `"INSERT"` is not wired, same reason as `BLOCK_STATUS` above. |
| `TRIGGER_TYPE`, `TRIGGER_FORM`, `TRIGGER_BLOCK`, `TRIGGER_ITEM`, `TRIGGER_RECORD` | ✅ **live** — `TriggerManager.ExecuteTriggerChain`/`ExecuteTriggerChainAsync` call `SetTriggerContext(...)` before every trigger chain runs and `ClearTriggerContext()` after, for all ten `Fire*Trigger(Async)` variants (Form/Block/Item/Global × sync/async). Also populates `context.SystemVariables` itself, previously always null. |
| `LAST_ERROR`, `LAST_ERROR_CODE` | ✅ **live** — `SetLastError(message, code)` is called from the shared `protected void LogError(...)` helper (`FormsManager.Helpers.cs`, wired 2026-08-25), which every one of `FormsManager`'s 114+ `catch` blocks already reports failures through; `code` is `ex.HResult` (there is no Oracle-style `ORA-`/`FRM-` number available from a .NET exception). `ClearLastError()` is deliberately not called anywhere — real Oracle Forms has no "clear" semantic for this variable either; it just persists until the next error overwrites it. |
| `LAST_QUERY` | ✅ **live** — `SetLastQuery(queryText)` is called from `ExecuteQueryEnhancedAsync` (`FormsManager.EnhancedOperations.cs`, wired 2026-08-25) right after `UnitOfWork.Get(filters)`/`Get()` succeeds, using `DataSourceAppFilterExtensions.BuildSelectQueryDefinition`'s `QueryText` (a `"SELECT * FROM entity WHERE ..."` string built from the same `AppFilter` list). Best-effort: if the block's `DataSourceName` doesn't resolve to a real `IDataSource`, `LAST_QUERY` is simply left at its prior value — the query itself has already succeeded and is not failed for this. |
| `CURRENT_FORM` | ✅ **live** — `SetCurrentForm(formName)` is called from `CurrentFormName`'s property setter (`FormsManager.Properties.cs`) and from both `OpenFormAsync`/`CloseFormAsync` (`FormsManager.FormOperations.cs`, which set the backing field directly and so bypass the property) — three writers, all wired 2026-08-25. |
| everything | `Reset()` returns the form-level snapshot and the per-block cache to their construction-time defaults. |

**All ten `Set*`/`UpdateFor*` methods have a real caller today, except `SetFormStatus` as a direct
call (it is reached only implicitly, via `SetBlockStatus(_, "CHANGED")`'s cascade).**
*(An earlier version of this section claimed all ten had zero callers — that was a grep
mistake: it only matched calls through the public `manager.SystemVariables.` property, and
`FormsManager` calls the manager through its private field, `_systemVariablesManager.<name>(...)`,
instead. Corrected 2026-08-25 by re-grepping `_systemVariablesManager\.` directly against source.)*
Live: `SetTriggerContext`/`ClearTriggerContext` (wired 2026-08-25, one choke point in
`TriggerManager`), `UpdateForBlockChange` (wired 2026-08-25, one choke point in `SwitchToBlockAsync`),
`SetCurrentForm` (wired 2026-08-25, three writers — not one, but still a small, fully-enumerated set,
not "scattered"), `SetMode` (wired 2026-08-25, four writers across two files — the original three-site
count in `ModeTransitions.cs` missed a fourth in `EnhancedOperations.cs`), `SetLastError` (wired
2026-08-25, one genuine choke point — the shared `LogError` helper every catch block already reports
through), `SetBlockStatus`/`SetRecordStatus` (wired 2026-08-25 for the `"CHANGED"` value specifically —
the block-registration `ItemChanged` handler, confirmed never fired by query population; see below for
the earlier revert and how it was re-attempted and landed), `SetLastQuery` (wired 2026-08-25, the same
`ExecuteQueryEnhancedAsync` choke point `SetMode` already uses, once
`DataSourceAppFilterExtensions.BuildSelectQueryDefinition` was found to already provide the
filter-to-string serialization this was originally blocked on — see below), and `UpdateForItemChange`
(pre-existing, in `GoItemAsync`). Partially live: `UpdateForRecordChange` (pre-existing, but only from
savepoint rollback, not ordinary navigation).
Genuinely unwired: `SetFormStatus` as a direct call, and the `INSERT` value for
`BLOCK_STATUS`/`RECORD_STATUS` (`"CHANGED"`, `"QUERY"`, and `"NEW"` are all wired — see below).

**`SetLastQuery`: the "no existing serialization to reuse" premise was wrong — re-checked and
landed (2026-08-25).** The original pass found `ExecuteQueryEnhancedAsync`'s one natural landing
spot (right where it calls `blockInfo.UnitOfWork.Get(filters)`) but concluded the fix needed a new
filter-to-string serializer designed from scratch, since `ExecuteQueryEnhancedAsync` receives a
`List<AppFilter>`, not a WHERE-clause string. That conclusion was based on a grep that didn't cover
`DataManagementModelsStandard/Extensions/DataSourceAppFilterExtensions.cs`, where
`BuildSelectQueryDefinition(this IDataSource, entityNameOrSelect, filters, selectedColumns)` already
builds a full parameterized `"SELECT ... FROM ... WHERE ..."` string (plus a parameter dictionary)
from exactly this shape of input — a real, existing, general-purpose capability with zero callers
anywhere in the engine before this pass, not a Forms-specific one. `ExecuteQueryEnhancedAsync` now
resolves the block's `IDataSource` via `_dmeEditor.GetDataSource(blockInfo.DataSourceName)` (the same
pattern `FormsManager.Validation.cs` already uses) and calls `SetLastQuery(queryDefinition.QueryText)`
right after the query succeeds — best-effort: an unresolvable data source name leaves `LAST_QUERY`
unchanged rather than failing a query that already succeeded. Two new tests
(`ExecuteQueryEnhancedAsync_OnSuccess_SetsSystemVariablesLastQuery`,
`ExecuteQueryEnhancedAsync_UnresolvableDataSource_DoesNotSetLastQuery`), proven via revert.

**`SetBlockStatus`/`SetRecordStatus`'s `"CHANGED"` transition: found, prototyped, reverted on an
unexplained test interaction, re-attempted and landed (2026-08-25).** The first attempt wired
`SetBlockStatus(blockName, "CHANGED")`/`SetRecordStatus(blockName, "CHANGED")` into the `ItemChanged`
handler and compiled and passed its own new test in isolation, but made a pre-existing, unrelated test
(`ItemChanged_FieldHasLOV_FiresWhenLOVValidationTrigger`) fail consistently (3/3) whenever both tests
ran in the same suite; the mechanism was not root-caused (no `static` state found in `TriggerManager`
or `LOVManager`), so it was reverted rather than shipped — see G0.36 in `gaps.md` for that account in
full. The re-attempt used the identical wiring and choke point, plus a new direct test
(`ItemChanged_NoLov_SetsBlockAndRecordStatusToChanged`, deliberately exercising the no-LOV branch the
three `WHEN-LOV-VALIDATION` tests do not) — and did **not** reproduce the earlier failure: 25
consecutive full-suite runs (166/166) were green with the wiring in place, versus one confirmed red run
with it commented out (the new test alone fails predictably, "CHANGED" vs "NEW"). The original 3/3
reproduction was real but its exact cause was never identified and could not be reproduced again under
the same wiring and a comparable new test; it is recorded here rather than erased, in case a future
session hits the same symptom and needs the history.

**`BLOCK_STATUS`/`RECORD_STATUS`'s `"QUERY"` and `"NEW"` transitions: found and landed at the same
choke points `SetMode`/`SetLastQuery` already use (2026-08-25).** Once the `"CHANGED"` transition
was safely landed, re-checking `"NEW"`/`"QUERY"`/`"INSERT"` (rather than leaving them under the
original "genuinely larger, scoped-per-call-site" characterization) found that two of the three
needed no new investigation at all. `"QUERY"` — a record just fetched by a query and not yet
touched — shares `ExecuteQueryEnhancedAsync`'s existing hook (the same site `SetMode`/`SetLastQuery`
already call from), set unconditionally on a successful `Get`/`Get(filters)` regardless of row
count, the same simplification `SetMode` already makes there. `"NEW"` — a blank record just
created, not yet edited — shares `EnterCrudModeForNewRecordAsync`'s existing hook (right after
`CreateNewRecord` succeeds), which both the direct single-block path and
`CreateNewRecordInMasterBlockAsync`'s master-block delegation already funnel through. `"INSERT"`
(Oracle's status for a `"NEW"` record that has since been *edited* — distinct from `"CHANGED"`,
which Oracle reserves for an edited *queried* record) is **not** wired: distinguishing it needs
per-record "was this row ever queried" state, which the current per-block `SystemVariables` snapshot
does not carry — a genuinely bigger design question, left open. `SetFormStatus` as its own direct
call remains open too, on the strength of the original grep plus DML-verb ordering constraints —
not yet re-checked at this depth. Two new tests
(`ExecuteQueryEnhancedAsync_OnSuccess_SetsSystemVariablesQueryStatus`,
`EnterCrudModeForNewRecordAsync_OnSuccess_SetsSystemVariablesNewStatus`), proven via revert.
Check current call sites with `grep` (both the public property
*and* the private field — this section's own history is the reason why) before relying on any
specific field being live.

## A dedicated per-block snapshot, separate from the lazy one

`UpdateBlockVariables(blockName, masterBlockName, mode, cursorRecord, lastRecord, recordsDisplayed,
isQueryMode, isDirty, triggerItem, activeTrigger)` writes into a **second**, independent per-block
store (`_blockVars`, read back via `GetBlockVariables(blockName)`) — separate from the
`GetSystemVariables`-backed one above, which is lazily created on any access. `BLOCK_STATUS` here is
derived (`"Query"` / `"Changed"` / `"Normal"`) rather than caller-supplied. This exists so a runtime
host (`BeepDataBlock` or similar) can read a rich block snapshot without going back through
`FormsManager` directly. `GetBlockVariables` on a block with no snapshot yet returns a fresh, empty
`SystemVariables()`, not null.

**Checked 2026-08-25: `BeepDataBlock` is the pre-extraction legacy WinForms control** (Beep.Forms'
`WinFormsScanner.cs`/`CodeGenConstants.cs` both refer to it as "legacy" by name) that the Beep.Forms
extraction deliberately left behind — the current replacement, `WinFormBlockHost`, does not read this
snapshot. `UpdateBlockVariables`/`GetBlockVariables` have zero callers on both the write and read side
in this repo. Do not wire this in as if it were another `SetMode`-shaped gap: it would mean maintaining
a second, redundant per-block dictionary alongside `GetSystemVariables(blockName)` (house rule 3) for a
consumer that no longer exists here. See G0.36 in `gaps.md` for the full reasoning; left unwired and
undeleted pending a decision on whether to build a real consumer or retire it.

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

    // Also live: CURRENT_BLOCK/MASTER_BLOCK follow every block switch
    // (SwitchToBlockAsync), CURRENT_ITEM/CURSOR_ITEM/CURSOR_VALUE follow
    // every item-focus change (GoItemAsync). CURSOR_RECORD/LAST_RECORD are
    // only refreshed after a savepoint rollback, not ordinary navigation —
    // check the "When each is updated" table before relying on any specific
    // field being current.
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
| `CHANGED` | Passed explicitly by the caller (also forces `FORM_STATUS = "CHANGED"`); wired from the `ItemChanged` handler on a real field edit. |
| `NEW` | The `SystemVariables` constructor default, and also passed explicitly by `EnterCrudModeForNewRecordAsync` right after a blank record is created. |
| `QUERY` | Passed explicitly by `ExecuteQueryEnhancedAsync` right after a successful `Get`/`Get(filters)`. |

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
