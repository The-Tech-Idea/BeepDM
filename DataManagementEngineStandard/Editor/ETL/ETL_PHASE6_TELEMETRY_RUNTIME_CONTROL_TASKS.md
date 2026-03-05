# ETL Phase 6 Implementation Tasks

This file breaks down **Phase 6** from `ETL_INTEGRATION_ENHANCEMENT_PLAN.md` into concrete implementation tasks.

## Phase 6 Goal
- Standardize ETL telemetry, progress, and runtime control behavior.
- Make run diagnostics consistent across create/copy/import paths.
- Improve operational visibility without changing ETL public method signatures.

## Files In Scope
- `DataManagementEngineStandard/Editor/ETL/ETLEditor.cs`
- `DataManagementEngineStandard/Editor/ETL/ETLScriptManager.cs`
- `DataManagementEngineStandard/Editor/ETL/ETLDataCopier.cs`
- `DataManagementModelsStandard/Editor/ETLScriptDet.cs`
- `DataManagementModelsStandard/Editor/ETLScriptHDR.cs`
- `DataManagementModelsStandard/Workflow/LoadDataLogResult.cs`

---

## Workstream A - Progress Model Normalization

### A1. Define progress semantics
- [ ] Define clear meanings for:
  - [ ] `ScriptCount` (total script steps vs total records)
  - [ ] `CurrentScriptRecord` (current step index vs inserted record index)
- [ ] Document semantics in ETL code comments.

### A2. Harmonize progress event payloads
- [ ] Standardize `PassedArgs` fields used for ETL progress:
  - [ ] `EventType`
  - [ ] `Messege`
  - [ ] `ParameterInt1`
  - [ ] `ParameterInt2`
- [ ] Ensure all ETL paths emit consistent values and sequence.

### A3. Distinguish step-level vs record-level progress
- [ ] Emit step lifecycle events (`start`, `complete`, `failed`) at script-detail level.
- [ ] Emit record/batch counters separately for copy/import operations.

---

## Workstream B - Structured ETL Run Telemetry

### B1. Introduce run correlation id
- [ ] Generate one correlation id per ETL run.
- [ ] Include it in all ETL log lines and tracking items during that run.

### B2. Add run summary object
- [ ] Define internal run summary model with:
  - [ ] start/end time
  - [ ] elapsed duration
  - [ ] steps total/succeeded/failed
  - [ ] records processed/succeeded/failed
  - [ ] retry attempts
  - [ ] cancellation status
- [ ] Emit summary at run end.

### B3. Persist summary to ETL logs
- [ ] Add summary entry to `LoadDataLogs`.
- [ ] Optionally attach summary to `ETLScriptHDR` metadata where appropriate.

---

## Workstream C - Error and Tracking Consistency

### C1. Unify error propagation
- [ ] Ensure all ETL execution paths set `DMEEditor.ErrorObject` consistently.
- [ ] Ensure exceptions become structured failures with stage/entity context.

### C2. Tracking completeness
- [ ] Ensure each failed step appends `SyncErrorsandTracking` with:
  - [ ] step id
  - [ ] entity name
  - [ ] datasource names
  - [ ] record index when available
  - [ ] error message

### C3. Deduplicate noisy logs
- [ ] Remove duplicate "start/finish" logs for same event.
- [ ] Keep one authoritative message for each significant transition.

---

## Workstream D - Runtime Control Guardrails

### D1. StopErrorCount enforcement
- [ ] Ensure `StopErrorCount` is checked uniformly in all loops.
- [ ] Ensure halt condition emits explicit stop event and summary.

### D2. Cancellation behavior standardization
- [ ] Ensure cancellation token checks exist in:
  - [ ] preflight
  - [ ] script-step iteration
  - [ ] record/batch loops
- [ ] Emit terminal cancellation event with final counters.

### D3. Timeout/long-run diagnostics
- [ ] Add periodic heartbeat progress message for long-running operations.
- [ ] Include current entity, step, and counters in heartbeat.

---

## Workstream E - Logging Taxonomy and Message Quality

### E1. Standard log categories
- [ ] Use consistent categories, for example:
  - [ ] `ETL.Run`
  - [ ] `ETL.Preflight`
  - [ ] `ETL.Schema`
  - [ ] `ETL.Copy`
  - [ ] `ETL.ImportBridge`
  - [ ] `ETL.Summary`

### E2. Message format policy
- [ ] Ensure each log message includes:
  - [ ] run correlation id
  - [ ] datasource/entity context when relevant
  - [ ] stage and severity

### E3. Failure detail quality
- [ ] Include actionable context in failures:
  - [ ] operation attempted
  - [ ] target entity
  - [ ] source/destination datasource
  - [ ] recommendation or next check where possible

---

## Workstream F - Interop with Importing Telemetry

### F1. Status mapping alignment
- [ ] Map Importing status events into ETL progress model consistently.
- [ ] Ensure ETL run summary can include Importing stats (if route used).

### F2. Correlated cross-module traces
- [ ] Share ETL run correlation id with Importing calls.
- [ ] Reflect Importing run id/reference in ETL summary and logs.

---

## Workstream G - Regression and Observability Tests

### G1. Progress contract tests
- [ ] Verify event ordering for successful run.
- [ ] Verify event ordering for failure and stop-threshold paths.
- [ ] Verify event ordering for cancellation paths.

### G2. Log quality tests
- [ ] Validate required context fields exist in emitted logs.
- [ ] Validate run summary contains expected counters and duration.

### G3. Tracking integrity tests
- [ ] Ensure failures always populate `Tracking` and `LoadDataLogs`.
- [ ] Ensure summary totals match tracked details.

---

## Suggested Implementation Order
1. A1 -> A2 -> A3 (progress semantics)
2. B1 -> B2 -> B3 (run telemetry)
3. C1 -> C2 -> C3 (error/tracking consistency)
4. D1 -> D2 -> D3 (runtime controls)
5. E1 -> E2 -> E3 (logging taxonomy)
6. F1 -> F2 (Importing interop)
7. G1 -> G2 -> G3 (tests)

---

## Definition of Done (Phase 6)
- [ ] ETL progress semantics are explicit and consistent.
- [ ] Every ETL run emits a correlated summary with reliable counters.
- [ ] Stop and cancellation behavior is uniform across all ETL paths.
- [ ] Logs and tracking are actionable and non-duplicative.
- [ ] Observability regression tests pass.
