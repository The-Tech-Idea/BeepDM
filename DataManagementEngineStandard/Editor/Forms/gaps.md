# FormsManager — Gaps (CRUD & Data Management Focus)

This document lists the gaps between the current engine and full Oracle Forms
emulation, **scoped to CRUD, data management, and database data entry**. UI
rendering concerns (windows, menus, images, visual attributes, reports, OLE)
are deliberately excluded — they are the host UI's responsibility.

Items marked ⚠️ partial or ❌ missing in [`ORACLE-FORMS-MAPPING.md`](ORACLE-FORMS-MAPPING.md).

**Audit date:** 2026-06-17 — All 37 P0-P3 gaps resolved (32 fixed/enhanced, 5 deferred). 
Second pass closed 8 additional code-quality issues (duplication, DI bypass, stubs, naming).

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

### G0.2: `WHEN-CUSTOM-ITEM-EVENT` now a first-class trigger (FIXED 2026-06-17)

**Fix:** Fixed duplicate enum value (was 174, now 178 — removed collision with
`WhenMouseMove`). Added `OnCustomItemEvent` event and `TriggerCustomItemEvent` method
to `IEventManager` / `EventManager`. Added `CustomItemEventArgs` model carrying
`EventType`, `BlockName`, `ItemName`, `Payload`, and `Properties` dictionary.

**Where:** `Models/TriggerEnums.cs:381`, `Models/CustomItemEventArgs.cs` (new),
`Helpers/EventManager.cs:54,254-264`, `Interfaces/ICoreHelpers.cs:72-78`.

---

### G0.3: Master/detail sync — silent failure on computed keys (FIXED 2026-06)

**Fix:** Added `CanRead` + `CanWrite` check with computed-property heuristic in
`Helpers/RelationshipManager.cs:323-378`. Loud log on unresolvable keys.

**Risk of fix:** Low.

---

### G0.4: Sequence collision in distributed scenarios (IMPROVED 2026-06-17)

**What:** In-memory `SequenceProvider` is per-instance. Two instances can return duplicate values.
Not a blocking gap for single-instance use. For distributed scenarios, use a
datasource-backed sequence by passing a custom `ISequenceProvider` via the constructor.

**Where:** `Helpers/SequenceProvider.cs`. No code changes needed — the interface supports injection.

---

### G0.5: TriggerDependencyManager depth limit + cycle timeout (FIXED 2026-06-17)

**Fix:** Added `MaxDependencyDepth` (default: 100) and `CycleDetectionTimeout` (default: 5s)
properties. `OrderByDependency` tracks traversal depth; `FindCycle` checks a deadline and
skips detection with a warning on timeout.

**Where:** `Helpers/TriggerDependencyManager.cs:19-33,64-75,97-103`.

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

### G1.1: Composite-key master/detail relationships (FIXED 2026-06-17)

**Fix:** Added `DataBlockRelationship.KeyFieldMappings` collection and new
`CreateMasterDetailRelation` overload accepting `DataBlockFieldMapping[]`.
The resolver already supports multi-field mappings via `MasterDetailKeyResolution.Mappings`.
Backward-compatible — single-key string overload still works.

**Where:** `Models/DataBlockRelationship.cs:21-33`, `FormsManager.Relationships.cs:118-185`.

---

### G1.2: `RECORD_GROUP` / `RECORDGROUP_FROM_QUERY` built-ins (FIXED 2026-06-17)

**Fix:** Added `RecordGroup` model, `IRecordGroupRegistry` interface, and FormsManager
implementation. `PopulateRecordGroupAsync` creates a UoW, executes the query, and stores
records in-memory. Usable for LOVs, combo boxes, and find dialogs.

**Where:** `Models/RecordGroup.cs` (new), `Interfaces/IRecordGroupAndParameterInterfaces.cs` (new),
`FormsManager.RecordGroups.cs:13-86`, `Interfaces/IUnitofWorksManager.cs` (new members).

---

### G1.3: `LIST_VALUES` built-in (ALREADY EXISTS)

**Clarification:** `IBeepBuiltins.ListValues(blockName, fieldName)` already exists at
`Builtins/IBeepBuiltins.cs:246`. The host (`IBuiltinHost.ListLovRecords`) returns the
LOV's records as a `IReadOnlyList<object>`. No engine-side gap — already surfaced.

**Where:** `Builtins/IBeepBuiltins.cs:246`, `Builtins/IBeepBuiltins.cs:106`.

