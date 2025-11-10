# BeepShell Assembly Management Guide

## Overview

BeepShell provides comprehensive assembly management commands as the **base of work**, giving you direct access to AssemblyHandler operations for managing assemblies, types, drivers, and extensions.

## Why Assembly Commands Are Base

The assembly management system is foundational to BeepDM because:
- **Data drivers** are loaded from assemblies
- **Extensions** are discovered by scanning assemblies
- **Type creation** requires assembly type resolution
- **Plugin system** depends on assembly loading/unloading
- **Dynamic functionality** is enabled through reflection

## Command Structure

All assembly commands use the `assembly` command (alias: `asm`):

```bash
assembly <subcommand> [options] [arguments]
```

## Core Commands

### 1. List Assemblies

View all currently loaded assemblies:

```bash
# Basic list
assembly list

# Detailed view
assembly list --verbose
assembly list -v

# Filter by name
assembly list --filter MyExtension
assembly list -f BeepDM

# Using alias
asm list -v
```

**Output includes:**
- Assembly name
- Version (with --verbose)
- Location (with --verbose)
- GAC status (with --verbose)

**Example:**
```bash
beep> assembly list --verbose

┌───────────────────────────────────────────────────────────┐
│                Loaded Assemblies (47)                     │
├────┬──────────────────────┬─────────┬──────────────┬──────┤
│ #  │ Name                 │ Version │ Location     │ GAC  │
├────┼──────────────────────┼─────────┼──────────────┼──────┤
│ 1  │ BeepDM               │ 1.0.0   │ C:\BeepDM... │ No   │
│ 2  │ Spectre.Console      │ 0.49.1  │ C:\BeepDM... │ No   │
└────┴──────────────────────┴─────────┴──────────────┴──────┘
```

### 2. Load Assembly

Load assemblies or directories:

```bash
# Load single assembly
assembly load MyExtension.dll

# Load from directory
assembly load C:\Extensions

# Specify folder type
assembly load MyExtension.dll --type ProjectClass
assembly load MyExtension.dll -t Addin

# Using alias
asm load MyPlugin.dll
```

**Folder Types:**
- `SharedAssembly` - Shared assemblies (default)
- `ProjectClass` - Project class libraries
- `Addin` - Add-in assemblies
- `OtherDLL` - Other DLLs

**Example:**
```bash
beep> assembly load C:\MyExtensions\DataTools.dll

✓ Assembly loaded successfully
Total assemblies: 48
```

### 3. Unload Assembly

Remove assemblies from memory:

```bash
# Unload assembly
assembly unload MyExtension

# Unload nugget package
assembly unload MyPackage --nugget
assembly unload MyPackage -n
```

**Note:** .NET Framework has limitations on true assembly unloading. Use plugins for hot-reload scenarios.

### 4. Scan Assemblies

Scan for types, drivers, and extensions:

```bash
# Scan specific assembly
assembly scan --path MyExtension.dll
assembly scan -p MyExtension.dll

# Scan all loaded assemblies
assembly scan --all
assembly scan -a
```

**What scanning does:**
- Discovers data source implementations
- Finds workflow actions and steps
- Identifies add-ins and extensions
- Detects loader extensions
- Catalogs view models

**Example:**
```bash
beep> assembly scan --all

⠋ Scanning all assemblies...
✓ All assemblies scanned
```

### 5. List Types

Query types from assemblies:

```bash
# List all types
assembly types

# Filter by assembly
assembly types --assembly BeepDM
assembly types -a MyExtension

# Filter by interface
assembly types --interface IDataSource
assembly types -i IDM_Addin

# Limit results
assembly types --limit 100
assembly types -l 20

# Combine filters
asm types -a MyExtension -i IDataSource -l 10
```

**Example:**
```bash
beep> assembly types --interface IDataSource --limit 10

┌──────────────────────────────────────────────────────────┐
│           Types (234 total, showing 10)                  │
├─────────────────┬──────────────┬────────────────────────┤
│ Type Name       │ Assembly     │ Namespace              │
├─────────────────┼──────────────┼────────────────────────┤
│ SqlServerSource │ BeepDM       │ TheTechIdea.Beep.Data  │
│ MySqlSource     │ BeepDM       │ TheTechIdea.Beep.Data  │
└─────────────────┴──────────────┴────────────────────────┘
```

### 6. List Drivers

View all data drivers:

```bash
# List all drivers
assembly drivers

# Filter by category
assembly drivers --category RDBMS
assembly drivers -c NoSQL
```

**Categories:**
- RDBMS - Relational databases
- NoSQL - Document/key-value stores
- File - File-based sources
- Cloud - Cloud services
- API - API connectors

**Example:**
```bash
beep> assembly drivers --category RDBMS

┌────────────────────────────────────────────────────────┐
│                  Data Drivers (12)                     │
├─────────────────┬──────────┬────────────┬─────────────┤
│ Name            │ Category │ Package    │ Version     │
├─────────────────┼──────────┼────────────┼─────────────┤
│ SqlServerSource │ RDBMS    │ SqlServer  │ 1.0.0       │
│ MySqlSource     │ RDBMS    │ MySql      │ 1.0.0       │
└─────────────────┴──────────┴────────────┴─────────────┘
```

### 7. List Loader Extensions

View loader extensions:

```bash
assembly extensions
```

**Example:**
```bash
beep> assembly extensions

┌──────────────────────────────────────────────────────┐
│            Loader Extensions (3)                     │
├────────────────────┬────────────┬──────────────────┤
│ Class Name         │ Assembly   │ Namespace        │
├────────────────────┼────────────┼──────────────────┤
│ ShellExtScanner    │ BeepShell  │ BeepShell.Infra  │
└────────────────────┴────────────┴──────────────────┘
```

