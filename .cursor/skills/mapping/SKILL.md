---
name: mapping
description: Expert guidance for entity mapping operations using MappingManager. Use when mapping fields between source and destination entities, transforming data during migration, or creating entity mappings for ETL/import operations.
---

# Entity Mapping Guide

Expert guidance for entity mapping operations using MappingManager, which provides utility methods to create and manage entity mappings between source and destination entities.

## Overview

**MappingManager** provides entity mapping capabilities:
- **Entity Mapping Creation**: Create mappings between source and destination entities
- **Field Mapping**: Automatic and manual field mapping
- **Object Transformation**: Map objects from source to destination format
- **DefaultsManager Integration**: Automatic default value application during mapping
- **Mapping Persistence**: Save and load mappings for reuse

## Core Concepts

### EntityDataMap
Main mapping structure containing:
- `MappingName` - Unique name for the mapping
- `EntityFields` - Destination entity fields
- `MappedEntities` - List of source entities mapped to destination

### EntityDataMap_DTL
Detail mapping for a specific source entity:
- `EntityName` - Source entity name
- `EntityDataSource` - Source datasource name
- `SelectedDestFields` - Fields selected for mapping
- `FieldMapping` - List of field mappings

### Mapping_rep_fields
Individual field mapping:
- `FromFieldName` - Source field name
- `FromFieldType` - Source field type
- `ToFieldName` - Destination field name
- `ToFieldType` - Destination field type

## Basic Usage

### Create Entity Map (Source Entity to Destination)

```csharp
// Create mapping from source entity to destination entity structure
var destEntity = dmeEditor.GetDataSource("MainDB")
    .GetEntityStructure("Customers", true);

var (errorInfo, entityMap) = MappingManager.CreateEntityMap(
    dmeEditor,
    destEntity,                    // Destination entity structure
    "LegacyCustomers",             // Source entity name
    "LegacyDB"                     // Source datasource name
);

if (errorInfo.Flag == Errors.Ok)
{
    // Mapping created successfully
    var mappedEntity = entityMap.MappedEntities.First();
    Console.WriteLine($"Mapped: {mappedEntity.EntityName}");
}
```

### Create Entity Map (Entity Names)

```csharp
// Create mapping using entity names
var (errorInfo, entityMap) = MappingManager.CreateEntityMap(
    dmeEditor,
    "LegacyCustomers",    // Source entity name
    "LegacyDB",          // Source datasource name
    "Customers",         // Destination entity name
    "MainDB"             // Destination datasource name
);
```

### Create Empty Entity Map

```csharp
// Create empty mapping for destination entity
var (errorInfo, entityMap) = MappingManager.CreateEntityMap(
    dmeEditor,
    "Customers",    // Destination entity name
    "MainDB"        // Destination datasource name
);

// Add source entities later
var mappedEntity = MappingManager.AddEntityToMappedEntities(
    dmeEditor,
    "LegacyDB",
    "LegacyCustomers",
    destEntity
);

entityMap.MappedEntities.Add(mappedEntity);
```

## Field Mapping

### Automatic Field Mapping

```csharp
var (errorInfo, entityMap) = MappingManager.CreateEntityMap(
    dmeEditor,
    "LegacyCustomers",
    "LegacyDB",
    "Customers",
    "MainDB"
);

// Fields are automatically mapped by name matching
var mappedEntity = entityMap.MappedEntities.First();
foreach (var fieldMapping in mappedEntity.FieldMapping)
{
    Console.WriteLine($"{fieldMapping.FromFieldName} -> {fieldMapping.ToFieldName}");
}
```

### Manual Field Mapping

```csharp
// Get mapped entity
var mappedEntity = entityMap.MappedEntities.First();

// Modify field mappings
mappedEntity.FieldMapping = new List<Mapping_rep_fields>
{
    new Mapping_rep_fields
    {
        FromFieldName = "CustName",
        ToFieldName = "Name",
        FromFieldType = "String",
        ToFieldType = "String"
    },
    new Mapping_rep_fields
    {
        FromFieldName = "CustEmail",
        ToFieldName = "Email",
        FromFieldType = "String",
        ToFieldType = "String"
    }
};
```

### Map Entity Fields

```csharp
// Get source entity structure
var sourceEntity = dmeEditor.GetDataSource("LegacyDB")
    .GetEntityStructure("LegacyCustomers", true);

// Map fields to destination
var fieldMappings = MappingManager.MapEntityFields(
    dmeEditor,
    sourceEntity,
    mappedEntity
);

// fieldMappings contains automatic mappings based on field name matching
```

## Object Transformation

### Map Object to Another

```csharp
// Get source object
var sourceObject = sourceDataSource.GetEntity("LegacyCustomers", filters);

// Map to destination object
var mappedEntity = entityMap.MappedEntities.First();
var destObject = MappingManager.MapObjectToAnother(
    dmeEditor,
    "Customers",           // Destination entity name
    mappedEntity,          // Mapping detail
    sourceObject           // Source object
);

// destObject is now a destination entity object with:
// - Mapped fields from source
// - Default values applied via DefaultsManager
// - Proper types converted
```

### Map with DefaultsManager Integration

```csharp
// MappingManager automatically applies defaults after mapping
var destObject = MappingManager.MapObjectToAnother(
    dmeEditor,
    "Customers",
    mappedEntity,
    sourceObject
);

// Defaults applied:
// - CreatedAt = DateTime.Now (if rule configured)
// - CreatedBy = Environment.UserName (if rule configured)
// - Status = "Active" (if static default configured)
```

