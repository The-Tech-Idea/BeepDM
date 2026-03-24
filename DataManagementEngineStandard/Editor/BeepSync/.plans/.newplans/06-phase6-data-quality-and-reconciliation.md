# Phase 6 (Enhanced) — Data Quality, Reconciliation, Rules-Gated DQ, and Defaults Fill

## Supersedes
`../06-phase6-data-quality-and-reconciliation.md`

## Objective
Layer `RuleEngine`-driven DQ gate rules per field and per entity, use `DefaultsManager` to
fill missing destination fields before the DQ check, and use `MappingManager` quality scoring
as a hard gate before sync applies.

---

## Scope
- DQ gate rule evaluation per field/entity.
- Missing-field default fill before DQ check.
- Mapping quality score threshold as preflight gate.
- Reject channel for DQ failures.
- Reconciliation report model.

---

## File Targets

| File | Change Description |
|---|---|
| `BeepSync/Helpers/SyncValidationHelper.cs` | Add `EvaluateDqGateRules(...)`, `FillMissingFieldsWithDefaults(...)`, `CheckMappingQualityGate(...)` |
| `BeepSync/Helpers/SyncProgressHelper.cs` | Add `BuildReconciliationReport(...)` with DQ reject counts |
| `BeepSync/BeepSyncManager.Orchestrator.cs` | Apply DQ gate sequence: fill defaults → evaluate rules → quarantine rejects |
| `BeepSync/Models/DqGateResult.cs` *(new)* | Per-record + per-field DQ result with rule key and reason code |
| `BeepSync/Models/SyncReconciliationReport.cs` *(enhancement)* | Add DQ reject breakdown and mapping quality section |

---

## Integration Points: Rule Engine

### 1. Per-Field DQ Gate Rules
Before writing each destination record, evaluate all registered DQ rules for the entity:
```csharp
if (_integrationCtx?.RuleEngine != null && schema.DqPolicy?.RuleKeys?.Count > 0)
{
    var dqContext = new Dictionary<string, object>
    {
        ["record"]     = destinationRecord,
        ["entityName"] = schema.DestinationEntityName,
        ["schemaId"]   = schema.SchemaID
    };

    foreach (var ruleKey in schema.DqPolicy.RuleKeys)
    {
        var (_, dqResult) = _integrationCtx.RuleEngine.SolveRule(
            ruleKey, dqContext,
            schema.RulePolicy?.ExecutionPolicy ?? RuleExecutionPolicy.DefaultSafe);

        if (!dqResult.Success)
        {
            var dqFailure = new DqGateResult
            {
                RuleKey    = ruleKey,
                Passed     = false,
                ReasonCode = dqResult.Outputs.GetValueOrDefault("reasonCode")?.ToString() ?? "DQ-FAIL",
                FieldName  = dqResult.Outputs.GetValueOrDefault("field")?.ToString()
            };
            dqFailures.Add(dqFailure);
        }
    }

    if (dqFailures.Any())
        return RouteToRejectChannel(destinationRecord, dqFailures, schema);
}
```

### 2. Entity-Level DQ Threshold Rule
After processing a batch, evaluate an entity-level threshold rule:
```csharp
var (_, thresholdResult) = _integrationCtx.RuleEngine.SolveRule(
    "sync.dq.batch-threshold",
    new Dictionary<string, object>
    {
        ["totalRecords"] = batch.Count,
        ["rejectCount"]  = rejectCount,
        ["rejectRate"]   = (double)rejectCount / batch.Count,
        ["maxRejectRate"] = schema.DqPolicy?.MaxRejectRatePercent / 100.0 ?? 0.05
    }, policy);
// If thresholdResult.Outputs["action"] = "AbortRun" → fail the entire sync run
```

### 3. Built-In DQ Rule Templates
| Rule Key | Description |
|---|---|
| `sync.dq.required-fields` | Fails if any required field is null/empty |
| `sync.dq.type-validity` | Fails if field value cannot be coerced to declared type |
| `sync.dq.key-integrity` | Fails if primary key is null or duplicate within batch |
| `sync.dq.referential-check` | Fails if foreign key value does not exist in reference set |
| `sync.dq.batch-threshold` | Fails the run if reject rate exceeds threshold |

### 4. Rule Keys Defined in This Phase
Include all DQ rule keys above plus a `sync.dq.custom.{entityName}` convention for entity-specific rules.

---

## Integration Points: Defaults Manager

### 1. Fill Missing Destination Fields Before DQ Check
Before evaluating DQ rules, fill fields that have no source mapping but have defaults defined:
```csharp
// Step 1: Mapping produces destination record with mapped fields
var mappedRecord = MappingManager.MapObjectToAnother(sourceRecord, destEntityStructure);

// Step 2: Fill remaining fields from EntityDefaultsProfile BEFORE DQ check
if (schema.DefaultsPolicy?.ApplyOnInsert == true)
{
    DefaultsManager.Apply(editor,
        schema.DestinationDataSourceName,
        schema.DestinationEntityName,
        mappedRecord, context);
    // e.g., fills: CreatedAt = :NOW, CreatedBy = :USERNAME, Status = "Active", Version = "1"
}

// Step 3: Now evaluate DQ rules — all required fields should be present
EvaluateDqGateRules(mappedRecord, schema);
```

