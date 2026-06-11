# Phase 08 — Governance, Approvals & Audit (`IGovernanceService`)

> **Scope:** implement `IGovernanceService` — the Studio's policy, approval, and
> audit sub-service. Wraps the engine's existing `IBeepAudit` pipeline with a
> Studio-shaped view-model, adds the policy + approval workflow on top, and
> wires every mutation in Phases 2-7 to record an audit event.

> Legend: `[ ]` open · `[~]` in-progress · `[x]` done · `[!]` blocked

---

## Why this phase

The engine's `IBeepAudit` is the right transport (hash-chained, tamper-evident,
NDJSON or SQLite sink, retention, redactors, enrichers). What it lacks:

1. A **policy** model — which operations are allowed in which env tier, and
   what approval workflow is required.
2. An **approval workflow** — request → decide → apply gate.
3. A **redaction policy** for the audit view — Password / ApiKey / OAuthAccessToken
   must be redacted in the before/after JSON.
4. A **view-model** of the audit event that the host UI can bind to.
5. The wiring that every Phase 2-7 mutation actually records an event.

This phase adds all five.

## Public surface (this phase fills in)

```csharp
// Contracts/IGovernanceService.cs
public interface IGovernanceService
{
    // ---- policies ----
    Task<StudioResult<IReadOnlyList<GovernancePolicy>>> ListPoliciesAsync(CancellationToken ct = default);
    Task<StudioResult<GovernancePolicy>> GetPolicyAsync(string policyId, CancellationToken ct = default);
    Task<StudioResult<GovernancePolicy>> GetPolicyForTierAsync(RolloutTier tier, CancellationToken ct = default);
    Task<StudioResult<GovernancePolicy>> UpsertPolicyAsync(GovernancePolicy policy, CancellationToken ct = default);
    Task<StudioResult<bool>> DeletePolicyAsync(string policyId, CancellationToken ct = default);

    // ---- approval workflow ----
    Task<StudioResult<PolicyEvaluationResult>> EvaluateRequestAsync(ApprovalRequest request, CancellationToken ct = default);
    Task<StudioResult<ApprovalRequest>> RequestApprovalAsync(ApprovalRequest request, CancellationToken ct = default);
    Task<StudioResult<ApprovalRequest>> DecideApprovalAsync(string approvalId, ApprovalDecision decision, string decider, string? comment = null, CancellationToken ct = default);
    Task<StudioResult<ApprovalRequest>> WithdrawApprovalAsync(string approvalId, string withdrawer, string? reason = null, CancellationToken ct = default);
    Task<StudioResult<IReadOnlyList<ApprovalRequest>>> ListApprovalsAsync(ApprovalListFilter? filter = null, CancellationToken ct = default);
    Task<StudioResult<ApprovalRequest>> GetApprovalAsync(string approvalId, CancellationToken ct = default);

    // ---- audit ----
    Task<StudioResult<string>> RecordAuditAsync(StudioAuditEvent evt, CancellationToken ct = default);
    Task<StudioResult<IReadOnlyList<StudioAuditEvent>>> QueryAuditAsync(AuditQuery query, CancellationToken ct = default);
    Task<StudioResult<AuditIntegrityReport>> VerifyAuditIntegrityAsync(CancellationToken ct = default);

    // ---- SLO + alerts ----
    Task<StudioResult<IReadOnlyList<SloSnapshot>>> GetSloSnapshotsAsync(string? subjectId = null, int skip = 0, int take = 100, CancellationToken ct = default);
    Task<StudioResult<IReadOnlyList<AlertEvent>>> GetAlertsAsync(AlertListFilter? filter = null, CancellationToken ct = default);
}
```

## Models

