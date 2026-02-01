# IDataSource Quick Reference

## Core Properties
```csharp
string GuidID { get; set; }
string DatasourceName { get; set; }
DataSourceType DatasourceType { get; set; }
DatasourceCategory Category { get; set; }
IDataConnection Dataconnection { get; set; }
IErrorsInfo ErrorObject { get; set; }
IDMLogger Logger { get; set; }
List<string> EntitiesNames { get; set; }
List<EntityStructure> Entities { get; set; }
IDMEEditor DMEEditor { get; set; }
ConnectionState ConnectionStatus { get; set; }
string ColumnDelimiter { get; set; }
string ParameterDelimiter { get; set; }
```

## Required Methods

### Connection
- `ConnectionState Openconnection()`
- `ConnectionState Closeconnection()`

### CRUD
- `IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)`
- `PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)`
- `IErrorsInfo InsertEntity(string EntityName, object InsertedData)`
- `IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)`
- `IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)`
- `Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)`

### Schema
- `bool CheckEntityExist(string EntityName)`
- `bool CreateEntityAs(EntityStructure entity)`
- `IEnumerable<string> GetEntitesList()`
- `EntityStructure GetEntityStructure(string EntityName, bool refresh)`

### Transactions
- `IErrorsInfo BeginTransaction(PassedArgs args)`
- `IErrorsInfo Commit(PassedArgs args)`
- `IErrorsInfo EndTransaction(PassedArgs args)`

### Scripts
- `IEnumerable<object> RunQuery(string qrystr)`
- `IErrorsInfo ExecuteSql(string sql)`
- `IErrorsInfo RunScript(ETLScriptDet dDLScripts)`

## Implementation Checklist

- [ ] Add `[AddinAttribute]` to class
- [ ] Implement all required properties
- [ ] Implement all required methods
- [ ] Use `IDataSourceHelper` for SQL generation
- [ ] Populate `ErrorObject` on failures
- [ ] Check `ConnectionStatus` before operations
- [ ] Validate entity before `CreateEntityAs()`
- [ ] Set correct `ColumnDelimiter` and `ParameterDelimiter`

## Common Patterns

### CreateEntityAs Pattern
```csharp
var helper = DMEEditor.GetDataSourceHelper(DatasourceType);
var (valid, msg) = helper.ValidateEntity(entity);
var (createSql, success, error) = helper.GenerateCreateTableSql(...);
ExecuteSql(createSql);
```

### GetEntity Pattern
```csharp
var entity = GetEntityStructure(EntityName, refresh: false);
var helper = DMEEditor.GetDataSourceHelper(DatasourceType);
var (selectSql, success, error) = helper.GenerateSelectSql(...);
return RunQuery(selectSql);
```

### Error Handling Pattern
```csharp
ErrorObject.Flag = Errors.Ok;
try { /* operation */ }
catch (Exception ex)
{
    ErrorObject.Flag = Errors.Failed;
    ErrorObject.Message = ex.Message;
    ErrorObject.Ex = ex;
}
```
