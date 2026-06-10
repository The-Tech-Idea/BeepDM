---
name: etl
description: Detailed ETL guidance for ETLEditor workflows in BeepDM. Use when migrating data between datasources, generating ETL scripts, running direct schema/data copy operations, or diagnosing ETL runtime behavior and script execution paths.
---

# ETL Operations Guide

Use this skill when working with `ETLEditor` and the ETL runtime.

## Use this skill when
- Generating or executing ETL scripts
- Copying schema or data directly between datasources
- Using `ETLDataCopier`, `ETLEntityCopyHelper`, or `ETLScriptManager`
- Diagnosing ETL runtime telemetry, cancellation, or script persistence

## Do not use this skill when
- The task is primarily governed import execution with quality, replay, or staging. Use [`importing`](../importing/SKILL.md).
- The task is sync-schema orchestration over imports. Use [`beepsync`](../beepsync/SKILL.md).

## Core Runtime Types
- `ETLEditor`
- `ETLScriptHDR`
- `ETLScriptDet`
- `ETLValidator`
- `ETLDataCopier`
- `ETLEntityCopyHelper`
- `ETLScriptBuilder`
- `ETLScriptManager`
- `ETLEntityProcessor`

## File Locations
> **Note:** This skill covers the legacy ETL runtime. For the modern pipeline-based ETL engine, see the `etl-engine` skill.

- `DataManagementEngineStandard/Editor/ETL_BK/ETLEditor.cs`
- `DataManagementEngineStandard/Editor/ETL_BK/ETLDataCopier.cs`
- `DataManagementEngineStandard/Editor/ETL_BK/ETLEntityCopyHelper.cs`
- `DataManagementEngineStandard/Editor/ETL_BK/ETLEntityProcessor.cs`
- `DataManagementEngineStandard/Editor/ETL_BK/ETLScriptBuilder.cs`
- `DataManagementEngineStandard/Editor/ETL_BK/ETLScriptManager.cs`
- `DataManagementEngineStandard/Editor/ETL_BK/ETLValidator.cs`

## Working Rules
1. Resolve and open source/destination datasources before high-cost runs.
2. Keep destination names and entity mappings explicit.
3. Treat ETL script persistence and execution as part of the workflow, not incidental output.
4. Use ETL for direct copy/script flows; hand off to importing when governance features dominate.

## Related Skills
- [`importing`](../importing/SKILL.md)
- [`beepsync`](../beepsync/SKILL.md)
- [`migration`](../migration/SKILL.md)
- [`idatasource`](../idatasource/SKILL.md)

## Integration with the data-management layer

ETL is the **runtime data-mover** of BeepDM. It runs after the system is initialized and uses the persisted config:

| Direction | Layer | What flows |
|---|---|---|
| ← **configeditor** | `ConfigEditor` façade | Reads `EntityDataMap` from `EntityMappingManager` for field translation. |
| ← **migration** | `MigrationManager` | Assumes the target schema exists. If a column is missing, ETL fails fast and reports. |
| ← **schema** | `ISchemaManager` | `ETLEditor.TryRunImportingPreflightAsync` calls this service directly. Replaces the old "spin up a full `DataImportManager` just for preflight" pattern. |
| ↔ **unitofwork** | `UnitofWork<T>` | ETL sinks wrap per-record writes in UoW when the target needs transactional semantics. |
| ← **setup** | Setup Framework (Phase 6) | Setup may invoke a one-shot pipeline to load reference data on first run. |
| ↔ **forms** | `FormsManager` | ETL produces records; Forms displays them. Forms does not trigger ETL — that's the workflow engine's job. |

The Mavis cross-project equivalent of this skill lives at `.harness/skills/beepdm-etl/SKILL.md`.

## Detailed Reference
Use [`reference.md`](./reference.md) for workflows, runtime controls, and operational pitfalls.
