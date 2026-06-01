# BeepDM: Beep Data Management Engine

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
**Current Status: Alpha** - Actively developed, expect bugs, contributions welcome!

BeepDM is a modular, extensible data management engine providing a full-stack backend for .NET applications — from configuration bootstrapping and plugin discovery, through data access and entity CRUD, to synchronization, ETL, and workflows.

## Architecture: The Five Core Services

BeepDM's runtime is composed of five core services wired by `BeepService`. Understanding these is understanding the engine:

```
BeepService (Services)
  ├── ConfigEditor (ConfigUtil)       ← configuration persistence, JSON files, paths
  ├── AssemblyHandler (AssemblyHandler) ← plugin/DLL/NuGet discovery and loading
  ├── Util                              ← data conversion, driver linking
  └── DMEEditor (Editor/DM)            ← central hub: data sources, logging, ETL
        └── UnitofWork<T> (Editor/UOW) ← consumer-facing CRUD, validation, export
```

### 1. BeepService — Bootstrapper
**Path:** `DataManagementEngineStandard/Services/`  |  **Doc:** [`Help/services-registration-lifetimes.html`](Help/services-registration-lifetimes.html)

Creates and wires the entire object graph. Supports Desktop, Web API, Blazor Server, Blazor WASM, and MAUI.

```csharp
// Minimal startup — all platforms
services.AddBeepServices(options =>
{
    options.AppPath = "C:\\MyApp";
    options.AppRepoName = "MyAppRepo";
    options.ConfigType = BeepConfigType.DataConnector;
});

// Or use platform shortcuts:
services.AddBeepForDesktop("C:\\MyApp", "MyAppRepo");   // WinForms/WPF
services.AddBeepForWeb("C:\\MyApp", "MyAppRepo");       // ASP.NET Core
services.AddBeepForBlazorServer("C:\\MyApp", "MyAppRepo");
services.AddBeepForBlazorWasm("C:\\MyApp", "MyAppRepo");
```

#### `LoadConfigurations(string AppReponame)` — Preloading the Engine's Defaults

Called automatically during `Configure()`, this method populates the engine with all built-in knowledge before any consumer code runs. It is **thread-safe** (`lock` + `isconfigloaded` flag) and runs exactly once per `BeepService` instance.

```csharp
public void LoadConfigurations(string AppReponame)
{
    lock (_configLock)
    {
        if (isconfigloaded) return;

        EnvironmentService.AddAllConnectionConfigurations(this.DMEEditor);
        EnvironmentService.AddAllDataSourceMappings(this.DMEEditor);
        EnvironmentService.AddAllDataSourceQueryConfigurations(this.DMEEditor);
        EnvironmentService.CreateMainFolder();
        EnvironmentService.CreateAppRepofolder(AppReponame);

        isconfigloaded = true;
    }
}
```

**What each call adds:**

| Call | Populates | Source | Purpose |
|---|---|---|---|
| `AddAllConnectionConfigurations` | `ConfigEditor.DataDriversClasses` | `ConnectionHelper.GetAllConnectionConfigs()` | Registers connection drivers for SQL Server, PostgreSQL, MySQL, SQLite, Oracle, and all other supported databases — so the engine knows how to connect to each type |
| `AddAllDataSourceMappings` | `ConfigEditor.DataTypesMap` | `DataTypeFieldMappingHelper.GetMappings()` | Maps database column types (e.g. `varchar`, `int`, `datetime`) to .NET/C# types (`string`, `int`, `DateTime`) — essential for schema generation and type-safe entity CRUD |
| `AddAllDataSourceQueryConfigurations` | `ConfigEditor.QueryList` | `RDBMSHelper.CreateQuerySqlRepos()` | Seeds the query repository with pre-built SQL templates (schema inspection, CRUD helpers, metadata queries) for each supported RDBMS |
| `CreateMainFolder` | File system | N/A | Creates `{basepath}/TheTechIdea/Beep/` — the root folder for all Beep application data |
| `CreateAppRepofolder` | File system | N/A | Creates `{basepath}/TheTechIdea/Beep/{AppReponame}/` — isolates this application's config files, connection drivers, data files, and scripts |

**Why it matters:** Without `LoadConfigurations`, `DMEEditor` starts with no known database drivers, no type mappings, and no query templates. A consumer calling `dm.GetDataSource(...)` would find nothing. This method is the bootstrap step that gives the engine its built-in intelligence before plugins and user configuration are loaded by `LoadAssemblies()`.

#### Deep Dive: `DataDriversClasses` → `classHandler` → `IDataSource` (DataSourcesClasses)

The `DataDriversClasses` collection is the metadata catalog — it tells the engine *what* databases exist. But it doesn't contain the actual C# code to talk to them. That code is discovered later by `AssemblyHandler` and stored in `ConfigEditor.DataSourcesClasses`. The `classHandler` string property on `ConnectionDriversConfig` is the **bridge** between the two:

```
LoadConfigurations() seeds DataDriversClasses                   LoadAssemblies() populates DataSourcesClasses
┌─────────────────────────────────────────┐                   ┌──────────────────────────────────────────┐
│ ConnectionDriversConfig (e.g. SQLite)   │                   │ AssemblyClassDefinition (e.g. SQLite)    │
│   DriverClass  = "System.Data.SQLite"   │    classHandler   │   className   = "SQLiteDataSource"       │
│   dllname      = "System.Data.SQLite"   │ ◄───────────────► │   dllname     = "SomePlugin.dll"         │
│   AdapterType  = "SQLiteDataAdapter"    │    className       │   type        = typeof(SQLiteDataSource) │
│   classHandler = "SQLiteDataSource" ────┼──────────────────►│   PackageName = "SQLitePlugin"           │
│   ...                                   │                   │   IsDataSource = true                    │
└─────────────────────────────────────────┘                   └──────────────────────────────────────────┘
```

