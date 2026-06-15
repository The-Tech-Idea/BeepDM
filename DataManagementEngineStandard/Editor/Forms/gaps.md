# FormsManager — Gaps (CRUD & Data Management Focus)

This document lists the gaps between the current engine and full Oracle Forms
emulation, **scoped to CRUD, data management, and database data entry**. UI
rendering concerns (windows, menus, images, visual attributes, reports, OLE)
are deliberately excluded — they are the host UI's responsibility.

Items marked ⚠️ partial or ❌ missing in [`ORACLE-FORMS-MAPPING.md`](ORACLE-FORMS-MAPPING.md).

---
## P0 — Correctness / Existing-User Impact

### G0.1: Multi-form transactional rollback (FIXED 2026-06)

**Fix:** `CommitFormAsync` now walks the call stack via `ResolveCrossFormCommitTargets()`
to discover all FormsManager instances that should participate in the commit.
When called from a modal child form, all dirty blocks from the entire call
chain are committed together. `TryCrossFormTransactionCommitAsync` handles
failure by rolling back already-committed forms in reverse order.

This matches Oracle Forms' behavior where `CALL_FORM` shares the same database
session and `COMMIT` from the child commits everything.

**Where:** `FormsManager.FormOperations.cs` — `ResolveCrossFormCommitTargets()`,
`TryCrossFormTransactionCommitAsync()`. Lines 488-580.

**Risk of fix:** Medium. Existing users that relied on the old "first caller
commits, then child commits independently" pattern will see a behavior change.
A form that calls `CommitFormAsync` from a modal child now commits the parent's
blocks too.

---

### G0.2: `WHEN-CUSTOM-ITEM-EVENT` not a first-class trigger

**What:** No canonical event type for host-defined custom events.

**Where:** `Helpers/EventManager.cs`, `Helpers/TriggerManager.cs`, `Models/TriggerEnums.cs`

**Effort:** Small. **Risk:** Low.

---

### G0.3: Master/detail sync — silent failure on computed keys (FIXED 2026-06)

**Fix:** Added `CanRead` + `CanWrite` check with computed-property heuristic in
`Helpers/RelationshipManager.cs:323-378`. Loud log on unresolvable keys.

**Risk of fix:** Low.

---

### G0.4: Sequence collision in distributed scenarios

**What:** In-memory `SequenceProvider` is per-instance. Two instances can
return duplicate values.

**Effort:** Medium. **Risk:** Low.

---

### G0.6: Reflection-based UoW method resolution (FIXED 2026-06)

**Fix:** Replaced 6 `GetMethod("DeleteAsync")` / `GetMethod("Get")` reflection
sites with direct `IUnitofWork` interface calls. No more silent-no-op on
renamed methods.

---

### G0.7: Reflection on `Units` (Count, CurrentIndex) (FIXED 2026-06)

**Fix:** Replaced with `dynamic` dispatch. Filtered-units count now correct.

---

### G0.8: `LOVManager` concurrency + perf defects (FIXED 2026-06)

6 bugs fixed: cache read/write race, O(N) validation scan, re-registration
silent overwrite, culture-sensitive search, cleanup abort on bad definition,
property lookup bypass of `RecordPropertyAccessor`.

---

### G0.9: `TriggerManager` correctness + consolidation (FIXED 2026-06)

8 bugs fixed: re-register double-fire, 4 Get*Triggers lock-less reads,
`_suspended` non-volatile, `ClearAllTriggers` race, reflection bypass,
case-sensitive field lookup, missing cancellation token, silent missing-dep
in `OrderByDependency`.

---

### G0.10: Multi-form / inter-form correctness (FIXED 2026-06)

7 bugs fixed: broken modal-suspension, stack imbalance on exception, stack
corruption in `ReturnToCallerAsync`, TOCTOU `FormExists`/`GetForm`, silent
handler-exception swallow in message bus, silent overwrite on re-register,
TOCTOU lock release.

---

### G0.11: `ModeTransitions` correctness (FIXED 2026-06)

