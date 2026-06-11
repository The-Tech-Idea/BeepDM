# FormsManager — Triggers

This document covers the trigger system: when triggers fire, what they can do, how they chain, and the audit log.

## Overview

The trigger system is the closest analog to Oracle Forms' WHEN-* / KEY-* / PRE-* / POST-* triggers. The engine implements **39 trigger types** and **54 trigger points** across the orchestrator and the helper events. See [`ORACLE-FORMS-MAPPING.md`](../ORACLE-FORMS-MAPPING.md) section 7 for the full list and section 17 for the related system variables.

## How triggers are organized

### Trigger types (the kinds of triggers)

Triggers are categorized by what they fire on:

| Category | Examples | Where fired |
| --- | --- | --- |
| **Form lifecycle** | `WHEN-NEW-FORM-INSTANCE` | `FormsManager.OnFormOpen` / `OnFormClose` / `OnFormCommit` / etc. |
| **Block lifecycle** | `WHEN-NEW-BLOCK-INSTANCE` | `SwitchToBlockAsync`, `EnterQueryModeAsync`, etc. |
| **Record lifecycle** | `WHEN-NEW-RECORD-INSTANCE` | Navigation, mode transitions, record insertion. |
| **Validation** | `WHEN-VALIDATE-ITEM` / `WHEN-VALIDATE-RECORD` / `WHEN-VALIDATE-FORM` | `ValidationManager` on each rule timing. |
| **Query** | `WHEN-*-POST-QUERY` / `WHEN-*-PRE-QUERY` | `ExecuteQueryAsync` family. |
| **DML** | `WHEN-*-PRE-INSERT` / `WHEN-*-POST-INSERT` / etc. | `InsertRecordAsync`, `UpdateCurrentRecordAsync`, `DeleteCurrentRecordAsync`. |
| **Transaction** | `WHEN-*-PRE-COMMIT` / `WHEN-*-POST-COMMIT` | `CommitFormAsync`, `CommitFormBatchAsync`. |
| **Timer** | `WHEN-TIMER-EXPIRED` | `TimerManager.TimerFired`. |
| **Error** | `ON-ERROR` | `EventManager.OnError`. |
| **Key** | `KEY-F1`, `KEY-EXIT`, etc. | `RegisterKeyTrigger` + `FireKeyTriggerAsync`. |

### Trigger DTOs

Triggers are represented by:

- `TriggerDefinition` — the trigger's name, type, scope (form/block/item), source rule.
- `TriggerContext` — the per-trigger execution context (current block, current item, current record, the rule engine inputs).
- `TriggerResult` — the result of firing a trigger (succeeded/failed, cancelled, message).
- `TriggerExecutingEventArgs` / `TriggerExecutedEventArgs` — the events raised before/after firing.
- `TriggerEventArgs` — the older base event args (still in use for some legacy code paths).

## How triggers are registered

The engine does NOT have a centralized "register a trigger" API like `SET_TRIGGER('WHEN-NEW-BLOCK-INSTANCE', 'my_handler')`. Instead, triggers are registered **per concern**:

- **Form lifecycle** — `FormsManager.OnFormOpen += handler;` etc.
- **Block lifecycle** — `FormsManager.OnBlockFieldChanged += handler;`
- **DML** — `FormsManager.OnPreInsert += handler;` etc.
- **Custom triggers** — `TriggerManager.RegisterTrigger(definition, asyncCallback)` (low-level).
- **Key triggers** — `FormsManager.RegisterKeyTrigger(KeyTriggerDescriptor)`.
- **Validation** — `FormsManager.Validation.Rules.Add(ValidationRule)`.

The orchestrator-level events are the public trigger surface for the common cases. For custom trigger types or per-block triggers, use the `TriggerManager` directly.

## How triggers fire

### The lifecycle of a single trigger fire

1. **Triggering action** — a navigation, mode change, validation, etc.
2. **`OnPre*` event fires** — the BEFORE event (cancellable). Handler can set `args.Cancel = true`.
3. **`TriggerManager.FireTriggerAsync(triggerType, context)`** — the actual trigger body runs.
4. **`OnPost*` event fires** — the AFTER event (not cancellable).
5. **Trigger log updated** — the fire is recorded in `TriggerExecutionLog`.

For a `FireTriggersInOrderAsync(orderedTriggers, context)`, the steps above repeat for each trigger. The `TriggerDependencyManager` is consulted to verify the order and detect cycles.

### The `TriggerExecuting` / `TriggerExecuted` events

`TriggerManager` raises these for **every** trigger fire (regardless of the trigger type). They're a unified hook for any UI that wants to observe trigger activity. The `BeepForms` host uses these to dispatch triggers to the host's UI-specific event subscribers.

### Cancellable triggers

Most `OnPre*` events are cancellable. The `OnPost*` events are not — by the time the action has happened, cancelling is meaningless.

To cancel a trigger programmatically:

```csharp
manager.OnFormOpen += (sender, args) =>
{
    if (someCondition)
    {
        args.Cancel = true;
        args.Message = "Open cancelled by my code";
    }
};
```

