# AssemblyHandler vs SharedContextAssemblyHandler — Detailed Diff

> This document compares the two `IAssemblyHandler` implementations in BeepDM:
> - **AssemblyHandler** (4 partial files, ~1875 lines) — Legacy, direct implementation
> - **SharedContextAssemblyHandler** (1 file, 1487 lines) — Modern, delegation pattern

---

## Architecture Overview

| Aspect | AssemblyHandler | SharedContextAssemblyHandler |
|--------|----------------|------------------------------|
| **File structure** | 4 partial files: Core, Helpers, Loaders, Scanning | Single file (1487 lines) |
| **Pattern** | Direct implementation — all logic inline | Delegation — delegates to sub-components |
| **Sub-components** | `NuggetManager` only | `SharedContextManager`, `DriverDiscoveryAssistant`, `IScanningService`, `NuggetManager`, `PluginRegistry`, `PluginInstaller` |
| **Namespace** | `TheTechIdea.Beep.Addin` | `TheTechIdea.Beep.Addin` |
| **Interface** | `IAssemblyHandler` | `IAssemblyHandler` |

---

## Constructor & Initialization

### AssemblyHandler
```csharp
public AssemblyHandler(IConfigEditor configEditor, IErrorsInfo errorObject, IDMLogger logger, IUtil utilfunction)
```
- Sets 4 properties directly
- Hooks `AppDomain.CurrentDomain.AssemblyResolve`
- Creates `NuggetManager` (single sub-component)
- Calls `InitializeLoadedAssemblies()` using `DependencyContext.Default`
- No null checks on parameters

### SharedContextAssemblyHandler
```csharp
public SharedContextAssemblyHandler(IConfigEditor configEditor, IErrorsInfo errorObject, IDMLogger logger, IUtil utilfunction)
```
- **Throws `ArgumentNullException`** for null `configEditor`, `errorObject`, `logger`, `utilfunction`
- Creates 6 sub-components: `PluginRegistry`, `PluginInstaller`, `SharedContextManager`, `NuggetManager`, `DriverDiscoveryAssistant`, `ScanningService`
- Hooks `AppDomain.CurrentDomain.AssemblyResolve`
- Calls `InitializeLoadedAssemblies()` using `DependencyContext.Default`
- Wraps initialization in try-catch

---

## Fields & Storage

### AssemblyHandler
```csharp
private readonly NuggetManager _nuggetManager;
private readonly Dictionary<string, Type> _typeCache = new();
private readonly ConcurrentDictionary<string, Assembly> _loadedAssemblyCache = new();
private IProgress<PassedArgs> Progress;
private CancellationToken Token;
```

### SharedContextAssemblyHandler
```csharp
private readonly SharedContextManager _sharedContextManager;
private readonly DriverDiscoveryAssistant _driverAssistant;
private readonly IScanningService _scanningService;
private readonly NuggetManager _nuggetManager;
private readonly PluginRegistry _pluginRegistry;
private readonly PluginInstaller _pluginInstaller;
private readonly List<Assembly> _loadedAssemblies = new();
private readonly ConcurrentDictionary<string, Assembly> _loadedAssemblyCache = new();
private IProgress<PassedArgs> _progress;
private CancellationToken _token;
```

**Key difference:** SharedContext creates 6 sub-components; AssemblyHandler has inline logic with just `_typeCache` and `_nuggetManager`.

---

## Properties

### Common Properties (both have)
| Property | AssemblyHandler | SharedContextAssemblyHandler |
|----------|----------------|------------------------------|
| `ConfigEditor` | Auto-property | Auto-property |
| `ErrorObject` | Auto-property | Auto-property |
| `Logger` | Auto-property | Auto-property |
| `Utilfunction` | Auto-property | Auto-property |
| `LoadedAssemblies` | `List<Assembly>` auto-property | **Defensive getter**: returns `_loadedAssemblies.ToList()` |
| `Assemblies` | `List<assemblies_rep>` auto-property | `List<assemblies_rep>` auto-property |
| `DataSourcesClasses` | `List<AssemblyClassDefinition>` auto-property | **Delegates to `ConfigEditor.DataSourcesClasses`** |
| `DataDriversConfig` | `List<ConnectionDriversConfig>` auto-property | Auto-property |

### Property Differences

