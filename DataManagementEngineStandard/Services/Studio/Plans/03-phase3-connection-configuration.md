# Phase 04 — Connection Configuration (`ISourceService`)

> **Scope:** implement `ISourceService` — the Studio's source-configuration
> sub-service. A **source** is a named `ConnectionProperties` entry (see
> `DataManagementModelsStandard/ConfigUtil/ConnectionProperties.cs:17`) plus
> the Studio's metadata: which env profiles it belongs to, which projects
> consume it, when it was last tested, who owns it. This is the **central
> registry** that every other phase reads from and writes to.

> Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## Why this phase

The current state at
`C:\Users\f_ald\source\repos\fahadTheTechIdea\MyWebSite\TheTechIdeaWeb` is six
apps with six different `appsettings.json` files, each independently managing
its own connection strings. There is no shared registry. The Studio fixes
this by being **the only** place where a connection is created, edited, tested,
browsed, and bound to a project + environment.

The same registry will be **pushed** to the six apps in Phase 24 of the
Blazor workspace plan (`.plans/phase-24.md`); for now we just build the
registry + the CRUD + the test/browse operations.

## Public surface (this phase fills in)

```csharp
// Contracts/ISourceService.cs
public interface ISourceService
{
    Task<StudioResult<IReadOnlyList<SourceInfo>>> ListAsync(SourceListFilter? filter = null, CancellationToken ct = default);
    Task<StudioResult<SourceInfo>> GetAsync(string sourceName, CancellationToken ct = default);
    Task<StudioResult<SourceInfo>> SaveAsync(SourceConfigurationRequest request, CancellationToken ct = default);
    Task<StudioResult<bool>> DeleteAsync(string sourceName, CancellationToken ct = default);
    Task<StudioResult<SourceTestResult>> TestAsync(string sourceName, CancellationToken ct = default);
    Task<StudioResult<IReadOnlyList<EntityDescriptor>>> BrowseAsync(string sourceName, string? entityName = null, int sampleRows = 0, CancellationToken ct = default);
}

// Models
public sealed record SourceListFilter(
    string? EnvironmentId = null,
    string? ProjectId = null,
    string? OwnerId = null,
    string? SearchText = null,
    int Skip = 0,
    int Take = 100);

public sealed record SourceInfo(
    string Name,                                         // unique within the Studio
    string OwnerId,                                       // who created it
    IReadOnlyList<string> Tags,
    string? Documentation,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastTestedAt,
    SourceTestResult? LastTestResult,
    ConnectionProperties Properties);                   // the actual connection

public sealed record SourceConfigurationRequest(
    string Name,
    ConnectionProperties Properties,                      // full connection — host has already redacted secrets before calling
    string? OwnerId = null,
    IReadOnlyList<string>? Tags = null,
    string? Documentation = null,
    string? EnvironmentId = null,                         // optional: bind to one env on save
    string? ProjectId = null);                            // optional: bind to one project on save

public sealed record SourceTestResult(
    bool Success,
    int LatencyMs,
    string? ServerVersion,
    int? CatalogCount,
    int? EntityCount,
    string? ErrorMessage,
    IReadOnlyList<string>? Warnings);
```

## Folder layout (this phase creates)

```
Services/Studio/
├── Contracts/ISourceService.cs                       ← DONE in Phase 1
├── Models/
│   ├── SourceInfo.cs
│   ├── SourceConfigurationRequest.cs
│   ├── SourceTestResult.cs
│   ├── SourceListFilter.cs
│   ├── SourceBinding.cs                               ← DONE in Phase 1
│   └── ConnectionSecrets.cs                           ← secret-handling contract
├── Source/
│   ├── SourceService.cs                               ← implements ISourceService
│   ├── SourceRegistry.cs                              ← in-memory + on-disk registry
│   ├── ConnectionPropertyMapper.cs                    ← DataSourceType → UI fields
│   ├── SourceValidator.cs                             ← pre-save validation
│   ├── SourceHealthChecker.cs                         ← wraps DatasourceManagementService
│   ├── SchemaBrowser.cs                               ← wraps IDatasourceEntities / GetEntity
│   └── SecretRedactor.cs                              ← redacts Password / ApiKey / etc. before persistence
└── Keychain/
    ├── IKeychainProvider.cs                            ← DPAPI / libsecret / Keychain abstraction
    ├── WindowsKeychainProvider.cs
    ├── LinuxKeychainProvider.cs
    └── MacKeychainProvider.cs
```

