# ETL Phase 5 Implementation Tasks

This file breaks down **Phase 5** from `ETL_INTEGRATION_ENHANCEMENT_PLAN.md` into concrete implementation tasks.

## Phase 5 Goal
- Consolidate ETL script persistence/execution into one consistent system.
- Remove divergence between `ETLEditor` script files and `ETLScriptManager` repository behavior.
- Preserve backward compatibility for existing script files.

## Files In Scope
- `DataManagementEngineStandard/Editor/ETL/ETLEditor.cs`
- `DataManagementEngineStandard/Editor/ETL/ETLScriptManager.cs`
- `DataManagementModelsStandard/Editor/ETLScriptHDR.cs`
- `DataManagementModelsStandard/Editor/ETLScriptDet.cs`

---

## Workstream A - Canonical Script Storage Model

### A1. Define canonical layout
- [ ] Select single canonical storage convention:
  - [ ] root folder strategy
  - [ ] file naming strategy (`script-id`, datasource-based, or hybrid)
  - [ ] extension and metadata conventions

### A2. Add script metadata versioning
- [ ] Add version metadata field(s) in script header model.
- [ ] Define version semantics (format version, schema version).
- [ ] Add compatibility notes for old versions.

### A3. Define script identity strategy
- [ ] Standardize script `Id` generation/uniqueness.
- [ ] Ensure deterministic behavior across save/load/update.

---

## Workstream B - Unify Save/Load Paths

### B1. Route ETLEditor persistence through ETLScriptManager
- [ ] Refactor `ETLEditor.SaveETL(...)` to call manager save API.
- [ ] Refactor `ETLEditor.LoadETL(...)` to call manager load API.
- [ ] Keep existing ETLEditor signatures unchanged.

### B2. Remove duplicated file path logic
- [ ] Eliminate separate hardcoded script path logic from ETLEditor where replaced by manager.
- [ ] Keep one source of truth for script root and naming.

### B3. Consistent validation on save/load
- [ ] Ensure `ValidateScript(...)` is called prior to save.
- [ ] Ensure invalid scripts fail with clear error object/log output.

---

## Workstream C - Execution Path Consolidation

### C1. Define authoritative script executor
- [ ] Decide primary execution engine:
  - [ ] `ETLEditor` run path, or
  - [ ] `ETLScriptManager.ExecuteScriptAsync(...)` wrapped by ETLEditor.
- [ ] Document chosen authority in code comments and plan docs.

### C2. Align execution semantics
- [ ] Ensure script step ordering, active flags, and tracking behave identically across paths.
- [ ] Ensure cancellation/progress semantics are consistent in all invocation routes.

### C3. Deprecate duplicate path safely
- [ ] Keep legacy execution path behind internal feature switch during transition.
- [ ] Add logs to indicate active execution path.

---

## Workstream D - Legacy Script Migration

### D1. Detect old format scripts
- [ ] Add loader detection for legacy script location/file conventions.
- [ ] Identify scripts missing version metadata.

### D2. Implement migration conversion
- [ ] Convert old format to canonical format on load.
- [ ] Preserve script details, flags, mappings, and tracking where possible.
- [ ] Save migrated script in canonical location.

### D3. Migration safety
- [ ] Keep backup copy of original legacy scripts before write-back.
- [ ] Emit migration report logs per converted script.

---

## Workstream E - API and Compatibility Guarantees

### E1. Maintain public API behavior
- [ ] Keep ETLEditor script methods callable as before:
  - [ ] `SaveETL(...)`
  - [ ] `LoadETL(...)`
  - [ ] run methods that rely on `Script`

### E2. Manager API improvements (non-breaking)
- [ ] Add overloads/helpers in `ETLScriptManager` if needed, without breaking existing methods.
- [ ] Keep return types and error semantics consistent (`IErrorsInfo`).

### E3. Cross-module compatibility
- [ ] Ensure existing callers from in-memory datasource flows still function.
- [ ] Ensure imported scripts from prior ETL skills/examples remain usable.

---

## Workstream F - Diagnostics and Operational Visibility

### F1. Script lifecycle logs
- [ ] Log lifecycle events:
  - [ ] load source (canonical vs legacy)
  - [ ] validation result
  - [ ] migration action
  - [ ] save path/id

### F2. Script validation diagnostics
- [ ] Include specific validation failures (missing source, missing details, invalid datasource names).
- [ ] Emit script id/name in each failure log.

---

## Workstream G - Regression and Migration Tests

### G1. Save/load tests
- [ ] save then load returns equivalent script content.
- [ ] invalid script save is rejected.

### G2. Legacy migration tests
- [ ] old-format scripts are detected and migrated.
- [ ] migration preserves critical script data.
- [ ] backup behavior works.

### G3. Execution compatibility tests
- [ ] script execution still works after migration.
- [ ] both old and canonical scripts run successfully through ETLEditor entrypoints.

---

## Suggested Implementation Order
1. A1 -> A2 -> A3 (canonical model)
2. B1 -> B2 -> B3 (save/load unification)
3. D1 -> D2 -> D3 (legacy migration)
4. C1 -> C2 -> C3 (execution consolidation)
5. E1 -> E2 -> E3 (compatibility)
6. F1 -> F2 (diagnostics)
7. G1 -> G2 -> G3 (tests)

---

## Definition of Done (Phase 5)
- [ ] One canonical script persistence model is active.
- [ ] ETLEditor and ETLScriptManager use aligned save/load behavior.
- [ ] Legacy scripts migrate automatically and safely.
- [ ] Execution remains compatible for existing callers.
- [ ] Regression and migration tests pass.