### 8. Create Instance

Instantiate types dynamically:

```bash
# Create from type name
assembly create MyNamespace.MyClass

# Specify assembly
assembly create MyClass --assembly MyExtension
assembly create MyClass -a MyExtension.dll
```

**Example:**
```bash
beep> assembly create TheTechIdea.Beep.Data.SqlServerSource

✓ Instance created: TheTechIdea.Beep.Data.SqlServerSource
Type: SqlServerSource
```

### 9. Nugget Management

Manage NuGet packages:

```bash
# Load nugget package
assembly nugget load C:\Packages\MyPackage

# Load nugget DLL
assembly nugget load MyPackage.dll
```

**What nuggets are:**
- NuGet package assemblies
- Tracked separately for unloading
- Can include dependencies
- Support versioning

## Common Workflows

### Discover Available Data Sources

```bash
# 1. List all assemblies
asm list

# 2. Scan for extensions
asm scan --all

# 3. Find IDataSource types
asm types --interface IDataSource

# 4. View drivers
asm drivers
```

### Load Custom Extension

```bash
# 1. Load the assembly
asm load C:\MyExtensions\CustomDriver.dll

# 2. Scan it
asm scan --path C:\MyExtensions\CustomDriver.dll

# 3. Verify types
asm types --assembly CustomDriver

# 4. Check drivers
asm drivers
```

### Troubleshoot Assembly Issues

```bash
# Check what's loaded
asm list --verbose

# Look for specific types
asm types --interface IMyInterface

# Verify extensions
asm extensions

# Reload if needed
asm unload MyAssembly
asm load MyAssembly.dll
asm scan --path MyAssembly.dll
```

### Dynamic Type Creation

```bash
# Find the type you need
asm types --interface IDataSource -l 5

# Create instance
asm create TheTechIdea.Beep.Data.MySqlSource
```

## Integration with Other Commands

Assembly commands work together with other BeepShell features:

### With Data Sources

```bash
# Load driver assembly
asm load MySqlDriver.dll

# Configure connection
config connection add --name mysql1 --type MySql

# Test connection
ds test mysql1
```

### With Extensions

```bash
# Load extension
asm load MyExtension.dll

# Verify it's recognized
extensions

# Use extension commands
mycommand --help
```

### With Plugins

```bash
# For hot-reload, use plugin instead
plugin load MyPlugin.dll

# Check health
plugin health myplugin-id

# Reload after changes
plugin reload myplugin-id
```

## Best Practices

### 1. Scan After Loading

Always scan assemblies after loading to discover types:

```bash
asm load MyExtension.dll
asm scan --path MyExtension.dll
```

### 2. Use Filters for Performance

When listing types, use filters to reduce output:

```bash
# Instead of
asm types

# Use
asm types --assembly MyExtension --interface IDataSource
```

### 3. Check Before Unloading

Verify what will be affected:

```bash
# Check types
asm types --assembly MyExtension

# Then unload
asm unload MyExtension
```

### 4. Use Aliases for Efficiency

Set up shortcuts for common operations:

```bash
alias al asm list
alias at asm types
alias ad asm drivers
```

### 5. Verbose for Troubleshooting

Use verbose mode when debugging:

```bash
asm list --verbose
asm types --assembly MyExt --verbose
```

## Advanced Usage

### Scripting Assembly Operations

```bash
# Load multiple assemblies
asm load Extension1.dll
asm load Extension2.dll
asm load Extension3.dll

# Scan all
asm scan --all

# Verify
asm drivers
asm extensions
```

### Finding Implementation Types

```bash
# Find all workflow actions
asm types --interface IWorkFlowAction

# Find all view models
asm types --interface IBeepViewModel

# Find specific implementations
asm types --assembly MyExtension --interface IDataSource
```

### Assembly Diagnostics

```bash
# What assemblies are loaded?
asm list --verbose

# What types are available?
asm types --limit 1000

# What drivers do I have?
asm drivers

# What extensions are active?
asm extensions
```

## Error Handling

### Assembly Not Found

```bash
beep> asm load NonExistent.dll
Path not found: NonExistent.dll
```

**Solution:** Verify the path exists.

### Failed to Load

```bash
beep> asm load BadAssembly.dll
Error: Could not load file or assembly 'BadAssembly'
```

**Causes:**
- Missing dependencies
- Wrong .NET version
- Corrupted file
- Architecture mismatch

### No Types Found

```bash
beep> asm types --assembly MyAssembly
No types found
```

**Solution:** Ensure assembly is scanned:
```bash
asm scan --path MyAssembly.dll
```

## Integration Points

### With DMEEditor

Assembly commands directly use `editor.AssemblyHandler`:

```csharp
// Shell command calls
var assemblies = editor.AssemblyHandler.LoadedAssemblies;

// Load assembly
editor.AssemblyHandler.LoadAssembly(path);

// Get types
var type = editor.AssemblyHandler.GetType(typeName);
```

### With Configuration

Drivers discovered by assembly scanning appear in configuration:

```csharp
var drivers = editor.ConfigEditor.DataDriversClasses;
```

### With Extensions

Shell extensions are discovered through assembly scanning:

```csharp
var extensions = editor.AssemblyHandler.LoaderExtensionClasses;
```

## Summary

Assembly management is the **foundation** of BeepShell:

✅ **Base Commands** - Assembly operations are first-class  
✅ **Driver Discovery** - Find all available data sources  
✅ **Type Resolution** - Dynamic type creation and reflection  
✅ **Extension Loading** - Load and scan custom code  
✅ **Diagnostics** - Inspect what's loaded and available  
✅ **Integration** - Works with all other shell features  

Master these commands to unlock the full power of BeepDM's dynamic, extensible architecture!
