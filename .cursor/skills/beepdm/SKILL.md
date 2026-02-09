---
name: beepdm
description: Provides expert guidance for Beep Data Management Engine (BeepDM) development, including IDataSource implementations, IDataSourceHelper usage, data source creation, schema operations, and integration patterns. Use when working with BeepDM, creating new data sources, implementing IDataSourceHelper methods, or integrating with the BeepDM framework.
---

# BeepDM Development Guide

Expert guidance for developing with the Beep Data Management Engine (BeepDM), a modular framework with broad datasource support through IDataSource plugins.

## Core Architecture

### IDMEEditor (Mother Class)
Central orchestrator for all data management operations:
- **Location**: `DataManagementEngineStandard/Editor/DM/DMEEditor.cs`
- **Key Methods**: `GetDataSource()`, `CreateNewDataSourceConnection()`, `OpenDataSource()`, `GetDataSourceHelper()`
- **Responsibilities**: Manage datasource lifecycle, access ConfigEditor, provide ETL/typesHelper/classCreator services

### IDataSource Interface
Contract that all data sources must implement (40+ methods). This is the core abstraction for all datasource operations in BeepDM.

**Location**: `DataManagementModelsStandard/IDataSource.cs`

#### Properties
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

#### Connection Methods
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

#### Implementation Pattern
All IDataSource implementations must:
1. Use `[AddinAttribute]` for discovery
2. Implement all methods (no partial implementations)
3. Populate `ErrorObject` on failures (don't throw exceptions)
4. Use `IDataSourceHelper` for SQL generation (datasource-agnostic)
5. Handle connection state properly

### IDataSourceHelper Interface
Factory-managed helpers for datasource-specific query generation (24 methods):
- **DDL**: `GenerateCreateTableSql()`, `GenerateDropTableSql()`, `GenerateAddColumnSql()`, `GenerateAlterColumnSql()`
- **DML**: `GenerateInsertSql()`, `GenerateUpdateSql()`, `GenerateDeleteSql()`, `GenerateSelectSql()`
- **Schema**: `GetSchemaQuery()`, `GetTableExistsQuery()`, `GetColumnInfoQuery()`
- **Constraints**: `GenerateAddPrimaryKeySql()`, `GenerateAddForeignKeySql()`, `GetPrimaryKeyQuery()`
- **Transactions**: `GenerateBeginTransactionSql()`, `GenerateCommitSql()`, `GenerateRollbackSql()`
- **Utilities**: `QuoteIdentifier()`, `SupportsCapability()`, `ValidateEntity()`

**Current Implementations**: `RdbmsHelper`, `MongoDBHelper`, `RedisHelper`, `CassandraHelper`, `RestApiHelper`

### ConfigEditor (IConfigEditor)
Centralized configuration management system for BeepDM. Manages connections, drivers, queries, mappings, workflows, and all application configuration.

**Location**: `DataManagementEngineStandard/ConfigUtil/ConfigEditor.cs`

#### Architecture
ConfigEditor uses a **manager-based architecture** with specialized managers for different responsibilities:
- **ConfigPathManager**: Path and directory management
- **DataConnectionManager**: Connection properties CRUD operations
- **QueryManager**: SQL query template management
- **EntityMappingManager**: Entity structure and mapping persistence
- **ComponentConfigManager**: Components, drivers, workflows, reports
- **MigrationHistoryManager**: Migration history tracking

#### Key Properties
```csharp
BeepConfigType ConfigType { get; set; }                    // Application, DataConnector, etc.
ConfigandSettings Config { get; set; }                     // Main configuration object
string ConfigPath { get; set; }                            // Path to config directory
string ExePath { get; set; }                                // Executable path
string ContainerName { get; set; }                         // Container folder name
IJsonLoader JsonLoader { get; set; }                        // JSON serialization
IDMLogger Logger { get; set; }                             // Logging
IErrorsInfo ErrorObject { get; set; }                      // Error handling
bool IsLoaded { get; }                                      // Whether config is loaded
```

#### Configuration Collections
```csharp
List<ConnectionProperties> DataConnections { get; set; }           // Data source connections
List<ConnectionDriversConfig> DataDriversClasses { get; set; }     // Driver configurations
List<QuerySqlRepo> QueryList { get; set; }                        // SQL query templates
List<DatatypeMapping> DataTypesMap { get; set; }                  // Type mappings
List<WorkFlow> WorkFlows { get; set; }                            // Workflow definitions
List<CategoryFolder> CategoryFolders { get; set; }                // Category folders
List<EntityStructure> EntityCreateObjects { get; set; }           // Entity structures
List<AssemblyClassDefinition> DataSourcesClasses { get; set; }     // DataSource class definitions
List<ReportsList> Reportslist { get; set; }                       // Reports list
List<RootFolder> Projects { get; set; }                           // Project folders
// ... and many more component collections
```

#### Data Connection Management
```csharp
// CRUD Operations
List<ConnectionProperties> LoadDataConnectionsValues();           // Load from DataConnections.json
void SaveDataconnectionsValues();                                // Save to DataConnections.json
bool AddDataConnection(ConnectionProperties cn);                 // Add new connection
bool UpdateDataConnection(ConnectionProperties source, string targetguidid);
bool RemoveDataConnection(string pname);                        // Remove by name
bool RemoveConnByName(string pname);
bool RemoveConnByID(int ID);
bool RemoveConnByGuidID(string GuidID);
bool DataConnectionExist(string ConnectionName);                 // Check existence
bool DataConnectionExist(ConnectionProperties cn);
bool DataConnectionGuidExist(string GuidID);
```

#### Driver Management
```csharp
List<ConnectionDriversConfig> LoadConnectionDriversConfigValues();  // Load drivers
void SaveConnectionDriversConfigValues();                           // Save drivers
int AddDriver(ConnectionDriversConfig dr);                          // Add driver
```

#### Query Management
```csharp
List<QuerySqlRepo> LoadQueryFile();                                // Load QueryList.json
void SaveQueryFile();                                               // Save QueryList.json
List<QuerySqlRepo> InitQueryDefaultValues();                        // Initialize defaults
string GetSql(Sqlcommandtype CmdType, string TableName, string SchemaName, 
              string Filterparamters, List<QuerySqlRepo> QueryList, DataSourceType DatabaseType);
List<string> GetSqlList(Sqlcommandtype CmdType, string TableName, string SchemaName, 
                         string Filterparamters, List<QuerySqlRepo> QueryList, DataSourceType DatabaseType);
string GetSqlFromCustomQuery(Sqlcommandtype CmdType, string TableName, string customquery, 
                              List<QuerySqlRepo> QueryList, DataSourceType DatabaseType);
```

#### Entity Structure Management
```csharp
// Entity Structure Persistence
List<EntityStructure> LoadTablesEntities();                        // Load entity structures
void SaveTablesEntities();                                          // Save entity structures
DatasourceEntities LoadDataSourceEntitiesValues(string dsname);   // Load for datasource
void SaveDataSourceEntitiesValues(DatasourceEntities datasourceEntities);
bool RemoveDataSourceEntitiesValues(string dsname);
bool EntityStructureExist(string filepath, string EntityName, string DataSourceID);
void SaveEntityStructure(string filepath, EntityStructure entity);
EntityStructure LoadEntityStructure(string filepath, string EntityName, string DataSourceID);
```

#### Mapping Management
```csharp
void SaveMappingValues(string Entityname, string datasource, EntityDataMap mapping_Rep);
EntityDataMap LoadMappingValues(string Entityname, string datasource);
void SaveMappingSchemaValue(string schemaname, Map_Schema mapping_Rep);
Map_Schema LoadMappingSchema(string schemaname);
```

#### Type Mapping
```csharp
List<DatatypeMapping> ReadDataTypeFile(string filename = "DataTypeMapping");  // Load type mappings
void WriteDataTypeFile(string filename = "DataTypeMapping");                  // Save type mappings
```

#### Default Values
```csharp
List<DefaultValue> Getdefaults(IDMEEditor DMEEditor, string DatasourceName);
IErrorsInfo Savedefaults(IDMEEditor DMEEditor, List<DefaultValue> defaults, string DatasourceName);
```

#### Migration History
```csharp
MigrationHistory LoadMigrationHistory(string dataSourceName);
void SaveMigrationHistory(MigrationHistory history);
void AppendMigrationRecord(string dataSourceName, DataSourceType dataSourceType, MigrationRecord record);
```

#### Configuration Files
ConfigEditor manages these JSON files in the `ConfigPath` directory:
- **DataConnections.json**: Connection properties for all datasources
- **ConnectionConfig.json**: Driver configurations
- **QueryList.json**: SQL query templates for metadata discovery
- **DataTypeMapping.json**: Type translation mappings
- **CategoryFolders.json**: Category folder structure
- **Config.json**: Main application configuration
- **WorkFlow/**: Workflow definitions
- **Mapping/**: Entity mapping schemas
- **Entities/**: Entity structure definitions

#### Initialization
```csharp
// Constructor
public ConfigEditor(IDMLogger logger, IErrorsInfo per, IJsonLoader jsonloader, 
                   string folderpath = null, string containerfolder = null, 
                   BeepConfigType configType = BeepConfigType.Application)

// Initialize all configuration
IErrorsInfo Init();  // Loads all configuration files and initializes managers
```

#### Usage Pattern
```csharp
// Access ConfigEditor through IDMEEditor
var configEditor = dmeEditor.ConfigEditor;

// Load connections
var connections = configEditor.LoadDataConnectionsValues();

// Add new connection
var newConnection = new ConnectionProperties { ConnectionName = "MyDB", ... };
configEditor.AddDataConnection(newConnection);
configEditor.SaveDataconnectionsValues();

// Get SQL query template
var sql = configEditor.GetSql(Sqlcommandtype.Select, "Products", "dbo", "", 
                               configEditor.QueryList, DataSourceType.SqlServer);
```

## Creating a New Data Source

### Step 1: Implement IDataSource
```csharp
[AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.MyCustomDB)]
public class MyCustomDataSource : IDataSource
{
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

    public MyCustomDataSource(string name, IDMLogger logger, IDMEEditor editor, DataSourceType type, IErrorsInfo errors)
    {
        DatasourceName = name;
        Logger = logger;
        DMEEditor = editor;
        DatasourceType = type;
        ErrorObject = errors;
        Dataconnection = new MyDataConnection(editor) { Logger = logger, ErrorObject = errors };
    }

    // Implement all required IDataSource methods
    public ConnectionState Openconnection() { /* Implementation */ }
    public ConnectionState Closeconnection() { /* Implementation */ }
    public bool CheckEntityExist(string EntityName) { /* Implementation */ }
    public bool CreateEntityAs(EntityStructure entity) { /* Implementation */ }
    public object GetEntity(string EntityName, List<AppFilter> filter) { /* Implementation */ }
    // ... implement all 40+ methods
}
```

### Step 2: Create IDataSourceHelper (if new datasource type)
```csharp
public class MyCustomHelper : IDataSourceHelper
{
    public DataSourceType SupportedType => DataSourceType.MyCustomDB;
    public DataSourceCapabilities Capabilities { get; set; }

