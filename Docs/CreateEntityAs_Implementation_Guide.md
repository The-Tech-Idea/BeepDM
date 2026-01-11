# CreateEntityAs Implementation Guide for All Datasource Types

## Overview
This guide demonstrates how to implement the `CreateEntityAs()` method in IDataSource implementations to leverage the enhanced IDataSourceHelper with capability awareness.

## Architecture Pattern

```
POCO Class (EntityStructure)
        ↓
ClassCreator.CreateEntityStructureFromPoco()
        ↓
EntityStructure (metadata)
        ↓
DataSource.CreateEntityAs()
        ↓
IDataSourceHelper (capability-aware)
        ↓
Actual table/collection/object in datasource
```

## Implementation Pattern (Generic)

```csharp
public IErrorsInfo CreateEntityAs(string EntityName, EntityStructure entityStructure)
{
    IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
    try
    {
        // STEP 1: Validate input
        if (string.IsNullOrWhiteSpace(EntityName))
        {
            retval.Flag = Errors.Failed;
            retval.Message = "Entity name is required";
            return retval;
        }

        if (entityStructure == null || entityStructure.EntityFields == null || entityStructure.EntityFields.Count == 0)
        {
            retval.Flag = Errors.Failed;
            retval.Message = "Entity structure is empty";
            return retval;
        }

        // STEP 2: Check datasource capabilities
        if (!DatasourceHelper.SupportsCapability(CapabilityType.CreateSchema))
        {
            retval.Flag = Errors.Warning;
            retval.Message = $"Datasource {EntityName} does not support schema creation. Implementation will create structure implicitly.";
            // Continue with implicit creation for NoSQL/schemaless databases
        }

        // STEP 3: Validate entity structure using helper
        var validationResult = DatasourceHelper.ValidateEntity(entityStructure);
        if (!validationResult.IsValid)
        {
            retval.Flag = Errors.Failed;
            retval.Message = $"Entity structure validation failed: {validationResult.ErrorMessage}";
            return retval;
        }

        // STEP 4: Begin transaction (if supported)
        if (Capabilities.SupportsTransactions)
        {
            var beginResult = DatasourceHelper.GenerateBeginTransactionSql();
            if (beginResult.Success)
                ExecuteNonQuery(beginResult.Sql);
        }

        // STEP 5: Generate and execute CREATE TABLE/COLLECTION/OBJECT
        var createResult = DatasourceHelper.GenerateCreateTableSql(
            schemaName: entityStructure.SchemaName ?? "",
            tableName: EntityName,
            fields: entityStructure.EntityFields,
            dataSourceType: this.DataSourceType
        );

        if (!createResult.Success)
        {
            retval.Flag = Errors.Failed;
            retval.Message = $"Failed to generate CREATE statement: {createResult.ErrorMessage}";
            return retval;
        }

        ExecuteNonQuery(createResult.Sql);

        // STEP 6: Create indexes (if supported)
        if (Capabilities.SupportsIndexing)
        {
            foreach (var field in entityStructure.EntityFields.Where(f => f.IsUnique || f.IsIndexed))
            {
                var indexResult = DatasourceHelper.GenerateCreateIndexSql(
                    tableName: EntityName,
                    indexName: $"IX_{EntityName}_{field.FieldName}",
                    columnNames: new[] { field.FieldName },
                    isUnique: field.IsUnique
                );

                if (indexResult.Success)
                    ExecuteNonQuery(indexResult.Sql);
            }
        }

        // STEP 7: Create primary key (if supported)
        if (Capabilities.SupportsConstraints)
        {
            var pkFields = entityStructure.EntityFields.Where(f => f.IsIdentity).ToArray();
            if (pkFields.Length > 0)
            {
                var pkResult = DatasourceHelper.GenerateAddPrimaryKeySql(
                    tableName: EntityName,
                    columnNames: pkFields.Select(f => f.FieldName).ToArray()
                );

                if (pkResult.Success)
                    ExecuteNonQuery(pkResult.Sql);
            }
        }

        // STEP 8: Create foreign keys (if supported)
        if (Capabilities.SupportsConstraints && entityStructure.Relations?.Count > 0)
        {
            foreach (var relation in entityStructure.Relations)
            {
                var fkResult = DatasourceHelper.GenerateAddForeignKeySql(
                    tableName: EntityName,
                    columnNames: relation.ChildFields?.ToArray() ?? new[] { relation.ChildColumnName },
                    referencedTableName: relation.ParentEntityName,
                    referencedColumnNames: relation.ParentFields?.ToArray() ?? new[] { relation.ParentColumnName }
                );

                if (fkResult.Success)
                    ExecuteNonQuery(fkResult.Sql);
            }
        }

        // STEP 9: Commit transaction (if supported)
        if (Capabilities.SupportsTransactions)
        {
            var commitResult = DatasourceHelper.GenerateCommitSql();
            if (commitResult.Success)
                ExecuteNonQuery(commitResult.Sql);
        }

        retval.Flag = Errors.Ok;
        retval.Message = $"Entity '{EntityName}' created successfully";
    }
    catch (Exception ex)
    {
        // STEP 10: Rollback on error (if supported)
        if (Capabilities.SupportsTransactions)
        {
            try
            {
                var rollbackResult = DatasourceHelper.GenerateRollbackSql();
                if (rollbackResult.Success)
                    ExecuteNonQuery(rollbackResult.Sql);
            }
            catch { /* silently fail on rollback */ }
        }

        retval.Flag = Errors.Failed;
        retval.Message = $"Failed to create entity '{EntityName}': {ex.Message}";
        retval.Ex = ex;
    }

    return retval;
}
```

