# FormsManager — Multi-form and Globals

This document covers the multi-form registry, the inter-form messaging bus, the shared-block manager, and the `:GLOBAL.*` analog. Plus `CALL_FORM` / `OPEN_FORM` / `NEW_FORM` / `RETURN` semantics.

## The three concepts

| Concept | Use case | Engine surface |
| --- | --- | --- |
| **Form registry** | Track all open forms, their lifecycle events, and their parameters. | `IFormRegistry` |
| **Message bus** | Send a typed message from one form to another. Forms subscribe by message type. | `IFormMessageBus` |
| **Shared blocks** | A block that is logically one block, but can be accessed from multiple forms. With a lock so only one form can edit at a time. | `ISharedBlockManager` |

Plus the globals (`:GLOBAL.*` analog): a key-value store that any form can read/write.

## `IFormRegistry`

The form registry tracks every form opened through the engine. It exposes:

- `OpenForm(formName, parameters?)` — register a form as open.
- `CloseForm(formName)` — unregister.
- `GetOpenForms()` — the current list.
- `FormLifecycleChanged` event — raised on every open/close.

The registry is the backing store for `FormsManager.CallFormAsync` / `OpenFormAsync` / `NewFormAsync` / `CloseForm`. The orchestrator pushes/pops the registry as the call stack changes.

`Stack<FormCallStackEntry> _callStack` (in `FormsManager.Core.cs`) tracks `CALL_FORM` calls. The top of the stack is the current form; the rest is the calling chain.

## `CALL_FORM` (modal)

```csharp
await manager.CallFormAsync(
    targetForm: "ORDER_FORM",
    parameters: new Dictionary<string, object> { ["CustomerId"] = "ALFKI" },
    mode: FormCallMode.Modal);
```

The flow:

1. Push current form + parameters onto the call stack.
2. Fire `OnFormClose` (cancellable) for the current form.
3. Open the target form via the registry.
4. The target form receives the parameters via `GetFormParameter(name)`.
5. `ReturnToCallerAsync(returnData)` pops the call stack and returns to the caller with the data.

`CallFormAsync` is **modal** in the engine's sense: the caller waits for the target to `ReturnToCallerAsync` before continuing.

## `OPEN_FORM` (modeless)

```csharp
await manager.OpenFormAsync(
    targetForm: "ORDER_FORM",
    parameters: new Dictionary<string, object> { ["CustomerId"] = "ALFKI" });
```

Same as `CallFormAsync` but **non-blocking** — the engine registers the form and returns immediately. The original form continues. The target form's lifecycle is independent.

The overload `OpenFormAsync(string formName)` (no parameters) is the current-form lifecycle open operation, not the multi-form one. The README has been explicit about which overload does what since the early 3.0 docs.

## `NEW_FORM`

```csharp
await manager.NewFormAsync("BLANK_FORM");
```

Closes all open forms and opens the target as the new root. Use this when the user wants to "start over" without going through the call stack.

The implementation:

1. Close all forms in the registry (fire `OnFormClose` for each).
2. Reset the call stack.
3. Open the target form.
4. Reset `:GLOBAL.*` is **not** automatic — globals persist across `NEW_FORM`.

## `RETURN`

```csharp
await manager.ReturnToCallerAsync(returnData);
```

Pops the call stack. The caller of the current `CALL_FORM` resumes with the return data.

If the call stack is empty (i.e. no caller), `ReturnToCallerAsync` is a no-op or returns `false` — the form is at the root and there's no one to return to.

## `:GLOBAL.*` analog

```csharp
manager.SetGlobalVariable("ACTIVE_CUSTOMER_ID", "ALFKI");
var id = manager.GetGlobalVariable("ACTIVE_CUSTOMER_ID");
```

A simple `ConcurrentDictionary<string, object>` with case-insensitive key comparison. Globals persist across form open/close and across `NEW_FORM` — they live for the lifetime of the `FormsManager` instance.

There is no `DELETE_GLOBAL` — the global stays until the manager is disposed. Use a known prefix or namespace to avoid collisions.

## Inter-form messaging

### Sender side

```csharp
manager.PostMessage(
    targetForm: "ORDER_FORM",
    messageType: "CustomerChanged",
    payload: "ALFKI");

manager.BroadcastMessage(
    messageType: "AppWideShutdown",
    payload: null);
```

`PostMessage` enqueues a message to a specific form. `BroadcastMessage` enqueues to all open forms.

### Receiver side

```csharp
manager.SubscribeToMessage("CustomerChanged", message =>
{
    Console.WriteLine($"Received {message.MessageType} from {message.SenderForm}");
    // ...
});
```

