# Phase 5 — Identity, RBAC & Approvals

**Goal:** Add identity and authorization for enterprise, with **zero ceremony for solo**. Same
product; the solo path registers no-op providers and behaves exactly as it does today.

**Pre-condition:** Phase 2 (you authorize a *definition*; a compiled object graph has nothing to
authorize).

**Files touched:** `DataManagementModelsStandard/SetUp/`, `DataManagementEngineStandard/SetUp/`

---

## ✅ Status: complete

All items P5-01..06 landed (per-item summary in the master tracker). 193/193 tests green;
`SecurityTests.cs` (10) covers it, led by `Solo_NoSecurityConfig_StillRuns` — the design rule.

Notes:

- **The plan named an enterprise provider `RoleBasedSetupAuthorizer` and an approval provider without
  a concrete class.** Implemented `SeparationOfDutyApprovalProvider` for approvals — it takes an
  `approverLookup` delegate so a host wires it to whatever approval system it has (ticket, workflow,
  signed token), and the class enforces the one rule that must never be delegated: the approver can't
  be the requester.
- **`SetupState`'s new actor fields did *not* bump `SchemaVersion`.** They're additive — a v1
  document just deserializes them as null/false. Bumping would have made P3's version gate reject
  every existing checkpoint, which is worse than the change it'd be protecting against.
- The `AddBeepSetup().AsEnterprise(...)` DI sugar from the tracker's overview is **not** wired yet —
  that's a DI-registration concern that belongs with P7/P8's host integration. Today security is
  wired via `SetupWizardBuilder.WithSecurity(principal, authorizer)` and the `SchemaSetupStep` ctor.

---

## What's wrong today

- **No identity anywhere.** No principal, no permission concept in the whole framework.
- **`StrictPolicyMode` — a client-set `bool` — is the entire authorization model.**
- **Approvals are self-granted.** `SchemaSetupStepOptions.ApproverLabel` defaults to the literal
  `"SetupWizard"`, and `SchemaSetupStep` calls `ApproveMigrationPlan` with the note
  *"Auto-approved by setup wizard"*. The wizard approves its own migration. For solo that's fine —
  but it must be *recorded honestly*, not dressed up as an approval.

## Design rule

Every enterprise concept has a solo default that is a **no-op, not a stub that throws**. If
`AddBeepSetup()` alone stops working after this phase, the phase is wrong.

---

## 5-A  `ISetupPrincipal`

**New:** `DataManagementModelsStandard/SetUp/Security/ISetupPrincipal.cs`

```csharp
public interface ISetupPrincipal
{
    string Id { get; }
    string DisplayName { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsAuthenticated { get; }
}

/// <summary>Solo default. Identifies the local user without authenticating them.</summary>
public sealed class AnonymousSetupPrincipal : ISetupPrincipal
{
    public string Id => System.Environment.UserName;   // note: System.Environment — SetupOptions.Environment collides
    public string DisplayName => System.Environment.UserName;
    public IReadOnlyCollection<string> Roles => Array.Empty<string>();
    public bool IsAuthenticated => false;
}
```

Recording `IsAuthenticated = false` is the point — P6's audit trail must never imply a solo run was
authenticated.

## 5-B  `ISetupAuthorizer`

```csharp
public interface ISetupAuthorizer
{
    Task<SetupAuthorizationResult> AuthorizeAsync(ISetupPrincipal principal, SetupPermission permission,
                                                  SetupContext context, CancellationToken token = default);
}

public enum SetupPermission { RunSetup, ProvisionDriver, ConfigureConnection, ApplySchema,
                              ApproveMigration, Seed, Rollback, ViewState }

public sealed record SetupAuthorizationResult(bool Allowed, string Reason)
{
    public static SetupAuthorizationResult Allow() => new(true, null);
    public static SetupAuthorizationResult Deny(string reason) => new(false, reason);
}
```

- `AllowAllAuthorizer` — solo default; allows everything.
- `RoleBasedSetupAuthorizer` — enterprise; role → permission map.

## 5-C  Enforcement

