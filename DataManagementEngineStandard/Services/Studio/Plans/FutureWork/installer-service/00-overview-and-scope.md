# Installer Service — Overview, Scope, and Architecture

> **Status:** planning only. No code yet. Aspirational; no start date committed.
> Will live in a separate WinForms project (or potentially MAUI / WPF, TBD).

> Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## What this service is

A standalone **Installer service** for desktop Beep applications
(WinForms, WPF, MAUI). It provides:

1. A **first-install wizard** that walks the user through:
   - Accepting the license
   - Picking the install path
   - Choosing which features to install
   - Configuring data sources (via the engine's `ISourceService`)
   - Running the initial schema migration (via the engine's
     `IMigrationStudioService`)
   - Creating the first user account
2. An **online-update service** that:
   - Polls a feed (the same CI/CD service from the sibling plan) for new
     builds
   - Downloads the new build in the background
   - On the user's next launch, prompts to install the update
   - Performs the update without losing user data
3. A **data-migration service** that:
   - On every update, runs the engine's data migrations (`BuildPlan` →
     `DryRun` → `Preflight` → `Apply`)
   - On data-source reconfigurations, runs the engine's data sync
   - On first install, seeds the reference data
4. A **commercial-grade UX** (multi-page wizard, progress bars, branded
   visuals, error recovery, rollback on failure) comparable to
   InstallShield / WiX / Advanced Installer / ClickOnce / Squirrel.

## Why this is a separate service

- The engine (Phases 0-10) is **in-process**. It does not have a UI shell,
  does not have an installer UX, does not have an update protocol. Adding
  those to the engine would conflate "data-lifecycle orchestration" with
  "desktop installer / updater" — and the user has explicitly scoped the
  Studio to data lifecycle.
- Desktop installer / updater patterns are well-understood but verbose.
  A separate project keeps the engine's surface small and lets the
  installer iterate on its own release cadence.
- The installer is **hosted on the user's machine**, not in the data
  center. It needs Win32 / WinForms / WPF / MAUI APIs that the engine
  should not depend on.
- The user wants the option to **swap the installer technology**
  (InstallShield, WiX, Squirrel, ClickOnce, the in-house one) without
  touching the engine. A separate service is the right abstraction layer.

## What it is NOT

- **Not a packaging tool.** The installer is the **runtime** that
  installs / updates an already-packaged app. The packaging (MSI, EXE,
  MSIX) is done by the project's own build pipeline (e.g. via WiX or
  Visual Studio's Publish).
- **Not a code-signing service.** It does not generate code-signing
  certificates or sign binaries. The project's build pipeline does that
  (using a CI-supplied cert). The installer just verifies the signature
  on the new build before installing it.
- **Not an OS package manager.** It does not interact with `apt`, `brew`,
  `winget`, or Windows Store. It installs the app into a per-user or
  per-machine directory and updates it in place.
- **Not a sandbox.** The installed app runs with the same privileges as
  the user; the installer does not provide virtualization or sandboxing.
- **Not a multi-tenant SaaS.** It is a single-app installer. Each
  Beep-based desktop app ships its own copy.

## Goals

1. **A first-install experience that "just works."** A new user downloads
   the installer, double-clicks, and ends up with a working app + working
   data in < 5 minutes.
2. **Online updates that don't lose data.** An update from version N to
   version N+1 transparently runs the data migrations in the engine.
3. **Online updates that don't lose user trust.** The user sees a clear
   "what's changing" prompt before each update, can read the changelog,
   and can defer the update.
4. **Reuses the engine.** The installer's data-migration step is a thin
   wrapper over the engine's `IMigrationStudioService.ApplyAsync`. No
   duplicate migration logic.
5. **Reuses the manifest.** The installer's data-source configuration
   step reads the project's `DataLifecycleManifest.json` to know which
   sources to configure.
6. **Reuses the audit log.** The installer writes every step into the
   engine's `IBeepAudit` so the support team has the same audit trail
   they have for the data-lifecycle operations.

## Non-goals (explicitly out of scope)

