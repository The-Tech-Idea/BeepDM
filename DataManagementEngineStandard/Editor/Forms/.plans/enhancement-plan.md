# FormsManager Enhancement Plan

> **Inspired by Oracle Forms** — phased roadmap to bring full Oracle Forms parity and modern data management capabilities to the Beep FormsManager system.

**Current Score:** 9.5/10  
**Target Score:** 9.5/10  
**Audit Date:** 2026-04-09  
**Namespace:** `TheTechIdea.Beep.Editor.UOWManager`  
**Implementation Standard:** A phase is considered complete in this plan when the runtime capability exists in code and the corresponding help-site closeout has been applied where required.

---

## Phase Documents

Use the detailed phase documents below when creating, enhancing, updating, or fixing FormsManager.
The roadmap in this file stays high-level; the per-phase files capture implementation seams,
UoW considerations, primary-key handling rules, and validation guidance.

- [Phase 01 — Core Completion & Stabilization](phases/phase-01-core-completion-stabilization.md)
- [Phase 02 — Oracle Forms Built-in Emulation](phases/phase-02-oracle-builtins-emulation.md)
- [Phase 03 — Multi-Form & Cross-Form Communication](phases/phase-03-multi-form-cross-form.md)
- [Phase 04 — Advanced Trigger System](phases/phase-04-advanced-trigger-system.md)
- [Phase 05 — Audit Trail & Change Tracking](phases/phase-05-audit-trail-change-tracking.md)
- [Phase 06 — Security & Authorization](phases/phase-06-security-authorization.md)
- [Phase 07 — Performance & Scalability](phases/phase-07-performance-scalability.md)
- [Phase 08 — Testing & Documentation](phases/phase-08-testing-documentation.md)
- [Phase 09 — Help Documentation Update](phases/phase-09-help-documentation-update.md)

---

## Implementation Audit Snapshot

- Phases 01 through 09 are now complete across the FormsManager partials, helper managers, interfaces, tests, reference docs, and help-site page.
- `Help/formsmanager.html` was rewritten on 2026-04-09 to align the public help page with the audited runtime surface.
- The integrated WinForms host seam now mirrors trigger metadata and normalized block-level UoW activity through `BeepForms` / `IBeepFormsHost`, keeping `BeepBlock` on the UI side of the runtime boundary.
- Use [todo-tracker.md](todo-tracker.md) as the operational status source of truth. The detailed phase checklists later in this file are retained as historical implementation notes and seam references.

---

## Cross-Cutting Rules: UoW and Primary-Key Handling

- FormsManager orchestrates form behavior, but `IUnitofWork` remains the source of truth for persisted state, dirty tracking, insert/update/delete execution, commit, rollback, and datasource-backed key generation.
- New-record flow should stay consistent: create typed instance from `DataBlockInfo.EntityType` → apply audit/default values → apply primary-key strategy → fire `WHEN-CREATE-RECORD` / pre-insert logic → insert through UoW → refresh database-generated values → synchronize dependents.
- Primary-key strategy order:
	1. If the caller or trigger already supplied a valid key, preserve it.
	2. If the field is datasource-managed identity / auto-increment, leave it unset client-side and refresh it after `InsertAsync` / `CommitFormAsync`.
	3. If the block uses a real sequence, prefer `IUnitofWork.GetSeq(...)` / datasource-backed sequencing before FormsManager's in-memory sequence provider.
	4. Use `ISequenceProvider` for Oracle-style built-ins, deterministic tests, or non-database-backed scenarios.
	5. For GUID or custom keys, use item defaults or triggers explicitly; do not guess.
	6. Composite keys must be handled per field; never auto-number a composite key blindly.
- Never consume sequence values during query, paging, navigation, or cache prefetch. Sequence/identity acquisition belongs only to create/insert flows.
- Master/detail creation must respect key timing: preallocated sequence keys can flow into child FKs before commit, but identity keys usually require parent insert/refresh before the detail key is stable.

---

## Architecture Overview (Current State)