4 bugs fixed: `EnterQuery` source-mode rejection, double-mutate, dead
parameter, silent Query default for missing blocks.

---

### G0.12: `ValidationManager` second-pass (FIXED 2026-06)

7 bugs fixed: NRE on double-unregister, NRE on missing item, orphan entries on
concurrent `ClearAllRules`, wrong sentinel constants, `FutureDateRule`/`PastDateRule`
no-ops, uniqueness security bypass on DB error, custom-validator silent false.

---

### G0.13: `Master/Detail` second-pass (FIXED 2026-06)

4 bugs fixed: silent downgrade of explicit config, `;` separator not parsed,
over-strict primary-key fallback, dead fields on `DataBlockRelationship`.

---

### G0.14: `Triggers` second-pass (FIXED 2026-06)

6 bugs fixed: sync-over-async deadlock, `Clone` dropped `DependsOn`/`ChainMode`,
`Cancelled` not flagged in `WasCancelled`, `IsEnabled` non-volatile,
partial-registration race, timezone mix (local time vs UTC).

---

## P1 — CRUD & Data Management Parity Gaps

### G1.1: Composite-key master/detail relationships

**What:** `CreateMasterDetailRelation` takes a single key field. Oracle Forms
supports multi-key joins (e.g. `OrderNumber + LineNumber`).

**Where:** `FormsManager.Relationships.cs`, `Models/DataBlockRelationship.cs`

**Effort:** Small. Accept `string[]` for key fields. **Risk:** Low.

---

### G1.2: `RECORD_GROUP` / `RECORDGROUP_FROM_QUERY` built-ins

**What:** Oracle Forms has named in-memory record sets (`RECORDGROUP`) that
can be populated by query. The engine has no record-group concept. Forms
that build in-memory data sets for LOVs, combo boxes, or find dialogs
currently bypass the engine.

**Where:** `Helpers/`, `Builtins/IBeepBuiltins.cs`

**Effort:** Medium. Add `IRecordGroupRegistry`, `RecordGroup` DTO, and
`IBeepBuiltins.CreateRecordGroup` / `PopulateRecordGroup` / `GetRecordGroup`.

**Risk:** Low.

---

### G1.3: `LIST_VALUES` built-in

**What:** Oracle Forms has `LIST_VALUES` (a restricted `SHOW_LOV`) that
displays the current LOV's values as a flat list. The engine has
`ShowLOVAsync` but no `LIST_VALUES` equivalent.

**Where:** `Builtins/IBeepBuiltins.cs`, `IBuiltinHost`

**Effort:** Small. Add `IBeepBuiltins.ListValues(blockName, fieldName)` that
routes through `IBuiltinHost.ListValuesAsync`.

**Risk:** Low.

---

### G1.4: `PARAMETER` / `PARAMETER_LIST` built-ins

**What:** Oracle Forms has `PARAMETER` (a named value passed between forms or
to the database) and `PARAMETER_LIST` (a list of parameters). The engine has
`_formParameters` and `MultiFormSetGlobal` but no `PARAMETER` concept in
`IBeepBuiltins`.

**Where:** `Builtins/IBeepBuiltins.cs`, `FormsManager.InterFormComm.cs`

**Effort:** Small. Add `IBeepBuiltins.SetParameter` / `GetParameter` /
`CreateParameterList` / `AddParameterToList` / `DestroyParameterList`.
Map to the existing `_formParameters` dictionary.

**Risk:** Low.

---

### G1.5: `PROGRAM_UNIT` built-in

**What:** Oracle Forms has `PROGRAM_UNIT` for calling PL/SQL stored procedures.
The engine has triggers but no database-side program-unit concept.

**Where:** `Builtins/IBeepBuiltins.cs`, `IBuiltinHost`

**Effort:** Medium. Add a `IProgramUnitProvider` interface and
`IBeepBuiltins.ExecuteProgramUnit(unitName, parameters)`. The host routes
the call to the database via `IDataSource`.

**Risk:** Medium. Database-specific (Oracle vs SQL Server proc calling
conventions differ).