## Datasource-Specific Implementation Examples

### 1. RDBMS (SQL Server, MySQL, PostgreSQL, Oracle, SQLite)

```csharp
// RdbmsDataSource.CreateEntityAs()
public IErrorsInfo CreateEntityAs(string EntityName, EntityStructure entityStructure)
{
    // FULL SUPPORT: All capabilities enabled
    var helper = new RdbmsHelper();
    
    // All steps 1-10 from generic pattern apply
    // Transactions: Full ACID support
    // Constraints: Full PK/FK/Unique support
    // Indexing: Full index support
    // Schema enforcement: Strict schema required
}
```

### 2. MongoDB (Schema-less NoSQL)

```csharp
// MongoDbDataSource.CreateEntityAs()
public IErrorsInfo CreateEntityAs(string EntityName, EntityStructure entityStructure)
{
    IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
    try
    {
        // STEP 1-2: Validation (lightweight)
        if (string.IsNullOrWhiteSpace(EntityName))
        {
            retval.Flag = Errors.Failed;
            retval.Message = "Collection name is required";
            return retval;
        }

        // STEP 3: Create collection (implicit schema)
        var database = mongoClient.GetDatabase(DatabaseName);
        database.CreateCollection(EntityName);

        // STEP 6: Create indexes (MongoDB supports indexes)
        var collection = database.GetCollection<BsonDocument>(EntityName);
        foreach (var field in entityStructure.EntityFields.Where(f => f.IsIndexed || f.IsUnique))
        {
            var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending(field.FieldName);
            collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(
                indexKeys,
                new CreateIndexOptions { Unique = field.IsUnique }
            ));
        }

        // STEP 7-8: Skip constraints (MongoDB handles relationships differently)
        // Store relationship info in MongoDB-specific format (embedded documents or references)

        retval.Flag = Errors.Ok;
        retval.Message = $"Collection '{EntityName}' created successfully";
    }
    catch (Exception ex)
    {
        retval.Flag = Errors.Failed;
        retval.Message = $"Failed to create collection: {ex.Message}";
        retval.Ex = ex;
    }

    return retval;
}
```

### 3. Redis (Key-Value Store)

