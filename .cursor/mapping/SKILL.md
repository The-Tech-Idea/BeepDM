---
name: mapping
description: Guidance for MappingManager usage to create and persist entity and field mappings in BeepDM.
---

# Entity Mapping Guide

## Owned Metadata
Use Models `EntityMetadataSnapshot.Capture` for independent run definitions, not
the legacy shallow `EntityStructure.Clone`. It preserves key references and copies
supported nested options without event subscribers; custom/cyclic/oversized graphs
reject explicitly. Coordinate capture against concurrent edits and keep the result
private. `EntityField.Clone` now terminates, but remains shallow for custom members.
Read `DataManagementModelsStandard/DataBase/METADATA-SNAPSHOTS.md` for limits.
Generated types now use requested-name/source identity with bounded single-flight
compilation. Bare typeCache pre-seeds no longer override metadata; MyType/MyObject
remain unsafe last-result globals. Read Engine `ConfigUtil/GENERATED-TYPES.md` for
cache/compatibility/unloading limits. SyncSchemaTranslator.BindEntityMetadata now
binds captured provider metadata into both destination field lists. Validate actual
pairs and required target coverage before either bidirectional import; exact key
pairs are added once. Bound mode requires existing targets and does not infer mapped
DDL. Read Engine Editor/BeepSync/STORAGE-AND-OUTCOMES.md and test actual generated
payloads, not pre-seed types or weaken validation. Record DQ uses the separate
post-transform import admission gate. Sync thresholds/failed counts are separately
verified. File reject replay now uses prepared destination snapshots/CAS claims
and current quality, not repeated mappings/defaults. Read
`Editor/Importing/REJECT-RECOVERY.md`; complete intent and native/provider-run recovery remain.

Use this skill when implementing or updating mapping workflows in `MappingManager` for ETL/import/migration operations.

## Core Types
- `EntityDataMap`: destination-level mapping root.
- `EntityDataMap_DTL`: source entity detail + field mapping list.
- `Mapping_rep_fields`: field-to-field mapping contract.

## Implemented Capabilities
- Convention and scored auto matching (`AutoMapByConvention*`).
- Conversion policies and field transform pipelines.
- Rule-based conditional mapping with deterministic precedence.
- Nested/object graph and collection merge mapping.
- Validation scoring and schema drift detection.
- Performance path with compiled plan and accessor caches.
- Governance metadata (version, approval state, audit trail sidecar).

## Mapping Folder Architecture
- `Editor/Mapping/Configuration`: mapper options + type-map registration/configuration.
- `Editor/Mapping/Core`: runtime mapper engine (compile, execute, validate, perf, factory presets).
- `Editor/Mapping/Models`: mapping result/diff model types.
- `Editor/Mapping/Interfaces`: mapper contracts for execution/configuration abstraction.
- `Editor/Mapping/Helpers`: defaults, validation, property discovery, conversion, perf helpers.
- `Editor/Mapping/Extensions`: fluent configuration and convenience extension methods.
- `Editor/Mapping/Utilities`: reusable static mapper utility functions.

## Recommended Workflow
- For import admission use `MapObjectToAnotherStrict`: field failures propagate,
  unknown transforms reject, default conversion is Reject, and defaults belong to
  the caller's explicit stage. Deliberately registered conversion fallback/warning
  policies still apply. Legacy mapping/compiled plans remain best-effort; read
  `Editor/Importing/TRANSFORMATION-OUTCOMES.md` for exact limits.
1. Create/load map: `CreateEntityMap(...)` or `ConfigEditor.LoadMappingValues(...)`.
2. Apply auto matching and review low-confidence suggestions.
3. Configure conversion policy and field-level transforms.
4. Validate with score + drift checks before production execution.
5. Save mapping through `MappingManager.SaveEntityMap(...)`.
6. Execute record mapping via `MapObjectToAnother(...)` or `MapObjectGraph(...)`.
7. Use governance APIs for review/approval evidence.

## Governance Workflow
```csharp
using (MappingManager.BeginGovernanceScope(
    author: "etl-ops",
    changeReason: "Phase rollout mapping update",
    targetState: MappingApprovalState.Review))
{
    MappingManager.SaveEntityMap(editor, "Customers", "MainDb", map);
}

var history = MappingManager.GetMappingVersionHistory(editor, "Customers", "MainDb");
MappingManager.UpdateMappingApprovalState(
    editor, "Customers", "MainDb", MappingApprovalState.Approved, "release", "QA passed");
```

## Quality Gate Pattern
```csharp
var quality = MappingManager.ValidateMappingWithScore(editor, map, productionThreshold: 80);
var drift = MappingManager.DetectMappingDrift(editor, map, "LegacyDb", "MainDb");
if (!MappingManager.EnforceProductionQualityThreshold(quality, 80))
{
    throw new InvalidOperationException("Mapping quality gate failed.");
}
```

## Common Pitfalls
- Saving without governance scope loses actor/reason traceability.
- Ignoring drift checks can break production after source schema changes.
- Skipping transform/conversion policy causes inconsistent typed values.
