# Phase 7 (Enhanced) — Observability, SLO, Alerting, and Rule-Based Alert Triggers

## Supersedes
`../07-phase7-observability-slo-and-alerting.md`

## Objective
Build operational telemetry that surfaces sync SLOs, emits rule-evaluated alert triggers
using `RuleEngine.RuleEvaluated` events, stamps alert metadata via `DefaultsManager`,
and includes mapping plan version in all trace/correlation contexts.

---

## Scope
- Core SLIs and SLO profiles.
- Alert trigger evaluation via dedicated alert rules in `RuleCatalog`.
- `RuleEngine.RuleEvaluated` subscription for rule audit telemetry.
- Alert record metadata stamped by `DefaultsManager`.
- Mapping plan version included in metrics/correlation IDs.

---

## File Targets

| File | Change Description |
|---|---|
| `BeepSync/SyncMetrics.cs` | Add `MappingPlanVersion`, `RuleEvaluationCount`, `RejectRate`, `ConflictRate` fields |
| `BeepSync/Helpers/SyncProgressHelper.cs` | Add `EmitSloMetrics(...)`, `EvaluateAlertRules(...)`, `BuildAlertPayload(...)` |
| `BeepSync/BeepSyncManager.Orchestrator.cs` | Subscribe to `RuleEngine.RuleEvaluated`; emit SLO metrics at run end |
| `BeepSync/Models/SyncAlertRecord.cs` *(new)* | Alert artifact with rule key, severity, remediation hint, correlation id |
| `BeepSync/Models/SloProfile.cs` *(new)* | SLO thresholds: success rate, max duration, max freshness lag, conflict rate ceiling |

---

## Integration Points: Rule Engine

### 1. Alert Trigger Rules
At the end of each sync run, evaluate alert trigger rules against the collected `SyncMetrics`:
```csharp
if (_integrationCtx?.RuleEngine != null)
{
    var alertContext = new Dictionary<string, object>
    {
        ["successRate"]       = metrics.SuccessRate,
        ["runDurationMs"]     = metrics.Duration.TotalMilliseconds,
        ["rejectRate"]        = metrics.RejectRate,
        ["conflictRate"]      = metrics.ConflictRate,
        ["freshnesLagSeconds"] = metrics.FreshnessLagSeconds,
        ["retryCount"]        = metrics.RetryCount,
        ["sloProfile"]        = slo.ProfileName
    };

    foreach (var alertRuleKey in schema.SloProfile?.AlertRuleKeys ?? Enumerable.Empty<string>())
    {
        var (_, alertResult) = _integrationCtx.RuleEngine.SolveRule(
            alertRuleKey, alertContext,
            schema.RulePolicy?.ExecutionPolicy ?? RuleExecutionPolicy.DefaultSafe);

        if (alertResult.Success && alertResult.Outputs.TryGetValue("triggered", out var triggered)
            && (bool)triggered)
        {
            EmitAlert(alertRuleKey, alertResult, metrics, schema);
        }
    }
}
```

### 2. Built-In Alert Rules
| Rule Key | Trigger Condition |
|---|---|
| `sync.alert.low-success-rate` | `successRate < sloProfile.MinSuccessRate` |
| `sync.alert.run-duration-breach` | `runDurationMs > sloProfile.MaxDurationMs` |
| `sync.alert.freshness-breach` | `freshnessLagSeconds > sloProfile.MaxFreshnessLagSeconds` |
| `sync.alert.high-conflict-rate` | `conflictRate > sloProfile.MaxConflictRate` |
| `sync.alert.high-reject-rate` | `rejectRate > sloProfile.MaxRejectRate` |
| `sync.alert.repeated-failures` | Last 3 runs all failed (requires run-history context) |

### 3. `RuleEvaluated` Subscription for Rule Audit Telemetry
Subscribe at orchestrator level to capture all rule evaluation events as metrics:
```csharp
_integrationCtx.RuleEngine.RuleEvaluated += (_, e) =>
{
    _ruleAuditBuffer.Add(new RuleEvaluationTelemetry
    {
        RunId      = currentRunId,
        RuleKey    = e.RuleKey,
        Success    = e.Success,
        ElapsedMs  = e.Elapsed.TotalMilliseconds,
        EvaluatedAt = DateTime.UtcNow
    });
    Interlocked.Increment(ref _totalRuleEvaluationsThisRun);
};
```

### 4. Rule Keys Defined in This Phase
All `sync.alert.*` keys above, plus:
| Rule Key | Purpose |
|---|---|
| `sync.slo.classify-run` | Returns SLO compliance tier: `Green` \| `Yellow` \| `Red` based on all SLIs |

---