```
FormsManager (IUnitofWorksManager)
├── Core (FormsManager.cs)                    ─ Block registration, DI, properties
├── Navigation (FormsManager.Navigation.cs)   ─ Record/block navigation
├── FormOperations (.FormOperations.cs)       ─ Open/Close/Commit/Rollback form
├── EnhancedOperations (.EnhancedOperations.cs) ─ Type-safe CRUD, insert/delete
├── ModeTransitions (.ModeTransitions.cs)     ─ ENTER_QUERY / EXECUTE_QUERY
├── DataOperations (.DataOperations.cs)       ─ Undo/Redo, aggregates, batch, export
│
├── 18 Helper Managers (DI-injected)
│   ├── RelationshipManager       ─ Master-detail relationships
│   ├── DirtyStateManager         ─ Change tracking
│   ├── EventManager              ─ UoW event subscriptions
│   ├── FormsSimulationHelper     ─ Oracle Forms built-in emulation
│   ├── PerformanceManager        ─ Caching / performance stats
│   ├── ConfigurationManager      ─ Form/block configuration
│   ├── SystemVariablesManager    ─ :SYSTEM.* variable emulation
│   ├── ValidationManager         ─ WHEN-VALIDATE-ITEM/RECORD
│   ├── LOVManager                ─ List of Values
│   ├── ItemPropertyManager       ─ SET/GET_ITEM_PROPERTY
│   ├── TriggerManager            ─ Oracle Forms triggers
│   ├── SavepointManager          ─ Named savepoints + rollback
│   ├── LockManager               ─ Client-side record locking
│   ├── QueryBuilderManager       ─ Filter/query building
│   ├── BlockErrorLog             ─ Per-block error log
│   ├── MessageQueueManager       ─ Platform-agnostic messaging
│   ├── BlockFactory              ─ UoW + EntityStructure from connection
│   └── CrossBlockValidationManager ─ Multi-block validation rules
│
├── Models (55+ classes)
├── Interfaces (IUnitofWorksManagerInterfaces.cs)
└── Configuration (Form/Nav/Perf/Validation configs)
```

---

## What's Working Well (Production-Ready)

- ✅ Core block registration with ConcurrentDictionary
- ✅ Master-detail relationship management
- ✅ Mode transitions (ENTER_QUERY → EXECUTE_QUERY → CRUD)
- ✅ Form operations (Open/Close/Commit/Rollback with trigger events)
- ✅ Navigation with validation, unsaved changes checking, nav history
- ✅ Dirty state tracking across blocks and relationships
- ✅ Trigger system architecture (form/block/item/global scope, sync+async)
- ✅ Validation framework (item/record/block/form levels, fluent builder)
- ✅ LOV manager structure (register/load/cache/validate)
- ✅ Item property manager (SET/GET_ITEM_PROPERTY equivalents)
- ✅ Savepoint manager (named snapshots + rollback)
- ✅ Record locking (auto-lock, modes, per-block)
- ✅ Query builder with templates
- ✅ Undo/Redo via IUndoable UOW capability
- ✅ Batch commit with progress
- ✅ Export/Import (JSON, CSV, DataTable)
- ✅ Block aggregates (Sum, Average, Count)
- ✅ System variables (:SYSTEM.* emulation)
- ✅ Performance caching and statistics
- ✅ Configuration system (form/navigation/performance/validation)

---

## Post-Closeout Maintenance

### Ongoing documentation maintenance
1. **Planning and documentation alignment must stay synchronized** — the `.plans` files, mapping document, README guidance, migration guidance, and help page should continue to describe the same audited implementation state.

### Residual runtime hardening
2. **Reflection-heavy UoW seams still deserve cleanup** — some CRUD and update paths still rely on reflection-based fallbacks instead of strongly-typed UoW contracts.
3. **Programmatic trigger-raise coverage needs continued audit** — `RaiseFormTriggerAsync` exists, but explicit call-site usage should continue to be reviewed where direct trigger raising is expected.
4. **Remote cache invalidation remains a hardening area** — local cache controls exist, but external datasource change notification still depends on the surrounding performance and invalidation plumbing.
5. **Integrated trigger/UoW UI seams should stay proxy-based** — extend `BeepForms` / `IBeepFormsHost` when WinForms blocks need more runtime observability, rather than binding `BeepBlock` directly to `FormsManager` or raw block UoWs.

