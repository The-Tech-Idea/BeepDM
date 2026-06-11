# CI/CD Service — Planned Phases

> **Status:** draft only. No code yet. See `00-overview-and-scope.md` for the
> service's goals, non-goals, and architecture.

> Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## How to read this

Each phase below is a **coherent unit of work** that lands in a single PR.
Phases are sequenced so each builds on the previous; no parallel work is
planned. Total: **9 phases** + the overview (Phase 0).

The phases reference engine code from
`C:\Users\f_ald\source\repos\The-Tech-Idea\BeepDM\DataManagementEngineStandard\Services\Studio\`.
The CI/CD service does **not** fork the engine; it consumes the engine via
the `IStudioService` facade (engine Phase 1).

---

## Phase 0 — Overview & Scope · [`00-overview-and-scope.md`](./00-overview-and-scope.md)

- [x] P-CICD-00-01 Define the service's goals + non-goals.
- [x] P-CICD-00-02 Sketch the component diagram + protocol.
- [x] P-CICD-00-03 List the engine APIs the service consumes.
- [x] P-CICD-00-04 List the env vars the service sets.

> Phase 0 status: doc only.

---

## Phase 1 — Webhook Receiver + Signature Validation

**Goal:** the service can receive a webhook from GitHub (first provider) and
validate the signature.

**Scope:**
- New project `BeepDM.Cicd.Webhooks` with `IWebhookReceiver` + `GitHubWebhookReceiver`.
- HMAC-SHA256 signature validation against a per-repository secret.
- Parses the GitHub `push` / `pull_request` / `release` events into a
  `BuildJob` POCO.
- Enqueues the `BuildJob` into the (Phase 2) `JobQueue`.
- DI registration: `AddBeepCicd(opts => opts.WebhookSecret = "...")`.
- A test webhook endpoint at `POST /webhooks/github` (in the Worker host).
- A `dotnet user-secrets` pattern for local dev.

**Exit criteria:**
- A real GitHub webhook pointing at the local dev URL is accepted and a
  `BuildJob` appears in the queue.
- A tampered webhook (wrong signature) returns `401 Unauthorized`.

**Engine dependency:** none. The receiver does not touch the engine yet.

---

## Phase 2 — Job Queue + Persistent Journal

**Goal:** `BuildJob`s survive a service restart.

**Scope:**
- `JobQueue` backed by a `Channel<BuildJob>` with a persistent journal
  (NDJSON append-only log under `Options.DataRoot/build-journal.ndjson`).
- On startup, the queue re-hydrates from the journal.
- On `IHostApplicationLifetime.ApplicationStopping`, the journal is flushed.
- Concurrency model: 1 writer, N readers (the processors are the readers).
- Backpressure: `BoundedChannelFullMode.Wait` with a 100-job capacity.
- Tests: a crash mid-job is recovered on restart; a duplicate enqueue is
  detected and de-duplicated by `(Repository, Ref, Sha)`.

**Exit criteria:**
- A job in the queue at the time of `kill -9` is re-processed on restart.
- The same `(Repo, Ref, Sha)` is not enqueued twice.

**Engine dependency:** none.

---

## Phase 3 — Engine Integration + Manifest Pre-Build Gate

**Goal:** the service calls the engine's `IStudioService` for the first time.

**Scope:**
- New project `BeepDM.Cicd.Core` with the `BuildJobProcessor` skeleton.
- Reference `DataManagementEngineStandard` NuGet; call `AddBeepStudio(...)`
  + `AddBeepBlazorStudio()` (or a new `AddBeepCicdStudio()`).
- For each enqueued job:
  1. Set `BEEP_DEPLOYMENT_METADATA_JSON` from the trigger.
  2. Set `BEEP_MANIFEST_PATH` to the checkout's manifest.
  3. Call `IStudioService.Manifest.ValidateAsync(manifest)`.
  4. Fail the build on any `Error` severity issue; record the failure
     into `IBeepAudit`.
- `BuildJobProcessor` is a `BackgroundService`; one process per host.
- Tests: a manifest with `forbidDestructiveInLive = true` + a `DropEntity`
  plan fails the build with the right error code.

**Exit criteria:**
- A push to a repo with an invalid manifest fails the build.
- The failure appears in the engine's `IBeepAudit` log.

**Engine dependency:** Phases 1, 5, 7, 9, 10 (contracts + manifest + deployment metadata).

---

## Phase 4 — Migration + Sync Pre-Build Gates

**Goal:** before any data movement, the build runs the engine's dry-run +
preflight + policy checks.

**Scope:**
- Extend `BuildJobProcessor` to:
  1. Call `IStudioService.Migrations.BuildPlanAsync(planRequest)`.
  2. Call `DryRunAsync` + `PreflightAsync` + `EvaluatePolicyAsync`.
  3. If all pass: enqueue the plan for apply (Phase 5).
  4. If any fail: fail the build, record the issue, surface to the user.
- Same flow for `IStudioService.Sync` (Phase 6).
- Configuration: `Options.MigrationsToRun` lets the user declare which
  migrations to gate (e.g. "all migrations in `Migrations/v2/`").
- Tests: a plan that fails the dry-run stops the build.

**Exit criteria:**
- A push that would drop a column in Live fails the build (manifest's
  `forbidDestructiveInLive` + plan's `DropColumn` op).
- A push that violates the preflight check (e.g. a missing source alias)
  fails the build.

**Engine dependency:** Phases 1, 5, 6, 7, 9, 10.

---

## Phase 5 — Live / Staging Approval Flow

**Goal:** a Live / Staging apply requires a human approver; the service
blocks until the approver decides.

**Scope:**
- Extend `BuildJobProcessor` to:
  1. Detect the target env tier (`Live` or `Staging` → approval required).
  2. Call `IStudioService.Governance.RequestApprovalAsync(request)`.
  3. Long-poll `IGovernanceService.GetApprovalAsync(approvalId)` until
     the state is `Approved` / `Rejected` / `Expired`.
  4. On `Approved`: call `IStudioService.Deployment.IssueApprovalTokenAsync`
     and continue to `ApplyAsync`.
  5. On `Rejected` or `Expired`: fail the build, record the decision in
     `IBeepAudit`.
- Webhook endpoint: `POST /webhooks/approval/{approvalId}` lets an external
  approver click "Approve" in a Blazor host (e.g. the existing Phase 25
  shell) and the webhook signals the build to continue.
- Tests: a Live apply blocks until the test approval webhook fires.

**Exit criteria:**
- A Live apply without an approver blocks indefinitely.
- A Live apply with an approver who clicks "Approve" continues within 5s
  of the webhook firing.
- The approver's identity is recorded in `IBeepAudit`.

**Engine dependency:** Phases 1, 5, 7, 9, 10.

---

## Phase 6 — Webhook Sender (commit status, MR comment)

**Goal:** the build's status is reflected back on the source-control system.

**Scope:**
- `WebhookSender` posts the build's status to GitHub's commit status API
  (`POST /repos/{owner}/{repo}/statuses/{sha}`) at every state transition
  (`pending` → `success` / `failure` / `error`).
- A second sender posts an MR comment with a summary table (manifest
  validation, dry-run, preflight, policy, approval, apply status).
- The summary is built from `IStudioService.QueryAuditAsync` so the comment
  is **derived from the audit log**, not a separate text string.
- Tests: a `success` build posts a `success` commit status; a `failure`
  build posts a `failure` commit status with a link to the audit log.

**Exit criteria:**
- A successful Live apply shows a green check on the commit in GitHub.
- A failed Live apply shows a red X on the commit in GitHub.
- The MR comment contains a link to the full audit log.

**Engine dependency:** Phases 1, 5, 7, 9, 10.

---

## Phase 7 — Slack + Email Notifications

**Goal:** the build's status is also posted to Slack (and optionally email).

**Scope:**
- `SlackWebhookSender` posts a message to a per-channel Slack webhook URL.
- `EmailSender` posts via SMTP (configurable).
- Both senders are **opt-in** via `Options.Notifications.Slack.Enabled` /
  `Options.Notifications.Email.Enabled`.
- The message format is the same as the MR comment, so users get the same
  info regardless of channel.
- Tests: a build failure posts a Slack message with a clear subject.

**Exit criteria:**
- A failed Live apply posts a Slack message within 30s of the failure.
- The Slack message contains a link to the full audit log.

**Engine dependency:** none (purely CI/CD-side).

---

## Phase 8 — Provider Adapters (GitHub / GitLab / Azure DevOps / Jenkins)

**Goal:** the same service works against any of the four major CI systems.

**Scope:**
- New project `BeepDM.Cicd.Providers` with one adapter per provider:
  - `GitHubProvider` (the reference impl from Phases 1-7).
  - `GitLabProvider` (uses GitLab's `push` / `merge_request` webhooks;
    status posted to the MR API).
  - `AzureDevOpsProvider` (uses Azure DevOps' `git.push` /
    `git.pullrequest.created` webhooks; status posted to the build API).
  - `JenkinsProvider` (uses Jenkins' `git` / `ghprb` webhooks; status
    posted as a Jenkins build result).
- All four providers implement `IWebhookReceiver` + `IWebhookSender`.
- DI registration: `services.AddBeepCicd(opts => opts.Provider = "github")`.
- Tests: one test per provider, using a recorded webhook payload.

**Exit criteria:**
- The same `BuildJob` schema works against all four providers.
- The same audit-log-derived commit status works against all four.

**Engine dependency:** none.

---

## Phase 9 — Docker + Operations Docs

**Goal:** the service is shippable as a Docker image with clear operations docs.

**Scope:**
- Multi-stage `Dockerfile` (SDK → runtime).
- `docker-compose.yml` that includes the CI/CD service + the engine's
  `BeepDMS.Api` (engine Phase 1's API surface) + a SQLite volume for
  the audit log.
- `docs/OPERATIONS.md`:
  - How to set `BEEP_DEPLOYMENT_METADATA_JSON` and `BEEP_APPROVAL_HMAC_KEY`
    via Docker secrets or Kubernetes secrets.
  - How to rotate the HMAC key.
  - How to back up the audit log.
  - How to scale the service (1-N replicas behind a shared journal).
  - How to recover from a corrupted journal.
- `docs/PROVIDER-SETUP.md`:
  - Step-by-step for each provider (GitHub, GitLab, Azure DevOps, Jenkins):
    how to register the webhook, how to set the secret, how to grant the
    status-posting permissions.
- Tests: a `docker compose up` of the service + `BeepDMS.Api` + a
  SQLite volume passes a smoke test (a fake GitHub webhook is accepted,
  processed, and the resulting audit event is visible).

**Exit criteria:**
- `docker compose up` brings up the service in < 30s.
- The operations docs answer the 10 most likely "how do I…" questions.

**Engine dependency:** none.

---

## Cumulative counts

| Phase | Complete | In Progress | Remaining |
|---|---|---|---|
| Phase 0 — Overview | 4 | 0 | 0 |
| Phase 1 — Webhook receiver | 0 | 0 | ~8 |
| Phase 2 — Job queue | 0 | 0 | ~6 |
| Phase 3 — Engine integration | 0 | 0 | ~8 |
| Phase 4 — Migration / sync gates | 0 | 0 | ~8 |
| Phase 5 — Approval flow | 0 | 0 | ~8 |
| Phase 6 — Webhook sender | 0 | 0 | ~6 |
| Phase 7 — Slack + email | 0 | 0 | ~4 |
| Phase 8 — Provider adapters | 0 | 0 | ~12 |
| Phase 9 — Docker + ops docs | 0 | 0 | ~6 |
| **Total (CI/CD service)** | **4** | **0** | **~66** |

> The per-phase todos are **drafts**; finalise them when the first phase is
> started. The above is a planning estimate, not a contract.
