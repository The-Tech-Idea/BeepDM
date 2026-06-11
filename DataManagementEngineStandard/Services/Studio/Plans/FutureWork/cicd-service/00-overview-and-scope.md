# CI/CD Service — Overview, Scope, and Architecture

> **Status:** planning only. No code yet. Target: a separate solution
> (`BeepDM.Cicd.sln`) with its own projects.

> Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## What this service is

A standalone **CI/CD orchestration service** for the Beep ecosystem. It:

1. Watches the data-platform team's repos for changes (GitHub / GitLab / Azure DevOps).
2. Triggers builds when the `DataLifecycleManifest.json` (engine Phase 9) or
   the project's `*.csproj` changes.
3. Runs the engine's data-lifecycle checks as **build steps** (manifest
   validation, migration dry-run, preflight, policy evaluation, approval-token
   issuance).
4. Publishes build artifacts (NuGet packages, Docker images, app binaries)
   to the target feed.
5. Drives **Live / Staging migrations** through the engine's
   `IGovernanceService.RequestApprovalAsync` + `IMigrationStudioService.ApplyAsync`
   with the deployment metadata (engine Phase 10) injected as
   `BEEP_DEPLOYMENT_METADATA_JSON`.
6. Records every build + every data-migration step into the engine's
   `IBeepAudit` pipeline so the audit log has a single, time-ordered view of
   "what code was built, when, by whom, and what data did it touch."

## Why this is a separate service

- The engine (Phase 0-10) is **in-process**. It does not have a scheduler, a
  build queue, an artifact store, or a webhook receiver. Adding those to
  the engine would conflate "data-lifecycle orchestration" with "release
  orchestration" — and the user has explicitly excluded deployment
  orchestration from the engine's scope.
- A CI/CD service benefits from being a **long-running daemon** with its own
  state, queues, and DB. Putting it in the engine would force every Blazor
  / WinForms / WPF / Maui host to also be a CI/CD runner.
- The user wants the option to **swap CI/CD vendors** (GitHub Actions,
  GitLab CI, Azure DevOps, Jenkins) without touching the engine. A standalone
  service is the right abstraction layer.

## What it is NOT

- **Not a replacement for GitHub Actions / GitLab CI / Azure DevOps.** It
  *uses* those as the trigger source. The service is the Beep-specific
  layer that knows about the manifest, the approval workflow, and the
  audit log.
- **Not a code-deployment tool.** It does not push Docker images to
  registries, does not deploy to k8s, does not roll forward app
  deployments. The downstream CI/CD system handles the actual app deploy.
  This service only orchestrates the **data** part of the release.
- **Not a build server.** It does not compile code. The downstream CI/CD
  system (GitHub Actions, etc.) is the build server. This service receives
  webhook events and drives data-migration steps.
- **Not a multi-tenant SaaS.** It is a single-tenant service deployed
  alongside the data-platform team's tooling.

## Goals

1. **One source of truth for "what code revision triggered what data change."**
   The audit log answers: "Who applied this migration to Live? Against
   which code revision? With which approver? With which build id?"
2. **Make the engine's data-lifecycle checks CI-gateable.** A pull request
   that violates the manifest fails CI before merge.
3. **Drive Live / Staging migrations from a button or a webhook** with
   the right deployment metadata automatically populated.
4. **Surfaces everything in the existing audit log** — the CI/CD service
   is just another audit producer; the engine's `IBeepAudit` is the only
   audit store.

## Non-goals (explicitly out of scope)

- Code building, code packaging, code deployment.
- App-binary signing, code-signing certificates.
- Source-control hosting (the service reads from GitHub / GitLab / Azure
  DevOps via their APIs; it does not host the source itself).
- Multi-tenant SaaS billing / quotas.
- Real-time build-streaming UI (the service emits `IBeepAudit` events;
  the existing audit UI surfaces them).
