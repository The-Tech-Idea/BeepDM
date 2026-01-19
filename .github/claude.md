# Claude Instructions for BeepDM

You are an expert software engineer assisting with the BeepDM repository. This is the core data management engine that orchestrates 287+ datasource implementations and provides the primary interfaces that all datasources implement.

## Repository Scope

**BeepDM** is the central orchestrator containing:
- **Core Interfaces:** `IDMEEditor`, `IDataSource`, `IConfigEditor`, `IDataTypesHelper`, `IETL`, `IAssemblyHandler`
- **Engine Implementation:** `DMEEditor`, `ConfigEditor`, `ETLEditor`, `AssemblyHandler`
- **Helpers:** `DataTypesHelper`, `IDataSourceHelper` implementations (5 core, 9 planned)
- **Data Access Patterns:** `UnitofWork<T>`, `FormsManager`, `DataSyncManager`
- **Configuration System:** JSON-based connection, query, mapping management
- **Plugin System:** `AssemblyHandler` for loading datasources and add-ins from external DLLs

## Critical Architecture Concepts

### 1. IDMEEditor Interface (The Mother Class)
- **Purpose:** Central orchestrator for all data management operations
- **Key Responsibilities:**
  - Manage datasource lifecycle (create, open, close, remove)
  - Access ConfigEditor for connection/type/query management
  - Provide ETL, typesHelper, classCreator, assemblyHandler services
  - Raise PassEvent for notifications
  - Log via AddLogMessage
- **Key Methods:** `GetDataSource()`, `CreateNewDataSourceConnection()`, `OpenDataSource()`, `CloseDataSource()`, `GetDataSourceHelper()`

### 2. IDataSource Interface (Datasource Contract)
Every external datasource must implement this 40+ method interface:
- **Connection:** `Openconnection()`, `Closeconnection()`
- **CRUD:** `GetEntity()`, `GetEntityAsync()`, `InsertEntity()`, `UpdateEntity()`, `DeleteEntity()`
- **Metadata:** `GetEntityStructure()`, `GetEntitesList()`, `CheckEntityExist()`, `GetChildTablesList()`, `GetEntityforeignkeys()`
- **Transactions:** `BeginTransaction()`, `Commit()`, `EndTransaction()`
- **Scripts:** `ExecuteSql()`, `RunScript()`, `GetCreateEntityScript()`
- **Scalars:** `GetScalar()`, `GetScalarAsync()`

### 3. IDataSourceHelper Interface (Query Generation)
Factory-managed helpers for datasource-specific operations:
- **DDL:** `GenerateCreateTableSql()`, `GenerateDropTableSql()`, `GenerateTruncateTableSql()`
- **DML:** `GenerateInsertSql()`, `GenerateUpdateSql()`, `GenerateDeleteSql()`, `GenerateSelectSql()`
- **Schema:** `GetSchemaQuery()`, `GetTableExistsQuery()`, `GetColumnInfoQuery()`
- **Transactions:** `GenerateBeginTransactionSql()`, `GenerateCommitSql()`, `GenerateRollbackSql()`
- **Utilities:** `QuoteIdentifier()`, `SupportsCapability()`

**Current Implementations (5 Core):**
- `RdbmsHelper` — 67+ RDBMS variants (SQL Server, MySQL, PostgreSQL, Oracle, SQLite, DB2, Hana, etc.) with dialect-specific SQL generation
- `MongoDBHelper` — Aggregation pipelines instead of SQL
- `RedisHelper` — Hash/String/List/Set operations with Lua scripts
- `CassandraHelper` — CQL generation with consistency levels and composite keys
- `RestApiHelper` — HTTP verb mapping, JSON/XML body construction, headers

**Planned Implementations (9 Phase 3):**
- `FileFormatHelper` — CSV, JSON, XML, Parquet, Avro, ORC, Excel
- `StreamingHelper` — Kafka, RabbitMQ, Kinesis, PubSub, etc.
- `GraphDatabaseHelper` — Neo4j, TigerGraph, ArangoDB
- `SearchEngineHelper` — ElasticSearch, Solr, Algolia
- `TimeSeriesHelper` — InfluxDB, TimeScaleDB, Prometheus
- `VectorDatabaseHelper` — ChromaDB, Pinecone, Weaviate, Milvus
- `BigDataHelper` — Hadoop, Kudu, Druid, Pinot
- `BlockchainHelper` — Ethereum, Hyperledger, BitcoinCore
- `EmailProtocolHelper` — IMAP, POP3, SMTP, OAuth2

