# Core Architecture

## Overview

BeepDM is built around a central orchestrator (`IDMEEditor`) that coordinates data sources, configuration, ETL operations, and UI components. The architecture follows a plugin-based model where data sources and add-ins are discovered and loaded dynamically.

## Core Components

### 1. IDMEEditor (DMEEditor)

The main orchestration entry point for all BeepDM operations.

**Location**: `DataManagementEngineStandard/Editor/DM/DMEEditor.cs`

**Key Responsibilities**:
- Data source lifecycle management (create, open, close)
- Configuration access via `ConfigEditor`
- ETL operations
- Assembly loading and plugin discovery
- Helper resolution by `DataSourceType`

**Core Properties**:
```csharp
public partial class DMEEditor : IDMEEditor, IDisposable
{
    public List<IDataSource> DataSources { get; set; }        // Loaded data sources
    public IETL ETL { get; set; }                               // ETL operations
    public IConfigEditor ConfigEditor { get; set; }             // Configuration
    public IDataTypesHelper typesHelper { get; set; }           // Type mapping
    public IAssemblyHandler assemblyHandler { get; set; }        // Plugin loading
    public IErrorsInfo ErrorObject { get; set; }                // Error handling
    public IDMLogger Logger { get; set; }                       // Logging
}
```

**Typical Workflow**:
```csharp
// 1. Create or resolve DMEEditor via DI
var editor = serviceProvider.GetRequiredService<IDMEEditor>();

// 2. Load configuration
editor.ConfigEditor.LoadDataConnectionsValues();

// 3. Add connection
var props = new ConnectionProperties { /* ... */ };
editor.ConfigEditor.AddDataConnection(props);

// 4. Open data source
var state = editor.OpenDataSource("MyDatabase");
if (state != ConnectionState.Open)
    throw new InvalidOperationException("Failed to open");

// 5. Get data source for operations
var ds = editor.GetDataSource("MyDatabase");
```

### 2. IDataSource

The runtime contract for all data source implementations.

**Location**: `DataManagementModelsStandard/IDataSource.cs`

**Core Interface**:
```csharp
public interface IDataSource : IDisposable
{
    string GuidID { get; set; }
    string DatasourceName { get; set; }
    DataSourceType DatasourceType { get; set; }
    DatasourceCategory Category { get; set; }
    IDataConnection Dataconnection { get; set; }
    IErrorsInfo ErrorObject { get; set; }
    IDMLogger Logger { get; set; }
    List<string> EntitiesNames { get; set; }
    List<EntityStructure> Entities { get; set; }
    IDMEEditor DMEEditor { get; set; }
    ConnectionState ConnectionStatus { get; set; }
    
    // Connection
    ConnectionState Openconnection();
    ConnectionState Closeconnection();
    
    // CRUD
    object GetEntity(string entityName, List<AppFilter> filters);
    Task<object> GetEntityAsync(string entityName, List<AppFilter> filters);
    IErrorsInfo InsertEntity(string entityName, object data);
    IErrorsInfo UpdateEntities(string entityName, object data, IProgress<PassedArgs> progress);
    IErrorsInfo DeleteEntity(string entityName, List<AppFilter> filters);
    
    // Metadata
    List<string> GetEntitesList();
    EntityStructure GetEntityStructure(string entityName);
    bool CheckEntityExist(string entityName);
    bool CreateEntityAs(EntityStructure entity);
    
    // Execution
    object RunQuery(string query);
    IErrorsInfo ExecuteSql(string sql);
    
    // Transactions
    void BeginTransaction();
    void Commit();
    void EndTransaction();
}
```

### 3. IConfigEditor (ConfigEditor)

Persisted configuration manager with specialized sub-managers.

**Location**: `DataManagementEngineStandard/ConfigUtil/ConfigEditor.cs`

**Architecture**:
```
ConfigEditor (Facade)
├── ConfigPathManager          # Config root and folder structure
├── DataConnectionManager      # DataConnections CRUD
├── QueryManager               # QueryList operations
├── EntityMappingManager       # Entity metadata and mappings
├── ComponentConfigManager     # Drivers, workflows, reports
└── MigrationHistoryManager    # Per-datasource migration history
```

