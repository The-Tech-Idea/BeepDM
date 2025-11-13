# Runtime Compatibility Guide - BeepShell Driver Installation

## Overview

When you install a driver in BeepShell, the driver DLL is **loaded into BeepShell's runtime process** at runtime. This means the driver runs in the same process as BeepShell itself.

## Key Concepts

### 1. **BeepShell Runtime**

BeepShell is compiled for:
- **Target Framework:** `net8.0` (.NET 8.0)
- **Runtime:** .NET 8.0 Runtime
- **Platform:** Windows (net8.0-windows compatible)

### 2. **Driver Installation = Runtime Loading**

When you install a driver:

```bash
drv install
? Select driver: SqlLite
? Enter source: System.Data.SQLite
```

**What Happens:**
1. ✓ Package downloaded from NuGet.org
2. ✓ Package extracted to find compatible DLL
3. ✓ DLL loaded into BeepShell's AppDomain **at runtime**
4. ✓ Assembly scanned for types and registered
5. ✓ Driver becomes available for use **immediately**

**The driver DLL now runs inside BeepShell's process with .NET 8.0 runtime.**

## Runtime Compatibility

### ✅ Compatible Driver Frameworks

These frameworks can be loaded into BeepShell (net8.0) at runtime:

| Framework | Compatibility | Notes |
|-----------|---------------|-------|
| **net8.0** | ✅ Perfect | Exact match - best compatibility |
| **net8.0-windows** | ✅ Perfect | Windows-specific net8.0 |
| **net7.0** | ✅ Excellent | Forward compatible with net8.0 |
| **net6.0** | ✅ Excellent | Forward compatible with net8.0 |
| **netstandard2.1** | ✅ Good | Compatible with .NET Core 3.0+ |
| **netstandard2.0** | ✅ Good | Compatible with .NET Core 2.0+ |

### ⚠️ Potentially Incompatible Frameworks

| Framework | Compatibility | Issues |
|-----------|---------------|--------|
| **net48** | ⚠️ Limited | .NET Framework - may have API mismatches |
| **net472** | ⚠️ Limited | .NET Framework - may have API mismatches |
| **net471** | ⚠️ Limited | .NET Framework - may have API mismatches |
| **net47** | ⚠️ Limited | .NET Framework - may have API mismatches |
| **net462-46** | ⚠️ Limited | Older .NET Framework versions |

### Framework Selection

BeepShell's NuGetPackageDownloader automatically selects the **most compatible framework** from downloaded packages:

```csharp
var preferredOrder = new[] 
{ 
    "net8.0",           // Exact match
    "net8.0-windows",   // Windows-specific net8.0
    "net7.0",           // Compatible
    "net6.0",           // Compatible
    "netstandard2.1",   // Compatible
    "netstandard2.0",   // Compatible
    "net48",            // May work but not ideal
    // ... older frameworks
};
```

When extracting a package, it displays:
```
→ Selected framework: net8.0 (BeepShell runtime: net8.0)
```

## How Runtime Loading Works

### Step 1: Download and Extract

```
NuGetPackageDownloader downloads package
    ↓
Extracts .nupkg (ZIP file)
    ↓
Scans lib/ folder for framework folders
    ↓
Selects most compatible framework
    ↓
Returns DLL path
```

### Step 2: Compatibility Verification

Before loading, BeepShell verifies compatibility:

```csharp
if (!VerifyRuntimeCompatibility(packagePath))
{
    AnsiConsole.MarkupLine($"[yellow]⚠[/] Warning: Driver may have runtime compatibility issues");
    var proceed = AnsiConsole.Confirm("Do you want to try loading it anyway?");
}
```

**Verification checks:**
- ✓ Framework folder name (net8.0, net6.0, etc.)
- ✓ Assembly metadata (can we load AssemblyName?)
- ✓ Platform compatibility (x86/x64/AnyCPU)

### Step 3: Runtime Loading

```csharp
var loadSuccess = _editor.assemblyHandler.LoadNugget(packagePath);
```

**What happens internally:**

1. **AssemblyHandler.LoadNugget()** calls NuggetManager
2. **NuggetManager** loads assembly into current AppDomain
3. Assembly is scanned for types
4. Types are registered in DMEEditor
5. Driver becomes **immediately available**

### Step 4: Verification

```
✓ Driver package installed and loaded at runtime
Note: Driver is loaded into BeepShell's runtime (net8.0)
```

