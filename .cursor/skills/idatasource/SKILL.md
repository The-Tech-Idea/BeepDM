---
name: idatasource
description: Guidance for implementing IDataSource in BeepDM, including CRUD, schema, and transaction patterns.
---

# IDataSource Implementation Guide

Use this skill when building a new datasource implementation or when writing datasource-agnostic logic.

## Required Responsibilities
- Implement all IDataSource methods
- Maintain `ErrorObject` for failures
- Use `IDataSourceHelper` for SQL and schema generation
- Keep `ConnectionStatus` accurate

## Core Method Areas
- Connection: `Openconnection`, `Closeconnection`
- CRUD: `GetEntity`, `InsertEntity`, `UpdateEntity`, `DeleteEntity`
- Schema: `GetEntitesList`, `GetEntityStructure`, `CreateEntityAs`
- Transactions: `BeginTransaction`, `Commit`, `EndTransaction`

## Validation
- Use `IDataSourceHelper.ValidateEntity(entity)` before creating tables.
- Return `Errors.Failed` in `ErrorObject` when operations fail.

## Pitfalls
- Partial implementations break discovery and runtime behavior.
- Avoid throwing for common failures; return `ErrorObject` and log.
- Always refresh `Entities` and `EntitiesNames` after schema changes.

## File Locations
- DataManagementModelsStandard/IDataSource.cs
- DataManagementEngineStandard/DataSourcesPluginsCore/

## Example
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
    public string Id { get; set; }
    public event EventHandler<PassedArgs> PassEvent;

    public ConnectionState Openconnection() { /* ... */ return ConnectionState.Open; }
    public ConnectionState Closeconnection() { /* ... */ return ConnectionState.Closed; }
    // Implement all required methods
}
```

## Task-Specific Examples

### CreateEntityAs With Validation
```csharp
var helper = DMEEditor.GetDataSourceHelper(DatasourceType);
var (isValid, errors) = helper.ValidateEntity(entity);
if (!isValid)
{
    ErrorObject.Flag = Errors.Failed;
    ErrorObject.Message = string.Join("; ", errors);
    return false;
}

return ExecuteSql(helper.GenerateCreateTableSql(entity.SchemaName, entity.EntityName, entity.EntityFields).Sql)
    .Flag == Errors.Ok;
```

### Generate Select Query With Helper
```csharp
var helper = DMEEditor.GetDataSourceHelper(DatasourceType);
var (sql, success, error) = helper.GenerateSelectSql(
    entity.SchemaName ?? "dbo",
    entity.EntityName,
    filter,
    entity.EntityFields);

return success ? RunQuery(sql) : Enumerable.Empty<object>();
```