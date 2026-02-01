---
name: idatasource
description: Comprehensive guide for implementing IDataSource interface in BeepDM. Use when creating new datasource implementations, understanding IDataSource patterns, or working with datasource-agnostic code.
---

# IDataSource Implementation Guide

Expert guidance for implementing the `IDataSource` interface, the core abstraction for all datasource operations in BeepDM. This skill covers implementation patterns, best practices, and examples based on `RDBSource` and `SQLiteDataSource`.

## Overview

`IDataSource` is the primary interface that all datasource implementations must implement. It provides a unified API for CRUD operations, schema management, transactions, and query execution across 287+ datasource types.

**Location**: `DataManagementModelsStandard/IDataSource.cs`

## Interface Structure

### Core Properties

```csharp
string GuidID { get; set; }                    // Unique identifier
string DatasourceName { get; set; }            // Name of the datasource
DataSourceType DatasourceType { get; set; }    // Type (SqlServer, MySQL, MongoDB, etc.)
DatasourceCategory Category { get; set; }      // Category (RDBMS, NoSQL, File, etc.)
IDataConnection Dataconnection { get; set; }   // Connection object
IErrorsInfo ErrorObject { get; set; }          // Error handling
IDMLogger Logger { get; set; }                 // Logging
List<string> EntitiesNames { get; set; }       // List of entity names
List<EntityStructure> Entities { get; set; }   // List of entity structures
IDMEEditor DMEEditor { get; set; }             // Reference to editor
ConnectionState ConnectionStatus { get; set; }  // Current connection state
string ColumnDelimiter { get; set; }            // Column delimiter (default: "\"")
string ParameterDelimiter { get; set; }         // Parameter delimiter (default: "@")
string Id { get; set; }                         // Secondary identifier
event EventHandler<PassedArgs> PassEvent;      // Event for passing arguments
```

### Required Methods

#### Connection Management
```csharp
ConnectionState Openconnection();              // Open connection to datasource
ConnectionState Closeconnection();              // Close connection
```

#### CRUD Operations
```csharp
// Synchronous
IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter);
PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize);
IErrorsInfo InsertEntity(string EntityName, object InsertedData);
IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow);
IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress);
IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow);

// Asynchronous
Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter);
Task<double> GetScalarAsync(string query);
double GetScalar(string query);
```

#### Schema/Metadata Operations
```csharp
bool CheckEntityExist(string EntityName);                           // Check if entity exists
bool CreateEntityAs(EntityStructure entity);                      // Create entity from structure
IErrorsInfo CreateEntities(List<EntityStructure> entities);       // Create multiple entities
IEnumerable<string> GetEntitesList();                              // Get list of all entities
EntityStructure GetEntityStructure(string EntityName, bool refresh);
EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false);
Type GetEntityType(string EntityName);                             // Get .NET type for entity
int GetEntityIdx(string entityName);                              // Get index of entity in list
IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters);
IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName);
```

#### Transaction Management
```csharp
IErrorsInfo BeginTransaction(PassedArgs args);   // Begin transaction
IErrorsInfo Commit(PassedArgs args);             // Commit transaction
IErrorsInfo EndTransaction(PassedArgs args);      // End transaction
```

#### Script Execution
```csharp
IEnumerable<object> RunQuery(string qrystr);                      // Execute query
IErrorsInfo ExecuteSql(string sql);                              // Execute SQL command
IErrorsInfo RunScript(ETLScriptDet dDLScripts);                   // Execute script
IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null);  // Generate creation scripts
```

## Implementation Pattern

### Step 1: Basic Class Structure

