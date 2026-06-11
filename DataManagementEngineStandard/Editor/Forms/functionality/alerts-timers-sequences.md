# FormsManager — Alerts, Messages, Timers, Sequences

This document covers the small "utility" subsystems that the engine owns: alerts and messages (UI-routed), timers, and sequences. Plus the system variables they touch.

## Alerts and messages

### `SetMessage(string text, MessageLevel level)` and `ClearMessage()`

```csharp
manager.SetMessage("Order saved successfully.", MessageLevel.Info);
manager.SetMessage("Invalid quantity.", MessageLevel.Error);
manager.ClearMessage();
```

A simple status-bar update. The engine stores the message in `_currentStatusMessage` (exposed via `manager.CurrentMessage`). The host UI subscribes to the `OnFormOpen` / `OnFormClose` events (which see the message) or polls `CurrentMessage`.

The MessageLevel enum:
- `Hint` (level 0) — subtle, fades out fast.
- `Info` (level 5) — neutral info.
- `Warning` (level 10) — yellow, persists.
- `Error` (level 15) — red, persists until user dismisses.

### `ShowAlertAsync(title, message, style, button1, button2?, button3?)`

```csharp
var result = await manager.ShowAlertAsync(
    title: "Confirm save",
    message: "Save all pending changes?",
    style: BeepBuiltinAlertStyle.Caution,
    button1: "Yes",
    button2: "No",
    button3: "Cancel");

switch (result.ButtonClicked)
{
    case 1: // Yes
        await manager.CommitFormAsync();
        break;
    case 2: // No
        await manager.RollbackFormAsync();
        break;
    case 3: // Cancel
        // do nothing
        break;
}
```

Returns an `AlertResult` with the clicked button index. Up to 3 buttons; the third is optional. The engine calls back through `IBuiltinHost.ShowAlertAsync` to render the dialog (the host UI owns the actual modal).

`BeepBuiltinAlertStyle` enum:
- `Info` — informational, blue icon.
- `Caution` — warning, yellow icon.
- `Stop` — error, red icon.
- `Note` — note, no icon (Oracle Forms style).

### `ConfirmAsync(title, message)` — two-button shortcut

```csharp
if (await manager.ConfirmAsync("Save", "Save all pending changes?"))
{
    await manager.CommitFormAsync();
}
```

A shortcut for the common two-button case. Renders as an `Info` style with `Yes` / `No` buttons. Returns `true` if the user clicked the first button, `false` otherwise.

### `ShowInfoAsync(title, message)` — info-only shortcut

A shortcut for a one-button info alert. Returns the `AlertResult` (the button index is always 1).

### `IBeepBuiltins.AlertAsync(...)` and `Message(...)`

The host-facing built-ins. See [`builtins.md`](builtins.md).

### `IBeepBuiltins.Message` severity mapping

| `BeepBuiltinMessageSeverity` | Numeric level | Oracle Forms equivalent |
| --- | --- | --- |
| `Hint` | 0 | HINT |
| `Info` | 1 | INFO |
| `Warning` | 2 | WARNING |
| `Error` | 3 | ERROR |

`IBeepBuiltins.Message` takes a numeric `ack` parameter (0-25) and a `severity` parameter. The host can use the numeric form for compatibility with Oracle Forms code; the engine translates to the enum.

## Timers

### `CreateTimer(name, interval, repeating)`

```csharp
var timer = manager.CreateTimer(
    timerName: "REFRESH_ORDERS",
    interval: TimeSpan.FromSeconds(30),
    repeating: true);
```

