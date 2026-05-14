# Phase 2 — Connection and Driver Configuration Step

## Objective

Implement `ConnectionConfigStep`, the first concrete wizard step that guides a setup run through building, validating, driver-resolving, persisting, and opening a database connection using the existing `ConnectionHelper` helpers and `ConfigEditor` API.

This step is idempotent: if a connection with the target name already exists and is open, the step skips immediately.

---

## Scope

- **`DriverProvisionStep : ISetupStep`** — prerequisite step that ensures the required driver is loaded into `ConfigEditor.DataDriversClasses` before connection config runs
- `DriverProvisionStepOptions` — step-level options for driver provisioning
- `ConnectionConfigStep : ISetupStep`
- `ConnectionConfigStepOptions` — step-level configuration
- Integration points with `ConnectionHelper`, `ConfigEditor`, `assemblyHandler`, and `IDataSource` lifecycle
- Credential masking before any log write
- Path normalization for file-based datasources (SQLite, CSV, JSON)

---

## Background: Existing Helpers

| Helper | Location | Responsibility |
|---|---|---|
| `ConnectionDriverLinkingHelper` | `DataManagementEngineStandard/Helpers/ConnectionHelpers/` | Resolve `DriverName` + `DriverVersion` from driver registry |
| `ConnectionStringProcessingHelper` | same | Replace template `{placeholders}` in connection string |
| `ConnectionStringValidationHelper` | same | Structural and provider-specific validation |
| `ConnectionStringSecurityHelper` | same | Mask secrets before logs / display |
| `ConnectionHelper` | same | Facade over all of the above |
| `ConfigEditor.AddDataConnection` | `DataManagementEngineStandard/ConfigUtil/` | Persist `ConnectionProperties` to JSON config |
| `ConfigEditor.UpdateDataConnection` | same | Update an existing connection |
| `ConfigEditor.SaveConnectionDriversConfigValues` | same | Persist driver registry after load/sync |
| `assemblyHandler.LoadDriverFromLocalPackage` | `DMEEditor.assemblyHandler` | Load driver from local NuGet cache (no download) |
| `assemblyHandler.LoadNuggetFromNuGetAsync` | `DMEEditor.assemblyHandler` | Download + load driver from NuGet feed |
| `assemblyHandler.HasLocalPackage` | `DMEEditor.assemblyHandler` | Check if driver package is cached locally |
| `editor.OpenDataSource(name)` | `DMEEditor` | Open connection, returns `ConnectionState` |
| `editor.GetDataSource(name)` | `DMEEditor` | Retrieve opened datasource |

---

## Pre-Requisite Step: `DriverProvisionStep`

`ConnectionConfigStep` calls `ConnectionHelper.GetBestMatchingDriver`, which searches `ConfigEditor.DataDriversClasses`. If the required driver is not yet loaded, that call returns `null` and the step fails. `DriverProvisionStep` must run **before** `ConnectionConfigStep` to guarantee the driver is present.

### Three-State Driver Model

This model is directly derived from `ConnectionDrivers.razor` (`SyncDriverStatus`, `LoadDriverFromCacheAsync`, `DownloadNuGetAsync`):

| State | Condition | Action |
|---|---|---|
| **Loaded** | `!driver.IsMissing` | Driver is in process — skip, nothing to do |
| **Cached** | `driver.IsMissing && !driver.NuggetMissing` | Package is on disk but not loaded → `assemblyHandler.LoadDriverFromLocalPackage(driver)` |
| **Missing** | `driver.NuggetMissing` (or driver not in `DataDriversClasses`) | Package not downloaded → `assemblyHandler.LoadNuggetFromNuGetAsync(packageId, version, sources)` |

After any load operation, call `ConfigEditor.SaveConnectionDriversConfigValues()` to persist the updated `IsMissing` / `NuggetMissing` flags.

### `DriverProvisionStep` Contract

