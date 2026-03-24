# Entity Mapping Quick Reference

## Folder Index

- `Editor/Mapping/Configuration`: mapper options and type-map registration.
- `Editor/Mapping/Core`: AutoObjMapper execution and compiled delegate runtime.
- `Editor/Mapping/Models`: mapper result and diff models.
- `Editor/Mapping/Interfaces`: contracts for mapper components.
- `Editor/Mapping/Helpers`: defaults, conversion, validation, performance helpers.
- `Editor/Mapping/Extensions`: fluent and diagnostics-oriented extension APIs.
- `Editor/Mapping/Utilities`: reusable static utility methods.

## Create / Load Mapping

```csharp
// From entity names
var (error, entityMap) = MappingManager.CreateEntityMap(
    dmeEditor, "SourceEntity", "SourceDB", "DestEntity", "DestDB"
);

// From entity structure
var destEntity = dmeEditor.GetDataSource("DestDB").GetEntityStructure("Customers", true);
var (error, entityMap) = MappingManager.CreateEntityMap(dmeEditor, destEntity, "SourceEntity", "SourceDB");

// Empty mapping
var (error, entityMap) = MappingManager.CreateEntityMap(dmeEditor, "Customers", "MainDB");

// Load existing mapping
var existingMap = dmeEditor.ConfigEditor.LoadMappingValues("Customers", "MainDB");
```

## Add Source Entity

```csharp
var mappedEntity = MappingManager.AddEntityToMappedEntities(
    dmeEditor, "SourceDB", "SourceEntity", destEntity
);
entityMap.MappedEntities.Add(mappedEntity);
```

## Field Mapping

```csharp
// Automatic mapping (by name)
var mappings = MappingManager.MapEntityFields(dmeEditor, sourceEntity, mappedEntity);

// Manual mapping
mappedEntity.FieldMapping = new List<Mapping_rep_fields>
{
    new Mapping_rep_fields { FromFieldName = "CustName", ToFieldName = "Name" }
};
```

## Object Transformation

```csharp
var destObject = MappingManager.MapObjectToAnother(
    dmeEditor, "Customers", mappedEntity, sourceObject
);
// Automatically applies defaults via DefaultsManager
```

## Object Graph Transformation

```csharp
var (dest, result) = MappingManager.MapObjectGraph(
    dmeEditor,
    "Customers",
    mappedEntity,
    sourceObject,
    new ObjectGraphMappingOptions
    {
        MaxDepth = 6,
        DetectCycles = true,
        CollectionMode = CollectionMappingMode.MergeByKey,
        CollectionMergeKeyPropertyName = "Id"
    });
```

## Quality and Drift Gate

```csharp
var quality = MappingManager.ValidateMappingWithScore(dmeEditor, entityMap, productionThreshold: 80);
var drift = MappingManager.DetectMappingDrift(dmeEditor, entityMap, "SourceDB", "DestDB");
var allowed = MappingManager.EnforceProductionQualityThreshold(quality, 80);
```

## Persistence

```csharp
MappingManager.SaveEntityMap(dmeEditor, "Customers", "MainDB", entityMap);
var entityMap = dmeEditor.ConfigEditor.LoadMappingValues("Customers", "MainDB");
```

## Governance and Approval

```csharp
using (MappingManager.BeginGovernanceScope(
    author: "data-eng",
    changeReason: "Align source aliases",
    targetState: MappingApprovalState.Review))
{
    MappingManager.SaveEntityMap(dmeEditor, "Customers", "MainDB", entityMap);
}

var history = MappingManager.GetMappingVersionHistory(dmeEditor, "Customers", "MainDB");
MappingManager.UpdateMappingApprovalState(
    dmeEditor, "Customers", "MainDB", MappingApprovalState.Approved, "release", "Approved for prod");
```

## Integration with ETL

```csharp
var (error, entityMap) = MappingManager.CreateEntityMap(...);
var selected = entityMap.MappedEntities.First();
etl.CreateImportScript(entityMap, selected);
await etl.RunImportScript(progress, cancellationToken);
```