## Integration Points: Defaults Manager

### 1. Alert Record Metadata Stamping
Every emitted alert record gets metadata from `DefaultsManager`:
```csharp
DefaultsManager.SetColumnDefault(editor, "SyncAlerts", "SyncAlertRecord",
    "EmittedAt", ":NOW", isRule: true);
DefaultsManager.SetColumnDefault(editor, "SyncAlerts", "SyncAlertRecord",
    "EmittedBy", ":USERNAME", isRule: true);
DefaultsManager.SetColumnDefault(editor, "SyncAlerts", "SyncAlertRecord",
    "AcknowledgedAt", "", isRule: false);  // empty until acknowledged
DefaultsManager.SetColumnDefault(editor, "SyncAlerts", "SyncAlertRecord",
    "Status", "Open", isRule: false);
```

### 2. Run Completion Audit Record
Every run completion (success or failure) is recorded as an audit row:
```csharp
var completionRecord = new Dictionary<string, object>
{
    ["SchemaId"]   = schema.SchemaID,
    ["RunId"]      = currentRunId,
    ["Outcome"]    = metrics.SuccessRate >= slo.MinSuccessRate ? "SloMet" : "SloBreach"
};
DefaultsManager.Apply(editor, "SyncAudit", "RunCompletion", completionRecord, context);
// Stamps: CompletedAt = :NOW, CompletedBy = :USERNAME
```

---

## Integration Points: Mapping Manager

### 1. Mapping Plan Version in Metrics and Correlation ID
Embed the mapping governance version in every `SyncMetrics` record and every alert correlation ID:
```csharp
metrics.MappingPlanVersion = compiledPlan?.GovernanceVersion ?? "unknown";
metrics.CorrelationId      = $"{schema.SchemaID}.{runId}.{metrics.MappingPlanVersion}";
```

This ensures that when an alert fires, operators can trace it to the exact mapping version used
during that run — critical for debugging field-level data quality alerts.

### 2. Mapping Drift Alert
If drift was detected during preflight but run was allowed to proceed (DriftAction = `Warn`),
include mapping drift status in the alert payload:
```csharp
if (preflight.MappingDriftDetected)
    alertPayload.AdditionalContext["MappingDrift"] = "Detected — review mapping before next run";
```

---

## `SyncMetrics` Additions

```csharp
// Add to existing SyncMetrics class:
public string   MappingPlanVersion      { get; set; }
public string   CorrelationId           { get; set; }
public double   RejectRate              { get; set; }
public double   ConflictRate            { get; set; }
public double   FreshnessLagSeconds     { get; set; }
public int      RetryCount              { get; set; }
public int      RuleEvaluationCount     { get; set; }
public string   SloComplianceTier       { get; set; }  // "Green" | "Yellow" | "Red"
public bool     MappingDriftDetected    { get; set; }
```

## `SyncAlertRecord` Model

```csharp
public class SyncAlertRecord
{
    public string   AlertId          { get; set; }
    public string   SchemaId         { get; set; }
    public string   RunId            { get; set; }
    public string   CorrelationId    { get; set; }
    public string   RuleKey          { get; set; }
    public string   Severity         { get; set; }  // "Critical" | "Warning" | "Info"
    public string   Reason           { get; set; }
    public string   RemediationHint  { get; set; }
    public DateTime EmittedAt        { get; set; }
    public string   EmittedBy        { get; set; }
    public string   Status           { get; set; }  // "Open" | "Acknowledged" | "Resolved"
    public Dictionary<string, object> AdditionalContext { get; set; } = new();
}
```

## `SloProfile` Model

```csharp
public class SloProfile
{
    public string ProfileName           { get; set; }  // "Critical" | "Standard" | "NonCritical"
    public double MinSuccessRate        { get; set; }  // e.g., 0.99 for critical
    public long   MaxDurationMs         { get; set; }
    public double MaxFreshnessLagSeconds { get; set; }
    public double MaxConflictRate       { get; set; }
    public double MaxRejectRate         { get; set; }
    public List<string> AlertRuleKeys   { get; set; } = new();
}
```

---

## Acceptance Criteria
- Alert trigger rules are evaluated after every sync run using `SloProfile.AlertRuleKeys`.
- `RuleEngine.RuleEvaluated` events are captured as `RuleEvaluationTelemetry` per run.
- Every alert record has `EmittedAt`/`EmittedBy` stamped via `DefaultsManager`.
- `SyncMetrics.CorrelationId` includes mapping plan version.
- SLO compliance tier (`Green`/`Yellow`/`Red`) is computed via `sync.slo.classify-run` rule.
- Mapping drift detected during a run is surfaced in alert context when alerts fire.
