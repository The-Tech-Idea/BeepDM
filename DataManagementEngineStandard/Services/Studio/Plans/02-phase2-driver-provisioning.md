# Phase 03 — Driver Provisioning (`IDriverService`)

> **Scope:** implement `IDriverService` — the Studio's driver-provisioning sub-service.
> A **driver** is a `ConnectionDriversConfig` entry (see
> `DataManagementModelsStandard/DriversConfigurations/ConnectionDriversConfig.cs:8`)
> plus the actual `IDataSource` class that implements it. The Studio can provision
> drivers from three sources: **NuGet**, **local folder**, and **plugin assembly** —
> all without taking a hard dependency on any specific provider.

> Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## Why this phase

The engine's existing `SetUp/Steps/DriverProvisionStep.cs:25` already does this for
the first-run wizard, but it is locked inside the `SetupWizard` flow. The Studio
needs a **standalone** `IDriverService` so that a developer can add a new driver
mid-project (e.g. they decided to add MongoDB to a SQL Server app) without re-running
the wizard.

This phase is also the place where the **replacement for the two near-duplicate
SQL Server connection-string composers** in
`C:\Users\f_ald\source\repos\fahadTheTechIdea\MyWebSite\TheTechIdeaWeb\Beep.EventsRegistration\Services\Setup\`
finally lands. The driver's `ConnectionStringComposer` (one per category) is the
single source of truth.

## Public surface (this phase fills in)

```csharp
// Contracts/IDriverService.cs
public interface IDriverService
{
    Task<StudioResult<IReadOnlyList<DriverInfo>>> ListAsync(CancellationToken ct = default);
    Task<StudioResult<DriverInfo>> GetAsync(string packageName, CancellationToken ct = default);
    Task<StudioResult<DriverProvisionResult>> ProvisionAsync(DriverProvisionRequest request, IStudioProgress? progress = null, CancellationToken ct = default);
    Task<StudioResult<bool>> UnloadAsync(string packageName, CancellationToken ct = default);
}

// Models
public sealed record DriverInfo(
    string PackageName,                                  // e.g. "Beep.DataSource.SqlServer"
    string ClassName,                                    // e.g. "SqlServerDatasourceCore"
    string Version,
    DataSourceType DataSourceType,                       // enum from Models
    DatasourceCategory Category,
    string Source,                                       // "NuGet" | "Local" | "Plugin"
    string Location,                                     // path or URL
    bool IsLoaded,
    bool IsAutoLoad,
    string? IconName,
    IReadOnlyList<string> ExtensionsHandled,
    IReadOnlyList<string> FileExtensions);

public sealed record DriverProvisionRequest(
    string PackageName,
    string? Version,                                     // null = latest
    DriverSource Source,                                 // NuGet | Local | Plugin
    string? LocalPath,                                   // required for Source = Local
    string? PluginAssemblyPath,                          // required for Source = Plugin
    bool AutoLoad = true);

public enum DriverSource { NuGet, Local, Plugin }

public sealed record DriverProvisionResult(
    bool Success,
    string VersionResolved,
    string Location,
    bool Loaded,
    IReadOnlyList<string> ClassesRegistered);
```

## Folder layout (this phase creates)

```
Services/Studio/
├── Contracts/IDriverService.cs                      ← DONE in Phase 1
├── Models/
│   ├── DriverInfo.cs
│   ├── DriverProvisionRequest.cs
│   ├── DriverProvisionResult.cs
│   └── DriverSource.cs
└── Driver/
    ├── IDriverProvisioner.cs                         ← interface for the three source-specific provisioners
    ├── NuGetDriverProvisioner.cs                     ← NuGet path
    ├── LocalDriverProvisioner.cs                     ← local-folder path
    ├── PluginDriverProvisioner.cs                    ← plug-in assembly path
    ├── DriverCatalog.cs                              ← in-memory cache of installed drivers
    ├── DriverService.cs                              ← implements IDriverService
    └── DriverHealthChecker.cs                        ← verify a driver still loads