```csharp
// RedisDataSource.CreateEntityAs()
public IErrorsInfo CreateEntityAs(string EntityName, EntityStructure entityStructure)
{
    IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
    try
    {
        // MINIMAL SCHEMA SUPPORT
        // Redis doesn't enforce schemas; store metadata as separate key

        // Store entity structure as metadata in Redis
        var db = redisConnection.GetDatabase();
        var metadata = JsonSerializer.Serialize(entityStructure);
        db.StringSet($"entity:metadata:{EntityName}", metadata);

        // Optionally create a namespace/prefix for data keys
        // e.g., all records will use keys like: "{EntityName}:{id}"

        retval.Flag = Errors.Ok;
        retval.Message = $"Entity '{EntityName}' registered (Redis schema-less store)";
    }
    catch (Exception ex)
    {
        retval.Flag = Errors.Failed;
        retval.Message = $"Failed to register entity: {ex.Message}";
        retval.Ex = ex;
    }

    return retval;
}
```

### 4. Cassandra (Distributed NoSQL)

```csharp
// CassandraDataSource.CreateEntityAs()
public IErrorsInfo CreateEntityAs(string EntityName, EntityStructure entityStructure)
{
    IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
    try
    {
        // PARTIAL SCHEMA SUPPORT: Limited constraints, distributed design

        var helper = new CassandraHelper();  // Specialized helper for Cassandra

        // Generate CREATE TABLE (Cassandra supports schemas)
        var createResult = helper.GenerateCreateTableSql(
            schemaName: this.KeyspaceName,
            tableName: EntityName,
            fields: entityStructure.EntityFields,
            dataSourceType: DataSourceType.Cassandra
        );

        if (!createResult.Success)
        {
            retval.Flag = Errors.Failed;
            retval.Message = createResult.ErrorMessage;
            return retval;
        }

        // Execute CQL (Cassandra Query Language)
        session.Execute(createResult.Sql);

        // Create secondary indexes (Cassandra-specific)
        foreach (var field in entityStructure.EntityFields.Where(f => f.IsIndexed))
        {
            var indexCql = $"CREATE INDEX ON {EntityName} ({field.FieldName})";
            session.Execute(indexCql);
        }

        // Note: Cassandra doesn't support traditional FOREIGN KEYs
        // Relationships are handled through denormalization or application logic

        retval.Flag = Errors.Ok;
        retval.Message = $"Table '{EntityName}' created in keyspace '{this.KeyspaceName}'";
    }
    catch (Exception ex)
    {
        retval.Flag = Errors.Failed;
        retval.Message = $"Failed to create Cassandra table: {ex.Message}";
        retval.Ex = ex;
    }

    return retval;
}
```

### 5. Elasticsearch (Search Engine)

```csharp
// ElasticsearchDataSource.CreateEntityAs()
public IErrorsInfo CreateEntityAs(string EntityName, EntityStructure entityStructure)
{
    IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
    try
    {
        // SCHEMA MAPPING: Map EntityStructure to Elasticsearch mappings

        var helper = new ElasticsearchHelper();  // Specialized helper

        // Generate mapping from entity fields
        var mappingJson = helper.GenerateMappingJson(entityStructure);

        // Create index with mapping
        var indexRequest = new CreateIndexRequest(EntityName)
        {
            Mappings = new Mappings()
            {
                Properties = ParseMappings(mappingJson)
            }
        };

        var response = elasticClient.Indices.Create(indexRequest);

        if (!response.IsValid)
        {
            retval.Flag = Errors.Failed;
            retval.Message = $"Failed to create Elasticsearch index: {response.ServerError?.Error?.Reason}";
            return retval;
        }

        retval.Flag = Errors.Ok;
        retval.Message = $"Elasticsearch index '{EntityName}' created with mappings";
    }
    catch (Exception ex)
    {
        retval.Flag = Errors.Failed;
        retval.Message = $"Failed to create search index: {ex.Message}";
        retval.Ex = ex;
    }

    return retval;
}
```

### 6. REST API (Protocol-based)