The driver is now in memory and can be used to create connections.

## Runtime Considerations

### 1. **Memory Space**

- Drivers run in **the same process** as BeepShell
- Share the same memory space
- Driver crashes can crash BeepShell (no process isolation)

### 2. **Dependencies**

If a driver depends on other libraries:

```
System.Data.SQLite.dll
  ├── System.Data.SQLite.EF6.dll (dependency)
  └── SQLite.Interop.dll (native dependency)
```

**What BeepShell does:**
- Loads the main DLL
- .NET runtime resolves dependencies automatically
- If dependencies are in the same folder, they load correctly
- If dependencies are missing → **Runtime errors**

### 3. **Native Dependencies**

Some drivers have **native (unmanaged) DLLs**:

Example: `SQLite.Interop.dll` (C++ native code)

**Handling:**
- Native DLLs must be in the same directory as managed DLL
- Or in a platform-specific subfolder: `x64/`, `x86/`
- BeepShell's assembly loader can handle this

### 4. **Framework Compatibility**

**Scenario:** Package only has .NET Framework version

```
lib/
  └── net48/
      └── MyDriver.dll
```

**What happens:**
- BeepShell selects `net48` (only option)
- Shows warning: ⚠️ Using framework: net48 (may have runtime compatibility issues)
- Attempts to load anyway
- **May work** if driver only uses compatible APIs
- **May fail** if driver uses .NET Framework-specific features

### 5. **Multi-Targeting**

Best-case scenario - package supports multiple frameworks:

```
lib/
  ├── net8.0/
  │   └── MyDriver.dll
  ├── net6.0/
  │   └── MyDriver.dll
  ├── netstandard2.0/
  │   └── MyDriver.dll
  └── net48/
      └── MyDriver.dll
```

BeepShell automatically selects `net8.0` version → perfect compatibility!

## Common Scenarios

### Scenario 1: Modern Package (Best Case)

**Package:** `Npgsql` (PostgreSQL driver)

```
lib/
  ├── net8.0/
  ├── net7.0/
  └── netstandard2.0/
```

**Result:**
```
→ Selected framework: net8.0 (BeepShell runtime: net8.0)
✓ Driver package installed and loaded at runtime
```

**Compatibility:** ✅ Perfect - runs natively on .NET 8.0

### Scenario 2: .NET Standard Package (Good)

**Package:** `MongoDB.Driver`

```
lib/
  └── netstandard2.0/
      └── MongoDB.Driver.dll
```

**Result:**
```
→ Selected framework: netstandard2.0 (BeepShell runtime: net8.0)
✓ Driver package installed and loaded at runtime
```

**Compatibility:** ✅ Good - .NET Standard 2.0 is fully compatible with .NET 8.0

### Scenario 3: .NET Framework Package (Risky)

**Package:** `Oracle.ManagedDataAccess` (old version)

```
lib/
  └── net462/
      └── Oracle.ManagedDataAccess.dll
```

**Result:**
```
⚠ Using framework: net462 (may have runtime compatibility issues with BeepShell net8.0)
Do you want to try loading it anyway? [y/n]
```

**Compatibility:** ⚠️ Risky
- May work if driver uses compatible APIs
- May fail with missing types or methods
- Better to find a .NET Core/.NET 5+ version

### Scenario 4: Native Dependencies

**Package:** `System.Data.SQLite`

```
lib/
  └── net8.0/
      ├── System.Data.SQLite.dll      (managed)
      └── x64/
          └── SQLite.Interop.dll      (native C++)
```

**Result:**
```
→ Selected framework: net8.0 (BeepShell runtime: net8.0)
✓ Driver package installed and loaded at runtime
✓ Native dependencies loaded from x64/ subdirectory
```

**Compatibility:** ✅ Excellent - both managed and native components work

## Troubleshooting Runtime Issues

### Issue 1: FileNotFoundException

**Error:**
```
Could not load file or assembly 'SomeDependency, Version=1.0.0.0'
```

**Cause:** Missing dependency DLL

**Solution:**
1. Download the dependency package separately
2. Place DLLs in the same directory as driver
3. Or install the dependency using `nuget install`

### Issue 2: BadImageFormatException

**Error:**
```
BadImageFormatException: An attempt was made to load a program with an incorrect format
```

**Cause:** Platform mismatch (x86 vs x64)

