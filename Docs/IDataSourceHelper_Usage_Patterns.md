# IDataSourceHelper Usage Patterns for IDataSource Implementations

## Overview
This guide shows practical patterns for how any IDataSource implementation can leverage the enhanced IDataSourceHelper across multiple operations—not just schema creation.

---

## 1. Type Mapping & Validation

### Pattern: Validate POCO Before Entity Creation

```csharp
public class RdbmsDataSource : IDataSource
{
    public IDataSourceHelper Helper { get; set; }
    
    public IErrorsInfo CreateFromPoco(Type pocoType, string entityName)
    {
        IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
        
        try
        {
            // Convert POCO to EntityStructure
            var classCreator = new ClassCreator();
            var entity = classCreator.CreateEntityStructureFromPoco(pocoType);
            entity.EntityName = entityName;
            
            // VALIDATE using helper
            var (valid, msg) = Helper.ValidateEntity(entity);
            if (!valid)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Entity validation failed: {msg}";
                return retval;
            }
            
            // Safe to proceed with creation
            return CreateEntityAs(entityName, entity);
        }
        catch (Exception ex)
        {
            retval.Flag = Errors.Failed;
            retval.Message = ex.Message;
            retval.Ex = ex;
        }
        
        return retval;
    }
}
```

### Pattern: Map CLR Types When Importing Data

```csharp
public class CsvDataSource : IDataSource
{
    public IDataSourceHelper Helper { get; set; }
    
    public IErrorsInfo ImportFromCsv(string csvPath, string entityName)
    {
        IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
        
        try
        {
            // Read CSV and analyze column types
            var dataTable = ReadCsvFile(csvPath);
            var entity = new EntityStructure { EntityName = entityName };
            
            foreach (DataColumn col in dataTable.Columns)
            {
                // Use helper to map CLR type to datasource type
                var clrType = col.DataType;
                var datasourceType = Helper.MapClrTypeToDatasourceType(clrType);
                
                var field = new EntityField
                {
                    FieldName = col.ColumnName,
                    FieldType = clrType.Name,
                    Size = datasourceType.Contains("VARCHAR") ? 255 : 0
                };
                
                entity.EntityFields.Add(field);
            }
            
            // Store entity mapping
            StoreEntityMapping(entity);
            retval.Flag = Errors.Ok;
        }
        catch (Exception ex)
        {
            retval.Flag = Errors.Failed;
            retval.Message = ex.Message;
            retval.Ex = ex;
        }
        
        return retval;
    }
}
```

---

## 2. Identifier Quoting & SQL Generation

### Pattern: Generate Safe SQL Queries

```csharp
public class MySqlDataSource : IDataSource
{
    public IDataSourceHelper Helper { get; set; }
    
    public IDataReader GetData(string entityName, string whereClause = null)
    {
        try
        {
            // Use helper to quote identifiers (handles MySQL backticks)
            var quotedEntity = Helper.QuoteIdentifier(entityName);
            
            // Generate SELECT safely
            var (selectSql, success, error) = Helper.GenerateSelectSql(
                entityName,
                null,  // all columns
                whereClause
            );
            
            if (!success)
                throw new InvalidOperationException(error);
            
            // Execute with safe identifiers
            return ExecuteReader(selectSql);
        }
        catch (Exception ex)
        {
            Logger.WriteLog($"Error getting data: {ex.Message}");
            return null;
        }
    }
}
```

### Pattern: Batch Insert with Helper

```csharp
public class SqlServerDataSource : IDataSource
{
    public IDataSourceHelper Helper { get; set; }
    
    public IErrorsInfo InsertBatch(string entityName, List<Dictionary<string, object>> records)
    {
        IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
        
        try
        {
            // Begin transaction (if supported)
            if (Helper.Capabilities.SupportsTransactions)
            {
                var (beginSql, _, _) = Helper.GenerateBeginTransactionSql();
                ExecuteNonQuery(beginSql);
            }
            
            // Generate and execute INSERT for each record
            foreach (var record in records)
            {
                var (insertSql, success, error) = Helper.GenerateInsertSql(entityName, record);
                
                if (success)
                    ExecuteNonQuery(insertSql);
                else
                    Logger.WriteLog($"Failed to generate INSERT: {error}");
            }
            
            // Commit transaction (if supported)
            if (Helper.Capabilities.SupportsTransactions)
            {
                var (commitSql, _, _) = Helper.GenerateCommitSql();
                ExecuteNonQuery(commitSql);
            }
            
            retval.Flag = Errors.Ok;
            retval.Message = $"Inserted {records.Count} records";
        }
        catch (Exception ex)
        {
            // Rollback on error (if supported)
            if (Helper.Capabilities.SupportsTransactions)
            {
                try
                {
                    var (rollbackSql, _, _) = Helper.GenerateRollbackSql();
                    ExecuteNonQuery(rollbackSql);
                }
                catch { /* ignore rollback failures */ }
            }
            
            retval.Flag = Errors.Failed;
            retval.Message = ex.Message;
            retval.Ex = ex;
        }
        
        return retval;
    }
}
```

