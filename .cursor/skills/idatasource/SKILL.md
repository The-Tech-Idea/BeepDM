---
name: idatasource
description: Guidance for implementing and consuming IDataSource in BeepDM, including connection lifecycle, CRUD, metadata, scripts, and transaction patterns. Use when building a datasource plugin or writing datasource-agnostic runtime logic against the core DataManagementModels contract.
---

# IDataSource Guide

Use this skill when implementing a datasource plugin or when writing logic that should work across different BeepDM datasource types.

## Use this skill when
- Building a new `IDataSource` implementation
- Reviewing whether an existing datasource honors the BeepDM contract correctly
- Writing datasource-agnostic CRUD, schema, or transaction logic
- Debugging `ErrorObject`, `Entities`, or `ConnectionStatus` behavior

## Do not use this skill when
- The task is only about connection definition and validation. Use [`connectionproperties`](../connectionproperties/SKILL.md) and [`connection`](../connection/SKILL.md).
- The task is only about orchestration through `IDMEEditor`. Use [`beepdm`](../beepdm/SKILL.md).
- The task is primarily about unit-of-work behavior. Use [`unitofwork`](../unitofwork/SKILL.md).

## Required Responsibilities
- Implement the full `IDataSource` contract from `DataManagementModelsStandard`.
- Maintain `ErrorObject` for routine failures and set `ConnectionStatus` accurately.
- Populate and refresh `Entities` and `EntitiesNames` when metadata changes.
- Use `IDataSourceHelper` when provider-specific query or DDL generation is needed.
- Expose consistent behavior across sync/async calls and transaction methods.

## Core Method Areas
- Connection:
  - `Openconnection`, `Closeconnection`
- Query and execution:
  - `RunQuery`, `ExecuteSql`, `RunScript`
- CRUD:
  - `GetEntity`, `GetEntityAsync`, `InsertEntity`, `UpdateEntity`, `UpdateEntities`, `DeleteEntity`
- Metadata and schema:
  - `GetEntitesList`, `GetEntityStructure`, `CheckEntityExist`, `CreateEntityAs`, `CreateEntities`
- Transactions:
  - `BeginTransaction`, `Commit`, `EndTransaction`

## Typical Workflow
1. Let `IDMEEditor` create or resolve the datasource instance.
2. Open the underlying connection and update `ConnectionStatus`.
3. Use the datasource for CRUD or metadata operations.
4. For schema creation and migration, validate structures and delegate SQL generation to `IDataSourceHelper` when appropriate.
5. Keep `ErrorObject`, entity caches, and connection state synchronized with actual behavior.

## Validation and Safety
- Validate entities with `IDataSourceHelper.ValidateEntity(entity)` before `CreateEntityAs` where helper support exists.
- Return `Errors.Failed` in `ErrorObject` for expected runtime failures instead of throwing by default.
- Guard operations when `ConnectionStatus` is not usable.
- Refresh metadata collections after schema changes.

## Pitfalls
- Partial implementations break discovery, orchestration, and runtime assumptions.
- Throwing for every provider issue fights the BeepDM error-reporting model.
- Failing to refresh `Entities` and `EntitiesNames` makes migrations and UI generation stale.
- Hardcoding SQL in datasource code when a helper exists makes multi-provider support harder to maintain.

## File Locations
- `DataManagementModelsStandard/IDataSource.cs`
- `DataManagementModelsStandard/Editor/IDataSourceHelper.cs`
- `DataManagementEngineStandard/Editor/DM/DMEEditor.cs`

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

## Related Skills
- [`beepdm`](../beepdm/SKILL.md)
- [`connection`](../connection/SKILL.md)
- [`migration`](../migration/SKILL.md)
- [`unitofwork`](../unitofwork/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for a method checklist, contract summary, and common implementation patterns.
