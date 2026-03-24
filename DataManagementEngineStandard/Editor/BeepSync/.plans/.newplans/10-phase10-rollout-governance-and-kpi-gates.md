# Phase 10 (Enhanced) — Rollout Governance and KPI Gates

## Supersedes
`../10-phase10-rollout-governance-and-kpi-gates.md`

## Objective
Gate blue/green and phased rollout promotions behind rule-evaluated KPI checks,
enforced mapping approval states, and validated defaults profiles — ensuring that
no sync schema reaches production without passing a verifiable, auditable health check.

---

## Scope
- KPI gate rules evaluated against the last N sync runs before promotion.
- `MappingApprovalState.Approved` enforced for all mapping artifacts at cutover.
- `EntityDefaultsProfile` verification for all target environments.
- Integration health check suite (catalog loaded, plan compiled, profile ready, governance version aligned).
- Rollout runbook: canary → traffic ramp → full cutover, with automatic rollback triggers.

---

## File Targets

| File | Change Description |
|---|---|
| `BeepSync/BeepSyncManager.Orchestrator.cs` | Add `EvaluateRolloutReadinessAsync(schema, history, ctx)` returning `RolloutReadinessReport` |
| `BeepSync/Models/RolloutReadinessReport.cs` *(new)* | Rollout gate output with phase, status, per-gate detail lines |
| `BeepSync/Models/SyncRolloutPhase.cs` *(new)* | Enum: `Canary`, `PartialTraffic`, `FullCutover` |
| `BeepSync/Helpers/SyncValidationHelper.cs` | Add `VerifyIntegrationHealth(schema, editor, ctx)` |

---

## Integration Points: Rule Engine — KPI Gate Rules

### Rule Key: `sync.rollout.kpi-gate`
Evaluated against aggregated run history before any promotion is approved.

**Inputs** (passed as `PassedArgs`):
| Parameter | Value |
|---|---|
| `SuccessRatePct` | Average success rate over last N runs |
| `MeanDurationSec` | Average run duration |
| `MaxConflictRatePct` | Max conflict rate across last N runs |
| `MaxRejectRatePct` | Max reject rate across last N runs |
| `FailedRunStreak` | Consecutive failures at tail of history |
| `MappingDriftDetectedCount` | Runs where drift was flagged |

**Usage:**
```csharp
var args = new PassedArgs
{
    SuccessRatePct     = history.AverageSuccessRate(),
    MeanDurationSec    = history.MeanDurationSec(),
    MaxConflictRatePct = history.MaxConflictRate(),
    MaxRejectRatePct   = history.MaxRejectRate(),
    FailedRunStreak    = history.TailFailureStreak(),
    MappingDriftDetectedCount = history.CountRunsWithDrift()
};

var result = ruleEngine.SolveRule(
    "sync.rollout.kpi-gate",
    args,
    new RuleExecutionPolicy { MaxDepth = 5, EnforceLifecycle = true });

if (result.Output == "BLOCK")
    report.AddBlock("KPI-GATE-FAILED", result.Reason);
else if (result.Output == "WARN")
    report.AddWarning("KPI-GATE-WARNING", result.Reason);
```

### Default Built-in KPI Gate Logic (editable via `RuleCatalog`)

| KPI | Canary Gate | Full Cutover Gate |
|---|---|---|
| Success rate | ≥ 95 % | ≥ 99 % |
| Mean duration | ≤ 2× baseline | ≤ 1.2× baseline |
| Max conflict rate | ≤ 5 % | ≤ 1 % |
| Max reject rate | ≤ 2 % | ≤ 0.5 % |
| Tail failure streak | ≤ 1 | 0 |
| Drift detected count | ≤ 1 | 0 |

Operators can override thresholds by updating the `sync.rollout.kpi-gate` rule in `RuleCatalog`
without changing application code.

### Rule Key: `sync.rollout.auto-rollback`
Triggered when live metrics on the canary cohort breach SLO thresholds:
```csharp
var liveMetrics = collector.GetCurrentRunMetrics();
var rollbackResult = ruleEngine.SolveRule(
    "sync.rollout.auto-rollback",
    liveMetrics.ToPassedArgs(),
    new RuleExecutionPolicy { MaxDepth = 3 });

if (rollbackResult.Output == "ROLLBACK")
{
    await rolloutManager.RollbackToCheckpointAsync(lastStableCheckpointId);
    alertService.EmitRollbackAlert(schema.SchemaID, rollbackResult.Reason);
}
```

---

