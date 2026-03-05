# ETL Phase 1 Implementation Tasks

This file breaks down **Phase 1** from `ETL_INTEGRATION_ENHANCEMENT_PLAN.md` into concrete method-level work.

## Phase 1 Goal
- Add mandatory preflight and integration hooks before ETL execution.
- Keep `ETLEditor` public signatures unchanged.

## Files In Scope
- `DataManagementEngineStandard/Editor/ETL/ETLEditor.cs`
- `DataManagementEngineStandard/Editor/ETL/ETLValidator.cs`
- `DataManagementEngineStandard/Editor/Importing/DataImportManager*.cs` (consume preflight API only)
- `DataManagementEngineStandard/Editor/Mapping/MappingManager*.cs` (consume validation/map checks only)
- `DataManagementEngineStandard/Editor/Migration/MigrationManager.cs` (optional readiness checks)

---

## Workstream A - Preflight Orchestration in ETLEditor

### A1. Add internal preflight entrypoints
- [ ] In `ETLEditor.cs`, add private methods:
  - [ ] `PreflightCreateScriptAsync(IProgress<PassedArgs> progress, CancellationToken token)`
  - [ ] `PreflightImportScriptAsync(IProgress<PassedArgs> progress, CancellationToken token)`
- [ ] Ensure both methods return `IErrorsInfo` and set `DMEEditor.ErrorObject` consistently.
- [ ] Ensure both methods write clear log messages for pass/fail states.

### A2. Wire preflight into run methods
- [ ] In `RunCreateScript(...)`, call `PreflightCreateScriptAsync(...)` at top.
- [ ] In `RunImportScript(...)`, call `PreflightImportScriptAsync(...)` at top.
- [ ] If preflight fails, exit early with `Errors.Failed` and a clear message.

### A3. Backward compatibility behavior
- [ ] Preserve existing `RunCreateScript` and `RunImportScript` method signatures.
- [ ] Preserve existing default parameter values.
- [ ] Preserve existing progress event style (`PassedArgs`) for callers.

---

## Workstream B - Validation Gates

### B1. Mapping validation gate
- [ ] In `PreflightImportScriptAsync(...)`:
  - [ ] Validate script contains import detail and mapping payload.
  - [ ] Call `ETLValidator.ValidateEntityMapping(...)` for main mapping object when available.
  - [ ] Call `ETLValidator.ValidateMappedEntity(...)` for selected detail mapping.
  - [ ] Convert validation errors into one consolidated `IErrorsInfo`.

### B2. Entity consistency gate
- [ ] In `PreflightCreateScriptAsync(...)`:
  - [ ] For each active `CopyData` script step, resolve source/destination datasources.
  - [ ] Call `ETLValidator.ValidateEntityConsistency(...)`.
  - [ ] Stop on first hard failure (configurable later), report which entity pair failed.

### B3. Null/shape guards
- [ ] Add guards for:
  - [ ] null or empty `Script`.
  - [ ] missing `ScriptDetails`.
  - [ ] missing datasource names.
  - [ ] missing source or destination entity names.

---

## Workstream C - Importing Preflight Hook

### C1. Optional migration preflight bridge
- [ ] In `PreflightCreateScriptAsync(...)`, add optional call to `DataImportManager.RunMigrationPreflightAsync(...)`:
  - [ ] trigger only for runs containing `CreateEntity` or explicit flag.
  - [ ] pass context for source/destination entities.
- [ ] Log preflight summary from Importing module.
- [ ] Treat blocking preflight findings as ETL preflight failures.

### C2. Feature flag for rollout safety
- [ ] Add internal bool switch (default on) to toggle Importing preflight bridge.
- [ ] Keep fallback path (ETL local validation only) if hook disabled or unavailable.

---

## Workstream D - Preflight Reporting and Diagnostics

### D1. Structured preflight result
- [ ] Define internal preflight result object (or equivalent tuple) with:
  - [ ] `Passed`
  - [ ] `Stage`
  - [ ] `EntityName`
  - [ ] `Message`
- [ ] Convert result into:
  - [ ] `DMEEditor.ErrorObject`
  - [ ] `LoadDataLogs` preflight entries
  - [ ] progress notifications (`PassedArgs`)

### D2. Logging consistency
- [ ] Standardize log categories: `ETL.Preflight`, `ETL.Validation`, `ETL.ImportBridge`.
- [ ] Include datasource + entity in failure logs.
- [ ] Emit one preflight summary line before exiting run path.

---

## Workstream E - Regression Tests for Phase 1

### E1. Create-script preflight tests
- [ ] Fails when script has missing datasource names.
- [ ] Fails when source/destination entity structure mismatch exists.
- [ ] Passes when all `CopyData` script details are consistent.

### E2. Import-script preflight tests
- [ ] Fails when mapping detail is missing fields/field mappings.
- [ ] Fails when selected mapped entity is invalid.
- [ ] Passes with valid `EntityDataMap` and mapped detail.

### E3. Compatibility tests
- [ ] Existing caller code can call `RunCreateScript(...)` unchanged.
- [ ] Existing caller code can call `RunImportScript(...)` unchanged.
- [ ] Early-failure preflight does not execute copy/create work.

---

## Suggested Implementation Order
1. A1 -> A2 (add/wire preflight methods)
2. B1 -> B2 -> B3 (validation guards)
3. D1 -> D2 (diagnostics and output consistency)
4. C1 -> C2 (Importing bridge and feature toggle)
5. E1 -> E2 -> E3 (tests)

---

## Definition of Done (Phase 1)
- [ ] Both run entrypoints enforce preflight before execution.
- [ ] Mapping and entity-consistency failures are surfaced early and clearly.
- [ ] Importing migration preflight bridge is available and safe to toggle.
- [ ] No public API signature changes in `ETLEditor`.
- [ ] Phase 1 tests pass and cover failure + success paths.
