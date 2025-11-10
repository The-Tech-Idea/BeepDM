# NuggetManager Usage Guide

## Overview
The `NuggetManager` class provides a unified interface for loading and managing NuGet packages (nuggets) in both traditional and isolated assembly loading scenarios.

## Basic Usage

### Initialization

```csharp
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

// Create NuggetManager instance
var nuggetManager = new NuggetManager(logger, errorObject, utilFunction);
```

### Loading a Nugget

#### Option 1: Shared Context (Traditional Loading)
```csharp
// Load nugget into shared AppDomain (cannot be unloaded)
bool success = nuggetManager.LoadNugget(@"C:\Plugins\MyPlugin.dll", useIsolatedContext: false);

if (success)
{
    Console.WriteLine("Nugget loaded successfully in shared context");
}
```

#### Option 2: Isolated Context (True Unloading)
```csharp
// Load nugget into isolated context (can be unloaded)
bool success = nuggetManager.LoadNugget(@"C:\Plugins\MyPlugin", useIsolatedContext: true);

if (success)
{
    Console.WriteLine("Nugget loaded successfully in isolated context");
}
```

### Getting Nugget Information

```csharp
// Get nugget info
var nuggetInfo = nuggetManager.GetNuggetInfo("MyPlugin");

if (nuggetInfo != null)
{
    Console.WriteLine($"Name: {nuggetInfo.Name}");
    Console.WriteLine($"Version: {nuggetInfo.Version}");
    Console.WriteLine($"Loaded At: {nuggetInfo.LoadedAt}");
    Console.WriteLine($"Assembly Count: {nuggetInfo.LoadedAssemblies.Count}");
    Console.WriteLine($"Is Shared Context: {nuggetInfo.IsSharedContext}");
}
```

### Getting Assemblies from a Nugget

```csharp
// Get all assemblies loaded by a nugget
List<Assembly> assemblies = nuggetManager.GetNuggetAssemblies("MyPlugin");

foreach (var assembly in assemblies)
{
    Console.WriteLine($"Assembly: {assembly.FullName}");
}
```

### Checking if a Nugget is Loaded

```csharp
if (nuggetManager.IsNuggetLoaded("MyPlugin"))
{
    Console.WriteLine("MyPlugin is currently loaded");
}
```

### Finding Which Nugget Owns an Assembly

```csharp
string assemblyPath = @"C:\Plugins\MyPlugin\MyPlugin.dll";
string nuggetName = nuggetManager.FindNuggetByAssemblyPath(assemblyPath);

if (!string.IsNullOrEmpty(nuggetName))
{
    Console.WriteLine($"Assembly belongs to nugget: {nuggetName}");
}
```

### Unloading a Nugget

```csharp
// Unload a nugget
bool success = nuggetManager.UnloadNugget("MyPlugin");

if (success)
{
    Console.WriteLine("Nugget unloaded successfully");
    // Note: For shared context, only tracking is removed
    // For isolated context, memory is truly freed
}
```

### Getting All Loaded Nuggets

```csharp
List<NuggetInfo> allNuggets = nuggetManager.GetAllNuggets();

foreach (var nugget in allNuggets)
{
    Console.WriteLine($"Nugget: {nugget.Name}, Assemblies: {nugget.LoadedAssemblies.Count}");
}
```

### Clearing All Nuggets

```csharp
// Clear all loaded nuggets
nuggetManager.Clear();
Console.WriteLine("All nuggets cleared");
```

## Advanced Scenarios

### Loading Multiple Nuggets

```csharp
var nuggetPaths = new[]
{
    @"C:\Plugins\Plugin1.dll",
    @"C:\Plugins\Plugin2",
    @"C:\Plugins\Plugin3.dll"
};

foreach (var path in nuggetPaths)
{
    try
    {
        bool success = nuggetManager.LoadNugget(path, useIsolatedContext: true);
        Console.WriteLine($"Loaded {Path.GetFileName(path)}: {success}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading {path}: {ex.Message}");
    }
}
```

### Plugin Hot-Swapping

```csharp
// Unload old version
nuggetManager.UnloadNugget("MyPlugin_v1");

// Load new version
bool success = nuggetManager.LoadNugget(@"C:\Plugins\MyPlugin_v2.dll", useIsolatedContext: true);

if (success)
{
    Console.WriteLine("Plugin upgraded successfully");
}
```

### Monitoring Nugget Usage

```csharp
// Get all loaded nuggets
var nuggets = nuggetManager.GetAllNuggets();

foreach (var nugget in nuggets)
{
    var timeLoaded = DateTime.UtcNow - nugget.LoadedAt;
    Console.WriteLine($"{nugget.Name}:");
    Console.WriteLine($"  Loaded for: {timeLoaded.TotalMinutes:F2} minutes");
    Console.WriteLine($"  Assemblies: {nugget.LoadedAssemblies.Count}");
    Console.WriteLine($"  Can Unload: {!nugget.IsSharedContext}");
}
```

## Integration with AssemblyHandler

### Using with AssemblyHandler

```csharp
// AssemblyHandler uses NuggetManager internally
var assemblyHandler = new AssemblyHandler(configEditor, errorObject, logger, utilFunction);

// Load a nugget through AssemblyHandler
bool success = assemblyHandler.LoadNugget(@"C:\Plugins\MyPlugin.dll");

// AssemblyHandler will:
// 1. Use NuggetManager to load the nugget
// 2. Add assemblies to its tracking lists
// 3. Scan assemblies for types
```

### Using with SharedContextAssemblyHandler

