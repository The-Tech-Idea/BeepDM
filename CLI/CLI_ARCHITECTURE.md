# BeepDM CLI Architecture

## Overview

The BeepDM CLI is a **stateless command-line interface** where each command execution creates fresh instances of all services and dependencies. Data persistence is achieved through configuration files on disk, not in-memory state.

## Core Architecture Pattern

### Execution Flow

```
User Command
    ↓
Program.Main() parses arguments
    ↓
Command Handler Executes
    ↓
BeepServiceProvider created with profile
    ↓
ServiceCollection configured
    ├── DMLogger (new instance)
    ├── ErrorsInfo (new instance)
    ├── JsonLoader (new instance)
    ├── ConfigEditor (new instance, reads from disk)
    ├── AssemblyHandler (new instance)
    └── DMEEditor (new instance)
    ↓
Command logic executes
    ↓
Changes saved to disk (if applicable)
    ↓
Command completes, all instances disposed
    ↓
Process exits
```

## BeepServiceProvider Usage

### Centralized Service Creation

All commands use `BeepServiceProvider` with profile support:

```csharp
// Standard pattern in all commands
var services = new BeepServiceProvider(profile);
var editor = services.GetEditor();
```

### Helper Methods (CliHelper)

For convenience, use these helper methods:

```csharp
// Create service provider
var services = CliHelper.CreateServiceProvider(profileName);

// Or get editor directly
var editor = CliHelper.GetEditor(profileName);
```

## Command Categories and BeepServiceProvider Usage

### 1. Profile Management (`ProfileCommands.cs`)
- **Does NOT use BeepServiceProvider** - manages profile directories directly
- Commands: list, create, delete, show, rename, export, import, clean

### 2. Configuration Management (`ConfigCommands.cs`)
- ✅ Uses `BeepServiceProvider` with profile support
- Commands: show, path, validate, connection (list, add, update, delete)
- Pattern:
```csharp
var services = new BeepServiceProvider(profile);
var editor = services.GetEditor();
editor.ConfigEditor.SaveDataconnectionsValues(); // Persist changes
```

### 3. Data Source Operations (`DataSourceCommands.cs`)
- ✅ Uses `BeepServiceProvider` with profile support
- Commands: test, info, entities
- Pattern:
```csharp
var services = new BeepServiceProvider(profile);
var editor = services.GetEditor();
var ds = editor.GetDataSource(name);
```

### 4. Driver Management (`DriverCommands.cs`)
- ✅ Uses `BeepServiceProvider` with profile support
- Commands: list, scan, info, validate, for-extension
- Manages driver discovery and registration

### 5. Query Execution (`QueryCommands.cs`)
- ✅ Uses `BeepServiceProvider` with profile support
- Commands: exec, entity
- Pattern:
```csharp
var services = new BeepServiceProvider(profile);
var editor = services.GetEditor();
var result = ds.RunQuery(sql);
```

### 6. ETL Operations (`ETLCommands.cs`)
- ✅ Uses `BeepServiceProvider` with profile support
- Commands: copy-structure, copy-data, validate
- Pattern:
```csharp
var services = new BeepServiceProvider(profile);
var editor = services.GetEditor();
editor.ETL.CopyEntityStructure(...);
```

### 7. Class Generation (`ClassCreatorCommands.cs`)
- ✅ Uses `BeepServiceProvider` with profile support
- 32 commands for code generation
- Pattern:
```csharp
var services = new BeepServiceProvider(profile);
var editor = services.GetEditor();
var classCreator = new ClassCreator(editor);
```

### 8. Data Management (`DataManagementCommands.cs`)
- ✅ Uses `BeepServiceProvider` with profile support
- Commands: schema, list-entities, export-schema, compare-schemas, stats
- Enhanced schema and data operations

### 9. Field Mapping (`MappingCommands.cs`)
- ✅ Uses `BeepServiceProvider` with profile support
- Commands: create, list, show, delete
- Pattern:
```csharp
var services = new BeepServiceProvider(profile);
var editor = services.GetEditor();
editor.ConfigEditor.SaveMapping(...);
```

### 10. Data Synchronization (`SyncCommands.cs`)
- ✅ Uses `BeepServiceProvider` with profile support
- Commands: create, run, list, show, delete
- Bi-directional data sync management

