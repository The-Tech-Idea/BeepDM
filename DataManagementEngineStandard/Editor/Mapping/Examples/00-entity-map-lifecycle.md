# 00 - Entity Map Lifecycle

## Goal
Create, edit, persist, and execute a basic entity mapping.

## Example
```csharp
var (error, map) = MappingManager.CreateEntityMap(
    editor,
    "LegacyCustomers",
    "LegacyDb",
    "Customers",
    "MainDb");

if (error.Flag != Errors.Ok)
{
    throw new InvalidOperationException(error.Message);
}

var detail = map.MappedEntities.FirstOrDefault();
if (detail == null)
{
    throw new InvalidOperationException("No mapped source detail was created.");
}

detail.FieldMapping = new List<Mapping_rep_fields>
{
    new Mapping_rep_fields { FromFieldName = "CustName", ToFieldName = "Name" },
    new Mapping_rep_fields { FromFieldName = "CustEmail", ToFieldName = "Email" },
    new Mapping_rep_fields { FromFieldName = "CustStatus", ToFieldName = "Status" }
};

MappingManager.SaveEntityMap(editor, "Customers", "MainDb", map);

var sourceObject = sourceRow; // any source record instance
var destinationObject = MappingManager.MapObjectToAnother(
    editor,
    "Customers",
    detail,
    sourceObject);
```

## Outcome
- Mapping is persisted under `Config.MappingPath/MainDb/Customers_Mapping.json`.
- Defaults are applied after field copy via DefaultsManager helper integration.
