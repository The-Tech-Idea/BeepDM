---
name: beepdm
description: Expert guidance for BeepDM development, including IDMEEditor usage, IDataSource implementations, IDataSourceHelper patterns, and ConfigEditor integration.
---

# BeepDM Development Guide

Use this skill when building or integrating with BeepDM, implementing new datasources, or working with configuration and schema operations.

## Scope
- IDMEEditor as the central orchestrator
- IDataSource and IDataSourceHelper contracts
- ConfigEditor for persisted configuration
- ETL, mapping, and UnitOfWork integration points

## Core Types
- IDMEEditor: main entry point for datasource lifecycle and services
- IDataSource: CRUD, schema, and transaction operations
- IDataSourceHelper: dialect-aware SQL and schema helpers
- ConfigEditor: persisted configuration and metadata

## Common Workflow
1. Create or obtain `IDMEEditor`.
2. Add or update `ConnectionProperties` via `ConfigEditor`.
3. Open datasource with `OpenDataSource(name)` and fetch with `GetDataSource(name)`.
4. Use helpers for schema or SQL generation, then execute via datasource.
5. Use UnitOfWork, ETL, MappingManager, or DataImportManager as needed.

## Validation
- Check `ConnectionState.Open` after `OpenDataSource`.
- For operations returning `IErrorsInfo`, require `Flag == Errors.Ok`.
- Validate entity structures with `IDataSourceHelper.ValidateEntity()` before create/alter.

## Pitfalls
- Do not throw for routine datasource failures; populate `ErrorObject` and return.
- Avoid bypassing `ConfigEditor` managers when persisting settings.

## File Locations
- DataManagementEngineStandard/Editor/DM/DMEEditor.cs
- DataManagementModelsStandard/IDataSource.cs
- DataManagementEngineStandard/ConfigUtil/ConfigEditor.cs
- DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionHelper.cs

## Example
```csharp
var editor = new DMEEditor();

var props = new ConnectionProperties
{
    ConnectionName = "MyDb",
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS,
    ConnectionString = "Data Source=./Beep/dbfiles/app.db"
};

editor.ConfigEditor.AddDataConnection(props);
var state = editor.OpenDataSource(props.ConnectionName);
if (state != ConnectionState.Open)
{
    throw new InvalidOperationException("Failed to open datasource");
}

var ds = editor.GetDataSource(props.ConnectionName);
var entities = ds.GetEntitesList();
```

## Task-Specific Examples

### Generate and Execute DDL Using Helper
```csharp
var helper = editor.GetDataSourceHelper(DataSourceType.SqlServer);
var fields = new List<EntityField>
{
    new EntityField { FieldName = "Id", Fieldtype = "System.Int32", IsIdentity = true },
    new EntityField { FieldName = "Name", Fieldtype = "System.String", Size1 = 100 }
};

var (sql, success, error) = helper.GenerateCreateTableSql("dbo", "Sample", fields);
if (success)
{
    ds.ExecuteSql(sql);
}
```

### Update Existing Connection Then Open
```csharp
var existing = editor.ConfigEditor.DataConnections
    .FirstOrDefault(c => c.ConnectionName == "MyDb");
if (existing != null)
{
    existing.ConnectionString = "Data Source=./Beep/dbfiles/app.db";
    editor.ConfigEditor.SaveDataconnectionsValues();
}

editor.OpenDataSource("MyDb");
```