```csharp
[AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.MyCustomDB)]
public class MyCustomDataSource : IDataSource
{
    // Required properties
    public string GuidID { get; set; } = Guid.NewGuid().ToString();
    public string DatasourceName { get; set; }
    public DataSourceType DatasourceType { get; set; }
    public DatasourceCategory Category { get; set; }
    public IDataConnection Dataconnection { get; set; }
    public IErrorsInfo ErrorObject { get; set; }
    public IDMLogger Logger { get; set; }
    public List<string> EntitiesNames { get; set; } = new();
    public List<EntityStructure> Entities { get; set; } = new();
    public IDMEEditor DMEEditor { get; set; }
    public ConnectionState ConnectionStatus { get; set; }
    public string ColumnDelimiter { get; set; } = "\"";
    public string ParameterDelimiter { get; set; } = "@";
    public string Id { get; set; }
    public event EventHandler<PassedArgs> PassEvent;

    // Constructor
    public MyCustomDataSource(string name, IDMLogger logger, IDMEEditor editor, DataSourceType type, IErrorsInfo errors)
    {
        DatasourceName = name;
        Logger = logger;
        DMEEditor = editor;
        DatasourceType = type;
        ErrorObject = errors;
        Dataconnection = new MyDataConnection(editor) { Logger = logger, ErrorObject = errors };
    }

    // Implement all required methods...
}
```

### Step 2: Connection Management

```csharp
public ConnectionState Openconnection()
{
    try
    {
        if (ConnectionStatus == ConnectionState.Open)
        {
            return ConnectionState.Open;
        }

        // Use IDataConnection to open connection
        ConnectionStatus = Dataconnection.OpenConnection();
        
        if (ConnectionStatus == ConnectionState.Open)
        {
            // Load entity list
            EntitiesNames = GetEntitesList().ToList();
            DMEEditor.AddLogMessage("Success", $"Opened connection to {DatasourceName}", DateTime.Now, 0, null, Errors.Ok);
        }
        
        return ConnectionStatus;
    }
    catch (Exception ex)
    {
        ErrorObject.Flag = Errors.Failed;
        ErrorObject.Message = ex.Message;
        ErrorObject.Ex = ex;
        ConnectionStatus = ConnectionState.Broken;
        DMEEditor.AddLogMessage("Fail", $"Failed to open connection: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
        return ConnectionStatus;
    }
}

public ConnectionState Closeconnection()
{
    try
    {
        if (Dataconnection != null)
        {
            ConnectionStatus = Dataconnection.CloseConnection();
            DMEEditor.AddLogMessage("Success", $"Closed connection to {DatasourceName}", DateTime.Now, 0, null, Errors.Ok);
        }
        return ConnectionStatus;
    }
    catch (Exception ex)
    {
        ErrorObject.Flag = Errors.Failed;
        ErrorObject.Message = ex.Message;
        ErrorObject.Ex = ex;
        DMEEditor.AddLogMessage("Fail", $"Failed to close connection: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
        return ConnectionStatus;
    }
}
```

### Step 3: Entity Creation (CreateEntityAs)

**Critical**: Always use `IDataSourceHelper` for SQL generation to maintain datasource-agnostic design.

```csharp
public bool CreateEntityAs(EntityStructure entity)
{
    ErrorObject.Flag = Errors.Ok;
    
    try
    {
        // 1. Validate entity using helper
        var helper = DMEEditor.GetDataSourceHelper(DatasourceType);
        var (valid, msg) = helper.ValidateEntity(entity);
        if (!valid)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"Validation failed: {msg}";
            return false;
        }

        // 2. Check if entity already exists
        if (CheckEntityExist(entity.EntityName))
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = $"Entity {entity.EntityName} already exists";
            return false;
        }

        // 3. Use IDataSourceHelper to generate CREATE TABLE SQL
        var (createSql, success, error) = helper.GenerateCreateTableSql(
            entity.SchemaName ?? "dbo",
            entity.EntityName,
            entity.EntityFields
        );

        if (!success)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"Failed to generate SQL: {error}";
            return false;
        }

        // 4. Execute SQL
        var result = ExecuteSql(createSql);
        if (result.Flag == Errors.Ok)
        {
            // Add to collections
            EntitiesNames.Add(entity.EntityName);
            Entities.Add(entity);
            
            DMEEditor.AddLogMessage("Success", $"Created entity {entity.EntityName}", DateTime.Now, 0, null, Errors.Ok);
            return true;
        }
        
        return false;
    }
    catch (Exception ex)
    {
        ErrorObject.Flag = Errors.Failed;
        ErrorObject.Message = ex.Message;
        ErrorObject.Ex = ex;
        DMEEditor.AddLogMessage("Fail", $"Failed to create entity: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
        return false;
    }
}
```

