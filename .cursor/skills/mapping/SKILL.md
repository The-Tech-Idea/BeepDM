---
name: mapping
description: Guidance for MappingManager usage to create and persist entity and field mappings in BeepDM.
---

# Entity Mapping Guide

Use this skill when mapping fields between source and destination entities, or when preparing mappings for ETL or import.

## Core Types
- EntityDataMap: destination mapping root
- EntityDataMap_DTL: source entity mapping details
- Mapping_rep_fields: per-field mapping

## Workflow
1. Create mapping via `MappingManager.CreateEntityMap`.
2. Adjust or add `FieldMapping` as needed.
3. Save mapping for reuse.
4. Use mapping with ETL or import operations.

## Validation
- Ensure destination entity structure exists.
- Validate that `FieldMapping` is not empty before use.
- Use DefaultsManager to fill missing destination fields.

## Pitfalls
- ObjectType mismatches cause downstream UI discovery issues.
- Missing mappings lead to null destination fields.
- Forgetting to save mappings means they are not reusable.

## File Locations
- DataManagementEngineStandard/Editor/Mapping/MappingManager.cs
- DataManagementEngineStandard/Editor/Mapping/README.md

## Example
```csharp
var (errorInfo, map) = MappingManager.CreateEntityMap(
    editor,
    "LegacyCustomers",
    "LegacyDB",
    "Customers",
    "MainDB");

if (errorInfo.Flag == Errors.Ok)
{
    MappingManager.SaveMapping(editor, "Customers", "MainDB", map);
}
```

## Task-Specific Examples

### Manual Field Mapping
```csharp
var mapped = map.MappedEntities.First();
mapped.FieldMapping = new List<Mapping_rep_fields>
{
    new Mapping_rep_fields { FromFieldName = "CustName", ToFieldName = "Name" },
    new Mapping_rep_fields { FromFieldName = "CustEmail", ToFieldName = "Email" }
};
```

### Map Object To Destination
```csharp
var sourceObject = sourceDs.GetEntity("LegacyCustomers", new List<AppFilter>()).First();
var destObject = MappingManager.MapObjectToAnother(editor, "Customers", map.MappedEntities.First(), sourceObject);
```