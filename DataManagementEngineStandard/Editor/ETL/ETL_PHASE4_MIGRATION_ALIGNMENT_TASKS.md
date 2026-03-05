# ETL Phase 4 Implementation Tasks

This file breaks down **Phase 4** from `ETL_INTEGRATION_ENHANCEMENT_PLAN.md` into implementation tasks.

## Phase 4 Goal
- Align ETL schema preparation and evolution with `MigrationManager`.
- Make create/alter behavior deterministic and migration-aware.
- Preserve ETL script compatibility.

## Files In Scope
- ETL:
  - `DataManagementEngineStandard/Editor/ETL/ETLEditor.cs`
  - `DataManagementEngineStandard/Editor/ETL/ETLScriptBuilder.cs` (as needed)
- Migration:
  - `DataManagementEngineStandard/Editor/Migration/IMigrationManager.cs`
  - `DataManagementEngineStandard/Editor/Migration/MigrationManager.cs`
- Helper/metadata usage:
  - datasource helper calls through `IDMEEditor.GetDataSourceHelper(...)`

---

## Workstream A - Schema Orchestration Contract in ETL

### A1. Add migration-aware schema helper methods in ETL
- [ ] Add private ETL methods:
  - [ ] `EnsureDestinationEntityAsync(...)`
  - [ ] `ApplySchemaDeltaIfNeededAsync(...)`
- [ ] These methods should encapsulate MigrationManager interactions.

### A2. Keep ETL entrypoint signatures unchanged
- [ ] `RunCreateScript(...)` and create-related paths remain signature-compatible.
- [ ] Migration usage is internal to ETL orchestration.

---

## Workstream B - Integrate EnsureEntity for CreateEntity Steps

### B1. Replace direct create-first path
- [ ] In `RunCreateScript(...)`, for `DDLScriptType.CreateEntity`, route through:
  - [ ] `MigrationManager.EnsureEntity(...)` where applicable.
- [ ] Preserve fallback to `destds.CreateEntityAs(...)` when migration manager unavailable.

### B2. Entity name and structure normalization
- [ ] Ensure destination rename logic (`SourceEntityName` vs `DestinationEntityName`) is applied before migration call.
- [ ] Ensure schema source (`sc.SourceEntity` vs fetched structure) is resolved consistently.

### B3. Existing-entity behavior
- [ ] If entity exists:
  - [ ] use migration path to add missing columns when policy allows.
  - [ ] avoid recreate unless explicitly requested.

---

## Workstream C - Alter/Delta Script Support

### C1. Activate script types currently non-operational
- [ ] Add handling path for script types such as:
  - [ ] `AlterFor`
  - [ ] `AlterPrimaryKey` (if policy allows)
  - [ ] related alter script categories currently skipped.

### C2. Delegate DDL deltas through MigrationManager/helper SQL
- [ ] Use migration + helper-generated SQL for column add/alter/drop operations.
- [ ] Enforce provider capability checks before unsupported DDL actions.

### C3. Script-state updates
- [ ] Update `ETLScriptDet` state flags (`IsCreated`, `IsModified`, `Failed`, `ErrorMessage`) after each migration action.

---

## Workstream D - Migration Policy and Safety Controls

### D1. Define ETL schema policy flags
- [ ] Add internal options for:
  - [ ] create-if-missing
  - [ ] alter-if-needed
  - [ ] drop-not-allowed (default safe)
  - [ ] strict schema mode vs permissive mode

### D2. Guard unsafe operations
- [ ] Block destructive schema changes by default.
- [ ] Require explicit opt-in for drop/rename destructive operations.

### D3. Transaction and rollback strategy
- [ ] Where datasource supports transactions, wrap multi-step schema updates in transaction.
- [ ] Roll back on failure and emit schema-step error context.

---

## Workstream E - Logging and Migration Summary in ETL

### E1. Per-entity migration summary
- [ ] Log for each entity:
  - [ ] existed/created
  - [ ] columns added
  - [ ] columns altered
  - [ ] skipped actions and reasons

### E2. Final run summary
- [ ] Emit one ETL schema summary per run:
  - [ ] entities processed
  - [ ] entities created
  - [ ] entities altered
  - [ ] entities failed

### E3. Tracking alignment
- [ ] Persist migration-related details into `ETLScriptDet.Tracking` and `LoadDataLogs`.

---

## Workstream F - Compatibility and Fallback Paths

### F1. Runtime fallback matrix
- [ ] If MigrationManager missing/unavailable:
  - [ ] fallback to legacy create path.
- [ ] If helper capabilities are limited:
  - [ ] log skip + reason,
  - [ ] continue safely when possible.

### F2. Provider-specific extension points
- [ ] Keep hooks for provider-specific schema behavior (SQLite, DuckDB, etc.).
- [ ] Avoid hardcoded DDL assumptions in ETL core.

---

## Workstream G - Regression and Integration Tests

### G1. Schema creation tests
- [ ] creates missing destination entity via migration-aware path.
- [ ] does not recreate existing entity unnecessarily.

### G2. Schema delta tests
- [ ] adds missing columns when allowed.
- [ ] blocks unsupported/destructive operations in default policy.

### G3. End-to-end ETL tests
- [ ] create+copy script works with migration integration enabled.
- [ ] fallback path works when migration integration disabled.

### G4. Provider behavior tests
- [ ] verify at least one strict provider and one permissive provider path.

---

## Suggested Implementation Order
1. A1 -> A2 (ETL migration helper wrappers)
2. B1 -> B2 -> B3 (create step integration)
3. D1 -> D2 -> D3 (policy/safety controls)
4. C1 -> C2 -> C3 (alter script support)
5. E1 -> E2 -> E3 (summary/telemetry)
6. F1 -> F2 (fallback/provider hooks)
7. G1 -> G2 -> G3 -> G4 (tests)

---

## Definition of Done (Phase 4)
- [ ] ETL create flow is migration-aware by default.
- [ ] Schema delta behavior is policy-controlled and safe.
- [ ] Unsupported operations degrade gracefully with logs.
- [ ] ETL run includes clear schema migration summaries.
- [ ] Regression tests pass for create/delta/fallback paths.
