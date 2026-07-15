# MASTER-TODO-TRACKER — Beep Studio (engine abstraction layer)

> Single source of truth for the `DataManagementEngineStandard/Services/Studio/` work.
> The Studio is the platform-agnostic orchestration layer for **data lifecycle**
> (source registry, schema migrations, data sync, governance) and for the
> **metadata that ties data lifecycle to code lifecycle** (a
> `DataLifecycleManifest` in the project repo + `DeploymentMetadata` resolved at
> runtime).
>
> Cross-link every TODO to its phase doc. Mark `[x]` only after the verification
> criteria in the phase doc are met.

Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## Phase 00 — Overview & Scope · [`00-overview-and-scope.md`](./00-overview-and-scope.md)

- [x] P00-01 Inventory all engine touchpoints the Studio composes.
- [x] P00-02 Inventory engine-team plans to avoid overlap.
- [x] P00-03 Folder layout + public-surface sketch.
- [x] P00-04 Non-goals + v1 scope freeze.
- [x] P00-05 **(rev 2)** Rephrase scope: data lifecycle + code-lifecycle metadata, **not** project scaffolding, **not** deployment orchestration.
- [x] P00-06 **(rev 2)** Add `DataLifecycleManifest` (Phase 9) and `DeploymentMetadata` (Phase 10) as the new pieces that tie data lifecycle to code lifecycle.

> Phase 00 status: doc + folder scaffolding only. No code yet.

---

## Phase 01 — Core Contracts & Options · [`01-phase1-core-contracts-and-options.md`](./01-phase1-core-contracts-and-options.md)

- [ ] P01-01 Create the 10 folders under `Services/Studio/` (drop `Lifecycle/`).
- [ ] P01-02 `Contracts/IStudioService.cs` — top-level facade (incl. `Manifest` + `Deployment` accessors).
- [ ] P01-03 `Contracts/IStudioProgress.cs` + `StudioProgressUpdate` record + enums.
- [ ] P01-04 `Contracts/IEnvironmentProfileService.cs`.
- [ ] P01-05 `Contracts/IDriverService.cs` — stubs (Phase 2 implements).
- [ ] P01-06 `Contracts/ISourceService.cs` — stubs (Phase 3 implements).
- [ ] P01-07 `Contracts/ISchemaService.cs` — stubs (Phase 4 implements).
- [ ] P01-08 `Contracts/IMigrationStudioService.cs` — stubs (Phase 5 implements).
- [ ] P01-09 `Contracts/ISyncStudioService.cs` — stubs (Phase 6 implements).
- [ ] P01-10 `Contracts/IGovernanceService.cs` — stubs (Phase 7 implements).
- [ ] P01-11 `Contracts/IDataLifecycleManifestService.cs` — stubs (Phase 9 implements).
- [ ] P01-12 `Contracts/IDeploymentMetadataService.cs` — stubs (Phase 10 implements).
- [ ] P01-13 `Models/StudioResult.cs` + `StudioError.cs` + `StudioErrorCode.cs` (incl. 4 new codes from rev 2).
- [ ] P01-14 `Models/StudioOptions.cs` + `StudioPersistenceMode` enum (incl. 3 new fields from rev 2).
- [ ] P01-15 `Models/StudioInfo.cs` (incl. `ManifestLoaded` + `ManifestVersion` from rev 2).
- [ ] P01-16 `Models/StudioConstants.cs` (incl. `DefaultManifestFileName = "data-lifecycle-manifest.json"`).
- [ ] P01-17 `Models/EnvironmentProfile.cs` + `RolloutTier` enum.
- [ ] P01-18 `Models/DataLifecycleManifest.cs` + 11 sibling records (rev 2).
- [ ] P01-19 `Models/DeploymentMetadata.cs` + 4 sibling records (rev 2).
- [ ] P01-20 `BeepServiceExtensions.Studio.cs` — `AddBeepStudio()` (incl. manifest + deployment registrations).
- [ ] P01-21 `StudioService.cs` — top-level facade impl.
- [ ] P01-22 `NullStudioService.cs` — no-op default.
- [ ] P01-23 Build: `dotnet build DataManagementEngineStandard` succeeds with **0 errors**.