`SubscribeToMessage` registers a handler for a message type. Handlers are invoked when the form receives a message of that type.

### Delivery

The `FormMessageBus` delivers messages on the same thread that called `PostMessage` / `BroadcastMessage`. This means:

- If the sender is on a UI thread, the handler runs on the UI thread.
- If the sender is on a background thread, the handler runs on the background thread.

The bus is **synchronous** in the current implementation. There is no async delivery.

## Shared blocks

### Create a shared block

```csharp
manager.CreateSharedBlock("GLOBAL_CUSTOMER_LIST", customerUow);
```

The block is registered in the shared-block manager. Any form can fetch it via `GetSharedBlock`.

### Use a shared block

```csharp
var uow = manager.GetSharedBlock("GLOBAL_CUSTOMER_LIST");
if (uow != null)
{
    var first = uow.CurrentItem;
    // ...
}
```

### Lock the shared block for editing

```csharp
if (manager.TryLockSharedBlock("GLOBAL_CUSTOMER_LIST", TimeSpan.FromSeconds(5)))
{
    try
    {
        // Edit the block
    }
    finally
    {
        manager.ReleaseSharedBlockLock("GLOBAL_CUSTOMER_LIST");
    }
}
```

The lock is **exclusive** — only one form can hold it at a time. `TryLockSharedBlock` waits up to the timeout for the lock to be available; returns `false` if the timeout expires.

## `SendParameterToForm`

```csharp
manager.SendParameterToForm(
    targetFormName: "ORDER_FORM",
    paramName: "CustomerId",
    value: "ALFKI");
```

A direct one-shot parameter set. Unlike `PostMessage`, this does not queue; it sets a value in the target form's parameter dictionary. The target form reads it via `GetFormParameter("CustomerId")` whenever it next checks.

Use `SendParameterToForm` for "set this value before the form opens" scenarios. Use `PostMessage` for "fire this event after the form is open" scenarios.

## `GetCallStack` / `GetFormParameter`

- `GetCallStack()` returns the current call stack as `IReadOnlyList<FormCallStackEntry>`. Useful for debugging ("where am I in the navigation?") and for "Back" navigation.
- `GetFormParameter(name)` returns a parameter from the current form's parameter dictionary. Returns `null` if the parameter wasn't set.

## Form lifecycle events

`IFormRegistry.FormLifecycleChanged` raises on every open/close. Args (`FormLifecycleEventArgs`) include the form name, the operation (open/close), and the parameters.

The orchestrator's `OnFormOpen` / `OnFormClose` / `OnFormCommit` / `OnFormRollback` / `OnFormValidate` are **separate** from the registry's events — they fire on the corresponding orchestrator method calls, not on the registry's internal open/close.

## Multi-form transactional rollback

The engine's transaction is **per-form**. If `CallFormAsync` opens a modal child form, the child has its own transaction state. There's no automatic two-phase commit across the call stack.

This is a real gap — see [`gaps.md`](../gaps.md). Workarounds:

- The caller validates and commits before the `CallFormAsync` (so the caller has no dirty state when the call starts).
- The child form commits its own state on `ReturnToCallerAsync`.
- If the child returns data and the caller needs to do something with it, the caller does the second-step commit.

## Notes for callers

- The registry is in-memory. Closing the `FormsManager` clears it.
- Globals are case-insensitive. `GetGlobalVariable("customerId")` and `GetGlobalVariable("CUSTOMERID")` are the same key.
- `PostMessage` handlers run on the sender's thread. If you call `PostMessage` from a background thread, the handler runs on the background thread. To ensure UI-thread delivery, marshal the call to the UI thread.
- Shared blocks **lock** at the level of the UoW, not at the level of the block metadata. If two forms try to edit the same shared block without locking, the UoW's own state-management may produce inconsistent results.
- The `Forms` method `OpenFormAsync(string formName)` is **not** the same as `OpenFormAsync(string formName, Dictionary parameters)`. The single-arg version is the current-form lifecycle; the two-arg version is the modeless multi-form open. They share the same method name for Oracle compatibility.

## See also

- [`architecture.md`](../architecture.md) — the four-layer model and where the registry/bus live.
- [`triggers.md`](triggers.md) — `OnFormOpen` / `OnFormClose` / `OnFormCommit` orchestrator events.
- [`ORACLE-FORMS-MAPPING.md`](../ORACLE-FORMS-MAPPING.md) sections 1, 14 — the multi-form and globals mapping.