---

### G1.6: `DBMS_APPLICATION_INFO` built-ins

**What:** Oracle Forms has `DBMS_APPLICATION_INFO.SET_CLIENT_INFO` /
`SET_MODULE` / `SET_ACTION` for passing metadata to the database. Useful
for audit trails and monitoring. The engine has no DBMS concept.

**Where:** `Builtins/IBeepBuiltins.cs`, `IBuiltinHost`

**Effort:** Small. Add `IBeepBuiltins.SetClientInfo(key, value)` and
`SetDbmsModule(moduleName, actionName)`. Host routes through `IDataSource`.

**Risk:** Low.

---

### G1.7: `CLIENT_HOST` / `CLIENT_INFO` built-ins

**What:** Oracle Forms has `CLIENT_HOST` (the client hostname), `CLIENT_INFO`
(user-defined client metadata), `CLIENT_IP_ADDRESS`. Useful for audit
trails showing "who did what from where."

**Where:** `Builtins/IBeepBuiltins.cs`, `IBuiltinHost`

**Effort:** Small. Add `IBeepBuiltins.GetClientInfo(key)`. Host provides
values (WinForms `Dns.GetHostName()`, etc.).

**Risk:** Low.

---

## P2 — Data Management Nice-to-Have

### G2.1: Built-in query construction language

**What:** Parse `WHERE` clause strings (e.g. `WHERE CustomerId = :1`) into
`List<AppFilter>`. The engine currently requires callers to construct the
filter list programmatically.

**Where:** `Helpers/QueryBuilderManager.cs`

**Effort:** Medium. Add a `WhereClauseParser`. **Risk:** Low.

---

### G2.2: `EDITOR` / `TEXT_IO` built-ins (large text editing)

**What:** Oracle Forms has `EDIT_TEXTITEM` (multi-line text editor pop-up),
`TEXT_IO` (text file I/O for data import/export), `TEXT_EDITOR` (external
editor). Relevant for large-text data entry fields (notes, comments, JSON).

**Where:** `Builtins/IBeepBuiltins.cs`, `IBuiltinHost`

**Effort:** Medium. Host-routed (WinForms `RichTextBox`, etc.). **Risk:** Low.

---

### G2.3: `VARR` (variable arrays) / batch operations

**What:** Oracle Forms has `VARR` (fixed-size value arrays) and `TABLE`
(PL/SQL tables) for passing arrays to the database. The engine has per-record
CRUD but no batch array-passing surface. Forms that batch-delete N records by
ID currently loop one at a time.

**Where:** `Helpers/`, `Builtins/IBeepBuiltins.cs`

**Effort:** Medium. Requires an `IVarArray` interface and wire format for
array passing to the underlying datasource.

**Risk:** Medium. Datasource support for batch operations varies.

---

### G2.4: `DBMS_PIPE` / `DBMS_ALERT` (cross-session messaging)

**What:** Oracle Forms has cross-session pipe-based messaging and alerts.
Useful for "user A updated a record, user B should refresh" multi-user
data entry coordination.

**Where:** `Helpers/`, `FormsManager.InterFormComm.cs`

**Effort:** Large. Requires database-side coordination and polling. **Risk:** Medium.

---

### G2.5: `SET_APPLICATION_PROPERTY` for cursor / data entry mode

**What:** Oracle Forms has `SET_APPLICATION_PROPERTY('CURSOR_MODE', 'OPEN')`
and `SET_APPLICATION_PROPERTY('DATA_MODE', 'QUERY')` for controlling data
entry behavior. The engine has `SetApplicationProperty` (generic bag) but
no specific presets.

**Where:** `Builtins/IBeepBuiltins.cs`, `IBuiltinHost`

**Effort:** Small. Add specific property keys. **Risk:** Low.

---

## P3 — IUnitofWork / IDataSource Capability Gaps

`IUnitofWork<T>` (313 lines, 34 properties, 71 methods, 17 events) and
`IDataSource` (313 lines, 15 properties, 21 methods) provide extensive
CRUD, navigation, validation, and schema-management capabilities that
`FormsManager` does not yet surface.

