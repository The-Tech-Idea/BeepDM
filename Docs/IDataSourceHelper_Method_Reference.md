# IDataSourceHelper - Complete Method Reference

## Interface Definition Summary

**Location**: `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/Core/IDataSourceHelper.cs`

**Total Methods**: 24 (enhanced from 11)

**Return Type**: All methods return `(string Sql, bool Success, string ErrorMessage)` tuple

**Available Since**: Session 6 Enhancement

---

## Method Categories

### 1️⃣ UNIVERSAL OPERATIONS (2 methods)

#### SupportsCapability()
```csharp
(string Sql, bool Success, string ErrorMessage) SupportsCapability(CapabilityType capability);
```
- **Purpose**: Check if datasource supports a specific capability
- **Parameters**: 
  - `capability` - CapabilityType enum value
- **Returns**: Tuple with (description, true, "")
- **Example**: `if (helper.SupportsCapability(CapabilityType.SupportsTransactions).Success)`
- **Use Case**: Conditional execution based on capabilities

#### ValidateEntity()
```csharp
(bool IsValid, string ErrorMessage) ValidateEntity(EntityStructure entityStructure);
```
- **Purpose**: Validate entity structure before DDL execution
- **Parameters**:
  - `entityStructure` - EntityStructure object with fields and metadata
- **Returns**: Tuple with (isValid, errorMessage)
- **Example**: `var (valid, msg) = helper.ValidateEntity(entity);`
- **Use Case**: Pre-validation before CreateEntityAs

---

### 2️⃣ SCHEMA OPERATIONS (3 methods)

#### GetSchemaQuery()
```csharp
(string Sql, bool Success, string ErrorMessage) GetSchemaQuery(string entityName, DataSourceType dataSourceType = null);
```
- **Purpose**: Get query to retrieve schema information for entity
- **Parameters**:
  - `entityName` - Name of table/collection
  - `dataSourceType` - Type of datasource (optional)
- **Returns**: Query string and status
- **Example**: `var (query, _, _) = helper.GetSchemaQuery("Products");`
- **Use Case**: Retrieve table structure from database

#### GetTableExistsQuery()
```csharp
(string Sql, bool Success, string ErrorMessage) GetTableExistsQuery(string tableName, string schemaName = "dbo", DataSourceType dataSourceType = null);
```
- **Purpose**: Check if table exists in database
- **Parameters**:
  - `tableName` - Name of table to check
  - `schemaName` - Schema name (default: "dbo")
  - `dataSourceType` - Type of datasource (optional)
- **Returns**: Query string that returns 1 if exists, 0 if not
- **Example**: `var (query, _, _) = helper.GetTableExistsQuery("Products", "dbo");`
- **Use Case**: Conditional table creation

#### GetColumnInfoQuery()
```csharp
(string Sql, bool Success, string ErrorMessage) GetColumnInfoQuery(string tableName, string schemaName = "dbo", DataSourceType dataSourceType = null);
```
- **Purpose**: Get query to retrieve column information
- **Parameters**:
  - `tableName` - Name of table
  - `schemaName` - Schema name (default: "dbo")
  - `dataSourceType` - Type of datasource (optional)
- **Returns**: Query string
- **Example**: `var (query, _, _) = helper.GetColumnInfoQuery("Products");`
- **Use Case**: Get all columns, data types, constraints for table

---

### 3️⃣ DDL OPERATIONS - LEVEL 1: TABLE/INDEX (8 methods)

#### GenerateCreateTableSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateCreateTableSql(
    string schemaName,
    string tableName,
    List<EntityField> fields,
    DataSourceType dataSourceType = null);
```
- **Purpose**: Generate CREATE TABLE statement
- **Parameters**:
  - `schemaName` - Schema/database name (e.g., "dbo", "public")
  - `tableName` - Table name
  - `fields` - List of EntityField objects defining columns
  - `dataSourceType` - Type of datasource (optional)
- **Returns**: SQL CREATE TABLE statement
- **Example**: 
```csharp
var (sql, success, _) = helper.GenerateCreateTableSql(
    "dbo", "Products", fields, DataSourceType.SqlServer
);
// CREATE TABLE [dbo].[Products] ([Id] INT PRIMARY KEY, [Name] NVARCHAR(100), ...)
```
- **Use Case**: Table creation from EntityStructure

#### GenerateDropTableSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateDropTableSql(string tableName, string schemaName = null, DataSourceType dataSourceType = null);
```
- **Purpose**: Generate DROP TABLE statement
- **Parameters**:
  - `tableName` - Table name
  - `schemaName` - Schema name (optional)
  - `dataSourceType` - Type of datasource (optional)