---

## 3. Schema Introspection

### Pattern: Discover Table Schema

```csharp
public class PostgreSqlDataSource : IDataSource
{
    public IDataSourceHelper Helper { get; set; }
    
    public EntityStructure GetTableStructure(string tableName)
    {
        var entity = new EntityStructure { EntityName = tableName };
        
        try
        {
            // Check if table exists
            var (existsQuery, _, _) = Helper.GetTableExistsQuery(tableName, "public");
            var exists = ExecuteScalar(existsQuery);
            
            if (exists == null || (int)exists == 0)
                return null;
            
            // Get column information
            var (columnQuery, _, _) = Helper.GetColumnInfoQuery(tableName, "public");
            var columnData = ExecuteDataTable(columnQuery);
            
            // Parse columns
            foreach (DataRow row in columnData.Rows)
            {
                var field = new EntityField
                {
                    FieldName = row["column_name"].ToString(),
                    FieldType = row["data_type"].ToString(),
                    AllowNull = (bool)row["is_nullable"],
                    DefaultValue = row["column_default"]?.ToString()
                };
                
                entity.EntityFields.Add(field);
            }
            
            // Get primary key
            var (pkQuery, _, _) = Helper.GetPrimaryKeyQuery(tableName);
            var pkData = ExecuteDataTable(pkQuery);
            
            foreach (DataRow row in pkData.Rows)
            {
                var pkField = entity.EntityFields.FirstOrDefault(
                    f => f.FieldName == row["column_name"].ToString()
                );
                
                if (pkField != null)
                    pkField.IsIdentity = true;
            }
            
            // Get foreign keys (if supported)
            if (Helper.Capabilities.SupportsConstraints)
            {
                var (fkQuery, _, _) = Helper.GetForeignKeysQuery(tableName);
                var fkData = ExecuteDataTable(fkQuery);
                
                foreach (DataRow row in fkData.Rows)
                {
                    var relation = new EntityRelation
                    {
                        ParentEntityName = row["referenced_table"].ToString(),
                        ChildColumnName = row["column_name"].ToString(),
                        ParentColumnName = row["referenced_column"].ToString()
                    };
                    
                    entity.Relations.Add(relation);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.WriteLog($"Error getting table structure: {ex.Message}");
        }
        
        return entity;
    }
}
```

---

## 4. Capability-Aware Operations

### Pattern: Feature Detection Before Execution

```csharp
public class DataSourceBase : IDataSource
{
    public IDataSourceHelper Helper { get; set; }
    
    public IErrorsInfo AddColumn(string tableName, EntityField newColumn)
    {
        IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
        
        // Check capability
        if (!Helper.Capabilities.SupportsAlteringSchema)
        {
            retval.Flag = Errors.Warning;
            retval.Message = "Datasource does not support schema alteration. Column not added.";
            return retval;
        }
        
        try
        {
            var (addColSql, success, error) = Helper.GenerateAddColumnSql(tableName, newColumn);
            
            if (!success)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Failed to generate ADD COLUMN: {error}";
                return retval;
            }
            
            ExecuteNonQuery(addColSql);
            retval.Flag = Errors.Ok;
            retval.Message = $"Column '{newColumn.FieldName}' added to '{tableName}'";
        }
        catch (Exception ex)
        {
            retval.Flag = Errors.Failed;
            retval.Message = ex.Message;
            retval.Ex = ex;
        }
        
        return retval;
    }
    
    public IErrorsInfo CreateIndex(string tableName, string indexName, string[] columnNames, bool isUnique = false)
    {
        IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
        
        // Check capability
        if (!Helper.Capabilities.SupportsIndexing)
        {
            retval.Flag = Errors.Warning;
            retval.Message = "Datasource does not support indexing. Index not created.";
            return retval;
        }
        
        try
        {
            var (indexSql, success, error) = Helper.GenerateCreateIndexSql(
                tableName, indexName, columnNames, isUnique
            );
            
            if (!success)
            {
                retval.Flag = Errors.Failed;
                retval.Message = $"Failed to generate CREATE INDEX: {error}";
                return retval;
            }
            
            ExecuteNonQuery(indexSql);
            retval.Flag = Errors.Ok;
            retval.Message = $"Index '{indexName}' created on '{tableName}'";
        }
        catch (Exception ex)
        {
            retval.Flag = Errors.Failed;
            retval.Message = ex.Message;
            retval.Ex = ex;
        }
        
        return retval;
    }
}
```