### Intentionally UI-owned Oracle Forms concepts
6. **Canvases, tab pages, focus rendering, LOV dialog presentation, and message-area rendering stay outside FormsManager** — these belong to the host UI and are not blockers for FormsManager runtime parity.

---

## Historical Phase Backlog

The detailed phase checklists below are retained as historical implementation notes and seam references. They are not the active measure of completion anymore. At the time of this audit, Phases 01 through 09 are complete.

---

## Phase 1: Core Completion & Stabilization

**Goal:** Complete all interface methods, fix type safety, ensure all existing managers are properly wired.  
**Priority:** Critical  
**Estimated Scope:** ~30 tasks

### 1.1 Generic Block Registration
- [x] Add `RegisterBlock<T>(string blockName, IUnitofWork<T> uow, IEntityStructure es, ...)` overload
- [x] Store `Type` reference in `DataBlockInfo` for runtime type resolution
- [x] Add `GetBlock<T>(string blockName)` typed accessor
- [x] Update `CreateNewRecord` to use stored `Type` instead of `ExpandoObject` fallback
- [ ] Add `InsertRecordAsync<T>(string blockName, T record)` generic CRUD overload

### 1.2 Complete Trigger Wiring
- [ ] Wire `PreInsert`/`PostInsert` triggers in `InsertRecordEnhancedAsync`
- [ ] Wire `PreUpdate`/`PostUpdate` triggers in update operations
- [ ] Wire `PreDelete`/`PostDelete` triggers in `DeleteCurrentRecordAsync`
- [ ] Wire `PreQuery`/`PostQuery` triggers in `ExecuteQueryEnhancedAsync`
- [ ] Wire `PreCommit`/`PostCommit` triggers in `CommitFormAsync`
- [ ] Wire `WhenNewBlockInstance` trigger in `SwitchToBlockAsync`
- [ ] Wire `WhenCreateRecord` trigger in `CreateNewRecord`
- [ ] Wire `WhenRemoveRecord` trigger in delete operations
- [ ] Wire `OnPopulateDetails` trigger in `SynchronizeDetailBlocksAsync`
- [ ] Add trigger result handling: if `TriggerResult.Cancel`, abort the operation

### 1.3 Validation Integration
- [x] Call `ValidationManager.ValidateItem` automatically on field change (in `OnBlockFieldChanged`)
- [ ] Call `ValidationManager.ValidateRecord` before commit per record
- [ ] Call `CrossBlockValidationManager.Validate` in `CommitFormAsync` before save
- [ ] Integrate `ValidationManager.ValidateBlock` in `ExecuteQueryAndEnterCrudModeAsync` post-query
- [ ] Add `ValidateBeforeNavigation` config option — validate current record before `NextRecord`/`PreviousRecord`

### 1.4 LOV Full Integration  
- [ ] Wire LOV validation in `OnBlockFieldChanged` when item has registered LOV
- [ ] Auto-populate related fields when LOV value selected (use `GetRelatedFieldValues`)
- [ ] Add `ShowLOVAsync(blockName, fieldName)` method on FormsManager (Oracle: SHOW_LOV)
- [ ] Add cascading LOV support — when parent LOV value changes, reload dependent LOV
- [ ] Implement LOV auto-complete — `FilterLOVData` on keystroke

### 1.5 Missing Interface Implementations
- [ ] Audit `IUnitofWorksManager` interface vs `FormsManager` — implement any missing methods
- [ ] Ensure `ValidateField`, `ValidateBlock`, `ValidateForm` delegate to `ValidationManager`
- [ ] Ensure `InsertRecordAsync`, `DeleteCurrentRecordAsync` (interface signatures) are properly routed
- [ ] Implement `DuplicateCurrentRecordAsync` using `IUnitofWorkHistory.CloneItem`

---

## Phase 2: Oracle Forms Built-in Emulation

**Goal:** Add the set of Oracle Forms built-in procedures/functions that developers expect.  
**Priority:** High  
**Estimated Scope:** ~25 tasks

