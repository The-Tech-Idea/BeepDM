# Phase 01 — Core Contracts & Options

> **Scope:** define the public surface of the Studio — interfaces, options POCOs, and
> the top-level `IStudioService` facade. Every other phase implements one of these
> interfaces. No business logic in this phase; just contracts. **Project-scaffolding
> contracts (Phase 2 of the v1 plan) are removed in rev 2** — the Studio does not
> scaffold new projects. The new `IDataLifecycleManifestService` and
> `IDeploymentMetadataService` contracts (Phases 9 and 10) are added.

> Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## Why this phase

The Studio spans 9 sub-services (driver, source, schema, migration, sync, governance,
manifest, deployment-metadata, plus a top-level facade) and two cross-cutting
contracts (`IStudioProgress`, `StudioResult<T>`). If the contracts aren't nailed
down first, the sub-services drift. This phase fixes the wire format before any
implementation begins.

## Folder layout (this phase creates)

```
Services/Studio/
├── Contracts/
│   ├── IStudioService.cs              ← top-level facade
│   ├── IStudioProgress.cs             ← platform-agnostic progress reporter
│   ├── IStudioHostAdapter.cs          ← Phase 8 stub interface
│   ├── IEnvironmentProfileService.cs
│   ├── IDriverService.cs              ← Phase 2 stub
│   ├── ISourceService.cs              ← Phase 3 stub
│   ├── ISchemaService.cs              ← Phase 4 stub
│   ├── IMigrationStudioService.cs     ← Phase 5 stub
│   ├── ISyncStudioService.cs          ← Phase 6 stub
│   ├── IGovernanceService.cs          ← Phase 7 stub
│   ├── IDataLifecycleManifestService.cs   ← Phase 9 stub
│   └── IDeploymentMetadataService.cs      ← Phase 10 stub
├── Models/
│   ├── StudioResult.cs                ← Result<T> for engine calls
│   ├── StudioInfo.cs
│   ├── StudioOptions.cs
│   ├── StudioConstants.cs
│   ├── StudioErrorCode.cs
│   ├── EnvironmentProfile.cs
│   ├── DataLifecycleManifest.cs       ← Phase 9 model
│   ├── DataLifecycleManifestEntry.cs
│   ├── ManifestValidationReport.cs
│   ├── DeploymentMetadata.cs          ← Phase 10 model
│   ├── CodeRevisionRef.cs
│   └── ApprovalToken.cs
├── BeepServiceExtensions.Studio.cs   ← `AddBeepStudio()` DI helper
├── StudioService.cs                   ← top-level facade impl (delegates to sub-services)
└── NullStudioService.cs               ← no-op default for opt-out hosts
```

## Cross-cutting contracts

### `IStudioProgress` — platform-agnostic progress

```csharp
namespace TheTechIdea.Beep.Studio.Contracts;

/// <summary>
/// Platform-agnostic progress reporter. The Studio reports lifecycle events
/// (Begin / Update / Complete / Failed) with a structured payload so any UI
/// shell (Blazor, WinForms, WPF, Maui, Console) can render it without
/// knowing engine internals.
/// </summary>
public interface IStudioProgress
{
    void Report(StudioProgressUpdate update);
}

public sealed record StudioProgressUpdate(
    string OperationId,                                  // correlates all updates in one operation
    string OperationName,                                // "Provisioning SQLite driver"
    StudioProgressStage Stage,                           // Begin | Update | Complete | Failed
    string? CurrentStep,                                 // "Downloading package..."
    int Percent,                                         // 0..100
    StudioProgressSeverity Severity,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object?>? Payload);     // operation-specific data

public enum StudioProgressStage { Begin, Update, Complete, Failed }
public enum StudioProgressSeverity { Info, Warning, Error }
```

`Adapters/StudioProgressBridge.cs` (Phase 8) provides the conversion to
`IProgress<PassedArgs>` (and back) so the engine's existing
`MigrationManager`/`BeepSyncManager` callers can keep using their existing
progress types.

### `StudioResult<T>` — Result pattern for engine calls