---

## 5. Data Integrity & Constraints

### Pattern: Add Primary Key Constraint

```csharp
public class RdbmsDataSource : IDataSource
{
    public IDataSourceHelper Helper { get; set; }
    
    public IErrorsInfo DefineKeyStructure(string tableName, EntityStructure entity)
    {
        IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
        
        try
        {
            // Check if datasource supports constraints
            if (!Helper.Capabilities.SupportsConstraints)
            {
                retval.Flag = Errors.Warning;
                retval.Message = "Datasource does not support constraint definitions";
                return retval;
            }
            
            // Add primary key
            var pkFields = entity.EntityFields.Where(f => f.IsIdentity).ToArray();
            if (pkFields.Length > 0)
            {
                var (pkSql, success, error) = Helper.GenerateAddPrimaryKeySql(
                    tableName,
                    pkFields.Select(f => f.FieldName).ToArray()
                );
                
                if (success)
                {
                    ExecuteNonQuery(pkSql);
                    Logger.WriteLog($"Primary key added to '{tableName}'");
                }
                else
                {
                    Logger.WriteLog($"Warning: Could not add primary key: {error}");
                }
            }
            
            // Add foreign keys
            if (entity.Relations != null && entity.Relations.Count > 0)
            {
                foreach (var relation in entity.Relations)
                {
                    var (fkSql, success, error) = Helper.GenerateAddForeignKeySql(
                        tableName,
                        new[] { relation.ChildColumnName },
                        relation.ParentEntityName,
                        new[] { relation.ParentColumnName }
                    );
                    
                    if (success)
                    {
                        ExecuteNonQuery(fkSql);
                        Logger.WriteLog($"Foreign key added from '{tableName}' to '{relation.ParentEntityName}'");
                    }
                    else
                    {
                        Logger.WriteLog($"Warning: Could not add foreign key: {error}");
                    }
                }
            }
            
            retval.Flag = Errors.Ok;
            retval.Message = "Key structure defined";
        }
        catch (Exception ex)
        {
            retval.Flag = Errors.Failed;
            retval.Message = ex.Message;
            retval.Ex = ex;
        }
        
        return retval;
    }
}
```

---

## 6. Schema Evolution & Maintenance

### Pattern: Rename Table for Refactoring