    public (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSql(
        string schemaName, string tableName, List<EntityField> fields, DataSourceType dataSourceType = null)
    {
        // Generate datasource-specific CREATE TABLE SQL
    }

    // Implement all 24 IDataSourceHelper methods
}
```

### Step 3: Register in Configuration
Add to `ConnectionConfig.json`:
```json
{
  "PackageName": "MyCustomDB",
  "DatasourceType": "MyCustomDB",
  "ClassHandler": "MyCustomDataSource",
  "version": "1.0.0",
  "ConnectionString": "Host={Host};Database={Database};...",
  "DbConnectionType": "MyCustomConnection"
}
```

## Using IDataSourceHelper

### Pattern: Capability-Aware Operations
```csharp
var helper = dmeEditor.GetDataSourceHelper(DataSourceType.SqlServer);
if (helper.Capabilities.SupportsTransactions)
{
    var (beginSql, success, error) = helper.GenerateBeginTransactionSql();
    if (success) ExecuteNonQuery(beginSql);
}
```

### Pattern: Schema Operations
```csharp
// Check if table exists
var (existsQuery, success, _) = helper.GetTableExistsQuery("Products", "dbo");
var exists = ExecuteScalar(existsQuery);

// Get column information
var (colQuery, _, _) = helper.GetColumnInfoQuery("Products", "dbo");
var columns = ExecuteDataTable(colQuery);

// Add column
var (addColSql, success, error) = helper.GenerateAddColumnSql(
    "Products",
    new EntityField { FieldName = "DiscountPrice", FieldType = "Decimal", Size = 18 }
);
if (success) ExecuteNonQuery(addColSql);
```

### Pattern: DDL Operations with Validation
```csharp
// Validate entity before creation
var (valid, msg) = helper.ValidateEntity(entityStructure);
if (!valid)
{
    ErrorObject.Flag = Errors.Failed;
    ErrorObject.Message = $"Validation failed: {msg}";
    return false;
}

// Generate CREATE TABLE SQL
var (createSql, success, error) = helper.GenerateCreateTableSql(
    "dbo", "Products", entityStructure.EntityFields
);

if (success)
{
    ExecuteNonQuery(createSql);
}
```

### Pattern: Transaction Management
```csharp
if (helper.Capabilities.SupportsTransactions)
{
    var (beginSql, _, _) = helper.GenerateBeginTransactionSql();
    ExecuteNonQuery(beginSql);

    try
    {
        // Perform operations
        ExecuteNonQuery(insertSql);
        ExecuteNonQuery(updateSql);

        var (commitSql, _, _) = helper.GenerateCommitSql();
        ExecuteNonQuery(commitSql);
    }
    catch
    {
        var (rollbackSql, _, _) = helper.GenerateRollbackSql();
        ExecuteNonQuery(rollbackSql);
    }
}
```

## Common Workflows

### Workflow: Create Entity from POCO
```csharp
// 1. Convert POCO to EntityStructure
var classCreator = new ClassCreator();
var entity = classCreator.CreateEntityStructureFromPoco(typeof(Product));
entity.EntityName = "Products";

// 2. Validate using helper
var helper = dmeEditor.GetDataSourceHelper(DataSourceType.SqlServer);
var (valid, msg) = helper.ValidateEntity(entity);
if (!valid) throw new Exception(msg);

// 3. Generate CREATE TABLE SQL
var (sql, success, error) = helper.GenerateCreateTableSql("dbo", "Products", entity.EntityFields);
if (!success) throw new Exception(error);

// 4. Execute
dataSource.ExecuteSql(sql);
```

### Workflow: Batch Operations with Transactions
```csharp
var helper = dmeEditor.GetDataSourceHelper(dataSource.DatasourceType);
bool useTransaction = helper.Capabilities.SupportsTransactions;

if (useTransaction)
{
    var (beginSql, _, _) = helper.GenerateBeginTransactionSql();
    dataSource.ExecuteSql(beginSql);
}

try
{
    foreach (var record in records)
    {
        var (insertSql, success, _) = helper.GenerateInsertSql("Products", record);
        if (success) dataSource.ExecuteSql(insertSql);
    }

    if (useTransaction)
    {
        var (commitSql, _, _) = helper.GenerateCommitSql();
        dataSource.ExecuteSql(commitSql);
    }
}
catch
{
    if (useTransaction)
    {
        var (rollbackSql, _, _) = helper.GenerateRollbackSql();
        dataSource.ExecuteSql(rollbackSql);
    }
}
```

### Workflow: Schema Discovery
```csharp
var helper = dmeEditor.GetDataSourceHelper(dataSource.DatasourceType);

// Get all tables
var (schemaQuery, success, _) = helper.GetSchemaQuery(null, dataSource.DatasourceType);
var tables = dataSource.ExecuteDataTable(schemaQuery);

foreach (DataRow row in tables.Rows)
{
    var tableName = row["TABLE_NAME"].ToString();
    
    // Get column info
    var (colQuery, _, _) = helper.GetColumnInfoQuery(tableName);
    var columns = dataSource.ExecuteDataTable(colQuery);
    
    // Build EntityStructure
    var entity = new EntityStructure { EntityName = tableName };
    foreach (DataRow colRow in columns.Rows)
    {
        entity.EntityFields.Add(new EntityField
        {
            FieldName = colRow["COLUMN_NAME"].ToString(),
            FieldType = colRow["DATA_TYPE"].ToString(),
            AllowNull = (bool)colRow["IS_NULLABLE"]
        });
    }
}
```

## Key Patterns

### Error Handling Pattern
Always populate `ErrorObject` instead of throwing exceptions for expected failures:
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

### Identifier Quoting Pattern
Always use `QuoteIdentifier()` for safe SQL generation:
```csharp
var quotedTable = helper.QuoteIdentifier(tableName);
var sql = $"SELECT * FROM {quotedTable}";
```

### Type Mapping Pattern
Use helper for consistent type conversion:
```csharp
var clrType = typeof(string);
var datasourceType = helper.MapClrTypeToDatasourceType(clrType, 255);
// Returns datasource-specific type (e.g., "VARCHAR(255)", "NVARCHAR(255)")
```

## Critical Rules

1. **Always use `[AddinAttribute]`** on IDataSource implementations - required for AssemblyHandler discovery
2. **Implement all IDataSource methods** - No partial implementations allowed
3. **Populate ErrorObject on failures** - Don't throw exceptions for expected errors
4. **Use IDataSourceHelper for SQL generation** - Never hard-code SQL dialects
5. **Check capabilities before operations** - Use `SupportsCapability()` or `Capabilities` property
6. **Return tuple pattern** - All IDataSourceHelper methods return `(string Sql, bool Success, string ErrorMessage)`
7. **Validate before DDL** - Always call `ValidateEntity()` before `CreateEntityAs()`

## Documentation References

- **IDataSourceHelper Method Reference**: `Docs/IDataSourceHelper_Method_Reference.md` - Complete API with all 24 methods
- **IDataSourceHelper Usage Patterns**: `Docs/IDataSourceHelper_Usage_Patterns.md` - 10 practical usage patterns
- **CreateEntityAs Guide**: `Docs/CreateEntityAs_Implementation_Guide.md` - Step-by-step implementation for all datasource types
- **How to Create New Data Source**: `Docs/HowToCreateNewDataSource.md` - Minimum steps for new IDataSource implementation
- **Quick Reference**: `Docs/IDataSourceHelper_Quick_Reference.md` - Enhancement summary and capability matrix

## File Locations

- **Core Interfaces**: `DataManagementModelsStandard/Editor/`
- **Engine Implementation**: `DataManagementEngineStandard/Editor/DM/`
- **IDataSourceHelper**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/`
- **RdbmsHelper**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/RdbmsHelper.cs` (5 partial classes)
- **ConfigEditor**: `DataManagementEngineStandard/ConfigUtil/ConfigEditor.cs`
- **Documentation**: `Docs/` directory

## Migration Management (Entity Framework-like)

### MigrationManager Overview
`MigrationManager` provides datasource-agnostic schema migration capabilities similar to Entity Framework Core's migration system. It discovers Entity classes and automatically creates/updates database schema.

**Location**: `DataManagementEngineStandard/Editor/Migration/MigrationManager.cs`

### Key Features

1. **Entity Discovery**: Automatically discovers all classes that inherit from `Entity` or implement `IEntity`
2. **Database Creation**: Similar to EF Core's `Database.EnsureCreated()`
3. **Migration Application**: Similar to EF Core's `Database.Migrate()`, compares Entity classes with database and applies changes
4. **Datasource-Agnostic**: Uses `IDataSource.CreateEntityAs()` for entity creation, compatible with any IDataSource implementation

### Core Methods

#### DiscoverEntityTypes
```csharp
// Discover Entity types in a specific namespace
var entityTypes = migrationManager.DiscoverEntityTypes("MyApp.Entities");

// Discover all Entity types in all assemblies
var allEntityTypes = migrationManager.DiscoverAllEntityTypes();

// Discover in specific assembly
var assemblyEntityTypes = migrationManager.DiscoverEntityTypes("MyApp.Entities", myAssembly);
```

#### EnsureDatabaseCreated
Creates database schema for all discovered Entity types (like EF's `EnsureCreated()`):
```csharp
var migrationManager = new MigrationManager(dmeEditor, dataSource);
migrationManager.MigrateDataSource = dataSource;

// Create all entities in a namespace
var result = migrationManager.EnsureDatabaseCreated("MyApp.Entities", detectRelationships: true);

// Create all entities in all assemblies
var result = migrationManager.EnsureDatabaseCreated();

// With progress reporting
var progress = new Progress<PassedArgs>(args => Console.WriteLine(args.Messege));
var result = migrationManager.EnsureDatabaseCreated("MyApp.Entities", progress: progress);
```

#### ApplyMigrations
Applies migrations comparing Entity classes with current database state (like EF's `Migrate()`):
```csharp
// Apply migrations for namespace
var result = migrationManager.ApplyMigrations("MyApp.Entities", addMissingColumns: true);

// Apply with relationship detection and missing column addition
var result = migrationManager.ApplyMigrations(
    namespaceName: "MyApp.Entities",
    detectRelationships: true,
    addMissingColumns: true,
    progress: progress);
```

#### GetMigrationSummary
Gets a summary of what needs to be migrated:
```csharp
var summary = migrationManager.GetMigrationSummary("MyApp.Entities");

Console.WriteLine($"Entities to create: {summary.EntitiesToCreate.Count}");
// Output: ["Product", "Order", "Customer"]

Console.WriteLine($"Entities to update: {summary.EntitiesToUpdate.Count}");
// Output: ["Product (3 missing column(s))", "Order (1 missing column(s))"]

Console.WriteLine($"Entities up-to-date: {summary.EntitiesUpToDate.Count}");
// Output: ["Category", "Supplier"]

if (summary.HasPendingMigrations)
{
    // Apply migrations
    migrationManager.ApplyMigrations("MyApp.Entities");
}
```

### Entity Class Requirements

Entity classes must:
1. Inherit from `Entity` base class (`TheTechIdea.Beep.Editor.Entity`)
2. OR implement `IEntity` interface
3. Be non-abstract, non-interface, non-generic classes
4. Optionally use `[Table]` attribute for custom table names

**Example Entity Class**:
```csharp
using TheTechIdea.Beep.Editor;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.Entities
{
    [Table("Products")]  // Optional: custom table name
    public class Product : Entity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
```

### Migration Workflow

**Complete Migration Workflow**:
```csharp
// 1. Initialize MigrationManager
var migrationManager = new MigrationManager(dmeEditor, dataSource);
migrationManager.MigrateDataSource = dataSource;

// 2. Get migration summary
var summary = migrationManager.GetMigrationSummary("MyApp.Entities");
Console.WriteLine($"Pending migrations: {summary.TotalPendingMigrations}");

// 3. Apply migrations (creates missing entities, adds missing columns)
var result = migrationManager.ApplyMigrations(
    namespaceName: "MyApp.Entities",
    detectRelationships: true,
    addMissingColumns: true);

if (result.Flag == Errors.Ok)
{
    Console.WriteLine($"Migration successful: {result.Message}");
}
```

**Initial Database Creation**:
```csharp
// For new databases, use EnsureDatabaseCreated
var result = migrationManager.EnsureDatabaseCreated("MyApp.Entities");

// This creates all entities but doesn't add missing columns to existing entities
```

### Design Principles

1. **Datasource-Agnostic Entity Creation**: Uses `IDataSource.CreateEntityAs()` instead of SQL generation
2. **Type Mapping**: Uses `DataTypesHelper` to map .NET types to datasource-specific types
3. **Validation**: Uses `IDataSourceHelper.ValidateEntity()` before creation
4. **Column Operations**: Uses `IDataSourceHelper` for column-level DDL when needed

### Discovery Process

The discovery process:
1. Scans all assemblies in `AppDomain.CurrentDomain`
2. Also scans assemblies from `DMEEditor.assemblyHandler.Assemblies`
3. Filters classes that inherit from `Entity` or implement `IEntity`
4. Supports namespace filtering (with optional sub-namespace inclusion)
5. Handles `ReflectionTypeLoadException` for partially loaded assemblies

### Migration Summary Structure

```csharp
public class MigrationSummary
{
    public List<string> EntitiesToCreate { get; set; }      // Entities not in database
    public List<string> EntitiesToUpdate { get; set; }      // Entities with missing columns
    public List<string> EntitiesUpToDate { get; set; }       // Entities matching Entity classes
    public List<string> Errors { get; set; }                 // Errors during discovery
    public int TotalPendingMigrations { get; }                // Total entities needing migration
    public bool HasPendingMigrations { get; }                 // True if migrations needed
}
```

## Defaults Management

### DefaultsManager Overview
`DefaultsManager` provides a comprehensive system for managing default values for entity columns across all datasources. It supports both static values and dynamic rules with extensible resolvers.

**Location**: `DataManagementEngineStandard/Editor/Defaults/DefaultsManager.cs`

### Key Features

1. **Helper-Based Architecture**: Modular design with separation of concerns
2. **Extensible Resolvers**: Easy to add custom rule resolvers
3. **Entity Column Defaults**: Set defaults at the column level for any entity
4. **Dynamic Rules**: Support for formulas, expressions, and logic
5. **Validation**: Comprehensive validation for rules and configurations
6. **Templates**: Pre-built templates for common scenarios (AuditFields, SystemFields, CommonDefaults)
7. **Import/Export**: Portable configuration management

### Architecture

#### Core Components

- **DefaultsManager**: Main entry point (partial class with static methods)
- **IDefaultValueHelper**: Manages CRUD operations for default values
- **IDefaultValueResolverManager**: Manages rule resolvers
- **IDefaultValueValidationHelper**: Validates configurations and rules
- **Built-in Resolvers**: DateTime, UserContext, SystemInfo, Guid, Formula, DataSource, Environment, Configuration, Expression, ObjectProperty

#### Initialization

```csharp
// Initialize once at application startup
DefaultsManager.Initialize(dmeEditor);

// Access helpers directly if needed
var helper = DefaultsManager.DefaultValueHelper;
var resolverManager = DefaultsManager.ResolverManager;
var validator = DefaultsManager.ValidationHelper;
```

### Basic Usage

#### Setting Column Defaults

```csharp
// Static value
DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "Status", "Active");

// Dynamic rule - current timestamp
DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "CreatedDate", "NOW", isRule: true);