**`LoadedAssemblies` setter:**
- **AssemblyHandler**: Simple `set;`
- **SharedContext**: Defensive: `_loadedAssemblies.Clear(); _loadedAssemblies.AddRange(value);`

**`DataSourcesClasses`:**
- **AssemblyHandler**: Own `List<AssemblyClassDefinition>` — managed locally
- **SharedContext**: **Delegates to `ConfigEditor.DataSourcesClasses`** — single source of truth in config

### Extra Properties (SharedContext only)
```csharp
public PluginRegistry PluginRegistry => _pluginRegistry;
public PluginInstaller PluginInstaller => _pluginInstaller;
```

---

## Assembly Resolution (CurrentDomain_AssemblyResolve)

### AssemblyHandler — 2-tier resolution
```
1. Search LoadedAssemblies by name match
2. Search _loadedAssemblyCache by name match
→ Return null if not found
```

### SharedContextAssemblyHandler — 3-tier resolution with on-demand loading
```
1. Search _sharedContextManager loaded assemblies by name match
2. Search local _loadedAssemblies by name match  
3. ON-DEMAND: Try to load from configured folders via _sharedContextManager.LoadNuggetAsync()
   - Scans ConnectionDriversPath, DataSourcesPath, ProjectClassPath, and ExePath subfolders
   - Uses GetAwaiter().GetResult() for sync context
→ Return null if all fail
```

**Key difference:** SharedContext will attempt to **discover and load** a missing assembly at resolve time. AssemblyHandler only checks already-loaded assemblies.

---

## Type Resolution (GetType)

### AssemblyHandler — 3-tier with caching
```csharp
public Type GetType(string classname)
```
1. Check `_typeCache` dictionary
2. Try `Type.GetType(classname)` directly
3. Search all `AppDomain.CurrentDomain.GetAssemblies()` → `GetTypes()`
4. Search referenced assemblies of loaded assemblies
5. Search `LoadedAssemblies` list
6. Cache result in `_typeCache` on success

### SharedContextAssemblyHandler — delegates to SharedContextManager
```csharp
public Type GetType(string classname)
{
    return _sharedContextManager.GetType(classname);
}
```
- SharedContextManager has its own internal cache and resolution strategy
- Resolution details hidden behind the manager

---

## Instance Creation

### AssemblyHandler
```csharp
public object CreateInstanceFromString(string typeName, params object[] args)
```
- Uses own `GetType()` + `Activator.CreateInstance(type, args)`
- Direct inline implementation
- Two overloads (with and without args)

### SharedContextAssemblyHandler
```csharp
public object CreateInstanceFromString(string typeName, params object[] args)
{
    return _sharedContextManager.CreateInstance(typeName, args);
}
```
- Delegates to `SharedContextManager.CreateInstance()`
- SharedContextManager may use factory caching internally
- Also has `CreateInstanceFromAssembly(Assembly, string, object[])` — delegates to SharedContextManager

---

## Assembly Loading

### LoadAssembly(string path)

| Aspect | AssemblyHandler | SharedContextAssemblyHandler |
|--------|----------------|------------------------------|
| Method | `Assembly.LoadFrom(path)` directly | `_sharedContextManager.LoadNuggetAsync(path).GetAwaiter().GetResult()` |
| Caching | `_loadedAssemblyCache[path] = assembly` | `_loadedAssemblyCache[path] = assembly` |
| Tracking | Adds to `LoadedAssemblies` and `Assemblies` | Adds to `_loadedAssemblies` and `Assemblies` |
| Error handling | Returns null on failure | Returns null on failure |

### LoadAssembly(string path, FolderFileTypes fileTypes)

| Aspect | AssemblyHandler | SharedContextAssemblyHandler |
|--------|----------------|------------------------------|
| Loading | Calls `LoadAssemblySafely()` → `Assembly.LoadFrom()` | Calls `ResolveFrameworkSpecificPath()` first, then `_sharedContextManager.LoadNuggetAsync()` |
| Framework resolution | **None** — loads whatever DLL is found | **TFM-aware** — resolves `net8.0`/`net6.0`/`netstandard2.0` paths |
| Subfolder scanning | Only in `LoadFolderAssemblies` helper | Inline 3-level folder fallback + subfolder scanning |

