# FormsManager — Navigation (block, record, item)

This document covers navigation in three scopes: block-to-block (`GO_BLOCK`), record-to-record within a block (`FIRST_RECORD`, `NEXT_RECORD`, `GO_RECORD`), and item-to-item within a record (`GO_ITEM`, `NEXT_ITEM`). Plus navigation history (back/forward).

## Block navigation

### `SwitchToBlockAsync(string blockName)` (and alias `GoBlockAsync`)

The canonical block-switch. Sequence:

1. **Validate** — block must exist; `_currentBlockName` is set.
2. **Check unsaved changes** — if the *current* block (about to be left) has unsaved changes, route through `CheckAndHandleUnsavedChangesAsync`. This may prompt the user (via `OnFormClose` event) and either commit, rollback, or cancel the switch.
3. **Fire block leave** — `_eventManager.TriggerBlockLeave(currentBlock)`. The WHEN-LEAVE-BLOCK trigger runs.
4. **Update `_currentBlockName`** to the new block.
5. **Fire block enter** — `_triggerManager.FireBlockTriggerAsync("WHEN-NEW-BLOCK-INSTANCE", newBlock)`. This raises `OnBlockEnter` and runs any registered rules.
6. **Update system variables** — `_systemVariablesManager.UpdateForBlockChange(newBlock)`.
7. **Return** `true` on success, `false` if any step failed.

`GoBlockAsync` is a one-line alias: `public Task<bool> GoBlockAsync(string blockName) => SwitchToBlockAsync(blockName);` — both names are public for callers who prefer the Oracle Forms verb.

### Multi-block calls (the `IBeepBuiltins` surface)

The `IBeepBuiltins` interface exposes the Oracle Forms verbs directly:

```csharp
bool GoBlock(string blockName);    // → SwitchToBlockAsync
bool NextBlock();                  // → NextBlockAsync
bool PreviousBlock();              // → PreviousBlockAsync
bool FirstBlock();
bool LastBlock();
```

`NextBlockAsync` and `PreviousBlockAsync` find the next/previous block in registration order. They share the unsaved-changes check with `SwitchToBlockAsync`.

## Record navigation

### `NavigateWithValidationAsync` (the inner workhorse)

Every record-navigation method (`FirstRecordAsync`, `LastRecordAsync`, `NextRecordAsync`, `PreviousRecordAsync`, `NavigateToRecordAsync`) routes through `NavigateWithValidationAsync`. The full flow:

1. **Validate the target** — block exists, UoW exists.
2. **Compute previous index** — for history tracking.
3. **Validate the current block** if `Configuration.Navigation.ValidateBeforeNavigation` is true.
4. **Check unsaved changes** — if the *current record* has unsaved changes, route through `CheckAndHandleUnsavedChangesAsync`.
5. **Fire `OnNavigate`** (cancellable) — host UI can intercept and cancel.
6. **Suppress detail sync** — wrap the navigation in `SuppressSync` / `ResumeSync` so the master/detail auto-sync doesn't fire for the intermediate state.
7. **Perform the navigation** — `unitOfWork.MoveTo(newIndex)`.
8. **Push to history** — if the index changed, push the previous index to `NavigationHistoryManager`.
9. **Synchronize detail blocks** — `SynchronizeDetailBlocksAsync(masterBlockName)`.
10. **Fire `OnCurrentChanged`**.
11. **Update system variables** — `:SYSTEM.CURSOR_RECORD` is updated.

### `GoRecord(int oneBased)` — the Oracle Forms verb

`IBeepBuiltins.GoRecord(n)` is 1-based (Oracle convention); the engine internally calls `NavigateToRecordAsync(blockName, n - 1)`.

### Record index conventions

The engine stores records in 0-based arrays internally (matches `IUnitofWork.Units`). The `IBeepBuiltins.GoRecord` adapter translates to/from 1-based. The orchestrator-level `NavigateToRecordAsync(blockName, int)` takes 0-based.

## Item navigation

### `GoItemAsync(string blockName, string itemName)`

