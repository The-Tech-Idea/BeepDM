# Phase 00 — Overview & Scope: Beep Studio (data lifecycle + code lifecycle metadata)

> Single source of truth for the **Studio abstraction layer** that lives on top of
> the existing `Editor/`, `SetUp/`, and `Services/` folders in
> `DataManagementEngineStandard`. The Studio is the platform-agnostic orchestration
> layer for **data lifecycle** (source registry, schema migrations, data sync,
> governance) and for the **metadata that ties data lifecycle to code lifecycle**
> (a `DataLifecycleManifest` that lives in the project repo and is read at runtime
> + CI time).

> Cross-link every TODO to its phase doc. Mark `[x]` only after the verification criteria in the phase doc are met.

Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## Why this exists

The current BeepDM engine already has strong primitives for **data lifecycle**:

| Layer | What it does | Where |
|---|---|---|
| `Editor/DM/DMEEditor.cs` | Central orchestrator; `IDataSource` cache, config editor, logger, ETL | `DataManagementEngineStandard/Editor/DM/` |
| `Editor/Migration/MigrationManager.cs` | Schema plan → dry-run → preflight → apply → checkpoint → rollback | `DataManagementEngineStandard/Editor/Migration/` |
| `Editor/BeepSync/BeepSyncManager.Core.cs` | Data sync (full / incremental, one-way / bidirectional, watermarks, conflicts) | `DataManagementEngineStandard/Editor/BeepSync/` |
| `Editor/Defaults/DefaultsManager.cs` | Column defaults + rule engine | `DataManagementEngineStandard/Editor/Defaults/` |
| `Editor/Mapping/MappingManager.cs` | Field / entity mapping | `DataManagementEngineStandard/Editor/Mapping/` |
| `Editor/Schema/SchemaManager.cs` | Strict destination-acceptance preflight | `DataManagementEngineStandard/Editor/Schema/` |
| `Editor/EntityDiscovery/EntityDiscoveryService.cs` | CLR-side entity discovery | `DataManagementEngineStandard/Editor/EntityDiscovery/` |
| `SetUp/SetupWizard.cs` + `SetUp/Steps/*` | First-run wizard (DriverProvision → ConnectionConfig → Schema → Defaults → Seeding → DataImport) | `DataManagementEngineStandard/SetUp/` |
| `Services/Audit/IBeepAudit` | Cross-platform audit trail (hash chain, retention, redactors, file/SQLite sinks) | `DataManagementEngineStandard/Services/Audit/` |
| `Services/Logging/IBeepLog` | Cross-platform logging pipeline | `DataManagementEngineStandard/Services/Logging/` |
| `Services/EnvironmentService.cs` | Cross-platform data-folder helper (Windows / macOS / Linux) | `DataManagementEngineStandard/Services/EnvironmentService.cs` |
| `Services/DatasourceManagement/DatasourceManagementService.cs` | Status / health checks per data source | `DataManagementEngineStandard/Services/DatasourceManagement/` |
| `Services/BeepService.cs` + `BeepServiceExtensions.*` | The `IBeepService` container + per-platform extension methods | `DataManagementEngineStandard/Services/` |
| `Helpers/ConnectionHelpers/ConnectionHelper*` | Per-category connection-string parsers (RDBMS, NoSQL, File, WebAPI, …) | `DataManagementEngineStandard/Helpers/ConnectionHelpers/` |

**What's missing** for a **centralized, cross-project, multi-environment control plane**:

1. A single, high-level **Studio facade** that composes the above primitives into
   the full data-lifecycle workflow (registry → driver → source → schema →
   migration → sync → audit).
2. Platform-agnostic **view-models + adapters** so any UI shell (Blazor, WinForms,
   WPF, Maui) renders the same workflow without re-implementing it.
3. A **`DataLifecycleManifest`** (the new piece) — a JSON/YAML file in the project
   repo that declares: "this code revision expects these data sources, with these
   schemas, with this approval tier, with this retention policy." The Studio reads
   it on startup, enforces it at runtime, and validates it in CI. This is the
   **link between data lifecycle and code lifecycle**.