---

### G1.4: `PARAMETER` / `PARAMETER_LIST` built-ins (FIXED 2026-06-17)

**Fix:** Added `ParameterList` model, `IParameterListManager` interface, and FormsManager
implementation. Supports Create/Destroy/Add/Get/Remove/Has/Clear operations on named
parameter lists. Thread-safe via `ConcurrentDictionary`.

**Where:** `Models/ParameterList.cs` (new), `Interfaces/IRecordGroupAndParameterInterfaces.cs` (new),
`FormsManager.RecordGroups.cs:91-153`, `Interfaces/IUnitofWorksManager.cs` (new members).

---

### G1.5: `PROGRAM_UNIT` built-in — DEFERRED. RDBMS/datasource-specific (Oracle PL/SQL,
SQL Server T-SQL, etc. have incompatible calling conventions). The datasource driver
should own stored-procedure execution. Use custom triggers with `IDataSource` for
database-side procedure calls.

---

## Code Quality Fixes (Second Pass, 2026-06-17)

### CQ-1: Duplicate `SetAuditDefaults` / `ApplyAuditDefaults` (FIXED)
`ApplyAuditDefaults` was a duplicate of `SetAuditDefaults` with the same signature
and same delegate. Marked `ApplyAuditDefaults` as `[Obsolete]` and routed to
`SetAuditDefaults`. Both exist for backward compatibility.
**Where:** `FormsManager.EnhancedOperations.cs:554-562`.

### CQ-2: `OpenFormAsync` overload ambiguity (FIXED)
`FormsManager.FormOperations.cs:48` opens the LOCAL form; `FormsManager.MultiFormNavigation.cs:120`
opened a DIFFERENT form modelessly (confusing same-name overload). Renamed the
multi-form version to `OpenFormModelessAsync`; kept `OpenFormAsync` as `[Obsolete]` alias.
**Where:** `FormsManager.MultiFormNavigation.cs:120-140`.

### CQ-3: DI bypass for `_securityManager`, `_pagingManager`, `_auditManager`, `_crossBlockValidation` (FIXED)
These four managers were hardcoded to `new` instances, breaking the DI pattern.
Added constructor parameters with fallback defaults, matching the other 20+ managers.
**Where:** `FormsManager.Core.cs:119-124,154-157`.

### CQ-4: TriggerChaining DI not used (FIXED)
`InitializeTriggerChaining` supported DI parameters but the constructor never passed them.
Added `ITriggerExecutionLog` and `ITriggerDependencyManager` constructor params.
**Where:** `FormsManager.Core.cs:125-126,161`.

### CQ-5: `BeepFormsHostAdapter` stub implementations (FIXED)
Multi-form methods (`MultiFormOpenForm`, `MultiFormCloseForm`, etc.), application/form
property methods, and `ListLovRecords` were all no-op stubs returning null/false/empty.
Wired them to delegate to `_host.FormsManager` where applicable. `ListLovRecords` now
attempts to read LOV data from the block's UoW.
**Where:** `Builtins/BeepFormsHostAdapter.cs:78-120`.

### CQ-6: TriggerEnums.cs reserved-range comment (FIXED)
`WhenValidateRecord = 55` was followed by comment "Reserved 55-69" overwriting
the occupied value. Changed to "Reserved 56-69."
**Where:** `Models/TriggerEnums.cs:150`.

### CQ-11: `IBeepFormsHost` missing `CancellationToken` on 4 mutation methods (FIXED 2026-06-17)
Added `CancellationToken ct = default` to `SaveBlockAsync`, `RollbackBlockAsync`,
`InsertBlockRecordAsync`, `DeleteBlockCurrentRecordAsync` on `IBeepFormsHost`.
`BeepFormsHostAdapter` now forwards `ct` instead of silently dropping it.
**Where:** `Hosts/IBeepFormsHost.cs:56-59`, `Builtins/BeepFormsHostAdapter.cs:53-56`.

### CQ-12: `FormsManager.Logging.cs` file-scoped namespace (FIXED 2026-06-17)
Converted from file-scoped namespace (`namespace X;`) to block-scoped (`namespace X { }`)
to match all other 27 `FormsManager.*.cs` partials.
**Where:** `FormsManager.Logging.cs:1-80`.

