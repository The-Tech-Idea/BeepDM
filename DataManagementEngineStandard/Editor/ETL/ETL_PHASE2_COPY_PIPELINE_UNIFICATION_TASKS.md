# ETL Phase 2 Implementation Tasks

This file breaks down **Phase 2** from `ETL_INTEGRATION_ENHANCEMENT_PLAN.md` into concrete implementation tasks.

## Phase 2 Goal
- Unify ETL copy execution through one efficient engine.
- Reduce duplicate row-copy logic in `ETLEditor`.
- Preserve ETL API compatibility and behavior defaults.

## Files In Scope
- `DataManagementEngineStandard/Editor/ETL/ETLEditor.cs`
- `DataManagementEngineStandard/Editor/ETL/ETLDataCopier.cs`
- `DataManagementEngineStandard/Editor/ETL/ETLEntityCopyHelper.cs`
- `DataManagementEngineStandard/Editor/ETL/ETLScriptBuilder.cs` (optional cleanup alignment)
- `DataManagementEngineStandard/Editor/Mapping/Helpers/MappingDefaultsHelper.cs` (consume only)

---

## Workstream A - Define Unified Copy Runtime Contract

### A1. Introduce internal copy options model
- [ ] Add internal `EtlCopyExecutionOptions` model (or equivalent) with:
  - [ ] `BatchSize`
  - [ ] `EnableParallel`
  - [ ] `MaxRetries`
  - [ ] `UseEntityStructure`
  - [ ] `UseMapping`
  - [ ] `CustomTransformation`
- [ ] Add safe defaults matching current ETL behavior.

### A2. Add ETLEditor -> ETLDataCopier adapter method
- [ ] In `ETLEditor.cs`, add private adapter:
  - [ ] `ExecuteCopyStepAsync(ETLScriptDet step, IDataSource srcDs, IDataSource destDs, IProgress<PassedArgs> progress, CancellationToken token, EtlCopyExecutionOptions options)`
- [ ] Ensure adapter maps ETL script fields to `CopyEntityDataAsync(...)` arguments.
- [ ] Keep existing logging/tracking calls around adapter invocation.

---

## Workstream B - Route CopyData Path Through ETLDataCopier

### B1. Replace direct row-loop path
- [ ] In `RunCreateScript(...)`, for `DDLScriptType.CopyData`, replace call path to `RunCopyEntityScript(...)` with `ExecuteCopyStepAsync(...)`.
- [ ] Keep old path behind temporary internal fallback switch for rollout safety.

### B2. Update RunImportScript copy execution
- [ ] In `RunImportScript(...)`, route copy execution through same adapter.
- [ ] Ensure mapping detail `sc.Mapping` is passed through.
- [ ] Ensure existing import defaults path remains applied.

### B3. Harmonize direct copy public methods
- [ ] Update `CopyEntityData(...)` and related wrappers to call unified adapter or `ETLDataCopier`.
- [ ] Preserve public signatures and existing return types.

---

## Workstream C - Normalize Transformation/Defaults Pipeline

### C1. Single transform order
- [ ] Enforce transform sequence in one place:
  1. map record (if mapping exists),
  2. apply defaults (`MappingDefaultsHelper`),
  3. apply custom transform delegate (optional),
  4. write to destination.

### C2. Remove duplicated transform logic
- [ ] Deprecate/reduce duplicated transformation code in `RunCopyEntityScript(...)`.
- [ ] Keep relation/parent integrity checks in one canonical place where required.

### C3. Data shape normalization
- [ ] Standardize source result normalization (`DataTable`, `IEnumerable<object>`, binding-list variants).
- [ ] Remove fragile type-name string checks where practical.

---

## Workstream D - Constraints, Transactions, and Error Semantics

### D1. FK handling consistency
- [ ] Ensure FK disable/enable wraps full batch copy unit consistently.
- [ ] Ensure correct entity structure is used for re-enable call.

### D2. Error propagation consistency
- [ ] Convert copier exceptions/failures into ETL `IErrorsInfo` consistently.
- [ ] Ensure `StopErrorCount` behavior remains enforced.
- [ ] Ensure per-step `Tracking` and `LoadDataLogs` are still populated.

### D3. Optional transaction wrapping
- [ ] Add optional transaction wrapper around per-entity batch copy where datasource supports transactions.
- [ ] Rollback on hard failure when enabled.

---

## Workstream E - Progress and Telemetry Alignment

### E1. Progress normalization
- [ ] Align `CurrentScriptRecord` increments with actual inserted records.
- [ ] Keep script-level vs record-level progress semantics distinct.
- [ ] Ensure progress messages include source/destination entity names.

### E2. Run summary
- [ ] Add per-entity summary at end of each copy step:
  - [ ] records read
  - [ ] records inserted
  - [ ] retries attempted
  - [ ] failures

---

## Workstream F - Cleanup and Compatibility

### F1. Legacy method strategy
- [ ] Mark `RunCopyEntityScript(...)` as internal legacy path.
- [ ] Keep for compatibility during transition, with TODO removal marker for later phase.

### F2. ETLEntityCopyHelper position
- [ ] Decide role:
  - [ ] keep as adapter helper around unified pipeline, or
  - [ ] deprecate and redirect to `ETLDataCopier`.
- [ ] Document decision in code comments.

### F3. Builder integration
- [ ] Optionally align `ETLScriptBuilder` usage from `ETLEditor` script creation paths to reduce script-construction duplication.

---

## Workstream G - Regression and Performance Tests

### G1. Functional regression
- [ ] copy-only ETL script copies expected record counts.
- [ ] create+copy script still honors creation and then data load.
- [ ] import script with mapping still produces mapped records.
- [ ] cancellation interrupts large copy safely.

### G2. Reliability tests
- [ ] retry behavior verified for transient insert failures.
- [ ] `StopErrorCount` halts run after threshold.
- [ ] FK constraints restored on success/failure.

### G3. Throughput baseline
- [ ] benchmark old vs unified copy path on representative dataset.
- [ ] validate no severe regression in low-volume paths.

---

## Suggested Implementation Order
1. A1 -> A2 (runtime contract and adapter)
2. B1 -> B2 -> B3 (route all copy entrypoints)
3. C1 -> C2 -> C3 (transform normalization)
4. D1 -> D2 -> D3 (constraints/errors/transactions)
5. E1 -> E2 (progress/summary)
6. F1 -> F2 -> F3 (cleanup)
7. G1 -> G2 -> G3 (tests and benchmark)

---

## Definition of Done (Phase 2)
- [ ] `CopyData` execution in ETL routes through a unified pipeline.
- [ ] Transformation/defaults/mapping sequence is centralized.
- [ ] Public ETL APIs remain backward compatible.
- [ ] Error tracking and progress reporting remain accurate.
- [ ] Regression + reliability tests pass for key copy scenarios.