## Integration Points: Mapping Manager — Approval Gate

### Prerequisite Check at Each Rollout Phase
```csharp
var mapMeta = MappingManager.GetCurrentMappingVersionMeta(
    schema.SourceDataSourceName, schema.DestinationDataSourceName, schema.SourceEntityName);

var requiredState = rolloutPhase switch
{
    SyncRolloutPhase.Canary       => MappingApprovalState.ReviewedAndApproved,
    SyncRolloutPhase.PartialTraffic => MappingApprovalState.Approved,
    SyncRolloutPhase.FullCutover  => MappingApprovalState.Approved,
    _ => MappingApprovalState.Draft
};

if (mapMeta.ApprovalState < requiredState)
    report.AddBlock("MAPPING-NOT-APPROVED",
        $"Phase '{rolloutPhase}' requires mapping approval state '{requiredState}', " +
        $"current state is '{mapMeta.ApprovalState}'.");
```

### Governance Version Alignment Check
```csharp
var schemaMappingVersion = schema.SchemaVersion?.MappingVersion;
var liveMappingVersion   = mapMeta?.Version;

if (schemaMappingVersion != null && schemaMappingVersion != liveMappingVersion)
    report.AddWarning("MAPPING-VERSION-MISMATCH",
        $"Schema was authored against mapping version {schemaMappingVersion}, " +
        $"live mapping is now at version {liveMappingVersion}. " +
        "Re-evaluate mapping or update schema's recorded mapping version.");
```

### Compiled Plan Health Check
```csharp
var compiledPlan = MappingManager.GetOrCompileMappingPlan(
    schema.SourceDataSourceName, schema.DestinationDataSourceName, schema.SourceEntityName);

if (compiledPlan == null || compiledPlan.FieldMaps.Count == 0)
    report.AddBlock("MAPPING-PLAN-EMPTY",
        "Compiled mapping plan is empty or failed to compile. " +
        "Run AutoMapByConventionWithScoring or define field maps explicitly.");
```

---

## Integration Points: Defaults Manager — Profile Verification

### 1. Profile Coverage Audit
Ensure every destination entity in the schema batch has a valid defaults profile in the target environment:
```csharp
foreach (var schema in schemaSet)
{
    var profile = DefaultsManager.GetProfile(
        schema.DestinationDataSourceName, schema.DestinationEntityName);

    if (profile == null)
    {
        report.AddBlock("DEFAULTS-PROFILE-MISSING",
            $"No EntityDefaultsProfile for '{schema.DestinationEntityName}' in " +
            $"'{schema.DestinationDataSourceName}'. Run DefaultsManager.ExportDefaults " +
            "from staging environment and import to target before rollout.");
    }
    else
    {
        // Verify dynamic defaults resolve successfully in target environment
        var testRecord = new ExpandoObject() as IDictionary<string, object>;
        var applyErrors = DefaultsManager.TestApply(
            editor, schema.DestinationDataSourceName, schema.DestinationEntityName, testRecord);

        if (applyErrors.Flag == Errors.Failed)
            report.AddWarning("DEFAULTS-TEST-APPLY-FAILED", applyErrors.Message);
    }
}
```

### 2. Dynamic Default Resolver Validation
Confirm that expression-based defaults (`:NOW`, `:USERNAME`, `:NEWGUID`) have resolvers
registered in the target environment — environments without a user session context may
need a service-account resolver:
```csharp
var unresolvedExpressions = DefaultsManager.FindUnresolvedExpressions(
    editor, schema.DestinationDataSourceName, schema.DestinationEntityName);

if (unresolvedExpressions.Any())
    report.AddWarning("DEFAULTS-UNRESOLVED-EXPRESSION",
        "The following expressions have no registered resolver in this environment: " +
        string.Join(", ", unresolvedExpressions));
```

---

## `RolloutReadinessReport` Model

```csharp
public class RolloutReadinessReport
{
    public string           SchemaId         { get; set; }
    public SyncRolloutPhase TargetPhase      { get; set; }
    public bool             ReadyToPromote   { get; set; } = true;
    public DateTime         EvaluatedAt      { get; set; } = DateTime.UtcNow;
    public string           EvaluatedBy      { get; set; }
    public List<RolloutGateLine> Blocks   { get; set; } = new();
    public List<RolloutGateLine> Warnings { get; set; } = new();

    public void AddBlock(string code, string detail)
    {
        ReadyToPromote = false;
        Blocks.Add(new RolloutGateLine { Code = code, Detail = detail });
    }

    public void AddWarning(string code, string detail)
        => Warnings.Add(new RolloutGateLine { Code = code, Detail = detail });
}

public class RolloutGateLine
{
    public string Code   { get; set; }
    public string Detail { get; set; }
}

public enum SyncRolloutPhase { Canary, PartialTraffic, FullCutover }
```