The triggering method sees `args.Cancel == true` and returns `false` / `Errors.Failed` accordingly.

## WHEN-NEW-* triggers

The most-fired triggers. They fire:

- `WHEN-NEW-FORM-INSTANCE` — `OnFormOpen` event + `IBeepBuiltins.OpenForm` (host-routed).
- `WHEN-NEW-BLOCK-INSTANCE` — `OnBlockEnter` event + `_triggerManager.FireBlockTriggerAsync(...)` called from `SwitchToBlockAsync`.
- `WHEN-NEW-RECORD-INSTANCE` — `OnRecordEnter` event.
- `WHEN-NEW-ITEM-INSTANCE` — fired on item navigation, but via `OnItemValueChanged` rather than a dedicated `OnItemEnter` event. (See [gaps](../gaps.md).)

## KEY- triggers

`FormsManager.RegisterKeyTrigger(descriptor)` and `FireKeyTriggerAsync(keyName, ...)`. The engine does NOT bind keys to actual keyboard input — that's a host UI concern. The host fires the trigger when the user presses the key.

The standard Oracle Forms KEY- names are recognized: `KEY-F1` through `KEY-F12`, `KEY-EXIT`, `KEY-COMMIT`, `KEY-ROLLBACK`, `KEY-ENTER`, `KEY-TAB`, etc. Custom keys can be registered with any name.

## Trigger chaining

For complex forms with multiple triggers that depend on each other, the engine provides:

- `TriggerManager.FireTriggersInOrderAsync(triggers, context)` — fires a list of triggers in order. Each trigger's return value is checked before proceeding.
- `TriggerDependencyManager` — tracks explicit dependencies between triggers (e.g. "WHEN-VALIDATE-ITEM must run before WHEN-VALIDATE-RECORD"). Cycle detection.
- `TriggerChainCompletedEventArgs` — raised when a chain completes successfully or fails.

The dependency manager is **opt-in** — most forms don't need it. Use it when you have a multi-step trigger flow where order matters.

## Trigger audit log

Every trigger fire is logged via `TriggerExecutionLog`. The orchestrator exposes:

- `FormsManager.TriggerLog` (property) → `ITriggerExecutionLog`.
- `FormsManager.GetTriggerLog()` → `IReadOnlyList<TriggerExecutionLogEntry>`.
- `FormsManager.ClearTriggerLog()`.

Each log entry contains:

- The trigger name.
- The block / record / item that was current when it fired.
- The start time, end time, duration.
- The result (succeeded / failed / cancelled).
- Any error message.
- Whether the trigger was a `Pre*` or `Post*` (or `On*`).

The log is **in-memory only** — it does not persist across process restarts. For a persisted audit trail, use the `AuditManager` (which has file/in-memory stores).

## Trigger failure semantics

### What happens when a trigger fails

If a trigger body throws, the engine catches it and:

1. The `TriggerResult` has `Success = false` and the exception message.
2. The triggering action (e.g. `CommitFormAsync`) sees the failure and returns `Errors.Failed`.
3. The transaction state depends on the trigger type:
   - **Pre-* trigger failed** — the action is cancelled. The transaction state is unchanged. (E.g. `OnPreInsert` failed → insert is cancelled.)
   - **Post-* trigger failed** — the action happened but the post-hook failed. The transaction state may be inconsistent. Caller must check.
   - **Validation trigger failed** — the validation error is collected; commit is blocked if any error-severity validation fails.
4. The trigger log records the failure.

### `RAISE_FORM_TRIGGER_FAILURE`

`IBeepBuiltins.RaiseFormTriggerFailure(failureCode, message)` throws a `BeepBuiltinException` with a Forms-style code (e.g. `FRM-41003`). The exception is caught by the triggering action, recorded in the trigger log, and the action returns `Errors.Failed` with the message in the result.

Use this for custom trigger validation that needs to fail with a specific error code.

## WHEN-CUSTOM-ITEM-EVENT (partial)

Oracle Forms has a `WHEN-CUSTOM-ITEM-EVENT` trigger that fires for host-defined events. The engine does NOT have a direct equivalent. The closest are:

- `OnItemValueChanged` — fires on any value change.
- `OnItemErrorChanged` — fires on validation error state change.
- `OnError` — fires on any error.

A host UI that needs to dispatch custom events should subscribe to these and dispatch accordingly. See [gaps](../gaps.md) for the long-term fix.

## See also

- [`ORACLE-FORMS-MAPPING.md`](../ORACLE-FORMS-MAPPING.md) section 7 — the full list of trigger types and their mapping.
- [`architecture.md`](../architecture.md) — where `TriggerManager` sits in the helper layer.
- [`navigation.md`](navigation.md) — `WHEN-NEW-RECORD-INSTANCE` fires on navigation.
- [`mode-transitions.md`](mode-transitions.md) — `WHEN-NEW-BLOCK-INSTANCE` fires on mode transitions.
- [`audit.md`](audit.md) — the persisted audit trail, which is different from the in-memory trigger log.
- [`validation.md`](validation.md) — `WHEN-VALIDATE-ITEM` / `WHEN-VALIDATE-RECORD` triggers.