### 2.1 Block Property Built-ins
- [ ] Add `SetBlockProperty(string blockName, string property, object value)` on FormsManager
- [ ] Add `GetBlockProperty(string blockName, string property)` on FormsManager
- [ ] Supported properties: `INSERT_ALLOWED`, `UPDATE_ALLOWED`, `DELETE_ALLOWED`, `QUERY_ALLOWED`, `DEFAULT_WHERE`, `ORDER_BY`, `RECORDS_DISPLAYED`, `CURRENT_RECORD`, `TOP_RECORD`
- [ ] Wire property changes to DataBlockInfo fields
- [ ] Add `IBlockPropertyManager` interface for extensibility

### 2.2 Navigation Built-ins
- [ ] Add `GoBlock(string blockName)` — switch focus to block (alias for `SwitchToBlockAsync`)
- [ ] Add `GoItem(string blockName, string itemName)` — set current item focus
- [ ] Add `GoRecord(string blockName, int recordIndex)` — navigate to specific record
- [ ] Add `NextBlock()` / `PreviousBlock()` — cycle through registered blocks
- [ ] Add `NextItem()` / `PreviousItem()` — cycle through items using `ItemPropertyManager` tab order
- [ ] Wire `KEY-NEXT-ITEM`, `KEY-PREV-ITEM` trigger types into item navigation

### 2.3 Message & Alert Built-ins
- [ ] Add `SetMessage(string text, MessageLevel level)` at FormsManager level (Oracle: MESSAGE)
- [ ] Add `ShowAlertAsync(string title, string message, AlertStyle style)` → returns `AlertResult` (Button1/Button2/Button3)
- [ ] Define `AlertStyle` enum: `Stop`, `Caution`, `Note`
- [ ] Define `AlertResult` enum: `Button1`, `Button2`, `Button3`
- [ ] Add `IAlertProvider` interface — inject UI-layer implementation for platform independence
- [ ] Add `ClearMessage()` built-in

### 2.4 Sequence & Default Value Built-ins
- [ ] Add `GetNextSequence(string sequenceName)` → returns next value from configured sequences
- [ ] Add `ISequenceProvider` interface — inject DB or in-memory sequence generator
- [ ] Add `SetItemDefault(string blockName, string itemName, Func<object> defaultFactory)` — dynamic defaults
- [ ] Add `CopyFieldValue(string srcBlock, string srcField, string destBlock, string destField)` built-in
- [ ] Add `PopulateGroupFromBlock(string blockName, string groupName)` — record group population

### 2.5 Timer Management
- [ ] Add `CreateTimer(string timerName, TimeSpan interval, bool repeating)` on FormsManager
- [ ] Add `DeleteTimer(string timerName)`
- [ ] Add `GetTimer(string timerName)` → `TimerInfo`
- [ ] Add `WHEN-TIMER-EXPIRED` trigger type
- [ ] Wire timer expiration to trigger execution
- [ ] Add `ITimerManager` interface with registration, lifecycle, and events

---

## Phase 3: Multi-Form & Cross-Form Communication

**Goal:** Enable Oracle Forms multi-form patterns (CALL_FORM, OPEN_FORM, NEW_FORM) and inter-form data sharing.  
**Priority:** High  
**Estimated Scope:** ~20 tasks

### 3.1 Form Registry
- [ ] Create `IFormRegistry` — singleton registry of all active FormsManager instances
- [ ] Add `RegisterForm(string formName, IUnitofWorksManager form)` / `UnregisterForm`
- [ ] Add `GetForm(string formName)` → `IUnitofWorksManager`
- [ ] Add `GetActiveFormNames()` → `IReadOnlyList<string>`
- [ ] Add form lifecycle events: `OnFormRegistered`, `OnFormUnregistered`

### 3.2 Multi-Form Navigation
- [ ] Add `CallFormAsync(string formName, ...)` — Oracle CALL_FORM: opens child form, suspends parent
- [ ] Add `OpenFormAsync(string formName, ...)` — Oracle OPEN_FORM: opens independent form
- [ ] Add `NewFormAsync(string formName, ...)` — Oracle NEW_FORM: closes current, opens new
- [ ] Add `ReturnToCallerAsync()` — return from CALL_FORM with optional data
- [ ] Add form modality: `Modal`, `Modeless`
- [ ] Add `FormCallStack` for CALL_FORM nesting

