# FormsManager — Master / Detail

This document covers master/detail relationships: how to register them, what automatic synchronization does, and the propagation rules for mode transitions and form lifecycle.

## Overview

A master/detail relationship in Oracle Forms links two blocks so that navigating to a new record in the master block automatically filters the detail block to show only the records that match the master's current key. FormsManager implements this with:

- `DataBlockRelationship` — the relationship's metadata.
- `CreateMasterDetailRelation` — the registration entry point.
- `MasterDetailKeyResolver` — the per-master/detail key-matching logic.
- `SynchronizeDetailBlocksAsync` — the auto-sync that runs on master current-record change.
- Per-block relationship list in `FormsManager._relationships` (concurrent dictionary keyed by master block name).

## Registering a relationship

```csharp
manager.CreateMasterDetailRelation(
    masterBlockName: "CUSTOMERS",
    detailBlockName: "ORDERS",
    masterKeyField: "CustomerId",        // master's column that holds the join key
    detailForeignKeyField: "CustomerId"); // detail's column that references the master
```

`CreateMasterDetailRelation` does the following:

1. Verifies both blocks exist.
2. Verifies the key fields exist on both blocks' `IEntityStructure`.
3. Stores the relationship in `_relationships[masterBlockName]`.
4. Subscribes to the master's `unitOfWork.CurrentChanged` event (set up in `BlockRegistration`).

Multiple detail blocks can attach to a single master. The relationships list is per-master, so one master can have many details. A detail can have its own detail blocks (multi-level master/detail), and the `SynchronizeDetailBlocksAsync` is recursive.

## Auto-sync on master current-record change

When the master block's `unitOfWork.CurrentChanged` event fires, the engine calls `SynchronizeDetailBlocksAsync(masterBlockName)`. The flow:

1. Read the master's current record.
2. Extract the value of the master key field (e.g. `CustomerId` of the current customer).
3. For each detail block in the relationship list:
   - Apply a filter to the detail's UoW: `WHERE detailForeignKeyField = masterKeyValue`.
   - If the detail has any existing records that no longer match the new master key, they're removed.
   - The detail is set to `Mode = Crud` (or stays in the current mode).
4. The detail's `OnRecordEnter` / `OnCurrentChanged` events may fire (if the detail had records).
5. The detail's `WHEN-NEW-RECORD-INSTANCE` trigger fires for the new first record.

The auto-sync is **suppressed** during certain operations (notably navigation on the master itself) via `SuppressSync(masterBlockName)` / `ResumeSync(masterBlockName)`. This prevents the detail from re-querying for every intermediate navigation step.

## Loop prevention

The `_mdCurrentChangedHandlers` map tracks per-block handlers. The handlers are added on `RegisterBlock` and removed on `UnregisterBlock`. The `SuppressSync` / `ResumeSync` counter ensures that a detail's own re-query doesn't cascade.

## Getting relationship info

- `GetDetailBlocks(masterBlockName)` — returns the list of detail block names for a master.
- `GetMasterBlock(detailBlockName)` — returns the master block name for a detail (or null if it's a root block).

## Mode-transition propagation

When the master block transitions mode (e.g. `EnterQueryModeAsync` or `ExecuteQueryAndEnterCrudModeAsync`), the engine propagates the transition to all detail blocks:

| Master mode change | Detail action |
| --- | --- |
| `EnterQuery` | Detail also enters `EnterQuery`. |
| `ExecuteQuery (rows)` | Detail re-queries with the master's current key. If empty, detail stays in `Query` mode. |
| `ExecuteQuery (no rows)` | Detail is cleared. |
| `CreateNewRecord` (new master record) | Detail is cleared; new master record's key is blank so the detail shows nothing. |
| `Commit` | Detail commits its dirty state after the master commits. |
| `Rollback` | Detail rolls back its dirty state. |

The propagation is automatic and recursive (a master-of-a-master also propagates).

## Cyclic relationships

You cannot create a cyclic relationship (block A is master of B, and B is master of A). `CreateMasterDetailRelation` detects this and rejects the request.

## Multi-key relationships

`CreateMasterDetailRelation` currently takes a single key field. For composite-key relationships, you must register the relationship programmatically (the engine supports it via `DataBlockRelationship` direct construction but the public `CreateMasterDetailRelation` overload is single-key only).

This is documented in [gaps](../gaps.md) — multi-key relationship registration is a real omission.

## Detail-block filters

The detail block's `DefaultWhere` is **extended** with the master-detail filter, not replaced. So:

- If the detail has its own `DefaultWhere = "IsDeleted = 0"`, the master/detail sync appends `AND CustomerId = <master-key>` (or equivalent).
- The filter is rebuilt on every master current-record change.

The result is the detail shows the rows that match **both** the detail's own where-clause and the master's current key.

## Detail block ordering

The order in which detail blocks are auto-synced is the order they were registered with `CreateMasterDetailRelation`. The first registered is the first synced. If you have multiple details and care about sync order, register them in the order you want.

## Transactions

The master and detail blocks are typically **part of the same transaction**. When `CommitFormAsync` runs:

1. Validate all blocks.
2. Cross-block validation.
3. Commit the master block.
4. Commit each detail block (in registration order).
5. If any block fails to commit, all preceding blocks are rolled back (where supported).

This is **best-effort** cross-block transactional integrity. Oracle Forms uses a similar "first master, then details, rollback all on failure" pattern. See [gaps](../gaps.md) for the multi-form transactional rollback limitations.

## `UnitofWorksManagerConfiguration.Relationships`

You can pre-configure relationships via the `UnitofWorksManagerConfiguration` DTO. The configuration is applied during `InitializeManager()` (called from the constructor). For runtime relationship changes, use `CreateMasterDetailRelation` / `UnregisterDetailRelationship`.

## Notes for callers

- The relationship is **per-form session** — it's stored in `_relationships` which is an in-memory dictionary. Closing/reopening a form does not clear it; unregistering a block does.
- `SuppressSync` / `ResumeSync` is **per-block**. If you suppress sync on a master block, the details don't auto-sync. Don't forget to `ResumeSync`.
- The master's `unitOfWork.CurrentChanged` event is the **only** signal that triggers auto-sync. If you change the master's "current record" without going through the UoW (e.g. by directly mutating `_currentBlockName` somehow), the detail won't re-query.
- Composite keys and self-referencing relationships are not supported through the public `CreateMasterDetailRelation` API. Use direct `DataBlockRelationship` construction for advanced cases.

## See also

- [`block-lifecycle.md`](block-lifecycle.md) — block registration.
- [`mode-transitions.md`](mode-transitions.md) — how mode transitions propagate.
- [`navigation.md`](navigation.md) — what triggers `CurrentChanged`.
- [`ORACLE-FORMS-MAPPING.md`](../ORACLE-FORMS-MAPPING.md) section 20 — the master/detail mapping.