```

## Provisioner contract

```csharp
// Driver/IDriverProvisioner.cs
public interface IDriverProvisioner
{
    bool CanHandle(DriverSource source);
    Task<DriverProvisionResult> ProvisionAsync(DriverProvisionRequest request, IStudioProgress? progress, CancellationToken ct);
    Task<IReadOnlyList<DriverInfo>> ListAsync(CancellationToken ct);
}
```

### `NuGetDriverProvisioner`

Wraps the engine's existing `NuGetManagement/` folder. Falls back to a direct
HTTP download from `https://api.nuget.org/v3-flatcontainer/` if the local
NuGet tooling isn't available. Writes the package to
`%DataRoot%/drivers/<packageName>/<version>/`. Adds the DLL to the
`IAssemblyHandler.LoadDriverFromLocalPackage` pipeline (defined in
`SetUp/Steps/DriverProvisionStep.cs:25`).

### `LocalDriverProvisioner`

Loads a driver from a folder the user supplies (`--local-path` on the future CLI,
file picker on the Blazor host). Validates that the folder contains a DLL with
at least one `IDataSource` implementation, then registers it.

### `PluginDriverProvisioner`

Loads a driver from an already-registered plugin assembly. Used when the host
project bundles drivers as part of its own plug-in system.

## Driver catalog

`DriverCatalog` is the in-memory index of all installed drivers. It is the
`IDriverService.ListAsync` data source. Persists to
`%DataRoot%/driver-catalog.json` (rebuilt on startup by scanning the
`%DataRoot%/drivers/` folder).

The catalog is the answer to the question: "what data sources can a host
app connect to right now?" This is what the **Sources** UI (Phase 4) uses to
populate the driver picker.

## Connection-string composer (the dead-code replacement)

This phase also adds `Driver/ConnectionStringComposers/` — a folder containing
one composer per category. This is the **replacement for the two near-duplicate
SQL Server composers** in `Beep.EventsRegistration/Services/Setup/`.

```csharp
public interface IConnectionStringComposer
{
    DatasourceCategory Category { get; }
    string Compose(ConnectionProperties properties);
    IErrorsInfo Validate(ConnectionProperties properties);
}

// One impl per category
internal sealed class RdbmsConnectionStringComposer : IConnectionStringComposer { ... }
internal sealed class NoSqlConnectionStringComposer : IConnectionStringComposer { ... }
internal sealed class FileConnectionStringComposer : IConnectionStringComposer { ... }
internal sealed class WebApiConnectionStringComposer : IConnectionStringComposer { ... }
internal sealed class CloudConnectionStringComposer : IConnectionStringComposer { ... }
internal sealed class CacheConnectionStringComposer : IConnectionStringComposer { ... }
internal sealed class InMemoryConnectionStringComposer : IConnectionStringComposer { ... }
internal sealed class StreamingConnectionStringComposer : IConnectionStringComposer { ... }
internal sealed class VectorDbConnectionStringComposer : IConnectionStringComposer { ... }
internal sealed class BlockchainConnectionStringComposer : IConnectionStringComposer { ... }
```

These composers are registered in DI as `IEnumerable<IConnectionStringComposer>`
and resolved by category. They **delegate to the existing
`ConnectionHelper_RDBMS.cs` etc.** in the engine — they don't re-implement the
string-building logic. Their job is to be the **single entrypoint** that
`ISourceService` (Phase 4) calls.

The two `Beep.EventsRegistration` composers (`DatabaseConnectionStringComposer.cs`
+ `SqlServerConnectionStringComposer.cs`) become **dead code** and get deleted
as part of the per-app conversion in
`C:\Users\f_ald\source\repos\The-Tech-Idea\BeepWeb\.plans\phase-24.md` task 24.14.

## Cross-cutting

- `IDriverService.ProvisionAsync` records an `IBeepAudit` event with the
  `Source`, `PackageName`, `Version`, and result. Wired in Phase 8.
- Provisioning a driver must be **idempotent**: calling it twice for the same
  `(package, version)` is a no-op.
- The driver catalog is **lazy**: it only scans disk on first call to
  `ListAsync`, then caches the result for the process lifetime.

---

## Todo Tracker