**How the two collections come to life:**

| Stage | What happens | Populates |
|---|---|---|
| **1. `LoadConfigurations()`** | `ConnectionHelper.GetAllConnectionConfigs()` creates ~60+ `ConnectionDriversConfig` entries — one per database type (SQL Server, PostgreSQL, MySQL, SQLite, Oracle, Snowflake, DuckDB, etc.). Each entry hardcodes the `classHandler` string to a known name like `"SQLServerDataSource"`, `"SQLiteDataSource"`. | `ConfigEditor.DataDriversClasses` |
| **2. `LoadAssemblies()`** | `AssemblyHandler` scans DLLs for classes implementing `IDataSource` (marked with `[AddinAttribute]`). Each matching class is wrapped in an `AssemblyClassDefinition` containing the actual `Type`, `className`, `dllname`, and other metadata. | `ConfigEditor.DataSourcesClasses` |
| **3. `RestoreDriverConfigAndAutoLoad()`** | For drivers with `AutoLoad = true`, downloads NuGet packages and re-scans, adding their `IDataSource` implementations to `DataSourcesClasses`. Also loads/saves `ConnectionConfig.json` to persist user-modified driver settings. | Updates both collections |

**The `classHandler` link in action — what happens when you call `dm.GetDataSource("northwind.db")`:**

```csharp
// In DMEEditor.CreateNewDataSourceConnection() (DMEEditor.cs:975):
var cn = ConfigEditor.DataConnections.Find("northwind.db");
                              // Step A: Find the connection properties

var driversConfig = Utilfunction.LinkConnection2Drivers(cn);
                              // Step B: Match to a ConnectionDriversConfig in DataDriversClasses
                              //   by DatasourceType (e.g. DataSourceType.SqlLite)

                              // Step C: Read the classHandler string
                              //   e.g. driversConfig.classHandler = "SQLiteDataSource"

var ase = ConfigEditor.DataSourcesClasses
    .FirstOrDefault(x => x.className == driversConfig.classHandler);
                              // Step D: Find the matching AssemblyClassDefinition
                              //   in DataSourcesClasses by className

var type = assemblyHandler.GetType(ase.type.AssemblyQualifiedName);
                              // Step E: Get the actual System.Type

var ds = activator(type, name, logger, editor, dbType, errorObject);
                              // Step F: Instantiate the IDataSource
```

**Summary of the three collections:**

| Collection | Type | Role | Populated by |
|---|---|---|---|
| `DataDriversClasses` | `List<ConnectionDriversConfig>` | **Metadata catalog** — ADO.NET driver names, NuGet package info, connection string templates | `LoadConfigurations()` (seeded), then persisted to `ConnectionConfig.json` |
| `DataSourcesClasses` | `List<AssemblyClassDefinition>` | **Code registry** — actual C# types implementing `IDataSource`, discovered via reflection | `LoadAssemblies()` (scanned from DLLs) |
| `classHandler` (on `ConnectionDriversConfig`) | `string` | **The bridge** — maps a driver config entry to its runtime `IDataSource` implementation | Hardcoded in each `CreateXxxConfig()` factory method |

---

### 2. ConfigEditor — Configuration Hub
**Path:** `DataManagementEngineStandard/ConfigUtil/`  |  **Doc:** [`Help/configeditor.html`](Help/configeditor.html)

Central configuration store persisting to JSON files. Delegates to specialized managers:

| Manager | Responsibility | JSON File |
|---|---|---|
| `DataConnectionManager` | Connection strings, connection properties | `DataConnections.json` |
| `QueryManager` | SQL / query repository | `QueryList.json` |
| `ComponentConfigManager` | Drivers, workflows, reports, projects | `ConnectionConfig.json` |
| `EntityMappingManager` | Entity structures, field mappings | entity files |
| `MigrationHistoryManager` | Per-datasource migration history | migration files |

```csharp
var config = beepService.Config_editor;

// Add a data source connection
config.AddDataConnection(new ConnectionProperties
{
    ConnectionString = "Data Source=./northwind.db",
    ConnectionName = "northwind.db",
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS
});

// Discovered type registries (populated by AssemblyHandler)
config.DataSourcesClasses   // All IDataSource implementations found
config.DataDriversClasses    // All driver configs found
config.DefaultResolverClasses // Default value resolvers
config.WorkFlowActions       // Workflow action types
```

---

### 3. AssemblyHandler — Plugin & Driver Discovery
**Path:** `DataManagementEngineStandard/AssemblyHandler/`  |  **Doc:** [`Help/assemblyhandler-loading-nuget-extensions.html`](Help/assemblyhandler-loading-nuget-extensions.html)

Scans DLLs, discovers plugins, and manages NuGet packages. Two implementations:

| Implementation | When to Use |
|---|---|
| `AssemblyHandler` (default) | Single app, few plugins — direct `Assembly.LoadFrom` |
| `SharedContextAssemblyHandler` | Multi-tenant, plugin-heavy — `AssemblyLoadContext` isolation, plugin lifecycle, health monitoring |

```csharp
var handler = beepService.DMEEditor.assemblyHandler;

// Full scan: built-in + drivers + plugins + extensions + NuGet packages
handler.LoadAllAssembly(progress, CancellationToken.None);

// Search & install NuGet drivers
var packages = await handler.SearchNuGetPackagesAsync("postgresql", take: 10);
await handler.InstallAndLoadNuGetPackageAsync(packages[0].PackageId);

// Switch to SharedContext for plugin-heavy apps
beepService.AssemblyHandlerType = AssemblyHandlerType.SharedContext;
```