```csharp
public readonly record struct StudioResult<T>(
    bool IsSuccess,
    T? Value,
    StudioError Error)
{
    public static StudioResult<T> Ok(T value) => new(true, value, StudioError.None);
    public static StudioResult<T> Fail(StudioErrorCode code, string message, Exception? ex = null)
        => new(false, default, new StudioError(code, message, ex));
}

public readonly record struct StudioError(
    StudioErrorCode Code,
    string Message,
    Exception? Exception,
    IReadOnlyDictionary<string, object?>? Details);

public enum StudioErrorCode
{
    None = 0,
    Unknown = 1,
    InvalidArgument = 2,
    NotFound = 3,
    AlreadyExists = 4,
    PermissionDenied = 5,
    DriverMissing = 10,
    DriverLoadFailed = 11,
    ConnectionFailed = 20,
    ConnectionTestFailed = 21,
    SchemaIncompatible = 30,
    PlanRejected = 40,
    PolicyViolation = 41,
    ApprovalRequired = 42,
    ApprovalRejected = 43,
    ApplyFailed = 44,
    RollbackFailed = 45,
    SyncRunFailed = 50,
    ConflictUnresolved = 51,
    ManifestInvalid = 60,                                ← NEW in rev 2
    ManifestVersionUnsupported = 61,                     ← NEW in rev 2
    DeploymentMetadataMissing = 62,                      ← NEW in rev 2
    ApprovalTokenInvalid = 63,                           ← NEW in rev 2
    AuditFailed = 70,
    Cancelled = 80,
    HostNotSupported = 90,
    InternalError = 99,
}
```

### `StudioOptions` — DI options POCO

```csharp
public sealed class StudioOptions
{
    /// <summary>Path to the Studio data folder. Defaults to
    /// <c>%ProgramData%\TheTechIdea\Beep\Studio</c> on Windows,
    /// <c>~/.config/TheTechIdea/Beep/Studio</c> on Linux,
    /// <c>~/Library/Application Support/TheTechIdea/Beep/Studio</c> on macOS.
    /// </summary>
    public string DataRoot { get; set; } = EnvironmentService.CreateAppfolder("Studio");

    /// <summary>Override for the persistence layout. Default: <c>json</c>.</summary>
    public StudioPersistenceMode Persistence { get; set; } = StudioPersistenceMode.Json;

    /// <summary>Disable the audit hook. Defaults to <c>false</c> (audit ON).</summary>
    public bool DisableAudit { get; set; } = false;

    /// <summary>Disable the built-in hosted services (sync runner etc.). Useful
    /// in test hosts and CLI runs.</summary>
    public bool DisableHostedServices { get; set; } = false;

    /// <summary>Disable the auto-load of plug-in assemblies on startup.</summary>
    public bool DisableAssemblyLoad { get; set; } = false;

    /// <summary>Default tier applied to environments created via the API when
    /// the caller does not specify one.</summary>
    public RolloutTier DefaultEnvironmentTier { get; set; } = RolloutTier.Dev;

    /// <summary>How many days of audit history to retain on the local file sink.
    /// 0 = forever.</summary>
    public int AuditRetentionDays { get; set; } = 365;

    /// <summary>Override the keychain provider (DPAPI / libsecret / Keychain).
    /// Default: platform-appropriate auto-detect.</summary>
    public IKeychainProvider? Keychain { get; set; }

    // ---- NEW in rev 2 ----

    /// <summary>Path to the project's <c>DataLifecycleManifest</c>. If <c>null</c>,
    /// the Studio will look for <c>beep/data-lifecycle-manifest.json</c> in the
    /// current directory and walk up to the repo root.</summary>
    public string? ManifestPath { get; set; }

    /// <summary>When <c>true</c> (default), the Studio refuses to apply a migration
    /// or run a sync that does not match the manifest's expected source list.</summary>
    public bool EnforceManifestOnApply { get; set; } = true;

    /// <summary>When <c>true</c> (default), every audit event is enriched with
    /// <see cref="DeploymentMetadata"/> (code revision + build id).</summary>
    public bool EnrichAuditWithDeploymentMetadata { get; set; } = true;

    /// <summary>Override the HMAC key used to sign approval tokens. <c>null</c>
    /// means the DI extension will read from <c>BEEP_APPROVAL_HMAC_KEY</c>
    /// (or generate an ephemeral key in dev).</summary>
    public string? ApprovalHmacKey { get; set; }
}

public enum StudioPersistenceMode { Json, Sqlite, Hybrid }
```

