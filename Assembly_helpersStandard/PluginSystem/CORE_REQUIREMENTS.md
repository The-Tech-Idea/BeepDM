# Plugin System - Core Requirements & Implementation

## 🎯 Core Purpose

The Plugin System has **4 critical capabilities** that must be preserved and enhanced:

### 1. Load/Unload NuGet Packages from Multiple Sources
- ✅ Load from any NuGet feed URL
- ✅ Load from local file system
- ✅ Check multiple package sources (like Visual Studio)
- ✅ Support custom/private feeds

### 2. Dependency Resolution with Correct Framework Targeting
- ✅ Automatically resolve all dependencies
- ✅ Select correct framework version (.NET 6, .NET 8, etc.)
- ✅ Handle transitive dependencies
- ✅ Skip system/framework packages

### 3. True Unload and Reload
- ✅ Collectible AssemblyLoadContext for true unloading
- ✅ Memory reclamation via GC
- ✅ Hot reload without app restart
- ✅ Version updates

### 4. Smart Reference Resolution
- ✅ Reuse already-loaded assemblies
- ✅ Don't download duplicates
- ✅ Resolve from loaded plugins first
- ⚠️ Challenge: Makes unloading harder (needs careful design)

---

## 📊 Current Implementation Status

### ✅ What's Working Perfectly

#### 1. Multi-Source NuGet Loading

**Current Implementation:**
```csharp
// NuggetPackageDownloader.cs handles multiple sources
var sources = new[] { 
    "https://api.nuget.org/v3/index.json",
    "https://mycompany.com/nuget/v3/index.json",
    "C:\\LocalPackages"
};

var assemblies = await assemblyHandler.LoadNuggetFromNuGetAsync(
    "Oracle.ManagedDataAccess.Core", 
    "23.4.0", 
    sources,
    useSingleSharedContext: true
);
```

**What's Good:**
- ✅ Tries each source in order
- ✅ Falls back to next source on failure
- ✅ Caches downloaded packages locally
- ✅ Supports both HTTP and file system sources

**User-Friendly Enhancement Needed:**
```bash
# CLI command to manage sources
beep plugin source add "https://mycompany.com/nuget/v3/index.json"
beep plugin source list
beep plugin source test "https://mycompany.com/nuget/v3/index.json"

# Interactive installation with source selection
beep plugin install MyPackage --source (shows interactive menu)
```

---

#### 2. Dependency Resolution & Framework Targeting

**Current Implementation:**
```csharp
// NuggetPackageDownloader.cs
// 1. Downloads package
// 2. Extracts .nupkg
// 3. Parses .nuspec for dependencies
// 4. Finds framework-specific DLLs (lib/net6.0, lib/net8.0, etc.)
// 5. Recursively downloads dependencies
// 6. Skips system packages (System.*, Microsoft.NETCore.*)

var packagePath = await downloader.DownloadAndExtractPackageAsync(
    packageId, 
    version, 
    sources
);

// Automatically selects best framework match
var frameworkDlls = downloader.GetFrameworkSpecificDlls(packagePath);
```

**What's Good:**
- ✅ Automatic framework selection
- ✅ Recursive dependency resolution
- ✅ Handles complex dependency trees
- ✅ Optimized for .NET 6/8

**User-Friendly Enhancement Needed:**
```bash
# Show dependency tree BEFORE installing
beep plugin install MyPackage --dry-run --show-deps

# Output with interactive visualization:
MyPackage v1.0.0 (net8.0)
├── Newtonsoft.Json v13.0.3 (net6.0) [✓ Compatible]
├── Dapper v2.0.123 (netstandard2.0) [✓ Compatible]
│   └── System.Data.Common v6.0.0 [⊙ Framework Package - Skip]
└── AutoMapper v12.0.1 (net8.0) [✓ Compatible]
    └── AutoMapper.Extensions.Microsoft.DependencyInjection v12.0.1
```

---

#### 3. Collectible Contexts for True Unloading

**Current Implementation:**
```csharp
// SharedContextManager.cs - Creates collectible contexts
public class SharedContextLoadContext : AssemblyLoadContext
{
    public SharedContextLoadContext(string contextId) 
        : base(contextId, isCollectible: true) // ← Key!
    {
    }
}

// Unloading triggers GC
public void UnloadNugget(string nuggetId)
{
    if (_sharedContexts.TryRemove(nuggetId, out var context))
    {
        context.Unload();
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
```

**What's Good:**
- ✅ True memory reclamation
- ✅ No memory leaks
- ✅ Can reload updated versions
- ✅ Proper cleanup