```csharp
namespace TheTechIdea.Beep.SetUp.Steps
{
    /// <summary>
    /// Prerequisite step: ensures the required driver is loaded into
    /// ConfigEditor.DataDriversClasses before ConnectionConfigStep runs.
    /// Implements the three-state model from ConnectionDrivers.razor:
    ///   Loaded → skip
    ///   Cached → LoadDriverFromLocalPackage
    ///   Missing → LoadNuggetFromNuGetAsync
    /// </summary>
    public class DriverProvisionStep : ISetupStep
    {
        public string StepId   => "driver-provision";
        public string StepName => "Provision Connection Driver";
        public string Description =>
            "Ensures the required driver is loaded into the driver registry " +
            "before connection configuration runs.";
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();

        private readonly DriverProvisionStepOptions _options;

        public DriverProvisionStep(DriverProvisionStepOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Skip when a driver matching the requested DataSourceType is already loaded
        /// (IsMissing == false).
        /// </summary>
        public bool CanSkip(SetupContext context)
        {
            if (context.Editor?.ConfigEditor?.DataDriversClasses == null) return false;
            var driver = context.Editor.ConfigEditor.DataDriversClasses
                .FirstOrDefault(d =>
                    d.DatasourceType == _options.DataSourceType && !d.IsMissing);
            return driver != null;
        }

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context.Editor?.assemblyHandler == null)
                return Fail("SetupContext.Editor.assemblyHandler must not be null.");
            if (string.IsNullOrWhiteSpace(_options.PackageId))
                return Fail("DriverProvisionStepOptions.PackageId must be set.");
            return Ok();
        }

        public IErrorsInfo Execute(SetupContext context,
            IProgress<PassedArgs> progress = null)
        {
            var editor  = context.Editor;
            var drivers = editor.ConfigEditor.DataDriversClasses;

            // Locate existing registry entry (may or may not be loaded)
            var entry = drivers.FirstOrDefault(d =>
                d.DatasourceType == _options.DataSourceType ||
                string.Equals(d.PackageName, _options.PackageId,
                    StringComparison.OrdinalIgnoreCase));

            // ── State 1: already loaded ───────────────────────────────────────
            if (entry != null && !entry.IsMissing)
            {
                Report(progress, 100, $"Driver '{entry.PackageName}' already loaded.");
                return Ok($"Driver '{entry.PackageName}' is already loaded.");
            }

            // ── State 2: cached locally, not yet loaded ───────────────────────
            if (entry != null && !entry.NuggetMissing)
            {
                Report(progress, 30, $"Loading '{entry.PackageName}' from local cache...");
                bool loaded = editor.assemblyHandler.LoadDriverFromLocalPackage(entry, out _);
                if (!loaded)
                {
                    // Re-check — may now be genuinely missing
                    entry.NuggetMissing = !editor.assemblyHandler.HasLocalPackage(entry);
                    editor.ConfigEditor.SaveConnectionDriversConfigValues();

                    if (entry.NuggetMissing)
                        goto downloadFromNuGet;  // fall through to State 3

                    return Fail($"LoadDriverFromLocalPackage failed for '{entry.PackageName}'.");
                }
                editor.ConfigEditor.SaveConnectionDriversConfigValues();
                Report(progress, 100, $"Driver '{entry.PackageName}' loaded from cache.");
                return Ok($"Driver '{entry.PackageName}' loaded from local cache.");
            }

            // ── State 3: not downloaded — fetch from NuGet ────────────────────
            downloadFromNuGet:
            var packageId = _options.PackageId;
            var version   = string.IsNullOrWhiteSpace(_options.Version) ? null : _options.Version;
            var sources   = _options.NuGetSources?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                            ?? new List<string>();

            Report(progress, 20, $"Downloading '{packageId}' from NuGet...");
            editor.Logger?.WriteLog(
                $"[DriverProvisionStep] Downloading '{packageId}' v{version ?? "latest"} " +
                $"from sources: {string.Join(", ", sources)}");

            List<System.Reflection.Assembly> loaded2;
            try
            {
                loaded2 = editor.assemblyHandler.LoadNuggetFromNuGetAsync(
                    packageId,
                    version,
                    sources.Count > 0 ? sources : null,
                    _options.UseSingleSharedContext,
                    _options.InstallPath,
                    _options.UseProcessHost).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return Fail($"NuGet download failed for '{packageId}': {ex.Message}", ex);
            }

            if (loaded2 == null || loaded2.Count == 0)
                return Fail($"'{packageId}' was downloaded but no assemblies were loaded.");

            editor.ConfigEditor.SaveConnectionDriversConfigValues();

            // Verify driver now appears in registry
            var confirmed = editor.ConfigEditor.DataDriversClasses
                .Any(d => d.DatasourceType == _options.DataSourceType && !d.IsMissing);
            if (!confirmed)
                return Fail(
                    $"Package '{packageId}' loaded ({loaded2.Count} assemblies) but " +
                    $"no driver for {_options.DataSourceType} appeared in DataDriversClasses. " +
                    "Ensure the package has an [AddinAttribute] on its IDataSource class.");

            Report(progress, 100,
                $"Driver '{packageId}' installed ({loaded2.Count} assemblies).");
            return Ok($"Driver '{packageId}' installed and loaded.");
        }

        private static IErrorsInfo Ok(string msg = "Ok") =>
            new ErrorsInfo { Flag = Errors.Ok, Message = msg };
        private static IErrorsInfo Fail(string msg, Exception ex = null) =>
            new ErrorsInfo { Flag = Errors.Failed, Message = msg, Ex = ex };
        private static void Report(IProgress<PassedArgs> p, int pct, string msg)
            => p?.Report(new PassedArgs { ParameterInt1 = pct, Messege = msg });
    }
}
```