- Server-side artifact caching (use the downstream CI/CD system's cache).

## Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                      BeepDM.Cicd Service                            │
│   (standalone .NET worker process; can be hosted in Docker / k8s)   │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐    │
│  │  WebhookReceiver  (GitHub / GitLab / Azure DevOps / Jenkins) │    │
│  │  → enqueues a BuildJob into JobQueue                         │    │
│  └──────────────────────────────────────────────────────────────┘    │
│                              ▼                                       │
│  ┌──────────────────────────────────────────────────────────────┐    │
│  │  JobQueue  (Channel<BuildJob> + persistent on-disk journal)  │    │
│  └──────────────────────────────────────────────────────────────┘    │
│                              ▼                                       │
│  ┌──────────────────────────────────────────────────────────────┐    │
│  │  BuildJobProcessor  (BackgroundService; dequeues)            │    │
│  │                                                              │    │
│  │  For each job:                                               │    │
│  │   1. Resolve BEEP_DEPLOYMENT_METADATA_JSON from the trigger  │    │
│  │   2. Call engine's IDataLifecycleManifestService             │    │
│  │      → ValidateAsync(manifest) → fail-fast                   │    │
│  │   3. For each pending migration:                             │    │
│  │      a. Call engine's IMigrationStudioService                │    │
│  │         → BuildPlanAsync(planRequest)                         │    │
│  │      b. → DryRunAsync + PreflightAsync → assert pass         │    │
│  │      c. → EvaluatePolicyAsync → assert allowed               │    │
│  │      d. For Live / Staging:                                  │    │
│  │         → IGovernanceService.RequestApprovalAsync            │    │
│  │         → wait for human / auto-approval (webhook poll)      │    │
│  │         → IDeploymentMetadataService.IssueApprovalTokenAsync  │    │
│  │      e. → ApplyAsync(plan, policy, token, progress)          │    │
│  │   4. Record every step into IBeepAudit                        │    │
│  │   5. Report status to WebhookSender                          │    │
│  └──────────────────────────────────────────────────────────────┘    │
│                              ▼                                       │
│  ┌──────────────────────────────────────────────────────────────┐    │
│  │  WebhookSender  (GitHub commit status / GitLab MR / Azure    │    │
│  │  DevOps build result / Slack / email)                        │    │
│  └──────────────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────────────┘
```

## Components (planned)

| Component | Purpose |
|---|---|
| `WebhookReceiver` | Listens for `push` / `pull_request` / `release` events from GitHub / GitLab / Azure DevOps / Jenkins. Validates the webhook signature. Enqueues a `BuildJob` into `JobQueue`. |
| `BuildJob` POCO | `{ JobId, Repository, Ref, Sha, TriggeredBy, TriggeredAt, ManifestPath, Migrations[], ApprovalPolicy }` |
| `JobQueue` | `Channel<BuildJob>` with a persistent journal (NDJSON append-only log) for crash recovery. |
| `BuildJobProcessor` | `BackgroundService` that dequeues jobs and drives them through the engine's services. |
| `ApprovalWatcher` | Long-poll or webhook for the engine's approval-state changes. For Live / Staging, blocks the build until a human approves. |
| `WebhookSender` | Posts the build's status back to GitHub / GitLab / Azure DevOps (commit status, MR comment, build result). Also posts to Slack / email for human-readable summaries. |
| `CicdOptions` | DI options: webhook secrets, queue size, retry policy, poll intervals, target environment, etc. |
| `CicdService` | Top-level facade that hosts the components. |

## Engine integration (what the service calls)

| Engine API (from Phases 0-10) | What the service does with it |
|---|---|
| `IStudioService.Manifest.ValidateAsync(manifest)` | Pre-build gate. Fails the build on any `Error` severity issue. |
| `IStudioService.Migrations.BuildPlanAsync(planRequest)` | Builds the plan for each pending migration. |
| `IStudioService.Migrations.DryRunAsync(handle)` | Pre-build gate on the dry-run report. |
| `IStudioService.Migrations.PreflightAsync(handle)` | Pre-build gate on the preflight report. |
| `IStudioService.Migrations.EvaluatePolicyAsync(handle)` | Pre-build gate on the policy evaluation. |
| `IStudioService.Governance.RequestApprovalAsync(request)` | For Live / Staging: create an approval request. |
| `IStudioService.Deployment.IssueApprovalTokenAsync(request, deployment)` | Mint an HMAC-signed token for the approval. |
| `IStudioService.Migrations.ApplyAsync(handle, policy, token, progress)` | Run the migration. |
| `IStudioService.QueryAuditAsync(query)` | Read the audit log to drive the webhook sender's commit-status messages. |
| `IStudioService.Sync.*` (Phase 6) | Drive data-sync runs the same way as migrations. |
| Engine's `IBeepAudit` (via `IStudioService.QueryAuditAsync` or directly) | The service is just another audit producer. |

## Environment variables the service sets

| Var | Value | Why |
|---|---|---|
| `BEEP_DEPLOYMENT_METADATA_JSON` | The full `DeploymentMetadata` (ref, sha, buildId, buildUrl, version, builtAt, labels) | The engine reads it first in the resolution chain (engine Phase 10). |
| `BEEP_APPROVAL_HMAC_KEY` | The CI/CD's HMAC key (fetched from a secret store) | Required for signing approval tokens. The service signs them; the engine verifies them. |
| `BEEP_MANIFEST_PATH` | The absolute path to the project's `beep/data-lifecycle-manifest.json` | Tells the engine where to find the manifest in this build's checkout. |

## Folder layout (planned — not yet created)

```
BeepDM.Cicd.sln                                  ← NEW solution
├── src/
│   ├── BeepDM.Cicd.Core/                        ← contracts, options, BuildJob POCO
│   ├── BeepDM.Cicd.Webhooks/                    ← webhook receiver + sender
│   ├── BeepDM.Cicd.Worker/                      ← BackgroundService host (Program.cs + AddBeepCicd())
│   └── BeepDM.Cicd.Providers/                   ← per-CI-provider adapters (GitHub, GitLab, Azure DevOps, Jenkins)
├── tests/
│   └── BeepDM.Cicd.Tests/
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml                       ← service + the engine's BeepDMS.Api
└── docs/
    ├── OPERATIONS.md
    └── PROVIDER-SETUP.md
```

## Phases (draft — see `01-phases.md`)

| Phase | Title | Notes |
|---|---|---|
| CICD-00 | Overview, scope, architecture | this file |
| CICD-01 | Webhook receiver + signature validation | GitHub first, then GitLab + Azure DevOps |
| CICD-02 | Job queue + persistent journal | crash-recovery on the `Channel<BuildJob>` |
| CICD-03 | Engine integration + manifest pre-build gate | `IStudioService` reference |
| CICD-04 | Migration + sync pre-build gates | dry-run + preflight + policy |
| CICD-05 | Live / Staging approval flow | human-in-the-loop via webhook poll |
| CICD-06 | Webhook sender (commit status, MR comment) | status reflects audit log |
| CICD-07 | Slack + email notifications | secondary surface |
| CICD-08 | Provider adapters (GitHub / GitLab / Azure DevOps / Jenkins) | one per provider |
| CICD-09 | Docker + operations docs | ship the service |

## P-CICD-00-01 … P-CICD-00-04

- [x] P-CICD-00-01 Define the service's goals + non-goals.
- [x] P-CICD-00-02 Sketch the component diagram + protocol.
- [x] P-CICD-00-03 List the engine APIs the service consumes.
- [x] P-CICD-00-04 List the env vars the service sets.

> Phase 00 status: planning only. No code yet.
