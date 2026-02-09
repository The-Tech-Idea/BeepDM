---
name: inmemorydb
description: Guide for implementing IInMemoryDB interface for in-memory database operations in BeepDM. Use when creating in-memory datasource implementations, understanding in-memory patterns, or working with temporary databases.
---

# IInMemoryDB Implementation Guide

Expert guidance for implementing the `IInMemoryDB` interface, which extends `IDataSource` with in-memory database capabilities. This skill covers implementation patterns based on `InMemoryRDBSource`.

## Overview

`IInMemoryDB` provides capabilities for managing in-memory databases that can be created, loaded, saved, and synchronized with persistent datasources. It's ideal for temporary databases, testing, caching, and data manipulation without affecting persistent storage.

**Location**: `DataManagementModelsStandard/DataBase/IInMemoryDB.cs`

## Interface Structure

### Properties

```csharp
bool IsCreated { get; set; }                    // Whether database structure is created
bool IsLoaded { get; set; }                      // Whether data is loaded
bool IsSaved { get; set; }                       // Whether structure is saved
bool IsSynced { get; set; }                      // Whether data is synchronized
bool IsStructureCreated { get; set; }           // Whether structure is created
ETLScriptHDR CreateScript { get; set; }          // Script for creating structure
List<EntityStructure> InMemoryStructures { get; set; }  // In-memory entity structures
```

### Events

```csharp
event EventHandler<PassedArgs> OnLoadData;           // Fired when data is loaded
event EventHandler<PassedArgs> OnLoadStructure;      // Fired when structure is loaded
event EventHandler<PassedArgs> OnSaveStructure;       // Fired when structure is saved
event EventHandler<PassedArgs> OnCreateStructure;     // Fired when structure is created
event EventHandler<PassedArgs> OnRefreshData;         // Fired when data is refreshed
event EventHandler<PassedArgs> OnRefreshDataEntity;   // Fired when entity data is refreshed
event EventHandler<PassedArgs> OnSyncData;            // Fired when data is synchronized
```

### Required Methods

```csharp
IErrorsInfo OpenDatabaseInMemory(string databasename);  // Open/create in-memory database
string GetConnectionString();                            // Get connection string
IErrorsInfo SaveStructure();                            // Save structure to persistent storage
IErrorsInfo LoadStructure(IProgress<PassedArgs> progress, CancellationToken token, bool copydata = false);
IErrorsInfo CreateStructure(IProgress<PassedArgs> progress, CancellationToken token);
IErrorsInfo LoadData(IProgress<PassedArgs> progress, CancellationToken token);
IErrorsInfo SyncData(IProgress<PassedArgs> progress, CancellationToken token);
IErrorsInfo SyncData(string entityname, IProgress<PassedArgs> progress, CancellationToken token);
IErrorsInfo RefreshData(IProgress<PassedArgs> progress, CancellationToken token);
IErrorsInfo RefreshData(string entityname, IProgress<PassedArgs> progress, CancellationToken token);
```

## Implementation Pattern

### Step 1: Class Structure

```csharp
[AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.InMemory)]
public class MyInMemoryDataSource : RDBSource, IInMemoryDB, IDisposable
{
    // IInMemoryDB Properties
    public bool IsCreated { get; set; } = false;
    public bool IsLoaded { get; set; } = false;
    public bool IsSaved { get; set; } = false;
    public bool IsSynced { get; set; } = false;
    public bool IsStructureCreated { get; set; } = false;
    public bool IsStructureLoaded { get; set; } = false;
    public ETLScriptHDR CreateScript { get; set; } = new ETLScriptHDR();
    public List<EntityStructure> InMemoryStructures { get; set; } = new List<EntityStructure>();

    // Events
    public event EventHandler<PassedArgs> OnLoadData;
    public event EventHandler<PassedArgs> OnLoadStructure;
    public event EventHandler<PassedArgs> OnSaveStructure;
    public event EventHandler<PassedArgs> OnCreateStructure;
    public event EventHandler<PassedArgs> OnRefreshData;
    public event EventHandler<PassedArgs> OnRefreshDataEntity;
    public event EventHandler<PassedArgs> OnSyncData;

    public MyInMemoryDataSource(string name, IDMLogger logger, IDMEEditor editor, DataSourceType type, IErrorsInfo errors)
        : base(name, logger, editor, type, errors)
    {
        // Initialize in-memory specific properties
    }
}
```

### Step 2: OpenDatabaseInMemory

