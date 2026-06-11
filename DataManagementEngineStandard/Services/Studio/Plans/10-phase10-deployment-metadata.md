# Phase 10 — Deployment Metadata + Approval Tokens (`IDeploymentMetadataService`)

> **Scope:** implement `IDeploymentMetadataService` — the Studio's code-revision
> resolver. The service captures the **code revision** the Studio is running
> against, enriches every audit event with it, and issues **HMAC-signed approval
> tokens** that bind an approval to a specific code revision. We do **not**
> orchestrate deployments; we just record the metadata that ties a data change
> to a code change.

> Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## Why this phase

Once we have the `DataLifecycleManifest` (Phase 9) declaring which data sources
a code revision expects, the **next** question is: "which code revision is the
Studio currently running against?" Without this, a DBA who approves a migration
at 09:00 against revision `9f3a4b1` could see the same approval replayed
against revision `2c7e8f9` at 09:05 if the host restarts with a new build.

This phase adds:

1. **`DeploymentMetadata` resolution** — a small, well-defined lookup chain
   (env var → manifest → git rev-parse → assembly version).
2. **Audit enrichment** — every `IBeepAudit` event is enriched with the
   `DeploymentMetadata` so the audit trail can answer "who did what to which
   data, running against which code."
3. **HMAC-signed approval tokens** — every approval request is bound to a
   specific `DeploymentMetadata` via a signed token. The token is verified
   on every apply; a token issued for revision A cannot be replayed against
   revision B.
4. **NO deployment orchestration.** The service resolves metadata. It does
   not build, deploy, push, or publish anything. The CI/CD system (Phase 23
   of the Blazor workspace plan) reads the manifest and the deployment
   metadata; it does not ask the Studio to do anything.

## Public surface (this phase fills in)

```csharp
// Contracts/IDeploymentMetadataService.cs
public interface IDeploymentMetadataService
{
    Task<StudioResult<DeploymentMetadata>> GetCurrentAsync(CancellationToken ct = default);
    void Override(DeploymentMetadata? metadata);
    Task<StudioResult<ApprovalToken>> IssueApprovalTokenAsync(ApprovalTokenRequest request, CancellationToken ct = default);
    Task<StudioResult<ApprovalTokenClaims>> VerifyApprovalTokenAsync(string token, CancellationToken ct = default);
}
```

## Models (declared in Phase 1, documented here)

| Type | Purpose |
|---|---|
| `DeploymentMetadata` | `codeRevisionRef`, `codeRevisionSha`, `buildId`, `buildUrl`, `version`, `builtAt`, `labels` |
| `CodeRevisionRef` | `ref` ("refs/heads/main") + `sha` (full git SHA) |
| `ApprovalTokenRequest` | `approvalId`, `planHash`, `tier`, `issuedAt`, `lifetime` |
| `ApprovalTokenClaims` | `approvalId`, `planHash`, `tier`, `codeRevisionRef`, `codeRevisionSha`, `issuedAt`, `expiresAt` |
| `ApprovalToken` | The `token` string (HMAC-signed base64url) + `claims` + `issuedAt` + `expiresAt` |

## Folder layout (this phase creates)

```
Services/Studio/
├── Contracts/IDeploymentMetadataService.cs          ← DONE in Phase 1
├── Models/  (all the records above — DONE in Phase 1)
└── Deployment/
    ├── DeploymentMetadataService.cs                 ← implements IDeploymentMetadataService
    ├── CodeRevisionResolver.cs                       ← the env-var → manifest → git → assembly chain
    ├── GitRevisionReader.cs                          ← `git rev-parse` + `git symbolic-ref`
    ├── AssemblyVersionReader.cs                      ← reads AssemblyInformationalVersion + build timestamp
    ├── DeploymentMetadataEnricher.cs                 ← IEnricher for IBeepAudit
    ├── ApprovalTokenIssuer.cs                        ← HMAC-SHA256 signing
    ├── ApprovalTokenVerifier.cs                      ← verifies signature + expiry + claims
    ├── HmacKeyProvider.cs                            ← reads BEEP_APPROVAL_HMAC_KEY or generates ephemeral
    └── DeploymentMetadataEnricherRegistration.cs     ← wires the enricher into AddBeepAudit()
```

