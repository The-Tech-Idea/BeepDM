---
name: beepdm
description: Expert guidance for BeepDM development, including IDMEEditor usage, IDataSource implementations, IDataSourceHelper patterns, and ConfigEditor integration.
---

# BeepDM Development Guide

Use this skill when the task spans multiple BeepDM subsystems and you need the right entry point before dropping into a narrower skill.

## Use this skill when
- Bootstrapping or debugging core BeepDM flows around `IDMEEditor`
- Adding a new datasource or integrating an existing datasource into application startup
- Routing work between connection, configuration, schema, ETL, mapping, and unit-of-work concerns
- Reviewing BeepDM code and deciding which specialized skill should handle the next step

## Do not use this skill as the only source when
- The task is only about DI registration and app startup. Use [`beepserviceregistration`](../beepserviceregistration/SKILL.md).
- The task is only about legacy desktop initialization. Use [`beepservice`](../beepservice/SKILL.md).
- The task is only about connection building, driver selection, or connection-string validation. Use [`connection`](../connection/SKILL.md) and [`connectionproperties`](../connectionproperties/SKILL.md).
- The task is only about CRUD/query execution. Use [`idatasource`](../idatasource/SKILL.md).
- The task is only about transactions. Use [`unitofwork`](../unitofwork/SKILL.md).
- The task is only about migration, ETL, import, sync, or forms. Use the dedicated subsystem skill.

## Responsibilities
- Treat `IDMEEditor` as the orchestration boundary for datasource lifecycle, helper resolution, and service access.
- Keep `ConfigEditor` as the persisted source of truth for connections and metadata-related configuration.
- Route implementation details to the narrowest useful BeepDM skill instead of duplicating guidance.
- Ground recommendations in current source files and existing BeepDM patterns.

## Core Types
- `IDMEEditor`: main orchestration entry point for datasource creation, lookup, helpers, ETL, mapping, and unit-of-work.
- `IDataSource`: runtime datasource contract for CRUD, metadata, execution, and transaction operations.
- `IDataSourceHelper`: dialect-aware SQL/schema helper surface used for DDL, DML, and capability checks.
- `ConfigEditor`: persisted configuration manager for `ConnectionProperties`, mappings, and runtime settings.

## Task Routing Matrix
- App startup or dependency injection: [`beepserviceregistration`](../beepserviceregistration/SKILL.md)
- Legacy desktop bootstrapping: [`beepservice`](../beepservice/SKILL.md)
- Connection creation, normalization, validation, security: [`connection`](../connection/SKILL.md)
- Building `ConnectionProperties`: [`connectionproperties`](../connectionproperties/SKILL.md)
- Direct datasource CRUD and queries: [`idatasource`](../idatasource/SKILL.md)
- Batch commit and rollback flows: [`unitofwork`](../unitofwork/SKILL.md)
- Entity copy or transformation pipelines: [`etl`](../etl/SKILL.md)
- Schema creation and upgrade flows: [`migration`](../migration/SKILL.md)
- Dynamic UI or master-detail forms: [`forms`](../forms/SKILL.md)

## Typical Workflow
1. Decide whether the code should start from `DMEEditor` directly or from the DI/service-registration path.
2. Load or create `ConnectionProperties` through `ConfigEditor`, not ad-hoc objects that are never persisted.
3. Open the datasource with `OpenDataSource(connectionName)` and confirm it reached `ConnectionState.Open`.
4. Fetch the live datasource with `GetDataSource(connectionName)` only after open succeeds.
5. Resolve `IDataSourceHelper` by `DataSourceType` when schema or SQL generation is required.
6. Hand off to ETL, mapping, migrations, forms, or unit-of-work only after the datasource and helper state is valid.

## Validation and Safety
- Check `ConnectionState.Open` after `OpenDataSource`.
- For operations returning `IErrorsInfo`, require `Flag == Errors.Ok` before continuing.
- Validate entity structures with `IDataSourceHelper.ValidateEntity()` before create/alter operations.
- Prefer capability checks before emitting dialect-specific SQL.
- Preserve the BeepDM error model for routine failures: populate `ErrorObject` or `IErrorsInfo`, do not convert every runtime issue into a thrown exception.

## Pitfalls
- Do not call `GetDataSource` before a successful `OpenDataSource`.
- Do not bypass `ConfigEditor` when a connection needs to survive application restarts.
- Do not hardcode helper assumptions across datasource types; resolve by `DataSourceType`.
- Do not mix orchestration guidance into subsystem skills; keep this skill as the router and overview layer.

## Working Set
- `DataManagementEngineStandard/Editor/DM/DMEEditor.cs`
- `DataManagementEngineStandard/Editor/DM/DMEEditor.UniversalDataSourceHelpers.cs`
- `DataManagementModelsStandard/IDataSource.cs`
- `DataManagementModelsStandard/Editor/IDataSourceHelper.cs`
- `DataManagementEngineStandard/ConfigUtil/ConfigEditor.cs`
- `DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionHelper.cs`
- `.cursor/skills/beepdm/reference.md`

## Review Checklist
- Is the task starting from the correct entry point: direct editor, service registration, or subsystem helper?
- Is connection state validated before issuing CRUD or schema operations?
- Is persisted configuration updated through `ConfigEditor` when the change should survive restarts?
- Is there a narrower skill that should own the next implementation step?

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

## Related Skills
- [`beepserviceregistration`](../beepserviceregistration/SKILL.md)
- [`beepservice`](../beepservice/SKILL.md)
- [`connection`](../connection/SKILL.md)
- [`connectionproperties`](../connectionproperties/SKILL.md)
- [`idatasource`](../idatasource/SKILL.md)
- [`unitofwork`](../unitofwork/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for method-level examples, defaults management, migration snippets, and capability-oriented helper usage.
