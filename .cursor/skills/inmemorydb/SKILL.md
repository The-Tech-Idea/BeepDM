---
name: inmemorydb
description: Guidance for IInMemoryDB implementations, including structure and data load workflows in BeepDM.
---

# IInMemoryDB Implementation Guide

Use this skill when implementing or using in-memory datasources for temporary or cached data operations.

## Core Responsibilities
- Create/open in-memory store
- Load structure and create entities
- Load or sync data from a persistent source

## Workflow
1. `OpenDatabaseInMemory` to initialize connection.
2. `LoadStructure` from config.
3. `CreateStructure` to create entities in memory.
4. `LoadData` or `SyncData` to populate.

## Validation
- Ensure `ConnectionStatus == Open` before load.
- Track `IsStructureCreated` and `IsLoaded` flags.
- Check `IErrorsInfo.Flag` after ETL calls.

## Pitfalls
- Skipping `LoadStructure` leads to empty entity lists.
- Not syncing entity names and entities can break lookups.
- Forgetting to save structure loses metadata.

## File Locations
- DataManagementModelsStandard/DataBase/IInMemoryDB.cs
- DataSourcesPluginsCore/ (in-memory implementations)

## Example
```csharp
var inMemory = (IInMemoryDB)editor.GetDataSource("InMemoryDb");
var progress = new Progress<PassedArgs>(p => Console.WriteLine(p.Messege));
var token = CancellationToken.None;

inMemory.OpenDatabaseInMemory("InMemoryDb");
inMemory.LoadStructure(progress, token, copydata: false);
inMemory.CreateStructure(progress, token);
inMemory.LoadData(progress, token);
```

## Task-Specific Examples

### Sync A Single Entity
```csharp
inMemory.SyncData("Customers", progress, token);
```

### Refresh All Data
```csharp
inMemory.RefreshData(progress, token);
```