### `DriverProvisionStepOptions`

```csharp
namespace TheTechIdea.Beep.SetUp.Steps
{
    public class DriverProvisionStepOptions
    {
        /// <summary>Target datasource type (e.g. DataSourceType.PostgreSQL).</summary>
        public DataSourceType DataSourceType { get; set; }

        /// <summary>NuGet package ID for the driver (e.g. "TheTechIdea.Beep.PostgreSQLDataSource").</summary>
        public string PackageId { get; set; } = string.Empty;

        /// <summary>Specific version to install. Null or empty = latest.</summary>
        public string? Version { get; set; }

        /// <summary>NuGet feed URLs. Null = use assemblyHandler defaults.</summary>
        public List<string>? NuGetSources { get; set; }

        /// <summary>Override install path. Null = default ConnectionDrivers/{PackageId}/.</summary>
        public string? InstallPath { get; set; }

        /// <summary>Passed to assemblyHandler.LoadNuggetFromNuGetAsync.</summary>
        public bool UseSingleSharedContext { get; set; } = true;

        /// <summary>Passed to assemblyHandler.LoadNuggetFromNuGetAsync.</summary>
        public bool UseProcessHost { get; set; } = false;
    }
}
```

### `DriverProvisionStep` Workflow

```
DriverProvisionStep.Execute
  │
  ├─ Locate entry in ConfigEditor.DataDriversClasses by DataSourceType or PackageName
  │
  ├─ State 1: entry found && !IsMissing
  │       → already loaded, return Ok immediately
  │
  ├─ State 2: entry found && !NuggetMissing (cached locally)
  │       → assemblyHandler.LoadDriverFromLocalPackage(entry)
  │       → ConfigEditor.SaveConnectionDriversConfigValues()
  │       → if load failed AND now NuggetMissing → fall through to State 3
  │
  └─ State 3: NuggetMissing or no entry
          → assemblyHandler.LoadNuggetFromNuGetAsync(packageId, version, sources)
          → ConfigEditor.SaveConnectionDriversConfigValues()
          → verify entry now in DataDriversClasses with !IsMissing
          → if not found → Errors.Failed (package has no [AddinAttribute])
```

---

## Step Contract