| # | Task | Status | Notes |
|---|------|--------|-------|
| P03-01 | `Models/DriverInfo.cs` + `DriverProvisionRequest.cs` + `DriverProvisionResult.cs` + `DriverSource.cs` | ⬜ | |
| P03-02 | `Driver/IDriverProvisioner.cs` | ⬜ | |
| P03-03 | `Driver/NuGetDriverProvisioner.cs` — wraps `NuGetManagement/` + fallback HTTP download | ⬜ | |
| P03-04 | `Driver/LocalDriverProvisioner.cs` — loads from a user-supplied folder | ⬜ | |
| P03-05 | `Driver/PluginDriverProvisioner.cs` — loads from a registered plugin assembly | ⬜ | |
| P03-06 | `Driver/DriverCatalog.cs` — in-memory index, persists to `driver-catalog.json` | ⬜ | |
| P03-07 | `Driver/DriverService.cs` — implements `IDriverService` | ⬜ | |
| P03-08 | `Driver/DriverHealthChecker.cs` — verifies a driver still loads | ⬜ | |
| P03-09 | `Driver/ConnectionStringComposers/IConnectionStringComposer.cs` | ⬜ | |
| P03-10 | One `*ConnectionStringComposer.cs` per category (10 total: RDBMS, NoSQL, File, WebApi, Cloud, Cache, InMemory, Streaming, VectorDb, Blockchain) | ⬜ | |
| P03-11 | Register `IDriverService`, the three provisioners, and the composers in `AddBeepStudio()` | ⬜ | |
| P03-12 | Tests: `NuGetDriverProvisionerTests` (2+), `LocalDriverProvisionerTests` (2+), `DriverServiceTests` (3+), `RdbmsConnectionStringComposerTests` (3+ for SQL Server, SQLite, Postgres) | ⬜ | Land in the host test project |
| P03-13 | Document: per-platform "drivers" folder convention (Windows / Linux / macOS) | ⬜ | |
| P03-14 | Update `00-overview-and-scope.md` + `MASTER-TODO-TRACKER.md` to mark Phase 03 done | ⬜ | |

---

## Validation (definition of done)

- [ ] `dotnet build DataManagementEngineStandard` succeeds with **0 errors**.
- [ ] `DriverService.ListAsync` returns the built-in drivers that ship with the engine (SQLite, SQL Server, MySQL, Postgres, Oracle, FileDataSource, WebAPI, InMemory).
- [ ] `DriverService.ProvisionAsync` for a local path works end-to-end: copies the DLL, registers the class, refreshes the catalog.
- [ ] The `RdbmsConnectionStringComposer.Compose` method produces the same connection strings as the engine's existing `ConnectionHelper_RDBMS.cs`.
- [ ] All 3 provisioner tests + 3 driver-service tests + 3 RDBMS composer tests pass.

---

## Pitfalls

1. **Don't add a NuGet dependency on `NuGet.Protocol`** — the engine's `NuGetManagement/` folder is the right entrypoint. If it doesn't expose what we need, add a method there first.
2. **Don't load a driver DLL into the engine process** if the host process is the only consumer — the driver should be loaded into the host's `IAssemblyHandler`, not the engine's. The Studio delegates the loading decision to the host via the `IStudioHostAdapter` (Phase 9).
3. **Don't re-implement connection-string parsing** — the composers are thin wrappers over `ConnectionHelper_*`. Re-using the existing parsers is the whole point.
4. **Don't fail the entire `ProvisionAsync` if one driver fails to load** — return a `DriverProvisionResult` with `Success = false` and a list of which classes did register.
5. **Don't take a hard dependency on `Spectre.Console`** in this phase — the host provides the progress reporter via `IStudioProgress` (Phase 1).
6. **Don't run the NuGet download in the Blazor Server circuit** — use a `Task.Run` or an `IBackgroundTaskQueue` (defined in Phase 9) to keep the circuit responsive.

---

## Related

- Phase 01 — contracts (this phase implements `IDriverService`)
- Phase 04 — source configuration (uses the catalog to populate the driver picker)
- Phase 09 — platform adapters (the provisioners run on background threads in Blazor Server)
- `SetUp/Steps/DriverProvisionStep.cs:25` — the existing step that the Studio's `IDriverService` extends, not replaces
- `Helpers/ConnectionHelpers/ConnectionHelper_RDBMS.cs` — the existing RDBMS composer we wrap (not re-implement)
