# FileManager — Enterprise File Data Source

`DataManagementEngineStandard/FileManager` provides file-based data ingestion and querying through the Beep `IDataSource` abstraction.  
It supports CSV today, with a plugin model (Phase 8) for TSV, fixed-width, JSONL, and other formats.

---

## Quick Start

```csharp
// 1. Register the file connection
editor.ConfigEditor.AddDataConnection(new ConnectionProperties
{
    ConnectionName = "customers",
    FileName       = "customers.csv",
    FilePath       = "C:\\data\\",
    DatabaseType   = DataSourceType.CSV,
    Category       = DatasourceCategory.FILE,
    Delimiter      = ','
});

// 2. Get and open the datasource
var ds = (CSVDataSource)editor.GetDataSource("customers");
ds.Openconnection();

// 3. Read all rows
var table = (DataTable)ds.GetEntity("customers", new List<AppFilter>());

// 4. Query with filters
var filters = new List<AppFilter>
{
    new AppFilter { FieldName = "country", Operator = "=", FilterValue = "DE" }
};
var germanCustomers = (DataTable)ds.GetEntity("customers", filters);
```

---

## Source Files

| File | Purpose |
|------|---------|
| `CSVDataSource.cs` | Main `IDataSource` implementation — connection, schema, query, CRUD |
| `CSVAnalyser.cs` | Schema inference, delimiter detection, content sampling |
| `CSVTypeMapper.cs` | Column-value type inference and conversion |
| `ICSVDataReader.cs` | Streaming reader abstraction (forward-only, low-memory) |
| `TextFieldParser.cs` | RFC-4180 compliant CSV tokeniser with quote handling |

---

## Enterprise Architecture

The FileManager is structured into two tracks of improvements — see [`.plans/README.md`](.plans/README.md) for the full plan index.

### Engineering Track (Phases 1–10)

Addresses correctness bugs and feature gaps in the existing code:

| Phase | What it fixes |
|-------|--------------|
| 1 | Reader contracts — ICSVDataReader invariants, `Dispose` safety |
| 2 | CSV parsing correctness — RFC 4180, quoted-delimiter handling, multiline fields |
| 3 | Schema inference — confidence-scored types, robust uniqueness heuristics |
| 4 | Streaming — large-file reads without loading everything into memory |
| 5 | Error handling — structured row/column diagnostics |
| 6 | Performance — filter pushdown, optional column indexing |
| 7 | Security — path traversal guards, static audit profiles |
| 8 | Format expansion — TSV, fixed-width, JSONL plugin adapters |
| 9 | ETL integration — normalized row envelope, Rules Engine hooks |
| 10 | Rollout — canary controls, KPI gates |

### Enterprise Track (Phases 11–17)

Adds architecture-level capabilities for production, regulated, and SaaS environments:

#### Phase 11 — Idempotent Ingestion Contracts

Every file ingestion job is tracked by `JobId` and `FileChecksum`.  
Re-submitting the same file is a safe no-op.

```csharp
// Check if already ingested
var priorJobId = await ingester.IsAlreadyIngestedAsync(descriptor);
if (priorJobId != null)
    return; // already done

// Run idempotent ingestion
var status = await ingester.IngestAsync(descriptor, progressCallback);
// status.State == IngestionState.Complete
```

**Ingestion states:** `Pending → Validating → Ingesting → Complete / Failed / Quarantined / Suspended`

#### Phase 12 — Schema Registry and Catalog

Every CSV entity's schema is versioned.  Schema drift (new columns, type changes) is detected before any rows are written.

```csharp
// Register schema after analysis
await schemaRegistry.RegisterAsync("CRM", "contacts", schemaVersion);

// Detect drift before ingesting a new file
var driftReport = await schemaRegistry.DetectDriftAsync("CRM", "contacts", candidateSchema);
if (driftReport.IsBreaking)
    throw new SchemaDriftException(driftReport.Summary);
```

#### Phase 13 — Resilience and Continuity

Large-file ingestion saves byte-offset checkpoints every 50 MB.  
Failed or cancelled jobs resume from where they stopped.  
Bad rows go to a dead-letter store — they never abort the rest of the job.

```csharp
// Resume a suspended job
var status = await ingester.ResumeAsync(jobId);

// Inspect dead-letter rows
var deadRows = await deadLetterStore.GetByJobAsync(jobId);
foreach (var entry in deadRows)
    Console.WriteLine($"Row {entry.SourceRowIndex}: {entry.ErrorMessage}");
```

Quality gate: if > 5% of rows are rejected, the job is suspended automatically.

#### Phase 14 — Data Classification and DLP

PII is auto-detected in column samples.  Masking runs per-value as the file is read — raw data never reaches the target store.

```csharp
// Classify columns automatically
var result = await classificationEngine.ClassifySchemaAsync(schema, columnSamples);
Console.WriteLine($"High-sensitivity: {string.Join(", ", result.HighSensitivityColumns)}");

// Configure masking
maskingStore.SetPolicy("PII-Email",      new ColumnMaskingPolicy { Strategy = MaskingStrategy.PartialMask });
maskingStore.SetPolicy("PII-SSN",        new ColumnMaskingPolicy { Strategy = MaskingStrategy.Redact });
maskingStore.SetPolicy("PII-CreditCard", new ColumnMaskingPolicy { Strategy = MaskingStrategy.PartialMask });
```