### 2. Reject Record Defaults
Records routed to the reject channel also get defaults applied:
```csharp
DefaultsManager.SetColumnDefault(editor, schema.RejectChannelDs, schema.RejectChannelEntity,
    "RejectedAt", ":NOW", isRule: true);
DefaultsManager.SetColumnDefault(editor, schema.RejectChannelDs, schema.RejectChannelEntity,
    "RejectionReason", "DQ-FAIL", isRule: false);
```

### 3. Reconciliation Timestamp
Final reconciliation report record is stamped automatically:
```csharp
var reconciliationRecord = new SyncReconciliationReport { ... };
DefaultsManager.Apply(editor, "SyncAudit", "ReconciliationRecord", reconciliationRecord, context);
// Stamps: GeneratedAt = :NOW, GeneratedBy = :USERNAME
```

---

## Integration Points: Mapping Manager

### 1. Mapping Quality Score as Hard Gate
Before running DQ checks at all, verify mapping quality:
```csharp
var qualityCheck = MappingManager.ValidateMappingQuality(
    schema.SourceDataSourceName, schema.DestinationDataSourceName, schema.SourceEntityName);

if (qualityCheck.QualityScore < schema.MappingPolicy?.MinQualityScore ?? 70)
{
    return CreateError(
        $"Mapping quality score {qualityCheck.QualityScore} is below threshold " +
        $"{schema.MappingPolicy.MinQualityScore}. DQ checks cannot run on low-quality mapping.");
}

reconciliationReport.MappingQualityScore = qualityCheck.QualityScore;
reconciliationReport.MappingQualityBand  = qualityCheck.QualityBand;
```

### 2. Unmapped Fields Report
After mapping, report which destination fields received no source value and no default:
```csharp
var unmappedFields = destEntityStructure.Fields
    .Where(f => !mappedRecord.ContainsKey(f.FieldName) && !DefaultsManager.HasDefault(schema.DestinationDataSourceName, schema.DestinationEntityName, f.FieldName))
    .Select(f => f.FieldName)
    .ToList();

if (unmappedFields.Any())
    reconciliationReport.UnmappedRequiredFields = unmappedFields;
```

---

## `SyncReconciliationReport` Model (Enhanced)

```csharp
public class SyncReconciliationReport
{
    // Run identity
    public string   SchemaId              { get; set; }
    public string   RunId                 { get; set; }
    public DateTime GeneratedAt           { get; set; }
    public string   GeneratedBy           { get; set; }

    // Row counts
    public int SourceRowsScanned          { get; set; }
    public int DestRowsWritten            { get; set; }
    public int DestRowsInserted           { get; set; }
    public int DestRowsUpdated            { get; set; }
    public int DestRowsSkipped            { get; set; }
    public int RejectCount                { get; set; }
    public int QuarantineCount            { get; set; }
    public int DefaultsFillCount          { get; set; }  // rows where defaults filled gaps
    public int ConflictCount              { get; set; }

    // DQ summary
    public double RejectRate              { get; set; }
    public bool   RunAbortedByThreshold   { get; set; }
    public List<DqGateResult> DqFailures  { get; set; } = new();

    // Mapping quality
    public int    MappingQualityScore     { get; set; }
    public string MappingQualityBand      { get; set; }
    public List<string> UnmappedRequiredFields { get; set; } = new();
}
```

---

## DQ Gate Execution Order (Per Record)

```
Source record
    → MappingManager.MapObjectToAnother(...)         [field-level transform]
    → DefaultsManager.Apply(...)                     [fill gaps with defaults]
    → RuleEngine.SolveRule(dq.required-fields)       [required field check]
    → RuleEngine.SolveRule(dq.type-validity)         [type coercion check]
    → RuleEngine.SolveRule(dq.key-integrity)         [primary key check]
    → RuleEngine.SolveRule(dq.custom.{entity})       [entity-specific rules]
    → Pass: write to destination
    → Fail: route to reject channel
```

---

## Acceptance Criteria
- DQ rules are registered in `RuleCatalog` and referenced by key in `DataSyncSchema.DqPolicy`.
- `DefaultsManager.Apply(...)` runs before DQ gate evaluation — required fields are filled.
- Mapping quality score below threshold blocks DQ evaluation and fails the run.
- Every reject record is written to the reject channel with `RejectedAt`/`RejectionReason`.
- `SyncReconciliationReport` includes `DefaultsFillCount`, `MappingQualityScore`, and `DqFailures`.
- Batch reject rate exceeding threshold triggers `AbortRun` action via `sync.dq.batch-threshold` rule.