// Dynamic rule - current user
DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "CreatedBy", "USERNAME", isRule: true);

// Dynamic rule - auto-generated GUID
DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "UserID", "NEWGUID", isRule: true);
```

#### Getting Column Defaults

```csharp
// Get resolved default value for a column
var defaultValue = DefaultsManager.GetColumnDefault(editor, "MyDatabase", "Users", "CreatedDate");

// Get all defaults for an entity
var entityDefaults = DefaultsManager.GetEntityDefaults(editor, "MyDatabase", "Users");
foreach (var def in entityDefaults)
{
    Console.WriteLine($"{def.Key}: {def.Value.PropertyValue} / Rule: {def.Value.Rule}");
}
```

#### Applying Defaults to Records

```csharp
// Apply defaults to a new record
var user = new User();
user = DefaultsManager.ApplyDefaultsToRecord(editor, "MyDatabase", "Users", user) as User;
```

### Built-in Rule Types

#### DateTime Resolver
```csharp
"NOW"                    // Current date and time
"TODAY"                  // Current date (midnight)
"YESTERDAY"              // Yesterday's date
"TOMORROW"               // Tomorrow's date
"CURRENTDATE"            // Same as TODAY
"CURRENTTIME"            // Current time of day
"ADDDAYS(TODAY, 7)"      // Add 7 days to today
"FORMAT(NOW, 'yyyy-MM-dd')" // Formatted current date
```

#### User Context Resolver
```csharp
"USERNAME"               // Current Windows username
"USERID"                 // Current user identifier
"USEREMAIL"              // Current user email (if available)
"CURRENTUSER"            // Same as USERNAME
```

#### System Info Resolver
```csharp
"MACHINENAME"            // Current machine name
"HOSTNAME"               // Same as machine name
"VERSION"                // .NET Framework version
"APPVERSION"             // Application version
```

#### GUID Resolver
```csharp
"NEWGUID"                // Generate new GUID
"GUID"                   // Same as NEWGUID
"UUID"                   // Same as NEWGUID
"GUID(N)"                // GUID without hyphens
"GUID(D)"                // GUID with hyphens (default)
```

#### Formula Resolver
```csharp
"SEQUENCE(1000)"         // Start sequence from 1000
"INCREMENT(FieldName)"   // Increment based on existing field
"RANDOM(1, 100)"         // Random number between 1 and 100
"CALCULATE(field1 + field2)" // Simple calculation
```

### Advanced Features

#### Bulk Operations

```csharp
// Set multiple defaults at once
var columnDefaults = new Dictionary<string, (string value, bool isRule)>
{
    { "CreatedBy", ("USERNAME", true) },
    { "CreatedDate", ("NOW", true) },
    { "Status", ("Active", false) },
    { "IsDeleted", ("false", false) },
    { "Priority", ("Normal", false) }
};