```csharp
public virtual IErrorsInfo OpenDatabaseInMemory(string databasename)
{
    ErrorObject.Flag = Errors.Ok;
    
    try
    {
        // Set in-memory connection string
        var connectionString = GetInMemoryConnectionString(databasename);
        Dataconnection.ConnectionProp.ConnectionString = connectionString;
        Dataconnection.ConnectionProp.IsInMemory = true;
        Dataconnection.InMemory = true;

        // Open connection
        var state = Openconnection();
        if (state == ConnectionState.Open)
        {
            IsCreated = true;
            DMEEditor.AddLogMessage("Success", $"Opened in-memory database {databasename}", DateTime.Now, 0, null, Errors.Ok);
        }
        else
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "Failed to open in-memory database";
        }
    }
    catch (Exception ex)
    {
        ErrorObject.Flag = Errors.Failed;
        ErrorObject.Message = ex.Message;
        ErrorObject.Ex = ex;
        DMEEditor.AddLogMessage("Fail", $"Failed to open in-memory database: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
    }
    
    return ErrorObject;
}

private string GetInMemoryConnectionString(string databasename)
{
    // Example for SQLite: "Data Source=:memory:;Version=3;New=True;"
    // Example for SQL Server: Use special in-memory connection string
    // Adjust based on datasource type
    return $":memory:{databasename}";
}
```

### Step 3: LoadStructure

Loads entity structures from persistent storage into in-memory database.

```csharp
public virtual IErrorsInfo LoadStructure(IProgress<PassedArgs> progress, CancellationToken token, bool copydata = false)
{
    ErrorObject.Flag = Errors.Ok;
    
    try
    {
        if (!IsStructureLoaded)
        {
            // Clear existing structures
            Entities.Clear();
            EntitiesNames.Clear();

            // Load entities from ConfigEditor
            LoadEntities(DatasourceName);

            if (Entities != null && Entities.Any())
            {
                IsStructureLoaded = true;
                IsLoaded = true;
                
                // Report progress
                progress?.Report(new PassedArgs 
                { 
                    Messege = $"Loaded {Entities.Count} entity structures",
                    DatasourceName = DatasourceName
                });

                // Fire event
                OnLoadStructure?.Invoke(this, new PassedArgs 
                { 
                    DatasourceName = DatasourceName,
                    Messege = "Structure loaded successfully"
                });
            }
        }
    }
    catch (Exception ex)
    {
        IsStructureLoaded = false;
        IsStructureCreated = false;
        ErrorObject.Flag = Errors.Failed;
        ErrorObject.Message = ex.Message;
        ErrorObject.Ex = ex;
        DMEEditor.AddLogMessage("Fail", $"Failed to load in-memory structure: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
    }
    
    return ErrorObject;
}

private IErrorsInfo LoadEntities(string datasourcename)
{
    ErrorObject.Flag = Errors.Ok;
    
    try
    {
        var ents = DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues(datasourcename);
        if (ents != null && ents.Entities.Count > 0)
        {
            // Remove duplicates
            ents.Entities = ents.Entities.GroupBy(x => x.EntityName).Select(g => g.First()).ToList();
            Entities = ents.Entities;
            EntitiesNames = ents.Entities.Select(x => x.EntityName).ToList();
            InMemoryStructures = Entities;
        }
    }
    catch (Exception ex)
    {
        ErrorObject.Flag = Errors.Failed;
        ErrorObject.Message = ex.Message;
        ErrorObject.Ex = ex;
    }
    
    return ErrorObject;
}
```

### Step 4: CreateStructure

Creates entity structures in the in-memory database.

```csharp
public IErrorsInfo CreateStructure(IProgress<PassedArgs> progress, CancellationToken token)
{
    ErrorObject.Flag = Errors.Ok;
    
    try
    {
        if (!IsStructureCreated)
        {
            int total = Entities.Count;
            int current = 0;

            foreach (var entity in Entities)
            {
                if (token.IsCancellationRequested)
                {
                    ErrorObject.Flag = Errors.Cancelled;
                    break;
                }

                // Create entity in in-memory database
                bool created = CreateEntityAs(entity);
                if (created)
                {
                    entity.IsCreated = true;
                    current++;
                    
                    progress?.Report(new PassedArgs 
                    { 
                        Messege = $"Created entity {entity.EntityName} ({current}/{total})",
                        DatasourceName = DatasourceName
                    });
                }
            }

            if (ErrorObject.Flag != Errors.Cancelled)
            {
                IsStructureCreated = true;
                
                // Fire event
                OnCreateStructure?.Invoke(this, new PassedArgs 
                { 
                    DatasourceName = DatasourceName,
                    Messege = "Structure created successfully"
                });
            }
        }
    }
    catch (Exception ex)
    {
        IsStructureCreated = false;
        ErrorObject.Flag = Errors.Failed;
        ErrorObject.Message = ex.Message;
        ErrorObject.Ex = ex;
        DMEEditor.AddLogMessage("Fail", $"Failed to create in-memory structure: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
    }
    
    return ErrorObject;
}
```

