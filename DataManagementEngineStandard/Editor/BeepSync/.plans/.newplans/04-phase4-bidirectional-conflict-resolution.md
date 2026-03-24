# Phase 4 (Enhanced) — Bidirectional Conflict Resolution with Rule Engine

## Supersedes
`../04-phase4-bidirectional-conflict-resolution.md`

## Objective
Replace hard-coded source-wins / destination-wins with `RuleEngine`-evaluated conflict
resolution, use `DefaultsManager` to stamp audit fields on the winning record, and use
`MappingManager` precedence rules for per-field tie-breaking.

---

## Scope
- Conflict detection per synced record.
- Rule-driven conflict policy per schema.
- Per-field resolution via `MappingManager.Rules.cs`.
- Evidence capture: before/after, rule used, winner.
- Defaults applied to resolved record before write.
- Quarantine path for unresolvable conflicts.

---

## File Targets

| File | Change Description |
|---|---|
| `BeepSync/BeepSyncManager.Orchestrator.cs` | Call conflict rule before reverse import; handle quarantine |
| `BeepSync/Helpers/SyncSchemaTranslator.cs` | Pass conflict policy into `DataImportConfiguration` |
| `BeepSync/Helpers/FieldMappingHelper.cs` | Add `GetFieldConflictRules(...)` to query `MappingManager.Rules` |
| `BeepSync/Models/ConflictEvidence.cs` *(new)* | Evidence record: before/after values, rule key, winner, reason code |
| `BeepSync/Models/ConflictPolicy.cs` *(new)* | Policy: resolution mode, rule key, quarantine path, field overrides |

---

## Integration Points: Rule Engine

### 1. Record-Level Conflict Resolution Rule
When a bidirectional sync detects a conflict (both source and destination have changed since
last sync), evaluate the conflict rule to pick the winner:
```csharp
if (_integrationCtx?.RuleEngine != null && schema.ConflictPolicy?.ResolutionRuleKey != null)
{
    var (_, resolution) = _integrationCtx.RuleEngine.SolveRule(
        schema.ConflictPolicy.ResolutionRuleKey,
        new Dictionary<string, object>
        {
            ["sourceRecord"]    = sourceRecord,
            ["destRecord"]      = destinationRecord,
            ["sourceTimestamp"] = sourceRecord.ModifiedAt,
            ["destTimestamp"]   = destinationRecord.ModifiedAt,
            ["entityName"]      = schema.SourceEntityName,
            ["schemaId"]        = schema.SchemaID
        },
        schema.RulePolicy?.ExecutionPolicy ?? RuleExecutionPolicy.DefaultSafe);

    // resolution.Outputs["winner"]     = "source" | "destination" | "quarantine"
    // resolution.Outputs["reasonCode"] = "LATEST-WINS" | "CUSTOM" | "UNRESOLVABLE"
}
```

### 2. Built-In Conflict Rule Catalog
Register these rules in `RuleCatalog` at bootstrap:
| Rule Key | Behaviour |
|---|---|
| `sync.conflict.source-wins` | Always returns `winner = source` |
| `sync.conflict.destination-wins` | Always returns `winner = destination` |
| `sync.conflict.latest-timestamp-wins` | Compares `sourceTimestamp` vs `destTimestamp`; picks newer |
| `sync.conflict.fail-on-conflict` | Returns `winner = quarantine`; forces manual review |

Schemas reference one of these keys (or a custom registered rule) via `ConflictPolicy.ResolutionRuleKey`.

### 3. Rule Evaluated Subscription for Conflict Audit
Subscribe to `RuleEngine.RuleEvaluated` during sync to capture every conflict decision into `ConflictEvidence`:
```csharp
_integrationCtx.RuleEngine.RuleEvaluated += (_, e) =>
{
    if (e.RuleKey.StartsWith("sync.conflict."))
        _conflictAuditLog.Add(new ConflictEvidence { RuleKey = e.RuleKey, Elapsed = e.Elapsed, ... });
};
```

### 4. Rule Keys Defined in This Phase
| Rule Key | Stage | Behaviour |
|---|---|---|
| `sync.conflict.source-wins` | Bidirectional conflict | Source always wins |
| `sync.conflict.destination-wins` | Bidirectional conflict | Destination always wins |
| `sync.conflict.latest-timestamp-wins` | Bidirectional conflict | Newer timestamp wins |
| `sync.conflict.fail-on-conflict` | Bidirectional conflict | Quarantine all conflicts |