4. **App-deployment-awareness metadata** (the second new piece) — properties,
   classes, and manifest fields that let the Studio know which **app build / code
   revision** a migration or sync is running against, so audit trails and approval
   tokens are bound to the right code. We do **not** orchestrate deployments;
   we just record the metadata that lets a deployment system (or a DBA) correlate
   a data change to a code change.

## Goals

1. **Data lifecycle in one orchestration layer.** A DBA or data engineer can:
   register a source → discover its schema → build a migration plan → apply it →
   audit it. All from one engine API.

2. **Platform-agnostic.** The Studio's public surface is `IDataSource`-aware, not
   `MudBlazor`-aware. The same `IMigrationStudioService.BuildPlanAsync(...)` call
   works from a Blazor Server tab, a WinForms dialog, a WPF view model, a Maui
   mobile page, or a CLI.

3. **Audit-by-default.** Every mutation in the Studio records an `IBeepAudit`
   event with the actor, plan-hash, code-revision (from the manifest), before/after
   diff, and tier. The existing `IBeepAudit` pipeline (hash-chained, tamper-evident,
   NDJSON or SQLite sink) is the transport — the Studio just calls it.

4. **Code-lifecycle aware.** Every migration / sync / approval / audit event
   carries the **code revision** it was triggered against. The source of truth is
   the project's `DataLifecycleManifest.json` (or `.yaml`). The manifest is read
   on Studio startup, validated on every apply, and required for every approval.

5. **No EF Core.** The Studio orchestrates `IMigrationManager` and
   `BeepSyncManager` only. EF Core is an **adapter** concern (e.g. scanning a
   `DbContext` for entity types in Phase 4) and never leaks into the engine core.

6. **Reuse, don't replace.** The Studio wraps `IMigrationManager`,
   `BeepSyncManager`, `IDefaultsManager`, `IMappingManager`, `ISchemaManager`,
   `EntityDiscoveryService`, `IBeepAudit`, `IBeepLog`, `DatasourceManagementService`,
   `EnvironmentService`, `ConnectionHelper_*`, `SetupWizard` steps, and the
   `SetUp/Adapters/*`. It does **not** fork any of them.

## Non-goals (explicitly out of scope)

- **Application / code deployment orchestration.** The Studio does not run
  `dotnet publish`, does not build Docker images, does not deploy to Kubernetes,
  does not push to App Service, does not orchestrate CI pipelines. Those are
  separate concerns owned by the project's release system. The Studio **records**
  the code revision (git SHA, build id, version) that a data change was triggered
  against — it does **not** trigger the build.
- **Project scaffolding.** The Studio does not generate `.csproj`, `Program.cs`,
  `appsettings.json`, `Dockerfile`, or any other project file. The host project
  is the responsibility of the developer and the IDE (Cursor / VS / Rider).
- **Code / source file editing.** The Studio does not write to a project's
  `.cs` files. It reads them (for entity discovery) and reads the manifest, but
  it never modifies them.
- **Remote web service / API.** The Studio is a **local in-process** orchestration
  layer. A host may choose to expose the Studio as an API (the Blazor workspace's
  `BeepDMS.Api` project does this), but that is the host's concern — the engine
  is in-process.
- **Migration / sync between two **different** IDMEEditor instances.** The
  Studio operates on a single `IBeepService` per process. Multi-process sync
  is a future concern.

## Folder layout (the new code lives here)