> **Interface impact: NONE.** All gaps below use methods that already exist on
> the interfaces. No `IDataSource` or `IUnitofWork` changes are needed —
> every implementation (RDBMS, NoSQL, file, web API) already supports or
> gracefully degrades these operations. The work is purely additive:
> new wrapper methods on `FormsManager` that delegate to existing UoW/DataSource
> methods.

### IUnitofWork features not surfaced by FormsManager

#### G3.1: Bookmarks (SET_BOOKMARK / GO_BOOKMARK)

**Source:** `IUnitofWork<T>.SetBookmark(string)`, `GoToBookmark(string)`,
`RemoveBookmark(string)`, `ClearBookmarks()`. Phase 8 feature.

**What FormsManager should do:** Expose `SetBlockBookmark(blockName, bookmarkName)`,
`GoToBlockBookmark(blockName, bookmarkName)`, `RemoveBlockBookmark`,
`ClearBlockBookmarks`. Oracle Forms has no direct bookmark equivalent, but
named cursor positions are essential for multi-step data entry workflows
(e.g., "save this position, go to a detail record, return here").

**Effort:** Small. Thin delegation to UoW. **Risk:** Low.

---

#### G3.2: Computed Columns

**Source:** `IUnitofWork<T>.RegisterComputed(name, Func<T,object>)`,
`UnregisterComputed`, `GetComputed`, `GetAllComputed`, `ComputedColumnNames`.
Phase 7 feature.

**What FormsManager should do:** Expose `RegisterBlockComputed(blockName, name, computation)`,
`UnregisterBlockComputed`, `GetBlockComputedValue`. Computed columns are
essential for forms that derive values from other fields (e.g.,
`FullName = FirstName + " " + LastName`, `OrderTotal = SUM(LineItems)`).

**Effort:** Small. Thin delegation. **Risk:** Low.

---

#### G3.3: Freeze / Batch Update

**Source:** `IUnitofWork<T>.Freeze()`, `Unfreeze()`, `IsFrozen`, `BeginBatchUpdate()`.
Phase 9 feature.

**What FormsManager should do:** Expose `FreezeBlock(blockName)` /
`UnfreezeBlock(blockName)` / `BeginBlockBatchUpdate(blockName)`.
When bulk-loading records or performing mass updates, freezing the UoW
suppresses `CurrentChanged` and `ItemChanged` events, avoiding UI thrash
and unnecessary event cascades.

**Effort:** Small. Thin delegation. **Risk:** Low.

---

#### G3.4: Entity-Level Search (Find / Clone)

**Source:** `IUnitofWork<T>.FindAsync(Func<T,bool>, CancellationToken)`,
`FindManyAsync`, `CloneItem(T, bool deepCopy)`.

**What FormsManager should do:** Expose `FindBlockRecordAsync(blockName, predicate)`,
`FindBlockRecordsAsync(blockName, predicate)`, `CloneBlockRecord(blockName, deepCopy)`.
Essential for "search within this block" and "duplicate current row" workflows.

**Effort:** Small. **Risk:** Low.

---

#### G3.5: UoW Change Log (richer than audit)

**Source:** `IUnitofWork<T>.GetChangeLog()` → `List<ChangeRecord>` with
per-property before/after values per entity.

**What FormsManager should do:** Wire `GetBlockChangeSummary` to `GetChangeLog`
for per-field change detail. The current `GetBlockChangeSummary` returns a
summary; `GetChangeLog` provides per-field granularity. Useful for
"show what changed before committing" preview dialogs.

**Effort:** Small. **Risk:** Low.

---

#### G3.6: UoW event → FormsManager sync is now complete (FIXED 2026-06)

**Fix:** `EventManager.cs` rewritten to subscribe to all 22 IUnitofWork events
via stored named delegates. Non-generic-safe optional events (OnItemReverted,
batch, rollback) use `dynamic` dispatch with try/catch fallback — silently
skipped on implementations that don't expose them. All delegates are now
removable via `UnsubscribeFromUnitOfWorkEvents`, eliminating the permanent
memory leak from the previous anonymous-lambda approach.