### Step 4: CRUD Operations

#### GetEntity (Query)
```csharp
public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
{
    ErrorObject.Flag = Errors.Ok;
    
    try
    {
        // 1. Get entity structure
        var entity = GetEntityStructure(EntityName, refresh: false);
        if (entity == null)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"Entity {EntityName} not found";
            return Enumerable.Empty<object>();
        }

        // 2. Build query using helper
        var helper = DMEEditor.GetDataSourceHelper(DatasourceType);
        var (selectSql, success, error) = helper.GenerateSelectSql(
            entity.SchemaName ?? "dbo",
            entity.EntityName,
            filter,
            entity.EntityFields
        );

        if (!success)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"Failed to generate SELECT SQL: {error}";
            return Enumerable.Empty<object>();
        }

        // 3. Execute query
        return RunQuery(selectSql);
    }
    catch (Exception ex)
    {
        ErrorObject.Flag = Errors.Failed;
        ErrorObject.Message = ex.Message;
        ErrorObject.Ex = ex;
        return Enumerable.Empty<object>();
    }
}
```

#### InsertEntity
```csharp
public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
{
    ErrorObject.Flag = Errors.Ok;
    
    try
    {
        // 1. Get entity structure
        var entity = GetEntityStructure(EntityName, refresh: false);
        if (entity == null)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"Entity {EntityName} not found";
            return ErrorObject;
        }

        // 2. Convert object to dictionary
        var dataDict = ConvertToDictionary(InsertedData);

        // 3. Generate INSERT SQL using helper
        var helper = DMEEditor.GetDataSourceHelper(DatasourceType);
        var (insertSql, success, error) = helper.GenerateInsertSql(
            entity.SchemaName ?? "dbo",
            entity.EntityName,
            dataDict
        );

        if (!success)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"Failed to generate INSERT SQL: {error}";
            return ErrorObject;
        }

        // 4. Execute SQL
        return ExecuteSql(insertSql);
    }
    catch (Exception ex)
    {
        ErrorObject.Flag = Errors.Failed;
        ErrorObject.Message = ex.Message;
        ErrorObject.Ex = ex;
        return ErrorObject;
    }
}
```

### Step 5: Schema Discovery

```csharp
public IEnumerable<string> GetEntitesList()
{
    ErrorObject.Flag = Errors.Ok;
    var entities = new List<string>();
    
    try
    {
        // Use helper to get schema query
        var helper = DMEEditor.GetDataSourceHelper(DatasourceType);
        var (schemaQuery, success, error) = helper.GetSchemaQuery(
            Dataconnection.ConnectionProp?.SchemaName,
            DatasourceType
        );

        if (!success)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"Failed to get schema query: {error}";
            return entities;
        }

        // Execute query and extract entity names
        var results = RunQuery(schemaQuery);
        foreach (var row in results)
        {
            // Extract entity name based on datasource-specific result structure
            var entityName = ExtractEntityName(row);
            if (!string.IsNullOrEmpty(entityName))
            {
                entities.Add(entityName);
            }
        }

        return entities;
    }
    catch (Exception ex)
    {
        ErrorObject.Flag = Errors.Failed;
        ErrorObject.Message = ex.Message;
        ErrorObject.Ex = ex;
        return entities;
    }
}
```

## Implementation Examples

### RDBSource Pattern (Partial Class Architecture)

`RDBSource` uses a partial class architecture with specialized partial classes:

- **RDBSource.cs**: Core class definition and constructor
- **RDBSource.Connection.cs**: Connection management
- **RDBSource.CRUD.cs**: CRUD operations
- **RDBSource.Schema.cs**: Schema operations
- **RDBSource.Transaction.cs**: Transaction management
- **RDBSource.Query.cs**: Query execution
- **RDBSource.DMLGeneration.cs**: DML SQL generation
- **RDBSource.Pagination.cs**: Pagination support
- **RDBSource.BulkOperations.cs**: Bulk operations
- **RDBSource.Cache.cs**: Entity structure caching
- **RDBSource.TypeMapping.cs**: Type mapping
- **RDBSource.Utilities.cs**: Utility methods

**Benefits**:
- Better code organization
- Easier maintenance
- Clear separation of concerns
- Supports large implementations

