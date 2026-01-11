# Universal DataSource Helpers Framework

## Overview

The `UniversalDataSourceHelpers` framework provides a unified abstraction layer for working with 40+ datasource types (RDBMS, NoSQL, APIs, File-based systems, etc.) in the BeepDM ecosystem. It replaces the legacy RDBMS-only helpers with a datasource-agnostic pattern that enables consistent query generation, feature detection, and data manipulation across all supported systems.

## Architecture

### Core Components

```
UniversalDataSourceHelpers/
├── Core/
│   ├── IDataSourceHelper.cs           # Interface contract for all helpers
│   ├── DataSourceCapabilities.cs      # Capability definition model (20+ features)
│   └── DataSourceCapabilityMatrix.cs  # Matrix of 40+ datasources × 20 capabilities
│
├── Conversion/
│   └── PocoToEntityConverter.cs        # POCO → EntityStructure conversion
│
├── RdbmsHelpers/                      # Migrated from legacy RDBMSHelpers
│   ├── RdbmsHelper.cs
│   ├── Schema/
│   ├── Ddl/
│   ├── Dml/
│   └── Entity/
│
├── MongoDBHelpers/                    # MongoDB-specific implementations
├── RedisHelpers/                      # Redis-specific implementations
├── CassandraHelpers/                  # Cassandra-specific implementations
├── RestApiHelpers/                    # REST API-specific implementations
└── FileDataSourceHelpers/             # File-based data source implementations
```

### Design Patterns

1. **IDataSourceHelper Interface** — Defines the contract all datasource helpers must implement:
   - Schema operations (get schemas, check existence, retrieve column info)
   - DDL operations (CREATE, DROP, TRUNCATE, CREATE INDEX)
   - DML operations (INSERT, UPDATE, DELETE, SELECT)
   - Utility methods (identifier quoting, type mapping, validation)

2. **Static Helper Classes** — Each datasource type has a static helper class implementing `IDataSourceHelper`:
   - Methods are stateless and thread-safe
   - No instantiation required
   - Example: `RdbmsHelper.GenerateInsertSql()`, `MongoDBHelper.GenerateInsertSql()`

3. **DataSourceCapabilityMatrix** — Static matrix mapping datasources to capabilities:
   - 40+ datasource types pre-configured
   - 20+ features (transactions, joins, aggregations, TTL, etc.)
   - Enables graceful degradation when features aren't available
   - Query: `DataSourceCapabilityMatrix.Supports(DataSourceType.Redis, "Transactions")` → true

4. **POCO-to-Entity Conversion** — Automatic schema generation from C# classes:
   - Reflects on POCO properties
   - Detects keys via `[Key]` attribute or naming convention
   - Extracts metadata from DataAnnotations (`[Required]`, `[StringLength]`, `[Range]`)
   - Detects and prevents circular references
   - Returns fully-configured `EntityStructure` objects

## Usage Examples

### Example 1: Detect Datasource Capabilities

```csharp
// Check if Redis supports transactions
bool hasTransactions = DataSourceCapabilityMatrix.Supports(DataSourceType.Redis, "SupportsTransactions");
// Result: true (Redis Lua scripts provide atomicity)

// Check if Cassandra supports JOINs
bool hasJoins = DataSourceCapabilityMatrix.Supports(DataSourceType.Cassandra, "SupportsJoins");
// Result: false (Cassandra requires denormalization)

// Get all datasources that support full-text search
var ftSearchDatasources = DataSourceCapabilityMatrix.GetDatasourcesSupportingCapability("SupportsFullTextSearch");
// Result: [Elasticsearch, MongoDB, ClickHouse, PostgreSQL, ...]
```

### Example 2: Convert POCO to EntityStructure

```csharp
// Define a POCO class
public class Product
{
    [Key]
    public int ProductId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }
    
    public string Description { get; set; }
}

// Convert to EntityStructure
var entity = PocoToEntityConverter.ConvertPocoToEntity<Product>(
    strategy: PocoToEntityConverter.KeyDetectionStrategy.AttributeThenConvention,
    entityName: "Products");

// Result: EntityStructure with:
// - EntityName: "Products"
// - Fields: [ProductId (key), Name (required, max 100), Price (range 0-max), Description]
// - Ready to use with any datasource helper
```

### Example 3: Graceful Degradation Based on Capabilities

```csharp
public static string GenerateQuery(DataSourceType type, string tableName)
{
    var caps = DataSourceCapabilityMatrix.GetCapabilities(type);
    
    string query = $"SELECT * FROM {tableName}";
    
    if (caps.SupportsFullTextSearch)
        query += " WHERE MATCH(description) AGAINST('search term')";
    else if (caps.IsSchemaEnforced)
        query += " WHERE description LIKE '%search term%'";
    else
        query = "Note: Full-text search not available for this datasource";
    
    if (caps.SupportsOrderByWindowFunctions)
        query += " ORDER BY created_date DESC FETCH FIRST 10 ROW ONLY";
    
    return query;
}
```

### Example 4: Use Datasource Helper (RDBMS)

```csharp
// Generate SQL INSERT for MySQL
var (sql, parameters, success, error) = RdbmsHelper.GenerateInsertSql(
    DataSourceType.Mysql,
    "users",
    new Dictionary<string, object>
    {
        { "name", "John Doe" },
        { "email", "john@example.com" },
        { "age", 30 }
    });

// Result: 
// sql = "INSERT INTO `users` (`name`, `email`, `age`) VALUES (@name, @email, @age)"
// parameters = { name: "John Doe", email: "john@example.com", age: 30 }
```

### Example 5: Use Datasource Helper (MongoDB)