### CQ-13: `ModeTransitionValidationResult` / `BlockModeInfo` placement (FIXED 2026-06-17)
Moved from inline definitions in `FormsManager.ModeTransitions.cs` to dedicated model file
`Models/ModeTransitionModels.cs`, matching the pattern of 65 other model classes.
**Where:** `Models/ModeTransitionModels.cs` (new), `FormsManager.ModeTransitions.cs:983-1045` (removed).

### CQ-15: `SetAuditDefaults` missing `Environment.UserName` fallback (FIXED 2026-06-17)
When `currentUser` was null, the audit-field code silently skipped `CreatedBy`/`ModifiedBy`
fields. Added `effectiveUser = currentUser ?? Environment.UserName` fallback in
`FormsSimulationHelper.SetAuditDefaults` so user audit fields are never silently skipped.
**Where:** `Helpers/FormsSimulationHelper.cs:74-88`.

### CQ-16: `PostBlockAsync` missing from host interfaces (FIXED 2026-06-17)
Added `PostBlockAsync(string, CancellationToken)` to both `IBuiltinHost` and `IBeepFormsHost`
interfaces. The `BeepBuiltins.Post()` stub (which calls Commit instead of Post) can now be
updated in the WinForms layer to call `PostBlockAsync` once the host implements it.
**Where:** `Builtins/IBeepBuiltins.cs:63`, `Hosts/IBeepFormsHost.cs:65`,
`Builtins/BeepFormsHostAdapter.cs:61`.

### CQ-17: Unused `DataBlockMode` enum values documented (FIXED 2026-06-17)
`Normal` (0), `ReadOnly` (4), and `Insert` (5) had no code that set or checked them.
Marked as "reserved for future use" with doc comments explaining current alternatives.
**Where:** `Models/DataBlockInfo.cs:91-109`.

### CQ-19: Security violation lambda → named method (memory leak fix) (FIXED 2026-06-17)
`InitializeSecurity` used an anonymous lambda for `OnSecurityViolation`, making
unsubscription impossible. Replaced with named `OnSecurityViolationHandler` method
and added `-=` call in `Dispose()`.
**Where:** `FormsManager.Security.cs:18-27`, `FormsManager.Lifecycle.cs:40`.

### CQ-20: Orphaned `DisposeTriggerChaining` never called (FIXED 2026-06-17)
`DisposeTriggerChaining()` existed but was never invoked from `Dispose()`. Added
the call in `Dispose()` alongside the other cleanup unsubscriptions.
**Where:** `FormsManager.Lifecycle.cs:41`.

### CQ-21: `_dirtyStateManager.OnUnsavedChanges` never unsubscribed (FIXED 2026-06-17)
Added `-=` unsubscription in `Dispose()`. Previously only subscribed in
`InitializeManager` with no matching cleanup.
**Where:** `FormsManager.Lifecycle.cs:39`.

### CQ-22: `Blocks` property returned mutable ConcurrentDictionary (FIXED 2026-06-17)
Replaced `=> _blocks` with `=> new ReadOnlyDictionary<string, DataBlockInfo>(_blocks)`.
Prevents callers from casting to `ConcurrentDictionary` and mutating internal state.
**Where:** `FormsManager.Properties.cs:44`.

### CQ-24: `PostBlockAsync` chain completed (FIXED 2026-06-17)
Added `PostBlockAsync` to `FormsManager.BasicDataOps.cs` that calls `UoW.SaveChangesAsync`
(validate + send, no commit). Added to `IUnitofWorksManager` interface so host layer can
call it without casting. The `BeepBuiltins.Post()` can now be updated to call
`Host.PostBlockAsync()` instead of `Commit()`.
**Where:** `FormsManager.BasicDataOps.cs:286-307`, `Interfaces/IUnitofWorksManager.cs:430`.

### CQ-25: Missing interface methods added to `IUnitofWorksManager` (FIXED 2026-06-17)
Added 25+ critical methods to the interface: alerts (`SetMessage`, `ClearMessage`,
`ShowAlertAsync`), inter-form communication (`SetGlobalVariable`, `GetGlobalVariable`,
`PostMessage`, `BroadcastMessage`, `SubscribeToMessage`, `UnsubscribeFromMessage`,
`SendParameterToForm`), key triggers (`RegisterKeyTrigger`, `FireKeyTriggerAsync`),
multi-form navigation (`CallFormAsync`, `OpenFormModelessAsync`, `NewFormAsync`,
`ReturnToCallerAsync`), and `RaiseFormTriggerAsync`. Hosts no longer need to cast
`IUnitofWorksManager` to `FormsManager`.
**Where:** `Interfaces/IUnitofWorksManager.cs:433-458`.