## The keychain abstraction

The Studio **never** stores secrets (passwords, API keys, OAuth tokens) in
JSON files. It stores them in a per-platform keychain:

| Platform | Provider | Notes |
|---|---|---|
| Windows | DPAPI (`ProtectedData.Protect`) | The existing pattern in `Beep.EventsRegistration/Services/Setup/IPlatformSettingProtectionService`. |
| Linux | `libsecret` via `SecretService` D-Bus | Falls back to AES-encrypted file under `~/.config/TheTechIdea/Beep/Studio/secrets.bin` if libsecret is unavailable. |
| macOS | Keychain (`Security.framework`) | Same fallback as Linux. |

`IKeychainProvider`:

```csharp
public interface IKeychainProvider
{
    Task<StudioResult<bool>> SetSecretAsync(string key, string value, CancellationToken ct = default);
    Task<StudioResult<string?>> GetSecretAsync(string key, CancellationToken ct = default);
    Task<StudioResult<bool>> DeleteSecretAsync(string key, CancellationToken ct = default);
    string PlatformName { get; }                        // "Windows DPAPI" / "Linux libsecret" / "macOS Keychain"
}
```

The Studio's `SourceService.SaveAsync` extracts the `Password`, `ApiKey`, `OAuthAccessToken`,
`ClientSecret`, and any other property annotated `[BeepSecret]` in `ConnectionProperties`,
stores them in the keychain under `<sourceName>:<propertyName>`, and stores a
**reference** (`"keychain:SourceName:Password"`) in the JSON registry. The secret
itself never touches disk in clear text.

## Source registry persistence

`%DataRoot%/source-registry.json` — the canonical store:

```json
{
  "version": 1,
  "sources": [
    {
      "name": "BeepMain_Dev",
      "ownerId": "fahad",
      "tags": ["primary", "domain"],
      "documentation": "Main dev database for TheTechIdeaWeb.ApiService",
      "createdAt": "2026-06-11T08:00:00Z",
      "updatedAt": "2026-06-11T08:30:00Z",
      "lastTestedAt": "2026-06-11T08:35:00Z",
      "lastTestResult": { "success": true, "latencyMs": 12, "serverVersion": "16.0.1000", "catalogCount": 1, "entityCount": 187 },
      "properties": {
        "databaseType": "SqlServer",
        "category": "RDBMS",
        "host": "dev-sql.internal",
        "port": 1433,
        "database": "BeepMain_Dev",
        "userId": "beep_app",
        "passwordRef": "keychain:BeepMain_Dev:Password"
      },
      "bindings": [
        { "environmentId": "dev", "projectId": "TheTechIdeaWeb.ApiService" }
      ]
    }
  ]
}
```

## Health check

`SourceService.TestAsync` calls into the engine's existing
`DatasourceManagementService.GetDatasourceStatus` (defined in
`DataManagementEngineStandard/Services/DatasourceManagement/DatasourceManagementService.cs`).
It wraps the result into a `SourceTestResult` and **also** updates the registry's
`lastTestedAt` + `lastTestResult` fields.

## Schema browser

`SourceService.BrowseAsync` calls into `IDataSource.GetEntityStructure(name)` for
each entity in the source, then for the named entity (or the first if `entityName`
is null) calls `IDataSource.GetEntity(...)` to fetch up to `sampleRows` rows. Returns
an `IReadOnlyList<EntityDescriptor>` with the column metadata + the sample rows.

This is the API the Blazor `SourcesTab` calls from Phase 19 of the Blazor workspace
plan. The engine returns the data; the host renders it.

## Cross-cutting

- Every `SaveAsync` / `DeleteAsync` records an `IBeepAudit` event (wired in Phase 8).
- The `SourceRegistry` is safe for concurrent reads (multiple Blazor circuits
  reading the same registry) but writes go through a `SemaphoreSlim` to prevent
  lost updates.
- The registry's `bindings` field is the source of truth for which projects use
  which source in which environment. The `IProjectRegistry` (Phase 2) reads from
  this when computing push previews.

---

## Todo Tracker