### 3.3 Inter-Form Communication
- [ ] Add `SetGlobalVariable(string name, object value)` / `GetGlobalVariable(string name)` — Oracle :GLOBAL.*
- [ ] Add `SendParameterToForm(string formName, string paramName, object value)` — Oracle parameter lists
- [ ] Add `IFormMessageBus` — publish/subscribe between forms
- [ ] Add `PostMessage(string targetForm, string messageType, object payload)`
- [ ] Add `OnFormMessage` event on FormsManager for receiving messages

### 3.4 Shared Data Blocks
- [ ] Add `CreateSharedBlock(string blockName, IUnitofWork uow)` — block visible across forms
- [ ] Add `GetSharedBlock(string blockName)` from any form
- [ ] Add lock coordination for shared blocks
- [ ] Add change notification across forms when shared block is modified

---

## Phase 4: Advanced Trigger System

**Goal:** Complete Oracle Forms trigger parity and add modern extensions.  
**Priority:** Medium-High  
**Estimated Scope:** ~20 tasks

### 4.1 Key Triggers
- [ ] Add `KeyTriggerType` enum: `KEY_NEXT_ITEM`, `KEY_PREV_ITEM`, `KEY_NEXT_RECORD`, `KEY_PREV_RECORD`, `KEY_COMMIT`, `KEY_EXIT`, `KEY_OTHERS`, `KEY_F1` through `KEY_F12`
- [ ] Add `RegisterKeyTrigger(KeyTriggerType type, string blockName, ...)` on `ITriggerManager`
- [ ] Add `FireKeyTrigger(KeyTriggerType type, string blockName, ...)` — invokable by UI layer
- [ ] Wire key triggers into navigation and commit operations

### 4.2 DML Triggers (Transactional)
- [ ] Add `ON-INSERT`, `ON-UPDATE`, `ON-DELETE`, `ON-LOCK` transactional triggers
- [ ] These replace default DML — give full control to the developer
- [ ] Wire into `CommitFormAsync` flow: if `ON-INSERT` trigger exists, call it instead of UoW default
- [ ] Add `RAISE_FORM_TRIGGER(triggerName)` — programmatically fire a named trigger

### 4.3 Trigger Chaining & Dependency
- [ ] Add trigger dependency graph: trigger A fires trigger B
- [ ] Add circular dependency detection
- [ ] Add `TriggerChainMode`: `StopOnFailure`, `ContinueOnFailure`, `RollbackOnFailure`
- [ ] Add execution timeout per trigger with configurable `TriggerTimeout`
- [ ] Add trigger execution log with timing for diagnostics

### 4.4 Trigger Templates & Libraries
- [ ] Create `TriggerLibrary` — pre-built common triggers (audit stamping, cascading deletes, field formatting)
- [ ] Add `AutoNumberTrigger(blockName, fieldName, sequenceName)` — auto-generate IDs
- [ ] Add `AuditStampTrigger(blockName)` — set CreatedBy/ModifiedBy/timestamps
- [ ] Add `CascadeDeleteTrigger(masterBlock, detailBlock)` — delete details when master deleted
- [ ] Add `FormatFieldTrigger(blockName, fieldName, format)` — auto-format on field change

---

## Phase 5: Audit Trail & Change Tracking

**Goal:** Enterprise-grade change tracking with full audit capabilities.  
**Priority:** Medium  
**Estimated Scope:** ~15 tasks

### 5.1 Audit Log Infrastructure
- [ ] Create `IAuditManager` interface
- [ ] Add `AuditEntry` model: `Timestamp`, `User`, `BlockName`, `RecordKey`, `FieldName`, `OldValue`, `NewValue`, `OperationType`
- [ ] Add `AuditConfiguration`: enable/disable per block, per field, retention policy
- [ ] Add `GetAuditLog(blockName, filters)` → `IReadOnlyList<AuditEntry>`
- [ ] Add `GetFieldHistory(blockName, recordKey, fieldName)` → field value timeline

