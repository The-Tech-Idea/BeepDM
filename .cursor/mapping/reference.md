# Entity Mapping Quick Reference

## Create Mapping

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

## Persistence

```csharp
MappingManager.SaveMapping(dmeEditor, "Customers", "MainDB", entityMap);
var entityMap = MappingManager.LoadOrInitializeMapping(dmeEditor, "Customers", "MainDB");
```

## Integration with ETL

```csharp
var (error, entityMap) = MappingManager.CreateEntityMap(...);
var selected = entityMap.MappedEntities.First();
etl.CreateImportScript(entityMap, selected);
await etl.RunImportScript(progress, cancellationToken);
```
