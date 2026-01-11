# Universal DataSource Helpers - Quick Reference

## Framework Hierarchy

```
UniversalDataSourceHelpers/
├── [CORE LAYER]
│   ├── IDataSourceHelper         - Interface contract (schema, DDL, DML, utilities)
│   ├── DataSourceCapabilities    - Capability model (20 boolean flags)
│   └── DataSourceCapabilityMatrix - 40 datasources × 20 capabilities lookup
│
├── [CONVERSION LAYER]
│   └── PocoToEntityConverter     - C# POCO → EntityStructure + reverse
│
├── [DATASOURCE HELPERS]
│   ├── RdbmsHelpers/             - SQL databases (migrated in Phase 2)
│   ├── MongoDBHelpers/           - MongoDB aggregation pipeline
│   ├── RedisHelpers/             - Redis commands + Lua
│   ├── CassandraHelpers/         - Cassandra CQL
│   ├── RestApiHelpers/           - HTTP endpoints (GET/POST/PUT/DELETE)
│   └── FileDataSourceHelpers/    - CSV/JSON/XML (Phase 2)
│
└── [DOCUMENTATION]
    ├── README.md                  - Framework overview + examples
    ├── IMPLEMENTATION_SUMMARY.md  - What was built in Phase 1
    └── QUICK_REFERENCE.md         - This file
```

## Quick API Reference

### 1. Check Datasource Capabilities

```csharp
// Import
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Core;

// Single capability check
bool hasTransactions = DataSourceCapabilityMatrix.Supports(
    DataSourceType.Redis, 
    "SupportsTransactions");  // true

// Get all capabilities for a datasource
var caps = DataSourceCapabilityMatrix.GetCapabilities(DataSourceType.MongoDB);
if (caps.SupportsJoins) { /* ... */ }
if (caps.SupportsTTL) { /* ... */ }

// Find all datasources with a capability
var fullTextSupported = DataSourceCapabilityMatrix
    .GetDatasourcesSupportingCapability("SupportsFullTextSearch");
// Result: [Elasticsearch, MongoDB, ClickHouse, PostgreSQL, ...]

// Get summary of all capabilities
var summary = DataSourceCapabilityMatrix.GetCapabilitySummary();
foreach (var kvp in summary)
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
```

### 2. Convert POCO to Entity

```csharp
// Import
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.Conversion;

// Define POCO
public class Customer
{
    [Key]
    public int CustomerId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    
    [Range(0, 999999.99)]
    public decimal CreditLimit { get; set; }
    
    public DateTime CreatedDate { get; set; }
}

// Convert
var entity = PocoToEntityConverter.ConvertPocoToEntity<Customer>(
    strategy: PocoToEntityConverter.KeyDetectionStrategy.AttributeThenConvention,
    entityName: "Customers",
    throwOnError: true);

// Result: EntityStructure ready for any datasource
// - Fields: [CustomerId (key), Name (required, 100), CreditLimit (range), CreatedDate]
// - EntityType: Entity
// - EntityName: "Customers"

// Diagnostic: Get circular references without throwing
var diagnostics = PocoToEntityConverter.GetCircularReferenceDiagnostics<Customer>();
if (diagnostics.Count > 0)
    foreach (var issue in diagnostics)
        Console.WriteLine($"Circular ref issue: {issue}");
```

### 3. Generate Queries per Datasource