> Phase 01 build status: pending.

---

## Phase 02 — Driver Provisioning · [`02-phase2-driver-provisioning.md`](./02-phase2-driver-provisioning.md)

- [ ] P02-01 `Models/DriverInfo.cs` + `DriverProvisionRequest.cs` + `DriverProvisionResult.cs` + `DriverSource.cs`.
- [ ] P02-02 `Driver/IDriverProvisioner.cs`.
- [ ] P02-03 `Driver/NuGetDriverProvisioner.cs` — wraps `NuGetManagement/`.
- [ ] P02-04 `Driver/LocalDriverProvisioner.cs`.
- [ ] P02-05 `Driver/PluginDriverProvisioner.cs`.
- [ ] P02-06 `Driver/DriverCatalog.cs` — in-memory + on-disk registry.
- [ ] P02-07 `Driver/DriverService.cs` — implements `IDriverService`.
- [ ] P02-08 `Driver/DriverHealthChecker.cs`.
- [ ] P02-09 Wire `IDriverService` into `AddBeepStudio()`.
- [ ] P02-10 Tests: `NuGetDriverProvisionerTests` (2+), `LocalDriverProvisionerTests` (2+), `DriverServiceTests` (3+).
- [ ] P02-11 Document: per-platform "drivers" folder convention.

> Phase 02 build status: pending.

---

## Phase 03 — Connection Configuration · [`03-phase3-connection-configuration.md`](./03-phase3-connection-configuration.md)

- [ ] P03-01 `Models/SourceInfo.cs` + `SourceConfigurationRequest.cs` + `SourceTestResult.cs` + `SourceListFilter.cs`.
- [ ] P03-02 `Models/ConnectionSecrets.cs` — annotation list of secret properties.
- [ ] P03-03 `Keychain/IKeychainProvider.cs`.
- [ ] P03-04 `Keychain/WindowsKeychainProvider.cs` (DPAPI).
- [ ] P03-05 `Keychain/LinuxKeychainProvider.cs` (libsecret + fallback).
- [ ] P03-06 `Keychain/MacKeychainProvider.cs` (Keychain + fallback).
- [ ] P03-07 `Keychain/KeychainFactory.cs` — picks the right provider per OS.
- [ ] P03-08 `Source/SecretRedactor.cs` — extracts secrets, stores in keychain, replaces with refs.
- [ ] P03-09 `Source/SourceRegistry.cs` — JSON read/write of `source-registry.json`.
- [ ] P03-10 `Source/ConnectionPropertyMapper.cs` — maps `DataSourceType` → UI field list.
- [ ] P03-11 `Source/SourceValidator.cs` — pre-save validation.
- [ ] P03-12 `Source/SourceHealthChecker.cs` — wraps `DatasourceManagementService`.
- [ ] P03-13 `Source/SchemaBrowser.cs` — wraps `IDataSource.GetEntityStructure` + `GetEntity`.
- [ ] P03-14 `Source/SourceService.cs` — implements `ISourceService`.
- [ ] P03-15 Wire `ISourceService` into `AddBeepStudio()`.
- [ ] P03-16 Tests: `SecretRedactorTests` (3+), `SourceRegistryTests` (3+), `SourceServiceTests` (4+), `KeychainProviderTests` (3+).
- [ ] P03-17 Update `00-overview-and-scope.md` + this tracker.

> Phase 03 build status: pending.

---

## Phase 04 — Schema Discovery & EF Core Interop · [`04-phase4-schema-discovery-and-ef-interop.md`](./04-phase4-schema-discovery-and-ef-interop.md)