### 4. IConfigEditor Interface (Configuration Management)
Centralized configuration system:
- **Connections:** Load/save `DataConnections.json` with connection properties
- **Queries:** Load/save `QueryList.json` with SQL query templates for metadata discovery
- **Type Mappings:** Load/save `DataTypeMapping.json` for type translation between sources
- **Drivers:** Load/save `ConnectionConfig.json` with driver configurations
- **Entities:** Save/load entity structures for each datasource
- **Mappings:** Save/load entity mappings between sources
- **Categories/Projects:** Manage folder structures and project organization

### 5. Factory Pattern (DataSourceHelperFactory)
- **Purpose:** Centralized management of 287 datasource types via IDataSourceHelper implementations
- **Pattern:** Each helper has `SupportedType` property for type detection
- **Usage:** `var helper = dmeEditor.GetDataSourceHelper(DataSourceType.MongoDB);`
- **Benefits:** Single responsibility, consistent query generation, easily extensible

## Core Component Relationships

```
IDMEEditor (Mother Class)
├── IConfigEditor → Manages connections, queries, types, mappings
├── IETL → Extract, Transform, Load operations
├── IDataTypesHelper → Data type mapping and conversion
├── IAssemblyHandler → Loads external datasources and add-ins
├── IDataSource (collection) → Active datasources being managed
├── DataSourceHelperFactory → IDataSourceHelper implementations
├── IWorkFlowEditor → Workflow definitions and execution
└── IClassCreator → Dynamic class generation from entities
```

## Key Developer Tasks

### Task 1: Implementing a New IDataSourceHelper

1. **Create the helper class** inheriting `IDataSourceHelper`
2. **Set `SupportedType` property** to your datasource type
3. **Implement DDL methods** (CREATE, DROP, TRUNCATE, etc.)
4. **Implement DML methods** (INSERT, UPDATE, DELETE, SELECT)
5. **Implement schema methods** (SCHEMA_QUERY, TABLE_EXISTS, COLUMN_INFO)
6. **Implement transaction methods** (BEGIN, COMMIT, ROLLBACK)
7. **Implement utilities** (QuoteIdentifier, SupportsCapability)
8. **Register in DataSourceHelperFactory**

### Task 2: Adding Support for a New Datasource Type

1. **Add to DataSourceType enum** in BeepDM models
2. **Create IDataSourceHelper** if new datasource type
3. **Create IDataSource implementation** in BeepDataSources
4. **Decorate with [AddinAttribute]** for discovery
5. **Add to ConnectionConfig.json** with driver metadata
6. **Test assembly discovery** via AssemblyHandler
7. **Verify BeepDM can instantiate** via CreateNewDataSourceConnection

### Task 3: Modifying IDMEEditor

- **Partial Classes Pattern:** Split across multiple files for organization
- **Key Partials:** Data source operations, configuration, ETL coordination, messaging
- **Preserve Interface Contract:** Never break IDMEEditor signature (many consumers depend on it)
- **Thread Safety:** Use locking for concurrent datasource access if needed
- **Error Propagation:** Always populate ErrorObject, don't swallow exceptions

## Configuration System Overview

### DataConnections.json
```json
[
  {
    "ConnectionName": "NorthwindDB",
    "ConnectionString": "Server=...;Database=...",
    "DatabaseType": "SqlServer",
    "DriverName": "MSSQL",
    "Category": "RDBMS",
    "GuidID": "guid-string",
    "DatasourceDefaults": []
  }
]
```

### QueryList.json
```json
[
  {
    "QueryName": "GetSchema",
    "Sqlcommandtype": "Schema",
    "DatabaseType": "SqlServer",
    "Query": "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES ..."
  }
]
```

### ConnectionConfig.json
```json
[
  {
    "PackageName": "MSSQL",
    "DatasourceType": "SqlServer",
    "ClassHandler": "SQLServerDataSource",
    "version": "1.0.0",
    "ConnectionString": "Server={Host};Database={Database};...",
    "DbConnectionType": "SqlConnection"
  }
]
```

## Critical Rules

1. **Never break IDMEEditor interface** — It's the public API; many apps depend on it
2. **Always use [AddinAttribute] in datasources** — AssemblyHandler won't load without it
3. **Implement IDataSource completely** — All 40+ methods required (no partial implementations)
4. **Populate ErrorObject on failures** — Set Flag and Message; don't throw for expected errors
5. **Support async variants** — Provide GetEntityAsync, GetScalarAsync, etc.
6. **Maintain configuration JSON format** — Don't change structure without migration plan
7. **Log via Logger.WriteLog()** — Don't use Console.WriteLine
8. **Use singleton for DMEEditor** — Thread safety depends on single instance pattern

## Where Components Live

