# Connections

## Overview

Connection classes that bind BeepDM abstractions to concrete connection models for various data source types.

## Key Files

- `DefaulDataConnection.cs` - Default data connection implementation for standard datasources
- `FileConnection.cs` - File-based connection implementation for file datasources

## Features

- Connection state management (Open, Closed, Broken)
- Connection property abstraction
- Transaction support stubs
- File path handling for file-based sources

## Usage

```csharp
// Standard connection
var conn = new DefaulDataConnection();
conn.ConnectionString = "Server=localhost;Database=MyDb;";
conn.Open();

// File connection
var fileConn = new FileConnection();
fileConn.FilePath = "./data/customers.csv";
fileConn.Open();
```

## How It Fits

Used by datasource implementations and editor orchestration to:
- Create and open connections
- Manage connection state
- Handle connection-specific properties
- Support transaction workflows

## Related Documentation

- [Core Architecture](../Docs/CoreArchitecture.md)
- [Data Source Implementation](../Docs/HowToCreateNewDataSource.md)
- [WebAPI DataSource](../Docs/WebAPI.md)
