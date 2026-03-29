# FileManager Enhancement Plans

Phased enhancement program for `DataManagementEngineStandard/FileManager`, revised after end-to-end code audit.

**Two tracks:**
- **Engineering (Phases 0–10):** Bug fixes, correctness, streaming, format expansion.
- **Enterprise (Phases 11–17):** Idempotent ingestion, schema registry, resilience, DLP, governance, observability, operations.

## Source Files Audited
- `CSVAnalyser.cs`
- `CSVDataSource.cs`
- `CSVTypeMapper.cs`
- `ICSVDataReader.cs`
- `TextFieldParser.cs`

---

## Engineering Track (Phases 0–10)

| # | Document | Focus | Status |
|---|---|---|---|
| 0 | [00-overview-filemanager-gap-matrix.md](./00-overview-filemanager-gap-matrix.md) | Gap matrix, 10 capability gaps | in-progress |
| 1 | [01-phase1-contracts-and-reader-abstractions.md](./01-phase1-contracts-and-reader-abstractions.md) | Reader contracts, invariants | planned |
| 1A | [01a-phase1a-filedatasource-reader-registry-interface.md](./01a-phase1a-filedatasource-reader-registry-interface.md) | FileDataSource interface for list/switch readers via registry/factory | planned |
| 2 | [02-phase2-csv-parsing-correctness-and-edge-cases.md](./02-phase2-csv-parsing-correctness-and-edge-cases.md) | Parsing correctness, RFC 4180 | planned |
| 3 | [03-phase3-schema-inference-and-type-mapping.md](./03-phase3-schema-inference-and-type-mapping.md) | Confidence-scored schema inference | planned |
| 4 | [04-phase4-streaming-large-files-and-memory-controls.md](./04-phase4-streaming-large-files-and-memory-controls.md) | Streaming reads, memory bounds | planned |
| 5 | [05-phase5-quality-validation-and-error-handling.md](./05-phase5-quality-validation-and-error-handling.md) | Structured diagnostics, row/col errors | planned |
| 6 | [06-phase6-performance-indexing-and-pushdown.md](./06-phase6-performance-indexing-and-pushdown.md) | Filter pushdown, indexing | planned |
| 7 | [07-phase7-security-governance-and-data-masking.md](./07-phase7-security-governance-and-data-masking.md) | Path policy, audit profiles (static) | planned |
| 8 | [08-phase8-format-expansion-and-plugin-model.md](./08-phase8-format-expansion-and-plugin-model.md) | TSV / fixedwidth / JSONL adapters | planned |
| 9 | [09-phase9-integration-with-etl-mapping-rules.md](./09-phase9-integration-with-etl-mapping-rules.md) | ETL mapping, Rules Engine hooks | planned |
| 10 | [10-phase10-rollout-observability-and-kpis.md](./10-phase10-rollout-observability-and-kpis.md) | Canary controls, KPI gates | planned |
| — | [implementation-hotspots-change-plan.md](./implementation-hotspots-change-plan.md) | 8 exact code hotspots to fix | planned |
| — | [standards-traceability-matrix.md](./standards-traceability-matrix.md) | Standards → code traceability | planned |
| — | [risk-register-and-cutover-checklists.md](./risk-register-and-cutover-checklists.md) | Risk register, cutover checklists | planned |

---

## Enterprise Track (Phases 11–17)

| # | Document | Focus | Status |
|---|---|---|---|
| 11 | [11-phase11-enterprise-file-ingestion-contracts.md](./11-phase11-enterprise-file-ingestion-contracts.md) | Idempotency, state machine, provenance | planned |
| 12 | [12-phase12-schema-registry-and-catalog-integration.md](./12-phase12-schema-registry-and-catalog-integration.md) | Versioned schema registry, drift detection, lineage | planned |
| 13 | [13-phase13-resilience-and-continuity.md](./13-phase13-resilience-and-continuity.md) | Checkpoint/resume, dead-letter, retry, file triggers | planned |
| 14 | [14-phase14-data-classification-and-dlp.md](./14-phase14-data-classification-and-dlp.md) | PII detection, masking strategies, audit log | planned |
| 15 | [15-phase15-multi-tenancy-and-governance.md](./15-phase15-multi-tenancy-and-governance.md) | Tenant paths, RBAC, RLS, data residency | planned |
| 16 | [16-phase16-observability-and-slos.md](./16-phase16-observability-and-slos.md) | OTel telemetry, SLO definitions, health checks | planned |
| 17 | [17-phase17-enterprise-operational-runbook.md](./17-phase17-enterprise-operational-runbook.md) | Onboarding, incident response, schema evolution, capacity | planned |

---

## Phase Dependencies

```
Engineering (1→2→3→4→5→6→7→8→9→10)
                │           │
                ▼           ▼
         Enterprise   Enterprise
         Phase 11     Phase 12
              │           │
              └─────┬─────┘
                    ▼
               Phase 13 (resilience)
                    │
         ┌──────────┼──────────┐
         ▼          ▼          ▼
     Phase 14   Phase 15   Phase 16
     (DLP)    (governance) (observability)
                    └──────────┘
                         │
                    Phase 17
                   (runbook)
```

---

## Primary Enterprise Outcomes

| Capability | Delivered by |
|------------|-------------|
| Idempotent re-runnable ingestion | Phase 11 |
| Schema versioning and drift alerting | Phase 12 |
| Resumable large-file ingestion | Phase 13 |
| Dead-letter isolation — no silent data loss | Phase 13 |
| Auto-PII detection and masking | Phase 14 |
| Multi-tenant file isolation | Phase 15 |
| RBAC and row-level security | Phase 15 |
| GDPR/data residency controls | Phase 15 |
| SLO-enforced ingestion pipelines | Phase 16 |
| OpenTelemetry tracing and metrics | Phase 16 |
| Operational runbooks | Phase 17 |

---

## Audited Hotspot Files (Engineering Track)

- `CSVDataSource.GetFieldsbyTableScan(...)` — wrong loop counter vars
- `CSVDataSource.GetEntity(...)` — `FirstOrDefault().Key` defaults to 0 on miss
- `CSVDataSource.GetEntityStructure(...)` / `GetEntityType(...)` — crash pattern on `Entities.Count == 0`
- `CSVDataSource` CRUD paths (`BulkInsert`, `InsertEntity`, `UpdateEntity`, `DeleteEntity`) — full-file reads
- `CSVAnalyser.AnalyzeCSVFile(...)` — unguarded uniqueness denominator
- `CSVTypeMapper.ConvertValue(...)` — enum parse throws, culture-implicit
- `CSVDataReader.Read()` / `GetValue()` / `GetOrdinal()` — projection map divergence
- `CsvTextFieldParser.ParseFieldAfterOpeningQuote(...)` — naive header split on quoted delimiters
