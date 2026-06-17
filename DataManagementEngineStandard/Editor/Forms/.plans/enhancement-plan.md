# FormsManager Enhancement Plan

> **Inspired by Oracle Forms** - phased roadmap to bring full Oracle Forms parity and modern data management capabilities to the Beep FormsManager system.

**Current Score:** 9.5/10 (engine)  
**Target Score:** 10/10 (full Oracle Forms parity + modern UoW surface)  
**Audit Date:** 2026-06-17  
**Namespace:** `TheTechIdea.Beep.Editor.UOWManager`
**Implementation Standard:** A phase is considered complete when the runtime capability exists in code and the corresponding help-site closeout has been applied where required.

---

## Phase Documents

Use the detailed phase documents below when creating, enhancing, updating, or fixing FormsManager.
The roadmap in this file stays high-level; the per-phase files capture implementation seams,
UoW considerations, primary-key handling rules, and validation guidance.

- [Phase 01 - Core Completion & Stabilization](phases/phase-01-core-completion-stabilization.md)
- [Phase 02 - Oracle Forms Built-in Emulation](phases/phase-02-oracle-builtins-emulation.md)
- [Phase 03 - Multi-Form & Cross-Form Communication](phases/phase-03-multi-form-cross-form.md)
- [Phase 04 - Advanced Trigger System](phases/phase-04-advanced-trigger-system.md)
- [Phase 05 - Audit Trail & Change Tracking](phases/phase-05-audit-trail-change-tracking.md)
- [Phase 06 - Security & Authorization](phases/phase-06-security-authorization.md)
- [Phase 07 - Performance & Scalability](phases/phase-07-performance-scalability.md)
- [Phase 08 - Testing & Documentation](phases/phase-08-testing-documentation.md)
- [Phase 09 - Help Documentation Update](phases/phase-09-help-documentation-update.md)
- [Phase 10 - Beep IDE Object Navigator](https://github.com/) — *2026-06-10, restructured 2026-06-11* (lives in the IDE project at `TheTechIdea.Beep.Desktop.IDE.Extensions/.plans/phase-10-object-navigator/`)

---

## Implementation Audit Snapshot (2026-06-17)

- Phases 01 through 09 are **complete** across the FormsManager partials (28 files), helper managers (34 files), interfaces (8 files), models (52 files), tests, reference docs, and help-site page.
- `Help/formsmanager.html` rewritten on 2026-04-09 to align the public help page with the audited runtime surface.
- The integrated WinForms host seam mirrors trigger metadata and normalized block-level UoW activity through `BeepForms` / `IBeepFormsHost`.
- **Phase 10 (IDE Object Navigator)** lives in the IDE project at `TheTechIdea.Beep.Desktop.IDE.Extensions/` — no new engine code.
- Use [todo-tracker.md](todo-tracker.md) as the operational status source of truth.

---

## Current Gaps — Priority-Ranked Enhancement Queue

### P0 — Correctness / Multi-Form Hardening

| ID | Item | Status | Where |
|----|------|--------|-------|
| G0.1 | Multi-form transactional rollback | FIXED 2026-06 | `FormsManager.FormOperations.cs` |
| G0.2 | `WHEN-CUSTOM-ITEM-EVENT` not a first-class trigger | OPEN | `EventManager.cs`, `TriggerEnums.cs` |
| G0.3 | Master/detail sync on computed keys | FIXED 2026-06 | `RelationshipManager.cs` |
| G0.4 | Sequence collision in distributed scenarios | OPEN | `SequenceProvider.cs` |
| G0.5 | TriggerDependencyManager depth limit (add configurable max depth + cycle timeout) | OPEN | `TriggerDependencyManager.cs` |
| G0.6 | Reflection-based UoW method resolution | FIXED 2026-06 | `BasicDataOps.cs`, `Navigation.cs` |
| G0.7 | Reflection on `Units` (Count, CurrentIndex) | FIXED 2026-06 | `Navigation.cs` |
| G0.8 | `LOVManager` concurrency + perf defects | FIXED 2026-06 | `LOVManager.cs` |
| G0.9 | `TriggerManager` correctness + consolidation | FIXED 2026-06 | `TriggerManager.cs` |
| G0.10 | Multi-form / inter-form correctness | FIXED 2026-06 | `MultiFormNavigation.cs`, `InterFormComm.cs` |
| G0.11 | `ModeTransitions` correctness | FIXED 2026-06 | `ModeTransitions.cs` |
| G0.12 | `ValidationManager` second-pass | FIXED 2026-06 | `ValidationManager.cs` |
| G0.13 | `Master/Detail` second-pass | FIXED 2026-06 | `MasterDetailKeyResolver.cs` |
| G0.14 | `Triggers` second-pass | FIXED 2026-06 | `TriggerManager.cs`, `TriggerDefinition.cs` |

### P1 — CRUD & Data Management Parity Gaps

| ID | Item | Effort | Where |
|----|------|--------|-------|
| G1.1 | **Composite-key master/detail relationships** — `CreateMasterDetailRelation` takes single key; Oracle Forms supports multi-key joins | Small | `FormsManager.Relationships.cs`, `DataBlockRelationship.cs` |
| G1.2 | **`RECORD_GROUP` / `RECORDGROUP_FROM_QUERY` built-ins** — named in-memory record sets for LOVs/combo boxes | Medium | `Helpers/`, `Builtins/IBeepBuiltins.cs` |
| G1.3 | **`LIST_VALUES` built-in** — restricted `SHOW_LOV` that displays current LOV values as flat list | Small | `IBeepBuiltins.cs`, `IBuiltinHost` |
| G1.4 | **`PARAMETER` / `PARAMETER_LIST` built-ins** — named parameters for inter-form communication | Small | `IBeepBuiltins.cs`, `InterFormComm.cs` |
| G1.5 | **`PROGRAM_UNIT` built-in** — calling PL/SQL stored procedures via `IDataSource` | Medium | `IBeepBuiltins.cs`, `IBuiltinHost` |
| G1.6 | **`DBMS_APPLICATION_INFO` built-ins** — `SET_CLIENT_INFO` / `SET_MODULE` / `SET_ACTION` | Small | `IBeepBuiltins.cs`, `IBuiltinHost` |
| G1.7 | **`CLIENT_HOST` / `CLIENT_INFO` built-ins** — client hostname/IP metadata for audit trails | Small | `IBeepBuiltins.cs`, `IBuiltinHost` |

### P2 — Data Management Nice-to-Have

| ID | Item | Effort | Where |
|----|------|--------|-------|
| G2.1 | **Built-in query construction language** — parse `WHERE` clause strings into `List<AppFilter>` | Medium | `QueryBuilderManager.cs` |
| G2.2 | **`EDITOR` / `TEXT_IO` built-ins** — large text editing, text file I/O | Medium | `IBeepBuiltins.cs`, `IBuiltinHost` |
| G2.3 | **`VARR` (variable arrays) / batch operations** — fixed-size value arrays for batch DB operations | Medium | `Helpers/`, `IBeepBuiltins.cs` |
| G2.4 | **`DBMS_PIPE` / `DBMS_ALERT` built-ins** — cross-session messaging for multi-user coordination | Large | `Helpers/`, `InterFormComm.cs` |
| G2.5 | **`SET_APPLICATION_PROPERTY` cursor/data entry mode presets** — specific property keys beyond generic bag | Small | `IBeepBuiltins.cs`, `IBuiltinHost` |

### P3 — IUnitofWork / IDataSource Capability Gaps (Engine Surface)

These are UoW/DataSource features that exist on the interfaces but are NOT yet surfaced by FormsManager:

| ID | Item | Effort | Where |
|----|------|--------|-------|
| G3.1 | **Bookmarks** (`SET_BOOKMARK` / `GO_BOOKMARK`) — named cursor positions | Small | New methods on `FormsManager.Navigation.cs` |
| G3.2 | **Computed Columns** (`RegisterComputed` / `UnregisterComputed`) — derived field values | Small | New methods on `FormsManager` |
| G3.3 | **Freeze / Batch Update** (`Freeze()` / `Unfreeze()` / `BeginBatchUpdate()`) — suppress events for bulk ops | Small | New methods on `FormsManager.DataOperations.cs` |
| G3.4 | **Entity-Level Search** (`FindAsync` / `FindManyAsync` / `CloneItem`) — predicate search within block | Small | New methods on `FormsManager.BasicDataOps.cs` |
| G3.5 | **UoW Change Log** (`GetChangeLog()`) — per-property before/after values (richer than audit) | Small | New method on `FormsManager.DataOperations.cs` |
| G3.6 | UoW event → FormsManager sync (22 events) | FIXED 2026-06 | `EventManager.cs` |
| G3.7 | **UoW Virtual/Lazy Loading surfaced** — align `FormsManager.Performance.cs` paging with UoW `GoToPageAsync` / `EnableVirtualMode` | Medium | `FormsManager.Performance.cs` |
| G3.8 | **Relationship auto-discovery from source metadata** — `GetChildTablesList()` / `GetEntityforeignkeys()` optional auto-discover | Medium | `FormsManager.Relationships.cs` |
| G3.9 | **Entity lifecycle operations** — `CreateEntityAs()` / `RunScript()` for runtime schema management | Large | New methods on `FormsManager` |
| G3.10 | **Source-level aggregate queries** — `GetScalar()` for COUNT/MAX/MIN/SUM that hit source directly | Small | New method on `FormsManager.DataOperations.cs` |
| G3.11 | **Source-level transactions** — `BeginTransaction()` / `EndTransaction()` / `Commit()` for atomic cross-block commits | Medium | New methods on `FormsManager.FormOperations.cs` |

---

## Cross-Cutting Rules: UoW and Primary-Key Handling

- FormsManager orchestrates form behavior, but `IUnitofWork` remains the source of truth for persisted state, dirty tracking, insert/update/delete execution, commit, rollback, and datasource-backed key generation.
- New-record flow: create typed instance from `DataBlockInfo.EntityType` → apply audit/default values → apply primary-key strategy → fire `WHEN-CREATE-RECORD` / pre-insert logic → insert through UoW → refresh database-generated values → synchronize dependents.
- Primary-key strategy order:
	1. If the caller or trigger already supplied a valid key, preserve it.
	2. If the field is datasource-managed identity / auto-increment, leave it unset client-side and refresh after `InsertAsync` / `CommitFormAsync`.
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
├── Core (FormsManager.Core.cs)                    — Block registration, DI, properties, 28 partials
├── Navigation (FormsManager.Navigation.cs)        — Record/block navigation + history
├── FormOperations (.FormOperations.cs)            — Open/Close/Commit/Rollback form
├── EnhancedOperations (.EnhancedOperations.cs)    — Type-safe CRUD with audit defaults
├── ModeTransitions (.ModeTransitions.cs)          — ENTER_QUERY / EXECUTE_QUERY / CRUD transitions
├── DataOperations (.DataOperations.cs)            — Undo/Redo, aggregates, batch, export/import, state snapshots
├── DmlTriggers (.DmlTriggers.cs)                  — ON-INSERT / ON-UPDATE / ON-DELETE wrappers + RAISE_FORM_TRIGGER
├── KeyTriggers (.KeyTriggers.cs)                  — KEY- trigger registration + default keyboard actions
├── TriggerChaining (.TriggerChaining.cs)          — Trigger execution DAG + execution log
├── Validation (.Validation.cs)                    — Field + block validation (event + rule-based)
├── Security (.Security.cs)                        — Block/field security + row filtering + masking
├── Audit (.Audit.cs)                              — Audit trail config + query + CSV/JSON export
├── Alerts (.Alerts.cs)                            — MESSAGE / SHOW_ALERT / BELL equivalents
├── Timers (.Timers.cs)                            — CREATE_TIMER / DELETE_TIMER / WHEN-TIMER-EXPIRED
├── Sequences (.Sequences.cs)                      — Sequences + item defaults + COPY/APPLY_DEFAULT_VALUES
├── InterFormComm (.InterFormComm.cs)              — :GLOBAL.* / parameters / message bus / shared blocks
├── MultiFormNavigation (.MultiFormNavigation.cs)  — CALL_FORM / OPEN_FORM / NEW_FORM / return-to-caller
├── BlockProperties (.BlockProperties.cs)          — SET_BLOCK_PROPERTY / GET_BLOCK_PROPERTY
├── Performance (.Performance.cs)                  — Paging / fetch-ahead / lazy load / cache / statistics
├── DirtyState (.DirtyState.cs)                    — Unsaved-changes detection + handling
├── Relationships (.Relationships.cs)              — Master/detail registration + sync
├── GenericOperations (.GenericOperations.cs)      — Typed block wrappers + ShowLOVAsync
├── FormsSimulation (.FormsSimulation.cs)          — SetFieldValue / GetFieldValue / ExecuteSequence
├── BasicDataOps (.BasicDataOps.cs)                — Insert/Delete/EnterQuery/ExecuteQuery base operations
├── BlockRegistration (.BlockRegistration.cs)      — Register/Unregister/SetupBlock + savepoints
├── Logging (.Logging.cs)                          — Structured logging via ILogger<FormsManager>
├── Helpers (.Helpers.cs)                          — Private cross-helper plumbing
├── Properties (.Properties.cs)                    — 28+ readonly public properties exposing helpers
└── Lifecycle (.Lifecycle.cs)                      — InitializeManager / Dispose

└── 24 Helper Managers (DI-injected, one per concern):
    ├── DirtyStateManager, PerformanceManager, SystemVariablesManager
    ├── ValidationManager, LOVManager, ItemPropertyManager
    ├── TriggerManager, SavepointManager, LockManager
    ├── QueryBuilderManager, BlockErrorLog, MessageQueueManager
    ├── BlockFactory, BlockPropertyManager, PagingManager
    ├── AlertProvider, SequenceProvider, TimerManager
    ├── SecurityManager, AuditManager, CrossBlockValidationManager
    ├── FormRegistry, FormMessageBus, SharedBlockManager
    └── EventManager, FormsSimulationHelper, MasterDetailKeyResolver, ConfigurationManager

└── Models (52 classes — DTOs, enums, EventArgs)
└── Interfaces (8 files — 38 interfaces/types)
└── Configuration (6 DTOs + ConfigurationManager)
└── Builtins (IBeepBuiltins, BeepBuiltinException, BeepFormsHostAdapter)
```

---

## What's Working Well (Production-Ready)

- Core block registration with ConcurrentDictionary
- Master-detail relationship management
- Mode transitions (ENTER_QUERY → EXECUTE_QUERY → CRUD)
- Form operations (Open/Close/Commit/Rollback with trigger events)
- Navigation with validation, unsaved changes checking, nav history
- Dirty state tracking across blocks and relationships
- Trigger system (200+ Oracle Forms trigger types, form/block/item/global scope, sync+async)
- Validation framework (item/record/block/form levels, fluent builder)
- LOV manager (register/load/cache/filter/validate/related-field population)
- Item property manager (SET_ITEM_PROPERTY / GET_ITEM_PROPERTY equivalents)
- Savepoint manager (named snapshots + rollback with monotonic ordering)
- Record locking (auto-lock, 4 modes, per-block)
- Query builder with templates
- Undo/Redo via IUndoable UOW capability
- Batch commit with progress
- Export/Import (JSON, CSV, DataTable)
- Block aggregates (Sum, Average, Count, Groups)
- System variables (:SYSTEM.* emulation — 20+ variables)
- Performance caching and statistics
- Configuration system (form/navigation/performance/validation)
- Multi-form navigation (CALL_FORM/OPEN_FORM/NEW_FORM with call stack)
- Inter-form communication (:GLOBAL.*, message bus, shared blocks)
- Audit trail (field-level change tracking, in-memory/file stores, CSV/JSON export)
- Security (block/field authorization, role-based, field masking)
- Timers (CREATE_TIMER/DELETE_TIMER with WHEN-TIMER-EXPIRED)
- Sequences (in-memory named auto-increment)
- Alerts (MESSAGE/SHOW_ALERT/BELL with configurable buttons)

---

## Post-Closeout Maintenance

### Ongoing documentation maintenance
1. **Planning and documentation alignment must stay synchronized** — the `.plans` files, mapping document, README guidance, migration guidance, and help page should describe the same audited implementation state.

### Residual runtime hardening
2. **Reflection-heavy UoW seams cleaned (FIXED 2026-06)** — 6 reflection sites replaced with direct `IUnitofWork` interface calls.
3. **Programmatic trigger-raise coverage audited** — `RaiseFormTriggerAsync` exists; explicit call-site usage continues to be reviewed.
4. **Remote cache invalidation remains a hardening area** — local cache controls exist, but external datasource change notification depends on surrounding plumbing.
5. **Integrated trigger/UoW UI seams stay proxy-based** — extend `BeepForms` / `IBeepFormsHost` rather than binding `BeepBlock` directly to `FormsManager`.

### Intentionally UI-owned Oracle Forms concepts
6. **Canvases, tab pages, focus rendering, LOV dialog presentation, and message-area rendering stay outside FormsManager** — these belong to the host UI and are not blockers for FormsManager runtime parity.

### Planned P1-P3 Enhancement Queue (Next Engine Iteration)
7. **P1 — CRUD parity:** Composite-key master/detail (G1.1), RECORD_GROUP built-ins (G1.2), LIST_VALUES (G1.3), PARAMETER_LIST (G1.4), PROGRAM_UNIT (G1.5), DBMS_APPLICATION_INFO (G1.6), CLIENT_HOST/CLIENT_INFO (G1.7).
8. **P2 — Nice-to-have:** WHERE clause parser (G2.1), TEXT_IO large-text editing (G2.2), VARR/batch arrays (G2.3), DBMS_PIPE cross-session messaging (G2.4), SET_APPLICATION_PROPERTY presets (G2.5).
9. **P3 — UoW/DataSource surface:** Bookmarks (G3.1), Computed columns (G3.2), Freeze/Batch update (G3.3), Entity search (G3.4), Change log (G3.5), Virtual/Lazy loading alignment (G3.7), Relationship auto-discovery (G3.8), Source-level aggregates (G3.10), Source-level transactions (G3.11). G3.6 is fixed. G3.9 (entity lifecycle) is large effort — defer.

---

## Phase Status Snapshot

| Phase | Current State | Notes |
|-------|---------------|-------|
| 1 — Core Completion & Stabilization | Complete | Typed block registration, CRUD orchestration, validation integration, interface routing present. |
| 2 — Oracle Forms Built-in Emulation | Complete | Block properties, navigation built-ins, alerts, sequences, defaults, timers exist. |
| 3 — Multi-Form & Cross-Form Communication | Complete | Modal/modeless/replace form flows, form registry, globals, message bus, shared blocks. |
| 4 — Advanced Trigger System | Complete | Key triggers, DML triggers, trigger chaining, dependency tracking, trigger-library helpers. |
| 5 — Audit Trail & Change Tracking | Complete | Audit manager, audit stores, field history, export/purge operations. |
| 6 — Security & Authorization | Complete | Security context, block security, field security, masking, violation handling. |
| 7 — Performance & Scalability | Complete | Paging, fetch-ahead, lazy loading, cache controls, performance statistics. G3.7 (UoW virtual mode alignment) remains a P3 gap. |
| 8 — Testing & Documentation | Complete | Test projects, README updates, migration guidance, mapping docs, helper READMEs exist. |
| 9 — Help Documentation Update | Complete | `Help/formsmanager.html` reflects audited runtime surface. |

---

## Oracle Forms Coverage Summary

- Core lifecycle, block properties, navigation, query, CRUD, LOV orchestration, alerts, sequences, timers, multi-form navigation, inter-form messaging, audit, security, paging, savepoints, and locking are implemented in code.
- 200+ Oracle Forms trigger types across 10 categories (FormLifecycle, BlockLifecycle, RecordLifecycle, ItemLifecycle, DataManipulation, Query, Validation, Navigation, KeyAction, Mouse, Timer, ErrorHandling, MasterDetail, Custom).
- 20+ `:SYSTEM.*` variables emulated (CURRENT_BLOCK, CURSOR_RECORD, LAST_QUERY, FORM_STATUS, etc.).
- 30+ `IBeepBuiltins` members exposed (GO_BLOCK, NEXT_RECORD, EXECUTE_QUERY, COMMIT, CREATE_RECORD, SHOW_LOV, MESSAGE/ALERT, OPEN_FORM/CLOSE_FORM/GO_FORM, SET_GLOBAL/GET_GLOBAL, etc.).
- The remaining open work is P1-P3 CRUD parity gaps and targeted hardening, not broad feature invention.
- UI-layer concerns such as canvases, visual focus management, LOV dialog rendering, and message-area rendering remain intentionally outside FormsManager.

---

## Post-Roadmap Follow-up

1. Keep [todo-tracker.md](todo-tracker.md), [ORACLE-FORMS-MAPPING.md](ORACLE-FORMS-MAPPING.md), [gaps.md](../gaps.md), [enhancements.md](../enhancements.md), and this enhancement plan aligned after any runtime changes.
2. Continue hardening external cache invalidation integration where needed.
3. Archive or rewrite stale historical planning notes outside `.plans` so they no longer read as active roadmap items.
4. P1-P3 gaps (G0.2, G0.4, G0.5, G1.1-G1.7, G2.1-G2.5, G3.1-G3.11) form the next engine iteration queue — prioritize P1 first (smallest effort, highest Oracle Forms parity payoff).