### Step 5: LoadData

Loads data from a source datasource into the in-memory database.

```csharp
public virtual IErrorsInfo LoadData(IProgress<PassedArgs> progress, CancellationToken token)
{
    ErrorObject.Flag = Errors.Ok;
    
    try
    {
        if (IsCreated && Entities.Any())
        {
            // Use ETL to copy data
            List<ETLScriptDet> scripts = DMEEditor.ETL.GetCopyDataEntityScript(
                this, 
                Entities, 
                progress, 
                token
            );

            DMEEditor.ETL.Script.ScriptDetails = scripts;
            DMEEditor.ETL.Script.LastRunDateTime = DateTime.Now;

            // Execute ETL script
            DMEEditor.ETL.RunCreateScript(progress, token, true);

            IsLoaded = true;
            
            // Fire event
            OnLoadData?.Invoke(this, new PassedArgs 
            { 
                DatasourceName = DatasourceName,
                Messege = "Data loaded successfully"
            });
        }
    }
    catch (Exception ex)
    {
        IsLoaded = false;
        ErrorObject.Flag = Errors.Failed;
        ErrorObject.Message = ex.Message;
        ErrorObject.Ex = ex;
        DMEEditor.AddLogMessage("Fail", $"Failed to load in-memory data: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
    }
    
    return ErrorObject;
}
```

### Step 6: SaveStructure

Saves the in-memory structure to persistent storage (ConfigEditor).

```csharp
public virtual IErrorsInfo SaveStructure()
{
    ErrorObject.Flag = Errors.Ok;

    try
    {
        if (Entities == null || Entities.Count == 0)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "No entities found in the in-memory structure";
            return ErrorObject;
        }

        if (ConnectionStatus != ConnectionState.Open)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "Connection is not established";
            return ErrorObject;
        }

        // Sync entity names and entities
        SyncEntitiesNameandEntities();

        // Save to ConfigEditor
        SaveEntites(DatasourceName);

        IsSaved = true;
        
        // Fire event
        OnSaveStructure?.Invoke(this, new PassedArgs 
        { 
            DatasourceName = DatasourceName,
            Messege = "Structure saved successfully"
        });
    }
    catch (Exception ex)
    {
        ErrorObject.Flag = Errors.Failed;
        ErrorObject.Message = ex.Message;
        ErrorObject.Ex = ex;
        DMEEditor.AddLogMessage("Fail", $"Failed to save in-memory structure: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
    }

    return ErrorObject;
}

private void SyncEntitiesNameandEntities()
{
    try
 {
        // Ensure collections are initialized
        if (Entities == null) Entities = new List<EntityStructure>();
        if (EntitiesNames == null) EntitiesNames = new List<string>();

        // Sync EntitiesNames with Entities
        foreach (string entityName in EntitiesNames.ToList())
        {
            if (string.IsNullOrEmpty(entityName)) continue;

            bool entityExists = Entities.Any(e => 
                e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));

            if (!entityExists)
            {
                EntityStructure entityStructure = GetEntityStructure(entityName, refresh: false);
                if (entityStructure != null)
                {
                    Entities.Add(entityStructure);
                }
                else
                {
                    EntitiesNames.Remove(entityName);
                }
            }
        }

        // Ensure all Entities are in EntitiesNames
        foreach (EntityStructure entity in Entities.ToList())
        {
            if (entity == null || string.IsNullOrEmpty(entity.EntityName)) continue;

            if (!EntitiesNames.Contains(entity.EntityName, StringComparer.OrdinalIgnoreCase))
            {
                bool created = CreateEntityAs(entity);
                if (created)
                {
                    EntitiesNames.Add(entity.EntityName);
                }
            }
        }

        // Sync with InMemoryStructures
        InMemoryStructures = new List<EntityStructure>(Entities);

        // Sync with ConnectionProp.Entities if available
        if (Dataconnection?.ConnectionProp?.Entities != null)
        {
            Dataconnection.ConnectionProp.Entities = InMemoryStructures;
        }

        IsSynced = true;
    }
    catch (Exception ex)
    {
        DMEEditor.AddLogMessage("Error", $"Error synchronizing entities: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
        IsSynced = false;
    }
}
```

### Step 7: RefreshData

Refreshes data by deleting existing data and reloading from source.

