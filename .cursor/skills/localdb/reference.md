# ILocalDB Quick Reference

## Core Properties
```csharp
bool CanCreateLocal { get; set; }
bool InMemory { get; set; }
string Extension { get; set; }
```

## Required Methods
- `bool CreateDB()` - Create in default location
- `bool CreateDB(bool inMemory)` - Create in-memory
- `bool CreateDB(string filepathandname)` - Create at path
- `bool DeleteDB()` - Delete database file
- `bool CopyDB(string DestDbName, string DesPath)` - Copy database
- `IErrorsInfo DropEntity(string EntityName)` - Drop entity

## Common Patterns

### Create Database
```csharp
// Default location
CreateDB();

// Specific path
CreateDB(filePath);

// Default directory
CreateDBDefaultDir(filename);
```

### Database Management
```csharp
// Delete
Closeconnection();
GC.Collect();
DeleteDB();

// Copy
Closeconnection();
GC.Collect();
CopyDB(destName, destPath);
```

### Drop Entity
```csharp
var helper = DMEEditor.GetDataSourceHelper(DatasourceType);
var (dropSql, success, error) = helper.GenerateDropTableSql(schema, entityName);
ExecuteSql(dropSql);
```

### OpenConnection Override
```csharp
if (!File.Exists(filePath))
{
    CreateDB(filePath);
}
return base.Openconnection();
```

### CloseConnection Override
```csharp
if (IsInMemory)
{
    SaveStructure();
}
base.Closeconnection();
```

## File Operations

### Check Existence
```csharp
string filePath = Path.Combine(FilePath, FileName);
if (!File.Exists(filePath)) { CreateDB(filePath); }
```

### Ensure Directory
```csharp
if (!Directory.Exists(directory))
{
    Directory.CreateDirectory(directory);
}
```

### Release File Handles
```csharp
Closeconnection();
GC.Collect();
GC.WaitForPendingFinalizers();
```