**Scanned contracts:** `IDataSource`, `IDM_Addin`, `ILoaderExtention`, `IWorkFlowAction`, `IRuleParser`, `IPipelinePlugin`, `IDefaultValueResolver`, `IFileFormatReader`, and 8 more.

---

### 4. DMEEditor — Central Hub
**Path:** `DataManagementEngineStandard/Editor/DM/`  |  **Doc:** [`Help/dmeeditor.html`](Help/dmeeditor.html)

The mother class. Orchestrates all other services. Every consumer operation flows through `DMEEditor`.

```csharp
var dm = beepService.DMEEditor;

// Open a data source (discovers from config, creates IDataSource via AssemblyHandler)
var ds = dm.GetDataSource("northwind.db");
ds.Openconnection();

// Query entity data
var customers = ds.GetEntity("Customers", new List<AppFilter>
{
    new AppFilter { FieldName = "Country", Operator = "=", FilterValue = "UK" }
});

// Insert
ds.InsertEntity("Customers", newRow);

// Access sub-services
dm.ConfigEditor       // IConfigEditor — configuration
dm.assemblyHandler    // IAssemblyHandler — plugins
dm.typesHelper        // IDataTypesHelper — type mapping
dm.ETL                // IETL — data transformation
dm.classCreator       // IClassCreator — dynamic type generation
dm.WorkFlowEditor     // IWorkFlowEditor — workflow management

// Schema operations
var structure = dm.GetEntityStructure("Customers", "northwind.db");
dm.CreateEntityAs(structure);
```

---

### 5. UnitofWork<T> — Consumer-Facing CRUD
**Path:** `DataManagementEngineStandard/Editor/UOW/`  |  **Doc:** [`Help/unitofwork.html`](Help/unitofwork.html)

Full-featured entity CRUD with change tracking, validation, undo/redo, import/export, virtual paging, and computed columns.

```csharp
var uow = new UnitofWork<Product>(dmEditor, "northwind.db", "Products");

// Query with filters and paging
uow.PageSize = 50;
uow.PageIndex = 1;
uow.Get(new List<AppFilter> { new AppFilter { FieldName = "CategoryId", FilterValue = "1" } });

// Create / Update / Delete
var product = uow.New();
product.ProductName = "Widget";
product.UnitPrice = 19.99m;
uow.Add(product);
uow.Commit();

// Change tracking
var changes = uow.GetChangeSummary(); // Inserted: 1, Updated: 0, Deleted: 0

// Export
using var stream = File.Create("products.json");
await uow.ToJsonAsync(stream);

// Undo / Redo
uow.EnableUndo(enable: true, maxDepth: 50);
uow.Undo(); // Reverts last operation

// Validation
uow.IsAutoValidateEnabled = true;
uow.BlockCommitOnValidationError = true;
var errors = uow.GetInvalidItems();

// Aggregates
var totalRevenue = uow.Sum("Revenue");
var avgPrice = uow.Average("UnitPrice");
var customersByCountry = uow.GroupBy("Country");
```

`UnitofWork<T>` also provides: soft delete, virtual/lazy loading, computed columns, bookmarks, batch freeze, concurrency control, and 12 lifecycle events (`PreInsert`, `PostInsert`, `PreUpdate`, `PostUpdate`, etc.).

---

### 6. ConnectionHelper — Driver Catalog & Connection Utilities
**Path:** `DataManagementEngineStandard/Helpers/ConnectionHelpers/`  |  **Doc:** [`Help/connection-helpers.html`](Help/connection-helpers.html)

A static facade split across 24 files providing the metadata catalog of 200+ pre-configured data source drivers, plus connection-string processing, validation, security, and liveness probing.

```
ConnectionHelper (static facade)
  ├── ConnectionDriverLinkingHelper     ← match connection → driver config
  ├── ConnectionStringProcessingHelper  ← {placeholder} → value substitution
  ├── ConnectionStringValidationHelper  ← structural checks (SqlServer, MySQL, etc.)
  ├── ConnectionStringSecurityHelper    ← mask passwords, API keys for logging
  ├── ConnectionHelper_GetParameterList ← key=value parse & mutate
  ├── ConnectionHelper_RDBMS            ← 19 drivers: SQL Server, PostgreSQL, MySQL, Oracle, SQLite, ...
  ├── ConnectionHelper_NoSQL            ← 17 drivers: MongoDB, Redis, Cassandra, Elasticsearch, ...
  ├── ConnectionHelper_VectorDB         ←  8 drivers: ChromaDB, Pinecone, Qdrant, Weaviate, ...
  ├── ConnectionHelper_File             ← 25+ drivers: CSV, JSON, Excel, Parquet, Avro, PDF, ...
  ├── ConnectionHelper_Cloud            ← 24 drivers: AWS, Azure, GCP, Databricks, Supabase, ...
  ├── ConnectionHelper_Streaming        ← 20 drivers: Kafka, RabbitMQ, Pulsar, NATS, ...
  ├── ConnectionHelper_InMemory/Cache   ← 14 drivers: SQLite Memory, Redis Cache, NCache, ...
  ├── ConnectionHelper_WebAPI           ← 11 drivers: REST, GraphQL, gRPC, SOAP, OData, ODBC, ...
  ├── ConnectionHelper_CRM              ← 11 drivers: Salesforce, HubSpot, Dynamics 365, ...
  ├── ConnectionHelper_Marketing        ← 10+ drivers: Mailchimp, Marketo, SendGrid, ...
  ├── ConnectionHelper_ECommerce        ←  9 drivers: Shopify, WooCommerce, Magento, ...
  ├── ConnectionHelper_ProjectMgmt      ← 11 drivers: Jira, Asana, Monday.com, Trello, ...
  ├── ConnectionHelper_Communication    ←  9 drivers: Slack, Teams, Discord, Zoom, ...
  ├── ConnectionHelper_Blockchain       ←  3 drivers: Ethereum, Hyperledger, Bitcoin
  ├── ConnectionHelper_Proxy            ←  2 drivers: BeepProxyNode, BeepProxyCluster
  ├── ProxyLivenessHelper               ← category-aware health probe for live IDataSource
  └── TestConnectionHelper              ← async setup-time & runtime connection tests
```