```
DataManagementEngineStandard/
└── Services/
    └── Studio/                                ← NEW — the Studio abstraction layer
        ├── Contracts/                         ← public interfaces (consumed by every UI host)
        │   ├── IStudioService.cs              ← top-level facade
        │   ├── IStudioProgress.cs             ← platform-agnostic progress reporter
        │   ├── IStudioHostAdapter.cs          ← Phase 8 stub interface
        │   ├── IEnvironmentProfileService.cs
        │   ├── IDriverService.cs
        │   ├── ISourceService.cs
        │   ├── ISchemaService.cs
        │   ├── IMigrationStudioService.cs
        │   ├── ISyncStudioService.cs
        │   ├── IGovernanceService.cs
        │   ├── IDataLifecycleManifestService.cs   ← Phase 9 — the new piece
        │   └── IDeploymentMetadataService.cs      ← Phase 10 — app-deployment awareness
        ├── Models/                            ← POCOs / DTOs / view-models
        │   ├── EnvironmentProfile.cs
        │   ├── DriverInfo.cs
        │   ├── SourceBinding.cs
        │   ├── EntityDescriptor.cs
        │   ├── MigrationRequest.cs
        │   ├── MigrationPlanVm.cs
        │   ├── DdlOperationVm.cs
        │   ├── SyncSchemaVm.cs
        │   ├── ConflictEvidenceVm.cs
        │   ├── GovernancePolicy.cs
        │   ├── ApprovalRequest.cs
        │   ├── DataLifecycleManifest.cs          ← Phase 9 — the new piece
        │   ├── DataLifecycleManifestEntry.cs
        │   ├── DeploymentMetadata.cs             ← Phase 10 — the new piece
        │   ├── CodeRevisionRef.cs
        │   ├── StudioResult.cs                  ← Result<T> for engine calls
        │   ├── StudioOptions.cs                 ← DI options POCO
        │   └── StudioConstants.cs
        ├── Manifest/                           ← Phase 9 — the manifest reader/writer
        │   ├── DataLifecycleManifestService.cs
        │   ├── ManifestReader.cs                ← JSON + YAML
        │   ├── ManifestWriter.cs
        │   ├── ManifestValidator.cs
        │   └── ManifestSchema.cs
        ├── Deployment/                         ← Phase 10 — code-revision + build-id metadata
        │   ├── DeploymentMetadataService.cs
        │   ├── CodeRevisionResolver.cs          ← git rev-parse, build id from CI
        │   ├── DeploymentMetadataEnricher.cs    ← IEnricher for IBeepAudit
        │   └── ApprovalTokenIssuer.cs           ← HMAC-signed approval tokens
        ├── Driver/                            ← Phase 2 — driver provisioning (wraps SetUp/Steps/DriverProvisionStep)
        ├── Source/                            ← Phase 3 — connection configuration (wraps ConnectionHelper_*)
        ├── Schema/                            ← Phase 4 — entity discovery (wraps EntityDiscoveryService)
        ├── Migration/                         ← Phase 5 — migration orchestration (wraps IMigrationManager)
        ├── Sync/                              ← Phase 6 — data sync orchestration (wraps BeepSyncManager)
        ├── Governance/                        ← Phase 7 — policies / approvals (wraps IBeepAudit)
        ├── Adapters/                          ← Phase 8 — per-platform adapters (wraps SetUp/Adapters/*)
        ├── BeepServiceExtensions.Studio.cs   ← `AddBeepStudio()` DI helper
        ├── StudioService.cs                   ← top-level facade implementation
        ├── NullStudioService.cs               ← no-op default for opt-out hosts
        └── Plans/                             ← the engine-team-style phase docs
            ├── MASTER-TODO-TRACKER.md
            ├── 00-overview-and-scope.md       ← this file
            ├── 01-phase1-core-contracts-and-options.md
            ├── 02-phase2-driver-provisioning.md
            ├── 03-phase3-connection-configuration.md
            ├── 04-phase4-schema-discovery-and-ef-interop.md
            ├── 05-phase5-migration-orchestration.md
            ├── 06-phase6-data-sync-orchestration.md
            ├── 07-phase7-governance-policies-approvals.md
            ├── 08-phase8-platform-adapters-and-ui-bridges.md
            ├── 09-phase9-data-lifecycle-manifest.md   ← NEW
            ├── 10-phase10-deployment-metadata.md      ← NEW
            ├── CHANGELOG.md
            └── RUNBOOK.md
```

## Public surface (the `IStudioService`)