### SQLiteDataSource Pattern (Inheritance)

`SQLiteDataSource` inherits from `InMemoryRDBSource` which inherits from `RDBSource`:

```csharp
[AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.SqlLite)]
public class SQLiteDataSource : InMemoryRDBSource, ILocalDB, IDataSource, IDisposable
{
    // SQLite-specific implementation
    // Overrides base methods for SQLite-specific behavior
}
```

**Benefits**:
- Code reuse through inheritance
- Specialized behavior for specific datasource types
- Supports multiple interfaces (IInMemoryDB, ILocalDB)

## Critical Rules

1. **Always use `[AddinAttribute]`** - Required for AssemblyHandler discovery
2. **Implement ALL methods** - No partial implementations allowed
3. **Use IDataSourceHelper for SQL** - Never hard-code SQL dialects
4. **Populate ErrorObject** - Don't throw exceptions for expected errors
5. **Check capabilities** - Use `SupportsCapability()` before operations
6. **Validate before DDL** - Always call `ValidateEntity()` before `CreateEntityAs()`
7. **Handle connection state** - Check `ConnectionStatus` before operations
8. **Use proper delimiters** - Set `ColumnDelimiter` and `ParameterDelimiter` correctly

## Best Practices

### Error Handling
```csharp
// Always populate ErrorObject instead of throwing
ErrorObject.Flag = Errors.Ok;
try
{
    // Operation
    ErrorObject.Flag = Errors.Ok;
}
catch (Exception ex)
{
    ErrorObject.Flag = Errors.Failed;
    ErrorObject.Message = ex.Message;
    ErrorObject.Ex = ex;
    DMEEditor.AddLogMessage("Fail", ex.Message, DateTime.Now, 0, null, Errors.Failed);
}
```

### Connection Management
```csharp
// Always check connection state
if (ConnectionStatus != ConnectionState.Open)
{
    var state = Openconnection();
    if (state != ConnectionState.Open)
    {
        ErrorObject.Flag = Errors.Failed;
        ErrorObject.Message = "Connection not available";
        return;
    }
}
```

### Entity Structure Caching
```csharp
// Cache entity structures to avoid repeated database queries
private Dictionary<string, EntityStructure> _entityCache = new();

public EntityStructure GetEntityStructure(string EntityName, bool refresh)
{
    if (!refresh && _entityCache.ContainsKey(EntityName))
    {
        return _entityCache[EntityName];
    }
    
    // Load from database
    var entity = LoadEntityStructureFromDatabase(EntityName);
    _entityCache[EntityName] = entity;
    return entity;
}
```

### Transaction Support
```csharp
// Check if datasource supports transactions
var helper = DMEEditor.GetDataSourceHelper(DatasourceType);
if (helper.Capabilities.SupportsTransactions)
{
    BeginTransaction(new PassedArgs());
    try
    {
        // Operations
        Commit(new PassedArgs());
    }
    catch
    {
        EndTransaction(new PassedArgs());
    }
}
```

## Related Interfaces

- **IRDBSource**: Extends IDataSource for RDBMS-specific operations
- **IInMemoryDB**: For in-memory database implementations (see **@inmemorydb** skill)
- **ILocalDB**: For local file-based database implementations (see **@localdb** skill)
- **IDataConnection**: Connection management interface
- **IDataSourceHelper**: SQL generation and validation helpers

## Related Skills

- **@beepdm** - Main BeepDM skill with IDataSource overview
- **@inmemorydb** - Guide for implementing IInMemoryDB
- **@localdb** - Guide for implementing ILocalDB
- **@connection** - Connection management patterns

## File Locations

- **IDataSource Interface**: `DataManagementModelsStandard/IDataSource.cs`
- **RDBSource Example**: `BeepDataSources/DataSourcesPlugins/RDBMSDataSource/PartialClasses/RDBSource/`
- **SQLiteDataSource Example**: `BeepDataSources/DataSourcesPluginsCore/SqliteDatasourceCore/SQLiteDataSource.cs`
- **InMemoryRDBSource Example**: `BeepDataSources/DataSourcesPlugins/RDBMSDataSource/InMemoryRDBSource.cs`