`SetupWizard.Run` checks `RunSetup` once, then each step's required permission before `Execute`. A
denial is a **step failure**, not an exception:

```csharp
var auth = await _authorizer.AuthorizeAsync(_principal, step.RequiredPermission, context, token);
if (!auth.Allowed)
    return FailStep(step, $"Not authorized: {auth.Reason}");
```

Add to `ISetupStep` as a DIM so existing steps compile:

```csharp
SetupPermission RequiredPermission => SetupPermission.RunSetup;
```

## 5-D  Real approvals

```csharp
public interface ISetupApprovalProvider
{
    Task<SetupApproval> RequestApprovalAsync(SetupContext context, string planHash,
                                             CancellationToken token = default);
}

public sealed record SetupApproval(string ApproverId, string ApproverLabel, DateTimeOffset ApprovedAt,
                                   string PlanHash, bool IsSelfApproved, string Note);
```

- **Solo** — `AutoApprovalProvider`: approves, sets `IsSelfApproved = true`, note
  `"Auto-approved (solo mode, no approver configured)"`. Honest, not laundered.
- **Enterprise** — self-approval is **rejected**: if `ApproverId == principal.Id`, deny. That's the
  entire point of an approval gate.

`SchemaSetupStep` calls `ApproveMigrationPlan` with the returned `SetupApproval` rather than the
hardcoded `"SetupWizard"` label. Bind approval to the **plan hash** so an approval can't be replayed
against a different plan.

## 5-E  Bind principal to state

Add to `SetupState` and `SetupReport`:

```csharp
public string ActorId { get; set; }
public string ActorDisplayName { get; set; }
public bool ActorAuthenticated { get; set; }
```

P6 consumes these. Bump `SetupState.SchemaVersion` (P2-F) — this is a shape change, exactly what the
upgrader exists for.

## 5-F  DI

```csharp
services.AddBeepSetup();          // Anonymous + AllowAll + AutoApproval — unchanged behavior
services.AddBeepSetup().AsEnterprise(o => {
    o.UseRbac();                                       // RoleBasedSetupAuthorizer
    o.UsePrincipal(sp => sp.GetRequiredService<IHostPrincipalAdapter>().Current);
    o.UseApprovals(sp => sp.GetRequiredService<IApprovalWorkflow>());
});
```

## 5-G  Tests

| Test | Guards |
|---|---|
| `Solo_NoConfig_StillRuns_Unchanged` | design rule |
| `Denied_Permission_FailsStep_DoesNotThrow` | 5-C |
| `Enterprise_RejectsSelfApproval` | 5-D |
| `Solo_AutoApproval_RecordsIsSelfApproved_True` | 5-D honesty |
| `Approval_BoundToPlanHash_CannotBeReplayed` | 5-D |
| `State_Records_Actor_And_AuthenticatedFlag` | 5-E |

## Files summary

| Action | File | Est. |
|---|---|---|
| New | `Models/SetUp/Security/ISetupPrincipal.cs` | ~35 |
| New | `Models/SetUp/Security/ISetupAuthorizer.cs` | ~45 |
| New | `Models/SetUp/Security/ISetupApprovalProvider.cs` | ~30 |
| Modify | `Models/SetUp/ISetupStep.cs` (DIM) | ~4 |
| Modify | `Models/SetUp/SetupState.cs`, `SetupReport.cs` | ~10 |
| New | `Engine/SetUp/Security/AllowAllAuthorizer.cs` | ~20 |
| New | `Engine/SetUp/Security/RoleBasedSetupAuthorizer.cs` | ~80 |
| New | `Engine/SetUp/Security/AutoApprovalProvider.cs` | ~30 |
| Modify | `Engine/SetUp/SetupWizard.cs` | ~40 |
| Modify | `Engine/SetUp/Steps/SchemaSetupStep.cs` | ~30 |
| Modify | `Engine/SetUp/SetupWizardServiceExtensions.cs` | ~50 |
| New | `tests/SetupWizardTests/SecurityTests.cs` | ~220 |
