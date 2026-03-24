# Phase 5 (Enhanced) — Reliability, Retry, Idempotency, and Rule-Classified Errors

## Supersedes
`../05-phase5-reliability-retry-and-idempotency.md`

## Objective
Harden reliability with `RuleEngine`-classified retry categories, `DefaultsManager`-stamped
checkpoint metadata, and `MappingManager` compiled mapping plan reuse across checkpoint resumes
for consistent field-level behaviour across retried batches.

---

## Scope
- Retry classification via rule (transient / non-retry / policy-driven).
- Checkpoint model: persist-progress, resume partial run.
- Idempotent write strategy using sync key.
- Defaults applied to checkpoint artifacts.
- Compiled mapping plan reused across checkpoint resume.

---

## File Targets

| File | Change Description |
|---|---|
| `BeepSync/BeepSyncManager.Orchestrator.cs` | Classify errors via rule; load checkpoint on resume; advance or abort based on rule |
| `BeepSync/Helpers/SyncSchemaTranslator.cs` | Embed compiled mapping plan into `DataImportConfiguration` for checkpoint resumes |
| `BeepSync/Models/SyncCheckpoint.cs` *(new)* | Checkpoint artifact: run id, progress offset, mapped fields snapshot, retry state |
| `BeepSync/Models/RetryPolicy.cs` *(new)* | Retry policy: max attempts, backoff mode, retry rule key, non-retryable categories |

---

## Integration Points: Rule Engine

### 1. Error Category Rule
When an error is returned from `DataImportManager.RunImportAsync`, evaluate a retry classification
rule to decide whether to retry, abort, or escalate:
```csharp
if (_integrationCtx?.RuleEngine != null && schema.RetryPolicy?.ErrorCategoryRuleKey != null)
{
    var (_, categoryResult) = _integrationCtx.RuleEngine.SolveRule(
        schema.RetryPolicy.ErrorCategoryRuleKey,
        new Dictionary<string, object>
        {
            ["errorCode"]    = importError.ErrorCode,
            ["errorMessage"] = importError.Message,
            ["attemptCount"] = currentAttempt,
            ["entityName"]   = schema.DestinationEntityName
        },
        schema.RulePolicy?.ExecutionPolicy ?? RuleExecutionPolicy.DefaultSafe);

    // categoryResult.Outputs["category"] = "Transient" | "Validation" | "Conflict" | "Fatal"
    // categoryResult.Outputs["action"]   = "Retry" | "Abort" | "Quarantine" | "Escalate"
}
```

### 2. Built-In Retry Category Rules
| Rule Key | Category Mapping |
|---|---|
| `sync.retry.transient-transport` | Network/timeout errors → `Transient → Retry` |
| `sync.retry.validation-failure` | DQ/constraint errors → `Validation → Quarantine` |
| `sync.retry.conflict-error` | Conflict write errors → `Conflict → policy` |
| `sync.retry.fatal` | Schema/permission errors → `Fatal → Abort` |

### 3. Checkpoint Resume Decision Rule
On restart, evaluate whether a stale checkpoint is safe to resume:
```csharp
var (_, resumeDecision) = _integrationCtx.RuleEngine.SolveRule(
    "sync.checkpoint.resume-safe",
    new Dictionary<string, object>
    {
        ["checkpointAge"]       = DateTime.UtcNow - checkpoint.SavedAt,
        ["maxResumeWindowHours"] = schema.RetryPolicy?.MaxResumeWindowHours ?? 24,
        ["partialRunPercent"]   = checkpoint.ProgressPercent
    }, policy);
// resumeDecision.Outputs["safe"] = true | false
```

### 4. Rule Keys Defined in This Phase
| Rule Key | Stage | Behaviour |
|---|---|---|
| `sync.retry.transient-transport` | Error classification | Transient → Retry |
| `sync.retry.validation-failure` | Error classification | Validation → Quarantine |
| `sync.retry.conflict-error` | Error classification | Conflict → policy-driven |
| `sync.retry.fatal` | Error classification | Fatal → Abort |
| `sync.checkpoint.resume-safe` | Checkpoint resume | Decide if stale checkpoint is safe |