```csharp
public class DatabaseMaintenanceService
{
    public IDataSourceHelper Helper { get; set; }
    public IDataSource DataSource { get; set; }
    
    public IErrorsInfo RefactorTableName(string oldName, string newName)
    {
        IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
        
        try
        {
            // Check capability
            if (!Helper.Capabilities.SupportsAlteringSchema)
            {
                retval.Flag = Errors.Warning;
                retval.Message = "Datasource does not support table renaming";
                return retval;
            }
            
            var (renameSql, success, error) = Helper.GenerateRenameTableSql(oldName, newName);
            
            if (!success)
            {
                retval.Flag = Errors.Failed;
                retval.Message = error;
                return retval;
            }
            
            DataSource.ExecuteNonQuery(renameSql);
            retval.Flag = Errors.Ok;
            retval.Message = $"Table '{oldName}' renamed to '{newName}'";
        }
        catch (Exception ex)
        {
            retval.Flag = Errors.Failed;
            retval.Message = ex.Message;
            retval.Ex = ex;
        }
        
        return retval;
    }
    
    public IErrorsInfo AddColumnToTable(string tableName, EntityField field)
    {
        IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
        
        try
        {
            if (!Helper.Capabilities.SupportsAlteringSchema)
            {
                retval.Flag = Errors.Warning;
                retval.Message = "Datasource does not support schema changes";
                return retval;
            }
            
            var (addSql, success, error) = Helper.GenerateAddColumnSql(tableName, field);
            
            if (!success)
            {
                retval.Flag = Errors.Failed;
                retval.Message = error;
                return retval;
            }
            
            DataSource.ExecuteNonQuery(addSql);
            retval.Flag = Errors.Ok;
            retval.Message = $"Column '{field.FieldName}' added to '{tableName}'";
        }
        catch (Exception ex)
        {
            retval.Flag = Errors.Failed;
            retval.Message = ex.Message;
            retval.Ex = ex;
        }
        
        return retval;
    }
    
    public IErrorsInfo DropObsoleteColumn(string tableName, string columnName)
    {
        IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
        
        try
        {
            if (!Helper.Capabilities.SupportsAlteringSchema)
            {
                retval.Flag = Errors.Warning;
                retval.Message = "Datasource does not support schema changes";
                return retval;
            }
            
            var (dropSql, success, error) = Helper.GenerateDropColumnSql(tableName, columnName);
            
            if (!success)
            {
                retval.Flag = Errors.Failed;
                retval.Message = error;
                return retval;
            }
            
            DataSource.ExecuteNonQuery(dropSql);
            retval.Flag = Errors.Ok;
            retval.Message = $"Column '{columnName}' dropped from '{tableName}'";
        }
        catch (Exception ex)
        {
            retval.Flag = Errors.Failed;
            retval.Message = ex.Message;
            retval.Ex = ex;
        }
        
        return retval;
    }
}
```

---

## 7. Transaction Management

### Pattern: Atomic Operations with Savepoints

```csharp
public class TransactionManager
{
    public IDataSourceHelper Helper { get; set; }
    public IDataSource DataSource { get; set; }
    
    public IErrorsInfo ExecuteAtomicBatchOperation(Action<TransactionScope> operation)
    {
        IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
        
        // Check capability
        if (!Helper.Capabilities.SupportsTransactions)
        {
            retval.Flag = Errors.Warning;
            retval.Message = "Datasource does not support transactions. Operation executed without atomicity.";
            
            try
            {
                operation(new TransactionScope { Helper = Helper, DataSource = DataSource });
            }
            catch (Exception ex)
            {
                retval.Flag = Errors.Failed;
                retval.Message = ex.Message;
                retval.Ex = ex;
            }
            
            return retval;
        }
        
        try
        {
            // Begin transaction
            var (beginSql, _, _) = Helper.GenerateBeginTransactionSql();
            DataSource.ExecuteNonQuery(beginSql);
            
            // Execute operation
            var scope = new TransactionScope { Helper = Helper, DataSource = DataSource };
            operation(scope);
            
            // Commit on success
            var (commitSql, _, _) = Helper.GenerateCommitSql();
            DataSource.ExecuteNonQuery(commitSql);
            
            retval.Flag = Errors.Ok;
            retval.Message = "Transaction committed successfully";
        }
        catch (Exception ex)
        {
            // Rollback on error
            try
            {
                var (rollbackSql, _, _) = Helper.GenerateRollbackSql();
                DataSource.ExecuteNonQuery(rollbackSql);
            }
            catch { /* ignore rollback errors */ }
            
            retval.Flag = Errors.Failed;
            retval.Message = $"Transaction rolled back: {ex.Message}";
            retval.Ex = ex;
        }
        
        return retval;
    }
}

public class TransactionScope
{
    public IDataSourceHelper Helper { get; set; }
    public IDataSource DataSource { get; set; }
    
    public void CreateSavepoint(string name)
    {
        if (Helper.Capabilities.SupportsTransactions)
        {
            var (savepointSql, success, _) = Helper.GenerateSavepointSql(name);
            if (success)
                DataSource.ExecuteNonQuery(savepointSql);
        }
    }
    
    public void RollbackToSavepoint(string name)
    {
        if (Helper.Capabilities.SupportsTransactions)
        {
            var (rollbackSql, success, _) = Helper.GenerateRollbackToSavepointSql(name);
            if (success)
                DataSource.ExecuteNonQuery(rollbackSql);
        }
    }
}
```

---

## 8. DML Batch Operations

