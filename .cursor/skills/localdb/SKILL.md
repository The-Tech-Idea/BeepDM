---
name: localdb
description: Guide for implementing ILocalDB interface for local file-based database operations in BeepDM. Use when creating local database implementations, understanding file-based database patterns, or working with SQLite-like databases.
---

# ILocalDB Implementation Guide

Expert guidance for implementing the `ILocalDB` interface, which extends `IDataSource` with local file-based database capabilities. This skill covers implementation patterns based on `SQLiteDataSource`.

## Overview

`ILocalDB` provides capabilities for managing local file-based databases that can be created, deleted, copied, and managed as files on the filesystem. It's ideal for embedded databases, local storage, and portable database applications.

**Location**: `DataManagementModelsStandard/DataBase/ILocalDB.cs`

## Interface Structure

### Properties

```csharp
bool CanCreateLocal { get; set; }    // Whether local database can be created
bool InMemory { get; set; }           // Whether database is in-memory
string Extension { get; set; }        // File extension (e.g., ".s3db", ".db")
```

### Required Methods

```csharp
bool CreateDB();                                    // Create database in default location
bool CreateDB(bool inMemory);                       // Create in-memory database
bool CreateDB(string filepathandname);              // Create database at specified path
bool DeleteDB();                                     // Delete database file
bool CopyDB(string DestDbName, string DesPath);     // Copy database to new location
IErrorsInfo DropEntity(string EntityName);          // Drop entity from database
```

## Implementation Pattern

### Step 1: Class Structure

```csharp
[AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.SqlLite)]
public class MyLocalDataSource : InMemoryRDBSource, ILocalDB, IDataSource, IDisposable
{
    // ILocalDB Properties
    public bool CanCreateLocal { get; set; } = true;
    public bool InMemory { get; set; } = false;
    public string Extension { get; set; } = ".db";

    public MyLocalDataSource(string name, IDMLogger logger, IDMEEditor editor, DataSourceType type, IErrorsInfo errors)
        : base(name, logger, editor, type, errors)
    {
        // Initialize local database specific properties
        Dataconnection.ConnectionProp.DatabaseType = type;
        ColumnDelimiter = "[]";
        ParameterDelimiter = "$";
    }
}
```

### Step 2: CreateDB (Default Location)

Creates database in the default location specified in connection properties.

```csharp
public bool CreateDB()
{
    try
    {
        // Ensure file has extension
        if (!Path.HasExtension(Dataconnection.ConnectionProp.FileName))
        {
            Dataconnection.ConnectionProp.FileName = 
                Dataconnection.ConnectionProp.FileName + Extension;
        }

        string filePath = Path.Combine(
            Dataconnection.ConnectionProp.FilePath, 
            Dataconnection.ConnectionProp.FileName
        );

        // Create database file if it doesn't exist
        if (!File.Exists(filePath))
        {
            CreateDatabaseFile(filePath);
            EnableForeignKeys(); // Enable FK constraints if needed
            DMEEditor.AddLogMessage("Success", $"Created database {filePath}", DateTime.Now, 0, null, Errors.Ok);
        }
        else
        {
            DMEEditor.AddLogMessage("Success", $"Database already exists: {filePath}", DateTime.Now, 0, null, Errors.Ok);
        }

        return true;
    }
    catch (Exception ex)
    {
        DMEEditor.AddLogMessage("Fail", $"Could not create database: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
        return false;
    }
}

private void CreateDatabaseFile(string filePath)
{
    // Ensure directory exists
    string directory = Path.GetDirectoryName(filePath);
    if (!Directory.Exists(directory))
    {
        Directory.CreateDirectory(directory);
    }

    // Create database file
    // For SQLite: SQLiteConnection.CreateFile(filePath);
    // For other databases: Use appropriate API
    CreateDatabaseFileInternal(filePath);
}

private void CreateDatabaseFileInternal(string filePath)
{
    // Implementation depends on datasource type
    // Example for SQLite:
    // SQLiteConnection.CreateFile(filePath);
    
    // Example for other databases:
    // Use datasource-specific API to create database file
}
```