- **Returns**: SQL DROP TABLE statement
- **Example**: `var (sql, _, _) = helper.GenerateDropTableSql("Products");`
- **Use Case**: Table deletion

#### GenerateTruncateTableSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateTruncateTableSql(string tableName, string schemaName = null, DataSourceType dataSourceType = null);
```
- **Purpose**: Generate TRUNCATE TABLE statement
- **Parameters**:
  - `tableName` - Table name
  - `schemaName` - Schema name (optional)
  - `dataSourceType` - Type of datasource (optional)
- **Returns**: SQL TRUNCATE TABLE statement
- **Example**: `var (sql, _, _) = helper.GenerateTruncateTableSql("Products");`
- **Use Case**: Delete all table data without dropping table

#### GenerateCreateIndexSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateCreateIndexSql(
    string tableName,
    string indexName,
    string[] columnNames,
    bool isUnique = false,
    string schemaName = null,
    DataSourceType dataSourceType = null);
```
- **Purpose**: Generate CREATE INDEX statement
- **Parameters**:
  - `tableName` - Table name
  - `indexName` - Index name (e.g., "IX_Products_Name")
  - `columnNames` - Array of column names to index
  - `isUnique` - Whether index is unique (default: false)
  - `schemaName` - Schema name (optional)
  - `dataSourceType` - Type of datasource (optional)
- **Returns**: SQL CREATE INDEX statement
- **Example**:
```csharp
var (sql, _, _) = helper.GenerateCreateIndexSql(
    "Products", "IX_Products_Name", new[] { "Name" }, false
);
// CREATE INDEX IX_Products_Name ON [Products] ([Name])
```
- **Use Case**: Create index on columns for query optimization

---

### 4️⃣ DDL OPERATIONS - LEVEL 2: COLUMNS (5 methods)

#### GenerateAddColumnSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateAddColumnSql(string tableName, EntityField column);
```
- **Purpose**: Add a column to existing table
- **Parameters**:
  - `tableName` - Table name
  - `column` - EntityField with column definition
- **Returns**: SQL ALTER TABLE ADD COLUMN statement
- **Example**:
```csharp
var (sql, _, _) = helper.GenerateAddColumnSql(
    "Products",
    new EntityField { FieldName = "Description", FieldType = "String", Size = 500 }
);
// ALTER TABLE [Products] ADD [Description] NVARCHAR(500) NULL
```
- **Use Case**: Schema evolution - add columns

#### GenerateAlterColumnSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateAlterColumnSql(
    string tableName,
    string columnName,
    EntityField newColumn);
```
- **Purpose**: Modify column definition
- **Parameters**:
  - `tableName` - Table name
  - `columnName` - Current column name
  - `newColumn` - New column definition
- **Returns**: SQL ALTER TABLE ALTER COLUMN statement
- **Example**:
```csharp
var (sql, _, _) = helper.GenerateAlterColumnSql(
    "Products", "Price",
    new EntityField { FieldName = "Price", FieldType = "Decimal", Size = 18 }
);
// ALTER TABLE [Products] ALTER COLUMN [Price] DECIMAL(18,2) NOT NULL
```
- **Use Case**: Change column data types, NULL constraints

#### GenerateDropColumnSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateDropColumnSql(string tableName, string columnName);
```
- **Purpose**: Remove column from table
- **Parameters**:
  - `tableName` - Table name
  - `columnName` - Column name
- **Returns**: SQL ALTER TABLE DROP COLUMN statement
- **Example**: `var (sql, _, _) = helper.GenerateDropColumnSql("Products", "LegacyField");`
- **Use Case**: Remove obsolete columns

#### GenerateRenameTableSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateRenameTableSql(string oldTableName, string newTableName);
```
- **Purpose**: Rename table
- **Parameters**:
  - `oldTableName` - Current table name
  - `newTableName` - New table name
- **Returns**: SQL RENAME statement
- **Example**: `var (sql, _, _) = helper.GenerateRenameTableSql("OldProducts", "Products");`
- **Use Case**: Refactoring table names
- **Note**: Syntax varies by RDBMS (SQL Server uses sp_rename, MySQL uses RENAME TABLE)

#### GenerateRenameColumnSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateRenameColumnSql(
    string tableName,
    string oldColumnName,
    string newColumnName);