```csharp
// Seed the catalog (called by LoadConfigurations)
var allDrivers = ConnectionHelper.GetAllConnectionConfigs();  // 200+ entries

// Match a user's connection to the right driver
var driver = ConnectionHelper.LinkConnection2Drivers(myConn, dmeEditor.ConfigEditor);

// Fill {placeholders} in the template
string cs = ConnectionHelper.ReplaceValueFromConnectionString(driver, conn, dmeEditor);

// Mask secrets before logging
logger.WriteLog(ConnectionHelper.SecureConnectionString(cs));

// Test before registering
var (ok, msg) = await TestConnectionHelper.TestConnectionAsync(config);
```

**Why ConnectionHelper matters:** Without the 200+ pre-configured `ConnectionDriversConfig` entries, `DMEEditor` would have zero knowledge of how to connect to any database, file format, cloud service, or SaaS platform. This catalog is the engine's built-in encyclopedia — every `classHandler` string maps to an `IDataSource` implementation discovered by `AssemblyHandler`, forming the complete bridge from configuration to runtime connection.

---

### 7. DataTypeFieldMappingHelper — Type Mapping Engine
**Path:** `DataManagementEngineStandard/Helpers/DataTypesHelpers/`  |  **Doc:** [`Help/data-types-helpers.html`](Help/data-types-helpers.html)

Converts between database-native column types and .NET/C# types, powered by 1000+ pre-defined mappings across 100+ database systems. Essential for schema generation, DDL creation, and type-safe entity CRUD.

```
DataTypeFieldMappingHelper (static facade, ~80 methods)
  ├── DataTypeMappingLookup        ← cached lookup: DB type ↔ .NET type
  ├── DataTypeMappingRepository    ← routes DataSourceType → filtered mapping lists
  ├── DataTypeBasicOperations      ← .NET type lists, validation, custom converters
  ├── TypeHelper                   ← dynamic type activation (expression trees)
  └── DatabaseTypeMappingRepositories/ (26 partial-class files)
      ├── SqlServer.cs, Oracle.cs, PostgreMySQL.cs, Common.cs, ...
      ├── NoSQL.cs, Modern.cs, Enterprise.cs, Cloud.cs, Graph.cs
      ├── Vector.cs, Streaming.cs, BigData.cs, MachineLearning.cs
      ├── FileFormats.cs, Protocols.cs, WorkflowIoT.cs, IoT.cs
      ├── CRM.cs, Accounting.cs, SocialMedia.cs, PaymentGateways.cs
      └── ... 100+ database systems with full type mappings
```

```csharp
// .NET type → database column type (for DDL generation)
string sqlType = DataTypeFieldMappingHelper.GetDataType(
    "MySqlServerDb", entityField, dmeEditor);
// System.String + Size1=100 → "nvarchar(100)"

// Database column type → .NET type (for schema reading)
string netType = DataTypeFieldMappingHelper.GetDataType(
    "MySqlServerDb", "decimal(18,2)", dmeEditor);
// → "System.Decimal"

// Get all 1000+ mappings for seeding (called by LoadConfigurations)
var all = DataTypeFieldMappingHelper.GetMappings();

// Placeholder system: (N)=length, (P,S)=precision+scale, (N,S)=alt
// Stored: "decimal(P,S)" → Resolved: "decimal(18,2)"
```

**Key design features:**
- `Fav` flag on each mapping — when multiple mappings exist for the same type (e.g. `nvarchar(N)` vs `varchar(N)`), the favored one wins
- Thread-safe caches — 3 `ConcurrentDictionary` caches eliminate repeated lookups
- Intelligent fallbacks — when no mapping is found, analyzes the raw type name (contains "int"? → `System.Int32`)
- All entries use `DataSourceName` matching `AssemblyClassDefinition.className` for automatic routing

---

### 8. RDBMSHelper — SQL & Schema Generation
**Path:** `DataManagementEngineStandard/Helpers/RDBMSHelpers/`  |  **Doc:** [`Help/rdbms-helpers.html`](Help/rdbms-helpers.html)

Generates database-specific SQL for every operation — DDL, DML, schema queries, feature detection, and entity-based CRUD — across 30+ database types. Every method accepts a `DataSourceType` enum and produces provider-specific syntax.

```
RDBMSHelper (static facade, ~40 methods)
  ├── DatabaseSchemaQueryHelper          ← schema/database metadata queries (25+ DBs)
  ├── DatabaseObjectCreationHelper       ← CREATE TABLE, indexes, PKs, DROP, TRUNCATE
  ├── DatabaseFeatureHelper              ← features, sequences, identity, transactions
  ├── DatabaseQueryRepositoryHelper      ← 200+ predefined schema queries, cached
  ├── DMLHelpers/ (10 files)
  │   ├── DatabaseDMLBasicOperations     ← INSERT/UPDATE/DELETE
  │   ├── DatabaseDMLBulkOperations      ← bulk insert, UPSERT/MERGE
  │   ├── DatabaseDMLAdvancedQueryGen    ← SELECT, JOIN, aggregation, window functions
  │   ├── DatabaseDMLParameterizedQueries← parameterized CRUD (injection-safe)
  │   └── DatabaseDMLUtilities           ← paging syntax, SafeQuote, identifier quoting
  └── EntityHelpers/ (8 files)
      ├── DatabaseEntitySqlGenerator     ← entity → parameterized SQL
      ├── DatabaseEntityValidator        ← 15+ structural validation rules
      ├── DatabaseEntityAnalyzer         ← compatibility analysis, improvement suggestions
      └── ... naming, reserved keyword, type checking
```