## Resolution chain

`CodeRevisionResolver.GetCurrent()` tries, in order:

| # | Source | Example |
|---|---|---|
| 1 | `BEEP_DEPLOYMENT_METADATA_JSON` env var | `{"codeRevisionRef":"refs/tags/v1.2.3","codeRevisionSha":"...","buildId":"42","buildUrl":"https://ci/...","version":"1.2.3","builtAt":"2026-06-11T08:00:00Z"}` |
| 2 | `BEEP_CODE_REVISION_SHA` + `BEEP_CODE_REVISION_REF` env vars | (the two are joined into a `DeploymentMetadata` with `buildId` from `BEEP_BUILD_ID`) |
| 3 | The manifest's `project.codeRevision` block (Phase 9) | |
| 4 | `git rev-parse HEAD` + `git symbolic-ref HEAD` (in a git repo) | `refs/heads/main` + `9f3a4b1c...` |
| 5 | Assembly `InformationalVersion` + build timestamp | |

The first non-empty source wins. The resolver **never falls through silently** —
if step 4 fails (not a git repo), the resolver returns the manifest's
`codeRevision` (step 3) or the assembly version (step 5) and **logs a warning**
via `IBeepLog` that the resolution was not the most precise.

## HMAC approval tokens

`ApprovalTokenIssuer` signs a token with **HMAC-SHA256**. The token is a
base64url-encoded JSON payload:

```
base64url(header) . base64url(payload) . base64url(hmac)
```

**Header:**
```json
{ "alg": "HS256", "typ": "beep-approval-v1" }
```

**Payload (= `ApprovalTokenClaims`):**
```json
{
  "approvalId": "apr_9f3a4b1c",
  "planHash": "9f3a4b1c8d2e5f7a...",
  "tier": "Live",
  "codeRevisionRef": "refs/heads/main",
  "codeRevisionSha": "9f3a4b1c8d2e5f7a6b3c9d0e1f2a3b4c5d6e7f8a",
  "issuedAt": "2026-06-11T08:00:00Z",
  "expiresAt": "2026-06-11T08:15:00Z"
}
```

**HMAC input:** `base64url(header) . base64url(payload)`
**HMAC key:** read from `BEEP_APPROVAL_HMAC_KEY` env var (or generated
ephemerally in dev; ephemeral keys log a warning).

`ApprovalTokenVerifier` checks:
1. The HMAC signature (constant-time comparison).
2. The expiry (`expiresAt > now`).
3. The `codeRevisionSha` matches the **current** `DeploymentMetadata` (this
   is the key check — a token issued for revision A cannot be replayed
   against revision B).
4. The `tier` matches the request's tier.
5. The `approvalId` matches the request's approval id.

If any check fails, the verifier returns
`StudioResult.Fail(StudioErrorCode.ApprovalTokenInvalid, ...)`.

## Audit enrichment

`DeploymentMetadataEnricher` is an `IEnricher<StudioAuditEvent>` registered
into the engine's existing `IBeepAudit` pipeline (see
`BeepDM/DataManagementEngineStandard/Services/Telemetry/IEnricher.cs`).

For every event, the enricher adds a `deployment` field to the `Labels`
dictionary:

```json
{
  "deployment": {
    "codeRevisionRef": "refs/heads/main",
    "codeRevisionSha": "9f3a4b1c8d2e5f7a6b3c9d0e1f2a3b4c5d6e7f8a",
    "buildId": "42",
    "buildUrl": "https://ci.example.com/builds/42",
    "version": "1.2.3"
  }
}
```

The `BeepServiceExtensions.Studio.cs` registration call wires the enricher
into the `IBeepAudit` pipeline (using the engine team's `AddBeepAudit` from
`BeepServiceExtensions.Audit.cs`):

```csharp
services.AddBeepAudit(opts =>
{
    opts.Enabled = !studioOptions.DisableAudit;
    opts.Enrichers.Add(new DeploymentMetadataEnricher(deploymentService));
    // ... other opts
});
```