---

## Integration Points: Defaults Manager

### 1. Stamp Checkpoint Metadata
On every checkpoint save:
```csharp
var checkpoint = new SyncCheckpoint
{
    RunId       = runId,
    SchemaId    = schema.SchemaID,
    SavedOffset = processedOffset
};

DefaultsManager.Apply(editor, "SyncCheckpoints", "SyncCheckpoint", checkpoint, context);
// Stamps: SavedAt = :NOW, SavedBy = :USERNAME, Status = "InProgress"
```

### 2. Stamp Retry Attempt Record
Each retry attempt is recorded as an audit row:
```csharp
DefaultsManager.SetColumnDefault(editor, "SyncRetries", "RetryRecord",
    "AttemptedAt", ":NOW", isRule: true);
DefaultsManager.SetColumnDefault(editor, "SyncRetries", "RetryRecord",
    "AttemptedBy", ":USERNAME", isRule: true);
DefaultsManager.SetColumnDefault(editor, "SyncRetries", "RetryRecord",
    "RetryStatus", "Pending", isRule: false);
```

---

## Integration Points: Mapping Manager

### 1. Compiled Mapping Plan Across Retries
Before the first attempt, compile the mapping plan once and cache it:
```csharp
var compiledPlan = MappingManager.GetOrCompileMappingPlan(
    schema.SourceDataSourceName, schema.DestinationDataSourceName, schema.SourceEntityName);

// Store compiled plan ID in checkpoint — reuse on resume
checkpoint.CompiledMappingPlanId = compiledPlan.PlanId;
```

On checkpoint resume:
```csharp
var compiledPlan = MappingManager.GetCompiledPlanById(checkpoint.CompiledMappingPlanId)
    ?? MappingManager.GetOrCompileMappingPlan(...);  // fallback: recompile
```

### 2. Mapping Plan Consistency Check on Resume
When resuming from checkpoint, verify the mapping plan has not changed (governance version check):
```csharp
if (compiledPlan.GovernanceVersion != checkpoint.MappingVersion)
    preflight.AddWarning("MAPPING-CHANGED-SINCE-CHECKPOINT",
        "Mapping was modified after checkpoint was saved. Field mapping may differ from original run.");
```

---

## `SyncCheckpoint` Model

```csharp
public class SyncCheckpoint
{
    public string   RunId                   { get; set; }
    public string   SchemaId                { get; set; }
    public int      ProcessedOffset         { get; set; }
    public int      TotalExpected           { get; set; }
    public double   ProgressPercent         => TotalExpected > 0 ? 100.0 * ProcessedOffset / TotalExpected : 0;
    public DateTime SavedAt                 { get; set; }
    public string   SavedBy                 { get; set; }
    public string   Status                  { get; set; }  // "InProgress" | "Completed" | "Failed" | "Stale"
    public int      AttemptCount            { get; set; }
    public string   LastErrorCategory       { get; set; }
    public string   CompiledMappingPlanId   { get; set; }
    public string   MappingVersion          { get; set; }  // governance version at checkpoint time
    public object   LastProcessedKeyValue   { get; set; }  // idempotency anchor
}
```

---

## Idempotency Contract

| Scenario | Strategy |
|---|---|
| Same window re-run | Sync key deduplication: if `DestKey` already exists → Update (not Insert) |
| Retry after partial batch | Resume from `LastProcessedKeyValue` in checkpoint |
| Retry after full failure | Replay from `ProcessedOffset = 0` with same compiled mapping plan |
| Quarantined record retry | Requires manual review → re-queue via separate `RequeueQuarantineAsync` |

---

## Acceptance Criteria
- Error categorization uses `IRule` — no hardcoded if/else on error codes.
- Every checkpoint artifact has `SavedAt`/`SavedBy` stamped via `DefaultsManager`.
- Compiled mapping plan ID is persisted in checkpoint and reused on resume.
- Transient errors are retried up to `RetryPolicy.MaxAttempts`; fatal errors abort immediately.
- Re-running the same window produces zero duplicate destination rows (idempotent).