**Solution:**
- Ensure driver is `AnyCPU` or matches BeepShell's platform
- Download the correct architecture version
- Check native dependencies (x64/ vs x86/)

### Issue 3: MissingMethodException

**Error:**
```
MissingMethodException: Method not found: 'Void SomeClass.SomeMethod()'
```

**Cause:** API version mismatch (.NET Framework vs .NET Core)

**Solution:**
- Find a newer version of the driver that targets .NET Core/.NET 5+
- Check NuGet for `netstandard2.0` or `net6.0+` versions
- Contact driver vendor for .NET Core support

### Issue 4: Type Initialization Failed

**Error:**
```
TypeInitializationException: The type initializer for 'DriverClass' threw an exception
```

**Cause:** Driver initialization fails in .NET Core runtime

**Solution:**
- Check driver documentation for .NET Core compatibility
- Look for configuration requirements
- Enable runtime logging to see initialization errors

## Best Practices

### 1. **Always Use Latest Packages**

Modern packages target .NET 8.0 or .NET Standard 2.0+:

```bash
# Good - modern package
drv install
? Enter source: Npgsql

# Better - specify version
drv install
? Enter source: Npgsql
(driver.version = "8.0.0" in config)
```

### 2. **Check Package Compatibility Before Installing**

Visit NuGet.org and check supported frameworks:

```
https://www.nuget.org/packages/System.Data.SQLite

Frameworks:
  ✓ net8.0
  ✓ net6.0
  ✓ netstandard2.1
```

### 3. **Test After Installation**

```bash
# Install driver
drv install

# Test connectivity
conn create MyConnection SqlLite

# Try a simple query
query run "SELECT 1"
```

### 4. **Monitor Runtime Logs**

Enable verbose logging to see assembly loading:

```bash
# Check logs for loading issues
AssemblyHandler: LoadNugget: Successfully loaded from C:\Temp\...
```

### 5. **Update Drivers Regularly**

```bash
# Check for updates
drv list

# Update specific driver
drv update --name SqlLite
```

## Advanced: How BeepShell Loads Assemblies

### AssemblyHandler Flow

```csharp
// 1. LoadNugget is called
public bool LoadNugget(string path)
{
    // 2. NuggetManager loads assembly
    var result = _nuggetManager.LoadNugget(path);
    
    if (result)
    {
        // 3. Get loaded assemblies from nugget
        var nuggetAssemblies = _nuggetManager.GetNuggetAssemblies(nuggetName);
        
        foreach (var assembly in nuggetAssemblies)
        {
            // 4. Add to LoadedAssemblies collection
            LoadedAssemblies.Add(assembly);
            
            // 5. Scan assembly for types
            ScanAssembly(assembly);
            // This finds IDbDriver implementations, data providers, etc.
        }
    }
}
```

### Runtime Isolation

BeepShell uses **AssemblyLoadContext** for some isolation:

- Drivers can be unloaded (if context supports it)
- Multiple versions of same driver can coexist
- But all run in same process

### Shared Dependencies

If two drivers need different versions of the same dependency:

```
Driver A → Newtonsoft.Json 12.0.0
Driver B → Newtonsoft.Json 13.0.0
```

**What happens:**
- .NET runtime loads the first version encountered
- Second driver may fail if API changed
- **Solution:** Use assembly binding redirects or ensure compatible versions

## Summary

**Key Takeaways:**

1. ✅ **Drivers load at runtime** into BeepShell's .NET 8.0 process
2. ✅ **BeepShell auto-selects** the most compatible framework version
3. ✅ **Best compatibility** with net8.0, net7.0, net6.0, netstandard2.x
4. ⚠️ **.NET Framework drivers** (net4x) may have compatibility issues
5. ✅ **Modern NuGet packages** usually support multiple frameworks
6. ✅ **Runtime verification** warns about potential issues
7. ✅ **Immediate availability** - driver works right after installation
8. ⚠️ **Same process** - driver issues can affect BeepShell

**Recommendations:**

- Use drivers that target .NET 6.0+ or .NET Standard 2.0+
- Check NuGet package frameworks before installing
- Test driver connectivity after installation
- Keep drivers updated to latest versions
- Monitor for runtime exceptions during driver operations

---

**Document Version:** 1.0.0  
**Last Updated:** 2025-11-13  
**BeepShell Runtime:** net8.0