### CQ-26: `ShowAlertAsync` on `IBeepFormsHost` — adapter no longer a stub (FIXED 2026-06-17)
Added `ShowAlertAsync` to `IBeepFormsHost`. `BeepFormsHostAdapter` now delegates
directly to `_host.ShowAlertAsync(...)` instead of returning a hardcoded
`Task.FromResult(1)`.
**Where:** `Hosts/IBeepFormsHost.cs:68-69`, `Builtins/BeepFormsHostAdapter.cs:72-73`.

### CQ-27: 4 remaining silent catch blocks — added logging (FIXED 2026-06-17)
Added `LogError` to cross-form rollback catch in `FormOperations.cs`. Added
`Debug.WriteLine` to `BeepFormsHostAdapter.ListLovRecords` catch.
`BlockPropertyManager.GetBlockProperty<T>` and `DirtyStateManager` dynamic catch
are legitimate type-conversion / optional-feature guards — left as-is with comments.
**Where:** `FormsManager.FormOperations.cs:687`, `Builtins/BeepFormsHostAdapter.cs:137`.

### CQ-28: Fragile string-type references replaced (FIXED 2026-06-17)
Replaced `"TheTechIdea.Beep.ConfigUtil.PassedArgs"` string resolution with a
`Lazy<Type>` cached field. Falls back to `typeof(object)` if the assembly reference
isn't available. Compile-time-validated type name.
**Where:** `FormsManager.ExtendedOperations.cs:21-23,486,527`.
Added null-conditional operators (`?.`) to `_lockManager`, `_dirtyStateManager` field
accesses on other FormsManager instances. These fields are initialized in the constructor
but the null-conditional provides defense-in-depth.
**Where:** `FormsManager.FormOperations.cs:307,673,686`.

### G1.6: `DBMS_APPLICATION_INFO` built-ins (FIXED 2026-06-17)

**Fix:** Added `ClientInfo` model with `ClientInfo`, `ModuleName`, `Action`, `ClientHost`,
`ClientIpAddress`, and `UserName`. FormsManager exposes `SetClientInfo`, `SetClientModule`,
`SetClientAction` methods. Datasource-agnostic — each driver translates these into its
native equivalent where supported.

**Where:** `Models/ClientInfo.cs` (new), `FormsManager.RecordGroups.cs:157-210`.

### G1.7: `CLIENT_HOST` / `CLIENT_INFO` built-ins (FIXED 2026-06-17)

**Fix:** Combined with G1.6. FormsManager exposes `SetClientHost`, `SetClientIpAddress`,
`GetClientHost` (defaults to `Environment.MachineName`), `GetClientIpAddress`.

**Where:** Same as G1.6.

---

## P2 — Data Management Nice-to-Have

### G2.1: Built-in query construction language (ENHANCED 2026-06-17)

**Enhancement:** The existing `ParseWhereClause` in `QueryBuilderManager` was enhanced with
proper parentheses-aware AND splitting, IN clause parsing, BETWEEN val1 AND val2 support,
and parameterized placeholder handling (`:1`, `:name`). The basic parser already existed;
this update added the missing operator support and robustness.

**Where:** `Helpers/QueryBuilderManager.cs:89-180`. `ParseWhereClause` + `SplitWhereConditions` + enhanced `ParseCondition`.

---

### G2.2: `EDITOR` / `TEXT_IO` built-ins (FIXED 2026-06-17)

**Fix:** Added `ReadTextFileAsync`, `WriteTextFileAsync`, `AppendTextFileAsync`,
`ReadTextLinesAsync` (TEXT_IO equivalents) and `GetMultiLineText`/`SetMultiLineText`
(EDITOR equivalents) to FormsManager. File I/O operations are datasource-agnostic.

**Where:** `FormsManager.ExtendedOperations.cs:39-82`.

### G2.3: `VARR` / batch operations — DEFERRED. Existing batch commit (`CommitFormBatchAsync`)
already handles bulk DML. Per-record VARR arrays are a niche Oracle concept.

### G2.4: `DBMS_PIPE` / `DBMS_ALERT` — DEFERRED. Datasource-agnostic engine; cross-session
messaging is a datasource-specific concern. Use `IFormMessageBus` for inter-form messaging.

### G2.5: `SET_APPLICATION_PROPERTY` presets (FIXED 2026-06-17)

