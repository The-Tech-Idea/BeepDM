# ETL Phase 3 Implementation Tasks

This file breaks down **Phase 3** from `ETL_INTEGRATION_ENHANCEMENT_PLAN.md` into concrete integration tasks.

## Phase 3 Goal
- Integrate ETL import workloads with Importing orchestration.
- Reuse Importing capabilities (batching, retry, lifecycle, status) instead of duplicating them in ETL.
- Keep ETL public API stable.

## Files In Scope
- ETL:
  - `DataManagementEngineStandard/Editor/ETL/ETLEditor.cs`
  - `DataManagementEngineStandard/Editor/ETL/ETLValidator.cs`
- Importing:
  - `DataManagementEngineStandard/Editor/Importing/DataImportManager.cs`
  - `DataManagementEngineStandard/Editor/Importing/DataImportManager.Core.cs`
  - `DataManagementEngineStandard/Editor/Importing/DataImportManager.Migration.cs`
  - `DataManagementEngineStandard/Editor/Importing/Interfaces/IDataImportInterfaces.cs`
- Mapping:
  - `DataManagementEngineStandard/Editor/Mapping/MappingManager.cs`

---

## Workstream A - ETL to Importing Adapter Layer

### A1. Add ETL adapter model for Importing config
- [ ] Create internal adapter model in ETL (or private mapping method) to translate:
  - ETL script detail (`ETLScriptDet`)
  - ETL script header (`ETLScriptHDR`)
  - selected mapping (`EntityDataMap_DTL`)
  into `DataImportConfiguration`.

### A2. Implement adapter method
- [ ] Add private method in `ETLEditor.cs`:
  - [ ] `BuildImportConfigurationFromEtlScript(ETLScriptHDR script, ETLScriptDet detail, IProgress<PassedArgs> progress, CancellationToken token)`
- [ ] Populate:
  - [ ] source/destination datasource names
  - [ ] source/destination entity names
  - [ ] mapping definition
  - [ ] batch strategy (default and overrides)
  - [ ] retry policy defaults
  - [ ] optional filters and preflight flags

### A3. Validate adapter output
- [ ] Add guard checks for null/missing fields in generated `DataImportConfiguration`.
- [ ] Fail fast with clear ETL error when adapter cannot build valid config.

---

## Workstream B - Route RunImportScript Through DataImportManager

### B1. Integrate `IDataImportManager`
- [ ] Resolve/import `IDataImportManager` from `IDMEEditor` context.
- [ ] Add fallback behavior if manager is unavailable.

### B2. Replace internal import-copy loop
- [ ] In `RunImportScript(...)`:
  - [ ] build `DataImportConfiguration` from ETL script.
  - [ ] invoke `RunImportAsync(...)`.
  - [ ] map returned status/errors back to ETL `IErrorsInfo`.
- [ ] Preserve existing method signature and caller contract.

### B3. Keep compatibility fallback path
- [ ] Keep current `RunCopyEntityScript(...)` path behind internal switch until stabilization.
- [ ] Add clear log indicating whether Importing path or legacy path was used.

---

## Workstream C - Mapping and Validation Alignment

### C1. Mapping validation consistency
- [ ] Before calling Importing manager, enforce:
  - [ ] `ETLValidator.ValidateEntityMapping(...)`
  - [ ] `ETLValidator.ValidateMappedEntity(...)`
- [ ] If map absent and policy allows:
  - [ ] auto-bootstrap map with `MappingManager` convention methods.

### C2. Mapping execution consistency
- [ ] Ensure field mapping and defaults are applied once (avoid double mapping).
- [ ] Ensure ETL and Importing use same mapped field semantics for selected entity.

---

## Workstream D - Runtime Controls and Policy Bridge

### D1. Bridge ETL controls into Importing config
- [ ] Map ETL controls to Importing:
  - [ ] error threshold (`StopErrorCount`) -> import fail policy
  - [ ] cancellation token
  - [ ] progress callback

### D2. Batch/retry policy standardization
- [ ] Define default batch size and retry values for ETL-import bridge.
- [ ] Allow ETL script/detail overrides where available.
- [ ] Add safe max bounds to prevent extreme settings.

### D3. Preflight and migration hook alignment
- [ ] Ensure Importing preflight (`RunMigrationPreflightAsync`) is called consistently via ETL path.
- [ ] Respect preflight failures as blocking unless explicitly configured otherwise.

---

## Workstream E - Telemetry and Error Harmonization

### E1. Unified status mapping
- [ ] Translate Importing result/status model into:
  - [ ] `DMEEditor.ErrorObject`
  - [ ] ETL `LoadDataLogs`
  - [ ] `ETLScriptDet.Tracking`

### E2. Correlation and run identifiers
- [ ] Add run-correlation id in ETL logs for each Importing run.
- [ ] Include source/destination entity and datasource in summary logs.

### E3. Summary lines
- [ ] Emit ETL summary line that includes:
  - [ ] records processed
  - [ ] records imported
  - [ ] failed records
  - [ ] retry attempts
  - [ ] total elapsed time

---

## Workstream F - Optional Advanced Importing Features

### F1. Feature toggles for progressive adoption
- [ ] Add opt-in toggles for ETL to use Importing advanced capabilities:
  - [ ] data quality rules
  - [ ] error store
  - [ ] run history store
  - [ ] watermark/sync draft features
  - [ ] replay failed records flow

### F2. Default-safe behavior
- [ ] Keep advanced features disabled by default until validated.
- [ ] Ensure core import path works without optional stores.

---

## Workstream G - Regression and Integration Tests

### G1. Functional integration tests
- [ ] ETL `RunImportScript(...)` executes via Importing manager path.
- [ ] Mapping import result matches legacy expected output.
- [ ] Preflight failure blocks import execution.

### G2. Reliability tests
- [ ] cancellation halts import cleanly and logs terminal state.
- [ ] retry policy works for transient insert failures.
- [ ] fallback to legacy path works when Importing manager unavailable.

### G3. Compatibility tests
- [ ] Existing ETL callers run unchanged.
- [ ] Existing ETL mapping scripts remain loadable and executable.

---

## Suggested Implementation Order
1. A1 -> A2 -> A3 (adapter foundation)
2. B1 -> B2 -> B3 (execution routing + fallback)
3. C1 -> C2 (mapping/validation alignment)
4. D1 -> D2 -> D3 (policy bridge)
5. E1 -> E2 -> E3 (telemetry/error harmonization)
6. F1 -> F2 (optional advanced features)
7. G1 -> G2 -> G3 (testing)

---

## Definition of Done (Phase 3)
- [ ] `RunImportScript(...)` can execute through Importing orchestration.
- [ ] ETL and Importing use aligned mapping and validation behavior.
- [ ] ETL logs and error semantics remain clear and compatible.
- [ ] Legacy fallback remains available during transition.
- [ ] Integration tests pass for success and failure paths.