### Step 3: CreateDB (Specified Path)

Creates database at a specified file path.

```csharp
public bool CreateDB(string filepathandname)
{
    try
    {
        if (File.Exists(filepathandname))
        {
            DMEEditor.AddLogMessage("Success", $"Database already exists: {filepathandname}", DateTime.Now, 0, null, Errors.Ok);
        }
        else
        {
            CreateDatabaseFile(filepathandname);
            DMEEditor.AddLogMessage("Success", $"Created database: {filepathandname}", DateTime.Now, 0, null, Errors.Ok);
        }

        // Update connection properties
        Dataconnection.ConnectionProp.ConnectionString = 
            BuildConnectionString(filepathandname);
        Dataconnection.ConnectionProp.FilePath = Path.GetDirectoryName(filepathandname);
        Dataconnection.ConnectionProp.FileName = Path.GetFileName(filepathandname);

        // Ensure file exists (create if needed)
        if (!File.Exists(filepathandname))
        {
            CreateDatabaseFile(filepathandname);
        }

        return true;
    }
    catch (Exception ex)
    {
        DMEEditor.AddLogMessage("Fail", $"Could not create database: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
        return false;
    }
}

private string BuildConnectionString(string filePath)
{
    // Build datasource-specific connection string
    // Example for SQLite:
    // return $"Data Source={filePath};Version=3;New=True;";
    
    // Adjust based on datasource type
    return $"Data Source={filePath};";
}
```

### Step 4: CreateDB (In-Memory)

Creates an in-memory database (typically returns false for file-based databases).

```csharp
public bool CreateDB(bool inMemory)
{
    // For file-based databases, in-memory creation may not be supported
    // Return false or delegate to IInMemoryDB implementation
    if (inMemory)
    {
        // Use in-memory connection string
        Dataconnection.ConnectionProp.IsInMemory = true;
        Dataconnection.InMemory = true;
        return OpenDatabaseInMemory(Dataconnection.ConnectionProp.Database).Flag == Errors.Ok;
    }
    
    return CreateDB();
}
```

### Step 5: DeleteDB

Deletes the database file.

```csharp
public bool DeleteDB()
{
    try
    {
        // Close connection first
        Closeconnection();
        
        // Force garbage collection to release file handles
        GC.Collect();
        GC.WaitForPendingFinalizers();

        string filePath = Path.Combine(
            Dataconnection.ConnectionProp.FilePath, 
            Dataconnection.ConnectionProp.FileName
        );

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            DMEEditor.AddLogMessage("Success", $"Deleted database: {filePath}", DateTime.Now, 0, null, Errors.Ok);
        }
        else
        {
            DMEEditor.AddLogMessage("Warning", $"Database file not found: {filePath}", DateTime.Now, 0, null, Errors.Warning);
        }

        return true;
    }
    catch (Exception ex)
    {
        DMEEditor.AddLogMessage("Fail", $"Could not delete database: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
        return false;
    }
}
```

### Step 6: CopyDB

Copies the database file to a new location.

```csharp
public bool CopyDB(string DestDbName, string DesPath)
{
    try
    {
        string sourcePath = Path.Combine(
            Dataconnection.ConnectionProp.FilePath, 
            Dataconnection.ConnectionProp.FileName
        );

        if (!File.Exists(sourcePath))
        {
            DMEEditor.AddLogMessage("Fail", $"Source database not found: {sourcePath}", DateTime.Now, 0, null, Errors.Failed);
            return false;
        }

        // Ensure destination directory exists
        if (!Directory.Exists(DesPath))
        {
            Directory.CreateDirectory(DesPath);
        }

        string destPath = Path.Combine(DesPath, DestDbName);
        
        // Ensure destination file has extension
        if (!Path.HasExtension(destPath))
        {
            destPath = destPath + Extension;
        }

        // Close connection before copying
        Closeconnection();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Copy file
        File.Copy(sourcePath, destPath, overwrite: true);
        
        DMEEditor.AddLogMessage("Success", $"Copied database to {destPath}", DateTime.Now, 0, null, Errors.Ok);
        return true;
    }
    catch (Exception ex)
    {
        DMEEditor.AddLogMessage("Fail", $"Could not copy database: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
        return false;
    }
}
```

