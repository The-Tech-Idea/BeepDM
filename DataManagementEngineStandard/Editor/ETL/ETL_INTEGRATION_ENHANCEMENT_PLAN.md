# ETL Integration Enhancement Plan

This plan upgrades ETL to be tightly integrated with Mapping, Migration, and Importing while preserving current ETL public APIs.

## Objectives
- Keep `ETLEditor` usable for existing callers.
- Remove duplicate ETL logic by reusing Importing and Mapping pipelines.
- Improve runtime efficiency (batching, async flow, retries, cancellation).
- Standardize script persistence and run telemetry.

## Scope
- ETL:
  - `DataManagementEngineStandard/Editor/ETL/ETLEditor.cs`
  - `DataManagementEngineStandard/Editor/ETL/ETLDataCopier.cs`
  - `DataManagementEngineStandard/Editor/ETL/ETLScriptManager.cs`
  - `DataManagementEngineStandard/Editor/ETL/ETLValidator.cs`
  - `DataManagementEngineStandard/Editor/ETL/ETLScriptBuilder.cs`
  - `DataManagementEngineStandard/Editor/ETL/ETLEntityCopyHelper.cs`
- Mapping:
  - `DataManagementEngineStandard/Editor/Mapping/MappingManager.cs`
- Migration:
  - `DataManagementEngineStandard/Editor/Migration/MigrationManager.cs`
- Importing:
  - `DataManagementEngineStandard/Editor/Importing/DataImportManager*.cs`
  - `DataManagementEngineStandard/Editor/Importing/Interfaces/IDataImportInterfaces.cs`

---

## Phase 1 - Integration Baseline and Preflight

### Goal
Introduce mandatory preflight and module integration points without breaking ETL call signatures.

### Task Checklist
- [ ] Add preflight step before `RunCreateScript(...)`.
- [ ] Add preflight step before `RunImportScript(...)`.
- [ ] Call `ETLValidator.ValidateEntityConsistency(...)` for each copy script detail.
- [ ] Call `ETLValidator.ValidateEntityMapping(...)` and `ValidateMappedEntity(...)` for mapping imports.
- [ ] Add optional hook to run `DataImportManager.RunMigrationPreflightAsync(...)` for large runs.
- [ ] Ensure failed preflight exits early with clear `IErrorsInfo`.

### Exit Criteria
- ETL run is blocked on failed mapping/schema preflight.
- Existing ETL callers still compile and execute.

---

## Phase 2 - Unified Data Copy Pipeline

### Goal
Use one efficient copy engine and remove duplicate row-copy behavior.

### Task Checklist
- [ ] Refactor `RunCopyEntityScript(...)` to use `ETLDataCopier.CopyEntityDataAsync(...)`.
- [ ] Reuse one transformation path for mapping/defaults/custom transform.
- [ ] Keep FK disable/enable behavior consistent around batch writes.
- [ ] Remove duplicate conversion/loop code paths now superseded by `ETLDataCopier`.
- [ ] Keep progress and cancellation callbacks compatible with existing UI listeners.

### Exit Criteria
- Main copy path supports batching/retries/parallel execution.
- Legacy behavior remains functionally equivalent for default settings.

---

## Phase 3 - ETL and Importing Deep Integration

### Goal
Delegate import-style ETL work to Importing orchestration for better reliability and reuse.

### Task Checklist
- [ ] Define ETL-to-Importing adapter from `ETLScriptDet` to `DataImportConfiguration`.
- [ ] Route `RunImportScript(...)` through `DataImportManager.RunImportAsync(...)`.
- [ ] Enable Importing batch strategy and retry settings from ETL script/config context.
- [ ] Integrate optional Importing quality/replay/history stores when configured.
- [ ] Ensure ETL logs include Importing run identifiers for traceability.

### Exit Criteria
- Mapping imports execute through Importing engine.
- ETL retains high-level orchestration while avoiding duplicated import logic.

---

