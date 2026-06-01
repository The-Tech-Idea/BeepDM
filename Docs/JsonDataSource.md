# JsonDataSource Guide

## Overview

JsonDataSource provides JSON file and document-based data management with schema inference, CRUD operations, filtering, and deep entity resolution.

## Features

- JSON file reading and writing
- Schema inference and synchronization
- CRUD operations on JSON documents
- JSONPath filtering and querying
- Graph hydration and deep entity resolution
- Relationship handling
- Async operations
- Caching support

## Quick Start

```csharp
// Create connection
var props = new ConnectionProperties
{
    ConnectionName = "MyJsonDb",
    DatabaseType = DataSourceType.Json,
    ConnectionString = "./data/documents.json",
    Category = DatasourceCategory.FileBased
};

editor.ConfigEditor.AddDataConnection(props);
var ds = editor.GetDataSource("MyJsonDb");
ds.Openconnection();

// Read data
var users = ds.GetEntity("Users", new List<AppFilter>());

// Insert
var newUser = new Dictionary<string, object>
{
    ["Id"] = 1,
    ["Name"] = "John Doe",
    ["Email"] = "john@example.com"
};
ds.InsertEntity("Users", newUser);
```

## Advanced Features

### Schema Inference

```csharp
// Automatically infer schema from JSON structure
var structure = ds.GetEntityStructure("Users");
```

### JSONPath Queries

```csharp
// Use JSONPath for complex queries
var filtered = ds.GetEntity("Users", new List<AppFilter>{
    new AppFilter { FieldName = "$.address.city", Operator = "=", FilterValue = "NYC" }
});
```

### Graph Hydration

```csharp
// Resolve nested relationships
var options = new GraphHydrationOptions
{
    MaxDepth = 3,
    FollowForeignKeys = true
};
var hydrated = ds.GetEntityWithHydration("Orders", options);
```

## File Locations

- `DataManagementEngineStandard/Json/JsonDataSource.cs`
- `DataManagementEngineStandard/Json/JsonDataSourceAdvanced.cs`
- `DataManagementEngineStandard/Json/Helpers/`

## Related Documentation

- [Core Architecture](CoreArchitecture.md)
- [File DataSource](Docs/FileDataSource.md)
