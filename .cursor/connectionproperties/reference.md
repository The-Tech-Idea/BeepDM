# ConnectionProperties Quick Reference

## Feature Flags
```csharp
bool IsLocal { get; set; }         // Local datasource
bool IsRemote { get; set; }        // Remote datasource
bool IsWebApi { get; set; }         // Web API
bool IsFile { get; set; }           // File-based
bool IsDatabase { get; set; }       // Database
bool IsComposite { get; set; }      // Composite
bool IsCloud { get; set; }          // Cloud-based
bool IsInMemory { get; set; }       // In-memory
bool IsDefault { get; set; }        // Default connection
bool IsFavourite { get; set; }      // Favorite
bool ReadOnly { get; set; }         // Read-only
```

## Core Properties
```csharp
string ConnectionName { get; set; }
DataSourceType DatabaseType { get; set; }
DatasourceCategory Category { get; set; }
string ConnectionString { get; set; }
string FilePath { get; set; }
string FileName { get; set; }
string Host { get; set; }
int Port { get; set; }
string Database { get; set; }
string UserID { get; set; }
string Password { get; set; }
string DriverName { get; set; }
string DriverVersion { get; set; }
```

## Filtering Patterns

### Local Connections
```csharp
var local = connections.Where(c => c.IsLocal).ToList();
```

### Local Databases
```csharp
var localDB = connections.Where(c => c.IsLocal && c.IsDatabase).ToList();
```

### In-Memory Local
```csharp
var inMemory = connections.Where(c => c.IsLocal && c.IsInMemory).ToList();
```

### Remote Databases
```csharp
var remote = connections.Where(c => c.IsRemote && c.IsDatabase).ToList();
```

## Common Patterns

### Create Local Connection
```csharp
var conn = new ConnectionProperties
{
    ConnectionName = "LocalDb",
    IsLocal = true,
    IsFile = true,
    IsDatabase = true,
    DatabaseType = DataSourceType.SqlLite,
    FilePath = path,
    FileName = "app.db"
};
```

### Link to Driver
```csharp
var driver = ConnectionHelper.GetBestMatchingDriver(conn, editor.ConfigEditor);
conn.DriverName = driver.PackageName;
conn.DriverVersion = driver.version;
```