```csharp
// SharedContextAssemblyHandler also uses NuggetManager
var handler = new SharedContextAssemblyHandler(configEditor, errorObject, logger, utilFunction);

// Load nugget - will use NuggetManager internally
bool success = handler.LoadNugget(@"C:\Plugins\MyPlugin");

// The handler will:
// 1. Load via NuggetManager
// 2. Scan using ScanningService
// 3. Track in SharedContextManager
```

## Best Practices

### 1. Choose the Right Loading Mode

```csharp
// Use shared context for:
// - Core libraries that shouldn't be unloaded
// - Long-running plugins
// - Plugins with complex dependencies

nuggetManager.LoadNugget(path, useIsolatedContext: false);

// Use isolated context for:
// - Plugins that need hot-swapping
// - Testing and development
// - Plugins with potential memory leaks

nuggetManager.LoadNugget(path, useIsolatedContext: true);
```

### 2. Handle Errors Properly

```csharp
try
{
    bool success = nuggetManager.LoadNugget(path, useIsolatedContext: true);
    
    if (!success)
    {
        // Check error object for details
        Console.WriteLine($"Failed to load nugget: {errorObject.Message}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Exception loading nugget: {ex.Message}");
    // Log and handle appropriately
}
```

### 3. Clean Up When Done

```csharp
// In dispose or shutdown logic
public void Cleanup()
{
    try
    {
        // Unload specific nuggets
        nuggetManager.UnloadNugget("TempPlugin1");
        nuggetManager.UnloadNugget("TempPlugin2");
        
        // Or clear all if shutting down
        nuggetManager.Clear();
    }
    catch (Exception ex)
    {
        logger.WriteLog($"Error during cleanup: {ex.Message}");
    }
}
```

### 4. Track What's Loaded

```csharp
// Before loading, check if already loaded
if (!nuggetManager.IsNuggetLoaded("MyPlugin"))
{
    nuggetManager.LoadNugget(pluginPath, useIsolatedContext: true);
}
else
{
    Console.WriteLine("Plugin already loaded");
}
```

### 5. Monitor Memory Usage

```csharp
// For isolated contexts, unloading will free memory
var beforeMemory = GC.GetTotalMemory(true);

nuggetManager.LoadNugget(path, useIsolatedContext: true);
// Use the plugin...

nuggetManager.UnloadNugget(nuggetName);

var afterMemory = GC.GetTotalMemory(true);
var freedMemory = beforeMemory - afterMemory;
Console.WriteLine($"Freed memory: {freedMemory / 1024 / 1024} MB");
```

## Common Patterns

### Pattern 1: Plugin Directory Loader

```csharp
public void LoadPluginsFromDirectory(string directory)
{
    if (!Directory.Exists(directory))
        return;

    foreach (var dllFile in Directory.GetFiles(directory, "*.dll"))
    {
        try
        {
            nuggetManager.LoadNugget(dllFile, useIsolatedContext: true);
        }
        catch (Exception ex)
        {
            logger.WriteLog($"Failed to load plugin {dllFile}: {ex.Message}");
        }
    }
}
```

### Pattern 2: Safe Plugin Reload

```csharp
public bool ReloadPlugin(string pluginName, string newPath)
{
    try
    {
        // Unload old version if exists
        if (nuggetManager.IsNuggetLoaded(pluginName))
        {
            nuggetManager.UnloadNugget(pluginName);
        }

        // Load new version
        return nuggetManager.LoadNugget(newPath, useIsolatedContext: true);
    }
    catch (Exception ex)
    {
        logger.WriteLog($"Error reloading plugin: {ex.Message}");
        return false;
    }
}
```

### Pattern 3: Plugin Dependency Chain

```csharp
public bool LoadPluginWithDependencies(string pluginPath, List<string> dependencies)
{
    // Load dependencies first
    foreach (var depPath in dependencies)
    {
        if (!nuggetManager.LoadNugget(depPath, useIsolatedContext: false))
        {
            return false;
        }
    }

    // Load main plugin in isolated context
    return nuggetManager.LoadNugget(pluginPath, useIsolatedContext: true);
}
```

## Troubleshooting

### Issue: Nugget Won't Load

```csharp
// Check if path exists
if (!File.Exists(path) && !Directory.Exists(path))
{
    Console.WriteLine("Path does not exist");
}

// Check if already loaded
if (nuggetManager.IsNuggetLoaded(nuggetName))
{
    Console.WriteLine("Already loaded - unload first");
}

// Check error object
if (!success)
{
    Console.WriteLine($"Error: {errorObject.Message}");
}
```

### Issue: Memory Not Released After Unload

```csharp
// Only isolated contexts can be truly unloaded
var nuggetInfo = nuggetManager.GetNuggetInfo(nuggetName);
if (nuggetInfo != null && nuggetInfo.IsSharedContext)
{
    Console.WriteLine("Cannot unload - loaded in shared context");
    Console.WriteLine("Reload with useIsolatedContext: true for true unloading");
}
```

### Issue: Assembly Not Found After Loading

```csharp
// Check if assemblies were loaded
var assemblies = nuggetManager.GetNuggetAssemblies(nuggetName);
if (assemblies.Count == 0)
{
    Console.WriteLine("No assemblies loaded from nugget");
}

// Verify assembly is in the list
foreach (var asm in assemblies)
{
    Console.WriteLine($"Loaded: {asm.FullName}");
}
```

## Performance Considerations

- **Shared Context**: Faster loading, no unloading overhead, shares types across app domain
- **Isolated Context**: Slower loading, proper unloading, isolated type resolution
- **Directory vs File**: Loading from directory will scan all DLLs recursively
- **Caching**: NuggetManager tracks loaded nuggets to prevent duplicate loads

## Conclusion

The NuggetManager provides a flexible, easy-to-use interface for managing plugin/nugget lifecycles in your application. Use shared context for permanent plugins and isolated context for dynamic, hot-swappable plugins.