### `StudioInfo` — version + capabilities

```csharp
public sealed record StudioInfo(
    string Version,
    string EngineVersion,
    IReadOnlyList<string> SupportedDataSourceTypes,
    IReadOnlyList<string> SupportedDataSourceCategories,
    IReadOnlyList<RolloutTier> SupportedTiers,
    bool AuditEnabled,
    bool HostedServicesEnabled,
    bool ManifestLoaded,                                  // NEW in rev 2
    string? ManifestVersion,                              // NEW in rev 2
    IReadOnlyDictionary<string, object?> Capabilities);
```

### `IStudioService` — top-level facade

See `00-overview-and-scope.md` for the full interface. Summary:

- 8 sub-service facades (`Environments`, `Drivers`, `Sources`, `Schemas`, `Migrations`,
  `Sync`, `Governance`, `Manifest`, `Deployment`) + a top-level `QueryAuditAsync` shortcut.
- Every method returns `StudioResult<T>` and accepts a `CancellationToken`.
- Mutation methods accept an optional `IStudioProgress?` reporter.
- Metadata methods are sync (`GetInfo`).

### `IEnvironmentProfileService` + `EnvironmentProfile`

```csharp
public interface IEnvironmentProfileService
{
    Task<StudioResult<IReadOnlyList<EnvironmentProfile>>> ListAsync(CancellationToken ct = default);
    Task<StudioResult<EnvironmentProfile>> GetAsync(string environmentId, CancellationToken ct = default);
    Task<StudioResult<EnvironmentProfile>> SaveAsync(EnvironmentProfile profile, CancellationToken ct = default);
    Task<StudioResult<bool>> DeleteAsync(string environmentId, CancellationToken ct = default);
    Task<StudioResult<EnvironmentProfile>> GetDefaultAsync(CancellationToken ct = default);
}

public sealed record EnvironmentProfile(
    string Id,
    string Name,                                   // "Dev", "Test", "Staging", "Live"
    RolloutTier Tier,
    int Order,                                     // display order in pickers
    string Color,                                  // UI hint, e.g. "#22C55E"
    bool RequiresApproval,
    int RequiredApproverCount,
    bool IsProduction,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public enum RolloutTier { Dev = 0, Test = 1, Staging = 2, Live = 3, Custom = 99 }
```

### NEW (rev 2): `IDataLifecycleManifestService` + `DataLifecycleManifest`