**User-Friendly Enhancement Needed:**
```bash
# Show memory impact
beep plugin stats MyPlugin
# Output:
# Memory Used: 45.2 MB
# Assemblies Loaded: 12
# Types Cached: 245

# Confirm before unload
beep plugin unload MyPlugin
# Are you sure? This will release 45.2 MB memory. (yes/no)

# Show what would be unloaded
beep plugin unload MyPlugin --dry-run
# Would unload:
# - MyPlugin v1.0.0
# - Dependent assemblies: 12
# - Memory to be freed: ~45.2 MB
```

---

#### 4. Smart Reference Resolution

**Current Implementation:**
```csharp
// SharedContextManager.cs - Reuses loaded assemblies
protected override Assembly Load(AssemblyName assemblyName)
{
    // 1. CHECK: Already loaded in ANY shared context?
    var sharedAssembly = _manager.GetSharedAssemblies()
        .FirstOrDefault(a => AssemblyNamesMatch(a.GetName(), assemblyName));
    
    if (sharedAssembly != null)
    {
        // ✅ REUSE instead of downloading again!
        return sharedAssembly;
    }
    
    // 2. Try resolver for local dependencies
    string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
    if (assemblyPath != null)
    {
        return LoadFromAssemblyPath(assemblyPath);
    }
    
    // 3. System assemblies use default context
    if (IsSystemAssembly(assemblyName))
        return null;
    
    // 4. Final fallback
    return Default.LoadFromAssemblyName(assemblyName);
}
```

**What's Good:**
- ✅ No duplicate downloads
- ✅ Type identity preserved
- ✅ Cross-plugin type sharing
- ✅ Efficient memory usage

**The Challenge:**
```csharp
// Problem: Shared assemblies prevent unloading!

Plugin A loads: Newtonsoft.Json v13.0.3
Plugin B also needs: Newtonsoft.Json v13.0.3
  ↓ SHARES the same assembly instance

When unloading Plugin A:
  ❌ Can't unload Newtonsoft.Json (Plugin B still uses it!)
  
When unloading Plugin B:
  ✅ NOW can unload Newtonsoft.Json
```

**Solution - Reference Counting:**
```csharp
public class SharedAssemblyTracker
{
    // Track who's using what
    private ConcurrentDictionary<Assembly, HashSet<string>> _assemblyUsers = new();
    
    public Assembly LoadShared(Assembly assembly, string pluginId)
    {
        if (!_assemblyUsers.ContainsKey(assembly))
        {
            _assemblyUsers[assembly] = new HashSet<string>();
        }
        _assemblyUsers[assembly].Add(pluginId);
        return assembly;
    }
    
    public bool CanUnload(Assembly assembly, string pluginId)
    {
        if (_assemblyUsers.TryGetValue(assembly, out var users))
        {
            users.Remove(pluginId);
            return users.Count == 0; // Can unload if no users left
        }
        return true;
    }
}
```

---

## 🎯 Enhanced Architecture (Preserves Core Capabilities)

### Design Principle
**"Make Complex Things Simple, Not Simple Things Complex"**

Keep the powerful core capabilities but wrap them in user-friendly interfaces.

### Proposed Structure

```
┌─────────────────────────────────────────────────────────┐
│                  User-Friendly CLI                      │
│  (Interactive commands, progress bars, wizards)         │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│            UnifiedPluginManager                         │
│  (Simple API, hides complexity)                         │
│                                                          │
│  • InstallAsync(package, sources)                       │
│  • UnloadAsync(pluginId, smart: true)                  │
│  • ReloadAsync(pluginId)                                │
│  • GetDependencies(pluginId, includeShared: true)      │
└────────────────────┬────────────────────────────────────┘
                     │
    ┌────────────────┼────────────────┐
    │                │                │
┌───▼────┐   ┌──────▼─────┐   ┌─────▼──────┐
│Loading │   │ Resolution │   │ Lifecycle  │
│        │   │            │   │            │
│• Multi │   │• Framework │   │• Collectible│
│  Source│   │  Selection │   │  Contexts  │
│• NuGet │   │• Dep Tree  │   │• Reference │
│  Cache │   │• Reuse     │   │  Counting  │
└────────┘   └────────────┘   └────────────┘
```

---

## 🚀 Core Capability Enhancements

### Enhancement 1: Smart Unloading with Reference Tracking

**New Feature: Unload Analysis**

```csharp
public class UnloadAnalysis
{
    public string PluginId { get; set; }
    public bool CanUnloadImmediately { get; set; }
    public List<string> SharedAssemblies { get; set; }
    public List<string> DependentPlugins { get; set; }
    public long EstimatedMemoryFreed { get; set; }
    
    public string GetUserFriendlyMessage()
    {
        if (CanUnloadImmediately)
        {
            return $"Can unload immediately. Will free ~{EstimatedMemoryFreed / 1024 / 1024}MB";
        }
        else
        {
            return $"Cannot unload yet. Still used by: {string.Join(", ", DependentPlugins)}";
        }
    }
}

// Usage
var analysis = await pluginManager.AnalyzeUnloadAsync("MyPlugin");
Console.WriteLine(analysis.GetUserFriendlyMessage());
```

