# Phase 3 (Enhanced) — Incremental Sync, CDC Strategy, and Rule-Driven Filtering

## Supersedes
`../03-phase3-incremental-sync-and-cdc-strategy.md`

## Objective
Formalize watermark/CDC policies with replay-safe execution, using `RuleEngine` for
CDC record filtering and late-arrival handling, `DefaultsManager` for watermark field
seed values, and `MappingManager` for drift detection on watermark field maps.

---

## Scope
- Watermark modes (timestamp, sequence, composite key).
- CDC filter rule execution path per sync schema.
- Late-arrival handling strategy with rule-based decisions.
- Watermark field default seed via `DefaultsManager`.
- Watermark field map drift detection.

---

## File Targets

| File | Change Description |
|---|---|
| `BeepSync/Helpers/SyncSchemaTranslator.cs` | Apply CDC filter rule before building import config; inject watermark params |
| `BeepSync/Helpers/SyncValidationHelper.cs` | Validate watermark field exists; warn on CDC drift via `MappingManager` |
| `BeepSync/BeepSyncManager.Orchestrator.cs` | Evaluate CDC filter rule before `RunImportAsync`; advance watermark after success |
| `BeepSync/Models/WatermarkPolicy.cs` *(new)* | Watermark mode, field name, overlap window, dedupe strategy |
| `BeepSync/Models/CdcFilterContext.cs` *(new)* | Rule evaluation context for per-record CDC decisions |

---

## Integration Points: Rule Engine

### 1. CDC Record Filter Rule
Before building the import configuration, evaluate a per-schema CDC filter rule to decide
which records qualify for this sync window. The rule returns a filter expression or
a boolean per-record decision:
```csharp
if (_integrationCtx?.RuleEngine != null && schema.WatermarkPolicy?.FilterRuleKey != null)
{
    var policy = schema.RulePolicy?.ExecutionPolicy ?? RuleExecutionPolicy.DefaultSafe;
    var (_, filterResult) = _integrationCtx.RuleEngine.SolveRule(
        schema.WatermarkPolicy.FilterRuleKey,
        new Dictionary<string, object>
        {
            ["watermarkField"]  = schema.WatermarkPolicy.WatermarkField,
            ["lastWatermark"]   = schema.WatermarkPolicy.LastWatermarkValue,
            ["overlapSeconds"]  = schema.WatermarkPolicy.OverlapWindowSeconds,
            ["sourceDs"]        = schema.SourceDataSourceName
        },
        policy);

    if (filterResult.Success && filterResult.Outputs.TryGetValue("filters", out var f))
        config.Filters = (List<AppFilter>)f;
}
```

### 2. Late-Arrival Decision Rule
After the sync window closes but before committing the watermark, evaluate a late-arrival rule:
```csharp
var (_, lateResult) = _integrationCtx.RuleEngine.SolveRule(
    "sync.cdc.late-arrival",
    new Dictionary<string, object>
    {
        ["recordTimestamp"] = record.Timestamp,
        ["windowClose"]     = windowClose,
        ["lagSeconds"]      = (windowClose - record.Timestamp).TotalSeconds
    }, policy);

// lateResult.Outputs["action"] = "include" | "quarantine" | "reject"
```

### 3. Delete / Tombstone Rule
For CDC tombstone handling, evaluate which records represent deletes and how to handle them:
```csharp
// "sync.cdc.tombstone" rule returns: "soft-delete" | "hard-delete" | "mark-inactive"
```

### 4. Rule Keys Defined in This Phase
| Rule Key | Stage | Behaviour |
|---|---|---|
| `sync.cdc.filter` (or per-schema custom key) | Pre-import | Build `AppFilter` list from watermark state |
| `sync.cdc.late-arrival` | Post-window | Decide include / quarantine / reject for late records |
| `sync.cdc.tombstone` | Per deleted record | Decide soft-delete / hard-delete / mark-inactive |

---

## Integration Points: Defaults Manager

### 1. Seed Watermark Field on First Sync
When a destination entity is new and a watermark field needs initialisation:
```csharp
DefaultsManager.SetColumnDefault(editor,
    schema.DestinationDataSourceName, schema.DestinationEntityName,
    schema.WatermarkPolicy.WatermarkField,
    ":MIN_DATETIME", isRule: true);
```
Define `:MIN_DATETIME` as a custom resolver returning `DateTime.MinValue.ToString("o")`.

### 2. Audit Fields on Watermark Advance
After a successful window commit, stamp the watermark advance record:
```csharp
var advanceRecord = new Dictionary<string, object>
{
    ["WatermarkValue"] = newWatermarkValue
};
DefaultsManager.Apply(editor,
    "SyncWatermarks", "WatermarkAudit",
    advanceRecord, context);
// Stamps AdvancedAt = :NOW, AdvancedBy = :USERNAME
```

---

## Integration Points: Mapping Manager

### 1. Watermark Field Drift Detection
Before executing incremental sync, check whether the source watermark field still matches
the persisted field map:
```csharp
var (currentMap, _) = MappingManager.LoadMappingValues(
    schema.SourceDataSourceName, schema.DestinationDataSourceName);

var driftReport = MappingManager.DetectMappingDrift(
    currentMap,
    schema.SourceDataSourceName, schema.SourceEntityName,
    schema.DestinationDataSourceName, schema.DestinationEntityName);

if (driftReport.HasDrift && schema.MappingPolicy?.OnDriftAction == SyncDriftAction.Block)
    return CreateError("CDC field map drift detected. Review mapping before resuming incremental sync.");
```

### 2. Watermark Field Map Version Co-Check
Verify the persisted watermark field mapping was approved in the same governance scope as
the current schema version — warn if versions diverge:
```csharp
if (currentMap.GovernanceVersion != schema.CurrentVersion.MappingVersion)
    preflight.AddWarning("MAPPING-VERSION-MISMATCH",
        $"Schema v{schema.CurrentVersion.Version} references mapping v{schema.CurrentVersion.MappingVersion} " +
        $"but current persisted mapping is v{currentMap.GovernanceVersion}.");
```

---

## `WatermarkPolicy` Model

```csharp
public class WatermarkPolicy
{
    public string WatermarkMode    { get; set; }  // "Timestamp" | "Sequence" | "CompositeKey"
    public string WatermarkField   { get; set; }
    public object LastWatermarkValue { get; set; }
    public int    OverlapWindowSeconds { get; set; } = 300;     // 5 min default overlap
    public string DedupeStrategy   { get; set; }  // "LastWrite" | "SourcePrimary" | "None"
    public string FilterRuleKey    { get; set; }  // IRule key in RuleCatalog
    public string LateArrivalRuleKey { get; set; }
    public string TombstoneRuleKey  { get; set; }
    public bool   ReplayEnabled    { get; set; } = true;
}
```

---

## Acceptance Criteria
- Incremental runs apply CDC filter rule to build `AppFilter` inputs.
- Late-arrival records are classified (include/quarantine/reject) by rule, not hard-coded logic.
- Watermark field is seeded with `:MIN_DATETIME` on first run without manual setup.
- Drift detection on watermark field map emits a `Warning` preflight issue; `Block` action stops run.
- Re-running the same window is idempotent (dedupe strategy respected, no duplicate destination rows).