### 5.2 Automatic Audit Capture
- [ ] Wire into `OnBlockFieldChanged` to capture field-level changes
- [ ] Wire into `CommitFormAsync` to capture commit-level audit entries
- [ ] Add `BeforeImage` / `AfterImage` record snapshotting per commit
- [ ] Add configurable audit fields: `CreatedBy`, `CreatedDate`, `ModifiedBy`, `ModifiedDate`
- [ ] Add `SetAuditUser(string userName)` — set current user for audit stamping

### 5.3 Audit Persistence
- [ ] Add `IAuditStore` interface — pluggable storage (in-memory, file, database)
- [ ] Add `InMemoryAuditStore` default implementation
- [ ] Add `FileAuditStore` — JSON file per form session
- [ ] Add audit export to CSV/JSON
- [ ] Add audit retention policy with auto-purge

---

## Phase 6: Security & Authorization

**Goal:** Row-level and field-level security with role-based access control.  
**Priority:** Medium  
**Estimated Scope:** ~15 tasks

### 6.1 Security Manager
- [ ] Create `ISecurityManager` interface
- [ ] Add `SecurityContext` model: current user, roles, permissions
- [ ] Add `SetSecurityContext(SecurityContext ctx)` on FormsManager
- [ ] Add `BlockSecurity` model: `CanQuery`, `CanInsert`, `CanUpdate`, `CanDelete` per role
- [ ] Add `FieldSecurity` model: `CanView`, `CanEdit` per role per field

### 6.2 Block-Level Security
- [ ] Add `SetBlockSecurity(string blockName, BlockSecurity security)`
- [ ] Enforce block security in `InsertRecordEnhancedAsync`, `DeleteCurrentRecordAsync`, etc.
- [ ] Auto-set `InsertAllowed`/`UpdateAllowed`/`DeleteAllowed` from security context
- [ ] Block query restrictions per role (appended WHERE clause)
- [ ] Raise security violation events

### 6.3 Field-Level Security
- [ ] Integrate with `ItemPropertyManager` — auto-set Enabled/Visible from security
- [ ] Add field masking for sensitive data (PII, SSN, credit card)
- [ ] Add `IFieldMaskProvider` interface for custom masking
- [ ] Add UI hints for read-only secured fields
- [ ] Log unauthorized access attempts

---

## Phase 7: Performance & Scalability

**Goal:** Handle large datasets, virtual scrolling support, and optimized data loading.  
**Priority:** Medium  
**Estimated Scope:** ~15 tasks

### 7.1 Virtual Scrolling / Paging
- [ ] Add `IPagingManager` interface on FormsManager
- [ ] Add `SetBlockPageSize(string blockName, int pageSize)` 
- [ ] Add `LoadPageAsync(string blockName, int pageNumber)` — server-side paging
- [ ] Add `GetTotalRecordCount(string blockName)` — count without loading all records
- [ ] Add configurable fetch-ahead: load next page when user approaches end

### 7.2 Lazy Loading
- [ ] Add `LazyLoadMode` on DataBlockInfo: `None`, `OnDemand`, `OnScroll`
- [ ] Detail blocks load on master record navigation (already done, optimize)
- [ ] Add field-level lazy loading for BLOB/CLOB fields
- [ ] Add `MaxRecordsPerFetch` config to prevent loading entire tables
- [ ] Add query result set streaming support

### 7.3 Caching Improvements
- [ ] Add LRU eviction policy for block data cache in PerformanceManager
- [ ] Add cache invalidation on external data changes (polling or push)
- [ ] Add block-level cache TTL configuration
- [ ] Add memory pressure monitoring — auto-evict when threshold exceeded
- [ ] Add cache hit/miss ratio logging to PerformanceStatistics

---

## Phase 8: Testing & Documentation

**Goal:** Comprehensive test coverage and developer documentation.  
**Priority:** High (runs parallel to all phases)  
**Estimated Scope:** ~20 tasks

