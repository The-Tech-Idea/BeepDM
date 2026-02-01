# BeepDM Reference Guide

Quick reference for common operations and patterns in BeepDM.

## Core Interface Methods

### IDMEEditor
```csharp
// Get datasource
var ds = dmeEditor.GetDataSource("ConnectionName");

// Create new connection
var connProps = new ConnectionProperties { /* ... */ };
dmeEditor.ConfigEditor.AddDataConnection(connProps);

// Get helper
var helper = dmeEditor.GetDataSourceHelper(DataSourceType.SqlServer);

// Open datasource
dmeEditor.OpenDataSource("ConnectionName");
```

### IDataSource
```csharp
// Connection
ds.Openconnection();
ds.Closeconnection();

// CRUD
var data = ds.GetEntity("TableName", filters);
var asyncData = await ds.GetEntityAsync("TableName", filters);
var result = ds.InsertEntity("TableName", data);
var updateResult = ds.UpdateEntities("TableName", data, progress);
var deleteResult = ds.DeleteEntity("TableName", filters);

// Metadata
var structure = ds.GetEntityStructure("TableName");
var entities = ds.GetEntitesList();
var exists = ds.CheckEntityExist("TableName");

// Transactions
ds.BeginTransaction();
ds.Commit();
ds.EndTransaction();
```

### IDataSourceHelper - DDL Operations
```csharp
// Table operations
var (sql, success, error) = helper.GenerateCreateTableSql(schema, table, fields);
var (sql, success, error) = helper.GenerateDropTableSql(schema, table);
var (sql, success, error) = helper.GenerateTruncateTableSql(schema, table);
var (sql, success, error) = helper.GenerateRenameTableSql(oldName, newName);

// Column operations
var (sql, success, error) = helper.GenerateAddColumnSql(table, field);
var (sql, success, error) = helper.GenerateAlterColumnSql(table, field);
var (sql, success, error) = helper.GenerateDropColumnSql(table, columnName);
var (sql, success, error) = helper.GenerateRenameColumnSql(table, oldName, newName);

// Constraints
var (sql, success, error) = helper.GenerateAddPrimaryKeySql(table, columns);
var (sql, success, error) = helper.GenerateAddForeignKeySql(table, childColumns, parentTable, parentColumns);
var (sql, success, error) = helper.GenerateAddConstraintSql(table, constraintName, constraintDefinition);
```

### IDataSourceHelper - DML Operations
```csharp
// CRUD SQL generation
var (sql, success, error) = helper.GenerateInsertSql(table, record);
var (sql, success, error) = helper.GenerateUpdateSql(table, record, whereClause);
var (sql, success, error) = helper.GenerateDeleteSql(table, whereClause);
var (sql, success, error) = helper.GenerateSelectSql(table, columns, whereClause);

// Utilities
var quoted = helper.QuoteIdentifier(identifier);
var (valid, msg) = helper.ValidateEntity(entityStructure);
var (supported, desc, _) = helper.SupportsCapability(CapabilityType.SupportsTransactions);
```

### IDataSourceHelper - Schema Queries
```csharp
// Schema discovery
var (query, success, error) = helper.GetSchemaQuery(entityName, dataSourceType);
var (query, success, error) = helper.GetTableExistsQuery(tableName, schema);
var (query, success, error) = helper.GetColumnInfoQuery(tableName, schema);
var (query, success, error) = helper.GetPrimaryKeyQuery(tableName);
var (query, success, error) = helper.GetForeignKeysQuery(tableName);
var (query, success, error) = helper.GetConstraintsQuery(tableName);
```

### IDataSourceHelper - Transactions
```csharp
// Transaction control
var (sql, success, error) = helper.GenerateBeginTransactionSql();
var (sql, success, error) = helper.GenerateCommitSql();
var (sql, success, error) = helper.GenerateRollbackSql();
```

## Data Type Mapping

```csharp
// CLR to datasource type
var datasourceType = helper.MapClrTypeToDatasourceType(typeof(string), 255);

// Datasource to CLR type
var clrType = helper.MapDatasourceTypeToClrType("VARCHAR(255)");

// Get limits
var maxString = helper.GetMaxStringSize();
var maxPrecision = helper.GetMaxNumericPrecision();
```

