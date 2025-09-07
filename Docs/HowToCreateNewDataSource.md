# How to Create a New IDataSource Implementation

This guide explains the minimum and recommended steps to add a new pluggable data source implementation to the Beep Data Management (BeepDM) platform.

## 1. Understand the Abstractions

Core contracts:
- `IDataSource` – CRUD, metadata, connection, scripting operations.
- `IDataConnection` – Low level connection state & properties.
- `EntityStructure` / `EntityField` – In-memory schema model.
- `RelationShipKeys` / `ChildRelation` – Relationship metadata.
- `IErrorsInfo` – Standard error container.
- `IDMEEditor` – Central orchestrator (logging, utilities, type builder, config, ETL context).

Optional helpers:
- `RDBMSHelper`, `DMTypeBuilder`, `ObservableBindingList<T>`, `ETLScriptDet`, `PagedResult`.

## 2. Create a New Project / Folder

Create a plugin project (Class Library) under `BeepDataSources/DataSourcesPlugins/<YourDataSource>`.
Reference core projects:
- `DataManagementModelsStandard`
- `DataManagementEngineStandard`
- `Assembly_helpersStandard` (if using assembly/type utilities)

## 3. Implement the Class Skeleton
```csharp
public class MySource : IDataSource {
    public string GuidID { get; set; } = Guid.NewGuid().ToString();
    public string DatasourceName { get; set; }
    public DataSourceType DatasourceType { get; set; }
    public DatasourceCategory Category { get; set; } = DatasourceCategory.Other; // or specialized
    public IDataConnection Dataconnection { get; set; }
    public IErrorsInfo ErrorObject { get; set; }
    public IDMLogger Logger { get; set; }
    public List<string> EntitiesNames { get; set; } = new();
    public List<EntityStructure> Entities { get; set; } = new();
    public IDMEEditor DMEEditor { get; set; }
    public ConnectionState ConnectionStatus { get; set; }
    public string ColumnDelimiter { get; set; } = "\""; // adjust per provider
    public string ParameterDelimiter { get; set; } = "@"; // adjust per provider

    public event EventHandler<PassedArgs> PassEvent;

    public MySource(string name, IDMLogger logger, IDMEEditor editor, DataSourceType type, IErrorsInfo errors) {
        DatasourceName = name;
        Logger = logger;
        DMEEditor = editor;
        DatasourceType = type;
        ErrorObject = errors;
        Dataconnection = new MyDataConnection(editor) { Logger = logger, ErrorObject = errors };
    }

    // Implement required members ...
}
```

## 4. Mandatory Methods to Implement
| Method | Purpose |
|--------|---------|
| `Openconnection` / `Closeconnection` | Manage physical connectivity. |
| `GetEntitesList` | Populate `EntitiesNames` (tables/collections). |
| `GetEntityStructure` | Load schema metadata (fields, PKs, relations). |
| `GetEntityType` | Build runtime type (via `DMTypeBuilder`). |
| `GetEntity` / `GetEntityAsync` | Retrieve data (return `IBindingList`). |
| `InsertEntity` / `UpdateEntity` / `DeleteEntity` | CRUD operations. |
| `ExecuteSql` / `RunQuery` / `GetScalar` | Generic execution helpers. |
| `CreateEntityAs` / `GetCreateEntityScript` | DDL support (optional for read-only providers). |

## 5. Schema Mapping Strategy
1. Obtain a lightweight schema snapshot (provider metadata API or DESCRIBE equivalent).
2. Map columns to `EntityField` (type, length, nullability, PK flag, uniqueness, identity/auto number, precision/scale).
3. Build `PrimaryKeys` & `Relations` lists if available.
4. Cache in `Entities` for reuse.

## 6. Data Retrieval Pattern
- Accept either a pure entity name or a raw SELECT.
- Support an optional `List<AppFilter>` (field, operator, value) to parameterize queries.
- Return an `ObservableBindingList<T>` where `T` is a runtime type built for the entity.

## 7. CRUD Parameterization
- Always use provider parameter prefix (e.g. `@`, `:`, `?`).
- Maintain a set of used parameter names to avoid collisions.
- Skip auto increment / computed fields on INSERT.
- Place PK fields only in WHERE clause for UPDATE & DELETE.

## 8. Error & Logging
Use `DMEEditor.AddLogMessage(level, message, date, code, context, flag)` and set `ErrorObject.Flag` / `ErrorObject.Message` consistently.

## 9. Pagination (Optional)
Expose a `GetEntity(..., pageNumber, pageSize)` using provider-specific paging (LIMIT/OFFSET, ROW_NUMBER, TOP + OFFSET/FETCH, etc.).

## 10. DDL Generation (Optional)
If write-enabled:
- Provide `CreateEntity(EntityStructure)` logic mapping field definitions to provider types.
- Handle auto number / identity syntax per provider.
- Generate FK constraints separately if required.

## 11. Foreign Key Discovery (Optional)
Implement introspection queries to populate `RelationShipKeys` and `ChildRelation` for navigation / modeling features.

## 12. Testing Checklist
- Open / close connection lifecycle.
- Entity list non-empty.
- Schema load matches actual column definitions.
- Insert / Update / Delete round trip.
- Filtering returns expected row subsets.
- Pagination boundaries (page 1, last page, empty).
- DDL script executes cleanly (if supported).

## 13. Minimal Read-Only Provider
If your backend is read-only (e.g. REST, flat file catalog):
- Implement only: connection open/close (may be no-op), `GetEntitesList`, `GetEntityStructure`, `GetEntity`, `RunQuery` (optional), and set CRUD methods to return failure (`Errors.NotImplemented`).

## 14. Packaging & Registration
1. Add project to solution & reference shared assemblies.
2. Implement any driver metadata (adapter/command types) in config if needed.
3. Update any plugin discovery manifest (if used) so `IDMEEditor.GetDataSource(name)` can instantiate it.
4. Build NuGet package if distributed separately.

## 15. Example Extension Points
Override in advanced scenarios:
- Custom parameter naming (`GetInsertString` etc.)
- Vendor specific identity fetch
- Advanced query rewriting (sharding / multi-tenancy)
- Optimistic concurrency tokens

## 16. Performance Tips
- Reuse commands where safe (prepare once for bulk loops).
- Avoid loading full schema repeatedly; cache until explicit refresh.
- Batch inserts where provider supports table-valued / bulk APIs.

## 17. Common Pitfalls
| Issue | Resolution |
|-------|-----------|
| Mismatched runtime type vs. schema | Refresh `EntityStructure` then rebuild type. |
| Parameter collisions | Track & uniquify names (suffix counter). |
| Identity not returned | Ensure provider-specific identity retrieval is correct. |
| Null vs empty string confusion | Normalize before parameter assignment. |

---
Follow this guide to ensure consistency and interoperability across all BeepDM data source plugins.