```csharp
namespace TheTechIdea.Beep.Studio.Contracts;

/// <summary>
/// Top-level facade for the Beep Studio. Composes every lower-level
/// service (driver, source, schema, migration, sync, governance,
/// manifest, deployment-metadata) behind a single async, platform-agnostic
/// API. UI hosts (Blazor, WinForms, WPF, Maui) call into this; engine
/// primitives (IMigrationManager, BeepSyncManager, IBeepAudit, IDMEEditor)
/// are never touched directly from the host.
/// </summary>
public interface IStudioService
{
    // ---- metadata ----
    StudioInfo GetInfo();
    Task<StudioResult<StudioInfo>> GetInfoAsync(CancellationToken ct = default);

    // ---- environment profiles (Dev / Test / Staging / Live) ----
    IEnvironmentProfileService Environments { get; }
    Task<StudioResult<IReadOnlyList<EnvironmentProfile>>> ListEnvironmentsAsync(CancellationToken ct = default);
    Task<StudioResult<EnvironmentProfile>> SaveEnvironmentAsync(EnvironmentProfile profile, CancellationToken ct = default);
    Task<StudioResult<bool>> DeleteEnvironmentAsync(string environmentId, CancellationToken ct = default);

    // ---- driver provisioning ----
    IDriverService Drivers { get; }
    Task<StudioResult<DriverInfo>> ProvisionDriverAsync(DriverProvisionRequest request, IStudioProgress? progress = null, CancellationToken ct = default);

    // ---- source (connection) configuration ----
    ISourceService Sources { get; }
    Task<StudioResult<SourceBinding>> ConfigureSourceAsync(SourceConfigurationRequest request, CancellationToken ct = default);
    Task<StudioResult<SourceTestResult>> TestSourceAsync(string sourceName, CancellationToken ct = default);

    // ---- schema discovery + design ----
    ISchemaService Schemas { get; }
    Task<StudioResult<IReadOnlyList<EntityDescriptor>>> DiscoverEntitiesAsync(EntityDiscoveryRequest request, CancellationToken ct = default);

    // ---- migration orchestration ----
    IMigrationStudioService Migrations { get; }
    Task<StudioResult<MigrationPlanVm>> BuildPlanAsync(MigrationRequest request, IStudioProgress? progress = null, CancellationToken ct = default);
    Task<StudioResult<MigrationExecutionHandle>> ApplyPlanAsync(MigrationPlanHandle planHandle, MigrationExecutionPolicy policy, IStudioProgress? progress = null, CancellationToken ct = default);

    // ---- sync orchestration ----
    ISyncStudioService Sync { get; }
    Task<StudioResult<SyncSchemaVm>> SaveSyncSchemaAsync(SyncSchemaVm schema, CancellationToken ct = default);
    Task<StudioResult<SyncRunHandle>> EnqueueSyncAsync(string schemaId, IStudioProgress? progress = null, CancellationToken ct = default);

    // ---- governance ----
    IGovernanceService Governance { get; }
    Task<StudioResult<ApprovalRequest>> RequestApprovalAsync(ApprovalRequest request, CancellationToken ct = default);
    Task<StudioResult<ApprovalRequest>> DecideApprovalAsync(string approvalId, ApprovalDecision decision, string decider, string? comment, CancellationToken ct = default);

    // ---- data lifecycle manifest (Phase 9) ----
    IDataLifecycleManifestService Manifest { get; }
    Task<StudioResult<DataLifecycleManifest>> LoadManifestAsync(string manifestPath, CancellationToken ct = default);
    Task<StudioResult<ManifestValidationReport>> ValidateManifestAsync(DataLifecycleManifest manifest, CancellationToken ct = default);

    // ---- deployment metadata (Phase 10) ----
    IDeploymentMetadataService Deployment { get; }
    Task<StudioResult<DeploymentMetadata>> GetCurrentDeploymentMetadataAsync(CancellationToken ct = default);
    Task<StudioResult<string>> IssueApprovalTokenAsync(ApprovalRequest request, DeploymentMetadata deployment, CancellationToken ct = default);

    // ---- audit ----
    Task<StudioResult<IReadOnlyList<AuditEventView>>> QueryAuditAsync(AuditQuery query, CancellationToken ct = default);
}
```

Every method:
- Returns `StudioResult<T>` (success/failure with error code) — never throws for business errors.
- Accepts an optional `IStudioProgress` callback (platform-agnostic — see Phase 1).
- Accepts a `CancellationToken`.
- Records an `IBeepAudit` event on success and on failure (Phase 7), enriched
  with the **deployment metadata** (Phase 10) so every event is bound to a code
  revision.

## The data↔code lifecycle link (Phases 9 + 10 — the new pieces)

### `DataLifecycleManifest` (Phase 9)