```
- **Purpose**: Rename column
- **Parameters**:
  - `tableName` - Table name
  - `oldColumnName` - Current column name
  - `newColumnName` - New column name
- **Returns**: SQL RENAME COLUMN statement
- **Example**: `var (sql, _, _) = helper.GenerateRenameColumnSql("Products", "ProductCode", "Code");`
- **Use Case**: Refactoring column names
- **Note**: Syntax varies by RDBMS

---

### 5️⃣ CONSTRAINT OPERATIONS - LEVEL 3 (6 methods)

#### GenerateAddPrimaryKeySql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateAddPrimaryKeySql(
    string tableName,
    params string[] columnNames);
```
- **Purpose**: Create PRIMARY KEY constraint
- **Parameters**:
  - `tableName` - Table name
  - `columnNames` - Column(s) to be primary key
- **Returns**: SQL ALTER TABLE ADD CONSTRAINT statement
- **Example**:
```csharp
var (sql, _, _) = helper.GenerateAddPrimaryKeySql("Products", "Id");
// ALTER TABLE [Products] ADD CONSTRAINT PK_Products_Id PRIMARY KEY ([Id])
```
- **Use Case**: Define primary key on table

#### GenerateAddForeignKeySql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateAddForeignKeySql(
    string tableName,
    string[] columnNames,
    string referencedTableName,
    string[] referencedColumnNames);
```
- **Purpose**: Create FOREIGN KEY constraint
- **Parameters**:
  - `tableName` - Table name
  - `columnNames` - Column(s) referencing foreign table
  - `referencedTableName` - Referenced (parent) table
  - `referencedColumnNames` - Referenced column(s)
- **Returns**: SQL ALTER TABLE ADD CONSTRAINT FOREIGN KEY statement
- **Example**:
```csharp
var (sql, _, _) = helper.GenerateAddForeignKeySql(
    "Orders", new[] { "CustomerId" },
    "Customers", new[] { "Id" }
);
// ALTER TABLE [Orders] ADD CONSTRAINT FK_Orders_Customers_CustomerId 
// FOREIGN KEY ([CustomerId]) REFERENCES Customers ([Id]) 
// ON DELETE CASCADE ON UPDATE CASCADE
```
- **Use Case**: Define relationships between tables

#### GenerateAddConstraintSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateAddConstraintSql(
    string tableName,
    string constraintName,
    string constraintDefinition);
```
- **Purpose**: Generic constraint creation (UNIQUE, CHECK, etc.)
- **Parameters**:
  - `tableName` - Table name
  - `constraintName` - Constraint name
  - `constraintDefinition` - Constraint definition (e.g., "UNIQUE (Email)" or "CHECK (Age > 0)")
- **Returns**: SQL ALTER TABLE ADD CONSTRAINT statement
- **Example**:
```csharp
var (sql, _, _) = helper.GenerateAddConstraintSql(
    "Users", "UQ_Users_Email",
    "UNIQUE (Email)"
);
// ALTER TABLE [Users] ADD CONSTRAINT UQ_Users_Email UNIQUE (Email)
```
- **Use Case**: Create UNIQUE, CHECK, or other constraints

#### GetPrimaryKeyQuery()
```csharp
(string Sql, bool Success, string ErrorMessage) GetPrimaryKeyQuery(string tableName, string schemaName = "dbo");
```
- **Purpose**: Query to get PRIMARY KEY information
- **Parameters**:
  - `tableName` - Table name
  - `schemaName` - Schema name (default: "dbo")
- **Returns**: SQL query to retrieve PK details
- **Example**: `var (query, _, _) = helper.GetPrimaryKeyQuery("Products");`
- **Use Case**: Inspect primary key of existing table

#### GetForeignKeysQuery()
```csharp
(string Sql, bool Success, string ErrorMessage) GetForeignKeysQuery(string tableName, string schemaName = "dbo");
```
- **Purpose**: Query to get FOREIGN KEY information
- **Parameters**:
  - `tableName` - Table name
  - `schemaName` - Schema name (default: "dbo")
- **Returns**: SQL query to retrieve FK details
- **Example**: `var (query, _, _) = helper.GetForeignKeysQuery("Orders");`
- **Use Case**: Inspect relationships of existing table

#### GetConstraintsQuery()
```csharp
(string Sql, bool Success, string ErrorMessage) GetConstraintsQuery(string tableName, string schemaName = "dbo");
```
- **Purpose**: Query to get all constraints
- **Parameters**:
  - `tableName` - Table name
  - `schemaName` - Schema name (default: "dbo")
- **Returns**: SQL query to retrieve all constraints
- **Example**: `var (query, _, _) = helper.GetConstraintsQuery("Users");`
- **Use Case**: Inspect all constraints (UNIQUE, CHECK, etc.)

---

### 6️⃣ TRANSACTION OPERATIONS - LEVEL 4 (3 methods)

#### GenerateBeginTransactionSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateBeginTransactionSql();
```
- **Purpose**: Start transaction
- **Returns**: SQL BEGIN TRANSACTION statement
- **Example**: `var (sql, _, _) = helper.GenerateBeginTransactionSql();`
- **Output**: "BEGIN TRANSACTION"
- **Use Case**: Wrap DDL/DML in transaction for ACID compliance
- **Note**: Works across all RDBMS

#### GenerateCommitSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateCommitSql();
```
- **Purpose**: Commit transaction
- **Returns**: SQL COMMIT statement
- **Example**: `var (sql, _, _) = helper.GenerateCommitSql();`
- **Output**: "COMMIT"
- **Use Case**: Finalize transaction changes
- **Note**: Works across all RDBMS

#### GenerateRollbackSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateRollbackSql();
```
- **Purpose**: Rollback transaction
- **Returns**: SQL ROLLBACK statement
- **Example**: `var (sql, _, _) = helper.GenerateRollbackSql();`
- **Output**: "ROLLBACK"
- **Use Case**: Undo transaction changes on error
- **Note**: Works across all RDBMS

---

### 7️⃣ DML OPERATIONS (5 methods - EXISTING)

#### GenerateInsertSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateInsertSql(
    string tableName,
    Dictionary<string, object> values);
```
- **Purpose**: Generate INSERT statement
- **Note**: Existing method, works with new enhancements

#### GenerateUpdateSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateUpdateSql(
    string tableName,
    Dictionary<string, object> values,
    string whereClause);
```
- **Purpose**: Generate UPDATE statement
- **Note**: Existing method, works with new enhancements

#### GenerateDeleteSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateDeleteSql(
    string tableName,
    string whereClause);
```
- **Purpose**: Generate DELETE statement
- **Note**: Existing method, works with new enhancements

#### GenerateSelectSql()
```csharp
(string Sql, bool Success, string ErrorMessage) GenerateSelectSql(
    string tableName,
    string[] columns = null,
    string whereClause = null);
```
- **Purpose**: Generate SELECT statement
- **Note**: Existing method, works with new enhancements

#### QuoteIdentifier()
```csharp
string QuoteIdentifier(string identifier);
```
- **Purpose**: Quote identifier for safe SQL (handles reserved words)
- **Example**: `QuoteIdentifier("Products")` → `[Products]` (SQL Server)
- **Note**: Existing method, handles RDBMS-specific quoting

---

### 8️⃣ UTILITY OPERATIONS (6 methods)

#### MapClrTypeToDatasourceType()
```csharp
string MapClrTypeToDatasourceType(
    Type clrType,
    int? size = null,
    int? precision = null,
    int? scale = null);
```
- **Purpose**: Map .NET CLR type to datasource type
- **Parameters**:
  - `clrType` - CLR type (e.g., typeof(string), typeof(int))
  - `size` - Size for string types (e.g., 100 for NVARCHAR(100))
  - `precision` - Precision for numeric types (e.g., 18)
  - `scale` - Scale for numeric types (e.g., 2 for 18,2)
- **Returns**: Datasource type string (e.g., "NVARCHAR(100)", "INT", "DECIMAL(18,2)")
- **Example**:
```csharp
var sqlType = helper.MapClrTypeToDatasourceType(typeof(string), 100);
// Returns: "NVARCHAR(100)"

var sqlType = helper.MapClrTypeToDatasourceType(typeof(decimal), 18, 18, 2);
// Returns: "DECIMAL(18,2)"
```
- **Use Case**: Convert POCO field types to DDL type strings

#### MapDatasourceTypeToClrType()
```csharp
Type MapDatasourceTypeToClrType(string datasourceType);
```
- **Purpose**: Map datasource type back to CLR type
- **Example**: `MapDatasourceTypeToClrType("NVARCHAR(100)")` → `typeof(string)`
- **Use Case**: Reverse mapping for schema analysis

#### GetMaxStringSize()
```csharp
int GetMaxStringSize();
```
- **Purpose**: Get maximum string size for datasource
- **Returns**: Max length (e.g., 8000 for SQL Server, unlimited for PostgreSQL)
- **Example**: `var maxSize = helper.GetMaxStringSize();`
- **Use Case**: Validate string field sizes

#### GetMaxNumericPrecision()
```csharp
int GetMaxNumericPrecision();
```
- **Purpose**: Get maximum numeric precision for datasource
- **Returns**: Max precision (e.g., 38 for most RDBMS)
- **Example**: `var maxPrecision = helper.GetMaxNumericPrecision();`
- **Use Case**: Validate numeric field precision

