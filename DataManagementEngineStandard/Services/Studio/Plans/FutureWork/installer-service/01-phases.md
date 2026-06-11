# Installer Service — Planned Phases

> **Status:** draft only. No code yet. See `00-overview-and-scope.md` for the
> service's goals, non-goals, and architecture.

> Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## How to read this

Each phase below is a **coherent unit of work** that lands in a single PR.
Phases are sequenced so each builds on the previous; some early phases can
run in parallel (e.g. INST-04 online update feed can start before
INST-05 update-time data migration is fully wired). Total: **9 phases** + the
overview (Phase 0).

The phases reference engine code from
`C:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\DataManagementEngineStandard\Services\Studio\`.
The installer does **not** fork the engine; it consumes the engine via the
`IStudioService` facade (engine Phase 1) + the `WinFormsStudioAdapter`
(engine Phase 8).

---

## Phase 0 — Overview & Scope · [`00-overview-and-scope.md`](./00-overview-and-scope.md)

- [x] P-INST-00-01 Define the installer's goals + non-goals.
- [x] P-INST-00-02 Sketch the architecture.
- [x] P-INST-00-03 List the engine APIs the installer consumes.
- [x] P-INST-00-04 List the commercial-grade products we benchmark against.

> Phase 0 status: doc only.

---

## Phase 1 — WinForms Shell + Multi-Page Wizard UX

**Goal:** the installer's WinForms shell can host a multi-page wizard with
branded visuals, progress strip, and cancel.

**Scope:**
- New project `BeepDM.Installer` (WinForms, `net10.0-windows`).
- A `BeepWizardControl` that hosts N pages with a step-progress strip at
  the top and Back / Next / Cancel buttons at the bottom.
- A branded theme (default + overrideable per app).
- A `WelcomePage` + `InstallPathPage` + `FeaturesPage` + `FinishPage` skeleton.
- The wizard runs **without** the engine first; the engine is added in
  Phase 2.
- Tests: the wizard navigates forward / backward; Cancel works on every
  page; the progress strip updates.

**Exit criteria:**
- A 5-page wizard with branded visuals renders in < 1s on a cold start.
- Cancel works on every page and aborts the install cleanly.

**Engine dependency:** none.

**WinForms dependency:** yes — this is the only phase that ships a WinForms
UI. All other phases are headless services.

---

## Phase 2 — First-Install Data-Source Configuration

**Goal:** during first install, the user configures their data sources via
the engine's `ISourceService`.

**Scope:**
- Reference `DataManagementEngineStandard`; call `AddBeepStudio()` +
  `AddBeepWinFormsStudio()` (engine Phase 8).
- New page `DataSourceConfigPage` that:
  1. Reads the project's `DataLifecycleManifest.json` (bundled in the
     installer).
  2. For each `expectedSources[]` in the manifest, prompts the user for
     the connection details (host, port, database, user, password).
  3. Calls `IStudioService.Sources.ConfigureSourceAsync(request)` for
     each source.
  4. Calls `IStudioService.Sources.TestSourceAsync(name)` to verify
     connectivity.
  5. Records every step in `IBeepAudit`.
- Passwords are stored in the engine's keychain (engine Phase 3).
- Tests: a valid manifest + valid connection details succeed; an invalid
  connection shows a clear error and lets the user retry.

**Exit criteria:**
- A first-install user with a SQL Server box ends the wizard with a
  working connection to the manifest's expected sources.
- The audit log shows the configure + test steps.

**Engine dependency:** Phases 1, 3, 9, 10.

---

## Phase 3 — First-Install Schema Migration

**Goal:** during first install, after data sources are configured, the
installer runs the initial schema migration.

**Scope:**
- New page `MigrationPage` that:
  1. Calls `IStudioService.Migrations.BuildPlanAsync(planRequest)`.
  2. Shows the plan in a `MudDataGrid`-equivalent WinForms grid (entity,
     op, DDL preview, risk).
  3. Calls `DryRunAsync` + `PreflightAsync` + `EvaluatePolicyAsync`.
  4. Shows the user a "Ready to apply" summary.
  5. On user click, calls `ApplyAsync(plan, policy, progress)`.
  6. Streams `IStudioProgress` updates into a `ProgressBar` on the page.
- On failure, the page shows the error and offers a Retry / Abort.
- For Live / Staging (rare for first install but possible for migration
  from a competitor's installer), call
  `IGovernanceService.RequestApprovalAsync` and show a "Waiting for
  approval" message.
- Tests: a valid manifest + valid sources apply the plan; a broken source
  fails with the right `StudioErrorCode`.

**Exit criteria:**
- A first-install user with valid sources + a 10-entity POCO namespace
  ends the wizard with the schema applied to their database.
- The audit log shows the build + apply steps.

**Engine dependency:** Phases 1, 2, 3, 5, 7, 9, 10.

---

## Phase 4 — Online Update Feed Reader + Downloader

**Goal:** the running app checks for updates on a schedule and downloads
new builds in the background.

**Scope:**
- New service `UpdateFeedReader` that polls the feed URL every 6 hours
  (configurable).
- New service `UpdateDownloader` that:
  1. Downloads the new build to a temp directory.
  2. Verifies the SHA-256 hash from the feed.
  3. Verifies the code-signing certificate (if provided in the feed).
  4. Stages the new build to a side-by-side install directory
     (`<installRoot>/app-1.2.3/` next to `<installRoot>/app-1.2.2/`).
- New form `UpdatePromptForm` shown on the next app launch:
  - "Update to 1.2.3 is available. The update will run N data migrations
    and 0 data syncs. Estimated downtime: 2 min. Continue?"
  - Buttons: "Install now" / "Install on next launch" / "Skip this version"
  - Link to release notes.
- A "checking for updates…" background indicator in the app's tray icon.
- Tests: a feed with a valid build downloads + verifies; a feed with a
  tampered hash aborts.

**Exit criteria:**
- The app tray icon shows "Update available" within 6 hours of a new
  build's release.
- The staged install directory contains the verified new build.

**Engine dependency:** none for the feed reader / downloader. The data
migration happens in Phase 5.

---

## Phase 5 — Update-Time Data Migration

**Goal:** when the user accepts an update, the installer runs the data
migrations via the engine, with the same workflow as Phase 3 (plan →
dry-run → preflight → policy → approval (if needed) → apply).

**Scope:**
- New service `MigrationRunner` that:
  1. Loads the new build's `DataLifecycleManifest.json`.
  2. Calls `IStudioService.Manifest.ValidateAsync(manifest)`.
  3. Calls `BuildPlanAsync` + `DryRunAsync` + `PreflightAsync`.
  4. For Live / Staging: calls `IGovernanceService.RequestApprovalAsync`
     and shows the approval prompt in the install wizard.
  5. Calls `IStudioService.Deployment.IssueApprovalTokenAsync` to mint
     the HMAC-signed token.
  6. Calls `IStudioService.Migrations.ApplyAsync(plan, policy, token,
     progress)`.
  7. Streams `IStudioProgress` into the installer's progress bar.
  8. Records every step in `IBeepAudit`.
- The migration is run **before** the binary replace, so a failed
  migration does not leave the user with a broken install.
- Tests: a valid manifest + valid sources + valid plan apply cleanly; a
  failed plan aborts the install and the previous build keeps running.

**Exit criteria:**
- An update from 1.2.2 to 1.2.3 with 3 schema migrations applies all 3
  within the wizard's progress bar.
- A failed update rolls back to the previous build (Phase 6).

**Engine dependency:** Phases 1, 2, 3, 5, 7, 9, 10.

---

## Phase 6 — Rollback on Failure

**Goal:** if any step of the install / update fails, the previous build
keeps running and the user sees a clear error.

**Scope:**
- New service `RollbackService` that:
  1. On any exception during Phase 2, 3, or 5, restores the previous
     build's binary.
  2. Reverts any partial migration that the engine's
     `RollbackFailedExecution` couldn't undo.
  3. Records the rollback in `IBeepAudit`.
  4. Shows the user a "Update failed — the previous version (1.2.2) is
     still installed. Please contact support." message.
- The install root is structured so rollback is just a directory rename:
  ```
  <installRoot>/
  ├── app-1.2.2/    ← currently running
  ├── app-1.2.3/    ← staged (during Phase 4-5)
  ├── current → app-1.2.2/    ← symlink (Windows: junction)
  ```
- Tests: a failed migration in `app-1.2.3/` leaves `current → app-1.2.2/`
  intact.

**Exit criteria:**
- A failed update leaves the user with the previous build's binary
  running and no data loss.
- The audit log shows the rollback.

**Engine dependency:** Phases 1, 5, 7.

---

## Phase 7 — Branding + Theming + EULA

**Goal:** the installer's UX is commercial-grade — branded visuals,
licensed EULA, polished progress bar, and an icon.

**Scope:**
- A `Theme/` folder with a default theme (the Beep brand) + an
  overrideable per-app theme.
- An `Eula.rtf` (default) + an overrideable per-app EULA.
- An icon (.ico) at multiple resolutions (16, 32, 48, 64, 128, 256).
- A `BeepWizardControl` that renders the theme correctly (back colour,
  button colour, font, banner image).
- A "Branding guide" markdown doc at `docs/BRANDING.md` explaining how
  an app overrides the theme + EULA + icon.
- Tests: a sample override theme renders correctly in the wizard.

**Exit criteria:**
- The installer's UI looks like a commercial product (no default WinForms
  grey boxes).
- A second app can override the theme + EULA + icon with a single folder
  of files.

**Engine dependency:** none.

---

## Phase 8 — Silent Install for Enterprises

**Goal:** an enterprise IT admin can install + update the app across
1000+ machines without any user interaction.

**Scope:**
- Command-line flags:
  - `/quiet` — no UI, no prompts, no tray icon.
  - `/source=<path-to-json>` — provides the data-source config (host,
    port, database, user, password) without prompting.
  - `/manifest=<path>` — overrides the bundled manifest.
  - `/log=<path>` — writes the install log to a file.
  - `/rollback` — rolls back to the previous build (no install).
  - `/check-update` — checks for an update and prints the result to
    stdout, no install.
- Exit codes:
  - `0` — success
  - `1` — user cancelled
  - `2` — invalid arguments
  - `3` — source unreachable
  - `4` — migration failed
  - `5` — rollback failed (previous build is broken; manual intervention
    required)
- A sample SCCM / Intune deployment script at `docs/SCCM-DEPLOY.md`.
- Tests: a `/quiet` install with a valid `/source` succeeds; a `/quiet`
  install with an invalid source returns exit code 3.

**Exit criteria:**
- `BeepDM.Installer.exe /quiet /source=source.json` installs the app with
  no UI.
- The exit code is correct in every scenario.

**Engine dependency:** same as Phase 2 + 3.

---

## Phase 9 — Telemetry + Crash Reporting

**Goal:** the installer reports its activity to the data-platform team
(opt-in) so they can spot patterns in install / update failures.

**Scope:**
- An opt-in `Options.Telemetry.Enabled` flag (default `false` in dev,
  `true` in production).
- When enabled, every install / update step writes a `StudioAuditEvent`
  to the engine's `IBeepAudit` with the deployment metadata
  (engine Phase 10).
- A crash reporter (`Telemetry.CrashReporter`) that catches unhandled
  exceptions in the installer and writes a crash record to
  `IBeepAudit` + an NDJSON file at `<installRoot>/logs/crash.log.ndjson`.
- A sample dashboard page in the Blazor host (Phase 25) that shows the
  install / crash history per app version.
- A privacy notice in the EULA explaining what is collected.
- Tests: an opt-in install writes audit events; an opt-out install does
  not.

**Exit criteria:**
- An opt-in install leaves a complete audit trail in `IBeepAudit`.
- An opt-out install leaves no telemetry.
- A crashing install leaves a crash record in the local crash log.

**Engine dependency:** Phases 1, 7, 10.

---

## Cumulative counts

| Phase | Complete | In Progress | Remaining |
|---|---|---|---|
| Phase 0 — Overview | 4 | 0 | 0 |
| Phase 1 — WinForms shell | 0 | 0 | ~6 |
| Phase 2 — Data-source config | 0 | 0 | ~8 |
| Phase 3 — Schema migration | 0 | 0 | ~8 |
| Phase 4 — Update feed | 0 | 0 | ~6 |
| Phase 5 — Update data migration | 0 | 0 | ~8 |
| Phase 6 — Rollback | 0 | 0 | ~5 |
| Phase 7 — Branding | 0 | 0 | ~5 |
| Phase 8 — Silent install | 0 | 0 | ~6 |
| Phase 9 — Telemetry | 0 | 0 | ~5 |
| **Total (installer service)** | **4** | **0** | **~57** |

> The per-phase todos are **drafts**; finalise them when the first phase is
> started. The above is a planning estimate, not a contract.