**Where:** `Helpers/EventManager.cs` — full rewrite (lines 1-450).

---

#### G3.7: UoW Virtual/Lazy Loading not surfaced

**Source:** `IUnitofWork<T>.EnableVirtualMode(totalCount)`, `DisableVirtualMode()`,
`GoToPageAsync`, `PrefetchAdjacentPagesAsync`, `IsVirtualMode`, `PageCacheSize`,
`VirtualTotalPages`.

**What FormsManager should do:** The existing `FormsManager.Performance.cs`
has separate paging; it should expose the UoW's native virtual mode for
blocks backed by large datasets. Currently `SetBlockPageSize` / `LoadPageAsync`
do NOT delegate to the UoW's `GoToPageAsync`.

**Effort:** Medium. Align FormsManager.Performance with UoW virtual mode.
**Risk:** Medium. Two paging implementations exist; consolidation needed.

---

### IDataSource features not surfaced by FormsManager

> **Design note:** `IDataSource` abstracts RDBMS, NoSQL, file, and web API
> sources. The gaps below must work with any data source or degrade
> gracefully when a source doesn't support a capability.

#### G3.8: Relationship auto-discovery from source metadata

**Source:** `IDataSource.GetChildTablesList(tableName, schema, filter)` →
`IEnumerable<ChildRelation>`, `GetEntityforeignkeys(entityName, schema)` →
`IEnumerable<RelationShipKeys>`. These represent the source's knowledge
of entity relationships. For RDBMS sources this is FK metadata; for NoSQL
it's embedded references; for file sources it may be empty.

**What FormsManager should do:** `SetupBlockAsync` should optionally
auto-discover and register known relationships. Return an empty list
when the source doesn't support relationship metadata. Always opt-in;
never force-register discovered relationships.

**Effort:** Medium. **Risk:** Low (opt-in). Cap auto-discovery at
`MaxAutoRelationships` to avoid flooding on schema-heavy sources.

---

#### G3.9: Entity lifecycle operations not surfaced (create/modify entity shape)

**Source:** `IDataSource.CreateEntityAs(EntityStructure)`, `CreateEntities(List)`,
`GetCreateEntityScript(List)`, `RunScript(ETLScriptDet)`. For RDBMS this
creates tables. For NoSQL this creates collections/document types. For
files this creates new file schemas. For web APIs this is a no-op.

**What FormsManager should do:** Expose `CreateBlockEntity(blockName, EntityStructure)`
as an optional engine operation for data-entry apps that need runtime
schema management (audit tables, staging tables, temp collections). The
operation is a no-op on sources that don't support it.

**Effort:** Large. **Risk:** Medium (source-dependent). Needs feature detection
on `IDataSource` to avoid crashing on unsupported operations.

---

#### G3.10: Source-level aggregate queries not surfaced

**Source:** `IDataSource.GetScalar(string query)` → `double`,
`GetScalarAsync(string query)` → `Task<double>`. The query string
format is source-dependent: SQL for RDBMS, filter expression for
NoSQL, query path for files.

**What FormsManager should do:** Expose `GetBlockAggregateScalar(blockName,
aggregateExpression)` for COUNT, MAX, MIN, SUM that hit the source
directly instead of computing on in-memory UoW data. Essential for
accurate totals on large datasets regardless of source type.

**Effort:** Small. **Risk:** Low. The expression format must match the
data source; callers already need source-awareness.

---

#### G3.11: Source-level transactions not surfaced

**Source:** `IDataSource.BeginTransaction(PassedArgs)`, `EndTransaction`,
`Commit(PassedArgs)`. Available on RDBMS and some NoSQL sources, not on
file or web API sources (those are no-ops or throw `NotSupportedException`).