- [ ] P04-01 `Models/EntityDescriptor.cs` + `EntityPropertyDescriptor.cs`.
- [ ] P04-02 `Models/EntityDiscoveryRequest.cs` + `EntityCategoryFilter.cs` + `EntityCategory.cs`.
- [ ] P04-03 `Schema/AssemblyScanner.cs` — file-system scan.
- [ ] P04-04 `Schema/EntityDiscoveryAdapter.cs` — wraps `EntityDiscoveryService`.
- [ ] P04-05 `Schema/EntityClassifier.cs` — categorizes each discovered type.
- [ ] P04-06 `Schema/PocoMetadataReader.cs` — reads `[Key]`, `[MaxLength]`, `[Required]`, etc.
- [ ] P04-07 `Schema/SchemaDesigner.cs` — calls `IMigrationManager.BuildMigrationPlanForModel`.
- [ ] P04-08 `Schema/SchemaService.cs` — implements `ISchemaService`.
- [ ] P04-09 Wire `ISchemaService` into `AddBeepStudio()`.
- [ ] P04-10 Create the separate `BeepDMS.Studio.EfCoreAdapter` project (sibling of the engine).
- [ ] P04-11 `Adapters/EfCore/IEfCoreEntityAdapter.cs` + `EfCoreEntityAdapter.cs` + `EfCoreDbContextIntrospector.cs`.
- [ ] P04-12 Tests: `AssemblyScannerTests` (3+), `SchemaServiceTests` (3+), `PocoMetadataReaderTests` (2+), `EfCoreEntityAdapterTests` (3+).
- [ ] P04-13 Update this tracker.

> Phase 04 build status: pending.

---

## Phase 05 — Migration Orchestration · [`05-phase5-migration-orchestration.md`](./05-phase5-migration-orchestration.md)

- [ ] P05-01 All `Models/*.cs` for this phase (~22 POCOs).
- [ ] P05-02 `Migration/MigrationPlanBuilder.cs` — wraps `IMigrationManager.BuildMigrationPlan*`.
- [ ] P05-03 `Migration/MigrationPolicyEvaluator.cs` — wraps `EvaluateMigrationPlanPolicy` + `EvaluateRolloutGovernance`.
- [ ] P05-04 `Migration/MigrationRunner.cs` — wraps `ExecuteMigrationPlan` + `ResumeMigrationPlan` + `RollbackFailedExecution`.
- [ ] P05-05 `Migration/MigrationProgressStream.cs` — `IProgress<PassedArgs>` ↔ `IStudioProgress`.
- [ ] P05-06 `Migration/MigrationArtifactExporter.cs` — wraps `ExportMigrationArtifacts`.
- [ ] P05-07 `Migration/MigrationPlanCache.cs` — in-memory cache keyed by plan-hash.
- [ ] P05-08 `Migration/MigrationExecutionStateStore.cs` — tracks active handles.
- [ ] P05-09 `Migration/MigrationStudioService.cs` — implements `IMigrationStudioService`.
- [ ] P05-10 Wire `IMigrationStudioService` into `AddBeepStudio()`.
- [ ] P05-11 Tests: `MigrationPlanBuilderTests` (3+), `MigrationPolicyEvaluatorTests` (2+), `MigrationRunnerTests` (3+), `MigrationProgressStreamTests` (2+), `MigrationPlanCacheTests` (2+).
- [ ] P05-12 Update this tracker.

> Phase 05 build status: pending.

---

## Phase 06 — Data Sync Orchestration · [`06-phase6-data-sync-orchestration.md`](./06-phase6-data-sync-orchestration.md)

