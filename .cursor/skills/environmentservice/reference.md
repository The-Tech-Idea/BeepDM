# EnvironmentService Quick Reference

## Folder Creation
```csharp
// Root folder
var root = EnvironmentService.CreateMainFolder();

// Container folder
var container = EnvironmentService.CreateContainerfolder("MyContainer");

// App folder under container
var app = EnvironmentService.CreateAppfolder("MyContainer", "MyApp");
```

## Mapping and Configuration
```csharp
editor.CreateBeepMapping();
// Internally calls:
// - AddAllConnectionConfigurations
// - AddAllDataSourceMappings
// - AddAllDataSourceQueryConfigurations
```

## File Location
- DataManagementEngineStandard/Services/EnvironmentService.cs