```csharp
public interface IDataLifecycleManifestService
{
    /// <summary>Load the manifest from the configured path (or auto-discover).</summary>
    Task<StudioResult<DataLifecycleManifest>> LoadAsync(string? overridePath = null, CancellationToken ct = default);

    /// <summary>Write the manifest to a path. Used by the host UI's manifest editor.</summary>
    Task<StudioResult<bool>> SaveAsync(DataLifecycleManifest manifest, string path, CancellationToken ct = default);

    /// <summary>Validate the manifest against the live Studio state (registered sources,
    /// registered drivers, governance policies).</summary>
    Task<StudioResult<ManifestValidationReport>> ValidateAsync(DataLifecycleManifest manifest, CancellationToken ct = default);

    /// <summary>Resolve the manifest path by walking up from CWD to the repo root,
    /// looking for <c>beep/data-lifecycle-manifest.json</c>.</summary>
    string? ResolveManifestPath(string? startDirectory = null);
}

public sealed record DataLifecycleManifest(
    int ManifestVersion,
    string Owner,
    ProjectRef Project,
    DataLifecycleSpec DataLifecycle,
    DateTimeOffset? GeneratedAt = null,
    string? SchemaUri = null);

public sealed record ProjectRef(
    string Name,
    string Type,                                          // "DotnetWebApi" | "BlazorServer" | "WinForms" | "WPF" | "Maui" | "Console" | "WebApi"
    string Repository,
    CodeRevisionRef CodeRevision);

public sealed record DataLifecycleSpec(
    string Owner,                                          // "data-platform@thetechidea.com"
    string Tier,                                           // "Standard" | "Regulated" | "Experimental"
    IReadOnlyList<EnvironmentSpec> Environments,
    IReadOnlyList<ExpectedSourceSpec> ExpectedSources,
    SchemaPolicies SchemaPolicies,
    SyncPolicies SyncPolicies,
    AuditPolicies AuditPolicies,
    ApprovalPolicies ApprovalPolicies);

public sealed record EnvironmentSpec(
    string Id,
    string Name,
    RolloutTier Tier,
    IReadOnlyList<string> DataSourceAliases,
    bool RequiresApproval = false,
    int RequiredApproverCount = 1,
    TimeSpan? CooldownBetweenRuns = null);

public sealed record ExpectedSourceSpec(
    string Alias,
    string Driver,                                         // "Beep.DataSource.SqlServer"
    DatasourceCategory Category,
    IReadOnlyDictionary<string, object?>? Policies = null); // free-form: { "pii": "Tag", "retention": { "days": 365 } }

public sealed record SchemaPolicies(
    bool RequireMigrationPlanHash = true,
    bool ForbidDestructiveInLive = true,
    bool RequirePreflightOnLive = true,
    IReadOnlyList<string> BlockedOperations = Array.Empty<string>());

public sealed record SyncPolicies(
    bool WatermarkRequired = true,
    string? ConflictResolutionRule = "sync.conflict.source-wins",
    int MaxRowsPerRun = 1_000_000,
    IReadOnlyList<string> BlockedSchemas = Array.Empty<string>());

public sealed record AuditPolicies(
    IReadOnlyList<string> Redact = Array.Empty<string>(),  // property names to redact
    int RetentionDays = 365,
    bool RequireHashChain = true);

public sealed record ApprovalPolicies(
    IReadOnlyList<string> DefaultApproverRoles = Array.Empty<string>(),
    bool RequirePlanHashMatch = true,
    TimeSpan? CooldownBetweenRuns = null);

public sealed record CodeRevisionRef(
    string Ref,                                            // "refs/heads/main"
    string Sha);                                           // full git SHA

public sealed record ManifestValidationReport(
    bool IsValid,
    IReadOnlyList<ManifestValidationIssue> Issues,
    DateTimeOffset ValidatedAt,
    string? ManifestSha256);                               // hash of the manifest bytes at validate time

public sealed record ManifestValidationIssue(
    string Code,
    string Path,                                           // JSON pointer into the manifest
    string Message,
    string Severity);                                      // "Info" | "Warn" | "Error"
```

### NEW (rev 2): `IDeploymentMetadataService` + `DeploymentMetadata`

```csharp
public interface IDeploymentMetadataService
{
    /// <summary>Resolve the deployment metadata for the current process.</summary>
    Task<StudioResult<DeploymentMetadata>> GetCurrentAsync(CancellationToken ct = default);

    /// <summary>Override the deployment metadata (used by tests).</summary>
    void Override(DeploymentMetadata? metadata);

    /// <summary>Issue an HMAC-signed approval token for a request + deployment pair.</summary>
    Task<StudioResult<string>> IssueApprovalTokenAsync(ApprovalTokenRequest request, CancellationToken ct = default);

    /// <summary>Verify an approval token against the current deployment metadata.</summary>
    Task<StudioResult<ApprovalTokenClaims>> VerifyApprovalTokenAsync(string token, CancellationToken ct = default);
}

public sealed record DeploymentMetadata(
    string CodeRevisionRef,                                // "refs/heads/main"
    string CodeRevisionSha,                                // full git SHA
    string? BuildId,                                       // CI build id
    string? BuildUrl,                                      // CI build URL
    string? Version,                                       // assembly version
    DateTimeOffset BuiltAt,
    IReadOnlyDictionary<string, string>? Labels);

public sealed record ApprovalTokenRequest(
    string ApprovalId,
    string PlanHash,
    RolloutTier Tier,
    DateTimeOffset IssuedAt,
    TimeSpan Lifetime);                                    // token validity

public sealed record ApprovalTokenClaims(
    string ApprovalId,
    string PlanHash,
    RolloutTier Tier,
    string CodeRevisionRef,
    string CodeRevisionSha,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);

public sealed record ApprovalToken(
    string Token,                                          // the HMAC-signed base64url string
    ApprovalTokenClaims Claims,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);
```