- [ ] P06-01 All `Models/*.cs` for this phase (~25 POCOs).
- [ ] P06-02 `Sync/SyncSchemaPersistence.cs`.
- [ ] P06-03 `Sync/SyncSchemaDesigner.cs` — wraps `BeepSyncManager.AddSyncSchema` / `UpdateSyncSchema` / `ValidateSchema`.
- [ ] P06-04 `Sync/SyncConflictResolver.cs` — Rule Engine bridge.
- [ ] P06-05 `Sync/SyncReconciliationView.cs` — wraps `LastRunReconciliationReport`.
- [ ] P06-06 `Sync/SyncWatermarkReader.cs` — reads `WatermarkPolicy`.
- [ ] P06-07 `Sync/SyncProgressForwarder.cs` — `IProgress<PassedArgs>` → `IStudioProgress`.
- [ ] P06-08 `Sync/SyncRunQueue.cs` — `Channel<SyncRunRequest>`.
- [ ] P06-09 `Sync/SyncRunRequest.cs` — internal record.
- [ ] P06-10 `Sync/SyncRunnerHostedService.cs` — `BackgroundService`.
- [ ] P06-11 `Sync/SyncStudioService.cs` — implements `ISyncStudioService`.
- [ ] P06-12 `Sync/SyncTelemetry.cs` — SLO + alert hooks.
- [ ] P06-13 Wire `ISyncStudioService`, `SyncRunQueue`, `SyncRunnerHostedService` into `AddBeepStudio()`.
- [ ] P06-14 Tests: `SyncRunQueueTests` (2+), `SyncSchemaDesignerTests` (3+), `SyncStudioServiceTests` (3+), `SyncConflictResolverTests` (2+), `SyncProgressForwarderTests` (2+).
- [ ] P06-15 Update this tracker.

> Phase 06 build status: pending.

---

## Phase 07 — Governance, Approvals & Audit · [`07-phase7-governance-policies-approvals.md`](./07-phase7-governance-policies-approvals.md)

- [ ] P07-01 All `Models/*.cs` for this phase (~25 POCOs).
- [ ] P07-02 `Governance/PolicyStore.cs` + `ApprovalRequestStore.cs`.
- [ ] P07-03 `Governance/AuditRedactor.cs` — `RedactJson`, `IsSecretProperty`.
- [ ] P07-04 `Governance/AuditIntegrator.cs` — wraps `IBeepAudit`.
- [ ] P07-05 `Governance/AuditQueryEngine.cs` — wraps `IAuditQueryEngine`.
- [ ] P07-06 `Governance/PolicyEvaluator.cs` — checks a request against a policy.
- [ ] P07-07 `Governance/ApprovalWorkflow.cs` — Pending → Approved/Rejected transitions.
- [ ] P07-08 `Governance/AlertDispatcher.cs` — SLO + alert hooks.
- [ ] P07-09 `Governance/GovernanceAuditHook.cs` — auto-wires every sub-service.
- [ ] P07-10 `Governance/GovernanceService.cs` — implements `IGovernanceService`.
- [ ] P07-11 Wire `IGovernanceService` + `AuditIntegrator` into `AddBeepStudio()`.
- [ ] P07-12 Modify sub-service services to call `AuditIntegrator.RecordAsync` on every mutation.
- [ ] P07-13 Tests: `PolicyEvaluatorTests` (3+), `ApprovalWorkflowTests` (4+), `AuditIntegratorTests` (3+), `AuditQueryEngineTests` (2+).
- [ ] P07-14 Update this tracker.

> Phase 07 build status: pending.

---

## Phase 08 — Platform Adapters & UI Bridges · [`08-phase8-platform-adapters-and-ui-bridges.md`](./08-phase8-platform-adapters-and-ui-bridges.md)