### Pattern: Bulk Update with Transaction

```csharp
public class BulkOperationService
{
    public IDataSourceHelper Helper { get; set; }
    public IDataSource DataSource { get; set; }
    
    public IErrorsInfo BulkUpdate(string entityName, List<Dictionary<string, object>> records, string whereClause)
    {
        IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
        
        try
        {
            // Start transaction if supported
            bool useTransaction = Helper.Capabilities.SupportsTransactions;
            if (useTransaction)
            {
                var (beginSql, _, _) = Helper.GenerateBeginTransactionSql();
                DataSource.ExecuteNonQuery(beginSql);
            }
            
            int updatedCount = 0;
            
            foreach (var record in records)
            {
                var (updateSql, success, error) = Helper.GenerateUpdateSql(
                    entityName, record, whereClause
                );
                
                if (success)
                {
                    DataSource.ExecuteNonQuery(updateSql);
                    updatedCount++;
                }
                else
                {
                    Logger.WriteLog($"Update failed: {error}");
                }
            }
            
            // Commit if transaction was used
            if (useTransaction)
            {
                var (commitSql, _, _) = Helper.GenerateCommitSql();
                DataSource.ExecuteNonQuery(commitSql);
            }
            
            retval.Flag = Errors.Ok;
            retval.Message = $"Updated {updatedCount} records";
        }
        catch (Exception ex)
        {
            // Rollback on error
            if (Helper.Capabilities.SupportsTransactions)
            {
                try
                {
                    var (rollbackSql, _, _) = Helper.GenerateRollbackSql();
                    DataSource.ExecuteNonQuery(rollbackSql);
                }
                catch { /* ignore */ }
            }
            
            retval.Flag = Errors.Failed;
            retval.Message = ex.Message;
            retval.Ex = ex;
        }
        
        return retval;
    }
    
    public IErrorsInfo BulkDelete(string entityName, List<string> whereConditions)
    {
        IErrorsInfo retval = new ErrorsInfo { Flag = Errors.Ok };
        
        try
        {
            bool useTransaction = Helper.Capabilities.SupportsTransactions;
            if (useTransaction)
            {
                var (beginSql, _, _) = Helper.GenerateBeginTransactionSql();
                DataSource.ExecuteNonQuery(beginSql);
            }
            
            int deletedCount = 0;
            
            foreach (var whereClause in whereConditions)
            {
                var (deleteSql, success, error) = Helper.GenerateDeleteSql(entityName, whereClause);
                
                if (success)
                {
                    DataSource.ExecuteNonQuery(deleteSql);
                    deletedCount++;
                }
            }
            
            if (useTransaction)
            {
                var (commitSql, _, _) = Helper.GenerateCommitSql();
                DataSource.ExecuteNonQuery(commitSql);
            }
            
            retval.Flag = Errors.Ok;
            retval.Message = $"Deleted {deletedCount} record sets";
        }
        catch (Exception ex)
        {
            if (Helper.Capabilities.SupportsTransactions)
            {
                try
                {
                    var (rollbackSql, _, _) = Helper.GenerateRollbackSql();
                    DataSource.ExecuteNonQuery(rollbackSql);
                }
                catch { /* ignore */ }
            }
            
            retval.Flag = Errors.Failed;
            retval.Message = ex.Message;
            retval.Ex = ex;
        }
        
        return retval;
    }
}
```

---

## 9. Integration with Data Source Discovery

### Pattern: Automatic Entity Registration

```csharp
public class EntityDiscoveryService
{
    public IDataSourceHelper Helper { get; set; }
    public IDataSource DataSource { get; set; }
    
    public List<EntityStructure> DiscoverAllTables()
    {
        var entities = new List<EntityStructure>();
        
        try
        {
            // Get schema query
            var (schemaQuery, success, error) = Helper.GetSchemaQuery(null, DataSource.DataSourceType);
            
            if (!success)
            {
                Logger.WriteLog($"Cannot discover schema: {error}");
                return entities;
            }
            
            var tables = DataSource.ExecuteDataTable(schemaQuery);
            
            foreach (DataRow row in tables.Rows)
            {
                var tableName = row["TABLE_NAME"]?.ToString();
                if (string.IsNullOrEmpty(tableName))
                    continue;
                
                // Get table structure using helper
                var entity = new EntityStructure { EntityName = tableName };
                
                var (colQuery, colSuccess, colError) = Helper.GetColumnInfoQuery(tableName);
                if (colSuccess)
                {
                    var columns = DataSource.ExecuteDataTable(colQuery);
                    
                    foreach (DataRow colRow in columns.Rows)
                    {
                        var field = new EntityField
                        {
                            FieldName = colRow["COLUMN_NAME"]?.ToString(),
                            FieldType = colRow["DATA_TYPE"]?.ToString(),
                            AllowNull = (bool?)colRow["IS_NULLABLE"] ?? true
                        };
                        
                        entity.EntityFields.Add(field);
                    }
                }
                
                entities.Add(entity);
            }
        }
        catch (Exception ex)
        {
            Logger.WriteLog($"Error discovering entities: {ex.Message}");
        }
        
        return entities;
    }
}
```