---

## Integration Health Check Suite

Called at the start of every rollout phase transition:

```csharp
public async Task<RolloutReadinessReport> VerifyIntegrationHealthAsync(
    DataSyncSchema schema, IDMEEditor editor, SyncIntegrationContext ctx,
    SyncRunHistory history, SyncRolloutPhase targetPhase)
{
    var report = new RolloutReadinessReport
    {
        SchemaId = schema.SchemaID,
        TargetPhase = targetPhase,
        EvaluatedBy = ctx.RunInitiatedBy
    };

    // 1. Rule catalog health
    if (ctx.RuleEngine != null)
    {
        LintRuleCatalog(schema, ctx.RuleEngine, ctx.RuleCatalog, report);
        EvaluateKpiGate(schema, history, ctx.RuleEngine, report);
    }

    // 2. Mapping governance
    CheckMappingApprovalState(schema, targetPhase, report);
    CheckMappingPlanCompiled(schema, report);
    CheckMappingVersionAlignment(schema, report);

    // 3. Defaults profile
    foreach (var s in schema.Enumerated())
        VerifyDefaultsProfile(s, editor, report);

    // 4. Stamp report with DefaultsManager (audit trail)
    if (ctx.DefaultsManager != null)
    {
        var reportRecord = report.ToDict();
        ctx.DefaultsManager.Apply(editor, "Rollout", "RolloutReadinessReport",
            reportRecord, ctx.ToDefaultsApplyContext());
    }

    return report;
}
```

---

## Rollout Phase Sequence

```
[Pre-Rollout]
  VerifyIntegrationHealthAsync(phase=Canary)      → all gates pass
  ∟ RuleEngine: kpi-gate with last 5 runs
  ∟ MappingManager: ApprovalState ≥ ReviewedAndApproved
  ∟ DefaultsManager: all profiles present, expressions resolving

[Canary (5% traffic)]
  Monitor live metrics for 1 rollout window (e.g., 30 min)
  ∟ RuleEngine: auto-rollback rule on each batch completion
  ∟ MappingManager: drift detection after each successful run

[Partial Traffic (50%)]
  VerifyIntegrationHealthAsync(phase=PartialTraffic) → re-gate
  ∟ KPI evaluation against canary run history
  ∟ MappingManager: ApprovalState = Approved
  ∟ DefaultsManager: verify resolvers under load

[Full Cutover]
  VerifyIntegrationHealthAsync(phase=FullCutover) → final gate
  ∟ Zero mapping drift allowed
  ∟ Zero tail failure streak
  ∟ Success rate ≥ 99%

[Post-Cutover]
  Archive pre-cutover mapping version snapshot
  DefaultsManager.ExportDefaults → store as baseline artifact
  RuleEngine: promote kpi-gate rule to Production lifecycle state
```

---

## Rollback Integration

If any live gate fails after promotion:

```csharp
// Revert mapping to last approved version
MappingManager.RevertToVersion(
    schema.SourceDataSourceName, schema.DestinationDataSourceName,
    schema.SourceEntityName, lastApprovedVersion);

// Revert defaults to snapshot from pre-cutover baseline
DefaultsManager.ImportDefaults(editor, schema.DestinationDataSourceName,
    baselineArtifact, overwrite: true);

// Resume from last stable checkpoint (Phase 5)
await syncManager.ResumeFromCheckpointAsync(lastStableCheckpoint.CheckpointId, schema);
```

---

## Acceptance Criteria
- `EvaluateRolloutReadinessAsync` evaluates all three integration vectors (Rule Engine KPI gate, mapping state, defaults profile) and returns a structured `RolloutReadinessReport`.
- Promotion to Canary is blocked unless `sync.rollout.kpi-gate` returns a non-`BLOCK` output.
- Promotion to Full Cutover requires `MappingApprovalState.Approved` and 0 tail failure streak.
- Defaults profiles verified for all destination entities before each phase transition.
- Auto-rollback rule evaluated after each canary batch completes.
- Rollback restores mapping version, defaults profile, and resumes from last stable checkpoint.
- All gate evaluations produce `RolloutReadinessReport` entries persisted as an audit trail via `DefaultsManager.Apply`.