```csharp
// RestApiDataSource.CreateEntityAs()
public IErrorsInfo CreateEntityAs(string EntityName, EntityStructure entityStructure)
{
    IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Warning };
    try
    {
        // REST APIs don't support schema creation through query language
        // Store metadata locally; actual endpoint creation depends on API documentation

        // Option 1: Store metadata in local cache
        this.LocalEntityCache[EntityName] = entityStructure;

        // Option 2: Attempt to call remote endpoint for schema registration (API-specific)
        var response = httpClient.PostAsJsonAsync(
            $"{ApiBaseUrl}/schema/register",
            new { EntityName, Structure = entityStructure }
        ).Result;

        if (!response.IsSuccessStatusCode)
        {
            retval.Flag = Errors.Warning;
            retval.Message = $"REST API does not support schema registration (HTTP {response.StatusCode})";
        }
        else
        {
            retval.Flag = Errors.Ok;
            retval.Message = $"Entity '{EntityName}' registered with REST API";
        }
    }
    catch (Exception ex)
    {
        retval.Flag = Errors.Warning;
        retval.Message = $"REST API schema registration not supported: {ex.Message}";
    }

    return retval;
}
```

### 7. CSV/JSON Files (File-based)

```csharp
// CsvDataSource.CreateEntityAs() / JsonFileDataSource.CreateEntityAs()
public IErrorsInfo CreateEntityAs(string EntityName, EntityStructure entityStructure)
{
    IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
    try
    {
        // FILE-BASED: Write metadata/schema to companion file

        var schemaFilePath = Path.Combine(this.FilePath, $"{EntityName}.schema.json");
        var schemaJson = JsonSerializer.Serialize(entityStructure, new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(schemaFilePath, schemaJson);

        retval.Flag = Errors.Ok;
        retval.Message = $"Schema for '{EntityName}' saved to {schemaFilePath}";
    }
    catch (Exception ex)
    {
        retval.Flag = Errors.Failed;
        retval.Message = $"Failed to create schema file: {ex.Message}";
        retval.Ex = ex;
    }

    return retval;
}
```

## Capability Awareness Pattern

All implementations should use DataSourceCapabilities to determine which features to execute:

```csharp
// Check capabilities before executing operations
if (this.Capabilities.SupportsTransactions)
    // Execute transaction-wrapped DDL

if (this.Capabilities.SupportsConstraints)
    // Create primary keys, foreign keys

if (this.Capabilities.SupportsIndexing)
    // Create indexes

if (this.Capabilities.IsSchemaEnforced)
    // Strict validation and schema creation

if (this.Capabilities.SupportsRelationships)
    // Define relationships/foreign keys
```

## Error Handling Strategy

1. **Validation Errors** (Errors.Failed): Stop execution, return error message
2. **Capability Warnings** (Errors.Warning): Log warning, continue with degraded functionality
3. **Execution Errors** (Errors.Failed): Rollback if supported, return error details
4. **Partial Success**: Return warning with details of skipped operations

## Testing Pattern

```csharp
// Test POCO → EntityStructure → CreateEntityAs workflow
[Test]
public void TestPocoToEntity_EndToEnd()
{
    // 1. Create POCO class
    var pocoType = typeof(Product);  // { Id, Name, Price, Category, CreatedDate }

    // 2. Convert to EntityStructure
    var classCreator = new ClassCreator();
    var entityStructure = classCreator.CreateEntityStructureFromPoco(pocoType);

    // 3. Call CreateEntityAs on datasource
    var dataSource = GetTestDataSource();
    var result = dataSource.CreateEntityAs("Products", entityStructure);

    // 4. Verify result
    Assert.AreEqual(Errors.Ok, result.Flag);

    // 5. Verify table was created
    var entities = dataSource.GetEntities();
    Assert.IsTrue(entities.Any(e => e.EntityName == "Products"));
}
```

## Migration Checklist

For each DataSource implementation:

- [ ] Update CreateEntityAs() to use capability-aware pattern
- [ ] Add transaction support (if supported)
- [ ] Add constraint creation (if supported)
- [ ] Add index creation (if supported)
- [ ] Add schema validation (if required)
- [ ] Add error handling with rollback
- [ ] Test with sample POCO class
- [ ] Document datasource-specific limitations
- [ ] Verify compilation (no errors or warnings)

---

**Next Steps:**
1. Implement CreateEntityAs in all IDataSource implementations (start with RDBMS)
2. Create datasource-specific helpers (MongoDBHelper, RedisHelper, CassandraHelper, etc.)
3. Add comprehensive unit tests for each datasource type
4. Validate end-to-end POCO → Entity workflow
