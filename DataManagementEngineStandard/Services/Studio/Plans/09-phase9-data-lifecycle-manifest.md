# Phase 09 — Data Lifecycle Manifest (`IDataLifecycleManifestService`)

> **Scope:** implement `IDataLifecycleManifestService` — the Studio's manifest
> reader / writer / validator. The manifest is a JSON or YAML file in the project
> repo that declares: "this code revision expects these data sources, with these
> schemas, with this approval tier, with this retention policy." The Studio reads
> it on startup, validates every apply against it, and is the **link between data
> lifecycle and code lifecycle**.

> Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## Why this phase

We already have a strong data-lifecycle engine (`IMigrationManager`,
`BeepSyncManager`, `IDefaultsManager`, `IMappingManager`). What's missing is the
**project-level artefact** that tells a data engineer, a DBA, and a CI pipeline:

- "Which data sources does this code revision expect?"
- "What tier is each environment, and which approvers are required?"
- "What schema policies apply (forbid destructive in Live, require preflight, …)?"
- "What sync policies apply (watermark required, conflict rule, max rows per run)?"
- "What audit policies apply (what to redact, retention, hash chain)?"
- "What approval policies apply (cooldown, approver roles, plan-hash match)?"

Without this artefact, every project re-invents it. With it, the Studio can:

1. **Refuse to apply** a migration that targets a source not in the manifest.
2. **Refuse to apply** a migration in Live that violates the manifest's
   `forbidDestructiveInLive` policy.
3. **Refuse to start** if the manifest version is unsupported.
4. **Refuse to issue an approval** that doesn't match the current code revision.
5. **Emit a clear error** to the DBA: "The manifest says the Live tier requires
   2 approvers with the `DBA` role; you have 1."

The manifest is **authored by the data-platform team** and committed to the repo
alongside the code. It is **read on Studio startup** and **validated on every
mutation**. The CI pipeline runs `beepdms manifest validate` as a pass/fail gate
(Phase 23 of the Blazor workspace plan).

## Public surface (this phase fills in)

```csharp
// Contracts/IDataLifecycleManifestService.cs
public interface IDataLifecycleManifestService
{
    Task<StudioResult<DataLifecycleManifest>> LoadAsync(string? overridePath = null, CancellationToken ct = default);
    Task<StudioResult<bool>> SaveAsync(DataLifecycleManifest manifest, string path, CancellationToken ct = default);
    Task<StudioResult<ManifestValidationReport>> ValidateAsync(DataLifecycleManifest manifest, CancellationToken ct = default);
    Task<StudioResult<ManifestValidationReport>> ValidateAtAsync(string path, CancellationToken ct = default);
    string? ResolveManifestPath(string? startDirectory = null);
    DataLifecycleManifest? Current { get; }                // null until LoadAsync succeeds
}
```

## Models (declared in Phase 1, documented here)

The `DataLifecycleManifest` POCO family is in `Models/DataLifecycleManifest.cs`.
The full set is:

| Type | Purpose |
|---|---|
| `DataLifecycleManifest` | Root — version, owner, project, dataLifecycle spec |
| `ProjectRef` | Name, type, repository, code revision |
| `DataLifecycleSpec` | Owner, tier, environments, expected sources, policies |
| `EnvironmentSpec` | Id, name, tier, source aliases, approval + cooldown |
| `ExpectedSourceSpec` | Alias, driver, category, free-form policies (PII, retention, encryption) |
| `SchemaPolicies` | `requireMigrationPlanHash`, `forbidDestructiveInLive`, `requirePreflightOnLive`, `blockedOperations` |
| `SyncPolicies` | `watermarkRequired`, `conflictResolutionRule`, `maxRowsPerRun`, `blockedSchemas` |
| `AuditPolicies` | `redact`, `retentionDays`, `requireHashChain` |
| `ApprovalPolicies` | `defaultApproverRoles`, `requirePlanHashMatch`, `cooldownBetweenRuns` |
| `ManifestValidationReport` | `isValid`, `issues[]`, `validatedAt`, `manifestSha256` |
| `ManifestValidationIssue` | `code`, `path` (JSON pointer), `message`, `severity` |

## Folder layout (this phase creates)

```
Services/Studio/
├── Contracts/IDataLifecycleManifestService.cs     ← DONE in Phase 1
├── Models/  (all the records above — DONE in Phase 1)
└── Manifest/
    ├── DataLifecycleManifestService.cs            ← implements IDataLifecycleManifestService
    ├── ManifestReader.cs                          ← JSON + YAML reader
    ├── ManifestWriter.cs                          ← JSON + YAML writer
    ├── ManifestValidator.cs                       ← schema + cross-reference validator
    ├── ManifestSchema.cs                          ← JSON Schema (Draft 2020-12) for self-validation
    ├── ManifestPathResolver.cs                    ← walks up from CWD to repo root
    ├── ManifestCache.cs                           ← in-memory cache; re-validates on file-change
    └── ManifestIssues.cs                          ← typed issue codes (e.g. ManifestInvalid, ManifestVersionUnsupported)
```