---

## Integration Points: Defaults Manager

### 1. Stamp Audit Fields on Winning Record
After the conflict rule picks a winner, apply defaults to the resolved record before writing:
```csharp
var winner = BuildWinnerRecord(resolution, sourceRecord, destinationRecord);

if (schema.DefaultsPolicy?.ApplyOnInsert == true || schema.DefaultsPolicy?.ApplyOnUpdate == true)
{
    DefaultsManager.Apply(editor,
        schema.DestinationDataSourceName, schema.DestinationEntityName,
        winner, context);
    // Stamps: LastModifiedAt = :NOW, LastModifiedBy = :USERNAME, ConflictResolved = "true"
}
```

### 2. Quarantine Record Defaults
Records routed to the quarantine path also get defaults stamped:
```csharp
DefaultsManager.SetColumnDefault(editor, "SyncQuarantine", "ConflictRecord",
    "QuarantinedAt", ":NOW", isRule: true);
DefaultsManager.SetColumnDefault(editor, "SyncQuarantine", "ConflictRecord",
    "QuarantinedBy", ":USERNAME", isRule: true);
DefaultsManager.SetColumnDefault(editor, "SyncQuarantine", "ConflictRecord",
    "ReviewStatus", "Pending", isRule: false);
```

---

## Integration Points: Mapping Manager

### 1. Per-Field Conflict Resolution via `MappingManager.Rules.cs`
Some conflicts are at the field level, not the record level. Use `MappingManager.Rules`
conditional rules to specify per-field tie-breaking:
```csharp
// Example: for field "Status", source wins; for "Price", destination wins if destination > 0
var fieldRules = MappingManager.GetConditionalRules(
    schema.SourceDataSourceName, schema.DestinationDataSourceName, schema.SourceEntityName);

foreach (var fieldRule in fieldRules.Where(r => r.Stage == MappingRuleStage.ConflictResolution))
{
    var (fieldWinner, _) = fieldRule.Evaluate(new { source = sourceRecord, dest = destinationRecord });
    winnerRecord[fieldRule.TargetField] = fieldWinner;
}
```

### 2. Precedence Order for Field Resolution
Precedence is documented and enforced in this order:
1. Explicit `MappingManager.Rules.cs` conditional rule (highest)
2. `ConflictPolicy.ResolutionRuleKey` record-level rule result
3. Schema-level `SyncDirection` fallback (source-wins if no rule matches)

---

## `ConflictPolicy` Model

```csharp
public class ConflictPolicy
{
    public string ResolutionRuleKey  { get; set; }  // IRule key in RuleCatalog
    public string QuarantineDsName   { get; set; }  // where unresolved records go
    public string QuarantineEntity   { get; set; }
    public bool   CaptureEvidence    { get; set; } = true;
    public int    MaxConflictsPerRun { get; set; } = -1;  // -1 = unlimited
    public string OnMaxExceededAction { get; set; } = "Abort";  // "Abort" | "Continue" | "QuarantineRest"
}
```

## `ConflictEvidence` Model

```csharp
public class ConflictEvidence
{
    public string EntityName      { get; set; }
    public string RecordKey       { get; set; }
    public object SourceValues    { get; set; }
    public object DestinationValues { get; set; }
    public string Winner          { get; set; }    // "source" | "destination" | "quarantine"
    public string ReasonCode      { get; set; }
    public string RuleKey         { get; set; }
    public TimeSpan RuleElapsed   { get; set; }
    public DateTime DetectedAt    { get; set; }
}
```

---

## Acceptance Criteria
- Bidirectional conflicts are resolved by an `IRule` in `RuleCatalog`, not by hard-coded logic.
- Winning record has audit defaults applied (stamped by `DefaultsManager`).
- Unresolvable conflicts are routed to quarantine with `ReviewStatus = Pending`.
- `ConflictEvidence` is captured for every resolved conflict.
- `MappingManager.Rules.cs` field-level rules override record-level rule results when present.
- Conflict rule key is stored in `DataSyncSchema.ConflictPolicy` and validated during preflight.
