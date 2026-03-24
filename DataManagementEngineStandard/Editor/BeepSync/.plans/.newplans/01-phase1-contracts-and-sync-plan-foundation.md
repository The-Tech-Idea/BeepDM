# Phase 1 (Enhanced) — Contracts, Sync Plan Foundation, and Integration Bootstrap

## Supersedes
`../01-phase1-contracts-and-sync-plan-foundation.md`

## Objective
Establish a plan-first contract for sync operations while bootstrapping the three integration vectors
(Rule Engine, Defaults Manager, Mapping Manager) as optional runtime dependencies — fully backward-compatible.

---

## Scope
- Sync plan DTOs with lifecycle state.
- `SyncIntegrationContext` — runtime holder for resolved engine/manager references.
- `SyncRulePolicy`, `SyncDefaultsPolicy`, `SyncMappingPolicy` DTOs.
- `SyncPreflightReport` composite output.
- Integration bootstrap in `BeepSyncManager` constructor or lazy init.

---

## File Targets

| File | Change Description |
|---|---|
| `BeepSync/BeepSyncManager.Orchestrator.cs` | Add `SyncIntegrationContext` field; bootstrap rule engine / defaults / mapping in constructor |
| `BeepSync/Interfaces/ISyncHelpers.cs` | Add `SyncRulePolicy`, `SyncDefaultsPolicy`, `SyncMappingPolicy`, `SyncIntegrationContext`, `SyncPreflightReport` DTOs |
| `BeepSync/Models/SyncPlanMetadata.cs` *(new)* | Plan artifact DTO: id, owner, approver, lifecycle state, linked policy refs |
| `BeepSync/Models/SyncIntegrationContext.cs` *(new)* | Runtime context: `IRuleEngine`, `IDefaultsManager`, mapping plan version |

---

## Integration Points: Rule Engine

### 1. Plan-Level Pre-Validation Rule
When a `DataSyncSchema` enters validation (`ValidateSyncOperation`), execute a registered plan
validation rule before the existing structural checks:
```csharp
// New: validate plan fields via rules before sync starts
if (_integrationCtx?.RuleEngine != null)
{
    var policy = schema.RulePolicy?.ExecutionPolicy ?? RuleExecutionPolicy.DefaultSafe;
    var (_, result) = _integrationCtx.RuleEngine.SolveRule(
        "sync.plan.validate",
        new Dictionary<string, object>
        {
            ["schema"]      = schema,
            ["sourceDs"]    = schema.SourceDataSourceName,
            ["destDs"]      = schema.DestinationDataSourceName,
            ["direction"]   = schema.SyncDirection
        },
        policy);

    if (!result.Success)
        return CreateError(result.FailureReason ?? "Plan validation rule rejected this sync schema.");
}
```

### 2. Plan Approval Rule
New `ApprovePlanAsync(string planId, string approver)` method — evaluates an `approval-gate` rule
from `RuleCatalog` using approver context. Failing gate keeps plan in `PendingApproval` state.

### 3. Rule Keys Defined in This Phase
| Rule Key | Stage | Behaviour |
|---|---|---|
| `sync.plan.validate` | Pre-sync validation | Structural and policy validation of plan fields |
| `sync.plan.approval-gate` | Approval workflow | Approver role and environment checks |

---

## Integration Points: Defaults Manager

### 1. Seed Plan Metadata Defaults
On plan creation (`CreateSyncPlan(...)`), apply `DefaultsManager` expressions to stamp
plan owner, creation timestamp, schema version:
```csharp
DefaultsManager.SetColumnDefault(editor, "SyncPlans", "SyncPlanMetadata", "CreatedAt", ":NOW", isRule: true);
DefaultsManager.SetColumnDefault(editor, "SyncPlans", "SyncPlanMetadata", "CreatedBy", ":USERNAME", isRule: true);
DefaultsManager.SetColumnDefault(editor, "SyncPlans", "SyncPlanMetadata", "SchemaVersion", "1", isRule: false);
```

### 2. `SyncDefaultsPolicy` per Schema
Each `DataSyncSchema` gains a `DefaultsPolicy` property of type `SyncDefaultsPolicy`:
- `ApplyOnInsert: bool` — call `DefaultsManager.Apply(...)` before each destination insert.
- `ApplyOnUpdate: bool` — call `DefaultsManager.Apply(...)` for update audit fields only.
- `ProfileKey: string` — `EntityDefaultsProfile` key in `DataManagementEngine.Defaults`.

---

## Integration Points: Mapping Manager

### 1. Mapping Quality Preflight
Before a plan is approved, compute mapping quality score for the entity pair:
```csharp
var (scoredMap, diagnostics) = MappingManager.AutoMapByConventionWithScoring(
    schema.SourceDataSourceName, schema.SourceEntityName,
    schema.DestinationDataSourceName, schema.DestinationEntityName);

if (scoredMap.QualityScore < schema.MappingPolicy?.MinQualityScore ?? 70)
    preflight.AddWarning("MAPPING-BELOW-THRESHOLD", scoredMap.QualityScore);
```

### 2. `SyncMappingPolicy` per Schema
Each `DataSyncSchema` gains a `MappingPolicy` property:
- `MinQualityScore: int` — preflight quality threshold (0–100).
- `RequiredApprovalState: MappingApprovalState` — e.g., `Approved` in production.
- `OnDriftAction: SyncDriftAction` — `Warn | Block | AutoRemapAndReview`.

### 3. Governance Scope on Mapping Save
```csharp
using (MappingManager.BeginGovernanceScope(
    author: currentUser,
    changeReason: $"Sync plan {planId} field mapping save",
    targetState: MappingApprovalState.Draft))
{
    MappingManager.SaveEntityMap(entityMap, sourceDsName, destDsName);
}
```

---

## `SyncPreflightReport` Model

```csharp
public class SyncPreflightReport
{
    public string PlanId         { get; set; }
    public bool   IsApproved     { get; set; }
    public int    MappingScore   { get; set; }
    public string MappingState   { get; set; }  // approval state string
    public bool   RulesPassed    { get; set; }
    public bool   DefaultsReady  { get; set; }
    public List<SyncPreflightIssue> Issues { get; set; } = new();
}

public class SyncPreflightIssue
{
    public string Code      { get; set; }
    public string Channel   { get; set; }  // "Rules" | "Defaults" | "Mapping" | "Schema"
    public string Severity  { get; set; }  // "Error" | "Warning" | "Info"
    public string Message   { get; set; }
}
```

---

## Acceptance Criteria
- `RunPreflightAsync(schema)` returns `SyncPreflightReport` without executing any sync.
- Plan validation rule failures block sync start (not just warn).
- Defaults are stamped on plan creation; `:NOW` and `:USERNAME` resolve correctly.
- Mapping quality gate below threshold produces an `Error`-severity preflight issue.
- All integration paths are null-safe: when `RuleEngine`/`DefaultsManager` are absent, sync falls through to existing behaviour.