#### QuoteIdentifier()
```csharp
string QuoteIdentifier(string identifier);
```
- **Purpose**: Quote identifier for safe SQL
- **Example**: `QuoteIdentifier("Product Name")` → `[Product Name]`
- **Use Case**: Handle column/table names with spaces or reserved words

#### Capabilities Property
```csharp
DataSourceCapabilities Capabilities { get; set; }
```
- **Purpose**: Access datasource capabilities
- **Properties**: SupportsTransactions, SupportsConstraints, IsSchemaEnforced, etc.
- **Example**: `if (helper.Capabilities.SupportsTransactions) {...}`
- **Use Case**: Conditional feature execution

---

## Quick Reference Table

| Method | Parameters | Returns | Purpose |
|--------|------------|---------|---------|
| **SupportsCapability()** | CapabilityType | (string, bool, string) | Check datasource capability |
| **ValidateEntity()** | EntityStructure | (bool, string) | Validate entity structure |
| **GetSchemaQuery()** | entityName, dsType | (string, bool, string) | Retrieve schema |
| **GetTableExistsQuery()** | tableName, schema, dsType | (string, bool, string) | Check table existence |
| **GetColumnInfoQuery()** | tableName, schema, dsType | (string, bool, string) | Get column information |
| **GenerateCreateTableSql()** | schema, table, fields, dsType | (string, bool, string) | CREATE TABLE |
| **GenerateDropTableSql()** | table, schema, dsType | (string, bool, string) | DROP TABLE |
| **GenerateTruncateTableSql()** | table, schema, dsType | (string, bool, string) | TRUNCATE TABLE |
| **GenerateCreateIndexSql()** | table, index, cols, unique, schema, dsType | (string, bool, string) | CREATE INDEX |
| **GenerateAddColumnSql()** | table, column | (string, bool, string) | ALTER TABLE ADD COLUMN |
| **GenerateAlterColumnSql()** | table, col, newCol | (string, bool, string) | ALTER COLUMN |
| **GenerateDropColumnSql()** | table, column | (string, bool, string) | DROP COLUMN |
| **GenerateRenameTableSql()** | oldName, newName | (string, bool, string) | RENAME TABLE |
| **GenerateRenameColumnSql()** | table, oldCol, newCol | (string, bool, string) | RENAME COLUMN |
| **GenerateAddPrimaryKeySql()** | table, columns | (string, bool, string) | ADD PRIMARY KEY |
| **GenerateAddForeignKeySql()** | table, cols, refTable, refCols | (string, bool, string) | ADD FOREIGN KEY |
| **GenerateAddConstraintSql()** | table, name, definition | (string, bool, string) | ADD CONSTRAINT |
| **GetPrimaryKeyQuery()** | table, schema | (string, bool, string) | Query PRIMARY KEY |
| **GetForeignKeysQuery()** | table, schema | (string, bool, string) | Query FOREIGN KEYS |
| **GetConstraintsQuery()** | table, schema | (string, bool, string) | Query all constraints |
| **GenerateBeginTransactionSql()** | - | (string, bool, string) | BEGIN TRANSACTION |
| **GenerateCommitSql()** | - | (string, bool, string) | COMMIT |
| **GenerateRollbackSql()** | - | (string, bool, string) | ROLLBACK |

---

## Usage Pattern

```csharp
// 1. Get helper instance
var helper = new RdbmsHelper();

// 2. Check capabilities
if (!helper.SupportsCapability(CapabilityType.SupportsTransactions).Success)
{
    Logger.LogWarning("Transactions not supported");
}

// 3. Validate entity
var (valid, msg) = helper.ValidateEntity(entityStructure);
if (!valid)
{
    throw new InvalidOperationException($"Invalid entity: {msg}");
}

// 4. Begin transaction (if supported)
if (helper.Capabilities.SupportsTransactions)
{
    var (beginSql, _, _) = helper.GenerateBeginTransactionSql();
    ExecuteNonQuery(beginSql);
}

// 5. Execute DDL
var (createSql, success, error) = helper.GenerateCreateTableSql(
    "dbo", "Products", fields, DataSourceType.SqlServer
);

if (success)
{
    ExecuteNonQuery(createSql);
}
else
{
    Logger.LogError($"Failed to create table: {error}");
    throw new Exception(error);
}

// 6. Commit (if supported)
if (helper.Capabilities.SupportsTransactions)
{
    var (commitSql, _, _) = helper.GenerateCommitSql();
    ExecuteNonQuery(commitSql);
}
```

---

**Total Methods**: 24  
**Return Type**: All return `(string Sql, bool Success, string ErrorMessage)`  
**Status**: Ready for implementation  
**Last Updated**: Session 6