```csharp
namespace TheTechIdea.Beep.SetUp.Steps
{
    public class ConnectionConfigStep : ISetupStep
    {
        public string StepId => "connection-config";
        public string StepName => "Configure Database Connection";
        public string Description =>
            "Validates, driver-resolves, persists, and opens the target database connection.";
        public IReadOnlyList<string> DependsOn => Array.Empty<string>();

        private readonly ConnectionConfigStepOptions _stepOptions;

        public ConnectionConfigStep(ConnectionConfigStepOptions options)
        {
            _stepOptions = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Skip if the connection already exists in ConfigEditor AND opens successfully.
        /// </summary>
        public bool CanSkip(SetupContext context)
        {
            if (context.Editor == null) return false;
            var existing = context.Editor.ConfigEditor
                .DataConnections
                .FirstOrDefault(c => c.ConnectionName == _stepOptions.ConnectionName);
            if (existing == null) return false;

            var state = context.Editor.OpenDataSource(existing.ConnectionName);
            if (state == ConnectionState.Open)
            {
                context.DataSource = context.Editor.GetDataSource(existing.ConnectionName);
                context.ConnectionProperties = existing;
                return true;
            }
            return false;
        }

        public IErrorsInfo Validate(SetupContext context)
        {
            if (context.Editor == null)
                return Fail("SetupContext.Editor must not be null before ConnectionConfigStep.");
            if (_stepOptions.ConnectionProperties == null)
                return Fail("ConnectionConfigStepOptions.ConnectionProperties must be set.");
            return Ok();
        }

        public IErrorsInfo Execute(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            var props = _stepOptions.ConnectionProperties;
            var editor = context.Editor;
            Report(progress, 5, "Resolving driver...");

            // 1. Resolve driver
            var driver = ConnectionHelper.GetBestMatchingDriver(props, editor.ConfigEditor);
            if (driver == null)
                return Fail($"No driver found for DatabaseType={props.DatabaseType}.");
            props.DriverName = driver.PackageName;
            props.DriverVersion = driver.version;

            // 2. Fill template placeholders
            Report(progress, 20, "Processing connection string...");
            props.ConnectionString = ConnectionHelper.ReplaceValueFromConnectionString(
                props, editor.ConfigEditor);

            // 3. Normalize file paths for local datasources
            if (props.IsFile || props.IsLocal)
            {
                props.FilePath = ConnectionHelper.NormalizePath(props.FilePath,
                    AppContext.BaseDirectory);
            }

            // 4. Structural and provider-specific validation
            Report(progress, 35, "Validating connection string...");
            var isValid = ConnectionHelper.IsConnectionStringValid(
                props.ConnectionString, props.DatabaseType);
            if (!isValid)
                return Fail("Connection string validation failed. " +
                    "Check host, port, database name, and credentials.");

            // 5. Mask and log before persisting
            var masked = ConnectionHelper.SecureConnectionString(
                props.ConnectionString, props.DatabaseType);
            editor.Logger?.WriteLog(
                $"[ConnectionConfigStep] Persisting connection '{props.ConnectionName}' " +
                $"driver={props.DriverName} type={props.DatabaseType} cs={masked}");

            // 6. Persist to ConfigEditor
            Report(progress, 55, "Saving connection configuration...");
            var existing = editor.ConfigEditor.DataConnections
                .FirstOrDefault(c => c.ConnectionName == props.ConnectionName);
            if (existing == null)
                editor.ConfigEditor.AddDataConnection(props);
            else
                editor.ConfigEditor.UpdateDataConnection(props);

            // 7. Open connection
            Report(progress, 75, "Opening connection...");
            var state = editor.OpenDataSource(props.ConnectionName);
            if (state != ConnectionState.Open)
                return Fail($"OpenDataSource returned {state} for '{props.ConnectionName}'.");

            // 8. Write back to context
            context.DataSource = editor.GetDataSource(props.ConnectionName);
            context.ConnectionProperties = props;

            Report(progress, 100, "Connection established.");
            return Ok($"Connection '{props.ConnectionName}' opened successfully.");
        }

        // --- helpers ---
        private static IErrorsInfo Ok(string msg = "Ok") =>
            new ErrorsInfo { Flag = Errors.Ok, Message = msg };
        private static IErrorsInfo Fail(string msg, Exception ex = null) =>
            new ErrorsInfo { Flag = Errors.Failed, Message = msg, Ex = ex };
        private static void Report(IProgress<PassedArgs> p, int pct, string msg)
        {
            p?.Report(new PassedArgs { ParameterInt1 = pct, Messege = msg });
        }
    }
}
```

### `ConnectionConfigStepOptions`

```csharp
namespace TheTechIdea.Beep.SetUp.Steps
{
    public class ConnectionConfigStepOptions
    {
        /// <summary>Connection name used as the lookup key in ConfigEditor.</summary>
        public string ConnectionName { get; set; }

        /// <summary>Draft ConnectionProperties to be validated and persisted.</summary>
        public ConnectionProperties ConnectionProperties { get; set; }

        /// <summary>
        /// If true, overwrite an existing connection with the same name rather than skipping.
        /// Useful for update/upgrade scenarios.
        /// </summary>
        public bool ForceUpdate { get; set; } = false;
    }
}
```

---

## Workflow Walkthrough