var result = DefaultsManager.SetMultipleColumnDefaults(editor, "MyDatabase", "Orders", columnDefaults);
```

#### Templates

```csharp
// Create audit field template
var auditFields = DefaultsManager.CreateDefaultValueTemplate(editor, DefaultValueTemplateType.AuditFields);

// Create system field template  
var systemFields = DefaultsManager.CreateDefaultValueTemplate(editor, DefaultValueTemplateType.SystemFields);

// Create common defaults template
var commonDefaults = DefaultsManager.CreateDefaultValueTemplate(editor, DefaultValueTemplateType.CommonDefaults);
```

#### Custom Resolvers

Create custom resolvers by implementing `IDefaultValueResolver`:

```csharp
public class CustomBusinessResolver : BaseDefaultValueResolver
{
    public CustomBusinessResolver(IDMEEditor editor) : base(editor) { }

    public override string ResolverName => "BusinessLogic";

    public override IEnumerable<string> SupportedRuleTypes => new[]
    {
        "NEXTORDERID", "CUSTOMERCODE", "SALESREP"
    };

    public override object ResolveValue(string rule, IPassedArgs parameters)
    {
        return rule.ToUpperInvariant() switch
        {
            "NEXTORDERID" => GetNextOrderId(),
            "CUSTOMERCODE" => GenerateCustomerCode(),
            "SALESREP" => GetDefaultSalesRep(),
            _ => null
        };
    }