### Step 7: DropEntity

Drops an entity (table) from the database.

```csharp
public IErrorsInfo DropEntity(string EntityName)
{
    ErrorObject.Flag = Errors.Ok;
    
    try
    {
        // Generate DROP TABLE SQL using helper
        var helper = DMEEditor.GetDataSourceHelper(DatasourceType);
        var (dropSql, success, error) = helper.GenerateDropTableSql(
            GetSchemaName(),
            EntityName
        );

        if (!success)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"Failed to generate DROP SQL: {error}";
            return ErrorObject;
        }

        // Execute DROP statement
        ErrorObject = ExecuteSql(dropSql);

        if (ErrorObject.Flag == Errors.Ok)
        {
            // Verify entity is dropped
            if (!CheckEntityExist(EntityName))
            {
                // Remove from collections
                EntitiesNames.Remove(EntityName);
                Entities.RemoveAll(e => e.EntityName == EntityName);
                
                DMEEditor.AddLogMessage("Success", $"Dropped entity {EntityName}", DateTime.Now, 0, null, Errors.Ok);
            }
            else
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Entity {EntityName} still exists after drop";
            }
        }
    }
    catch (Exception ex)
    {
        ErrorObject.Flag = Errors.Failed;
        ErrorObject.Message = ex.Message;
        ErrorObject.Ex = ex;
        DMEEditor.AddLogMessage("Fail", $"Failed to drop entity {EntityName}: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
    }
    
    return ErrorObject;
}
```

## Integration with Openconnection

Override `Openconnection()` to handle local database creation:

```csharp
public override ConnectionState Openconnection()
{
    var progress = new Progress<PassedArgs>(percent => { });
    var token = new CancellationTokenSource().Token;

    InMemory = Dataconnection.ConnectionProp.IsInMemory;
    Dataconnection.InMemory = Dataconnection.ConnectionProp.IsInMemory;

    if (Dataconnection.ConnectionStatus == ConnectionState.Open)
    {
        return ConnectionState.Open;
    }

    if (Dataconnection.ConnectionProp.IsInMemory)
    {
        // Handle in-memory database
        OpenDatabaseInMemory(Dataconnection.ConnectionProp.Database);
        
        if (ErrorObject.Flag == Errors.Ok)
        {
            LoadStructure(progress, token, false);
            CreateStructure(progress, token);
            return ConnectionState.Open;
        }
    }
    else
    {
        // Ensure database file exists
        string filePath = Path.Combine(
            Dataconnection.ConnectionProp.FilePath ?? "",
            Dataconnection.ConnectionProp.FileName ?? ""
        );

        if (!string.IsNullOrEmpty(filePath) && !File.Exists(filePath))
        {
            // Create database if it doesn't exist
            CreateDB(filePath);
        }

        // Use base implementation
        return base.Openconnection();
    }

    return ConnectionStatus;
}
```

## Integration with Closeconnection

Override `Closeconnection()` to save structure for in-memory databases:

```csharp
public override ConnectionState Closeconnection()
{
    try
    {
        if (RDBMSConnection.DbConn.State != ConnectionState.Open)
        {
            return ConnectionState.Closed;
        }

        // Clear connection pools if applicable
        // SQLiteConnection.ClearAllPools();

        if (RDBMSConnection.DbConn != null)
        {
            if (Dataconnection.ConnectionProp.IsInMemory)
            {
                // Save structure before closing in-memory database
                SaveStructure();
            }

            RDBMSConnection.DbConn.Close();
        }

        DMEEditor.AddLogMessage("Success", $"Closed connection to {DatasourceName}", DateTime.Now, 0, null, Errors.Ok);
    }
    catch (Exception ex)
    {
        DMEEditor.AddLogMessage("Fail", $"Error closing connection: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
    }

    return base.ConnectionStatus;
}
```