```csharp
// DDL: CREATE TABLE from entity
var (ddl, ok, err) = RDBMSHelper.GenerateCreateTableSQL(entity);
// PostgreSQL: CREATE TABLE Orders (Id BIGSERIAL NOT NULL, Name TEXT, ...

// DML: Cross-database paging
string paging = RDBMSHelper.GetPagingSyntax(DataSourceType.SqlServer, page: 2, size: 10);
// SQL Server: "OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY"
// MySQL:      "LIMIT 10 OFFSET 10"
// Oracle:     "OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY"

// Feature detection at runtime
if (RDBMSHelper.SupportsFeature(DataSourceType.Postgre, DatabaseFeature.Json))
    GenerateJsonColumn();

// Entity validation with 15+ rules
var (valid, errors) = RDBMSHelper.ValidateEntityStructure(entity);

// Entity-based CRUD — SQL generated from EntityStructure
var (sql, params, ok, err) = RDBMSHelper.GenerateInsertWithValues(entity, values);
```

---

### 9. Universal DataSource Helpers — One Interface, 40+ Datasources
**Path:** `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/`  |  **Doc:** [`Help/universal-datasource-helpers.html`](Help/universal-datasource-helpers.html)

A datasource-agnostic abstraction layer that replaces per-type `if/else` chains with a single `IDataSourceHelper` interface. The `DataSourceHelperFactory` resolves any `DataSourceType` to the correct implementation, and the capability matrix tells you what operations are safe before you generate them.

```
IDataSourceHelper (contract — 12+ methods: schema, DDL, DML, type mapping)
     │
     ├── RdbmsHelper          ← 18 SQL dialects (SQL Server→Snowflake)
     ├── MongoDBHelper         ← BSON docs, aggregation pipelines
     ├── RedisHelper           ← Hashes, Lua scripts, SCAN paging
     ├── CassandraHelper       ← CQL, composite keys, token pagination
     ├── RestApiHelper         ← HTTP methods, query params, JSON body
     ├── SearchHelper          ← Elasticsearch, Solr
     ├── GraphHelper           ← Neo4j, ArangoDB (Cypher)
     ├── TimeSeriesHelper      ← InfluxDB, ClickHouse, TimescaleDB
     ├── VectorHelper          ← ChromaDB, Pinecone, Qdrant, Milvus
     ├── StreamingHelper       ← Kafka, RabbitMQ, Pulsar
     └── FileDataSourceHelper  ← CSV, JSON, Excel (Phase 2)
              │
DataSourceHelperFactory.GetHelper(type, editor) ← resolves any type
DataSourceCapabilityMatrix.Supports(type, "Joins") ← 40×20 matrix
PocoToEntityConverter.ConvertPocoToEntity&lt;T&gt;() ← C# POCO → EntityStructure
```

```csharp
// One interface, same API for all 40+ datasources
var helper = new DataSourceHelperFactory(dmeEditor).GetHelper(DataSourceType.MongoDB);

// Schema / DDL / DML — same call, datasource-specific output
var (ddl, ok, err) = helper.GenerateCreateTableSql(entity);   // MongoDB: collection config
var (insert, params, ok, err) = helper.GenerateInsertSql("users", data); // BSON doc

// Check capabilities before generating unsupported SQL
if (!helper.Capabilities.SupportsJoins)
    return ClientSideJoin(results);

// POCO → EntityStructure → DDL — automatic full pipeline
var entity = PocoToEntityConverter.ConvertPocoToEntity&lt;Product&gt;(strategy, "Products");
var ddl = factory.GetHelper(DataSourceType.Postgre).GenerateCreateTableSql(entity).Sql;
```

---

## The IDataSource Framework — Plug-in Architecture

Every data source in BeepDM — from SQL Server to a CSV file to a REST API — is an implementation of a single interface: `IDataSource`. This is the core architectural pattern that enables the engine to treat a Parquet file the same way it treats PostgreSQL.

### The Two-Layer Model

```
┌──────────────────────────────────────────────────────────────────┐
│                    IDataConnection (connection transport)          │
│  How to physically reach the data: file path, host:port, URL      │
│                                                                   │
│  FileConnection ← validates file exists on disk                   │
│  DefaulDataConnection ← ADO.NET / driver-backed connection        │
└────────────────────────────┬─────────────────────────────────────┘
                             │ Dataconnection property
┌────────────────────────────▼─────────────────────────────────────┐
│                    IDataSource (data operations)                   │
│  What to do with the data: query, insert, discover schema         │
│                                                                   │
│  Entities: List<EntityStructure>  ← self-describing schema        │
│  GetEntity(name, filters)         ← unified query API             │
│  RunQuery(sql)                    ← raw query fallback            │
│  GetEntityStructure(name)         ← schema introspection          │
└──────────────────────────────────────────────────────────────────┘
```

### How a Data Source Comes to Life