### Sub-service stub interfaces

The 8 sub-service interfaces (`IDriverService`, `ISourceService`, `ISchemaService`,
`IMigrationStudioService`, `ISyncStudioService`, `IGovernanceService`,
`IDataLifecycleManifestService`, `IDeploymentMetadataService`) are declared in this
phase as **stubs with the full method signature**, but their implementations are
the work of Phases 2-10. This lets the host project (Blazor, WinForms, WPF, Maui)
reference the contracts today and the engine team implement them phase by phase
without blocking UI work.

> **Removed in rev 2:** the `ILifecycleService` interface and the
> `ProjectScaffolder` stub. The Studio does not scaffold projects. If a host
> needs scaffolding, it should call into the IDE (Cursor) or a separate
> `dotnet-beepdms-scaffold` CLI tool, not the engine.

### DI extension

```csharp
// BeepServiceExtensions.Studio.cs
public static IServiceCollection AddBeepStudio(
    this IServiceCollection services,
    Action<StudioOptions>? configure = null)
{
    var options = new StudioOptions();
    configure?.Invoke(options);
    services.TryAddSingleton(options);
    services.TryAddSingleton<StudioPaths>();
    services.TryAddSingleton<IDeploymentMetadataService, DeploymentMetadataService>();   // NEW in rev 2
    services.TryAddSingleton<IStudioService, StudioService>();
    services.TryAddSingleton<IEnvironmentProfileService, EnvironmentProfileService>();
    services.TryAddSingleton<IDataLifecycleManifestService, DataLifecycleManifestService>();  // NEW in rev 2
    // Sub-services are registered in their own phases via TryAddSingleton.
    return services;
}
```

The pattern uses `TryAddSingleton` so hosts can override individual services for
testing or customization.

---

## Todo Tracker