### 11. Data Import (`ImportCommands.cs`)
- ✅ Uses `BeepServiceProvider` with profile support
- Commands: file, validate
- Bulk data import from files

## Profile Support

### Standard Profile Option

All commands (except profile commands) support the `--profile` option:

```bash
beep config show --profile production
beep ds list --profile staging
beep class generate-poco MyDB Users --profile dev
```

### Default Profile

- Name: `default`
- Location: `%AppData%\TheTechIdea\BeepCLI\Profiles\default\`
- Used when `--profile` is not specified

### Profile Structure

```
%AppData%\TheTechIdea\BeepCLI\Profiles\
├── default/
│   ├── ConnectionDrivers.json
│   ├── Dataconnection.json
│   ├── DataSourcesClasses.json
│   └── ... other config files
├── dev/
├── staging/
└── production/
```

## Data Persistence

### What Persists

✅ **Configuration Files** (JSON)
- Connection definitions
- Driver configurations
- Data source mappings
- Field mappings
- Sync schemas

✅ **Generated Code Files**
- POCO classes
- Entity classes
- Web API controllers
- Repositories
- etc.

### What Does NOT Persist

❌ **In-Memory Objects**
- `DMEEditor` instance
- `ConfigEditor` instance
- `DataSources` list
- Open database connections
- Runtime state

### Persistence Mechanism

```csharp
// Read configuration from disk
var services = new BeepServiceProvider(profile);
var editor = services.GetEditor();
// ConfigEditor loads JSON files from profile directory

// Make changes
editor.ConfigEditor.AddDataConnection(conn);

// MUST explicitly save to persist
editor.ConfigEditor.SaveDataconnectionsValues();
// Writes back to JSON files in profile directory
```

## Benefits of Stateless Design

### ✅ Advantages

1. **Simplicity** - No complex state management
2. **Safety** - Commands can't interfere with each other
3. **Reliability** - Each command starts with clean state
4. **Testability** - Each command is independently testable
5. **Isolation** - Failures don't affect other operations

### ⚠️ Trade-offs

1. **Startup Time** - Must recreate all services per command
2. **Memory Churn** - Objects created/destroyed repeatedly
3. **No Caching** - Can't reuse loaded data between commands

## Best Practices

### For Command Developers

1. **Always create BeepServiceProvider with profile**
```csharp
var services = new BeepServiceProvider(profile);
```

2. **Use CliHelper for common operations**
```csharp
var editor = CliHelper.GetEditor(profile);
var ds = CliHelper.ValidateAndGetDataSource(editor, dsName);
```

3. **Explicitly save changes**
```csharp
editor.ConfigEditor.SaveDataconnectionsValues();
editor.ConfigEditor.SaveMapping(...);
```

4. **Close connections when done**
```csharp
if (ds != null && ds.ConnectionStatus == ConnectionState.Open)
{
    ds.Closeconnection();
}
```

### For CLI Users

1. **Use profiles for different environments**
```bash
beep profile create dev
beep profile create staging  
beep profile create production
```

2. **Specify profile in commands**
```bash
beep --profile dev config connection add
beep --profile production ds list
```

3. **Use environment variables**
```bash
# PowerShell
$env:BEEP_PROFILE = "production"
beep config show  # Uses production profile

# Or override config path
$env:BEEP_CONFIG_PATH = "C:\MyConfig"
```

## Command Registration

All commands are registered in `Program.cs`:

```csharp
var rootCommand = new RootCommand("BeepDM - Data Management Platform CLI")
{
    ProfileCommands.Build(),
    ConfigCommands.Build(),
    DriverCommands.Build(),
    DataSourceCommands.Build(),
    ETLCommands.Build(),
    MappingCommands.Build(),
    SyncCommands.Build(),
    ImportCommands.Build(),
    ClassCreatorCommands.Build(),
    DataManagementCommands.Build()
};
```

## Summary

The BeepDM CLI follows a **stateless architecture** where:

- Each command creates a fresh `BeepServiceProvider` instance
- All services (`DMEEditor`, `ConfigEditor`, etc.) are recreated per command
- **Data persists through JSON configuration files**, not in-memory state
- Profile support allows multiple isolated environments
- Changes must be explicitly saved to persist

This design ensures reliability and simplicity while supporting multiple configuration profiles for different environments.
