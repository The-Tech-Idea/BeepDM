# ETL Week 3-4 Execution Schedule

This schedule maps daily execution to:
- `ETL_PHASE3_IMPORTING_INTEGRATION_TASKS.md`
- `ETL_PHASE4_MIGRATION_ALIGNMENT_TASKS.md`
- `ETL_PHASE5_SCRIPT_SYSTEM_CONSOLIDATION_TASKS.md`
- `ETL_PHASE6_TELEMETRY_RUNTIME_CONTROL_TASKS.md`

Use this after completing the Week 1-2 plan.

## Assumptions
- Week 1-2 (Phases 1-2) are complete.
- Feature toggles remain available for safe fallback.
- Team executes one primary stream per day with test/cleanup at day end.

---

## Week 3 - Phases 3 and 4 (Importing + Migration Alignment)

### Day 11 - ETL to Importing Adapter Foundation
**Target:** Phase 3 Workstream A
- [ ] Add ETL adapter model to build `DataImportConfiguration` from ETL script/detail.
- [ ] Implement `BuildImportConfigurationFromEtlScript(...)`.
- [ ] Add adapter output validation and failure messaging.

**Deliverable**
- ETL can generate valid Importing configuration from script detail.

### Day 12 - Route RunImportScript Through Importing
**Target:** Phase 3 Workstream B
- [ ] Integrate `IDataImportManager` resolution in ETL.
- [ ] Route `RunImportScript(...)` via `RunImportAsync(...)`.
- [ ] Keep legacy fallback path switch.

**Deliverable**
- Import scripts execute through Importing path with fallback available.

### Day 13 - Mapping/Policy Bridge Stabilization
**Target:** Phase 3 Workstreams C and D
- [ ] Enforce mapping validation before Importing call.
- [ ] Bridge ETL controls (`StopErrorCount`, cancellation, progress) into Importing config.
- [ ] Add policy defaults for batch/retry with safe bounds.

**Deliverable**
- Importing bridge honors ETL control semantics.

### Day 14 - MigrationManager Create-Path Integration
**Target:** Phase 4 Workstreams A and B
- [ ] Add migration-aware schema helper wrappers in ETL.
- [ ] Route `CreateEntity` script steps through `MigrationManager.EnsureEntity(...)`.
- [ ] Keep direct create fallback for unsupported/unavailable contexts.

**Deliverable**
- ETL create path is migration-aware with safe fallback.

### Day 15 - Phase 3-4 Testing and Fixes
**Target:** Phase 3 Workstream G + Phase 4 Workstream G (partial)
- [ ] Run integration tests for ETL+Importing mapping import flow.
- [ ] Run regression tests for migration-aware create/update behavior.
- [ ] Fix compatibility and telemetry regressions.

**Deliverable**
- Phase 3 complete, Phase 4 core path stable.

---

## Week 4 - Phases 5 and 6 (Script Consolidation + Telemetry Controls)

### Day 16 - Canonical Script Persistence Model
**Target:** Phase 5 Workstream A
- [ ] Finalize canonical script storage layout and script identity strategy.
- [ ] Add/confirm script format version metadata.
- [ ] Document compatibility strategy for legacy scripts.

**Deliverable**
- Canonical script model approved and implemented.

### Day 17 - Save/Load Path Consolidation
**Target:** Phase 5 Workstream B
- [ ] Route ETLEditor `SaveETL`/`LoadETL` through `ETLScriptManager`.
- [ ] Remove duplicated file path logic in ETLEditor where superseded.
- [ ] Enforce validation on save/load operations.

**Deliverable**
- One save/load path active for ETL scripts.

### Day 18 - Legacy Script Migration and Execution Alignment
**Target:** Phase 5 Workstreams C and D
- [ ] Implement legacy script detection and conversion.
- [ ] Add migration backup behavior.
- [ ] Align execution semantics across ETLEditor and script manager paths.

**Deliverable**
- Legacy scripts load/migrate/run under unified model.

### Day 19 - Telemetry and Runtime Control Normalization
**Target:** Phase 6 Workstreams A-D
- [ ] Normalize progress counters and event payload semantics.
- [ ] Add run correlation id + summary metrics.
- [ ] Standardize stop/cancel behavior and logs.
- [ ] Ensure tracking (`LoadDataLogs`, script tracking) is complete and non-duplicative.

**Deliverable**
- ETL run telemetry is coherent and operationally useful.

### Day 20 - Phase 5-6 Testing and Hardening
**Target:** Phase 5 Workstream G + Phase 6 Workstream G
- [ ] Run save/load/migration compatibility tests.
- [ ] Run observability tests (progress ordering, summary integrity, tracking completeness).
- [ ] Address defects and finalize docs updates in phase files.

**Deliverable**
- Phases 5 and 6 complete and test-validated.

---

## End-of-Week Exit Criteria

### End of Week 3
- [ ] Phase 3 marked complete.
- [ ] Phase 4 core schema integration path complete (remaining deltas tracked).
- [ ] ETL import path stable with fallback switch retained.

### End of Week 4
- [ ] Phase 5 marked complete.
- [ ] Phase 6 marked complete.
- [ ] Script model unified and telemetry standardized.

---

## Daily Standup Template

- Yesterday:
  - [ ] completed tasks
- Today:
  - [ ] planned tasks
- Risk:
  - [ ] blockers and impact
- Decision needed:
  - [ ] architecture/policy calls

---

## Tracker Update Instructions
- Update `ETL_IMPLEMENTATION_MASTER_TRACKER.md` at the end of each day:
  - phase statuses,
  - blockers,
  - decisions,
  - weekly checkboxes.
- Keep toggles/fallback notes current in tracker notes column.
