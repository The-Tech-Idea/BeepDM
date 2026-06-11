# Future Work — Plans for services outside the Studio engine

> This folder holds **planning docs for services that live outside the Studio
> engine**. They are referenced from the engine plan (Phase 10) and the
> Blazor host plan (Phase 25) but are **not part of the Studio engine itself**.
> They are intentionally separated so each can evolve on its own schedule
> and ship in its own solution.

> Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## Why this folder exists

The Studio engine (Phases 0-10) is the **in-process data-lifecycle
orchestration layer**. It does:

- Source registry + driver provisioning + connection config
- Schema discovery + migration orchestration + data sync
- Governance + audit + approval workflow
- Data lifecycle manifest + deployment metadata + HMAC-signed approval tokens

It explicitly does **not**:

- Orchestrate code or application deployment (no `dotnet publish`, no Docker
  builds, no k8s pushes, no App Service deploys).
- Bundle or install desktop applications.
- Drive a UI installer wizard that copies files to `Program Files`.

These three concerns are real and important — but they belong to **separate
services** that the Studio **integrates with** but does not contain:

| Service | Lives where | Reads from the Studio | Writes to the Studio |
|---|---|---|---|
| **CI/CD service** (this folder) | New sibling project / solution | `BEEP_DEPLOYMENT_METADATA_JSON`, manifest, manifest-validation results | `IBeepAudit` events, `IDeploymentMetadataService.IssueApprovalTokenAsync` |
| **Installer service** (this folder) | New WinForms project (later) | `BEEP_DEPLOYMENT_METADATA_JSON`, manifest, online-update feed | `IBeepAudit` events, Studio's pre-apply + post-apply hooks |

Both services are **downstream consumers** of the Studio. The Studio does
**not** know they exist; the services know the Studio and call into it.

## Folder contents

```
FutureWork/
├── README.md                                ← this file
├── cicd-service/
│   ├── README.md                            ← what the service is, who it serves
│   ├── 00-overview-and-scope.md             ← goals, non-goals, architecture sketch
│   ├── 01-phases.md                         ← the planned phases (draft, no todos yet)
│   └── ARCHITECTURE.md                      ← component diagram + protocol sketch
└── installer-service/
    ├── README.md                            ← what the service is, who it serves
    ├── 00-overview-and-scope.md             ← goals, non-goals, architecture sketch
    ├── 01-phases.md                         ← the planned phases (draft, no todos yet)
    └── COMPETITOR-MATRIX.md                 ← what we borrow from InstallShield / WiX / Advanced Installer / etc.
```

Both sub-folders are **early-stage planning** (no code yet). The CI/CD service
plan is closer to first-PR-ready; the Installer service plan is
aspirational and not committed to a start date.

## Relationship to the engine plan

- **Engine Phase 10** (`10-phase10-deployment-metadata.md`) defines the
  `BEEP_DEPLOYMENT_METADATA_JSON` env var, the HMAC approval token format,
  and the audit-enrichment hook. The CI/CD service **sets** this env var when
  it runs a build; the engine **reads** it.
- **Engine Phase 7** defines the `ApprovalRequest` + `ApprovalDecision` POCOs
  and the `IGovernanceService.RequestApprovalAsync` /
  `DecideApprovalAsync` shape. The CI/CD service **calls** these to gate
  Live applies; the engine **enforces** them.
- **Engine Phase 9** defines the `DataLifecycleManifest` shape. The CI/CD
  service **validates** the manifest as a pre-build gate; the engine
  **reads** it at runtime.
- **Blazor host Phase 25** (`BeepDMS-master-todo-tracker.md`) defines the
  thin MudBlazor shell. The CI/CD service **exposes its own UI** for build
  status / approval workflows (or integrates with the existing host's
  Governance tab).

## Cross-references

| Topic | Engine plan | Future Work plan |
|---|---|---|
| `BEEP_DEPLOYMENT_METADATA_JSON` env var | `10-phase10-deployment-metadata.md` | `cicd-service/00-overview-and-scope.md` |
| `BEEP_APPROVAL_HMAC_KEY` env var | `10-phase10-deployment-metadata.md` | `cicd-service/00-overview-and-scope.md` |
| `DataLifecycleManifest` validation | `09-phase9-data-lifecycle-manifest.md` | `cicd-service/01-phases.md` (CI gate) |
| Approval tokens | `10-phase10-deployment-metadata.md` | `cicd-service/00-overview-and-scope.md` (issue + verify) |
| Audit enrichment | `10-phase10-deployment-metadata.md` | `installer-service/00-overview-and-scope.md` (installer writes audit events) |
| Online update feed | — (not in engine) | `installer-service/00-overview-and-scope.md` |
| Data migration during install/upgrade | — (not in engine) | `installer-service/00-overview-and-scope.md` (uses `IMigrationStudioService` from engine) |

---

## P-FW-01 … P-FW-04

- [x] P-FW-01 Create the `FutureWork/` folder + sub-folders.
- [x] P-FW-02 Write the **CI/CD service** overview + phases.
- [x] P-FW-03 Write the **Installer service** overview + phases.
- [x] P-FW-04 Link both plans from the engine plan and the Blazor host plan.

> Future-work status: planning only. No code yet. No start date committed.