## Capability Checking

```csharp
// Check specific capability
var (desc, supported, _) = helper.SupportsCapability(CapabilityType.SupportsTransactions);

// Check via Capabilities property
if (helper.Capabilities.SupportsTransactions) { /* ... */ }
if (helper.Capabilities.SupportsConstraints) { /* ... */ }
if (helper.Capabilities.SupportsAlteringSchema) { /* ... */ }
if (helper.Capabilities.SupportsIndexing) { /* ... */ }
if (helper.Capabilities.CreateSchema) { /* ... */ }
```

## Common Patterns

### Pattern: Safe SQL Execution
```csharp
var (sql, success, error) = helper.GenerateCreateTableSql(...);
if (success)
{
    dataSource.ExecuteSql(sql);
}
else
{
    Logger.WriteLog($"Failed: {error}");
    ErrorObject.Flag = Errors.Failed;
    ErrorObject.Message = error;
}
```

### Pattern: Capability-Aware Operation
```csharp
if (helper.Capabilities.SupportsAlteringSchema)
{
    var (sql, success, _) = helper.GenerateAddColumnSql(table, field);
    if (success) ExecuteSql(sql);
}
else
{
    Logger.WriteLog("Schema alteration not supported");
}
```

### Pattern: Transaction Wrapper
```csharp
if (helper.Capabilities.SupportsTransactions)
{
    ExecuteSql(helper.GenerateBeginTransactionSql().Sql);
    try
    {
        // Operations
        ExecuteSql(helper.GenerateCommitSql().Sql);
    }
    catch
    {
        ExecuteSql(helper.GenerateRollbackSql().Sql);
    }
}
```

## Error Handling

```csharp
IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
try
{
    // Operation
    retval.Flag = Errors.Ok;
    retval.Message = "Success";
}
catch (Exception ex)
{
    retval.Flag = Errors.Failed;
    retval.Message = ex.Message;
    retval.Ex = ex;
}
return retval;
```

## Configuration Files

### DataConnections.json
```json
{
  "ConnectionName": "MyDB",
  "ConnectionString": "Server=...;Database=...",
  "DatabaseType": "SqlServer",
  "DriverName": "MSSQL",
  "Category": "RDBMS"
}
```

### ConnectionConfig.json
```json
{
  "PackageName": "MSSQL",
  "DatasourceType": "SqlServer",
  "ClassHandler": "SQLServerDataSource",
  "version": "1.0.0",
  "ConnectionString": "Server={Host};Database={Database};..."
}
```

## Defaults Management

### DefaultsManager - Initialization
```csharp
// Initialize once at startup
DefaultsManager.Initialize(dmeEditor);

// Access helpers
var helper = DefaultsManager.DefaultValueHelper;
var resolverManager = DefaultsManager.ResolverManager;
var validator = DefaultsManager.ValidationHelper;
```

### DefaultsManager - Setting Defaults
```csharp
// Static value
DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "Status", "Active");

// Dynamic rule
DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "CreatedDate", "NOW", isRule: true);
DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "CreatedBy", "USERNAME", isRule: true);
DefaultsManager.SetColumnDefault(editor, "MyDatabase", "Users", "UserID", "NEWGUID", isRule: true);

// Bulk operation
var columnDefaults = new Dictionary<string, (string value, bool isRule)>
{
    { "CreatedBy", ("USERNAME", true) },
    { "CreatedDate", ("NOW", true) },
    { "Status", ("Active", false) }
};
DefaultsManager.SetMultipleColumnDefaults(editor, "MyDatabase", "Orders", columnDefaults);
```

### DefaultsManager - Getting Defaults
```csharp
// Get column default
var defaultValue = DefaultsManager.GetColumnDefault(editor, "MyDatabase", "Users", "CreatedDate");

// Get all entity defaults
var entityDefaults = DefaultsManager.GetEntityDefaults(editor, "MyDatabase", "Users");

// Apply defaults to record
var user = new User();
user = DefaultsManager.ApplyDefaultsToRecord(editor, "MyDatabase", "Users", user) as User;
```