```
[DriverProvisionStep.Execute]  ← NEW prerequisite — runs before ConnectionConfigStep
  │
  ├─ Check ConfigEditor.DataDriversClasses for DataSourceType
  │
  ├─ State 1 (!IsMissing): already loaded → return Ok
  ├─ State 2 (!NuggetMissing): assemblyHandler.LoadDriverFromLocalPackage → SaveConnectionDriversConfigValues
  └─ State 3 (NuggetMissing): assemblyHandler.LoadNuggetFromNuGetAsync → SaveConnectionDriversConfigValues

[ConnectionConfigStep.Execute]
  │
  ├─ 1. ConnectionDriverLinkingHelper.GetBestMatchingDriver
  │       → fills props.DriverName + DriverVersion
  │       (guaranteed to succeed because DriverProvisionStep ran first)
  │
  ├─ 2. ConnectionStringProcessingHelper.ReplaceValueFromConnectionString
  │       → resolves {host}, {database}, {port} placeholders
  │
  ├─ 3. Path normalization (file-based providers)
  │       → absolute path from relative FilePath + BaseDirectory
  │
  ├─ 4. ConnectionStringValidationHelper.IsConnectionStringValid
  │       → structural + provider-specific check
  │
  ├─ 5. ConnectionStringSecurityHelper.SecureConnectionString
  │       → masked copy for logging only
  │
  ├─ 6. ConfigEditor.AddDataConnection / UpdateDataConnection
  │       → persisted to DataConnections.json
  │
  ├─ 7. editor.OpenDataSource(name)
  │       → ConnectionState.Open expected
  │
  └─ 8. context.DataSource = editor.GetDataSource(name)
          → downstream steps receive open datasource
```

---

## Provider-Specific Notes

### SQLite / File-Based
- `props.FilePath` must be absolute before `AddDataConnection`.
- Use `ConnectionHelper.NormalizePath(props.FilePath, AppContext.BaseDirectory)`.
- Validate `.db` or `.sqlite` extension present.

### SQL Server
- Validate `Host`, `Database`, `UserID` are non-empty.
- If `IntegratedSecurity=true`, skip `UserID`/`Password` masking.
- For named instances, ensure `Host` includes `\InstanceName`.

### PostgreSQL / MySQL
- Validate `Port` is within 1–65535.
- Normalize `Host` to lowercase (case-sensitive on Linux).

### Oracle
- Identifier length limit: 30 chars for Oracle 11/12c, 128 chars for 18c+.
- Validate `ServiceName` or `SID` is present in connection string.

### In-Memory / CSV / JSON
- No host/port required; validate `FilePath` or `ConnectionString` format only.
- These providers rarely need driver resolution — driver is always the same.

---

## Security Rules

- **Never log raw `ConnectionString`** — always call `ConnectionStringSecurityHelper.SecureConnectionString` first.
- **Never store plain-text passwords** in `SetupState` or `SetupReport`.
- `ConnectionProperties.Password` should be cleared from context after step completes if `_stepOptions.ClearPasswordAfterOpen = true`.
- Validate that the connection string does not contain SQL injection sequences before persisting.

---

## Idempotency Contract

```
CanSkip returns true when:
  ConfigEditor.DataConnections contains a connection with matching ConnectionName
  AND  editor.OpenDataSource(name) returns ConnectionState.Open

Effect: context.DataSource and context.ConnectionProperties are populated
        without re-running driver resolution or re-persisting.
```

---

## File Layout

```
DataManagementEngineStandard/
  SetUp/
    Steps/
      DriverProvisionStep.cs
      DriverProvisionStepOptions.cs
      ConnectionConfigStep.cs
      ConnectionConfigStepOptions.cs
```

---

## Dependencies

| Type | Location |
|---|---|
| `ConnectionHelper` | `DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionHelper.cs` |
| `ConnectionDriverLinkingHelper` | same folder |
| `ConnectionStringProcessingHelper` | same folder |
| `ConnectionStringValidationHelper` | same folder |
| `ConnectionStringSecurityHelper` | same folder |
| `ConfigEditor` | `DataManagementEngineStandard/ConfigUtil/ConfigEditor.cs` |
| `IDMEEditor.OpenDataSource` | `DataManagementModelsStandard/Editor/IDMEEditor.cs` |
| `IDMEEditor.assemblyHandler` | `DataManagementModelsStandard/Editor/IDMEEditor.cs` |
| `IAssemblyHandler.LoadDriverFromLocalPackage` | `DataManagementModelsStandard/` (assembly handler interface) |
| `IAssemblyHandler.LoadNuggetFromNuGetAsync` | same |
| `IAssemblyHandler.HasLocalPackage` | same |
| `ISetupStep` | `DataManagementEngineStandard/SetUp/ISetupStep.cs` (Phase 1) |
| `SetupContext` | `DataManagementEngineStandard/SetUp/SetupContext.cs` (Phase 1) |