A JSON or YAML file in the project repo, typically at the repo root or in a
`beep/` subfolder. Example:

```json
{
  "$schema": "https://beep.thetechidea.com/schemas/data-lifecycle-manifest/v1.json",
  "manifestVersion": 1,
  "owner": "the-tech-idea",
  "project": {
    "name": "TheTechIdeaWeb.ApiService",
    "type": "DotnetWebApi",
    "repository": "https://github.com/fahadTheTechIdea/MyWebSite",
    "codeRevision": {
      "ref": "refs/heads/main",
      "sha": "9f3a4b1c8d2e5f7a6b3c9d0e1f2a3b4c5d6e7f8a"
    }
  },
  "dataLifecycle": {
    "owner": "data-platform@thetechidea.com",
    "tier": "Standard",
    "environments": [
      { "id": "dev",    "tier": "Dev",     "dataSourceAliases": ["BeepMain_Dev", "BeepAudit_Dev"] },
      { "id": "test",   "tier": "Test",    "dataSourceAliases": ["BeepMain_Test"] },
      { "id": "staging","tier": "Staging", "dataSourceAliases": ["BeepMain_Staging"], "requiresApproval": true },
      { "id": "live",   "tier": "Live",    "dataSourceAliases": ["BeepMain_Live", "BeepAudit_Live"], "requiresApproval": true, "requiredApproverCount": 2 }
    ],
    "expectedSources": [
      { "alias": "BeepMain_Dev",  "driver": "Beep.DataSource.SqlServer", "category": "RDBMS", "policies": { "pii": "Tag", "encryptionAtRest": "Required" } },
      { "alias": "BeepAudit_Dev", "driver": "Beep.DataSource.SqlServer", "category": "RDBMS", "policies": { "retention": { "days": 365, "afterAction": "Archive" } } }
    ],
    "schemaPolicies": {
      "requireMigrationPlanHash": true,
      "forbidDestructiveInLive":  true,
      "requirePreflightOnLive":   true
    },
    "syncPolicies": {
      "watermarkRequired":      true,
      "conflictResolutionRule": "sync.conflict.source-wins",
      "maxRowsPerRun":          1000000
    },
    "auditPolicies": {
      "redact": ["password", "apikey", "oauthaccesstoken"],
      "retentionDays": 365,
      "requireHashChain": true
    },
    "approvalPolicies": {
      "defaultApproverRoles": ["DBA", "Architect"],
      "requirePlanHashMatch": true,
      "cooldownBetweenRuns":  "00:05:00"
    }
  }
}
```

The manifest is:
- **Authored by the data-platform team** and committed to the repo.
- **Read on Studio startup** — `StudioService.LoadManifestAsync(manifestPath)`.
- **Validated** against the live state — every `ApplyAsync` / `RunAsync` is
  blocked if the manifest forbids the operation in the target env.
- **CI-required** — `beepdms manifest validate --path ./beep/data-lifecycle-manifest.json`
  is a pass/fail gate.
- **Versioned** — the `manifestVersion` field lets the Studio refuse to load
  manifests it doesn't understand.

### `DeploymentMetadata` (Phase 10)

Small POCO + service that resolves the **code revision** the Studio is running
against, at runtime:

```csharp
public sealed record DeploymentMetadata(
    string CodeRevisionRef,                              // "refs/heads/main", "refs/tags/v1.2.3", or "ci/<build-id>"
    string CodeRevisionSha,                              // full git SHA
    string? BuildId,                                     // CI build id
    string? BuildUrl,                                    // CI build URL
    string? Version,                                     // assembly version (from <Version> in csproj)
    DateTimeOffset BuiltAt,
    IReadOnlyDictionary<string, string>? Labels);        // free-form CI labels (commit author, branch, PR, etc.)

public interface IDeploymentMetadataService
{
    Task<StudioResult<DeploymentMetadata>> GetCurrentDeploymentMetadataAsync(CancellationToken ct = default);
}
```

The default resolver tries, in order:
1. `BEEP_DEPLOYMENT_METADATA_JSON` env var (the CI sets it).
2. `BEEP_CODE_REVISION_SHA` + `BEEP_CODE_REVISION_REF` env vars.
3. The manifest's `project.codeRevision` block.
4. `git rev-parse HEAD` + `git symbolic-ref HEAD` (in dev).
5. Assembly `InformationalVersion` + build timestamp.