Creates a timer that fires `WHEN-TIMER-EXPIRED` every `interval` (or once if `repeating = false`). Returns a `TimerDefinition` (which is the timer's metadata + handle).

### `DeleteTimer(name)` / `GetTimer(name)` / `GetAllTimers()` / `TimerExists(name)`

Standard CRUD on timers. `GetTimer` returns the `TimerDefinition` (or null if not found). `GetAllTimers` returns all live timers. `TimerExists` is the bool check.

### Timer events

`TimerManager.TimerFired` raises when a timer expires. Args (`TimerFiredEventArgs`) include:

- The timer name.
- The timer definition (interval, repeating, started-at).
- The current time.

A host can subscribe to this event to react to timer expiry. The engine does not know what the host does with the event — it's a pure notification.

### `:SYSTEM.TIMER`

The currently-firing timer is set in `:SYSTEM.TIMER`. The trigger body that fires from the timer can read this to know which timer expired.

### Timer persistence

Timers are **in-memory** — they don't survive a process restart. If the host needs persistent timers, it must register them on startup.

### Timer thread safety

`TimerManager` uses a single internal timer thread. The `TimerFired` event is raised on that thread. If a host subscribes and does heavy work in the handler, it can starve the timer thread. Hosts should marshal to a worker thread or UI thread as appropriate.

## Sequences

### `GetNextSequence(name)` / `PeekNextSequence(name)`

```csharp
var next = manager.GetNextSequence("ORDER_SEQ");  // returns and advances
var peeked = manager.PeekNextSequence("ORDER_SEQ"); // returns without advancing
```

`GetNextSequence` is `:SEQUENCE.NEXTVAL`. `PeekNextSequence` is `:SEQUENCE.CURRVAL` (but always returns the value that *would* be next, not the value last returned; this is a slight Oracle Forms divergence).

### `CreateSequence(name, startValue, incrementBy)` / `ResetSequence(name, newValue)`

```csharp
manager.CreateSequence("ORDER_SEQ", startValue: 1000, incrementBy: 1);
manager.ResetSequence("ORDER_SEQ", newValue: 1);
```

Creates a new sequence or resets an existing one to a new starting value. Resetting is irreversible — the old values are gone.

### `ISequenceProvider` — pluggable backing store

The default implementation is in-memory. You can inject a custom `ISequenceProvider` (e.g. one backed by a database sequence) via the constructor. The engine then delegates `:SEQUENCE.NEXTVAL` to the database.

**The engine rules say: prefer datasource-backed sequences when available.** This means:
- If the block's datasource owns identity (auto-increment), don't use a FormsManager sequence.
- If the datasource supports `SELECT NEXT VALUE FOR my_seq`, use that.
- Use FormsManager sequences for Oracle-style built-ins, deterministic tests, or non-database scenarios.

### `SetItemDefault(blockName, itemName, defaultFactory)` / `ClearItemDefault` / `ApplyItemDefaults`

```csharp
manager.SetItemDefault("ORDERS", "Status", () => "Draft");
manager.SetItemDefault("ORDERS", "CreatedOn", () => DateTime.UtcNow);

var newOrder = manager.CreateNewRecord("ORDERS");  // record with Status="Draft", CreatedOn=now
manager.ApplyItemDefaults("ORDERS", newOrder);   // apply to an existing record
manager.ClearItemDefault("ORDERS", "Status");
```

`SetItemDefault` registers a **lazy** default-value factory. `ApplyItemDefaults` invokes all registered factories for a block and sets the corresponding field values on the record. `CreateNewRecord` calls `ApplyItemDefaults` automatically.

Use this for "set the default status to Draft when the user creates a new order" scenarios. The factory is invoked **once per record creation**, not on every navigation.

### `CopyFieldValue(srcBlock, srcField, targetBlock, targetField)`

```csharp
manager.CopyFieldValue("CUSTOMERS", "ContactName", "ORDERS", "ContactName");
```

Sets the target's value to the source's value (current record of the source block → current record of the target block). Useful for "auto-fill related fields" UI patterns.

This is **not** the same as Oracle Forms' `COPY()` (which copies from a specific record to a specific record). The FormsManager version is a one-shot copy from the current record. For more control, use the helper's `CopyFields(sourceRecord, targetRecord, params fieldNames)` overload.

### `PopulateGroupFromBlock(blockName)`

```csharp
var record = manager.PopulateGroupFromBlock("ORDERS");
// record["Status"] = "Draft", record["CreatedOn"] = now, etc.
```

Returns a `Dictionary<string, object>` with the default values for a block (computed by invoking the default factories). Use this when you want the defaults as a separate dictionary, not applied to a record.

## System variables touched by these subsystems

| Subsystem | System variable | When set |
| --- | --- | --- |
| Timer | `:SYSTEM.TIMER` | When a timer fires. |
| Message | `:SYSTEM.MESSAGE_LEVEL` | When `SetMessage` is called. |
| Sequence | (none — sequences don't have a SYSTEM variable) | n/a |
| Alert | (none) | n/a |

## Notes for callers

- The alert dialog rendering is **host-routed** via `IBuiltinHost.ShowAlertAsync`. The engine does not know what the host's dialog looks like. The host is responsible for blocking the caller until the user responds (or for non-blocking the alert and reporting the result via an event).
- The message system is **not** a log. It holds **one current message**. The host UI is responsible for displaying the current message and clearing it when no longer needed.
- Timer expiry is **best-effort**. If the engine is busy, the timer may fire slightly after its scheduled time. The interval is the minimum gap, not an exact period.
- Sequence values are unique per `FormsManager` instance. If you have two `FormsManager` instances in the same process, they have independent sequence counters.
- Item defaults are **per-block**, not per-record. If you need per-record defaults (e.g. "the default customer is the current user"), use a custom default factory that reads the current context.

## See also

- [`architecture.md`](../architecture.md) — where the alert, timer, and sequence helpers sit.
- [`system-variables.md`](system-variables.md) — the full list of `:SYSTEM.*` variables.
- [`builtins.md`](builtins.md) — the host-facing built-ins for these subsystems.
- [`ORACLE-FORMS-MAPPING.md`](../ORACLE-FORMS-MAPPING.md) sections 11, 12, 13 — the alerts, sequences, and timers mapping.
