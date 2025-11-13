# NuGet Package Integration Guide

## Overview

BeepShell now supports **automatic NuGet package download** for driver installation. You can enter package names like `System.Data.SQLite` and the system will automatically download them from NuGet.org.

## Features

### 1. Package Name Detection

The system automatically detects whether your input is:
- **Package name** (e.g., `System.Data.SQLite`, `Npgsql`, `MongoDB.Driver`)
- **File path** (e.g., `C:\packages\driver.dll`)
- **Directory path** (e.g., `C:\packages\drivers\`)
- **URL** (e.g., `https://nuget.org/...`)

### 2. Automatic Download from NuGet.org

When you enter a package name, BeepShell will:
1. ✓ Download the package from NuGet.org using `dotnet` CLI
2. ✓ Extract the .nupkg file
3. ✓ Find the appropriate DLL for your framework
4. ✓ Install the driver
5. ✓ Update the installation tracker

### 3. Persistent Tracking

All installations are tracked in `installed_drivers.json`:
```json
{
  "SqlLite": {
    "DriverType": "SqlLite",
    "PackageName": "System.Data.SQLite",
    "Version": "1.0.118",
    "SourcePath": "C:\\Temp\\BeepShell\\packages\\...",
    "InstallDate": "2025-01-18T10:30:00",
    "IsInstalled": true
  }
}
```

## Usage Examples

### Example 1: Install SQLite Driver from NuGet

```bash
beep> drv install
```

Interactive prompts:
```
? Select driver to install: SqlLite
? Browse packages before installing? No
? Enter NuGet source, file path, or directory: System.Data.SQLite
```

Output:
```
⏳ Installing from NuGet source for SqlLite...
⏳ Analyzing source...
⏳ Downloading System.Data.SQLite from NuGet.org...
Downloading System.Data.SQLite from https://api.nuget.org/v3/index.json...
✓ Package downloaded successfully
✓ Downloaded to: C:\Temp\BeepShell\packages\system.data.sqlite\1.0.118\lib\net8.0\System.Data.SQLite.dll
⏳ Loading package: System.Data.SQLite.dll
✓ Driver package installed successfully for SqlLite
```

### Example 2: Install PostgreSQL Driver

```bash
beep> drv install
```

Enter package name:
```
Npgsql
```

The system will download `Npgsql` package from NuGet.org automatically.

### Example 3: Install with Specific Version

If your driver configuration has a version specified, it will use that version:

```csharp
driver.version = "5.0.0"
```

The downloader will fetch exactly version `5.0.0`.

### Example 4: Install from Local Directory (Still Works)

```bash
beep> drv install
? Enter NuGet source: C:\MyPackages\drivers
```

The system detects this is a directory and searches for DLLs there.

### Example 5: Install from Direct File Path (Still Works)

```bash
beep> drv install-from-file C:\packages\MongoDB.Driver.dll --type MongoDB
```

## Command Reference

### Install Driver with NuGet Integration

```bash
drv install
```

**Options:**
- Browse packages first
- Download from NuGet.org by package name
- Install from local directory
- Install from file path

### Install Missing Drivers (Batch)

```bash
drv install-missing
```

**Options:**
- Browse all missing drivers
- Auto-install from configured sources
- Specify custom directory

If drivers have NuGet sources configured, they'll be downloaded automatically.

### Check Installation Status

```bash
drv check
```

Shows:
- ✓ Installed drivers with source paths
- ✗ Missing drivers
- Installation dates and versions

## How It Works

### 1. NuGetPackageDownloader Class

Located in `BeepShell/Infrastructure/NuGetPackageDownloader.cs`

**Key Methods:**
- `DownloadPackageAsync(packageName, version, source)` - Downloads from NuGet feed
- `ExtractNuGetPackage(nupkgPath)` - Extracts .nupkg files
- `SearchPackageVersionsAsync(packageName)` - Searches available versions

**How Download Works:**
1. Creates temporary .csproj project
2. Runs: `dotnet add package {packageName} --version {version}`
3. Downloads package to `--package-directory`
4. Extracts .nupkg (which is a ZIP file)
5. Searches for DLL in `lib/{framework}/` folders
6. Returns path to DLL

### 2. Package Name Detection

The `IsPackageName()` method checks:
- ❌ Contains `\` or `/` → It's a file path
- ❌ Starts with `http` → It's a URL
- ❌ File or directory exists → It's a local path
- ✓ Simple name or dotted name → It's a package name

### 3. Driver Installation Flow

```
User enters source
    ↓
IsPackageName(source)?
    ↓ Yes
Download from NuGet.org
    ↓
Extract .nupkg
    ↓
Find DLL
    ↓
Load into AssemblyHandler
    ↓
Update driver config (NuggetMissing = false)
    ↓
Track in installed_drivers.json
    ↓
✓ Success
```

## Common Package Names

Here are package names for common database drivers:

| Database | Package Name |
|----------|--------------|
| SQLite | `System.Data.SQLite` |
| PostgreSQL | `Npgsql` |
| MongoDB | `MongoDB.Driver` |
| MySQL | `MySql.Data` |
| SQL Server | `Microsoft.Data.SqlClient` |
| Oracle | `Oracle.ManagedDataAccess.Core` |
| Cassandra | `CassandraCSharpDriver` |
| Redis | `StackExchange.Redis` |
| Elasticsearch | `NEST` |
| Neo4j | `Neo4j.Driver` |

## Configuration

### Driver Configuration (ConnectionDriversConfig)

```csharp
public class ConnectionDriversConfig
{
    public string DatasourceType { get; set; }  // "SqlLite"
    public string PackageName { get; set; }     // "System.Data.SQLite"
    public string version { get; set; }         // "1.0.118"
    public string NuggetSource { get; set; }    // Package name or path
    public bool NuggetMissing { get; set; }     // Auto-updated
}
```

### Download Directory

Default: `C:\Users\{username}\AppData\Local\Temp\BeepShell\packages\`

Each package is stored in: `{downloadDir}\{packageName}\{version}\`

### Tracker File

Location: `{executable_directory}\installed_drivers.json`

## Troubleshooting

### Package Not Found

**Error:**
```
✗ Failed to download package from NuGet.org
Try: dotnet add package {packageName}
```

**Solutions:**
1. Verify package name at https://www.nuget.org
2. Check internet connection
3. Try installing manually: `dotnet add package {packageName}`
4. Use exact package name (case-sensitive)

### DLL Not Found After Download

**Error:**
```
✗ Package not found at source: {packageName}
```

**Solutions:**
1. Check if package contains .NET compatible DLL
2. Verify target framework compatibility
3. Try extracting manually and using file path

### Permission Errors

**Error:**
```
✗ Error downloading package: Access denied
```

**Solutions:**
1. Run BeepShell with administrator privileges
2. Check temp directory permissions
3. Specify custom download directory

## Advanced Usage

### Custom NuGet Source

To use a custom NuGet feed, modify `NuGetPackageDownloader`:

```csharp
var downloader = new NuGetPackageDownloader(targetDir);
await downloader.DownloadPackageAsync(
    "MyPackage", 
    "1.0.0", 
    source: "https://my-nuget-feed.com/v3/index.json"
);
```

### Specific Version

Edit driver configuration to specify version:

```json
{
  "DatasourceType": "SqlLite",
  "version": "1.0.118"
}
```

### Offline Installation

For offline environments:
1. Download .nupkg files manually
2. Place in local directory
3. Use directory path instead of package name:
   ```
   ? Enter NuGet source: C:\offline-packages\
   ```

## Integration with NuGetShellCommands

BeepShell has two NuGet management systems:

1. **NuGetShellCommands** - General NuGet package management
   - `nuget install`, `nuget update`, `nuget remove`
   - Manages packages in project

2. **DriverShellCommands with NuGet Integration** - Driver-specific
   - `drv install` with automatic NuGet download
   - Specialized for database driver packages
   - Integrated with driver configuration and tracking

Both use the underlying `NuGetPackageDownloader` for downloads.

## Benefits

✓ **No manual download** - Just enter package name
✓ **Automatic extraction** - DLL is found and loaded automatically
✓ **Version control** - Specify versions in configuration
✓ **Persistent tracking** - Know what's installed and from where
✓ **Backward compatible** - File paths and directories still work
✓ **Interactive workflow** - Browse, download, install all in one flow

## Next Steps

1. Try installing a driver: `drv install`
2. Enter a package name like `System.Data.SQLite`
3. Check installation: `drv check`
4. View tracker: Open `installed_drivers.json`

---

**Created:** 2025-01-18  
**Version:** 1.0.0  
**Author:** BeepDM Team
