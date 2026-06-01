# InMemory DataSource

## Overview

In-memory datasource implementation for transient data scenarios, testing, and runtime-only data manipulation.

## Key Files

- `InMemoryDataSource.cs` - Primary in-memory datasource implementation

## Features

- Lightweight, transient data storage
- Full IDataSource contract implementation
- Ideal for testing and development
- Runtime-only data manipulation
- No persistence overhead

## Usage

```csharp
var props = new ConnectionProperties
{
    ConnectionName = "TempData",
    DatabaseType = DataSourceType.InMemory,
    Category = DatasourceCategory.Memory
};

editor.ConfigEditor.AddDataConnection(props);
var ds = editor.GetDataSource("TempData");
ds.Openconnection();

// Use like any other datasource
var data = ds.GetEntity("TestTable", new List<AppFilter>());
```

## How It Fits

Provides a lightweight datasource for:
- Unit testing without external dependencies
- Temporary workflows
- Runtime-only data manipulation
- Prototyping and development

## Related Documentation

- [Core Architecture](../Docs/CoreArchitecture.md)
- [Data Source Implementation](../Docs/HowToCreateNewDataSource.md)