**Key difference:** SharedContext has `ResolveFrameworkSpecificPath()` for intelligent TFM selection. AssemblyHandler loads DLLs directly without framework consideration.

### LoadAllAssembly (Main Orchestrator)

Both follow the same sequence:
```
1. GetBuiltinClasses
2. LoadAssemblyFormRunTime
3. GetExtensionScanners
4. LoadFolderAssemblies (ConnectionDriver, DataSources, ProjectClass, OtherDLL, Addin)
5. ScanForDrivers
6. ScanForDataSources
7. ScanProjectAndAddinAssemblies
8. AddEngineDefaultDrivers
9. CheckDriverAlreadyExistinList
10. ScanExtensions
```

Differences:
- **AssemblyHandler**: All logic inline with direct method calls
- **SharedContext**: Delegates scanning to `_scanningService` and `_driverAssistant`
- **SharedContext**: Uses `_driverAssistant.LoadDriverClasses()` and `_driverAssistant.LoadDataSourceClasses()` for folder loading (has 3-level fallback)

---

## Scanning

### ScanAssembly

| Aspect | AssemblyHandler | SharedContextAssemblyHandler |
|--------|----------------|------------------------------|
| Implementation | Inline with `Parallel.ForEach` for >100 types | Delegates to `_scanningService.ScanAssembly()` |
| Parallelism | Uses `ConcurrentBag<TypeInfo>` + `Parallel.ForEach` (>100 types threshold) | Unknown (inside ScanningService) |
| Error handling | 3-fallback `GetTypes()`: direct → `GetExportedTypes()` → skip | Delegates to service |
| Type processing | `ProcessTypeInfo()` checks 10 interfaces inline | Delegates to service |

### ProcessTypeInfo (10 interfaces checked)

AssemblyHandler checks these inline:
1. `ILoaderExtention`
2. `IDataSource`
3. `IWorkFlowAction`
4. `IDM_Addin`
5. `IWorkFlowStep`
6. `IBeepViewModel`
7. `IWorkFlowEditor`
8. `IWorkFlowRule`
9. `IFunctionExtension`
10. `IPrintManager` + `IAddinVisSchema`

SharedContextAssemblyHandler delegates all of this to `_scanningService`.

---

## Class Definition Building (GetAssemblyClassDefinition)

### AssemblyHandler — Complex inline (~100 lines)
- Processes `AddinAttribute`, `AddinVisSchema`, `CommandAttribute`, `IOrder`
- Checks `ILocalDB`, `IInMemoryDB`, `IDataSource` for `DatasourceCategory`
- Adds to `DataSourcesClasses`
- All logic inline in Helpers partial

### SharedContextAssemblyHandler — Delegates
```csharp
public AssemblyClassDefinition GetAssemblyClassDefinition(TypeInfo type, string[] Filters = null)
{
    return _scanningService.GetAssemblyClassDefinition(type, Filters);
}
```

---

## Driver Methods

### GetDrivers

| AssemblyHandler | SharedContextAssemblyHandler |
|----------------|------------------------------|
| **STUB** — returns `new List<ConnectionDriversConfig>()`, logs "Not implemented" | Delegates to `_driverAssistant.GetDrivers()` |

### AddEngineDefaultDrivers

| AssemblyHandler | SharedContextAssemblyHandler |
|----------------|------------------------------|
| **STUB** — logs and returns `true` | Delegates to `_driverAssistant.AddDefaultDrivers()` |

### CheckDriverAlreadyExistinList

| AssemblyHandler | SharedContextAssemblyHandler |
|----------------|------------------------------|
| Inline implementation — deduplicates by `PackageName` | Delegates to `_driverAssistant.MergeDrivers()` |

---

## Addin / Hierarchy Methods

### GetAddinObjects

| AssemblyHandler | SharedContextAssemblyHandler |
|----------------|------------------------------|
| **Full implementation** — iterates `DataSourcesClasses`, processes `IDM_Addin`, builds `ParentChildObject` list | **STUB** — returns `new List<ParentChildObject>()` |

### RearrangeAddin

| AssemblyHandler | SharedContextAssemblyHandler |
|----------------|------------------------------|
| **Full implementation** — reorders `DataSourcesClasses` by `IOrder.Order` + `AddinAttribute.order` | **STUB** — returns empty `AddinTreeStructure` |