```
1. User calls dm.GetDataSource("northwind.db")
         │
2. DMEEditor looks up ConnectionProperties in ConfigEditor
         │
3. Links ConnectionProperties → ConnectionDriversConfig (via DataDriversClasses)
         │  classHandler = "SQLiteDataSource"
         │
4. Finds AssemblyClassDefinition in DataSourcesClasses by className
         │  type = typeof(SQLiteDataSource)  ← discovered by AssemblyHandler
         │
5. Finds constructor with signature:
   (string name, IDMLogger logger, IDMEEditor editor, DataSourceType type, IErrorsInfo errors)
         │
6. Instantiates the IDataSource via ObjectActivator<IDataSource>
         │
7. Configures ds.Dataconnection (FileConnection or DefaulDataConnection)
         │
8. Auto-loads EntityStructure from persisted config
   ds.Entities = ConfigEditor.LoadDataSourceEntitiesValues(ds.DatasourceName).Entities
         │
9. Returns ready-to-use IDataSource
```

**Key insight:** The constructor signature `(string, IDMLogger, IDMEEditor, DataSourceType, IErrorsInfo)` is the **universal constructor contract**. Every `IDataSource` implementation must have a constructor matching this signature. `AssemblyHandler` enforces this when discovering types.

### EntityStructure — The Self-Describing Schema

Every `IDataSource` carries its schema as `List<EntityStructure> Entities`. This is the **universal schema model**:

```csharp
public class EntityStructure
{
    string        EntityName;            // "Products", "Orders"
    List<EntityField> Fields;            // Columns/fields
    List<EntityField> PrimaryKeys;       // Primary key fields
    List<RelationShipKeys> Relations;    // Foreign key relationships
    List<AppFilter> Filters;             // Default filters/views
    List<EntityParameters> Parameters;   // Parameterized query parameters

    // Schema metadata
    DataSourceType DatabaseType;         // Which DB type this entity came from
    string SchemaOrOwnerOrDatabase;      // dbo / public / keyspace
    EntityType EntityType;               // Entity, View, Query, etc.
}

public class EntityField
{
    string FieldName;          // "ProductName"
    string Fieldtype;          // "System.String" (full .NET type name)
    DbFieldCategory FieldCategory;
    bool IsKey, IsUnique, AllowDBNull;
    int Size1;                 // Length (e.g. 100 → nvarchar(100))
    short NumericPrecision;    // For decimal(p,s)
    short NumericScale;
    int FieldIndex;            // Ordinal position
}
```

**Where EntityStructure is populated:**
- **RDBMS:** `IDataSource.GetEntityStructure()` queries `INFORMATION_SCHEMA.COLUMNS` or equivalent
- **FileDataSource:** `InferEntityStructure()` reads file headers, samples rows, infers types via `TypeInferenceHelper`
- **CSVDataSource:** `GetFieldsbyTableScan()` reads CSV headers and sample rows
- **MongoDB:** Reads BSON document structure
- **REST API:** Reads OpenAPI/Swagger schema or JSON sample

### EntityStructure as the Bridge Between Layers

`EntityStructure` is the universal exchange format connecting every part of the engine:

```
                       ┌──────────────────────────────┐
 PocoToEntityConverter │     EntityStructure           │
    (C# POCO → entity) │                              │
                       └──────────┬───────────────────┘
                                  │
          ┌───────────────────────┼───────────────────────┐
          │                       │                       │
          ▼                       ▼                       ▼
  RDBMSHelper.Generate    DataTypeFieldMapping    UnitofWork<T>
  CreateTableSQL(entity)   Helper.GetDataType()   .GetEntityStructure()
  → DDL statement         → type conversion      → query by schema
```

### Real Implementations: FileDataSource vs CSVDataSource

Both implement `IDataSource`, both work with files, but at very different levels of sophistication:

| Capability | CSVDataSource (legacy) | FileDataSource (current) |
|---|---|---|
| **Format support** | CSV only | CSV, TSV, JSON, Text (pluggable `IFileFormatReader`) |
| **Reader** | Hardcoded `CsvTextFieldParser` | `FileReaderFactory.GetReader(DatasourceType)` |
| **Type inference** | Manual scan | `TypeInferenceHelper.InferWithStats()` with confidence scores |
| **Schema discovery** | `Getfields()` from headers + scan | `InferEntityStructure()` sampling up to 200 rows |
| **Governance** | None | Access policy enforcement, row-level security, field masking |
| **Validation** | None | `RowValidationMode` (Accept / Warn / Strict) with dead-letter store |
| **Transactions** | Basic file backup/restore | Deferred operations, pending queue |
| **Idempotent ingestion** | None | `IIdempotentFileIngester` with checksum dedup |
| **Reader switching** | None | Runtime switch via `IFileDataSourceReaderHost` |

```csharp
// FileDataSource — the newer, richer implementation
[AddinAttribute(Category = DatasourceCategory.FILE,
    DatasourceType = DataSourceType.CSV | DataSourceType.TSV |
                     DataSourceType.Json | DataSourceType.Text,
    FileType = "csv,tsv,json,txt")]
public partial class FileDataSource : IDataSource, IIdempotentFileIngester,
                                        IFileDataSourceReaderHost
{
    // Schema auto-discovery
    private EntityStructure InferEntityStructure(string entityName)
    {
        string[] headers = _reader.ReadHeaders(filePath);
        var entity = new EntityStructure { EntityName = entityName, ... };
        // Sample up to 200 rows per column
        var columnSamples = CollectColumnSamples(filePath, headers, 200);
        // Statistical type inference per column
        foreach (var (name, values) in columnSamples)
        {
            var (type, confidence) = TypeInferenceHelper.InferWithStats(values);
            entity.Fields.Add(new EntityField {
                FieldName = name, Fieldtype = type, ... });
        }
        return entity;
    }

    // CRUD operates on EntityStructure
    public IErrorsInfo InsertEntity(string entityName, object data)
    {
        var entity = GetEntityStructure(entityName, false);
        var row = ToDictionary(data, entity);  // Map object → field values
        ValidateRow(entity, row);              // Check constraints
        _reader.AppendRow(filePath, entity.Fields.Select(f => row[f.FieldName]));
    }
}
```

