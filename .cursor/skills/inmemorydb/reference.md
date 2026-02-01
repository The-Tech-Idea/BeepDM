# IInMemoryDB Quick Reference

## Core Properties
```csharp
bool IsCreated { get; set; }
bool IsLoaded { get; set; }
bool IsSaved { get; set; }
bool IsSynced { get; set; }
bool IsStructureCreated { get; set; }
ETLScriptHDR CreateScript { get; set; }
List<EntityStructure> InMemoryStructures { get; set; }
```

## Events
- `OnLoadData`
- `OnLoadStructure`
- `OnSaveStructure`
- `OnCreateStructure`
- `OnRefreshData`
- `OnSyncData`

## Required Methods
- `IErrorsInfo OpenDatabaseInMemory(string databasename)`
- `string GetConnectionString()`
- `IErrorsInfo SaveStructure()`
- `IErrorsInfo LoadStructure(IProgress<PassedArgs> progress, CancellationToken token, bool copydata = false)`
- `IErrorsInfo CreateStructure(IProgress<PassedArgs> progress, CancellationToken token)`
- `IErrorsInfo LoadData(IProgress<PassedArgs> progress, CancellationToken token)`
- `IErrorsInfo SyncData(IProgress<PassedArgs> progress, CancellationToken token)`
- `IErrorsInfo RefreshData(IProgress<PassedArgs> progress, CancellationToken token)`

## Workflow

### Initialization
1. `OpenDatabaseInMemory()` - Create/open in-memory database
2. `LoadStructure()` - Load entity structures from ConfigEditor
3. `CreateStructure()` - Create entities in in-memory database
4. `LoadData()` - Load data from source datasource

### Operations
- `RefreshData()` - Delete and reload data
- `SyncData()` - Synchronize with source
- `SaveStructure()` - Save structure to ConfigEditor

### Cleanup
- `SaveStructure()` - Save before disposing
- `Dispose()` - Clean up resources

## Common Patterns

### OpenConnection Override
```csharp
if (IsInMemory)
{
    OpenDatabaseInMemory(databaseName);
    LoadStructure(progress, token, false);
    CreateStructure(progress, token);
}
```

### LoadData Pattern
```csharp
var scripts = DMEEditor.ETL.GetCopyDataEntityScript(this, Entities, progress, token);
DMEEditor.ETL.Script.ScriptDetails = scripts;
DMEEditor.ETL.RunCreateScript(progress, token, true);
```

### SaveStructure Pattern
```csharp
SyncEntitiesNameandEntities();
SaveEntites(DatasourceName);
OnSaveStructure?.Invoke(this, args);
```