```csharp
public sealed record GovernancePolicy(
    string PolicyId,
    string Name,
    RolloutTier Tier,
    bool RequireApprover,
    int RequiredApproverCount,
    IReadOnlyList<string> AllowedApproverRoles,
    IReadOnlyList<string> BlockedOperations,            // operation codes that are blocked outright
    TimeSpan? CooldownBetweenRuns,
    bool RequireDryRunOnApply,
    bool RequirePreflightOnApply,
    int MaxRowsAffectedPerRun,
    IReadOnlyList<SloTarget> SloTargets,
    IReadOnlyList<AlertRule> AlertRules,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SloTarget(
    string Name,                                         // "Migration apply p95 latency"
    string Metric,                                       // "migration.apply.latency.p95"
    double ThresholdValue,
    string Comparator,                                   // "lt" | "lte" | "gt" | "gte" | "eq"
    TimeSpan Window,                                     // rolling window
    string Severity);                                    // "Info" | "Warn" | "Critical"

public sealed record AlertRule(
    string Name,
    string TriggerMetric,
    string Comparator,
    double ThresholdValue,
    IReadOnlyList<string> Actions,                       // "slack" | "email" | "snackbar"
    IReadOnlyDictionary<string, string>? ActionArgs);    // e.g. Slack webhook URL

public sealed record ApprovalRequest(
    string ApprovalId,
    string OperationType,                                // "Migration.Apply" | "Sync.Run" | "Source.Delete" | "Policy.Update"
    string OperationSubjectId,                           // the plan id, schema id, or source name
    string OperationSubjectJson,                         // serialized payload (plan / schema / etc.)
    string PlanHash,                                     // ties the approval to a plan
    RolloutTier Tier,
    string RequestedBy,
    DateTimeOffset RequestedAt,
    IReadOnlyList<ApprovalDecision> Decisions,
    ApprovalState State,
    DateTimeOffset? DecidedAt,
    string? PayloadJson);

public sealed record ApprovalDecision(
    string Decider,
    DateTimeOffset DecidedAt,
    bool Approved,
    string? Comment);

public enum ApprovalState { Pending, Approved, Rejected, Withdrawn, Expired }

public sealed record ApprovalListFilter(
    ApprovalState? State = null,
    RolloutTier? Tier = null,
    string? OperationType = null,
    string? RequestedBy = null,
    string? Decider = null,
    DateTimeOffset? Since = null,
    DateTimeOffset? Until = null,
    int Skip = 0,
    int Take = 100);

public sealed record StudioAuditEvent(
    long? Seq,                                          // null for new events
    DateTimeOffset At,
    string Actor,
    string Category,                                     // "Source" | "Migration" | "Sync" | "Policy" | "Approval" | "Lifecycle" | "Driver" | "Schema"
    string Action,                                       // "Create" | "Update" | "Delete" | "Apply" | "Approve" | "Reject" | "Provision" | ...
    string Subject,                                      // "Source:BeepLive"
    string? BeforeJson,                                  // already redacted
    string? AfterJson,                                   // already redacted
    string? CorrelationId,                               // plan-hash, run-id, etc.
    string? Notes);

public sealed record AuditQuery(
    string? Actor = null,
    string? Category = null,
    string? Action = null,
    string? Subject = null,
    string? CorrelationId = null,
    DateTimeOffset? Since = null,
    DateTimeOffset? Until = null,
    int Skip = 0,
    int Take = 100);

public sealed record AuditIntegrityReport(
    bool IsIntact,
    int VerifiedEvents,
    IReadOnlyList<AuditIntegrityIssue> Issues);

public sealed record AuditIntegrityIssue(
    long Seq,
    string Reason);

public sealed record SloSnapshot(
    string SubjectId,                                    // schema id, source name, etc.
    string Metric,
    double Value,
    DateTimeOffset At);

public sealed record AlertEvent(
    string AlertId,
    string RuleName,
    string SubjectId,
    double Value,
    DateTimeOffset TriggeredAt,
    IReadOnlyList<string> ActionsTaken);
```

## Folder layout (this phase creates)

```
Services/Studio/
├── Contracts/IGovernanceService.cs                    ← DONE in Phase 1
├── Models/  (all the records above)
└── Governance/
    ├── GovernanceService.cs                           ← implements IGovernanceService
    ├── PolicyEvaluator.cs                             ← checks a request against a policy
    ├── ApprovalWorkflow.cs                            ← Pending → Approved/Rejected/Withdrawn transitions
    ├── ApprovalRequestStore.cs                        ← read/write of governance-approvals.json
    ├── PolicyStore.cs                                 ← read/write of governance-policies.json
    ├── AuditIntegrator.cs                             ← wraps IBeepAudit
    ├── AuditRedactor.cs                               ← redacts Password / ApiKey / OAuthAccessToken
    ├── AuditQueryEngine.cs                            ← wraps IAuditQueryEngine
    ├── AlertDispatcher.cs                             ← SLO + alert hooks
    └── GovernanceAuditHook.cs                         ← auto-wires every Phase 2-7 mutation
```

## Approval workflow

The flow:

1. Host calls `EvaluateRequestAsync(request)` → returns a
   `PolicyEvaluationResult` with `IsAllowed` and a list of violations.
2. If `IsAllowed = true` and the policy does not require an approver:
   the host can proceed without an approval.
