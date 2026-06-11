# FormsManager — Mode Transitions (ENTER_QUERY, EXECUTE_QUERY, CRUD)

This document covers the three mode states a block can be in and the transitions between them.

## The three modes

| Mode | Enum value | Oracle Forms equivalent | What you can do |
| --- | --- | --- | --- |
| **Enter-Query** | `DataBlockMode.EnterQuery` | After `ENTER_QUERY` | The block is in "where clause" mode. You can enter filter values in items. No records are loaded. |
| **Query** | `DataBlockMode.Query` | After `EXECUTE_QUERY` (returned) | Records are loaded from the datasource. You can navigate. You cannot edit records (the block is read-only). |
| **CRUD** | `DataBlockMode.Crud` | After a successful `EXECUTE_QUERY` that returned rows | Records are loaded and editable. Insert, update, delete, commit, rollback all work. |

There's also `DataBlockMode.New` (the block is in "new record" mode after `CreateNewRecordInMasterBlock`).

## `ENTER_QUERY`

### `EnterQueryModeAsync(string blockName)`

The full flow:

1. **Validate the block** — block exists, has a UoW.
2. **Check for unsaved changes** — if the block is in CRUD mode and the current record is dirty, route through `CheckAndHandleUnsavedChangesAsync`.
3. **Fire `OnPreQuery`** — cancellable.
4. **Set `Mode = EnterQuery`** on the UoW.
5. **Save a savepoint** — the current state of CRUD mode is captured so `ABORT_QUERY` (via `RollbackToSavepointAsync`) can return to it.
6. **Fire `WHEN-NEW-RECORD-INSTANCE`** for the empty query record.
7. **Update system variables** — `:SYSTEM.MODE = "ENTER-QUERY"`, `:SYSTEM.BLOCK_STATUS = "QUERY"`, `:SYSTEM.LAST_QUERY = ""`.
8. **Return** `IErrorsInfo` with `Flag = Errors.Ok` (or `Failed` if any step failed).

After `EnterQueryModeAsync`, the user enters filter values in items. The values go into the UoW's "query record" (a transient record that holds the where-clause values). Validation rules with timing = `OnChange` still fire on each item.

## `EXECUTE_QUERY`

### `ExecuteQueryAsync(string blockName, List<AppFilter>? filters)` (basic)

Runs the query using the existing in-block filter values (or a passed-in filter list). Does NOT change mode — if the block is in Enter-Query, it stays in Enter-Query; if it's in Query, it stays in Query.

### `ExecuteQueryAndEnterCrudModeAsync(string blockName, List<AppFilter>? filters)` (canonical)

The "I want data and I want to be able to edit it" path. Sequence:

1. **Validate** — block exists, has a UoW, has at least one record to query against.
2. **Check for unsaved changes** — same as `EnterQueryModeAsync`.
3. **Fire `OnPreQuery`** — cancellable.
4. **Run the query** — `_queryBuilderManager` composes the SQL from the in-block filter values (plus the `filters` argument, if provided). The UoW executes the SQL and loads the result set.
5. **Set `Mode = Crud`** if rows were returned; `Mode = Query` if the result was empty (preserves read-only semantics on empty queries).
6. **Fire `OnPostQuery`** — includes the result count.
7. **Navigate to the first record** — `FirstRecordAsync(blockName)`.
8. **Update system variables** — `:SYSTEM.MODE = "NORMAL"` (CRUD is "NORMAL" in Oracle), `:SYSTEM.BLOCK_STATUS = "CHANGED"` if any data was changed, `:SYSTEM.LAST_QUERY = the SQL`.
9. **Return** `IErrorsInfo`.

If the result set is empty, the block stays in Query mode and the user can `INSERT_RECORD` directly without an explicit `ENTER_QUERY` step.

### `ExecuteQueryEnhancedAsync` (alias for richer return)

Same as `ExecuteQueryAndEnterCrudModeAsync` but with a richer return value (per-step errors, optional record-count metadata). Used when the caller wants more telemetry.

## `EXIT_QUERY` (cancel)

### `IBeepBuiltins.ExitQuery()`

Cancels the current Enter-Query session and returns the block to its pre-ENTER_QUERY state. The engine implements this via `RollbackToSavepointAsync(blockName, "__pre_enter_query__", ct)` — the savepoint created during `EnterQueryModeAsync`.

If no savepoint exists (the block was already past the Enter-Query state), `ExitQuery` is a no-op.

## `ENTER_QUERY` → `EXECUTE_QUERY` transitions on master/detail