### 8.1 Unit Tests
- [x] Create `FormsManager.Core.Tests` — RegisterBlock, UnregisterBlock, GetBlock, BlockExists
- [x] Create `FormsManager.Navigation.Tests` — record history, back/forward replay, direct-record navigation, validate-before-navigation
- [x] Create `FormsManager.FormOperations.Tests` — Open/Close/Commit no-dirty path, unsaved-change handling
- [x] Create `FormsManager.ModeTransitions.Tests` — CRUD→Query, Query→CRUD execution, unsaved-change blocking, new-record CRUD entry
- [x] Create `TriggerManager.Tests` — async registration/fire, priority order, failure chain behavior, suspend/resume
- [x] Create `ValidationManager.Tests` — Rule registration, item/record/block validation
- [x] Create `LOVManager.Tests` — register/load/cache/filter/validate LOVs and related-field mapping
- [x] Create `SavepointManager.Tests` — metadata capture, generated names, rollback pruning, release behavior
- [x] Create `LockManager.Tests` — mode config, current-record locking, auto-lock, unlock cleanup

### 8.2 Integration Tests
- [x] Master-detail cascade operations (master `CurrentChanged` → relationship filter build → detail `Get(filters)` sync)
- [x] Full form lifecycle: Open → RegisterBlocks → EnterQuery → ExecuteQuery → Edit → Commit → Close
- [x] Multi-block validation with cross-block rules
- [x] Concurrent block operations (overlapping block-local navigation keeps per-block record state consistent)
- [x] LOV with real data source loading
- [x] Export → Import round-trip (JSON, CSV)

### 8.3 Documentation
- [x] Update `README.md` with Phase 1-7 API additions
- [x] Create `MIGRATION-GUIDE.md` for upgrading from earlier versions
- [x] Create `ORACLE-FORMS-MAPPING.md` — side-by-side Oracle Forms ↔ FormsManager reference
- [x] Create per-helper `README.md` (TriggerManager, ValidationManager, LOVManager, etc.)
- [x] Add XML doc coverage to 100% for public API surface

---

## Phase Status Snapshot

| Phase | Current State | Notes |
|-------|---------------|-------|
| 1 — Core Completion & Stabilization | Implemented in code | Typed block registration, CRUD orchestration, validation integration, and interface routing are present in the runtime. |
| 2 — Oracle Forms Built-in Emulation | Implemented in code | Block properties, navigation built-ins, alerts, sequences, defaults, and timers exist on the FormsManager surface. |
| 3 — Multi-Form & Cross-Form Communication | Implemented in code | Modal/modeless/replace form flows, form registry, globals, message bus, and shared blocks are present. |
| 4 — Advanced Trigger System | Implemented in code | Key triggers, DML triggers, trigger chaining, dependency tracking, and trigger-library helpers exist. |
| 5 — Audit Trail & Change Tracking | Implemented in code | Audit manager, audit stores, field history, and export/purge operations are implemented. |
| 6 — Security & Authorization | Implemented in code | Security context, block security, field security, masking, and violation handling are available. |
| 7 — Performance & Scalability | Implemented in code | Paging, fetch-ahead, lazy loading, cache controls, and performance statistics are implemented. |
| 8 — Testing & Documentation | Implemented in code | Test projects, README updates, migration guidance, mapping docs, and helper READMEs exist in the repo. |
| 9 — Help Documentation Update | Complete | `Help/formsmanager.html` now reflects the audited FormsManager runtime surface and Oracle Forms mapping. |

---

## Oracle Forms Coverage Summary

- Core lifecycle, block properties, navigation, query, CRUD, LOV orchestration, alerts, sequences, timers, multi-form navigation, inter-form messaging, audit, security, paging, savepoints, and locking are implemented in code.
- The remaining open work is documentation closeout and targeted hardening, not broad feature invention.
- UI-layer concerns such as canvases, visual focus management, LOV dialog rendering, and message-area rendering remain intentionally outside FormsManager.

---

## Post-Roadmap Follow-up

1. Keep [todo-tracker.md](todo-tracker.md), [ORACLE-FORMS-MAPPING.md](ORACLE-FORMS-MAPPING.md), [formsmanager.html](../../../../Help/formsmanager.html), and this enhancement plan aligned after any runtime changes.
2. Continue hardening reflection-heavy UoW seams, explicit programmatic trigger-raise usage, and external cache invalidation integration where needed.
3. Archive or rewrite stale historical planning notes outside `.plans` so they no longer read as active roadmap items.