### Built-in Rule Types
```csharp
// DateTime: NOW, TODAY, YESTERDAY, TOMORROW, ADDDAYS(TODAY, 7), FORMAT(NOW, 'yyyy-MM-dd')
// User: USERNAME, USERID, USEREMAIL, CURRENTUSER
// System: MACHINENAME, HOSTNAME, VERSION, APPVERSION
// GUID: NEWGUID, GUID, UUID, GUID(N), GUID(D)
// Formula: SEQUENCE(1000), INCREMENT(FieldName), RANDOM(1, 100), CALCULATE(field1 + field2)
```

### DefaultsManager - Custom Resolvers
```csharp
// Create custom resolver
public class CustomResolver : BaseDefaultValueResolver
{
    public override string ResolverName => "BusinessLogic";
    public override IEnumerable<string> SupportedRuleTypes => new[] { "NEXTORDERID" };
    public override object ResolveValue(string rule, IPassedArgs parameters) { /* ... */ }
    public override bool CanHandle(string rule) { /* ... */ }
}

// Register resolver
DefaultsManager.RegisterCustomResolver(editor, new CustomResolver(editor));
```

### DefaultsManager - Validation & Testing
```csharp
// Test rule
var (validation, testValue) = DefaultsManager.TestRule(editor, "NOW");

// Validate default value
var validationResult = DefaultsManager.ValidateDefaultValue(editor, defaultValue);

// Get available resolvers
var resolvers = DefaultsManager.GetAvailableResolvers(editor);
var examples = DefaultsManager.GetResolverExamples(editor);
```

### DefaultsManager - Templates & Import/Export
```csharp
// Create templates
var auditFields = DefaultsManager.CreateDefaultValueTemplate(editor, DefaultValueTemplateType.AuditFields);
var systemFields = DefaultsManager.CreateDefaultValueTemplate(editor, DefaultValueTemplateType.SystemFields);

// Export/Import
var exported = DefaultsManager.ExportDefaults(editor, "MyDatabase");
var importResult = DefaultsManager.ImportDefaults(editor, "AnotherDatabase", exported, replaceExisting: false);
```

## Migration Management

### MigrationManager - Entity Discovery
```csharp
// Discover Entity types
var entityTypes = migrationManager.DiscoverEntityTypes("MyApp.Entities");
var allTypes = migrationManager.DiscoverAllEntityTypes();

// Get migration summary
var summary = migrationManager.GetMigrationSummary("MyApp.Entities");
// summary.EntitiesToCreate - entities not in database
// summary.EntitiesToUpdate - entities with missing columns
// summary.EntitiesUpToDate - entities matching Entity classes
// summary.HasPendingMigrations - true if migrations needed
```

### MigrationManager - Database Operations
```csharp
// Ensure database created (like EF's EnsureCreated)
var result = migrationManager.EnsureDatabaseCreated("MyApp.Entities");

// Apply migrations (like EF's Migrate)
var result = migrationManager.ApplyMigrations(
    namespaceName: "MyApp.Entities",
    detectRelationships: true,
    addMissingColumns: true,
    progress: progress);
```

### Entity Class Requirements
- Must inherit from `Entity` or implement `IEntity`
- Non-abstract, non-interface, non-generic
- Optional `[Table]` attribute for custom table names

### Migration Workflow
```csharp
// 1. Initialize
var migrationManager = new MigrationManager(dmeEditor, dataSource);
migrationManager.MigrateDataSource = dataSource;

// 2. Get summary
var summary = migrationManager.GetMigrationSummary("MyApp.Entities");

// 3. Apply migrations
if (summary.HasPendingMigrations)
{
    var result = migrationManager.ApplyMigrations("MyApp.Entities");
}
```

## Supported Data Source Types

### RDBMS (via RdbmsHelper)
- SQL Server, MySQL, PostgreSQL, Oracle, SQLite, DB2, Hana, and 60+ more

### NoSQL (via specialized helpers)
- MongoDB, Redis, Cassandra, Elasticsearch

### APIs (via RestApiHelper)
- REST APIs, Web Services

### Planned
- File formats (CSV, JSON, XML, Parquet, Excel)
- Streaming (Kafka, RabbitMQ, Kinesis)
- Graph databases (Neo4j, ArangoDB)
- Vector databases (Pinecone, ChromaDB)
- Time series (InfluxDB, TimescaleDB)