3. If `IsAllowed = true` and the policy requires an approver:
   the host calls `RequestApprovalAsync(request)` → the request is
   stored with `State = Pending`.
4. Approvers (matching the `AllowedApproverRoles`) see the request in
   the **Approvals** tab and call `DecideApprovalAsync(id, decision, decider, comment)`.
5. `ApprovalWorkflow` enforces:
   - `decider != requestor` (a user can't approve their own request).
   - `decider` has one of the `AllowedApproverRoles`.
   - The decision count meets `RequiredApproverCount` before flipping to `Approved`.
6. When the count is met, the request flips to `Approved` and the host's
   **Migrations** / **Sync** tab receives a SignalR / event notification
   that the original request is approved and the run can proceed.

The approval is **bound to a `PlanHash`** — if the plan changes after
approval, the approval is invalidated. The host must call
`EvaluateRequestAsync` again before `ApplyAsync`.

## Audit integration

`AuditIntegrator` is the thin wrapper over `IBeepAudit`. It:

1. Accepts a `StudioAuditEvent`.
2. Redacts the `BeforeJson` and `AfterJson` via `AuditRedactor`
   (strips `Password`, `ApiKey`, `OAuthAccessToken`, `ClientSecret`,
   `OAuthRefreshToken`, `KeyToken`).
3. Computes a correlation id if the caller didn't supply one
   (e.g. `planHash` for migration events, `runId` for sync events).
4. Calls `IBeepAudit.RecordAsync(...)` to enqueue the event.
5. Returns the engine-assigned sequence number.

`GovernanceAuditHook` is a static helper that every Phase 2-7 service
calls at the end of a successful (or failed) mutation. The hook is
**always on** unless `StudioOptions.DisableAudit = true`.

## Audit redaction

```csharp
public static class AuditRedactor
{
    private static readonly string[] SecretProperties = new[]
    {
        "password", "apikey", "keytoken", "oauthaccesstoken", "oauthrefreshtoken",
        "clientsecret", "authcode", "additionalauthinfo", "authority", "audience"
    };

    public static string RedactJson(string? json);
    public static bool IsSecretProperty(string propertyName);
}
```

The redactor walks the JSON tree and replaces any property whose name
matches (case-insensitive) with `"***REDACTED***"`. It also redacts
values that look like a connection string containing `Password=...;`.

## SLO + alerts

The Studio re-uses the engine's existing `BeepSyncManager` SLO / alert
primitives (`EmitSloMetrics`, `EvaluateAlertRules`) by wrapping them in
`AlertDispatcher`. The host's **Governance** tab renders a `MudChart`
time series of the SLO snapshots and a grid of `AlertEvent` rows.

## Cross-cutting

- Every mutation in `IDriverService` (Phase 2), `ISourceService` (Phase 3),
  `ISchemaService` (Phase 4), `IMigrationStudioService` (Phase 5),
  `ISyncStudioService` (Phase 6), `IGovernanceService` (Phase 7),
  `IDataLifecycleManifestService` (Phase 9), `IDeploymentMetadataService` (Phase 10)
  records an audit event through this phase's `AuditIntegrator.RecordAsync`.
- The audit pipeline is the engine's existing `IBeepAudit` (file or SQLite
  sink, hash chain, redactors, enrichers, retention) — the Studio does not
  re-implement persistence.
- The approval store is `governance-approvals.json`; the policy store is
  `governance-policies.json`. Both live under `StudioOptions.DataRoot`.

---

## Todo Tracker