The enricher is **always on** unless `StudioOptions.EnrichAuditWithDeploymentMetadata = false`
(default is `true`).

## Cross-cutting

- The HMAC key MUST come from a secret store, not from `appsettings.json`. The
  default provider reads from `BEEP_APPROVAL_HMAC_KEY`; in production the
  CI/CD system is expected to set this. In dev, an ephemeral key is
  generated and a warning is logged.
- The verifier's `codeRevisionSha` check is what makes tokens non-replayable.
  If the host's `DeploymentMetadata` is missing, the verifier returns
  `StudioResult.Fail(StudioErrorCode.DeploymentMetadataMissing, ...)` and the
  approval cannot be applied.
- The token is **stateless** — the issuer does not store it. The verifier
  needs only the token, the HMAC key, and the current `DeploymentMetadata`.
  This means tokens work across host restarts and across process boundaries
  (e.g. a CLI that issues a token and a Blazor Server that verifies it).

## Out of scope (explicitly)

- **Deployment orchestration.** The service does not run `dotnet publish`,
  does not build Docker images, does not push to a registry, does not deploy
  to Kubernetes or App Service. It only **reads** the deployment metadata
  that a CI/CD system has set.
- **Build triggering.** The service does not trigger a build. It is a
  consumer of build metadata.
- **Multi-revision tokens.** A token is bound to one `codeRevisionSha`. If
  the code changes, the token is invalid. This is intentional — it forces
  the DBA to re-approve after every code change.

---

## Todo Tracker

| # | Task | Status | Notes |
|---|------|--------|-------|
| P10-01 | `Deployment/HmacKeyProvider.cs` — reads `BEEP_APPROVAL_HMAC_KEY` or generates ephemeral | ⬜ | |
| P10-02 | `Deployment/GitRevisionReader.cs` — `git rev-parse` + `git symbolic-ref` | ⬜ | |
| P10-03 | `Deployment/AssemblyVersionReader.cs` — reads `AssemblyInformationalVersion` + build timestamp | ⬜ | |
| P10-04 | `Deployment/CodeRevisionResolver.cs` — the 5-step chain | ⬜ | |
| P10-05 | `Deployment/ApprovalTokenIssuer.cs` — HMAC-SHA256 signing | ⬜ | |
| P10-06 | `Deployment/ApprovalTokenVerifier.cs` — signature + expiry + claims checks | ⬜ | |
| P10-07 | `Deployment/DeploymentMetadataEnricher.cs` — `IEnricher<StudioAuditEvent>` | ⬜ | |
| P10-08 | `Deployment/DeploymentMetadataService.cs` — implements `IDeploymentMetadataService` | ⬜ | |
| P10-09 | `Deployment/DeploymentMetadataEnricherRegistration.cs` — wires the enricher into `AddBeepAudit` | ⬜ | |
| P10-10 | Wire `IDeploymentMetadataService` into `AddBeepStudio()` (already done in Phase 1; this task verifies the DI binding) | ⬜ | |
| P10-11 | Modify `IGovernanceService.DecideApprovalAsync` (Phase 7) to issue an `ApprovalToken` instead of just flipping the state | ⬜ | Cross-phase wiring |
| P10-12 | Modify `IMigrationStudioService.ApplyAsync` (Phase 5) to require a valid `ApprovalToken` for Live / Staging tiers | ⬜ | Cross-phase wiring |
| P10-13 | Modify `ISyncStudioService.EnqueueRunAsync` (Phase 6) to require a valid `ApprovalToken` for Live / Staging tiers | ⬜ | Cross-phase wiring |
| P10-14 | Tests: `CodeRevisionResolverTests` (3+), `GitRevisionReaderTests` (2+, with a temp git repo), `HmacKeyProviderTests` (2+), `ApprovalTokenIssuerTests` (3+), `ApprovalTokenVerifierTests` (5+ — including the replay protection check), `DeploymentMetadataEnricherTests` (2+) | ⬜ | |
| P10-15 | Document: how to set `BEEP_APPROVAL_HMAC_KEY` + `BEEP_DEPLOYMENT_METADATA_JSON` in CI (GitHub Actions / GitLab / Azure DevOps) | ⬜ | |
| P10-16 | Update `00-overview-and-scope.md` + `MASTER-TODO-TRACKER.md` to mark Phase 10 done | ⬜ | |

