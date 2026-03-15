---
name: environmentservice
description: Guidance for EnvironmentService utilities that create Beep data folders and register mappings, connections, and queries.
---

# EnvironmentService Guide

Use this skill when initializing BeepDM folders or when registering default mappings, connections, and query repositories.

## Core Responsibilities
- Create platform-appropriate root folders for Beep data
- Create container and app folders
- Register default mappings, connections, and query repositories

## Key Methods
- `CreateMainFolder()` and `CreateContainerfolder(name)`
- `CreateAppfolder(container, appfolder)` and `CreateAppfolder(appfolder)`
- `CreateBeepMapping(editor)`
- `AddAllConnectionConfigurations(editor)`
- `AddAllDataSourceMappings(editor)`
- `AddAllDataSourceQueryConfigurations(editor)`

## Validation
- Check returned path strings for empty values.
- Ensure `editor` is not null before calling `CreateBeepMapping`.
- Avoid duplicate registrations by relying on internal flags.

## Pitfalls
- Passing unsafe folder names without sanitizing can fail on some platforms.
- Calling mapping registration multiple times can add duplicates if flags are reset.
- Creating app folders before `CreateMainFolder` results in empty paths.

## File Locations
- DataManagementEngineStandard/Services/EnvironmentService.cs

## Example
```csharp
var editor = new DMEEditor();

// Create standard Beep folder
var root = EnvironmentService.CreateMainFolder();

// Create a container and app folder
var container = EnvironmentService.CreateContainerfolder("MyContainer");
var appPath = EnvironmentService.CreateAppfolder("MyContainer", "MyApp");

// Register mappings and queries
editor.CreateBeepMapping();
```

## Task-Specific Examples

### Create App Folder Under Main Root
```csharp
var appFolder = EnvironmentService.CreateAppfolder("MyApp");
```

### Register Default Connections And Queries
```csharp
EnvironmentService.AddAllConnectionConfigurations(editor);
EnvironmentService.AddAllDataSourceMappings(editor);
EnvironmentService.AddAllDataSourceQueryConfigurations(editor);
```
