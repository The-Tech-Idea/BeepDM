# Phase 08 — Testing & Documentation

**Status:** Complete (`20 / 20` in [todo-tracker.md](../todo-tracker.md))  
**Priority:** High  
**Depends on:** Runs in parallel, but should validate Phases 01 through 07 explicitly

---

## Objective

Turn the implemented FormsManager surface into something safe to maintain by adding focused unit tests, integration tests, and developer-facing documentation.

## Primary Implementation Seams

- Current completed unit suites: `FormsManager.Core.Tests`, `FormsManager.Navigation.Tests`, `FormsManager.FormOperations.Tests`, `FormsManager.ModeTransitions.Tests`, `TriggerManager.Tests`, `ValidationManager.Tests`, `LOVManager.Tests`, `SavepointManager.Tests`, `LockManager.Tests`
- Current completed integration slices: master-detail cascade (`CurrentChanged` → relationship filters → detail `Get(filters)`), full form lifecycle (`OpenFormAsync` → query transition → edit/dirty commit → lock cleanup → `CloseFormAsync`), multi-block validation commit blocking (`CommitFormAsync` short-circuits before dirty-state save when a cross-block rule fails), concurrent block-local navigation, concrete-datasource LOV loading/caching/field population, and JSON/CSV export-import round-trips
- Test projects for FormsManager core, navigation, form operations, mode transitions, triggers, validation, LOV, savepoints, and locking
- Integration tests for master/detail, full form lifecycle, multi-block validation, concurrent operations, and export/import
- README / migration / Oracle mapping / helper documentation

## UoW and Primary-Key Test Requirements

- Unit-test typed block registration and `CreateNewRecord` against explicit keys, sequence-backed keys, identity keys, GUID defaults, and composite keys.
- Verify `InsertRecordEnhancedAsync` + `CommitFormAsync` preserve PK correctness and UoW dirty-state transitions.
- Test identity-generated parent inserts followed by detail synchronization.
- Test sequence-reserved parent keys for pre-commit detail creation.
- Test rollback after key reservation so sequence/temporary-key behavior is documented and deterministic.
- Cover audit, security, and cache behavior for records whose PK appears only after insert.

## Documentation Deliverables

- Root `README.md` now documents the current FormsManager surface, ownership rules, quick-start setup, built-in operations, and current test coverage.
- `MIGRATION-GUIDE.md` now documents how older `UnitofWorksManager`-style consumers should move to typed block registration, FormsManager-owned master/detail orchestration, and `ShowLOVAsync`-driven LOV application.
- `ORACLE-FORMS-MAPPING.md` now maps Oracle Forms runtime concepts to FormsManager APIs, with explicit notes for key generation and UoW ownership.
- `Helpers/README.md` now documents the caller-facing behaviors of LOV, paging, audit, security, trigger, and state helpers.
- XML documentation coverage on the public Forms API is complete; the latest build no longer reports Forms-local `CS1591`, `CS1574`, or `CS1587` warnings.

## Done / Verify Checklist

- Core, navigation, validation, form operations, triggers, LOV, savepoints, and locking all have targeted tests.
- Navigation coverage verifies back/forward history, direct record navigation, and `ValidateBeforeNavigation` blocking.
- Form-operations coverage verifies open/close lifecycle, unsaved-change blocking, and the no-dirty commit short-circuit.
- Mode-transition coverage verifies CRUD→Query entry, Query→CRUD execution, unsaved-change blocking, and new-record CRUD entry.
- Trigger coverage verifies async registration/fire, priority ordering, stop-vs-continue failure chains, and suspend/resume execution.
- LOV coverage verifies registration, datasource-backed loads, cache reuse, server/client-side filtering, validation failures, and related-field value extraction.
- Savepoint coverage verifies metadata capture, generated naming, rollback pruning, and targeted vs block-wide release behavior.
- Lock coverage verifies mode configuration, current-record locking, auto-lock behavior, and unlock cleanup.
- Master/detail integration coverage verifies that a master current-record change builds relationship filters and reloads the detail block through `Get(filters)`.
- Lifecycle integration coverage verifies open/query/edit/commit/close coordination with dirty-state persistence and commit-time lock cleanup.
- Cross-block validation integration coverage verifies `CommitFormAsync` fails before dirty-state persistence when a registered rule returns an error.
- Concurrent-operations integration coverage verifies overlapping navigation calls on different blocks complete without corrupting per-block current-record state.
- LOV integration coverage verifies `ShowLOVAsync` loads from a concrete datasource, reuses cache, and populates the bound record from a selected LOV row.
- Export/import integration coverage verifies FormsManager forwards JSON and CSV round-trips through `IExportable` and `IImportable` block capabilities.
- Integration coverage exercises at least one full create → validate → commit → rollback lifecycle.
- PK/sequence/identity behavior is tested end to end with UoW-backed persistence.
- Documentation explains both the happy path and the constraints.

## Maintenance Notes

- Any future change to create/insert/commit or master/detail key logic should add or update tests here before code is considered complete.
- Tests added in this phase exposed an uninitialized `CrossBlockValidationManager`; constructor wiring now initializes it before any commit or cross-block validation path runs.
- The integration test project needed small compatibility fixes (`IDataSource` import and `EntityField.FieldName`) before new FormsManager lifecycle coverage could compile again.
- The added master/detail integration test validates the event-driven sync path wired during block registration, not only the direct `SynchronizeDetailBlocksAsync` helper call.
- The added cross-block validation integration test confirms `CommitFormAsync` stops before `SaveDirtyBlocksAsync` when `CrossBlockValidationManager.Validate()` returns an error.
- The concrete LOV integration test exposed a gap in `ShowLOVAsync`: the selected LOV return value was not being written back to the bound field because `__RETURN_VALUE__` was treated like a literal field name. The implementation now maps that sentinel back to the requested field name before applying related values.