- [ ] P08-01 `Adapters/IStudioHostAdapter.cs` (full interface).
- [ ] P08-02 `Adapters/StudioHostContext.cs`.
- [ ] P08-03 `Adapters/StudioProgressBridge.cs` — `IStudioProgress` ↔ `IProgress<PassedArgs>`.
- [ ] P08-04 `Adapters/BlazorServerStudioAdapter.cs` + `Adapters/BlazorWasmStudioAdapter.cs`.
- [ ] P08-05 `Adapters/WinFormsStudioAdapter.cs` + `Adapters/WpfStudioAdapter.cs`.
- [ ] P08-06 `Adapters/MauiStudioAdapter.cs` + `Adapters/ConsoleStudioAdapter.cs` + `Adapters/WebApiStudioAdapter.cs`.
- [ ] P08-07 `BeepServiceExtensions.StudioAdapters.cs` — `AddBeepBlazorStudio`, `AddBeepWinFormsStudio`, ….
- [ ] P08-08 Wire the engine's `csproj` to conditionally include the per-platform adapters.
- [ ] P08-09 Tests: one per adapter.
- [ ] P08-10 Update the Blazor host's `.plans/phase-18.md` … `phase-24.md` to point at the Studio + adapters.
- [ ] P08-11 Document: how to add a new host adapter (e.g. Avalonia, Uno).
- [ ] P08-12 Update this tracker.

> Phase 08 build status: pending.

---

## Phase 09 — Data Lifecycle Manifest · [`09-phase9-data-lifecycle-manifest.md`](./09-phase9-data-lifecycle-manifest.md)

- [ ] P09-01 `Manifest/ManifestReader.cs` — JSON + YAML.
- [ ] P09-02 `Manifest/ManifestWriter.cs` — canonical JSON.
- [ ] P09-03 `Manifest/ManifestSchema.cs` — embedded JSON Schema for self-validation.
- [ ] P09-04 `Manifest/ManifestValidator.cs` — the 11 checks (MNF001…MNF080).
- [ ] P09-05 `Manifest/ManifestPathResolver.cs` — walk-up resolver + `BEEP_MANIFEST_PATH`.
- [ ] P09-06 `Manifest/ManifestCache.cs` — in-memory cache + FileSystemWatcher.
- [ ] P09-07 `Manifest/ManifestIssues.cs` — typed issue codes.
- [ ] P09-08 `Manifest/DataLifecycleManifestService.cs` — implements `IDataLifecycleManifestService`.
- [ ] P09-09 Wire `IDataLifecycleManifestService` into `AddBeepStudio()`.
- [ ] P09-10 Modify `IMigrationStudioService.ApplyAsync` (Phase 5) to call `ManifestValidator.ValidateForApplyAsync(plan, env)` before applying.
- [ ] P09-11 Modify `ISyncStudioService.EnqueueRunAsync` (Phase 6) to call `ManifestValidator.ValidateForSyncAsync(schema, env)` before enqueueing.
- [ ] P09-12 Add `<PackageReference>` for `YamlDotNet` (if not already present).
- [ ] P09-13 Tests: `ManifestReaderTests` (3+), `ManifestValidatorTests` (8+), `ManifestPathResolverTests` (2+), `ManifestCacheTests` (2+), `ManifestEnforcementTests` (2+).
- [ ] P09-14 Sample manifest at `Services/Studio/Manifest/sample-data-lifecycle-manifest.json`.
- [ ] P09-15 Update `00-overview-and-scope.md` + this tracker.

> Phase 09 build status: pending.

---

## Phase 10 — Deployment Metadata + Approval Tokens · [`10-phase10-deployment-metadata.md`](./10-phase10-deployment-metadata.md)