## Helper Methods

### CreateDBDefaultDir

Creates database in default directory:

```csharp
public bool CreateDBDefaultDir(string filename)
{
    try
    {
        string dirpath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "TheTechIdea",
            "Beep",
            "DatabaseFiles"
        );

        if (!Directory.Exists(dirpath))
        {
            Directory.CreateDirectory(dirpath);
        }

        string filepathandname = Path.Combine(dirpath, filename);
        return CreateDB(filepathandname);
    }
    catch (Exception ex)
    {
        DMEEditor.AddLogMessage("Fail", $"Could not create database in default directory: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
        return false;
    }
}
```

### EnableForeignKeys

Enable foreign key constraints (datasource-specific):

```csharp
private void EnableForeignKeys()
{
    try
    {
        // Example for SQLite:
        // ExecuteSql("PRAGMA foreign_keys = ON;");
        
        // Adjust based on datasource type
        var helper = DMEEditor.GetDataSourceHelper(DatasourceType);
        if (helper.Capabilities.SupportsForeignKeys)
        {
            // Enable FK constraints using datasource-specific SQL
        }
    }
    catch (Exception ex)
    {
        DMEEditor.AddLogMessage("Warning", $"Could not enable foreign keys: {ex.Message}", DateTime.Now, 0, null, Errors.Warning);
    }
}
```

## Best Practices

1. **Check file existence** before creating database
2. **Ensure directory exists** before creating database file
3. **Close connection** before file operations (delete, copy)
4. **Use GC.Collect()** after closing to release file handles
5. **Handle file locks** properly
6. **Set proper file extensions** based on datasource type
7. **Update connection properties** after creating database
8. **Use IDataSourceHelper** for SQL generation

## Common Patterns

### Database Initialization

```csharp
// Check if database exists, create if needed
if (!File.Exists(databasePath))
{
    if (!CreateDB(databasePath))
    {
        throw new Exception("Failed to create database");
    }
}

// Open connection
var state = Openconnection();
if (state != ConnectionState.Open)
{
    throw new Exception("Failed to open database");
}
```

### Database Backup

```csharp
// Create backup copy
string backupPath = Path.Combine(backupDirectory, $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
if (!CopyDB(Path.GetFileName(backupPath), backupDirectory))
{
    throw new Exception("Failed to create backup");
}
```

### Database Migration

```csharp
// Copy database to new location
string newPath = Path.Combine(newDirectory, databaseName);
if (CopyDB(databaseName, newDirectory))
{
    // Update connection properties
    Dataconnection.ConnectionProp.FilePath = newDirectory;
    Dataconnection.ConnectionProp.FileName = databaseName;
    
    // Reopen connection
    Closeconnection();
    Openconnection();
}
```

## Related Interfaces

- **IDataSource**: Base interface (see **@idatasource** skill)
- **IInMemoryDB**: For in-memory database implementations (see **@inmemorydb** skill)
- **IRDBSource**: RDBMS-specific operations

## Related Skills

- **@idatasource** - Complete IDataSource implementation guide
- **@inmemorydb** - Guide for implementing IInMemoryDB
- **@beepdm** - Main BeepDM skill

## Example Implementation

See `SQLiteDataSource.cs` in `BeepDataSources/DataSourcesPluginsCore/SqliteDatasourceCore/` for a complete implementation example.


## Repo Documentation Anchors

- DataManagementModelsStandard/DataBase/ILocalDB.cs
- DataManagementEngineStandard/DataBase/README.md
- DataManagementEngineStandard/Docs/creating-custom-datasources.html

