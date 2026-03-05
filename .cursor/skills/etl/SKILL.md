---
name: etl
description: Detailed ETL guidance for ETLEditor workflows, script building, validation, import execution, runtime controls, and cross-skill integration in BeepDM.
---

# ETL Operations Guide

Use this skill when migrating data between datasources, generating ETL scripts, running mapping-based imports, or diagnosing ETL runtime behavior.

## Decision Guide
- Use `ETLEditor` for schema+data copy orchestration, script generation, and direct datasource-to-datasource copy methods.
- Use `DataImportManager` for advanced import governance (staging, replay, data quality, run history, profiling).
- Use `ETLScriptManager` only when you need standalone JSON script persistence/execution outside the normal `ETLEditor` flow.

## Prerequisites
1. `IDMEEditor` is initialized and services are available.
2. Source and destination datasource names resolve in `DMEEditor.GetDataSource(...)`.
3. Required datasources can be opened (`DMEEditor.OpenDataSource(...)`).
4. Entity metadata is loaded for source and destination.
5. `IProgress<PassedArgs>` and `CancellationToken` are provided by caller.

## Core Runtime Types
- `ETLEditor`: main ETL orchestrator implementing `IETL`.
- `ETLScriptHDR`: script container (`ScriptSource`, `ScriptDetails`, run metadata).
- `ETLScriptDet`: per-step operation (`CreateEntity`, `CopyData`, mappings, tracking).
- `ETLValidator`: preflight validator for mappings and source/destination consistency.
- `ETLDataCopier`: optional async/batched data copy utility.
- `ETLScriptManager`: script repository-style load/save/validate/execute service.

## Public ETLEditor API Surface
- Script setup:
  - `CreateScriptHeader(IDataSource Srcds, IProgress<PassedArgs> progress, CancellationToken token)`
  - `GetCreateEntityScript(IDataSource ds, List<string> entities, ..., bool copydata = false)`
  - `GetCreateEntityScript(IDataSource Dest, List<EntityStructure> entities, ..., bool copydata = false)`
  - `GetCopyDataEntityScript(IDataSource Dest, List<EntityStructure> entities, ...)`
- Direct copy APIs:
  - `CopyEntityStructure`, `CopyEntitiesStructure`
  - `CopyEntityData`, `CopyEntitiesData`, `CopyDatasourceData`
- Script execution APIs:
  - `RunCreateScript(..., bool copydata = true, bool useEntityStructure = true)`
  - `CreateImportScript(EntityDataMap mapping, EntityDataMap_DTL selectedMapping)`
  - `RunImportScript(..., bool useEntityStructure = true)`
- Persistence:
  - `SaveETL(string datasourceName)`
  - `LoadETL(string datasourceName)`

## Primary Workflows
### 1) Full migration (create + copy)
1. `CreateScriptHeader(sourceDs, progress, token)`.
2. Ensure each `ETLScriptDet.DestinationDataSourceName` points to destination datasource.
3. Optionally filter/reorder `Script.ScriptDetails` and set `CopyData`.
4. Run `RunCreateScript(progress, token, copydata: true, useEntityStructure: true)`.
5. Review `IErrorsInfo`, `LoadDataLogs`, and `ETLScriptDet.Tracking`.

### 2) Structure-only migration
1. Build create scripts using `GetCreateEntityScript(...)`.
2. Execute `RunCreateScript(..., copydata: false, ...)`.
3. Validate destination schema before data load.

### 3) Data-only copy into existing schema
1. Build copy-only scripts with `GetCopyDataEntityScript(...)`.
2. Assign `Script.ScriptDetails` to copy list.
3. Run `RunCreateScript(..., copydata: true, ...)` with existing destination entities.

### 4) Mapping-based import
1. Build mapping (`EntityDataMap` + selected `EntityDataMap_DTL`).
2. `CreateImportScript(mapping, selectedMapping)`.
3. `RunImportScript(progress, token, useEntityStructure: true)`.
4. Check `LoadDataLogs` and destination inserts.

## Validation and Preflight
- `RunCreateScript(...)` and `RunImportScript(...)` now run ETL preflight internally.
- `RunCreateScript(...)` can bridge preflight to `DataImportManager.RunMigrationPreflightAsync(...)` when enabled.
- For explicit caller-controlled checks, you can still use:
  - `ETLValidator.ValidateEntityMapping(mapping)`
  - `ValidateMappedEntity(...)`
  - `ValidateEntityConsistency(sourceDs, destDs, srcEntity, destEntity)`
- Enforce `IErrorsInfo.Flag == Errors.Ok` as go/no-go before manual operation chaining.

## Runtime Controls and Diagnostics
- `StopErrorCount` controls early stop threshold during script execution.
- Cancellation is honored through `CancellationToken`; always pass a real token.
- `LoadDataLogs` is human-readable run trace (`InputLine` messages).
- `ETLScriptDet.Tracking` stores detailed per-script error/record context.
- `DMEEditor.ErrorObject` is authoritative final status for operation calls.
- Run-level telemetry is now correlated with a run id and emits summary counters.
- Evidence artifacts are generated under `Scripts/ETL_Evidence`:
  - per-run file: `<runId>.md`
  - rolling summary: `ETL_EVIDENCE_SUMMARY.md`
  - current week/month views: `ETL_EVIDENCE_CURRENT_WEEK.md`, `ETL_EVIDENCE_CURRENT_MONTH.md`

## Script Persistence and Reuse
- `SaveETL(datasourceName)` now validates script and persists via `ETLScriptManager` canonical path.
- `LoadETL(datasourceName)` now loads from manager first, with legacy folder fallback for compatibility.
- `ETLScriptManager` supports repository-style operations (`LoadScripts`, `SaveScript`, `ValidateScript`, `ExecuteScriptAsync`) when managing scripts independently.

## Operational Pitfalls
- Do not keep destination names equal to source unless intentionally copying back.
- If `useEntityStructure` is false, ETL re-reads structures from datasource at runtime.
- For large entity copies, prefer `ETLDataCopier.CopyEntityDataAsync(...)` with bounded batch/retry/parallel settings.
- Internal ETL preflight is active in `RunCreateScript`/`RunImportScript`; keep custom validator calls only when you need stricter caller-side gates.
- Direct helper methods like `InsertEntity(...)` are public implementation details, not part of `IETL`; avoid treating them as stable contract APIs.

## In-Memory Integration Pattern
- In-memory datasource implementations (for example `InMemoryRDBSource` variants) commonly:
  1. build scripts via `GetCreateEntityScript` or `GetCopyDataEntityScript`,
  2. assign `DMEEditor.ETL.Script.ScriptDetails`,
  3. execute `RunCreateScript` for load/refresh operations.
- Keep in-memory structure and entity-name synchronization consistent before ETL runs.

## Additional Resources
- API examples and troubleshooting snippets: [reference.md](reference.md)
- Related skills:
  - [beepservice](../beepservice/SKILL.md)
  - [connection](../connection/SKILL.md)
  - [environmentservice](../environmentservice/SKILL.md)
  - [idatasource](../idatasource/SKILL.md)
  - [inmemorydb](../inmemorydb/SKILL.md)
  - [importing](../importing/SKILL.md)
  - [migration](../migration/SKILL.md)
  - [universal-helper-factory](../universal-helper-factory/SKILL.md)
  - [universal-rdbms-helper](../universal-rdbms-helper/SKILL.md)