| # | Task | Status | Notes |
|---|------|--------|-------|
| P01-01 | Create the 10 folders under `Services/Studio/` (drop `Lifecycle/`) | ⬜ | |
| P01-02 | `Contracts/IStudioService.cs` | ⬜ | Public surface per overview |
| P01-03 | `Contracts/IStudioProgress.cs` + `StudioProgressUpdate` record + enums | ⬜ | |
| P01-04 | `Contracts/IEnvironmentProfileService.cs` | ⬜ | |
| P01-05 | `Contracts/IDriverService.cs` — stubs (Phase 2 implements) | ⬜ | |
| P01-06 | `Contracts/ISourceService.cs` — stubs (Phase 3 implements) | ⬜ | |
| P01-07 | `Contracts/ISchemaService.cs` — stubs (Phase 4 implements) | ⬜ | |
| P01-08 | `Contracts/IMigrationStudioService.cs` — stubs (Phase 5 implements) | ⬜ | |
| P01-09 | `Contracts/ISyncStudioService.cs` — stubs (Phase 6 implements) | ⬜ | |
| P01-10 | `Contracts/IGovernanceService.cs` — stubs (Phase 7 implements) | ⬜ | |
| P01-11 | `Contracts/IDataLifecycleManifestService.cs` — stubs (Phase 9 implements) | ⬜ | NEW in rev 2 |
| P01-12 | `Contracts/IDeploymentMetadataService.cs` — stubs (Phase 10 implements) | ⬜ | NEW in rev 2 |
| P01-13 | `Models/StudioResult.cs` + `StudioError.cs` + `StudioErrorCode.cs` (add the 4 new codes from rev 2) | ⬜ | |
| P01-14 | `Models/StudioOptions.cs` + `StudioPersistenceMode` enum (add the 3 new fields from rev 2) | ⬜ | |
| P01-15 | `Models/StudioInfo.cs` (add `ManifestLoaded` + `ManifestVersion` from rev 2) | ⬜ | |
| P01-16 | `Models/StudioConstants.cs` — env names, default paths, well-known file names, default manifest filename | ⬜ | Add `DefaultManifestFileName = "data-lifecycle-manifest.json"` |
| P01-17 | `Models/EnvironmentProfile.cs` + `RolloutTier` enum | ⬜ | |
| P01-18 | `Models/DataLifecycleManifest.cs` + `ProjectRef.cs` + `DataLifecycleSpec.cs` + `EnvironmentSpec.cs` + `ExpectedSourceSpec.cs` + `SchemaPolicies.cs` + `SyncPolicies.cs` + `AuditPolicies.cs` + `ApprovalPolicies.cs` + `ManifestValidationReport.cs` | ⬜ | NEW in rev 2 |
| P01-19 | `Models/DeploymentMetadata.cs` + `CodeRevisionRef.cs` + `ApprovalToken.cs` + `ApprovalTokenRequest.cs` + `ApprovalTokenClaims.cs` | ⬜ | NEW in rev 2 |
| P01-20 | `BeepServiceExtensions.Studio.cs` — `AddBeepStudio()` | ⬜ | Updated to register manifest + deployment services |
| P01-21 | `StudioService.cs` — top-level facade impl; delegates to sub-services (or returns `StudioError.HostNotSupported` if a sub-service is not yet wired) | ⬜ | |
| P01-22 | `NullStudioService.cs` — no-op default for opt-out hosts | ⬜ | |
| P01-23 | Build: `dotnet build DataManagementEngineStandard` succeeds with **0 errors** | ⬜ | |

---

## Validation (definition of done)

- [ ] `dotnet build DataManagementEngineStandard` succeeds with **0 errors** (warnings tolerated, but no new ones).
- [ ] `IStudioService` exposes every method listed in the overview, including the 2 new `Manifest` + `Deployment` accessors.
- [ ] `StudioResult<T>` is a `readonly record struct` so it can be returned by value.
- [ ] `NullStudioService` returns `StudioResult<T>.Fail(StudioErrorCode.HostNotSupported, ...)` for every method.
- [ ] No `MudBlazor.*`, `System.Windows.*`, `System.Drawing.*`, or `Microsoft.Maui.*` reference in any new file.
- [ ] No `Microsoft.EntityFrameworkCore.*` reference in any new file.

---

## Pitfalls

1. **Don't make `IStudioService` a class** — keep it an interface so the host can mock it.
2. **Don't throw exceptions from mutation methods** — return `StudioResult.Fail`. Throw only for programmer errors (null arg, contract violation).
3. **Don't leak `IDMEEditor` types past the Studio boundary** — the host project should never import `TheTechIdea.Beep.Editor` directly. Use only `TheTechIdea.Beep.Studio.*` namespaces.
4. **Don't take a dependency on `MudBlazor` or any other UI toolkit** — the Studio is the platform-agnostic layer.
5. **Don't put platform-specific paths in `StudioOptions.DataRoot`** — derive them via `EnvironmentService` so the same code works on Windows, Linux, and macOS.
6. **Don't add `IProjectRegistry` back** (rev 2 reminder) — projects are not a Studio concern. The manifest is the project-level artefact; we read it but we don't register projects.
7. **Don't put deployment orchestration in `IDeploymentMetadataService`** — the service only **resolves** the metadata; it does not build, deploy, or push anything.

---

## Related

- Phase 00 — overview & scope (this phase implements the contracts sketched there)
- Phases 2-10 — implement the sub-service interfaces
- Phase 8 — adapters
- `.plans/phase-18.md` … `phase-24.md` in the Blazor workspace — these will be updated to point at the new contracts instead of duplicating logic