Built-in detectors: Email, Phone, SSN, Credit Card, IBAN, Name, Date-of-Birth, National ID, Health condition, Financial account number.

#### Phase 15 — Multi-Tenancy and Governance

Each tenant's files live in an isolated directory.  
Path traversal attacks are blocked at the resolver level.  
Row-level security filters tenant-key columns automatically.  
GDPR data residency is enforced per region.

```csharp
// Tenant-scoped path: /files/{TenantId}/{DataRegion}/{Entity}.csv
pathResolver.ValidatePathBoundary(resolvedPath, tenantContext); // throws on violation

// Row-level security — filters "organization_id = 'ACME'" into every query
rlsFilter.Configure("orders", new RlsRule { TenantKeyColumn = "organization_id" });

// RBAC — enforce operation permissions
accessPolicy.Enforce("orders", FileOperation.WriteData, tenantContext); // throws if denied
```

Roles: `FileReader`, `FileWriter`, `FileSchemaAdmin`, `FileAdmin`, `Auditor`.

#### Phase 16 — Observability and SLOs

Every ingestion job emits OpenTelemetry spans and metrics.  SLO breaches trigger automatic job suspension and alerting.

```csharp
// OTel: beep.filemanager.rows_processed, beep.filemanager.ingestion_duration, ...

// Health check for load-balancers / K8s probes
var health = await healthCheck.CheckAsync();
// health.Status: Healthy | Degraded | Unhealthy
```

**SLOs:**
- Ingestion latency p95 < 30s per 100K rows
- Row rejection rate < 1% per job
- Job completion rate >= 99% in 24h window
- Dead-letter backlog < 10 000 entries

#### Phase 17 — Operational Runbook

Step-by-step playbooks for:
- Onboarding a new file source (9-step checklist)
- Investigating ingestion failures (decision tree)
- Schema evolution (compatibility matrix + steps)
- SEV1/SEV2 incident response (timeline with T+0, T+5min, T+1h, T+24h)
- Capacity planning formula and large-file settings

See [`.plans/17-phase17-enterprise-operational-runbook.md`](.plans/17-phase17-enterprise-operational-runbook.md).

---

## Folder Layout (after implementation)

```
FileManager/
├── CSVDataSource.cs
├── CSVAnalyser.cs
├── CSVTypeMapper.cs
├── ICSVDataReader.cs
├── TextFieldParser.cs
│
├── Contracts/              # Phase 11 — ingestion contracts
│   ├── IFileIngestionDescriptor.cs
│   ├── IIngestionStateStore.cs
│   ├── IFileProvenanceEnvelope.cs
│   └── IIdempotentFileIngester.cs
│
├── Schema/                 # Phase 12 — schema registry
│   ├── IFileSchemaRegistry.cs
│   ├── FileSchemaVersion.cs
│   ├── SchemaDriftReport.cs
│   ├── IColumnLineageStore.cs
│   └── ICatalogExportAdapter.cs
│
├── Resilience/             # Phase 13 — resilience
│   ├── CheckpointPolicy.cs
│   ├── IDeadLetterStore.cs
│   ├── FileRetryPolicy.cs
│   ├── IFileArrivalTrigger.cs
│   └── IngestionQualityGate.cs
│
├── Classification/         # Phase 14 — DLP
│   ├── IDataClassificationEngine.cs
│   ├── IDataMaskingEngine.cs
│   ├── ColumnMaskingPolicy.cs
│   └── IMaskingPolicyStore.cs
│
├── Governance/             # Phase 15 — multi-tenancy
│   ├── ITenantContext.cs
│   ├── ITenantFilePathResolver.cs
│   ├── IFileAccessPolicy.cs
│   ├── IRowLevelSecurityFilter.cs
│   └── IDataResidencyPolicy.cs
│
├── Observability/          # Phase 16 — telemetry & SLOs
│   ├── IFileIngestionTelemetry.cs
│   ├── ISloEnforcer.cs
│   ├── IFileManagerHealthCheck.cs
│   └── IFileIngestionAlerting.cs
│
└── .plans/                 # Enhancement plan documents
    ├── README.md
    ├── 00-overview-filemanager-gap-matrix.md
    ├── 01 … 10 (engineering phases)
    ├── 11-phase11-enterprise-file-ingestion-contracts.md
    ├── 12-phase12-schema-registry-and-catalog-integration.md
    ├── 13-phase13-resilience-and-continuity.md
    ├── 14-phase14-data-classification-and-dlp.md
    ├── 15-phase15-multi-tenancy-and-governance.md
    ├── 16-phase16-observability-and-slos.md
    └── 17-phase17-enterprise-operational-runbook.md
```

---

## Compliance Coverage

| Regulation / Standard | Addressed by |
|-----------------------|-------------|
| GDPR Art. 5, 9, 25, 44 | Phases 14, 15 |
| PCI-DSS Req. 3.4 (PAN masking) | Phase 14 |
| HIPAA §164.514(b) | Phase 14 |
| SOX (audit completeness) | Phases 11, 13, 16 |
| ISO 8000-8 (data provenance) | Phase 11 |
| ISO/IEC 27001 A.9 (access control) | Phase 15 |
| OWASP A01:2021 (path traversal) | Phase 15 |
| OpenTelemetry semantic conventions | Phase 16 |
| SRE SLO practices | Phase 16 |