**CLI Integration:**
```bash
beep plugin unload MyPlugin

# Smart analysis before unload:
┌─────────────────────────────────────────────────┐
│ Unload Analysis: MyPlugin                       │
├─────────────────────────────────────────────────┤
│ Status: ⚠️  Cannot unload immediately           │
│                                                  │
│ Shared Assemblies (3):                          │
│   • Newtonsoft.Json v13.0.3 (used by Plugin B)  │
│   • Dapper v2.0.123 (used by Plugin C)          │
│   • AutoMapper v12.0.1 (unique to MyPlugin) ✓   │
│                                                  │
│ Options:                                         │
│   1. Force unload (may break Plugin B, C)       │
│   2. Unload Plugin B and C first                │
│   3. Cancel                                      │
└─────────────────────────────────────────────────┘
```

---

### Enhancement 2: Source Management

**New Feature: Package Source Registry**

```csharp
public class PackageSourceManager
{
    public void AddSource(string name, string url, int priority = 0);
    public void RemoveSource(string name);
    public IEnumerable<PackageSource> GetSources();
    public void SetSourcePriority(string name, int priority);
    public Task<bool> TestSourceAsync(string name);
}

public class PackageSource
{
    public string Name { get; set; }
    public string Url { get; set; }
    public int Priority { get; set; } // Higher = checked first
    public bool IsAvailable { get; set; }
    public DateTime LastChecked { get; set; }
}
```

**CLI Integration:**
```bash
# Manage sources
beep plugin source add "MyCompany" "https://nuget.mycompany.com/v3/index.json"
beep plugin source list

# Output:
┌──────────────────────────────────────────────────────┐
│ Package Sources                                       │
├─────────┬────────────────────────────┬──────┬────────┤
│ Name    │ URL                        │ Pri  │ Status │
├─────────┼────────────────────────────┼──────┼────────┤
│ NuGet   │ nuget.org/v3/index.json   │ 100  │ ✓ OK   │
│ Company │ nuget.mycompany.com/v3/... │ 90   │ ✓ OK   │
│ Local   │ C:\LocalPackages           │ 80   │ ✓ OK   │
│ VS Cache│ %USERPROFILE%\.nuget\...   │ 70   │ ✓ OK   │
└─────────┴────────────────────────────┴──────┴────────┘

# Test source connectivity
beep plugin source test "Company"
Testing: https://nuget.mycompany.com/v3/index.json
✓ Connected (285ms)
✓ Service index available
✓ Search service available
```

---

### Enhancement 3: Dependency Visualization

**New Feature: Dependency Inspector**

```csharp
public class DependencyInspector
{
    public DependencyTree GetDependencyTree(string pluginId);
    public List<Assembly> GetSharedAssemblies(string pluginId);
    public List<string> GetPluginsThatDependOn(string assemblyName);
    public ConflictAnalysis DetectVersionConflicts();
}
```

**CLI Integration:**
```bash
beep plugin dependencies MyPlugin --tree

MyPlugin v1.0.0 (net8.0)
├─ Newtonsoft.Json v13.0.3 (net6.0) [SHARED with Plugin B, C]
│  └─ (no dependencies)
├─ Dapper v2.0.123 (netstandard2.0) [SHARED with Plugin C]
│  └─ System.Data.Common v6.0.0 [Framework Package]
├─ AutoMapper v12.0.1 (net8.0) [UNIQUE]
│  └─ AutoMapper.Extensions.Microsoft.DependencyInjection v12.0.1
│     └─ Microsoft.Extensions.DependencyInjection.Abstractions v7.0.0
└─ Serilog v3.0.1 (net6.0) [SHARED with Plugin A, D]

Legend:
  [SHARED] = Used by multiple plugins (cannot unload independently)
  [UNIQUE] = Only this plugin uses it (safe to unload)
  [Framework Package] = System assembly (always available)

# Interactive mode
beep plugin dependencies MyPlugin --interactive
# Shows tree with ability to:
# - Click to see which plugins share an assembly
# - Identify version conflicts
# - Plan safe unload order
```

---

### Enhancement 4: Framework Targeting Visibility

**New Feature: Framework Compatibility Checker**

```csharp
public class FrameworkCompatibilityChecker
{
    public CompatibilityReport CheckCompatibility(string packageId, string version);
    public List<string> GetAvailableFrameworks(string packagePath);
    public string SelectBestFramework(List<string> available, string targetFramework);
}

public class CompatibilityReport
{
    public string PackageId { get; set; }
    public string Version { get; set; }
    public List<FrameworkSupport> SupportedFrameworks { get; set; }
    public FrameworkSupport RecommendedFramework { get; set; }
    public List<string> Warnings { get; set; }
}
```