```csharp
public IErrorsInfo RefreshData(IProgress<PassedArgs> progress, CancellationToken token)
{
    ErrorObject.Flag = Errors.Ok;
    bool isDeleted = false;
    
    try
    {
        if (IsCreated)
        {
            // Delete all data from in-memory entities
            foreach (var entity in InMemoryStructures)
            {
                if (token.IsCancellationRequested) break;

                string sql = $"DELETE FROM {entity.EntityName}";
                var result = ExecuteSql(sql);
                
                if (result.Flag == Errors.Ok)
                {
                    isDeleted = true;
                    DMEEditor.AddLogMessage("Success", $"Deleted data from {entity.EntityName}", DateTime.Now, 0, null, Errors.Ok);
                }
            }

            if (isDeleted)
            {
                // Reload data
                LoadData(progress, token);
            }

            // Fire event
            OnRefreshData?.Invoke(this, new PassedArgs 
            { 
                DatasourceName = DatasourceName,
                Messege = "Data refreshed successfully"
            });
        }
    }
    catch (Exception ex)
    {
        IsLoaded = false;
        ErrorObject.Flag = Errors.Failed;
        ErrorObject.Message = ex.Message;
        ErrorObject.Ex = ex;
        DMEEditor.AddLogMessage("Fail", $"Failed to refresh in-memory data: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
    }
    
    return ErrorObject;
}
```

### Step 8: SyncData

Synchronizes data with a source datasource.

```csharp
public IErrorsInfo SyncData(IProgress<PassedArgs> progress, CancellationToken token)
{
    ErrorObject.Flag = Errors.Ok;
    
    try
    {
        // Implement synchronization logic
        // This typically involves comparing data and updating differences
        
        IsSynced = true;
        
        // Fire event
        OnSyncData?.Invoke(this, new PassedArgs 
        { 
            DatasourceName = DatasourceName,
            Messege = "Data synchronized successfully"
        });
    }
    catch (Exception ex)
    {
        IsSynced = false;
        ErrorObject.Flag = Errors.Failed;
        ErrorObject.Message = ex.Message;
        ErrorObject.Ex = ex;
    }
    
    return ErrorObject;
}
```

## Integration with Openconnection

Override `Openconnection()` to handle in-memory database initialization:

```csharp
public override ConnectionState Openconnection()
{
    var progress = new Progress<PassedArgs>(percent => { });
    var token = new CancellationTokenSource().Token;

    // Check if in-memory
    InMemory = Dataconnection.ConnectionProp.IsInMemory;
    Dataconnection.InMemory = Dataconnection.ConnectionProp.IsInMemory;

    if (Dataconnection.ConnectionStatus == ConnectionState.Open)
    {
        return ConnectionState.Open;
    }

    if (Dataconnection.ConnectionProp.IsInMemory)
    {
        // Open in-memory database
        OpenDatabaseInMemory(Dataconnection.ConnectionProp.Database);

        if (ErrorObject.Flag == Errors.Ok)
        {
            // Load structure
            LoadStructure(progress, token, false);
            
            // Create structure
            CreateStructure(progress, token);
            
            return ConnectionState.Open;
        }
    }
    else
    {
        // Use base implementation for persistent databases
        return base.Openconnection();
    }

    return ConnectionStatus;
}
```

## Disposal Pattern

Save structure before disposing:

```csharp
protected virtual void Dispose(bool disposing)
{
    if (!disposedValue)
    {
        if (disposing)
        {
            // Save structure before disposing
            if (IsCreated && ConnectionStatus == ConnectionState.Open)
            {
                SaveStructure();
            }
        }
        disposedValue = true;
    }
}
```

## Best Practices

1. **Always check IsCreated** before data operations
2. **Use progress reporting** for long-running operations
3. **Handle cancellation tokens** properly
4. **Sync entities** before saving structure
5. **Fire events** after major operations
6. **Save structure** before disposing
7. **Use ETL for data loading** to leverage existing infrastructure

## Related Interfaces

- **IDataSource**: Base interface (see **@idatasource** skill)
- **ILocalDB**: For local file-based databases (see **@localdb** skill)
- **IRDBSource**: RDBMS-specific operations

## Related Skills

- **@idatasource** - Complete IDataSource implementation guide
- **@localdb** - Guide for implementing ILocalDB
- **@beepdm** - Main BeepDM skill

## Example Implementation

See `InMemoryRDBSource.cs` in `BeepDataSources/DataSourcesPlugins/RDBMSDataSource/` for a complete implementation example.


## Repo Documentation Anchors

- DataManagementModelsStandard/DataBase/IInMemoryDB.cs
- DataManagementEngineStandard/InMemory/README.md
- DataManagementEngineStandard/Editor/ETL/README.md