### Summary: How It All Connects

```
Your C# model (POCO)
    │
    ▼  PocoToEntityConverter
EntityStructure (schema model)
    │
    ├──► DataTypeFieldMappingHelper  → .NET type ↔ DB type
    ├──► RDBMSHelper                 → DDL/DML generation  
    ├──► IDataSourceHelper           → per-datasource SQL/native query
    │
    ▼  DMEEditor.GetDataSource()
IDataSource (runtime instance)
    │
    ├── .Entities = List<EntityStructure>   ← self-describing schema
    ├── .GetEntity(name, filters)           ← unified query
    ├── .InsertEntity / UpdateEntity / DeleteEntity  ← unified CRUD
    └── .RunQuery(sql)                     ← raw query fallback
    │
    ▼  IDataConnection
FileConnection / DefaulDataConnection  ← physical transport
                                           (validates paths, driver connections)
```

**The single most important rule:** Every data source is an `IDataSource`. Every schema is an `EntityStructure`. Every field is an `EntityField`. Once you understand these three types, you understand how the entire engine treats data — from files to cloud warehouses to vector databases — through a single unified lens.

---

| Interface | Purpose |
|---|---|---|
| `IBeepService` | Bootstrapper: creates and wires the entire object graph |
| `IDMEEditor` | Central orchestrator: data sources, logging, ETL, configuration |
| `IConfigEditor` | Configuration persistence with specialized per-concern managers |
| `IAssemblyHandler` | DLL discovery, NuGet lifecycle, plugin registration |
| `IDataSource` | All data source implementations (SQL, file, REST, in-memory) |
| `IUnitofWork<T>` | Consumer CRUD with validation, export, change tracking |
| `IDataImportManager` | Batch import pipeline with validation, quality rules, staging |
| `IMappingManager` | Entity mapping with auto-matching, conventions, versioning |
| `IDefaultsManager` | Column defaults with 10 built-in resolvers (DateTime, GUID, User, etc.) |
| `IBeepSyncManager` | Data synchronization with CDC, conflict resolution, SLO |
| `IMigrationManager` | Schema migration with preflight, dry-run, 2PC/Saga transactions |
| `IProxyDataSource` | Failover, circuit breaker, load-balanced data sources |
| `IDistributedDataSource` | Distributed queries with sharding, partition routing, resharding |
| `ISetupWizard` | Wizard-based application initialization with checkpoints |

## Getting Started

### Prerequisites
- .NET 8, 9, or 10
- Visual Studio 2022+

### Installation

```bash
git clone https://github.com/The-Tech-Idea/BeepDM.git
cd BeepDM
dotnet restore
dotnet build
```

### Quick Start — ASP.NET Core

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register BeepDM
builder.Services.AddBeepForWeb(
    appPath: builder.Environment.ContentRootPath,
    appName: "MyApp");

var app = builder.Build();

// Access DMEEditor
var dm = app.Services.GetRequiredService<IDMEEditor>();

// Connect to a data source
var props = new ConnectionProperties
{
    ConnectionString = "Data Source=./Data/northwind.db",
    ConnectionName = "northwind.db",
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS
};
dm.ConfigEditor.AddDataConnection(props);

var ds = dm.GetDataSource("northwind.db");
ds.Openconnection();

// Use UnitofWork
var uow = new UnitofWork<Customer>(dm, "northwind.db", "Customers");
uow.Get();
var customers = uow.Units;
```

### Quick Start — Desktop (WinForms/WPF)

```csharp
var services = new ServiceCollection();
services.AddBeepForDesktop("C:\\MyApp", "MyAppRepo");
var sp = services.BuildServiceProvider();
var beepService = sp.GetRequiredService<IBeepService>();

// Initialize defaults
beepService.LoadAssemblies();

var dm = beepService.DMEEditor;
// ... same data source access pattern
```

### Quick Start — Blazor Server

```csharp
builder.Services.AddBeepForBlazorServer(
    builder.Environment.ContentRootPath, "MyApp");
```

### Quick Start — Blazor WASM

```csharp
builder.Services.AddBeepForBlazorWasm(
    builder.HostEnvironment.BaseAddress, "MyApp");
// Uses BlazorIndexedDbSink for telemetry
```

## Directory Structure

Every BeepDM project follows this directory structure:

| Directory | Purpose |
|---|---|
| `Addin/` | DLLs implementing `IDM_Addin` (forms, controls, classes) |
| `Config/` | `DataConnections.json`, `ConnectionConfig.json`, `QueryList.json`, `DataTypeMapping.json` |
| `ConnectionDrivers/` | Database driver DLLs (SQLite, SQL Server, Oracle, etc.) |
| `DataFiles/` | Project data files (CSV, JSON, XLS, etc.) |
| `DataViews/` | Federated view definitions (JSON) |
| `LoadingExtensions/` | `ILoaderExtention` implementations |
| `Mapping/` | Entity mapping definitions |
| `ProjectClasses/` | Custom `IDataSource`, add-in, and extension DLLs |
| `Scripts/` | ETL scripts and logs |
| `WorkFlow/` | Workflow definitions |

## Extending BeepDM

BeepDM has two extension patterns depending on what you're adding. See [`Help/extending-beepdm.html`](Help/extending-beepdm.html) for the full guide with real source code walkthroughs.

### Pattern 1: Creating a New IDataSource
Use when adding a new data source type (database, file format, API) that plugs into `dm.GetDataSource()`.

```csharp
[AddinAttribute(Category = DatasourceCategory.CLOUD,
    DatasourceType = DataSourceType.WebService)]