**CLI Integration:**
```bash
beep plugin check Oracle.ManagedDataAccess.Core

┌────────────────────────────────────────────────────────┐
│ Compatibility Check: Oracle.ManagedDataAccess.Core     │
├────────────────────────────────────────────────────────┤
│ Package Version: 23.4.0                                │
│ Current Runtime: .NET 8.0                              │
│                                                         │
│ Available Frameworks:                                  │
│   ✓ net8.0      (Perfect match - Recommended)         │
│   ✓ net6.0      (Compatible, but not optimal)         │
│   ✓ netstandard2.1 (Compatible, minimal features)     │
│                                                         │
│ Dependencies (11):                                     │
│   All compatible with .NET 8.0 ✓                      │
│                                                         │
│ Download Size: 4.2 MB                                  │
│ Extracted Size: 12.8 MB                                │
└────────────────────────────────────────────────────────┘
```

---

## 📋 Implementation Priority

### Phase 1: Core Enhancements (Week 1-2)
1. ✅ Add Reference Counting to SharedContextManager
2. ✅ Implement UnloadAnalysis
3. ✅ Add PackageSourceManager
4. ✅ Create DependencyInspector

### Phase 2: CLI Integration (Week 3-4)
1. ✅ Create PluginCommands.cs
2. ✅ Add `plugin dependencies` command
3. ✅ Add `plugin source` commands
4. ✅ Add smart `plugin unload` with analysis

### Phase 3: Advanced Features (Week 5-6)
1. ✅ Dependency tree visualization
2. ✅ Framework compatibility checker
3. ✅ Interactive dependency browser
4. ✅ Conflict detection and resolution

---

## 🎯 Key Design Decisions

### Decision 1: Reference Counting vs. Separate Contexts

**Option A: Reference Counting (RECOMMENDED)**
```
✅ Pros:
- Efficient memory usage
- Type identity preserved
- Easy cross-plugin communication
- Automatic deduplication

⚠️ Cons:
- Cannot unload until all users done
- Need to track usage
- More complex unload logic
```

**Option B: Separate Contexts Per Plugin**
```
✅ Pros:
- Can always unload immediately
- True isolation
- Simpler lifecycle

❌ Cons:
- Memory duplication (multiple copies of same DLL)
- Type identity issues (Plugin A's Type ≠ Plugin B's Type)
- No cross-plugin type sharing
- Larger memory footprint
```

**Recommendation:** Keep reference counting, add unload analysis

---

### Decision 2: Source Priority System

**Implement priority-based source checking:**

```csharp
// Sources checked in priority order
[1] Local file system (priority: 100)
[2] Company private feed (priority: 90)
[3] NuGet.org (priority: 80)
[4] VS package cache (priority: 70)

// Benefits:
✅ Faster lookups (check local first)
✅ Offline capability (local cache)
✅ Corporate packages prioritized
✅ Fallback to public packages
```

---

### Decision 3: Smart Unload Modes

**Implement multiple unload strategies:**

```csharp
public enum UnloadMode
{
    Safe,        // Only unload if no dependencies
    Cascade,     // Unload with dependent plugins
    Force,       // Unload anyway (may break things)
    Scheduled    // Unload when safe (monitor for opportunity)
}

// Usage
await pluginManager.UnloadAsync("MyPlugin", UnloadMode.Safe);
```

---

## ✅ Success Criteria

The enhancement is successful when:

1. **Loading**
   - [ ] Can load from any NuGet source (HTTP, file system)
   - [ ] Automatic dependency resolution with correct framework
   - [ ] Shows progress during download/extraction
   - [ ] Visual dependency tree before installation

2. **Unloading**
   - [ ] Smart unload analysis (shows what will be affected)
   - [ ] Reference counting prevents premature unload
   - [ ] True memory reclamation (verified with profiler)
   - [ ] Options: Safe / Cascade / Force / Scheduled

3. **Reference Resolution**
   - [ ] Reuses already-loaded assemblies
   - [ ] No duplicate downloads
   - [ ] Type identity preserved across plugins
   - [ ] Can track which plugins use what

4. **User Experience**
   - [ ] < 30 seconds to install a plugin
   - [ ] Clear visual feedback during operations
   - [ ] Helpful error messages
   - [ ] Easy to understand dependency relationships

---

## 🎓 Next Steps

1. **This Week:**
   - Implement reference counting in SharedContextManager
   - Create UnloadAnalysis class
   - Add basic CLI commands (list, info, dependencies)

2. **Next Week:**
   - Add PackageSourceManager
   - Implement smart unload
   - Create dependency visualization

3. **Week 3:**
   - Framework compatibility checker
   - Interactive features
   - Comprehensive testing

**Start Here:** Add reference counting to `SharedContextManager.cs` to enable smart unloading while preserving shared assembly benefits.