- Building, code-signing, or packaging the app.
- Managing the OS-level dependencies (`.NET` runtime, Visual C++
  Redistributable, etc.) — the installer assumes they are present
  (prerequisite checks are a small win, not a goal).
- Multi-tenant SaaS-style metering / licensing (the installer can call
  a license-check endpoint, but licensing itself is the app's concern).
- Mobile app distribution (App Store, Google Play) — that's a separate
  concern handled by the MAUI build pipeline.
- Web-app deployment (IIS, k8s, App Service) — that's the CI/CD service
  from the sibling plan, not this one.

## Architecture (sketch)

```
┌──────────────────────────────────────────────────────────────────┐
│           BeepDM.Installer (WinForms app, ~3MB)                  │
│                                                                  │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────────┐  │
│  │ Wizard UI      │  │ Updater        │  │ Data Migration     │  │
│  │ (WinForms,     │  │ (background    │  │ Service             │  │
│  │  multi-page,   │  │  polling +     │  │ (calls engine's     │  │
│  │  branded,      │  │  download +    │  │  IMigrationStudio-  │  │
│  │  cancellable)  │  │  install)      │  │  Service)           │  │
│  └────────────────┘  └────────────────┘  └────────────────────┘  │
│           │                  │                    │              │
│           └──────────────────┼────────────────────┘              │
│                              ▼                                   │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  Engine Bridge  (calls the installed app's IStudioService   │  │
│  │  via an in-process .NET host)                               │  │
│  └────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
```

The installer is itself a small .NET app that:

1. **Hosts the engine in-process** (via `AddBeepStudio()` + a small subset
   of `AddBeepBlazorStudio()`-style services). The installer does **not**
   ship a Blazor UI; it ships a WinForms UI but consumes the engine's
   view-models.
2. **Talks to the online-update feed** (the same CI/CD service from the
   sibling plan, or a simpler static feed for small apps).
3. **Runs data migrations via the engine** during install / update.

## Online update protocol (draft)

The installer polls a feed URL (configurable, defaults to
`https://updates.<vendor>.com/<app>/feed.json`) every 6 hours. The feed
format (proposed):

```json
{
  "channel": "stable",
  "latest": {
    "version": "1.2.3",
    "buildId": "42",
    "downloadUrl": "https://updates.example.com/MyApp/1.2.3/MyApp-1.2.3-full.nupkg",
    "sha256": "9f3a4b1c8d2e5f7a6b3c9d0e1f2a3b4c5d6e7f8a...",
    "minSupportedVersion": "1.0.0",
    "manifestSha256": "...",
    "releaseNotes": "https://github.com/.../releases/tag/v1.2.3",
    "releasedAt": "2026-06-11T08:00:00Z"
  }
}
```

The installer verifies the `sha256` before installing. The
`manifestSha256` is the `DataLifecycleManifest` hash from engine Phase 9;
the installer uses it to know which data migrations to run.

## Data migration flow on update

1. Installer downloads + verifies the new build.
2. Installer prompts the user: "Update to 1.2.3 is available. The update
   will run 3 schema migrations and 1 data sync. Continue?"
3. User clicks Continue.
4. Installer spawns the **new** version of the app in a special
   `--migrate` mode that:
   a. Loads the **new** build's `DataLifecycleManifest.json`.
   b. Calls `IStudioService.Manifest.ValidateAsync` (the manifest's
      `expectedSources` must match the user's configured sources).
   c. Calls `IStudioService.Migrations.BuildPlanAsync` for the new
      schema changes.
   d. Calls `DryRunAsync` + `PreflightAsync` + `EvaluatePolicyAsync`.
   e. For Live / Staging: calls `IGovernanceService.RequestApprovalAsync`
      (the user is shown the approval prompt in the update wizard).
   f. On approval, calls `IMigrationStudioService.ApplyAsync` with the
      HMAC-signed approval token from engine Phase 10.
   g. Records every step in `IBeepAudit`.
5. If the migration fails, the installer **rolls back** the new build
   (keeps the old one running) and reports the failure to the user.
6. If the migration succeeds, the installer replaces the old binary with
   the new one and relaunches the app.

## Folder layout (planned — not yet created)

```
BeepDM.Installer/                               ← NEW project
├── BeepDM.Installer.csproj                     ← WinForms (.net10.0-windows)
├── Program.cs                                  ← WinForms entry; AddBeepStudio() + AddBeepInstaller()
├── Forms/
│   ├── MainForm.cs                             ← the install wizard host
│   ├── WelcomePage.cs                          ← license + intro
│   ├── InstallPathPage.cs
│   ├── FeaturesPage.cs                         ← feature picker
│   ├── DataSourceConfigPage.cs                 ← wraps ISourceService
│   ├── MigrationPage.cs                        ← wraps IMigrationStudioService
│   ├── AccountPage.cs
│   ├── FinishPage.cs
│   └── UpdatePromptForm.cs                     ← the "new version available" prompt
├── Services/
│   ├── UpdateFeedReader.cs                     ← polls the feed
│   ├── UpdateDownloader.cs                     ← background download
│   ├── UpdateInstaller.cs                      ← atomic replace
│   ├── MigrationRunner.cs                      ← wraps IMigrationStudioService
│   ├── RollbackService.cs                      ← restores the previous build
│   └── DeploymentMetadataResolver.cs           ← env-var → git → assembly chain
├── UI/
│   ├── BeepWizardControl.cs                    ← the multi-page wizard container
│   ├── BeepProgressBar.cs                      ← the step progress strip
│   └── Theme/                                  ← branded theme files
├── Resources/
│   ├── Eula.rtf
│   ├── icons/
│   └── banners/
└── docs/
    ├── FIRST-INSTALL.md
    ├── UPDATING.md
    ├── DATA-MIGRATION.md
    └── BRANDING.md
```

## Competitor matrix

See `COMPETITOR-MATRIX.md` for the detailed comparison. The short version:

| Product | What we borrow |
|---|---|
| **InstallShield** | Multi-page wizard UX, prerequisite checks, branded visuals |
| **WiX** | MSI authoring patterns, atomic replace, rollback on failure |
| **Advanced Installer** | Auto-update protocol design, feed format |
| **ClickOnce** | Per-user install simplicity, no admin required |
| **Squirrel.Windows** | Delta updates, "install on next launch" model |
| **Microsoft Visual Studio Installer** | Bootstrapper pattern, prerequisite chain |
| **Inno Setup** | Single-EXE deployment, no separate runtime needed |
| **NSIS** | Scriptable install steps, small footprint |

The installer will **not** ship as an MSI or use WiX — the .NET
self-contained publish + a small launcher gives us 80% of the benefits
with 5% of the complexity. The MSI/EXE packaging is left to the
project's own build pipeline.

## Phases (draft — see `01-phases.md`)

| Phase | Title | Notes |
|---|---|---|
| INST-00 | Overview, scope, architecture | this file |
| INST-01 | WinForms shell + multi-page wizard UX | TBD — depends on the WinForms Studio adapter from engine Phase 8 |
| INST-02 | First-install data-source configuration | wraps engine `ISourceService` |
| INST-03 | First-install schema migration | wraps engine `IMigrationStudioService` |
| INST-04 | Online update feed reader + downloader | poll + verify + atomic replace |
| INST-05 | Update-time data migration | wraps engine `IMigrationStudioService` + `IGovernanceService` |
| INST-06 | Rollback on failure | the previous build stays installed |
| INST-07 | Branding + theming + EULA | commercial-grade UX |
| INST-08 | Silent install for enterprises | `/quiet` + `/source=<json>` flags |
| INST-09 | Telemetry + crash reporting | opt-in, `IBeepAudit` only |

## P-INST-00-01 … P-INST-00-04

- [x] P-INST-00-01 Define the installer's goals + non-goals.
- [x] P-INST-00-02 Sketch the architecture.
- [x] P-INST-00-03 List the engine APIs the installer consumes.
- [x] P-INST-00-04 List the commercial-grade products we benchmark against.

> Phase 0 status: planning only. No code yet. No start date committed.