**Fix:** Added `SetApplicationProperty`/`GetApplicationProperty`/`HasApplicationProperty`/
`RemoveApplicationProperty` to FormsManager with a thread-safe `ConcurrentDictionary` backing.
The host can set/read any property key — presets like `CURSOR_MODE` and `DATA_MODE` are
just conventions on a generic property bag.

**Where:** `FormsManager.ExtendedOperations.cs:14-30`.

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

#### G3.1: Bookmarks (FIXED 2026-06-17)

**Fix:** Added `SetBlockBookmark`, `GoToBlockBookmark`, `RemoveBlockBookmark`,
`ClearBlockBookmarks` to FormsManager. Delegates to UoW via reflection.

**Where:** `FormsManager.ExtendedOperations.cs:88-127`.

---

#### G3.2: Computed Columns (FIXED 2026-06-17)

**Fix:** Added `RegisterBlockComputed`, `UnregisterBlockComputed`, `GetBlockComputedValue`,
`GetBlockComputedColumnNames`, `GetAllBlockComputedValues` to FormsManager.
Thread-safe via `ConcurrentDictionary`. Evaluates computation against current UoW record.

**Where:** `FormsManager.ExtendedOperations.cs:131-180`.

---

#### G3.3: Freeze / Batch Update (FIXED 2026-06-17)

**Fix:** Added `FreezeBlock`, `UnfreezeBlock`, `BeginBlockBatchUpdate` to FormsManager.
Delegates to UoW via reflection. Safe no-op when UoW doesn't support the feature.

**Where:** `FormsManager.ExtendedOperations.cs:184-218`.

---

#### G3.4: Entity-Level Search / Clone (FIXED 2026-06-17)

**Fix:** Added `FindBlockRecordAsync`, `FindBlockRecordsAsync`, `CloneBlockRecordAsync` to
FormsManager. Delegates to UoW `FindAsync`/`FindManyAsync`/`CloneItem` via reflection
with async support.

**Where:** `FormsManager.ExtendedOperations.cs:222-274`.

---

#### G3.5: UoW Change Log (FIXED 2026-06-17)

**Fix:** Added `GetBlockDetailedChangeLog` to FormsManager. Delegates to UoW
`GetChangeLog` to get per-property before/after values. Returns empty list on
unsupported UoW.

**Where:** `FormsManager.ExtendedOperations.cs:278-291`.

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

#### G3.7: UoW Virtual/Lazy Loading (FIXED 2026-06-17)

**Fix:** Added `EnableBlockVirtualMode`, `DisableBlockVirtualMode`, `GoToBlockPageAsync`,
`PrefetchBlockAdjacentPagesAsync` to FormsManager. Delegates to UoW native virtual mode
methods. Complements existing `FormsManager.Performance.cs` paging — callers can choose
the UoW-native or the FormsManager-level paging path.

**Where:** `FormsManager.ExtendedOperations.cs:295-343`.

---

### IDataSource features not surfaced by FormsManager

> **Design note:** `IDataSource` abstracts RDBMS, NoSQL, file, and web API
> sources. The gaps below must work with any data source or degrade
> gracefully when a source doesn't support a capability.

#### G3.8: Relationship auto-discovery — DEFERRED. Datasource-dependent metadata;
RDBMS has FKs, files don't. Opt-in only via `SetupBlockAsync` when source supports it.

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

#### G3.9: Entity lifecycle operations — DEFERRED. Datasource-dependent DDL (creates
tables for RDBMS, collections for NoSQL, file schemas for files, no-op for web APIs).
Too complex to surface at the engine level; each host/driver should own entity lifecycle.

---

#### G3.10: Source-level aggregate queries (FIXED 2026-06-17)

**Fix:** Added `GetBlockAggregateScalarAsync` to FormsManager. Delegates to
`IDataSource.GetScalarAsync` for COUNT/MAX/MIN/SUM that hit the source directly
instead of computing on in-memory UoW data.

**Where:** `FormsManager.ExtendedOperations.cs:347-368`.

---

#### G3.11: Source-level transactions (FIXED 2026-06-17)

**Fix:** Added `BeginFormTransaction`, `EndFormTransaction`, `CommitFormTransaction` to
FormsManager. Iterates over all blocks' datasources, attempting to create a shared
transaction boundary. On sources that don't support transactions (file, web API),
catches and silently continues. Cross-block atomicity when the source supports it.

**Where:** `FormsManager.ExtendedOperations.cs:372-443`.

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