```csharp
// Generate MongoDB aggregation pipeline
var (pipeline, parameters, success, error) = MongoDBHelper.GenerateInsertQuery(
    "users",
    new Dictionary<string, object>
    {
        { "_id", ObjectId.GenerateNewId() },
        { "name", "Jane Doe" },
        { "email", "jane@example.com" }
    });

// Result: MongoDB insert command with appropriate syntax
```

## Key Features

### 1. Datasource Abstraction
- Single interface for all 40+ datasources
- Consistent API across different database systems
- Easy to add new datasource types

### 2. Capability Matrix
- 20+ features per datasource (transactions, joins, aggregations, etc.)
- Automatic capability detection
- Enables intelligent query generation
- Supports graceful degradation

### 3. POCO Conversion
- Automatic schema generation from C# classes
- Key detection via attributes or conventions
- DataAnnotations support ([Required], [StringLength], [Range])
- Circular reference detection and prevention
- Phase 2: Advanced relationship mapping

### 4. Type Mapping
- C# type → database type conversion
- Database type → C# type conversion
- Database-specific type variations handled
- Example: C# `int` → SQL Server "INT", MySQL "INT", Cassandra "int"

### 5. Query Generation
- DDL: Table creation, dropping, truncation, index management
- DML: CRUD operations (INSERT, UPDATE, DELETE, SELECT)
- Entity-based queries from EntityStructure objects
- Database-specific syntax variations

## Supported Datasources

### Relational Databases (7)
- SQL Server, MySQL, PostgreSQL, Oracle, SQLite, DB2, Firebird

### Cloud Databases (7)
- Azure SQL, AWS RDS, Snowflake, AWS Redshift, Google BigQuery, CockroachDB, Cloud Spanner

### NoSQL Databases (6)
- MongoDB, Redis, Cassandra, Neo4j, CouchDB, Couchbase

### Search & Analytics (3)
- Elasticsearch, ClickHouse, AWS Redshift (also cloud)

### File-Based & In-Memory (4)
- Flat Files, CSV, JSON, XML, In-Memory Cache

### APIs & Web Services (3)
- REST APIs, GraphQL, OData

**Total: 40+ datasource types with full capability matrix**

## Key Methods

### DataSourceCapabilityMatrix

```csharp
// Get capabilities for a datasource
var caps = DataSourceCapabilityMatrix.GetCapabilities(DataSourceType.MongoDB);

// Check if a feature is supported
bool hasJoins = DataSourceCapabilityMatrix.Supports(DataSourceType.Redis, "SupportsJoins");

// Get all datasources supporting a capability
var list = DataSourceCapabilityMatrix.GetDatasourcesSupportingCapability("SupportsTransactions");

// Get summary of all capabilities
var summary = DataSourceCapabilityMatrix.GetCapabilitySummary();
```

### PocoToEntityConverter

```csharp
// Convert POCO to EntityStructure
var entity = PocoToEntityConverter.ConvertPocoToEntity<MyClass>(
    strategy: PocoToEntityConverter.KeyDetectionStrategy.AttributeThenConvention,
    entityName: "CustomName",
    throwOnError: true);

// Get circular reference diagnostics (without throwing)
var diagnostics = PocoToEntityConverter.GetCircularReferenceDiagnostics<MyClass>();
```

### IDataSourceHelper (Implemented by each datasource)

```csharp
// Schema operations
(string Query, bool Success) = helper.GetSchemaQuery(userName);
(string Query, bool Success) = helper.GetTableExistsQuery(tableName);
(string Query, bool Success) = helper.GetColumnInfoQuery(tableName);

// DDL operations
(string Sql, bool Success, string Error) = helper.GenerateCreateTableSql(entity);
(string Sql, bool Success, string Error) = helper.GenerateDropTableSql(tableName);

// DML operations
(string Sql, Dictionary Params, bool Success, string Error) = helper.GenerateInsertSql(table, data);
(string Sql, Dictionary Params, bool Success, string Error) = helper.GenerateSelectSql(table, columns, conditions);

// Utilities
string quoted = helper.QuoteIdentifier("user_table");
string dbType = helper.MapClrTypeToDatasourceType(typeof(int));
Type clrType = helper.MapDatasourceTypeToClrType("INT");
(bool Valid, List<string> Errors) = helper.ValidateEntity(entity);
```

## Migration from Legacy RDBMSHelper

The legacy `RDBMSHelper` class is now deprecated and will be removed in v3.0. It has been moved to `UniversalDataSourceHelpers.RdbmsHelpers.RdbmsHelper`.

### For New Code
```csharp
// Use new namespace
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers;

var (sql, success) = RdbmsHelpers.RdbmsHelper.GetSchemasorDatabases(type, user);
```

### For Existing Code
The legacy `RDBMSHelper` still works (with `[Obsolete]` warnings):
```csharp
// Old code still works but shows compiler warning
var (sql, success) = RDBMSHelper.GetSchemasorDatabases(type, user);
// ⚠️ Warning: 'RDBMSHelper' is obsolete. Use UniversalDataSourceHelpers.RdbmsHelpers instead.
```

## Extension Points for Phase 2

- Advanced relationship mapping (navigation properties, one-to-many)
- Reverse mapping: EntityStructure → POCO generation
- Query builder UI abstraction for complex queries
- Additional datasources (Elasticsearch, GraphDB helpers)
- Data validation pipeline (constraints → rules engine)
- Query optimization suggestions

## See Also

- [UniversalDataSourceHelpers-Architecture.md](./../UniversalDataSourceHelpers-Architecture.md) — Detailed architecture documentation
- [copilot-instructions.md](./../copilot-instructions.md) — Integration with BeepDM ecosystem
- `DataSourceCapabilityMatrix.cs` — Full capability definitions
- `PocoToEntityConverter.cs` — POCO conversion implementation
- `IDataSourceHelper.cs` — Interface contract documentation