---

## Testing Approach

### DriverProvisionStep Tests

| Test | Description |
|---|---|
| `DriverProvisionStep_DriverAlreadyLoaded_Skips` | `CanSkip` returns true when matching driver has `IsMissing = false` |
| `DriverProvisionStep_CachedLocally_LoadsFromDisk` | `!NuggetMissing` path calls `LoadDriverFromLocalPackage`, saves config |
| `DriverProvisionStep_CachedLoad_FallsThrough_ToNuGet_WhenDiskFails` | State 2 → disk load fails → entry now `NuggetMissing` → falls to State 3 |
| `DriverProvisionStep_NuGetDownload_PopulatesDataDriversClasses` | State 3 → after `LoadNuggetFromNuGetAsync`, entry appears in `DataDriversClasses` |
| `DriverProvisionStep_NuGetNoAddinAttribute_ReturnsFailed` | Assemblies loaded but no `[AddinAttribute]` type → `Errors.Failed` |
| `DriverProvisionStep_NuGetThrows_ReturnsFailed` | `LoadNuggetFromNuGetAsync` throws → `Errors.Failed` with exception |

### ConnectionConfigStep Tests

| Test | Description |
|---|---|
| `ConnectionConfigStep_NewConnection_PersistsAndOpens` | Happy path: new connection persisted + opened |
| `ConnectionConfigStep_ExistingOpenConnection_Skips` | CanSkip returns true, no re-persist |
| `ConnectionConfigStep_InvalidConnectionString_ReturnsFailed` | Validation failure returns Errors.Failed |
| `ConnectionConfigStep_DriverNotFound_ReturnsFailed` | No matching driver → Errors.Failed (should not occur if DriverProvisionStep ran) |
| `ConnectionConfigStep_FilePathNormalized_ForSqlite` | Relative path → absolute path in ConnectionProperties |
| `ConnectionConfigStep_PasswordNotInLog` | Logger is never called with raw password |

### Integration Tests

For each test that exercises an actual driver, resolve the driver through `ConfigEditor.DataDriversClasses` — do not hardcode a provider. Use `DriverProvisionStep` as the first step in the sequence.

---

## Acceptance Criteria

### DriverProvisionStep
- [ ] `DriverProvisionStep` implements `ISetupStep` with `StepId = "driver-provision"`.
- [ ] `CanSkip` returns `true` when a driver for the requested `DataSourceType` has `IsMissing = false`.
- [ ] State 1 (loaded): returns `Ok` without calling any assemblyHandler method.
- [ ] State 2 (cached): calls `LoadDriverFromLocalPackage` then `SaveConnectionDriversConfigValues`.
- [ ] State 2 fallthrough: if disk load sets `NuggetMissing = true`, proceeds to State 3.
- [ ] State 3 (missing): calls `LoadNuggetFromNuGetAsync`, saves config, verifies entry in `DataDriversClasses`.
- [ ] State 3 failure: if no `[AddinAttribute]` type found after load, returns `Errors.Failed` with clear message.
- [ ] All assembly handler calls are guarded; exceptions are caught and returned as `Errors.Failed`.

### ConnectionConfigStep
- [ ] `ConnectionConfigStep` implements `ISetupStep` with `StepId = "connection-config"`.
- [ ] Driver resolution via `ConnectionHelper.GetBestMatchingDriver` is called before persist.
- [ ] `IsConnectionStringValid` is called; failure returns `Errors.Failed` without persisting.
- [ ] `SecureConnectionString` is used for all log writes.
- [ ] `CanSkip` returns `true` when a connection with the same name is already open.
- [ ] `context.DataSource` and `context.ConnectionProperties` are populated on success.
- [ ] File-based providers have their `FilePath` normalized to absolute before `AddDataConnection`.

### Step Ordering
- [ ] `SetupWizardBuilder` registers `DriverProvisionStep` before `ConnectionConfigStep` when the desired driver may not be pre-loaded.
- [ ] Both steps are registered in the `DependsOn` chain if strict ordering is enforced at the wizard level.