---

## 10. Validation & Capability Checking

### Pattern: Pre-flight Checks Before Operations

```csharp
public class DataSourceValidator
{
    public IDataSourceHelper Helper { get; set; }
    public IDataSource DataSource { get; set; }
    
    public ValidationReport ValidateCapabilities()
    {
        var report = new ValidationReport();
        
        // Check all capabilities
        var caps = new[]
        {
            CapabilityType.CreateSchema,
            CapabilityType.SupportsConstraints,
            CapabilityType.SupportsTransactions,
            CapabilityType.SupportsIndexing,
            CapabilityType.SupportsAlteringSchema
        };
        
        foreach (var cap in caps)
        {
            var (_, success, _) = Helper.SupportsCapability(cap);
            report.SupportedCapabilities.Add(cap, success);
        }
        
        return report;
    }
    
    public bool CanPerformOperation(OperationType operation)
    {
        return operation switch
        {
            OperationType.CreateTable => Helper.Capabilities.CreateSchema,
            OperationType.AddPrimaryKey => Helper.Capabilities.SupportsConstraints,
            OperationType.CreateIndex => Helper.Capabilities.SupportsIndexing,
            OperationType.AlterColumn => Helper.Capabilities.SupportsAlteringSchema,
            OperationType.Transaction => Helper.Capabilities.SupportsTransactions,
            _ => false
        };
    }
}

public enum OperationType
{
    CreateTable,
    AddPrimaryKey,
    CreateIndex,
    AlterColumn,
    Transaction
}

public class ValidationReport
{
    public Dictionary<CapabilityType, bool> SupportedCapabilities { get; set; } = new();
    
    public string GetReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Datasource Capability Report ===");
        
        foreach (var (cap, supported) in SupportedCapabilities)
        {
            sb.AppendLine($"{cap}: {(supported ? "✓ Supported" : "✗ Not Supported")}");
        }
        
        return sb.ToString();
    }
}
```

---

## Summary: IDataSourceHelper Usage Areas

| Area | Helper Methods | Benefit |
|------|----------------|---------|
| **Type Mapping** | `MapClrTypeToDatasourceType()`, `MapDatasourceTypeToClrType()` | Consistent type conversion across datasources |
| **Validation** | `ValidateEntity()`, `SupportsCapability()` | Pre-flight checks before operations |
| **SQL Generation** | `GenerateCreateTableSql()`, `GenerateInsertSql()`, etc. | Safe, RDBMS-agnostic SQL creation |
| **Schema Discovery** | `GetTableExistsQuery()`, `GetColumnInfoQuery()`, `GetSchemaQuery()` | Automated entity discovery |
| **Identifier Quoting** | `QuoteIdentifier()` | Handle reserved words & special characters |
| **Constraints** | `GenerateAddPrimaryKeySql()`, `GenerateAddForeignKeySql()` | Data integrity enforcement |
| **Transactions** | `GenerateBeginTransactionSql()`, `GenerateCommitSql()`, `GenerateRollbackSql()` | ACID compliance |
| **Schema Evolution** | `GenerateAddColumnSql()`, `GenerateRenameTableSql()`, `GenerateDropColumnSql()` | Runtime schema changes |
| **Capability Checking** | `Capabilities` property | Graceful feature degradation |

---

**Key Principle**: IDataSourceHelper enables IDataSource implementations to leverage unified, datasource-agnostic methods while maintaining compatibility with diverse RDBMS platforms. Each datasource can implement specialized behavior while inheriting consistent patterns from the helper interface.
