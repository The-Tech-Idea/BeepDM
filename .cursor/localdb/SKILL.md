---
name: localdb
description: Guidance for ILocalDB implementations and local database file operations in BeepDM.
---

# ILocalDB Implementation Guide

Use this skill when implementing local file-based datasources or managing local database files.

## Core Responsibilities
- Create database files and paths
- Copy and delete database files
- Drop entities safely

## Workflow
1. Ensure file path and extension are set.
2. Create database if missing.
3. Open connection and operate via IDataSource methods.
4. Close connection before file copy or delete.

## Validation
- Verify file existence before copy/delete.
- Check `IErrorsInfo.Flag` from `DropEntity`.
- Ensure `ConnectionStatus` is closed before file operations.

## Pitfalls
- Not releasing file handles causes copy or delete to fail.
- Missing extension can create unexpected filenames.
- Creating DB without ensuring directory exists fails silently.

## File Locations
- DataManagementModelsStandard/DataBase/ILocalDB.cs
- DataSourcesPluginsCore/ (local DB implementations)

## Example
```csharp
var localDb = (ILocalDB)editor.GetDataSource("LocalDb");

// Create if missing
localDb.CreateDB();

// Copy
localDb.CopyDB("backup.db", Path.Combine(AppContext.BaseDirectory, "Backups"));

// Drop entity
var result = localDb.DropEntity("Customers");
```

## Task-Specific Examples

### Delete Database File Safely
```csharp
localDb.DeleteDB();
```

### Copy To Backup Folder
```csharp
var backupFolder = Path.Combine(AppContext.BaseDirectory, "Backups");
localDb.CopyDB("app_backup.db", backupFolder);
```