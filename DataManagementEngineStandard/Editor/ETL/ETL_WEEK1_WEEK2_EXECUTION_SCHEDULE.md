# ETL Week 1-2 Execution Schedule

This schedule maps daily execution to:
- `ETL_PHASE1_IMPLEMENTATION_TASKS.md`
- `ETL_PHASE2_COPY_PIPELINE_UNIFICATION_TASKS.md`

Use it as a delivery playbook for kickoff implementation.

## Assumptions
- Workdays: Monday-Friday
- Team can execute one main implementation stream per day plus test/cleanup
- No API-breaking changes during Week 1-2

---

## Week 1 - Phase 1 (Integration Baseline and Preflight)

### Day 1 - Preflight Scaffolding
**Target:** Workstream A (A1, A2 partial)
- [ ] Add `PreflightCreateScriptAsync(...)` in `ETLEditor`.
- [ ] Add `PreflightImportScriptAsync(...)` in `ETLEditor`.
- [ ] Wire both methods at top of `RunCreateScript(...)` and `RunImportScript(...)` with no-op pass behavior initially.
- [ ] Add initial log category entries for preflight.

**Deliverable**
- ETL run entrypoints invoke preflight stubs successfully.

### Day 2 - Validation Gates
**Target:** Workstream B
- [ ] Implement mapping validation gate (`ValidateEntityMapping`, `ValidateMappedEntity`) in import preflight.
- [ ] Implement entity consistency gate (`ValidateEntityConsistency`) for copy script details.
- [ ] Add null/missing script shape guards.
- [ ] Return early on preflight failure with clear error messages.

**Deliverable**
- Failed preflight blocks ETL execution deterministically.

### Day 3 - Importing Preflight Hook
**Target:** Workstream C + D partial
- [ ] Add optional bridge call to `RunMigrationPreflightAsync(...)`.
- [ ] Add rollout feature switch for Importing preflight bridge.
- [ ] Add staged preflight result mapping into ETL logs and progress messages.

**Deliverable**
- Preflight bridge integrated and controllable by internal flag.

### Day 4 - Diagnostics and Error Normalization
**Target:** Workstream D
- [ ] Add structured preflight outcome model (pass/fail/stage/entity/message).
- [ ] Normalize `DMEEditor.ErrorObject` output for preflight failures.
- [ ] Add summary preflight line to `LoadDataLogs`.
- [ ] Improve error message context (source/destination/entity).

**Deliverable**
- Preflight diagnostics are actionable and consistent.

### Day 5 - Phase 1 Tests and Stabilization
**Target:** Workstream E
- [ ] Add tests for create-script preflight success/failure.
- [ ] Add tests for import preflight success/failure.
- [ ] Add compatibility tests for unchanged ETL caller signatures.
- [ ] Fix regressions and finalize Phase 1 checklist.

**Deliverable**
- Phase 1 marked complete in tracker with passing tests.

---

## Week 2 - Phase 2 (Copy Pipeline Unification)

### Day 6 - Unified Copy Contract
**Target:** Workstream A
- [ ] Add `EtlCopyExecutionOptions` (internal model).
- [ ] Add `ExecuteCopyStepAsync(...)` adapter in `ETLEditor`.
- [ ] Map script detail fields to `ETLDataCopier.CopyEntityDataAsync(...)`.

**Deliverable**
- Adapter contract exists and compiles.

### Day 7 - Route CopyData Execution
**Target:** Workstream B
- [ ] Route `DDLScriptType.CopyData` path in `RunCreateScript(...)` through adapter.
- [ ] Route `RunImportScript(...)` data copy path through adapter.
- [ ] Add internal fallback switch to legacy `RunCopyEntityScript(...)`.

**Deliverable**
- Primary copy path executes through unified adapter.

### Day 8 - Transform/Defaults Normalization
**Target:** Workstream C
- [ ] Enforce single transform sequence: map -> defaults -> custom transform -> write.
- [ ] Remove duplicated transformation logic from legacy path where safe.
- [ ] Normalize source data shape conversion paths.

**Deliverable**
- One canonical transformation pipeline active.

### Day 9 - Constraints, Progress, and Error Semantics
**Target:** Workstream D + E
- [ ] Align FK disable/enable scope around batch/entity boundaries.
- [ ] Normalize error propagation and step tracking for unified path.
- [ ] Align progress counters and per-entity summary emission.
- [ ] Add optional transaction wrapper where provider supports it.

**Deliverable**
- Unified copy path has stable tracking, progress, and constraint behavior.

### Day 10 - Phase 2 Tests and Benchmark Check
**Target:** Workstream F + G
- [ ] Decide `ETLEntityCopyHelper` transition role (adapter/deprecate).
- [ ] Add regression tests for copy-only/create+copy/import mapping paths.
- [ ] Add reliability tests for retry/cancel/stop-threshold behavior.
- [ ] Capture basic throughput baseline vs old path.

**Deliverable**
- Phase 2 marked complete with validated behavior and initial performance evidence.

---

## End-of-Week Exit Criteria

### End of Week 1
- [ ] All Phase 1 tasks complete.
- [ ] Preflight fully active with feature-toggle for import bridge.
- [ ] No ETL signature-breaking changes.

### End of Week 2
- [ ] All Phase 2 tasks complete.
- [ ] Unified copy path active with fallback switch available.
- [ ] Core copy regression tests pass.

---

## Daily Standup Template

Use this format each day:

- Yesterday:
  - [ ] completed items
- Today:
  - [ ] planned items
- Risks:
  - [ ] blockers
- Decision needed:
  - [ ] architecture or policy decision

---

## Tracker Update Instructions
- Update `ETL_IMPLEMENTATION_MASTER_TRACKER.md` at end of each day:
  - phase status,
  - blocker log,
  - decisions log,
  - weekly snapshot checkboxes.