- [ ] P10-01 `Deployment/HmacKeyProvider.cs` — reads `BEEP_APPROVAL_HMAC_KEY` or generates ephemeral.
- [ ] P10-02 `Deployment/GitRevisionReader.cs` — `git rev-parse` + `git symbolic-ref`.
- [ ] P10-03 `Deployment/AssemblyVersionReader.cs` — reads `AssemblyInformationalVersion` + build timestamp.
- [ ] P10-04 `Deployment/CodeRevisionResolver.cs` — the 5-step chain.
- [ ] P10-05 `Deployment/ApprovalTokenIssuer.cs` — HMAC-SHA256 signing.
- [ ] P10-06 `Deployment/ApprovalTokenVerifier.cs` — signature + expiry + claims checks.
- [ ] P10-07 `Deployment/DeploymentMetadataEnricher.cs` — `IEnricher<StudioAuditEvent>`.
- [ ] P10-08 `Deployment/DeploymentMetadataService.cs` — implements `IDeploymentMetadataService`.
- [ ] P10-09 `Deployment/DeploymentMetadataEnricherRegistration.cs` — wires the enricher into `AddBeepAudit`.
- [ ] P10-10 Wire `IDeploymentMetadataService` into `AddBeepStudio()`.
- [ ] P10-11 Modify `IGovernanceService.DecideApprovalAsync` (Phase 7) to issue an `ApprovalToken`.
- [ ] P10-12 Modify `IMigrationStudioService.ApplyAsync` (Phase 5) to require a valid `ApprovalToken` for Live / Staging.
- [ ] P10-13 Modify `ISyncStudioService.EnqueueRunAsync` (Phase 6) to require a valid `ApprovalToken` for Live / Staging.
- [ ] P10-14 Tests: `CodeRevisionResolverTests` (3+), `GitRevisionReaderTests` (2+), `HmacKeyProviderTests` (2+), `ApprovalTokenIssuerTests` (3+), `ApprovalTokenVerifierTests` (5+), `DeploymentMetadataEnricherTests` (2+).
- [ ] P10-15 Document: how to set `BEEP_APPROVAL_HMAC_KEY` + `BEEP_DEPLOYMENT_METADATA_JSON` in CI.
- [ ] P10-16 Update this tracker.

> Phase 10 build status: pending.

---

## Phase 11 — App-Scoped Workflow Gaps · [`11-phase11-app-scoped-workflow-gaps.md`](./11-phase11-app-scoped-workflow-gaps.md)

> Raised by re-pointing the WPF AppStudio from the flat services onto `IAppStudioService.Apps.*`.
> All engine/contract work. Ordered by the phase doc's suggested order, not by id.

**P11-C — `RunSoloDevAsync` does not seed** (`ScenarioWorkflow.cs:66-71` reports success for a path that merely exists)

- [ ] P11-C-01 Call `AppQuickStartWorkflow.SeedAsync` instead of the `File.Exists` check.
- [ ] P11-C-02 Report *why* seeding failed in `SoloDevResult.Message`.
- [ ] P11-C-03 Verify `SeedAsync` actually inserts — `:127` passes a boxed `JsonElement` to `InsertEntity`.

**P11-D — QuickStart templates ship no entities** (`AppQuickStartWorkflow.cs:26-32`; `SchemaApplied` can never be true)

- [ ] P11-D-01 Populate `EntityTypeNames`, or drop the templates that promise a schema.
- [ ] P11-D-02 Honour or remove `AppTemplate.IsBlittableOnLocal` (declared, never read).
- [ ] P11-D-03 Make the message honest when a template claimed entities and none were applied.

**P11-A — App-scoped migration planning** (unblocks the last flat-tree consumer)

- [ ] P11-A-01 Add `EnvMigrationPlan` + `EnvMigrationOperation`.
- [ ] P11-A-02 Add `EnvPreflightReport` + `EnvExecutionHandle`.
- [ ] P11-A-03 Extend `IAppMigrationWorkflow` with BuildPlan / DryRunPlan / Preflight / ApplyPlan / RollbackExecution.
- [ ] P11-A-04 Implement in `AppMigrationWorkflow`, reusing `ResolveDatasource` + `ResolveEntityTypes`.
- [ ] P11-A-05 Thread `IStudioProgress` through the App-scoped migration API.
- [ ] P11-A-06 Re-point `PromotionPipelineViewModel` + `PromotionPipelineView` onto `Apps.Migrations`.
- [ ] P11-A-07 Migrate `PromotionPipelineViewModel` onto `StudioViewModelBase`.
- [ ] P11-A-08 Mark flat `IStudioService.Migrations` `[Obsolete]`.

**P11-B — App-scoped governance policy** (restores configurable approvals; `AppGovernanceWorkflow.cs:59` hardcodes `2`)