### BuildAddinHierarchy (SharedContext only)
SharedContextAssemblyHandler has a unique `BuildAddinHierarchy()` method that creates tree structures from `_scanningService.GetAllDiscoveredAddins()`. **Not present in AssemblyHandler.**

---

## Nugget Management

### LoadNugget

Both delegate to `_nuggetManager.LoadNugget(path, useIsolatedContext: true)`, then add assemblies to tracking and scan them. **Identical behavior.**

### UnloadNugget

Both delegate to `_nuggetManager.UnloadNugget(name)`. **Identical behavior.**

### LoadNuggetFromNuGetAsync

| AssemblyHandler | SharedContextAssemblyHandler |
|----------------|------------------------------|
| **Has this method** — creates new `NuggetPackageDownloader` per call, downloads with deps, loads via `_nuggetManager`, optionally installs to app dir + process host | **Does NOT have this method** |
| **NOT on `IAssemblyHandler` interface** | N/A |

### UnloadAssembly

Both remove from `LoadedAssemblies`, `Assemblies`, and `_loadedAssemblyCache`. **Similar behavior.**

---

## Type Cache

### AssemblyHandler
```csharp
public void AddTypeToCache(string typeName, Type type)
{
    if (!_typeCache.ContainsKey(typeName))
        _typeCache[typeName] = type;
}

public Type GetTypeFromCache(string typeName)
{
    _typeCache.TryGetValue(typeName, out var type);
    return type;
}

public void ClearTypeCache() => _typeCache.Clear();
```

### SharedContextAssemblyHandler
```csharp
public void AddTypeToCache(string typeName, Type type)
{
    // SharedContextManager handles caching - no-op here
}

public Type GetTypeFromCache(string typeName) => null;  // Delegates internally
public void ClearTypeCache() { }  // No-op
```

**Key difference:** AssemblyHandler manages its own `Dictionary<string, Type>` type cache. SharedContext treats these as no-ops because `SharedContextManager` has its own internal caching.

---

## RunMethod

### AssemblyHandler
```csharp
public object RunMethod(IDMEEditor dMEEditor, string FullClassName, string MethodName)
```
- Uses `GetType()` → `Activator.CreateInstance` → `MethodInfo.Invoke`
- Direct implementation

### SharedContextAssemblyHandler
```csharp
public object RunMethod(IDMEEditor dMEEditor, string FullClassName, string MethodName)
```
- Finds method from `ConfigEditor.BranchesClasses` by class name
- Creates instance via `_sharedContextManager.CreateInstance`
- Invokes method
- **Different approach**: Looks up class definition first, then creates instance

---

## Framework Resolution (SharedContext only)

### ResolveFrameworkSpecificPath
```csharp
private string ResolveFrameworkSpecificPath(string basePath)
```
- Checks for `lib/` subdirectory containing TFM folders
- Priority: `net8.0` > `net7.0` > `net6.0` > `netstandard2.1` > `netstandard2.0`
- Falls back to `basePath` if no TFM folders found

### GetCurrentTargetFramework
```csharp
private string GetCurrentTargetFramework()
```
- Returns `"net" + Environment.Version.Major + ".0"` (e.g., `"net8.0"`)

**Not present in AssemblyHandler** — loads DLLs without TFM consideration.

---

## Statistics & Discovery (SharedContext only)

### GetLoadingStatistics
```csharp
public string GetLoadingStatistics()
```
Returns formatted string with:
- Total loaded assemblies count
- Assemblies by `FolderFileTypes`
- Loaded nuggets from `_nuggetManager`
- Assembly list from `_sharedContextManager`

### GetDiscoveryStatistics
```csharp
public string GetDiscoveryStatistics()
```
Returns counts of discovered: DataSources, Drivers, Addins, LoaderExtensions, WorkflowActions, ViewModels.

### GetAllDiscovered* methods (6)
```csharp
public List<AssemblyClassDefinition> GetAllDiscoveredDrivers()
public List<AssemblyClassDefinition> GetAllDiscoveredDataSources()
public List<AssemblyClassDefinition> GetAllDiscoveredAddins()
public List<AssemblyClassDefinition> GetAllDiscoveredLoaderExtensions()
public List<AssemblyClassDefinition> GetAllDiscoveredWorkflowActions()
public List<AssemblyClassDefinition> GetAllDiscoveredViewModels()
```

