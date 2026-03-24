# Phase 17 — Enterprise Operational Runbook

| Attribute      | Value                                       |
|----------------|---------------------------------------------|
| Phase          | 17                                          |
| Status         | planned                                     |
| Priority       | Medium                                      |
| Dependencies   | All prior phases (especially 11–16)         |
| Est. Effort    | 3 days (cross-cutting documentation)        |

---

## Overview

This runbook is the **day-2 operations guide** for teams running the Beep FileManager in production.  
It covers five operational scenarios:

1. [Onboarding a new file source](#1-runbook-onboarding-a-new-file-source)
2. [Investigating ingestion failures](#2-runbook-investigating-ingestion-failures)
3. [Schema evolution procedure](#3-runbook-schema-evolution-procedure)
4. [Incident response for a data quality breach](#4-runbook-incident-response-for-a-data-quality-breach)
5. [Capacity planning for large-file workloads](#5-runbook-capacity-planning-for-large-file-workloads)

---

## 1. Runbook: Onboarding a New File Source

**Goal:** Register a new CSV/file source so it can be ingested by the pipeline reliably.

### Pre-requisites

| Item | Check |
|------|-------|
| A sample file (at least 1 000 rows) | Available |
| Target entity name confirmed with data owner | Confirmed |
| Source system name and tenant ID assigned | Assigned |
| Data classification requirements reviewed | Reviewed with data steward |
| Masking/tokenization keys provisioned (if PII expected) | Keys in vault |
| RBAC roles assigned for the service account | Assigned |

### Steps

#### Step 1 — File reconnaissance

```bash
# Check file encoding
file --mime-encoding /path/to/import.csv

# Count rows
wc -l /path/to/import.csv

# Inspect first 5 lines
head -5 /path/to/import.csv

# Check for BOM (Byte Order Mark) — common with Excel exports
xxd /path/to/import.csv | head -1
```

#### Step 2 — Register connection in Beep ConfigEditor

```csharp
var connProps = new ConnectionProperties
{
    ConnectionName  = "crm-contacts",          // logical name
    FileName        = "contacts.csv",
    FilePath        = "/data/crm/EU/",         // tenant+region path
    DatabaseType    = DataSourceType.CSV,
    Category        = DatasourceCategory.FILE,
    Delimiter       = ',',
    HasHeader       = true,
    Encoding        = "UTF-8"
};
editor.ConfigEditor.AddDataConnection(connProps);
editor.ConfigEditor.SaveDataconnectionsValues();
```

#### Step 3 — Run CSVAnalyser to infer schema

```csharp
var analyser = new CSVAnalyser(editor);
var schema = analyser.AnalyzeCSVFile("crm-contacts");
// Review: schema.Entities[0] — column names, inferred types, null rates, uniqueness ratios
```

#### Step 4 — Register schema in IFileSchemaRegistry

```csharp
// Convert EntityStructure → FileSchemaVersion (adapter in Phase 12)
var schemaVersion = SchemaConverter.ToFileSchemaVersion(schema, "CRM", "contacts");
await schemaRegistry.RegisterAsync("CRM", "contacts", schemaVersion);
```

#### Step 5 — Run classification

```csharp
var classificationResult = await classificationEngine.ClassifySchemaAsync(schemaVersion, columnSamples);
// Review: classificationResult.HighSensitivityColumns
// Configure masking policies for any PII columns found
maskingStore.SetPolicy("PII-Email",      new ColumnMaskingPolicy { Strategy = MaskingStrategy.PartialMask });
maskingStore.SetPolicy("PII-Phone",      new ColumnMaskingPolicy { Strategy = MaskingStrategy.PartialMask });
maskingStore.SetPolicy("PII-CreditCard", new ColumnMaskingPolicy { Strategy = MaskingStrategy.PartialMask });
```

#### Step 6 — Configure governance

```csharp
// Set tenant path
pathResolver.Configure("CRM", "/data/crm/{TenantId}/{DataRegion}/");

// Set RLS (if multi-tenant CSV)
rlsFilter.Configure("contacts", new RlsRule
{
    TenantKeyColumn = "organization_id",
    TenantValueSource = RlsValueSource.TenantContextId
});

// Configure data residency
residencyPolicy.Configure("EU", allowedRegions: new[]{"EU"});
```

#### Step 7 — Run a dry-run ingestion

```csharp
var descriptor = new FileIngestionDescriptor
{
    JobId          = Guid.NewGuid().ToString(),
    FilePath       = "/data/crm/EU/contacts.csv",
    FileChecksum   = await FileChecksumHelper.ComputeChecksumAsync("/data/crm/EU/contacts.csv"),
    TargetEntityName = "contacts",
    SourceSystem   = "CRM",
    TenantId       = "ACME"
};

// DryRun mode: validates schema, runs classification, reports issues — NO rows written
var result = await ingester.DryRunAsync(descriptor);
// Review result.ValidationIssues and result.SchemaDriftReport
```

#### Step 8 — Run production ingestion

```csharp
var status = await ingester.IngestAsync(descriptor, progressCallback);
// status.State should == IngestionState.Complete
```

#### Step 9 — Verify

- Check `IIngestionStateStore.GetStatusAsync(jobId).State == Complete`.
- Verify row count in target matches `status.RowsCommitted`.
- Check `IDeadLetterStore.GetByJobAsync(jobId)` is empty (or review accepted dead-letters).
- Confirm schema version is visible in catalog (Phase 12).

---

## 2. Runbook: Investigating Ingestion Failures

**Trigger:** Job in `Failed` or `Quarantined` state, or `RowRejectionRate > 5%`.

### Decision tree

```
Job state?
├── Quarantined
│   └── Check: IIngestionStateStore.GetStatusAsync → LastMessage
│       ├── "Schema drift: breaking"  → go to §2.1 (schema issue)
│       ├── "PII policy violation"    → go to §2.2 (governance)
│       └── "Header validation failed" → go to §2.3 (file corrupt)
│
├── Failed
│   └── Check: LastMessage
│       ├── IOException / FileNotFound → go to §2.4 (IO error)
│       └── UnrecoverableParseError   → go to §2.3 (file corrupt)
│
└── Complete but high rejection rate
    └── Check: IDeadLetterStore.GetByJobAsync(jobId)
        ├── TypeConversionError in column? → go to §2.5 (type mismatch)
        └── SchemaViolation (wrong column count)? → go to §2.3 (file corrupt)
```

### §2.1 Schema drift issue

```
1. Get drift report: schemaRegistry.DetectDriftAsync(sourceSystem, entity, candidateSchema)
2. Review SchemaDriftReport.DroppedColumns and TypeChanges
3. Options:
   a. Source system changed intentionally → update schema mapping in your ETL and re-register schema
   b. Source sent wrong file → reject the file, notify source system team
4. Move job from Quarantined → Pending (new job) after fix
```

### §2.2 PII policy violation

```
1. Identify which column triggered the policy: ClassifiedColumnAccessEvent audit log
2. If column should not contain PII:
   a. Notify source system team — upstream data quality issue
   b. Quarantine the file permanently until upstream is fixed
3. If column legitimately contains PII:
   a. Verify masking policy is registered for the classification
   b. Re-run ingestion with correct masking policy
```

### §2.3 File corrupt or malformed

```
1. Inspect dead-letter entries: IDeadLetterStore.GetByJobAsync → RawLine for examples
2. Common causes and fixes:
   a. Unmatched quotes: Use CsvTextFieldParser strict mode (Phase 2)
   b. Wrong delimiter: Re-detect with CSVAnalyser.DetectDelimiter
   c. Broken encoding: Re-export from source with UTF-8 BOM
   d. Truncated file: Check if file transfer (SFTP/S3 copy) completed cleanly — compare file sizes
3. After source fix: start new job (new JobId) — do NOT reuse the failed job's ID
```

### §2.4 IO / file-access error

```
1. Check file exists and is readable by the service account:
   Test-Path "C:\path\to\file.csv" (PowerShell)
   icacls "C:\path\to\file.csv"   (ACL check)

2. Check for file locks (Windows):
   Handle.exe -a -p <pid> | findstr /i csv   (Sysinternals)

3. If NFS/SMB mount:
   Check network connectivity and mount point health
   Remount if stale: umount / mount -a

4. After fix: job in Suspended state can be resumed via ingester.ResumeAsync(jobId)
   OR if job is in Failed state: start a new job
```

### §2.5 Type mismatch dead-letters

```
1. Query dead-letter entries for TypeConversionError:
   var entries = await deadLetterStore.GetByJobAsync(jobId);
   var typeErrors = entries.Where(e => e.ErrorCategory == "TypeConversionError");

2. Group by ColumnName and sample RawLine
3. Determine root cause:
   a. New locale format (e.g. "1.234,56" vs "1,234.56")  → Update CSVTypeMapper locale
   b. Enum value not in expected set → Update source system or extend mapping table
   c. Date format changed (MM/DD/YYYY vs DD-MM-YYYY) → Update date pattern in SchemaVersion

4. Fix: update the schema in the registry (new version), re-run ingestion
5. For rows already dead-lettered: export dead-letter CSV, fix manually, re-import as a separate job
```

---

## 3. Runbook: Schema Evolution Procedure

**Trigger:** Source system changes the CSV format (new columns, renamed columns, different types).

### Compatibility matrix

| Change | Action |
|--------|--------|
| New nullable column | Register new schema version; existing consumers receive null for that column |
| New NOT-NULL column | Coordinate with target team first; register new version **after** target schema updated |
| Column renamed | Register with `ColumnRename` record; update ETL mappings |
| Column dropped | Remove from active mapping; verify target can handle missing data |
| Type widening (int→long) | Register new version; no impact usually |
| Type narrowing (decimal→string) | Block ingestion; fix source first; coordinate with target |
| Encoding changed | Update `FileSchemaVersion.Encoding`; test with sample file |
| Delimiter changed | Update `FileSchemaVersion.Delimiter`; re-analyse with CSVAnalyser |

### Steps

1. Receive sample file with new format.
2. Run `CSVAnalyser` on the sample to get a new candidate schema.
3. Run `schemaRegistry.DetectDriftAsync(...)` to get a `SchemaDriftReport`.
4. Review `SchemaDriftReport.IsBreaking`:
   - `false`: Proceed to step 5.
   - `true`: Stop — coordinate with source and target teams before proceeding.
5. For non-breaking changes: run `schemaRegistry.RegisterAsync(...)` to create new version.
6. Update ETL column mappings if column names changed.
7. Update masking policies if new PII columns were added.
8. Notify downstream consumers of the new schema version.
9. Run a dry-run ingestion on the new file format to confirm no issues.
10. Update `.plans/README.md` with the change date and version bump.

---

## 4. Runbook: Incident Response for a Data Quality Breach

**Trigger:** A data quality incident — PII leaked, wrong data ingested to wrong tenant, large dead-letter spike.

### Severity classification

| Severity | Condition | Response SLA |
|----------|-----------|-------------|
| SEV1 | PII written unmasked to production store | Immediate (within 15 min) |
| SEV2 | Cross-tenant data leak | Immediate (within 15 min) |
| SEV3 | > 10% row rejection rate in production job | Within 1 hour |
| SEV4 | Schema drift quarantine on a scheduled job | Within 4 hours |

### SEV1/SEV2 Response steps

```
T+0:
    1. STOP all active ingestion jobs immediately:
       foreach jobId in activeJobs:
           await stateStore.TransitionAsync(jobId, Suspended, "SEV1 incident: emergency stop")

T+5min:
    2. Identify scope:
       a. Who ingested the affected data? → IIngestionStateStore timeline
       b. What job IDs are affected? → jobs in Complete state in the incident window
       c. What columns were leaked? → ClassifiedColumnAccessEvent audit log

T+15min:
    3. Contain:
       a. If PII in target DB: revoke read access to affected tables/rows immediately
       b. If cross-tenant: quarantine tenant partition

T+1h:
    4. Remediate:
       a. Delete or mask affected rows in target
       b. Replay the ingestion with correct masking policies
       c. Verify row counts match expected (no data loss after remediation)

T+4h:
    5. Notify:
       a. Data Protection Officer (GDPR requires notification within 72 hours of discovery)
       b. Affected tenants

T+24h:
    6. Post-incident review:
       a. Root cause analysis with 5-whys
       b. Update masking policies, add test coverage
       c. Update this runbook with lessons learned
```

### SEV3/SEV4 Response steps

```
1. Check IDeadLetterStore for pattern (TypeConversionError? SchemaViolation?)
2. Follow §2.5 or §2.3 investigation steps above
3. Re-run job after fix
4. If SLO breach: log SLO miss in SLO tracking system
```

---

## 5. Runbook: Capacity Planning for Large-File Workloads

### Key sizing parameters

| Parameter | Impact | Guidance |
|-----------|--------|----------|
| `CheckpointPolicy.BytesPerCheckpoint` | Memory per job | Keep < 50 MB for large files |
| `commitBatchSize` (rows per transaction) | Commit frequency vs throughput | 10 000–50 000 rows |
| Maximum concurrent jobs | CPU / IO contention | 1 job per CPU core as baseline |
| Dead-letter store size | Disk usage | Rotate entries older than 30 days |
| Schema registry size | Disk / DB | ~1 KB per schema version; prune after N versions per entity |

### Throughput estimation formula

```
Expected throughput (rows/sec) = 
    (Bytes per second of sequential disk read) 
    ÷ (Average bytes per row)
    × (CPU parse efficiency factor: 0.6–0.8 for CSV)

Example:
    Sequential disk: 500 MB/s
    Average row:     200 bytes
    Efficiency:      0.7
    → throughput ≈ 500_000_000 / 200 × 0.7 ≈ 1_750_000 rows/sec (single core, no masking)
    With masking (3 PII columns, 30% overhead): ≈ 1_225_000 rows/sec
```

### Vertical scaling triggers

| Metric exceeds threshold | Action |
|--------------------------|--------|
| File read latency p99 > 500 ms for local file | Check disk IOPS — consider NVMe |
| Memory > 2 GB per job | Reduce `BytesPerCheckpoint` or `commitBatchSize` |
| CPU > 90% on parse thread | Add worker threads (partition file by chunk) |
| Dead-letter store > 10 GB | Archive and purge resolved entries |

### Large-file specific settings

For files > 1 GB:
- Set `CheckpointPolicy.BytesPerCheckpoint = 25_000_000` (25 MB checkpoints).
- Set `commitBatchSize = 5_000` rows (smaller batches to reduce GC pressure).
- Enable streaming mode in `CSVDataSource` (Phase 4) — never load full file into memory.
- Set `IngestionQualityGate.MinRowsBeforeCheck = 10_000` (suppress early false-positive suspension).

---

## 6. Quick-Reference Card

| Situation | Tool / Method | Phase |
|-----------|--------------|-------|
| Register new file source | `ConfigEditor.AddDataConnection` + `AnalyzeCSVFile` | 1, 3 |
| Check if file already ingested | `ingester.IsAlreadyIngestedAsync(descriptor)` | 11 |
| Resume a suspended job | `ingester.ResumeAsync(jobId)` | 13 |
| Find dead-letter rows | `deadLetterStore.GetByJobAsync(jobId)` | 13 |
| Detect schema drift | `schemaRegistry.DetectDriftAsync(...)` | 12 |
| Check PII in a file | `classificationEngine.ClassifySchemaAsync(...)` | 14 |
| Set masking policy | `maskingStore.SetPolicy(patternKey, policy)` | 14 |
| Check job status | `stateStore.GetStatusAsync(jobId)` | 11 |
| Get health status | `healthCheck.CheckAsync()` | 16 |
| Force-stop all jobs | Transition each active job to `Suspended` | 13 |

---

## 7. Document Maintenance

This runbook must be reviewed and updated:
- After every SEV1 / SEV2 incident.
- After every major schema evolution that affected > 1 downstream consumer.
- Quarterly as part of operational readiness review.

Update history should be maintained in this file's git commit history.