**Key Methods**:
```csharp
// Connections
List<ConnectionProperties> LoadDataConnectionsValues();
void AddDataConnection(ConnectionProperties props);
void SaveDataconnectionsValues();

// Queries
List<QueryConfig> InitQueryDefaultValues();
void SaveQueryFile();

// Mappings
void SaveMappingValues(string entityName, string dataSourceName, EntityDataMap mapping);
```

### 4. IDataSourceHelper

Dialect-aware SQL/schema helper for DDL, DML, and capability checks.

**Location**: `DataManagementModelsStandard/Editor/IDataSourceHelper.cs`

**Key Capabilities**:
- Schema operations (CREATE, ALTER, DROP table)
- Constraint operations (PRIMARY KEY, FOREIGN KEY, UNIQUE)
- Transaction control (BEGIN, COMMIT, ROLLBACK)
- DML generation (INSERT, UPDATE, DELETE, SELECT)
- Type mapping (CLR <-> datasource types)
- Capability checking

**Example Usage**:
```csharp
var helper = editor.GetDataSourceHelper(DataSourceType.SqlServer);

// Check capability
if (helper.Capabilities.SupportsTransactions)
{
    // Generate SQL
    var (sql, success, error) = helper.GenerateCreateTableSql(entity);
    if (success)
        ds.ExecuteSql(sql);
}
```

## Data Flow

```
Application
    |
    v
IDMEEditor (DMEEditor)
    |
    +---> IConfigEditor (ConfigEditor)
    |         +---> DataConnectionManager
    |         +---> QueryManager
    |         +---> EntityMappingManager
    |         +---> ComponentConfigManager
    |
    +---> IDataSource (Plugin)
    |         +---> IDataConnection
    |         +---> IDataSourceHelper
    |
    +---> IETL (ETLEditor)
    |         +---> ETLDataCopier
    |         +---> ETLEntityCopyHelper
    |         +---> ETLScriptManager
    |
    +---> IUnitOfWork<T>
    |         +---> ObservableBindingList<T>
    |
    +---> IAssemblyHandler
              +---> Plugin Discovery
              +---> NuGet Package Loading
```

## Plugin Architecture

BeepDM uses a plugin model where data sources and add-ins are discovered dynamically:

1. **Discovery**: `AssemblyHandler` scans assemblies for `[AddinAttribute]` markers
2. **Registration**: Found types are registered in `ConfigEditor.DataDriversClasses`
3. **Instantiation**: `DMEEditor.GetDataSource()` creates instances via reflection
4. **Lifecycle**: Data sources are cached in `DMEEditor.DataSources`

**Example Plugin**:
```csharp
[AddinAttribute(
    Category = DatasourceCategory.RDBMS, 
    DatasourceType = DataSourceType.MyCustomDB)]
public class MyCustomDataSource : IDataSource
{
    // Implementation
}
```

## Error Handling Model

BeepDM uses `IErrorsInfo` for routine failures rather than exceptions:

```csharp
public interface IErrorsInfo
{
    Errors Flag { get; set; }      // Ok, Failed, Warning
    string Message { get; set; }
    Exception Ex { get; set; }
}
```

**Pattern**:
```csharp
IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
try
{
    // Operation
    retval.Flag = Errors.Ok;
}
catch (Exception ex)
{
    retval.Flag = Errors.Failed;
    retval.Message = ex.Message;
    retval.Ex = ex;
}
return retval;
```

## Related Documentation

- [Service Registration](ServiceRegistration.md) - DI setup for Desktop, Web, Blazor
- [Data Source Implementation](HowToCreateNewDataSource.md) - Building custom plugins
- [Unit of Work Pattern](UnitOfWork.md) - Transactional operations
- [Configuration Management](Configuration.md) - ConfigEditor deep dive
- [Assembly Handler](AssemblyHandler.md) - Plugin loading and NuGet