public class MyDataSource : IDataSource
{
    // ★ REQUIRED: Constructor must match this exact signature
    public MyDataSource(string name, IDMLogger logger, IDMEEditor editor,
                        DataSourceType type, IErrorsInfo errors)
    {
        DatasourceName = name; Logger = logger; DMEEditor = editor;
        DatasourceType = type; ErrorObject = errors;
        Entities = new List<EntityStructure>();
        EntitiesNames = new List<string>();
    }

    // Core methods to implement: Openconnection, GetEntity, GetEntityStructure,
    // InsertEntity, UpdateEntity, DeleteEntity, RunQuery, CreateEntityAs
}
```

Place in `ProjectClasses/`. Also add a `ConnectionDriversConfig` entry with `classHandler` matching the class name.

### Pattern 2: Loader Extension for a New Interface
Use when adding a completely new plug-in interface that `AssemblyHandler` doesn't natively scan for (e.g. `IBranch`, `IReport`, `ICustomDashboard`).

```csharp
// 1. Define your interface
public interface IBranch : IBranchID { ... }

// 2. Add a collection to ConfigEditor
ConfigEditor.BranchesClasses = new List<AssemblyClassDefinition>();

// 3. Create the loader extension
public class BranchLoaderExtension : ILoaderExtention
{
    public IErrorsInfo LoadAllAssembly()
    {
        foreach (var asm in Loader.Assemblies)
            foreach (var type in asm.DllLib.GetTypes())
                if (type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IBranch)))
                    Loader.ConfigEditor.BranchesClasses.Add(
                        Loader.GetAssemblyClassDefinition(type, "IBranch"));
        return new ErrorsInfo { Flag = Errors.Ok };
    }
}

// 4. AssemblyHandler auto-discovers it — no manual registration needed.
//    During LoadAllAssembly(), AssemblyHandler.ScanExtensions()
//    finds ILoaderExtention types, instantiates them with
//    new BranchLoaderExtension(this), and calls Scan(assembly)
//    for every loaded assembly.
beepService.LoadAssemblies();  // Now discovers IBranch implementations

// 5. Place the loader extension DLL in LoadingExtensions/ or ProjectClasses/
```

**Full walkthrough with real sources** ([`BranchLoaderExtension.cs`](https://github.com/The-Tech-Idea/Beep.Branchs), [`BeepAppTree.CreateRootTree()`](TheTechIdea.Beep.Winform.Controls.Integrated/ITrees/BeepTreeView/BeepAppTree.cs), complete 7-step integration diagram): [`Help/extending-beepdm.html`](Help/extending-beepdm.html).

## Documentation

### Help/ Directory (HTML — 70+ pages)
Open `Help/index.html` in a browser for comprehensive documentation covering:
- **Getting Started**: BeepService, Registration, Setup Framework
- **Core Concepts**: UnitOfWork, Connections, Data Sources, EntityStructure, IDataSource Framework
- **Data Management**: WebAPI, JSON, CSV, DataView, Proxy, Distributed, Caching
- **Editor Classes**: DMEEditor, ConfigEditor, DataSync, Import, Mapping, Defaults, Migration
- **Connection Helpers**: Driver catalog, connection strings, validation, security, liveness — 200+ pre-configured drivers
- **Data Types Helpers**: 1000+ type mappings across 100+ databases — DB type ↔ .NET type conversion
- **RDBMS Helpers**: SQL/DDL/DML generation, feature detection, entity validation — 30+ database types
- **Universal DataSource Helpers**: One interface for 40+ datasources — factory, capability matrix, POCO conversion
- **Extending BeepDM**: Create custom IDataSource, Loader Extensions for new interfaces, end-to-end integration
- **Advanced**: AssemblyHandler, Rules Engine, ETL Workflow, BeepSync, Services, Helpers, Utils

### Docs/ Directory (Markdown — 25+ guides)
- [Getting Started](Docs/GettingStarted.md)
- [Core Architecture](Docs/CoreArchitecture.md)
- [Service Registration](Docs/ServiceRegistration.md)
- [Unit of Work Pattern](Docs/UnitOfWork.md)
- [Assembly Handler](Docs/AssemblyHandler.md)
- [Configuration Management](Docs/Configuration.md)
- [Creating Custom Data Sources](Docs/HowToCreateNewDataSource.md)
- [ETL Operations](Docs/ETL.md)
- [WebAPI DataSource](Docs/WebAPI.md)
- [Connection Helpers](Help/connection-helpers.html) — Full driver catalog & connection-string utilities
- [Data Types Helpers](Help/data-types-helpers.html) — Database type ↔ .NET type mapping engine (100+ DBs)
- [RDBMS Helpers](Help/rdbms-helpers.html) — SQL/DDL/DML generation, feature detection, entity validation
- [Universal DataSource Helpers](Help/universal-datasource-helpers.html) — One interface for 40+ datasources with capability matrix
- [Extending BeepDM](Help/extending-beepdm.html) — Create custom IDataSource, Loader Extensions, end-to-end integration
- [And 15+ more...](Docs/)

## Project Status
- **Alpha Phase**: Core features functional, APIs may evolve.
- **Contributions**: Welcome! See [CONTRIBUTING.md](CONTRIBUTING.md).

## License
BeepDM is licensed under the [MIT License](LICENSE).

## Links
- [Wiki](https://github.com/The-Tech-Idea/BeepDM/wiki)
- [Issues](https://github.com/The-Tech-Idea/BeepDM/issues)
- [Beep Data Sources](https://github.com/The-Tech-Idea/BeepDataSources)
- [Beep Enterprize Winform](https://github.com/The-Tech-Idea/BeepEnterprize.winform)