| # | Task | Status | Notes |
|---|------|--------|-------|
| P08-01 | All `Models/*.cs` for this phase (~25 POCOs) | ⬜ | |
| P08-02 | `Governance/PolicyStore.cs` + `ApprovalRequestStore.cs` — JSON read/write | ⬜ | |
| P08-03 | `Governance/AuditRedactor.cs` — `RedactJson`, `IsSecretProperty` | ⬜ | |
| P08-04 | `Governance/AuditIntegrator.cs` — wraps `IBeepAudit` | ⬜ | |
| P08-05 | `Governance/AuditQueryEngine.cs` — wraps `IAuditQueryEngine` | ⬜ | |
| P08-06 | `Governance/PolicyEvaluator.cs` — checks a request against a policy | ⬜ | |
| P08-07 | `Governance/ApprovalWorkflow.cs` — Pending → Approved/Rejected transitions | ⬜ | |
| P08-08 | `Governance/AlertDispatcher.cs` — SLO + alert hooks | ⬜ | |
| P08-09 | `Governance/GovernanceAuditHook.cs` — auto-wires Phase 2-7 services | ⬜ | |
| P08-10 | `Governance/GovernanceService.cs` — implements `IGovernanceService` | ⬜ | |
| P08-11 | Wire `IGovernanceService` + `AuditIntegrator` into `AddBeepStudio()` | ⬜ | |
| P08-12 | Modify `LifecycleService` (Phase 2) to call `AuditIntegrator.RecordAsync` on every mutation | ⬜ | |
| P08-13 | Modify `DriverService` (Phase 3) to call `AuditIntegrator.RecordAsync` on every mutation | ⬜ | |
| P08-14 | Modify `SourceService` (Phase 4) to call `AuditIntegrator.RecordAsync` on every mutation | ⬜ | |
| P08-15 | Modify `SchemaService` (Phase 5) to call `AuditIntegrator.RecordAsync` on every mutation | ⬜ | |
| P08-16 | Modify `MigrationStudioService` (Phase 6) to call `AuditIntegrator.RecordAsync` on every apply | ⬜ | |
| P08-17 | Modify `SyncStudioService` (Phase 7) to call `AuditIntegrator.RecordAsync` on every run | ⬜ | |
| P08-18 | Tests: `PolicyEvaluatorTests` (3+), `ApprovalWorkflowTests` (4+ — including the self-approval guard), `AuditIntegratorTests` (3+ — including redaction), `AuditQueryEngineTests` (2+) | ⬜ | |
| P08-19 | Document: approval-token format + persistence layout | ⬜ | |
| P08-20 | Update `00-overview-and-scope.md` + `MASTER-TODO-TRACKER.md` to mark Phase 08 done | ⬜ | |

---

## Validation (definition of done)

- [ ] `dotnet build DataManagementEngineStandard` succeeds with **0 errors**.
- [ ] `UpsertPolicyAsync` with a `Live` tier policy stores it in `governance-policies.json`.
- [ ] `EvaluateRequestAsync` with a `Migration.Apply` request against a `Live` tier policy returns `IsAllowed = true` with a violation "approval required."
- [ ] `RequestApprovalAsync` creates a request with `State = Pending`.
- [ ] `DecideApprovalAsync` by the same user as the requestor returns `StudioResult.Fail(StudioErrorCode.PermissionDenied, ...)`.
- [ ] After 2 distinct approvers decide `Approved`, the request flips to `Approved` automatically.
- [ ] `RecordAuditAsync` on a source change with a `Password` field in `AfterJson` stores the JSON with `***REDACTED***` instead of the clear-text value.
- [ ] `VerifyAuditIntegrityAsync` on a clean audit file returns `IsIntact = true`.
- [ ] All 12+ new tests pass.

---

## Pitfalls

1. **Don't store clear-text secrets in the audit log** — `AuditRedactor` must run on every event.
2. **Don't allow self-approval** — `ApprovalWorkflow` must reject `decider == requestor`.
3. **Don't skip the cooldown check** — `EvaluateRequestAsync` must enforce `CooldownBetweenRuns` between two applies of the same target.
4. **Don't allow an approval to outlive its plan** — if the `planHash` of the apply differs from the approval's `planHash`, the apply is blocked.
5. **Don't write the audit log inline (synchronously)** — `IBeepAudit` is async with a `Channel`-backed pipeline; the Studio does not bypass it.
6. **Don't make the policy engine the only gate** — the Studio must also have a UI-level gate (the host shows the approval status in the **Migrations** / **Sync** tab) so the user understands why the apply is blocked.
7. **Don't delete audit events** — the engine's retention is set by `StudioOptions.AuditRetentionDays`; the Studio never deletes manually.

---

## Related

- Phase 01 — contracts (this phase implements `IGovernanceService`)
- Phases 2-7 — every mutation goes through `AuditIntegrator` thanks to the wiring in this phase
- Phase 09 — platform adapters (the audit log can be tailed in the Blazor Server hub)
- `BeepDM/DataManagementEngineStandard/Services/Audit/IBeepAudit.cs` — the engine surface we wrap
- `BeepDM/DataManagementEngineStandard/Services/Audit/BeepAuditOptions.cs` — the engine options we re-use for the Studio's audit pipeline
- `BeepDM/DataManagementEngineStandard/Editor/Migration/MigrationManager.DevExAutomation.cs` — `ValidatePlanForCi` is the CI gate we surface via `EvaluatePolicyAsync`
- `BeepDM/DataManagementEngineStandard/Editor/Migration/MigrationManager.RolloutGovernance.cs` — `EvaluateRolloutGovernance` is the wave / tier check we re-use