**None of these exist in AssemblyHandler.**

---

## Logging

| AssemblyHandler | SharedContextAssemblyHandler |
|----------------|------------------------------|
| `Logger?.WriteLog(message)` | `Logger?.LogWithContext(message, caller)` |
|  Standard logging | **Context-aware logging** with caller info |

---

## Dispose

### AssemblyHandler
```csharp
public void Dispose()
{
    _nuggetManager?.Dispose();
    _typeCache?.Clear();
    _loadedAssemblyCache?.Clear();
    AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
}
```

### SharedContextAssemblyHandler
```csharp
public void Dispose()
{
    _nuggetManager?.Dispose();
    _sharedContextManager?.Dispose();
    _pluginRegistry?.Dispose();
    _loadedAssemblies?.Clear();
    _loadedAssemblyCache?.Clear();
    AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
}
```

**Key difference:** SharedContext disposes 3 sub-components (`_nuggetManager`, `_sharedContextManager`, `_pluginRegistry`). AssemblyHandler disposes 1 (`_nuggetManager`).

---

## Summary: What AssemblyHandler Has That SharedContext Doesn't

| Feature | Details |
|---------|---------|
| `LoadNuggetFromNuGetAsync` | Full NuGet download with dependencies, install, process host |
| `GetAddinObjects` | Real implementation (builds `ParentChildObject` hierarchy) |
| `RearrangeAddin` | Real implementation (reorders by `IOrder.Order`) |
| `GetAssemblyClassDefinition` | Full inline implementation with attribute processing |
| Type cache (`_typeCache`) | Real `Dictionary<string, Type>` cache with `Add/Get/Clear` |
| `GetType` 3-tier resolution | Domain assemblies → referenced assemblies → loaded assemblies |
| `LoadAssemblySafely` | Safe assembly loading with fallback |

## Summary: What SharedContext Has That AssemblyHandler Doesn't

| Feature | Details |
|---------|---------|
| **On-demand assembly resolution** | `CurrentDomain_AssemblyResolve` tier 3: discovers and loads from folders |
| **Framework-specific path resolution** | `ResolveFrameworkSpecificPath` with TFM priority |
| **6 sub-component delegation** | `SharedContextManager`, `DriverDiscoveryAssistant`, `IScanningService`, `PluginRegistry`, `PluginInstaller` |
| **`PluginRegistry` / `PluginInstaller`** | Plugin lifecycle management |
| **`GetAllDiscovered*` methods (6)** | Query discovered types by category |
| **`GetLoadingStatistics`** | Formatted load statistics |
| **`GetDiscoveryStatistics`** | Formatted discovery statistics |
| **`BuildAddinHierarchy`** | Tree structure from scanning service |
| **Real `GetDrivers`** | Delegates to `DriverDiscoveryAssistant` (not a stub) |
| **Real `AddEngineDefaultDrivers`** | Delegates to `DriverDiscoveryAssistant` (not a stub) |
| **Defensive property setters** | `LoadedAssemblies` uses `Clear+AddRange` pattern |
| **`DataSourcesClasses` from ConfigEditor** | Single source of truth (not own list) |
| **Context-aware logging** | `Logger?.LogWithContext()` |
| **Null-check constructor** | `ArgumentNullException` for all parameters |

---

## Recommendation

**AssemblyHandler** should absorb the following from SharedContext:
1. On-demand assembly resolution (tier 3 in `AssemblyResolve`)
2. Framework-specific path resolution (`ResolveFrameworkSpecificPath`)
3. Statistics methods (`GetLoadingStatistics`, `GetDiscoveryStatistics`)
4. Discovery query methods (`GetAllDiscovered*`)
5. Fix stub methods (`GetDrivers`, `AddEngineDefaultDrivers`)
6. Add null-check constructor pattern
7. Add `DataSourcesClasses` delegation to ConfigEditor

**AssemblyHandler** should keep its unique features:
1. `LoadNuggetFromNuGetAsync` (promote to `IAssemblyHandler`)
2. Real `GetAddinObjects` and `RearrangeAddin` 
3. Inline type cache (useful for performance)
4. Direct `GetAssemblyClassDefinition` (no external dependency)

See [AssemblyHandlerEnhancement.md](AssemblyHandlerEnhancement.md) for the full implementation plan.