- [ ] P11-B-01 Add `AppGovernancePolicy` scoped to app + env tier.
- [ ] P11-B-02 Extend `IAppGovernanceWorkflow` with List/Upsert/Delete policy.
- [ ] P11-B-03 Add `Policies` to the JSON store record (`:117`).
- [ ] P11-B-04 `RequestApprovalAsync` reads `RequiredApproverCount` from policy.
- [ ] P11-B-05 `EvaluateAsync` honours `BlockedOperations` / `AllowedApproverRoles` / cooldown.
- [ ] P11-B-06 Restore the policy-authoring tab in `GovernanceView`.
- [ ] P11-B-07 Decide `VerifyAuditIntegrityAsync` — the App store has **no hash chain**.

**P11-E — `SeedAsync` contract vs implementation** (doc says folder/CSV/assembly; impl is one JSON file)

- [ ] P11-E-01 Widen the implementation or narrow the doc.
- [ ] P11-E-02 Match the WPF seed tooltips to whichever wins.

**P11-F — Deprecation cleanup** (after P11-A + P11-B)

- [ ] P11-F-01 `[Obsolete]` the flat `Migrations` + `Governance` accessors.
- [ ] P11-F-02 Delete the orphaned flat policy CRUD, or document why two policy stores coexist.
- [ ] P11-F-03 Check `BeepWeb` + `Beep.Desktop` before removing anything.

> Phase 11 status: not started. Raised 2026-07-15 from the AppStudio re-point; contracts unchanged.

---

## Cumulative counts (rev 2)

| Phase | Complete | In Progress | Remaining |
|---|---|---|---|
| Phase 00 — Overview | 6 | 0 | 0 |
| Phase 01 — Core contracts | 0 | 0 | 23 |
| Phase 02 — Driver | 0 | 0 | 11 |
| Phase 03 — Source | 0 | 0 | 17 |
| Phase 04 — Schema | 0 | 0 | 13 |
| Phase 05 — Migration | 0 | 0 | 12 |
| Phase 06 — Sync | 0 | 0 | 15 |
| Phase 07 — Governance | 0 | 0 | 14 |
| Phase 08 — Adapters | 0 | 0 | 12 |
| Phase 09 — Manifest | 0 | 0 | 15 |
| Phase 10 — Deployment | 0 | 0 | 16 |
| Phase 11 — App-scoped gaps | 0 | 0 | 26 |
| **Total (engine, rev 2)** | **6** | **0** | **184** |

---

## Conventions (engine-team style — copy from `Services/Plans/MASTER-TODO-TRACKER.md`)

- **No EF Core for app data.** EF Core stays an **adapter concern** (Phase 4 optional bridge).
- **No platform-specific types** in the engine core. `MudBlazor.*`, `System.Windows.*`,
  `System.Drawing.*`, `Microsoft.Maui.*` are forbidden in `Services/Studio/` **except**
  in the per-platform adapter files (`Adapters/Blazor*`, `Adapters/WinForms*`, etc.).
- **`partial class` pattern** for big types (one concern per file: `.Core`, `.Query`, `.Lifetime`).
- **No-op default** for every service (`NullStudioService`, etc.) so hosts can opt out per feature.
- **All async methods** accept `CancellationToken`; the only sync exceptions are pure
  metadata reads (e.g. `GetInfo()`).
- **Error contract:** `StudioResult<T>` with `StudioErrorCode`; never throw for business
  errors. Throw only for programmer errors (null args, etc.).
- **Audit-by-default:** every mutation records an `IBeepAudit` event, enriched with
  `DeploymentMetadata` (Phase 10).
- **Hash chain on, redactors on, file/SQLite sink** — re-use the engine team's existing
  `IBeepAudit` pipeline; do not duplicate.
- **No deployment orchestration** in any phase. The Studio records what code revision
  it ran against; it does not build, deploy, or push anything.
- **No project scaffolding** in any phase. The Studio reads the project's
  `DataLifecycleManifest`; it does not generate `.csproj`, `Program.cs`, etc.