| # | Task | Status | Notes |
|---|------|--------|-------|
| P04-01 | `Models/SourceInfo.cs` + `SourceConfigurationRequest.cs` + `SourceTestResult.cs` + `SourceListFilter.cs` | ⬜ | |
| P04-02 | `Models/ConnectionSecrets.cs` — annotation list of secret properties on `ConnectionProperties` | ⬜ | |
| P04-03 | `Keychain/IKeychainProvider.cs` | ⬜ | |
| P04-04 | `Keychain/WindowsKeychainProvider.cs` (DPAPI) | ⬜ | |
| P04-05 | `Keychain/LinuxKeychainProvider.cs` (libsecret + fallback) | ⬜ | |
| P04-06 | `Keychain/MacKeychainProvider.cs` (Keychain + fallback) | ⬜ | |
| P04-07 | `Keychain/KeychainFactory.cs` — picks the right provider per OS | ⬜ | |
| P04-08 | `Source/SecretRedactor.cs` — extracts secrets, stores in keychain, replaces with refs | ⬜ | |
| P04-09 | `Source/SourceRegistry.cs` — JSON read/write of `source-registry.json` | ⬜ | |
| P04-10 | `Source/ConnectionPropertyMapper.cs` — maps `DataSourceType` → UI field list (used by the host) | ⬜ | |
| P04-11 | `Source/SourceValidator.cs` — pre-save validation: required fields, port range, hostname | ⬜ | |
| P04-12 | `Source/SourceHealthChecker.cs` — wraps `DatasourceManagementService.GetDatasourceStatus` | ⬜ | |
| P04-13 | `Source/SchemaBrowser.cs` — wraps `IDataSource.GetEntityStructure` + `GetEntity` | ⬜ | |
| P04-14 | `Source/SourceService.cs` — implements `ISourceService` | ⬜ | |
| P04-15 | Wire `ISourceService` into `AddBeepStudio()` | ⬜ | |
| P04-16 | Tests: `SecretRedactorTests` (3+), `SourceRegistryTests` (3+), `SourceServiceTests` (4+), `KeychainProviderTests` (3+, mocked) | ⬜ | |
| P04-17 | Update `00-overview-and-scope.md` + `MASTER-TODO-TRACKER.md` to mark Phase 04 done | ⬜ | |

---

## Validation (definition of done)

- [ ] `dotnet build DataManagementEngineStandard` succeeds with **0 errors**.
- [ ] `SourceService.SaveAsync` on a SQL Server connection stores the password in the keychain and the `passwordRef` in the JSON.
- [ ] `SourceService.TestAsync` on a valid SQL Server returns `Success = true` with a `LatencyMs`.
- [ ] `SourceService.TestAsync` on a broken connection returns `Success = false` with the right `ErrorMessage`.
- [ ] `SourceService.BrowseAsync` on a connection with 5 entities returns 5 `EntityDescriptor` records with column metadata.
- [ ] `KeychainProvider` is the platform-appropriate implementation (Windows/Linux/macOS).
- [ ] All 13+ new tests pass.

---

## Pitfalls

1. **Don't store clear-text secrets in `source-registry.json`** — even during tests, use the keychain (a test-only `InMemoryKeychainProvider` is fine for unit tests).
2. **Don't call `IDataSource.Openconnection` from a Blazor circuit** — the connection test should be quick (< 5s) but still off the circuit thread. Use the `IStudioProgress` pattern from Phase 1.
3. **Don't bypass `DatasourceManagementService`** — it's the canonical health check. Wrap, don't replace.
4. **Don't assume every source has a database** — file, WebAPI, NoSQL, in-memory sources have different "test" semantics. The health check must handle each category.
5. **Don't mutate `ConnectionProperties` in place** — they're shared with the engine. Always copy via `MemberwiseClone` or a record-with syntax.
6. **Don't break existing appsettings** — the Studio's source registry is a **new** mechanism. The old `appsettings.json` files keep working until Phase 24 of the Blazor workspace plan migrates them.

---

## Related

- Phase 01 — contracts (this phase implements `ISourceService`)
- Phase 03 — driver provisioning (the driver picker is populated from the driver catalog)
- Phase 05 — schema discovery (re-uses `SchemaBrowser` for entity metadata)
- Phase 08 — governance (every `SaveAsync` / `DeleteAsync` is audited)
- `.plans/phase-19.md` — the Blazor `SourcesTab` that consumes this API
- `BeepDM/DataManagementEngineStandard/Services/DatasourceManagement/DatasourceManagementService.cs` — the existing health checker we wrap
- `BeepDM/DataManagementModelsStandard/ConfigUtil/ConnectionProperties.cs:17` — the connection POCO