---

## Validation (definition of done)

- [ ] `dotnet build DataManagementEngineStandard` succeeds with **0 errors**.
- [ ] `CodeRevisionResolver.GetCurrent()` in a git repo returns the current SHA + ref.
- [ ] `CodeRevisionResolver.GetCurrent()` with `BEEP_DEPLOYMENT_METADATA_JSON` set returns the env-var values.
- [ ] `ApprovalTokenIssuer.IssueApprovalTokenAsync` produces a valid token whose claims round-trip.
- [ ] `ApprovalTokenVerifier.VerifyApprovalTokenAsync` accepts a freshly issued token.
- [ ] `ApprovalTokenVerifier.VerifyApprovalTokenAsync` **rejects** a token when the `codeRevisionSha` doesn't match the current `DeploymentMetadata` (replay protection).
- [ ] `ApprovalTokenVerifier.VerifyApprovalTokenAsync` **rejects** an expired token.
- [ ] `ApprovalTokenVerifier.VerifyApprovalTokenAsync` **rejects** a token with a tampered payload (signature mismatch).
- [ ] `DeploymentMetadataEnricher` adds the `deployment` field to every `StudioAuditEvent`.
- [ ] `IMigrationStudioService.ApplyAsync` (mocked) refuses to apply to Live without a valid `ApprovalToken`.
- [ ] All 17+ new tests pass.

---

## Pitfalls

1. **Don't put the HMAC key in `appsettings.json`** — read it from `BEEP_APPROVAL_HMAC_KEY` env var (CI sets it) or from a `IKeychainProvider` (Phase 3's keychain abstraction).
2. **Don't use a non-constant-time comparison for the HMAC** — use `CryptographicOperations.FixedTimeEquals`.
3. **Don't use a hash weaker than SHA-256** — HS256 is the minimum; HS384/HS512 are acceptable. MD5 / SHA-1 are not.
4. **Don't trust the token's `codeRevisionSha` blindly** — the verifier always re-resolves the current `DeploymentMetadata` and compares. The token's value is for **the user** to see; the verification is against the **live** value.
5. **Don't skip the enricher registration** — every audit event must carry the deployment metadata. The wiring in `DeploymentMetadataEnricherRegistration.cs` is load-bearing; a misconfiguration silently drops the field.
6. **Don't allow the deploy service to override the manifest's `codeRevision`** — env vars win (steps 1-2 in the chain) so CI can override the manifest, but the manifest is the source of truth in dev.
7. **Don't add deployment orchestration to this phase** — the service is read-only. If a CI step is needed, the CI calls the engine's audit / manifest APIs, not this service.

---

## Related

- Phase 01 — contracts (this phase implements `IDeploymentMetadataService`)
- Phase 07 — governance (the approval workflow issues + verifies tokens)
- Phase 09 — data lifecycle manifest (the manifest's `codeRevision` is one source in the resolution chain)
- `BeepDM/DataManagementEngineStandard/Services/Telemetry/IEnricher.cs` — the engine's `IEnricher` contract that the deployment enricher implements
- `BeepDM/DataManagementEngineStandard/Services/Audit/BeepServiceExtensions.Audit.cs` — `AddBeepAudit` extension that the enricher plugs into
- **Future Work: CI/CD service** at `FutureWork/cicd-service/00-overview-and-scope.md` — a separate, future-dev service that **sets** `BEEP_DEPLOYMENT_METADATA_JSON` and **consumes** the audit log to drive commit status + approval flows
- **Future Work: Installer service** at `FutureWork/installer-service/00-overview-and-scope.md` — a separate, future WinForms project that wraps the engine's data-migration flow for first-install + online-update
- `.plans/phase-23.md` — superseded (CI/CD work has moved to the future-work service)
