# NuGet Driver Installation - Complete Workflow

## Overview

BeepShell now supports fully interactive driver installation from NuGet.org with automatic framework compatibility checking and persistent storage in the `ConnectionDrivers` folder.

## Installation Flow

### 1. User Command
```
drv install
```

### 2. Interactive Prompts
- **Select Driver Type**: Choose from available driver types (SqlLite, SqlServer, Oracle, etc.)
- **Enter Package Name**: Provide the NuGet package name (e.g., `System.Data.SQLite`)
- **Optional Version**: Leave blank for latest version

### 3. Download Phase
- Downloads package to temporary location: `C:\Users\{user}\AppData\Local\Temp\BeepShell\packages\`
- Uses `dotnet add package` command via NuGetPackageDownloader
- Displays download progress

### 4. Framework Selection
- Extracts .nupkg file (ZIP format)
- Scans `lib/` folder for compatible frameworks
- **Priority Order**:
  1. net8.0 (matches BeepShell runtime)
  2. net8.0-windows
  3. net7.0
  4. net6.0
  5. netstandard2.1
  6. netstandard2.0
  7. net48, net472, net471, etc. (warns user about compatibility)
- Displays selected framework to user

### 5. Copy to ConnectionDrivers
- Creates driver-specific folder: `{ExePath}\ConnectionDrivers\{DatasourceType}\`
- Copies main DLL and all dependencies
- Copies native architecture folders (x64, x86, arm64) if present
- **Example Structure**:
  ```
  BeepShell.exe
  ConnectionDrivers/
    SqlLite/
      System.Data.SQLite.dll
      System.Data.SQLite.xml
      x64/
        SQLite.Interop.dll
      x86/
        SQLite.Interop.dll
  ```

### 6. Load & Track
- Loads driver from ConnectionDrivers location
- Updates `installed_drivers.json` with permanent path
- Driver persists across BeepShell restarts

## Runtime Compatibility

### Compatible Frameworks
✅ **net8.0** - Perfect match (BeepShell runtime)  
✅ **net7.0** - Compatible  
✅ **net6.0** - Compatible  
✅ **netstandard2.1** - Compatible  
✅ **netstandard2.0** - Compatible  

### Incompatible Frameworks
❌ **net48, net472, net471** - .NET Framework (warns user before loading)

### Verification Methods
1. **Directory Name Check**: Examines framework folder names
2. **DLL Compatibility**: Uses `AssemblyName.GetAssemblyName()` to verify DLL can load
3. **User Warning**: Displays warning if incompatible framework detected

## File Locations

### Temporary Files
- **Download**: `C:\Temp\BeepShell\packages\{packageName}\{version}\`
- **Extraction**: Same as download location
- **Cleanup**: Can be deleted after installation

### Permanent Storage
- **Driver DLLs**: `{ExePath}\ConnectionDrivers\{DatasourceType}\`
- **Tracker**: `{ExePath}\installed_drivers.json`
- **Example ExePath**: `C:\Users\{user}\AppData\Local\BeepShell\`

### Folder Structure (ConfigEditor)
ConnectionDrivers folder is created by ConfigEditor.InitConfig() alongside:
- Config/
- DataSources/
- LoadingExtensions/
- Addin/
- Scripts/
- Reports/
- etc.

## Architecture Components

### NuGetPackageDownloader
- **Location**: `BeepShell/Infrastructure/NuGetPackageDownloader.cs`
- **Purpose**: Download and extract NuGet packages
- **Methods**:
  - `DownloadPackageAsync()` - Downloads from nuget.org
  - `ExtractNuGetPackage()` - Extracts and selects framework
  - `GetInstalledVersion()` - Checks if package already exists

### DriverShellCommands
- **Location**: `BeepShell/Commands/DriverShellCommands.cs`
- **Purpose**: Orchestrate installation workflow
- **Methods**:
  - `InstallDriverFromNuGetSource()` - Main installation method
  - `VerifyRuntimeCompatibility()` - Check framework compatibility
  - `VerifyDllCompatibility()` - Validate DLL can load

### ConfigEditor
- **Location**: `DataManagementEngineStandard/ConfigUtil/ConfigEditor.cs`
- **Purpose**: Manage BeepShell folder structure
- **Property**: `Config.ConnectionDriversPath` - Points to ConnectionDrivers folder

## Example Installation

### System.Data.SQLite

**Command**:
```
drv install
```

**Interactive Selections**:
1. Driver Type: `SqlLite`
2. Package Name: `System.Data.SQLite`
3. Version: _(leave blank for latest)_

**Process**:
```
→ Downloading package from NuGet.org...
→ Package downloaded: System.Data.SQLite.1.0.118.0.nupkg
→ Extracting package...
→ Selected framework: net8.0 (BeepShell runtime: net8.0)
→ Copying driver to ConnectionDrivers folder...
✓ Driver copied to: ConnectionDrivers\SqlLite
  Main DLL: System.Data.SQLite.dll
→ Loading driver...
✓ Driver installed successfully!
```

**Result**:
- Files in `ConnectionDrivers\SqlLite\`:
  - System.Data.SQLite.dll
  - System.Data.SQLite.xml
  - x64\SQLite.Interop.dll
  - x86\SQLite.Interop.dll
- Tracked in `installed_drivers.json`
- Auto-loads on BeepShell restart

## Removal

### Command
```
drv remove --name SqlLite
```

### Process
1. Deletes all files from `ConnectionDrivers\SqlLite\`
2. Removes folder
3. Updates `installed_drivers.json` (IsInstalled = false)
4. Driver no longer loads on restart

## Benefits

### For Users
- ✅ No manual DLL downloads
- ✅ Automatic framework selection
- ✅ Runtime compatibility checking
- ✅ Persistent across restarts
- ✅ Easy removal
- ✅ Native dependency handling (x64, x86, arm64)

### For Development
- ✅ Uses established ConfigEditor architecture
- ✅ Follows Beep folder structure patterns
- ✅ JSON-based tracking
- ✅ Comprehensive error handling
- ✅ User feedback at each step

## Future Enhancements

### Planned Features
- [ ] Driver update checking (compare installed vs latest version)
- [ ] Bulk driver installation from configuration file
- [ ] Driver dependency resolution (if one driver requires another)
- [ ] Signature verification for security
- [ ] Offline package installation (local .nupkg file)

### Potential Improvements
- [ ] Download progress bar with percentage
- [ ] Parallel installation of multiple drivers
- [ ] Driver metadata caching
- [ ] Auto-update on BeepShell startup