**What FormsManager should do:** Expose `BeginFormTransaction` /
`EndFormTransaction` / `CommitFormTransaction` wrapping the source's
transaction boundary. Cross-block commits would be atomic when the
source supports it. On sources that don't, commit each block
independently (current behavior).

**Effort:** Medium. **Risk:** Medium. Must handle `NotSupportedException`
gracefully on file/web API sources.

---

### IUnitofWork features already surfaced

Features the engine already delegates to UoW (no gap):

| UoW Feature | FormsManager Equivalent | How |
|-------------|------------------------|-----|
| `Get(filters)` | `ExecuteQueryAsync(blockName, filters)` | `BasicDataOps.cs` |
| `New()` | `InsertRecordAsync(blockName)` | `EnhancedOperations.cs` |
| `Delete()` | `DeleteCurrentRecordAsync(blockName)` | `BasicDataOps.cs` |
| `Commit()` | `CommitFormAsync()` | `FormOperations.cs` |
| `Rollback()` | `RollbackFormAsync()` | `FormOperations.cs` |
| `MoveFirst/Next/Previous/Last` | `FirstRecordAsync` etc. | `Navigation.cs` |
| `MoveTo(index)` | `NavigateToRecordAsync(blockName, index)` | `Navigation.cs` |
| `Sum/Average/Min/Max/Count/GroupBy` | `GetBlockSum` etc. | `DataOperations.cs` |
| `ToDataTable/ToJson/ToCsv` | `ExportBlockTo*` | `DataOperations.cs` |
| `CommitBatchAsync` | `CommitFormBatchAsync` | `DataOperations.cs` |
| `RefreshAsync` | `RefreshBlockAsync` | `DataOperations.cs` |
| `RevertItem` | `RevertCurrentRecord` / `RevertRecord` | `DataOperations.cs` |
| `Undo/Redo` | `UndoBlock` / `RedoBlock` | `DataOperations.cs` |
| `IsEmpty` | `HasUnsavedChanges` (inverse) | `DirtyState.cs` |
| `ValidateItem/ValidateAll` | `ValidateField` / `ValidateBlock` | `Validation.cs` |
| `GetInsertedItems/UpdatedItems/DeletedItems` | `GetBlockChangeSummary` | `DataOperations.cs` |

---

### IDataSource features already surfaced

| DataSource Feature | FormsManager Equivalent | How |
|--------------------|------------------------|-----|
| `GetEntityStructure(name, refresh)` | `BlockItemWorkflowCoordinator.ResolveEntityStructure` | IDE layer |
| `Entities` / `EntitiesNames` | `ConnectionNavigatorProvider.CreateEntityGroupNode` | IDE layer |
| `Openconnection` / `Closeconnection` | `ConnectionWorkflowCoordinator.OpenConnectionAsync/CloseConnectionAsync` | IDE layer |
| `GetEntity(name, filters)` | `ExecuteQueryAsync` → via UoW | `BasicDataOps.cs` |
| `ConnectionStatus` | `CreateDataSourceNode` → `IsOpen` badge | IDE provider |
| `DatasourceType` / `Category` | `GetDatabaseTypeUnicodeIcon` | IDE provider |
| `InsertEntity/UpdateEntity/DeleteEntity` | Not used directly — goes through UoW instead | Design decision |

These LOOK like gaps but are deliberate:

- **No PL/SQL engine** — Forms with PL/SQL must be ported to C# triggers.
- **No visual rendering** (fonts, colors, layouts, windows, menus, images,
  OLE, ActiveX, reports, FTP, web calls, filesystem paths) — host UI concerns.
- **No keyboard plumbing** (tab order, accelerators beyond `KEY-` triggers).
- **No data-source abstraction** — engine works through `IUnitofWork`/`IDataSource`
  regardless of backing store.
- **No user-management / authentication** — engine trusts the `SecurityContext`.

---

## See Also

- [`ORACLE-FORMS-MAPPING.md`](ORACLE-FORMS-MAPPING.md) — concept-by-concept mapping
- [`enhancements.md`](enhancements.md) — improvement opportunities
- [`architecture.md`](architecture.md) — engine structure and host model