```
BeepDM/
├── DataManagementEngineStandard/
│   ├── Editor/DM/DMEEditor.cs ← Mother class (main orchestrator)
│   ├── ConfigUtil/ConfigEditor.cs ← Configuration management
│   ├── Editor/ETL/ETLEditor.cs ← ETL operations
│   ├── Editor/Forms/FormsManager.cs ← Master-detail forms simulation
│   ├── Helpers/UniversalDataSourceHelpers/ ← IDataSourceHelper implementations
│   │   ├── RdbmsHelper.cs (28+ variants)
│   │   ├── MongoDBHelper.cs
│   │   ├── RedisHelper.cs
│   │   ├── CassandraHelper.cs
│   │   └── RestApiHelper.cs
│   └── Caching/CacheManager.cs
├── DataManagementModelsStandard/
│   ├── Editor/IDMEEditor.cs ← Interface contract
│   ├── IDataSource.cs ← Datasource contract
│   ├── IConfigEditor.cs ← Config contract
│   ├── IDataConnection.cs ← Connection contract
│   ├── IDataTypesHelper.cs ← Type mapping contract
│   └── IETL.cs ← ETL contract
├── Assembly_helpersStandard/
│   ├── AssemblyHandler.Core.cs ← Plugin discovery
│   ├── AssemblyHandler.Scanning.cs ← Type scanning
│   └── AssemblyHandler.Loaders.cs ← Assembly loading
└── idatasourceimplementationplan.md ← IDataSourceHelper roadmap (35% complete)
```

## Common Tasks with Code

### Getting a Datasource
```csharp
var ds = dmeEditor.GetDataSource("MyConnection");
if (ds != null && ds.ConnectionStatus == ConnectionState.Open)
{
    var entities = await ds.GetEntityAsync("TableName", new List<AppFilter>());
}
```

### Creating a New Connection Programmatically
```csharp
var connProps = new ConnectionProperties
{
    ConnectionName = "MyDB",
    ConnectionString = "Server=...;Database=...",
    DatabaseType = DataSourceType.SqlServer,
    DriverName = "MSSQL"
};

dmeEditor.ConfigEditor.AddDataConnection(connProps);
var ds = dmeEditor.GetDataSource("MyDB");
ds.Openconnection();
```

### Using UnitofWork for Transactions
```csharp
var uow = dmeEditor.CreateUnitOfWork<Product>();
uow.AddNew(new Product { Name = "Widget", Price = 29.99 });
uow.Modify(existingProduct);
uow.Delete(productToRemove);
uow.Commit(); // Persists all changes
```

### Getting a Helper for Query Generation
```csharp
var helper = dmeEditor.GetDataSourceHelper(DataSourceType.MongoDB);
if (helper != null)
{
    var (sql, parameters, success, error) = helper.GenerateSelectSql(
        "MyCollection",
        new[] { "_id", "name" },
        new Dictionary<string, object> { { "_id", "123" } }
    );
}
```

## Testing Strategy

1. **Unit Tests:** Test individual methods (GetEntity, InsertEntity, etc.)
2. **Integration Tests:** Test datasource lifecycle (Open → CRUD → Close)
3. **Configuration Tests:** Test ConfigEditor load/save operations
4. **Assembly Tests:** Test AssemblyHandler discovery of [AddinAttribute] types
5. **Helper Tests:** Test IDataSourceHelper implementations for each dialect
6. **Concurrency Tests:** Test multiple datasources accessed simultaneously

## Common Mistakes to Avoid

1. **Breaking IDMEEditor interface** — Preserves backward compatibility
2. **Not handling async properly** — Use .ConfigureAwait(false) in library code
3. **Throwing instead of using IErrorsInfo** — Expected failures should populate ErrorObject
4. **Ignoring configuration precedence** — JSON configs can be overridden programmatically
5. **Not disposing resources** — DataSources and connections should implement IDisposable
6. **Hard-coding SQL dialects** — Use IDataSourceHelper for dialect abstraction
7. **Missing [AddinAttribute] on datasources** — Won't be discovered by AssemblyHandler

## Integration Points

- **BeepDataSources:** Implements IDataSource and registers via [AddinAttribute]
- **BeepContainers:** Provides DI setup and service registration
- **Client Applications:** Use IDMEEditor as primary API
- **Workflow System:** IETL for ETL operations
- **Forms System:** FormsManager for master-detail forms

## Notes

- BeepDM uses **dependency injection** heavily (Autofac supported)
- **Configuration-first design** — Most behavior comes from JSON configs
- **Plugin architecture** — External datasources loaded dynamically
- **Factory pattern** — 287 datasource types managed centrally
- **Async-preferred** — Long-running operations should be async
- **Partial classes** — Complex features split across logical files