The item-level equivalent of `GO_ITEM`. Sequence:

1. Verify the block has the named item (uses `IItemPropertyManager`).
2. Fire `WHEN-NEW-ITEM-INSTANCE` through `ITriggerManager`.
3. Reject the navigation if the trigger fails or is cancelled.
4. Update system variables — `:SYSTEM.CURSOR_BLOCK`, `:SYSTEM.CURSOR_ITEM`,
   and `:SYSTEM.CURSOR_VALUE`.

UI hosts call this method before moving platform focus. The shared
`IBeepFormsHost.GoToItemAsync` contract therefore changes visual focus only
after the engine accepts the target item.

### `NextItemAsync` / `PreviousItemAsync`

Find the next/previous item in the block's item-order (registration order, not visual order — visual order is a host concern). Same flow as `GoItemAsync` but with auto-selected next/previous item.

Note: there is no `IBeepBuiltins.NextItem()` / `PreviousItem()` — those are engine-level only. Add them if a host needs them.

## Navigation history

`NavigationHistoryManager` keeps a per-block back/forward stack. Each `NavigateWithValidationAsync` push pushes the previous index. The history is bounded; old entries are dropped.

`FormsManager` exposes:

- `NavigateBackAsync(blockName)` — pops the history, navigates to the previous index, *without* pushing to history (so back-then-forward returns you to where you were).
- `NavigateForwardAsync(blockName)` — symmetric.
- `CanNavigateBack(blockName)` / `CanNavigateForward(blockName)`.
- `GetNavigationHistory(blockName)` — returns the full stack.
- `ClearNavigationHistory(blockName)`.

The history is **per-block**, not per-form. Closing/reopening a form does not clear it; unregistering the block does.

## System variables updated on navigation

| Variable | Set to |
| --- | --- |
| `:SYSTEM.CURSOR_BLOCK` | the new block name |
| `:SYSTEM.CURSOR_RECORD` | the new record index (0-based) |
| `:SYSTEM.CURSOR_ITEM` | the new item name (if item-level navigation) |
| `:SYSTEM.CURSOR_VALUE` | the current item's value (if item-level navigation) |
| `:SYSTEM.LAST_RECORD` | updated to the new record's "last" state |
| `:SYSTEM.LAST_QUERY` | unchanged (only updates on query) |

## Cancellation paths

A navigation operation can be cancelled at two points:

1. **`OnNavigate` event** — handler can set `args.Cancel = true`. The cancellation message goes to `Status` and a warning message is sent to the message manager.
2. **Unsaved-changes check** — if `CheckAndHandleUnsavedChangesAsync` returns `false` (e.g. user cancelled the "save changes?" prompt), navigation is cancelled.

In both cases, the method returns `false` and the state is unchanged.

## Concurrency and re-entrancy

`NavigateWithValidationAsync` is `async` and may be called from any thread. The actual `unitOfWork.MoveTo` call is the per-block synchronization point; the engine does not add another lock around it.

The `SuppressSync` / `ResumeSync` mechanism is a per-block counter, not a lock. Multiple nested navigations inside the same block will increment and decrement the counter; the master/detail sync fires only when the counter reaches zero.

## What's NOT here (and is in the host UI)

- **Visual focus** — which item receives keyboard focus after `GO_ITEM`. The engine sets `:SYSTEM.CURSOR_ITEM` but the host UI does the actual focus.
- **Mouse-based navigation** — clicks on items/records.
- **Keyboard navigation** — Tab order, accelerator keys (other than the `KEY-` triggers which are in the triggers subsystem).
- **Scroll position** — the host UI owns what the user sees on the canvas.

## See also

- [`block-lifecycle.md`](block-lifecycle.md) — block registration.
- [`mode-transitions.md`](mode-transitions.md) — query-mode navigation vs record navigation.
- [`triggers.md`](triggers.md) — WHEN-NEW-*-INSTANCE triggers that fire on navigation.
- [`system-variables.md`](system-variables.md) — the `:SYSTEM.*` variables updated on navigation.