## Reader / writer

`ManifestReader` accepts both JSON (`DataLifecycleManifest.json`) and YAML
(`DataLifecycleManifest.yaml`). The default is JSON; YAML is opt-in via
`StudioOptions.ManifestFormat = "yaml"`. The reader:

1. Loads the file as text.
2. Computes `manifestSha256` (used as the `CorrelationId` for every audit event
   that touches the manifest).
3. Parses it into `DataLifecycleManifest`.
4. Validates it against the embedded `ManifestSchema` (JSON Schema Draft 2020-12).
5. Returns a `ManifestValidationReport` with the parse + schema-validation issues.

`ManifestWriter` writes the canonical JSON form (sorted keys, 2-space indent,
UTF-8, LF line endings) so a re-read produces a stable `manifestSha256`.

## Validator

`ManifestValidator` runs the full cross-reference check:

| Check | Code | Severity |
|---|---|---|
| `manifestVersion` ∈ supported versions (1 for v1) | `MNF001` | Error |
| `project.codeRevision.sha` matches `git rev-parse HEAD` (if running in a git repo) | `MNF010` | Warn |
| `dataLifecycle.environments` non-empty | `MNF020` | Error |
| Every `environment.id` is unique | `MNF021` | Error |
| Every `environment.dataSourceAliases` references a `expectedSources.alias` | `MNF030` | Error |
| `expectedSources[].driver` matches a known driver in `IConfigEditor.DataDriversClasses` (or `Warn` if the driver is missing — the manifest can declare sources that are not yet provisioned) | `MNF040` | Warn / Error |
| `expectedSources[].category` is a valid `DatasourceCategory` | `MNF041` | Error |
| `syncPolicies.maxRowsPerRun > 0` | `MNF050` | Error |
| `auditPolicies.retentionDays >= 0` | `MNF060` | Error |
| `approvalPolicies.cooldownBetweenRuns` ≥ 0 if set | `MNF070` | Error |
| If `auditPolicies.requireHashChain = true`, the host's `IBeepAudit` must have hash chain enabled | `MNF080` | Error |

The validator returns a `ManifestValidationReport` with all issues (not just
the first). The host UI renders them as a list; the CI gate (`beepdms manifest
validate`) returns non-zero on any `Error` severity.

## Path resolver

`ManifestPathResolver.ResolveManifestPath(startDirectory)` walks up from
`startDirectory` (default: current working directory) to the filesystem root,
looking for a file at `beep/data-lifecycle-manifest.json` (or the
`StudioOptions.ManifestPath` override). The first match wins. If no file is
found, the resolver returns `null` and `LoadAsync` returns
`StudioResult.Fail(StudioErrorCode.NotFound, "No DataLifecycleManifest found in any parent directory")`.

The Studio also respects a `BEEP_MANIFEST_PATH` env var (for CI runs).

## Cache + change detection

`ManifestCache` holds the in-memory `DataLifecycleManifest` (the `Current`
property) and a `FileSystemWatcher` on the manifest file. On file change, the
cache re-reads and re-validates. If the new manifest is invalid, the cache
**keeps the old one** and emits a warning via `IBeepLog`. The host UI shows a
yellow chip: "Manifest changed but failed to validate — old version still active."

This matches the engine team's existing pattern in `BeepService.Configure` where
config is reloaded but a bad reload doesn't crash the process.

## Enforcement on Apply

`StudioOptions.EnforceManifestOnApply = true` (default) means:

- `IMigrationStudioService.ApplyAsync` (Phase 5) **rejects** the apply if the
  target source is not in the manifest's `expectedSources`.
- `IMigrationStudioService.ApplyAsync` **rejects** the apply if the target
  env tier is `Live` and the manifest's `schemaPolicies.forbidDestructiveInLive = true`
  and the plan contains a `Drop` or `Truncate` operation.
- `ISyncStudioService.EnqueueRunAsync` (Phase 6) **rejects** the run if the
  schema is in `syncPolicies.blockedSchemas`.

When enforcement is off, the manifest is **advisory** — the host UI can still
display it, but the Studio doesn't block the mutation.

## Cross-cutting

- The manifest is **never** written to the audit log directly. What gets audited
  is the **`manifestSha256`** and the **diff** between the previous and current
  manifest (via the `DeploymentMetadataEnricher` in Phase 10).
- The manifest is **versioned** by the `manifestVersion` field. The Studio
  supports version 1; any other version returns
  `StudioErrorCode.ManifestVersionUnsupported`.

---

## Todo Tracker