## Mapping Persistence

### Save Mapping

```csharp
// Save mapping for reuse
MappingManager.SaveMapping(
    dmeEditor,
    "Customers",    // Entity name
    "MainDB",       // Datasource name
    entityMap       // Mapping to save
);
```

### Load Mapping

```csharp
// Load existing mapping
var entityMap = MappingManager.LoadOrInitializeMapping(
    dmeEditor,
    "Customers",
    "MainDB"
);

if (entityMap.MappedEntities.Any())
{
    Console.WriteLine("Mapping loaded successfully");
}
```

## Integration with ETL

### Use Mapping in ETL Import

```csharp
var etl = new ETLEditor(dmeEditor);

// Create mapping
var (errorInfo, entityMap) = MappingManager.CreateEntityMap(
    dmeEditor,
    "LegacyCustomers",
    "LegacyDB",
    "Customers",
    "MainDB"
);

var selected = entityMap.MappedEntities.First();

// Create import script from mapping
etl.CreateImportScript(entityMap, selected);

// Execute import
await etl.RunImportScript(progress, cancellationToken);
```

## Advanced Patterns

### Pattern 1: Multi-Source Mapping

```csharp
// Create mapping with multiple source entities
var (errorInfo, entityMap) = MappingManager.CreateEntityMap(
    dmeEditor,
    "Customers",
    "MainDB"
);

// Add multiple source entities
var legacyCustomers = MappingManager.AddEntityToMappedEntities(
    dmeEditor,
    "LegacyDB",
    "LegacyCustomers",
    destEntity
);

var externalCustomers = MappingManager.AddEntityToMappedEntities(
    dmeEditor,
    "ExternalCRM",
    "CRM_Customers",
    destEntity
);

entityMap.MappedEntities.Add(legacyCustomers);
entityMap.MappedEntities.Add(externalCustomers);

// Save mapping
MappingManager.SaveMapping(dmeEditor, "Customers", "MainDB", entityMap);
```

### Pattern 2: Conditional Field Mapping

```csharp
var mappedEntity = entityMap.MappedEntities.First();

// Customize field mappings based on conditions
var customMappings = new List<Mapping_rep_fields>();

foreach (var destField in mappedEntity.SelectedDestFields)
{
    // Find matching source field
    var sourceField = sourceEntity.Fields
        .FirstOrDefault(f => f.FieldName.Equals(destField.FieldName, StringComparison.InvariantCultureIgnoreCase));
    
    if (sourceField != null)
    {
        customMappings.Add(new Mapping_rep_fields
        {
            FromFieldName = sourceField.FieldName,
            FromFieldType = sourceField.Fieldtype,
            ToFieldName = destField.FieldName,
            ToFieldType = destField.Fieldtype
        });
    }
    else
    {
        // Field doesn't exist in source, will use default value
        Console.WriteLine($"Field {destField.FieldName} not found in source, will use default");
    }
}

mappedEntity.FieldMapping = customMappings;
```

### Pattern 3: Batch Object Mapping

```csharp
public List<object> MapBatchObjects(
    IDMEEditor dmeEditor,
    EntityDataMap_DTL mapping,
    List<object> sourceObjects)
{
    var destinationObjects = new List<object>();
    
    foreach (var sourceObject in sourceObjects)
    {
        try
        {
            var destObject = MappingManager.MapObjectToAnother(
                dmeEditor,
                mapping.EntityName,
                mapping,
                sourceObject
            );
            
            destinationObjects.Add(destObject);
        }
        catch (Exception ex)
        {
            dmeEditor.AddLogMessage("Mapping", 
                $"Error mapping object: {ex.Message}", 
                DateTime.Now, -1, null, Errors.Failed);
        }
    }
    
    return destinationObjects;
}
```

## Best Practices

### 1. Always Validate Mapping Before Use
```csharp
// Validate mapping completeness
var mappedEntity = entityMap.MappedEntities.First();
if (mappedEntity.FieldMapping == null || !mappedEntity.FieldMapping.Any())
{
    throw new InvalidOperationException("No field mappings defined");
}
```

### 2. Use DefaultsManager for Missing Fields
```csharp
// Fields not in source will automatically get default values
// Ensure DefaultsManager is configured for destination entity
DefaultsManager.SetColumnDefault(dmeEditor, "MainDB", "Customers", "Status", "Active", isRule: false);
```

### 3. Save Mappings for Reuse
```csharp
// Save mappings after creation for future use
MappingManager.SaveMapping(dmeEditor, "Customers", "MainDB", entityMap);
```

### 4. Handle Type Conversions
```csharp
// MappingManager handles basic type conversions
// For complex conversions, use CustomTransformation in DataImportManager
```

### 5. Map Objects Before Bulk Operations
```csharp
// Map objects before bulk insert/update operations
var destObjects = sourceObjects.Select(source => 
    MappingManager.MapObjectToAnother(dmeEditor, "Customers", mappedEntity, source)
).ToList();

// Then use UnitOfWork or DataImportManager for bulk operations
```

## Related Skills

- **@beepdm** - Core BeepDM architecture
- **@etl** - ETLEditor for ETL operations using mappings
- **@importing** - DataImportManager for import operations
- **@defaults** - DefaultsManager for default values during mapping

## Key Files

- `MappingManager.cs` - Main mapping manager
- `Helpers/MappingDefaultsHelper.cs` - DefaultsManager integration
- `Models/EntityDataMap.cs` - Mapping data structures