    public override bool CanHandle(string rule)
    {
        var upperRule = rule.ToUpperInvariant().Trim();
        return SupportedRuleTypes.Any(type => upperRule.Contains(type));
    }

    public override IEnumerable<string> GetExamples()
    {
        return new[]
        {
            "NEXTORDERID - Get next order ID from sequence",
            "CUSTOMERCODE - Generate customer code",
            "SALESREP - Get default sales representative"
        };
    }
}

// Register the custom resolver
DefaultsManager.RegisterCustomResolver(editor, new CustomBusinessResolver(editor));
```

#### Validation

```csharp
// Validate a rule before using it
var (validation, testValue) = DefaultsManager.TestRule(editor, "NOW");
if (validation.Flag == Errors.Ok)
{
    Console.WriteLine($"Rule resolved to: {testValue}");
}

// Validate a default value configuration
var defaultValue = new DefaultValue { PropertyName = "CreatedDate", Rule = "NOW" };
var validationResult = DefaultsManager.ValidateDefaultValue(editor, defaultValue);
```

#### Import/Export

```csharp
// Export defaults configuration
var exportedConfig = DefaultsManager.ExportDefaults(editor, "MyDatabase");
File.WriteAllText("defaults-backup.json", exportedConfig);

// Import defaults configuration
var importedConfig = File.ReadAllText("defaults-backup.json");
var importResult = DefaultsManager.ImportDefaults(editor, "AnotherDatabase", importedConfig, replaceExisting: false);
```

### Best Practices

1. **Initialization**: Always initialize DefaultsManager at application startup
2. **Rule Validation**: Validate rules during configuration, not at runtime
3. **Error Handling**: Always check operation results
4. **Performance**: Cache resolved values when appropriate for frequently accessed static rules
5. **Custom Resolvers**: Design custom resolvers to be stateless and thread-safe

### Integration with Other Systems

DefaultsManager integrates seamlessly with:
- **UnitOfWork**: Apply defaults when creating new entities
- **ETLEditor**: Set defaults during data import/transformation
- **MigrationManager**: Apply defaults when creating new entities via migrations
- **DataImportManager**: Set defaults during bulk data import operations

## Integration Points

- **BeepDataSources**: External repositories can provide additional IDataSource implementations
- **BeepContainers**: Dependency injection setup (Autofac supported)
- **AssemblyHandler**: Plugin discovery system for loading datasources from DLLs
- **IDataSource**: Core interface for all datasource operations (see IDataSource Interface section above)
- **ConfigEditor**: Centralized configuration management (see ConfigEditor section above)
- **IDataSourceHelper**: Factory-managed helpers for datasource-specific query generation
- **UnitofWork<T>**: Transactional CRUD operations with change tracking (see **@unitofwork** skill)
- **FormsManager**: Master-detail forms simulation (see **@forms** skill)
- **ETLEditor**: Extract, Transform, Load operations (see **@etl** skill)
- **MigrationManager**: Entity Framework-like schema migration system
- **DefaultsManager**: Default value management for entity columns across all datasources
- **ConnectionHelper**: Connection management and driver configuration (see **@connection** skill)
- **DataSyncManager**: Data synchronization between datasources (see **@beepsync** skill)
- **DataImportManager**: Enhanced data import with transformation (see **@importing** skill)
- **MappingManager**: Entity mapping operations (see **@mapping** skill)

## Related Skills - Encapsulated Overview

The BeepDM framework includes specialized components, each with dedicated skills. This section provides quick overviews and links to detailed documentation.

### @connection - Connection Management
**Purpose**: Comprehensive connection management, driver linking, and connection string processing.

**Key Components**:
- **ConnectionHelper**: Facade for connection operations
- **ConnectionDriverLinkingHelper**: Links connections to drivers from ConfigEditor
- **ConnectionStringProcessingHelper**: Processes and manipulates connection strings
- **ConnectionStringValidationHelper**: Validates connection strings
- **ConnectionStringSecurityHelper**: Secures sensitive information

**Common Use Cases**:
- Creating connection properties dynamically
- Linking connections to drivers based on DatabaseType and Category
- Processing and normalizing connection strings
- Validating connection strings before use
- Securing sensitive connection information

**Quick Example**:
```csharp
var props = new ConnectionProperties { DatabaseType = DataSourceType.SqlServer, ... };
var driver = ConnectionHelper.GetBestMatchingDriver(props, configEditor);
props.DriverName = driver.PackageName;
props.DriverVersion = driver.version;
```

**See @connection skill** for complete API reference, examples, and best practices.

### @unitofwork - UnitOfWork Pattern
**Purpose**: Generic repository pattern for transactional CRUD operations with change tracking and DefaultsManager integration.

**Key Features**:
- **Change Tracking**: Automatic tracking of entity changes
- **Transaction Management**: Built-in transaction support
- **Defaults Integration**: Automatic application of default values
- **Event System**: Before/after CRUD events
- **Filtering & Paging**: Built-in query capabilities

**Common Use Cases**:
- Service layer CRUD operations
- Transactional multi-entity operations
- Change tracking for audit trails
- Automatic default value application

**Quick Example**:
```csharp
using var uow = new UnitofWork<Customer>(editor, "MyDatabase", "Customers", "Id");
var customer = await uow.GetByIdAsync(1);
customer.Name = "Updated Name";
await uow.UpdateAsync(customer);
await uow.CommitAsync();
```

**See @unitofwork skill** for complete API reference, transaction patterns, and service layer examples.

### @beepsync - Data Synchronization
**Purpose**: Synchronize data between different datasources using DataSyncManager.

**Key Features**:
- **Full Sync**: Complete data synchronization
- **Incremental Sync**: Only changed records
- **Bidirectional Sync**: Two-way synchronization
- **Field Mapping**: Automatic and manual field mapping
- **Progress Reporting**: Real-time sync progress

**Common Use Cases**:
- Database replication
- Multi-datasource data consistency
- Data migration between systems
- Scheduled synchronization

**Quick Example**:
```csharp
var syncManager = new DataSyncManager(editor);
var schema = new DataSyncSchema { SourceDataSource = "SourceDB", TargetDataSource = "TargetDB", ... };
var result = await syncManager.SyncDataAsync(schema, SyncType.Full);
```

**See @beepsync skill** for complete sync patterns, schema configuration, and advanced scenarios.

### @etl - Extract, Transform, Load
**Purpose**: ETL operations using ETLEditor for database migration and data copying.

**Key Features**:
- **Script-Based ETL**: Create and execute ETL scripts
- **Entity Creation**: Create entities during ETL
- **Data Copying**: Copy data between datasources
- **Transformation**: Custom data transformation
- **Import Integration**: Works with DataImportManager

**Common Use Cases**:
- Database migration
- Data warehouse ETL processes
- Selective entity copying
- Incremental data copy

**Quick Example**:
```csharp
var etlEditor = new ETLEditor(editor);
var script = etlEditor.CreateScript("MigrationScript");
etlEditor.AddEntityCreation(script, entityStructure);
etlEditor.AddDataCopy(script, "SourceDB", "TargetDB", "Products");
var result = await etlEditor.ExecuteScriptAsync(script);
```

**See @etl skill** for complete workflow, script management, and transformation patterns.

### @forms - Oracle Forms-Compatible Forms
**Purpose**: Master-detail form management using FormsManager (UnitofWorksManager).

**Key Features**:
- **Block Registration**: Register master and detail blocks
- **Mode Transitions**: Query, Insert, Update, Delete modes
- **Master-Detail Coordination**: Automatic detail filtering
- **Unsaved Changes**: Track and handle unsaved changes
- **Event System**: Form-level events

**Common Use Cases**:
- Oracle Forms migration
- Master-detail form simulation
- Complex form workflows
- Multi-block form management

**Quick Example**:
```csharp
var formsManager = new FormsManager(editor);
formsManager.RegisterBlock("Orders", new UnitofWork<Order>(...), BlockType.Master);
formsManager.RegisterBlock("OrderItems", new UnitofWork<OrderItem>(...), BlockType.Detail, "Orders");
formsManager.SetMode("Orders", FormMode.Query);
```

**See @forms skill** for complete form patterns, mode management, and CRUD operations.

### @importing - Data Import Operations
**Purpose**: Enhanced data import using DataImportManager with transformation and batch processing.

**Key Features**:
- **Enhanced Configuration**: Custom transformation, field selection/mapping
- **Batch Processing**: Process large datasets efficiently
- **Progress Monitoring**: Real-time import progress
- **Validation**: Built-in validation support
- **Defaults Integration**: Automatic default value application
- **Cancellation**: Support for cancellation, pause, resume

**Common Use Cases**:
- Bulk data import
- CSV/Excel import
- Data transformation during import
- Incremental import
- Multi-entity import

**Quick Example**:
```csharp
var importManager = new DataImportManager(editor);
var config = new DataImportConfiguration 
{ 
    DataSourceName = "MyDB", 
    EntityName = "Products",
    SourceData = dataTable,
    ApplyDefaults = true
};
var result = await importManager.ImportDataAsync(config, progress);
```

**See @importing skill** for complete import patterns, transformation examples, and batch processing.

### @mapping - Entity Mapping Operations
**Purpose**: Entity mapping operations using MappingManager for field mapping and object transformation.

**Key Features**:
- **EntityDataMap**: Field-to-field mapping definitions
- **Automatic Mapping**: Auto-map fields by name/type
- **Manual Mapping**: Custom field mappings
- **Object Transformation**: Transform objects between structures
- **Mapping Persistence**: Save/load mapping configurations
- **ETL Integration**: Works seamlessly with ETLEditor

**Common Use Cases**:
- Field mapping between different entity structures
- Object transformation
- ETL field mapping
- Multi-source data mapping

**Quick Example**:
```csharp
var mappingManager = new MappingManager(editor);
var map = mappingManager.CreateMap("SourceEntity", "TargetEntity");
mappingManager.AddSourceEntity(map, sourceEntityStructure);
mappingManager.MapFields(map, "SourceField", "TargetField");
var transformed = mappingManager.MapObjectToAnother(sourceObject, map);
```

**See @mapping skill** for complete mapping patterns, transformation examples, and ETL integration.

## Skill Reference Quick Links

For detailed guidance on specific topics, refer to these specialized skills:

### Core Implementation Skills
- **@idatasource** - Complete guide for implementing IDataSource interface, CRUD operations, schema management, and datasource-agnostic patterns
- **@inmemorydb** - Guide for implementing IInMemoryDB interface for in-memory database operations, structure management, and data synchronization
- **@localdb** - Guide for implementing ILocalDB interface for local file-based databases, database file management, and entity operations

### Configuration Skills
- **@connectionproperties** - Comprehensive guide for ConnectionProperties class, feature flags (IsLocal, IsRemote, IsInMemory, etc.), filtering patterns, and connection configuration

### Integration Skills
- **@connection** - Connection management, driver linking, connection string processing, validation, and security
- **@unitofwork** - UnitOfWork pattern, CRUD operations, change tracking, transaction management, DefaultsManager integration
- **@beepsync** - Data synchronization between datasources using DataSyncManager
- **@etl** - Extract, Transform, Load operations using ETLEditor for database migration and data copying
- **@forms** - Oracle Forms-compatible form management using FormsManager (UnitofWorksManager)
- **@importing** - Data import operations using DataImportManager with transformation and batch processing
- **@mapping** - Entity mapping operations using MappingManager for field mapping and object transformation



## Repo Documentation Anchors

- DataManagementEngineStandard/README.md
- DataManagementEngineStandard/FOLDER_REFERENCE.md
- DataManagementEngineStandard/ConfigUtil/README.md
- DataManagementEngineStandard/Editor/README.md
- DataManagementEngineStandard/Docs/index.html