| # | Task | Status | Notes |
|---|------|--------|-------|
| P09-01 | `Manifest/ManifestReader.cs` — JSON + YAML | ⬜ | Use `YamlDotNet` for YAML |
| P09-02 | `Manifest/ManifestWriter.cs` — canonical JSON | ⬜ | |
| P09-03 | `Manifest/ManifestSchema.cs` — embedded JSON Schema for self-validation | ⬜ | |
| P09-04 | `Manifest/ManifestValidator.cs` — the 11 checks in the table above | ⬜ | |
| P09-05 | `Manifest/ManifestPathResolver.cs` — walk-up resolver + `BEEP_MANIFEST_PATH` env var | ⬜ | |
| P09-06 | `Manifest/ManifestCache.cs` — in-memory cache + FileSystemWatcher | ⬜ | |
| P09-07 | `Manifest/ManifestIssues.cs` — typed issue codes (MNF001 … MNF080) | ⬜ | |
| P09-08 | `Manifest/DataLifecycleManifestService.cs` — implements `IDataLifecycleManifestService` | ⬜ | |
| P09-09 | Wire `IDataLifecycleManifestService` into `AddBeepStudio()` (already done in Phase 1; this task verifies the DI binding) | ⬜ | |
| P09-10 | Modify `IMigrationStudioService.ApplyAsync` (Phase 5) to call `ManifestValidator.ValidateForApplyAsync(plan, env)` before applying | ⬜ | Cross-phase wiring |
| P09-11 | Modify `ISyncStudioService.EnqueueRunAsync` (Phase 6) to call `ManifestValidator.ValidateForSyncAsync(schema, env)` before enqueueing | ⬜ | Cross-phase wiring |
| P09-12 | Add `<PackageReference>` for `YamlDotNet` to `DataManagementEngineStandard.csproj` (only if not already present) | ⬜ | |
| P09-13 | Tests: `ManifestReaderTests` (3+), `ManifestValidatorTests` (8+ — one per check code), `ManifestPathResolverTests` (2+), `ManifestCacheTests` (2+), `ManifestEnforcementTests` (2+ — verify Phase 5 + Phase 6 reject invalid manifests) | ⬜ | |
| P09-14 | Sample manifest at `Services/Studio/Manifest/sample-data-lifecycle-manifest.json` (committed for tests) | ⬜ | |
| P09-15 | Update `00-overview-and-scope.md` + `MASTER-TODO-TRACKER.md` to mark Phase 09 done | ⬜ | |

---

## Validation (definition of done)

- [ ] `dotnet build DataManagementEngineStandard` succeeds with **0 errors**.
- [ ] `DataLifecycleManifestService.LoadAsync` on the sample manifest returns a parsed `DataLifecycleManifest` with the right values.
- [ ] `ManifestValidator.ValidateAsync` on the sample manifest returns `IsValid = true` with no errors.
- [ ] `ManifestValidator.ValidateAsync` on a manifest with `forbidDestructiveInLive = false` + a `DropEntity` plan returns at least one `MNF030` issue.
- [ ] `ManifestPathResolver.ResolveManifestPath` from a sample sub-directory finds the manifest.
- [ ] `ManifestCache` re-reads the manifest when the file changes (verified via a `FileSystemWatcher` test).
- [ ] `IMigrationStudioService.ApplyAsync` (mocked) rejects an apply when the manifest's `expectedSources` does not include the target source.
- [ ] All 17+ new tests pass.

---

## Pitfalls

1. **Don't put a `BEEP_*` env var in the manifest schema** — the manifest is committed to the repo; env vars are runtime configuration. Use `StudioOptions` for env-aware configuration.
2. **Don't compute the manifest hash with a non-deterministic algorithm** — use SHA-256 over the canonical JSON bytes (sorted keys, LF line endings, no trailing whitespace).
3. **Don't break the build if `YamlDotNet` is missing** — gate the YAML reader behind a runtime check; the JSON reader is the default.
4. **Don't allow the manifest to disable the audit hash chain** if the host's `IBeepAudit` is configured with hash chain on. The validator flags the conflict but does not silently fix it.
5. **Don't auto-fix manifest issues** — return them, let the author fix them. Silent auto-fix hides real config drift.
6. **Don't put connection strings in the manifest** — only **source aliases**. The actual credentials live in the keychain (Phase 3).
7. **Don't allow the manifest to be loaded from outside the repo** — the path resolver must not follow symlinks that escape the repo root.

---

## Related

- Phase 01 — contracts (this phase implements `IDataLifecycleManifestService`)
- Phase 03 — connection configuration (every source must have a manifest alias)
- Phase 05 — migration orchestration (enforced on Apply)
- Phase 06 — data sync orchestration (enforced on Enqueue)
- Phase 07 — governance (the manifest's policies feed the governance policy)
- Phase 10 — deployment metadata (the manifest's `codeRevision` is the default if env vars don't override it)
- `.plans/phase-23.md` — CI/CD (the `beepdms manifest validate` CLI command is a pass/fail gate)
- `BeepDM/DataManagementEngineStandard/Services/EnvironmentService.cs` — the cross-platform path helper we re-use