```csharp
// Import datasource-specific helpers
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.MongoDBHelpers;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RedisHelpers;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.CassandraHelpers;
using TheTechIdea.Beep.Helpers.UniversalDataSourceHelpers.RestApiHelpers;

// Sample data
var data = new Dictionary<string, object>
{
    { "name", "John Doe" },
    { "email", "john@example.com" },
    { "age", 30 }
};

// MongoDB Insert
var (mongoSql, mongoParams, mongoSuccess, mongoError) = 
    MongoDBHelper.GenerateInsertSql("users", data);
// Result: 
// sql = "db.users.insertOne({ "name": "John Doe", "email": "john@example.com", "age": 30 })"

// Redis Insert
var (redisSql, redisParams, redisSuccess, redisError) = 
    RedisHelper.GenerateInsertSql("users", data);
// Result:
// sql = "HSET users:guid_1234 "name" "John Doe" "email" "john@example.com" "age" "30""

// Cassandra Insert
var (cassandraSql, cassandraParams, cassandraSuccess, cassandraError) = 
    CassandraHelper.GenerateInsertSql("users", data);
// Result:
// sql = "INSERT INTO "users" ("name", "email", "age") VALUES (?, ?, ?);"

// REST API Insert
var (restSql, restParams, restSuccess, restError) = 
    RestApiHelper.GenerateInsertSql("users", data);
// Result:
// sql = "POST /api/users with body: { "name": "John Doe", "email": "john@example.com", "age": 30 }"
```

### 4. Advanced: Capability-Based Query Generation

```csharp
public static string GenerateQuery(DataSourceType datasource, string table)
{
    var caps = DataSourceCapabilityMatrix.GetCapabilities(datasource);
    
    string query = $"SELECT * FROM {table}";
    
    // Add full-text search if supported, fallback to LIKE
    if (caps.SupportsFullTextSearch)
        query += " WHERE MATCH(description) AGAINST('search term')";
    else if (caps.IsSchemaEnforced)
        query += " WHERE description LIKE '%search term%'";
    
    // Add pagination appropriate for datasource
    if (caps.SupportsWindowFunctions)
        query += " ORDER BY id OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY";
    else
        query += " LIMIT 10 OFFSET 20";  // MySQL/PostgreSQL syntax
    
    return query;
}
```

### 5. Entity Validation Before Use

```csharp
// MongoDB validation
var (valid, errors) = MongoDBHelper.ValidateEntity(entity);
if (!valid)
    foreach (var error in errors)
        Console.WriteLine($"Validation error: {error}");

// Cassandra validation (requires primary key)
var (cassValid, cassErrors) = CassandraHelper.ValidateEntity(entity);

// REST API validation (simpler - just needs name + fields)
var (restValid, restErrors) = RestApiHelper.ValidateEntity(entity);
```

### 6. Type Mapping (C# ↔ Datasource)

```csharp
// C# type → Datasource type
string mongoType = MongoDBHelper.MapClrTypeToDatasourceType(typeof(int));
// Result: "int"

string redisType = RedisHelper.MapClrTypeToDatasourceType(typeof(decimal));
// Result: "float"

string cassandraType = CassandraHelper.MapClrTypeToDatasourceType(typeof(Guid));
// Result: "uuid"

// Datasource type → C# type
Type mongoClr = MongoDBHelper.MapDatasourceTypeToClrType("double");
// Result: typeof(double)

Type redisClr = RedisHelper.MapDatasourceTypeToClrType("binary");
// Result: typeof(byte[])
```

## Supported Datasources & Capabilities

### Capability Flags (20 total)

| Capability | Example Support |
|---|---|
| SupportsTransactions | ✅ SQL, MongoDB (v4.0+), Redis (Lua), ❌ Cassandra, REST |
| SupportsJoins | ✅ SQL, ❌ MongoDB, Redis, Cassandra |
| SupportsAggregations | ✅ SQL, MongoDB, ClickHouse, ❌ Redis, Cassandra |
| SupportsIndexes | ✅ All except REST, File |
| SupportsParameterization | ✅ SQL, Cassandra, GraphQL, OData, ❌ REST (varies) |
| SupportsIdentity | ✅ SQL, Redis, ❌ NoSQL (app-assigned) |
| SupportsTTL | ✅ Redis, MongoDB, Cassandra, Elasticsearch |
| SupportsTemporalTables | ✅ SQL Server, PostgreSQL, ❌ NoSQL |
| SupportsWindowFunctions | ✅ SQL, ClickHouse, ❌ NoSQL |
| SupportsStoredProcedures | ✅ SQL, ❌ NoSQL |
| SupportsBulkOperations | ✅ All except REST (single ops) |
| SupportsFullTextSearch | ✅ Elasticsearch, ClickHouse, MongoDB, PostgreSQL |
| SupportsNativeJson | ✅ SQL (2016+), MongoDB, PostgreSQL, CouchDB |
| SupportsPartitioning | ✅ SQL, BigQuery, Cassandra, Elasticsearch |
| SupportsReplication | ✅ SQL, NoSQL, BigQuery |
| SupportsViews | ✅ SQL, CouchDB, ClickHouse, ❌ NoSQL |
| SupportsSchemaEvolution | ✅ All (SQL via ALTER, NoSQL via flexibility) |
| IsSchemaEnforced | ✅ SQL, Cassandra, ❌ MongoDB, Redis |
| ... | ... |

