---
name: mapping
description: Guidance for MappingManager usage to create and persist entity and field mappings in BeepDM.
---

# Entity Mapping Guide

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