When the master block changes mode, **all detail blocks** must follow. `EnterQueryModeAsync` propagates to detail blocks; so does `ExecuteQueryAndEnterCrudModeAsync`. See [`master-detail.md`](master-detail.md) for the propagation rules.

## New-record mode

`CreateNewRecordInMasterBlockAsync(masterBlockName)` is the "create a fresh master record" path. It is used in forms like Order Entry where the user wants to start a new order without first querying for existing ones. The flow:

1. **Save the current state** — same savepoint mechanism as `ENTER_QUERY`.
2. **Create a new blank record** — `unitOfWork.AddNew()` or equivalent.
3. **Set `Mode = New`**.
4. **Navigate to the new record** — current index = 0.
5. **Fire `OnPreInsert` / `OnPostInsert`** (the new record is logically "about to be inserted").
6. **Detail blocks are reset** to match the new master record's key.

The new record is in memory only until the user calls `COMMIT`. If they navigate away, the savepoint can be rolled back to.

## The complete mode-transition matrix

| From | To | Method | Notes |
| --- | --- | --- | --- |
| (any) | `EnterQuery` | `EnterQueryModeAsync` | Captures savepoint. |
| `EnterQuery` | `Query` (empty) | `ExecuteQueryAsync` (no rows) | Stays in Query if no rows. |
| `EnterQuery` | `Crud` (rows) | `ExecuteQueryAndEnterCrudModeAsync` | |
| `EnterQuery` | `EnterQuery` (pre-state) | `IBeepBuiltins.ExitQuery` | Via savepoint rollback. |
| `Query` | `Query` (re-query) | `ExecuteQueryAsync` | |
| `Query` | `Crud` | `ExecuteQueryAndEnterCrudModeAsync` | |
| `Crud` | `EnterQuery` | `EnterQueryModeAsync` | Captures savepoint of the CRUD state. |
| `Crud` | `Crud` (new record) | `CreateNewRecordInMasterBlockAsync` | |
| (any) | (any) | `GetBlockMode` (read) | |

## Mode-aware behaviors

Some methods only work in specific modes. The orchestrator's mode checks:

| Method | Required mode | What happens in other modes |
| --- | --- | --- |
| `InsertRecordAsync` | `Crud` or `EnterQuery` | `ArgumentException` if `Query`. |
| `UpdateCurrentRecordAsync` | `Crud` | no-op if `Query`. |
| `DeleteCurrentRecordAsync` | `Crud` | no-op if `Query`. |
| `CommitFormAsync` | `Crud` | commits `Query` no-op; commits `EnterQuery` is the savepoint. |
| `ExecuteQueryAsync` | `EnterQuery` (preferred) or `Query` (re-query) | no-op in `Crud` unless `ExecuteQueryAndEnterCrudModeAsync` is used. |

The `ValidateAllBlocksForModeTransitionAsync` helper runs validation across all blocks before a mode transition. Used internally by `EnterQueryModeAsync` and `ExecuteQueryAndEnterCrudModeAsync`.

## System variables per mode

| Mode | `:SYSTEM.MODE` | `:SYSTEM.BLOCK_STATUS` |
| --- | --- | --- |
| `EnterQuery` | `ENTER-QUERY` | `QUERY` |
| `Query` (rows) | `NORMAL` (CRUD is "NORMAL" in Oracle) | `CHANGED` if any edits, else `QUERY` |
| `Query` (no rows) | `NORMAL` | `QUERY` |
| `New` | `NORMAL` | `NEW` |

See [`system-variables.md`](system-variables.md).

## Errors and rollback

If any step of a mode transition fails, the engine rolls back to the previous mode and returns an `IErrorsInfo` with `Flag = Errors.Failed` and a descriptive `Message`. Typical failure causes:

- Unsaved changes in current record (and user cancelled the save prompt).
- A `OnPreQuery` handler cancelled the transition.
- The UoW's `EnterQueryMode` returned an error.
- A `WHEN-NEW-RECORD-INSTANCE` trigger returned `false`.
- The query returned a datasource error.

## See also

- [`block-lifecycle.md`](block-lifecycle.md) — block registration.
- [`navigation.md`](navigation.md) — record navigation.
- [`master-detail.md`](master-detail.md) — how mode transitions propagate to detail blocks.
- [`system-variables.md`](system-variables.md) — the `:SYSTEM.*` variables updated on transition.
- [`triggers.md`](triggers.md) — `WHEN-NEW-*-INSTANCE` triggers that fire on mode transitions.