Every audit event in `IBeepAudit` is enriched with this metadata (via the
`DeploymentMetadataEnricher` registered in Phase 7). Every approval token
is HMAC-signed with a hash of the deployment metadata (via the
`ApprovalTokenIssuer` registered in Phase 10) so an approval granted for
revision A cannot be replayed against revision B.

## Phases (the order matters)

| Phase | Title | Doc |
|---|---|---|
| **00** | Overview & scope | [00-overview-and-scope.md](./00-overview-and-scope.md) ← this file |
| **01** | Core contracts & options | [01-phase1-core-contracts-and-options.md](./01-phase1-core-contracts-and-options.md) |
| **02** | Driver provisioning | [02-phase2-driver-provisioning.md](./02-phase2-driver-provisioning.md) |
| **03** | Connection configuration | [03-phase3-connection-configuration.md](./03-phase3-connection-configuration.md) |
| **04** | Schema discovery + EF Core interop | [04-phase4-schema-discovery-and-ef-interop.md](./04-phase4-schema-discovery-and-ef-interop.md) |
| **05** | Migration orchestration | [05-phase5-migration-orchestration.md](./05-phase5-migration-orchestration.md) |
| **06** | Data sync orchestration | [06-phase6-data-sync-orchestration.md](./06-phase6-data-sync-orchestration.md) |
| **07** | Governance, approvals, audit | [07-phase7-governance-policies-approvals.md](./07-phase7-governance-policies-approvals.md) |
| **08** | Platform adapters + UI bridges | [08-phase8-platform-adapters-and-ui-bridges.md](./08-phase8-platform-adapters-and-ui-bridges.md) |
| **09** | **Data lifecycle manifest** | [09-phase9-data-lifecycle-manifest.md](./09-phase9-data-lifecycle-manifest.md) |
| **10** | **Deployment metadata + approval tokens** | [10-phase10-deployment-metadata.md](./10-phase10-deployment-metadata.md) |

## Cross-references

- The Blazor UI work in `C:\Users\f_ald\source\repos\The-Tech-Idea\BeepWeb\.plans\phase-18.md` …
  `phase-24.md` is the **host project** that consumes the Studio. It will be updated
  in a follow-up commit to point at the new engine code.
- The engine team's existing plans at
  `C:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\DataManagementEngineStandard\Services\Plans\`
  cover the Logging & Audit feature (phases 1-13). The Studio **consumes** that work —
  it does not duplicate it. Phase 7 (governance) and Phase 10 (deployment metadata)
  add audit enrichers that plug into the existing `IBeepAudit` pipeline.

---

## P00-01 … P00-04

- [x] P00-01 Inventory all existing engine touchpoints that the Studio must compose
      (`DMEEditor`, `IMigrationManager`, `BeepSyncManager`, `IDefaultsManager`,
      `IMappingManager`, `ISchemaManager`, `EntityDiscoveryService`, `IConfigEditor`,
      `IBeepAudit`, `IBeepLog`, `EnvironmentService`, `DatasourceManagementService`,
      `IAssemblyHandler`, `SetupWizard` + steps, `ConnectionHelper_*`, `SetUp/Adapters/*`).
- [x] P00-02 Inventory the engine team's existing plans to avoid overlap
      (`Services/Plans/` — Phases 1-13 of Logging & Audit; `Editor/README.md`; `SetUp/README.md`).
- [x] P00-03 Produce folder layout and public-surface sketch (above).
- [x] P00-04 Define non-goals (above) and v1 scope freeze (Phases 0-10, in this order).
- [x] P00-05 (rev 2) Rephrase scope: data lifecycle + code-lifecycle metadata, NOT project
      scaffolding, NOT application deployment orchestration.
- [x] P00-06 (rev 2) Add `DataLifecycleManifest` (Phase 9) and `DeploymentMetadata` (Phase 10)
      as the new pieces that tie data lifecycle to code lifecycle.

> Phase 00 build status: documentation + folder scaffolding only. No code yet.