### Datasource Coverage

**Implemented Phase 1:**
- ✅ MongoDB (aggregation pipeline, document validation)
- ✅ Redis (hash storage, Lua atomicity)
- ✅ Cassandra (CQL, token pagination)
- ✅ REST API (HTTP methods, query params)
- 🔮 File-based (Phase 2)

**Planned Phase 2:**
- 🔮 SQL Server, MySQL, PostgreSQL (migrate from legacy)
- 🔮 Elasticsearch
- 🔮 Neo4j (Cypher)
- 🔮 DuckDB, ClickHouse

## Common Patterns

### Pattern 1: Graceful Degradation

```csharp
public async Task<IEnumerable<T>> QueryAsync<T>(
    string datasourceName, 
    string table,
    Expression<Func<T, bool>> filter = null)
{
    var type = GetDataSourceType(datasourceName);
    var caps = DataSourceCapabilityMatrix.GetCapabilities(type);
    
    if (!caps.SupportsAggregations && filter != null)
        return await QueryWithoutFiltering<T>(table);  // Client-side filtering
    
    if (!caps.SupportsBulkOperations)
        return await QueryOneByOne<T>(table);  // Slower but works
    
    return await QueryOptimal<T>(table, filter);  // Full capabilities
}
```

### Pattern 2: Dynamic Query Builder

```csharp
public static string BuildQuery(DataSourceType type, string table, 
    Dictionary<string, object> filters = null)
{
    var helper = GetHelperForType(type);
    
    var (sql, _, success, error) = helper.GenerateSelectSql(
        table,
        columns: null,
        conditions: filters,
        orderBy: null,
        skip: 0,
        take: 100);
    
    if (!success)
        throw new Exception($"Query generation failed: {error}");
    
    return sql;
}
```

### Pattern 3: Validation Before Operation

```csharp
public static bool TryOperateOn(EntityStructure entity, DataSourceType type)
{
    var helper = GetHelperForType(type);
    var (isValid, errors) = helper.ValidateEntity(entity);
    
    if (!isValid)
    {
        logger.LogError($"Entity invalid for {type}:");
        foreach (var error in errors)
            logger.LogError($"  - {error}");
        return false;
    }
    
    // Safe to proceed with operation
    return true;
}
```

## File Locations

```
c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\
└── DataManagementEngineStandard\
    └── Helpers\
        └── UniversalDataSourceHelpers\
            ├── Core\
            │   ├── IDataSourceHelper.cs
            │   ├── DataSourceCapabilities.cs
            │   └── DataSourceCapabilityMatrix.cs
            ├── Conversion\
            │   └── PocoToEntityConverter.cs
            ├── MongoDBHelpers\
            │   └── MongoDBHelper.cs
            ├── RedisHelpers\
            │   └── RedisHelper.cs
            ├── CassandraHelpers\
            │   └── CassandraHelper.cs
            ├── RestApiHelpers\
            │   └── RestApiHelper.cs
            ├── README.md
            ├── IMPLEMENTATION_SUMMARY.md
            └── QUICK_REFERENCE.md (this file)
```

## Next Steps

1. **Phase 1 Complete** ✅ → Ready for integration testing
2. **Phase 2:** Integrate with DMEEditor + migrate RDBMS helpers
3. **Phase 3:** Add advanced POCO features + additional datasources
4. **Docs:** Update `.github/copilot-instructions.md` with examples