## Phase 4 - Schema Alignment Through MigrationManager

### Goal
Move schema preparation to MigrationManager-centered operations.

### Task Checklist
- [ ] Replace direct schema assumptions in ETL create path with `MigrationManager.EnsureEntity(...)`.
- [ ] Support create-if-missing and add-missing-columns behavior.
- [ ] Add per-entity migration summaries into ETL run logs.
- [ ] Maintain compatibility for datasources that rely on direct `CreateEntityAs(...)`.

### Exit Criteria
- Entity creation/upgrade path is migration-aware and logged.
- ETL schema outcomes are deterministic across datasource types.

---

## Phase 5 - Script System Consolidation

### Goal
Unify script save/load/execute behaviors across `ETLEditor` and `ETLScriptManager`.

### Task Checklist
- [ ] Choose one script folder/versioning convention.
- [ ] Update `SaveETL(...)` and `LoadETL(...)` to align with `ETLScriptManager`.
- [ ] Remove or deprecate conflicting duplicate script execution paths.
- [ ] Add script version metadata for backward compatibility.
- [ ] Add migration logic for existing script files.

### Exit Criteria
- One authoritative script persistence model is active.
- Existing stored scripts continue to load via migration path.

---

## Phase 6 - Telemetry, Logging, and Runtime Control

### Goal
Make ETL run status accurate, diagnosable, and consistent with Importing.

### Task Checklist
- [ ] Normalize `ScriptCount` and `CurrentScriptRecord` semantics.
- [ ] Harmonize ETL `LoadDataLogs` format with import status snapshots.
- [ ] Standardize event payload content for progress, warning, and stop states.
- [ ] Add structured summary record at end of each ETL run.
- [ ] Ensure cancellation emits explicit terminal status.

### Exit Criteria
- Operators can diagnose ETL runs from logs without reading code.
- Progress and stop behavior are consistent across create/copy/import modes.

---

## Phase 7 - Performance and Async Cleanup

### Goal
Eliminate sync-over-async bottlenecks and improve throughput.

### Task Checklist
- [ ] Remove `Wait()` and blocking `GetResult()` patterns from ETL hot paths.
- [ ] Use async fetch/transform/insert flow throughout run pipeline.
- [ ] Replace type-name string checks with robust typed checks where possible.
- [ ] Benchmark representative copy workloads and compare before/after.
- [ ] Tune default batch size and parallelism with safe conservative defaults.

### Exit Criteria
- ETL runtime avoids blocking anti-patterns.
- Throughput and responsiveness are measurably improved.

---

## Phase 8 - Validation, Testing, and Rollout

### Goal
Ship safely with regression coverage and migration guidance.

### Task Checklist
- [ ] Add regression tests for:
  - [ ] create-only scripts
  - [ ] copy-only scripts
  - [ ] create+copy scripts
  - [ ] mapping-based import scripts
- [ ] Add integration tests for ETL + Mapping.
- [ ] Add integration tests for ETL + Migration.
- [ ] Add integration tests for ETL + Importing.
- [ ] Add cancellation/retry/error-threshold tests.
- [ ] Write rollout notes and migration guide for ETL callers.

### Exit Criteria
- Test suite covers critical ETL integration flows.
- Rollout guide is ready for downstream teams/plugins.

---

## Suggested Execution Order
1. Phase 1 (preflight baseline)
2. Phase 2 (copy unification)
3. Phase 3 (importing integration)
4. Phase 4 (migration alignment)
5. Phase 5 (script consolidation)
6. Phase 6-7 (telemetry + performance)
7. Phase 8 (test/rollout)

## Risks and Mitigations
- Risk: behavioral drift for legacy ETL consumers.
  - Mitigation: preserve public signatures and add compatibility adapter layer.
- Risk: script backward compatibility issues.
  - Mitigation: add script versioning and migration loader.
- Risk: mixed telemetry during transition.
  - Mitigation: introduce unified run-summary schema early (Phase 6).
