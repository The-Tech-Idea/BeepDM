# BeepSync Enhanced Plans — README

This folder contains the **enhanced phase plans** for `BeepSyncManager`, extending the original
`.plans/` documents with concrete integration guidance for three Beep subsystems:

- **Rule Engine** — `RuleEngine.SolveRule`, `RuleCatalog`, `RuleExecutionPolicy`
- **Defaults Manager** — `DefaultsManager.Apply`, `EntityDefaultsProfile`, dynamic expressions
- **Mapping Manager** — `MappingManager` static facade, governance, compiled plans, drift detection

All plans are additive and backward-compatible. Existing sync behaviour works unchanged when
none of the three integration vectors are configured.

---

## Index

| File | Phase | Summary |
|---|---|---|
| [00-overview-enhanced-integration-gap-matrix.md](./00-overview-enhanced-integration-gap-matrix.md) | Overview | Master gap matrix — maps each phase to which integration vectors apply and how |
| [01-phase1-contracts-and-sync-plan-foundation.md](./01-phase1-contracts-and-sync-plan-foundation.md) | 1 | Core contracts, preflight validation, plan bootstrap with Rule Engine approval gate, Defaults Manager stamping, Mapping quality check |
| [02-phase2-schema-governance-and-versioning.md](./02-phase2-schema-governance-and-versioning.md) | 2 | Schema versioning + co-promotion of Mapping approval state; Defaults stamps SavedAt/SavedBy; `SyncSchemaVersion` model |
| [03-phase3-incremental-sync-and-cdc-strategy.md](./03-phase3-incremental-sync-and-cdc-strategy.md) | 3 | CDC / watermark strategy; rule-driven filter and late-arrival / tombstone handling; `:MIN_DATETIME` custom expression; drift detection on watermark field |
| [04-phase4-bidirectional-conflict-resolution.md](./04-phase4-bidirectional-conflict-resolution.md) | 4 | Conflict resolution via `RuleEngine.SolveRule`; field-level MappingManager overrides; Defaults stamps winning record; `ConflictEvidence` model |
| [05-phase5-reliability-retry-and-idempotency.md](./05-phase5-reliability-retry-and-idempotency.md) | 5 | Retry classification rules; checkpoint model stores compiled mapping plan ID; DefaultsManager stamps CheckpointCreatedAt; governance version on resume |
| [06-phase6-data-quality-and-reconciliation.md](./06-phase6-data-quality-and-reconciliation.md) | 6 | DQ gate order (map → defaults → rules → write); mapping quality as hard gate; rejection defaults; `SyncReconciliationReport` extension |
| [07-phase7-observability-slo-and-alerting.md](./07-phase7-observability-slo-and-alerting.md) | 7 | SLO profiles; alert rules; `RuleEvaluated` telemetry subscription; `SyncAlertRecord` stamped by DefaultsManager; `SyncMetrics` additions |
| [08-phase8-performance-and-scale-strategy.md](./08-phase8-performance-and-scale-strategy.md) | 8 | Compiled plan reuse per batch/task; `EntityDefaultsProfile` TTL cache; FastPath vs. DefaultSafe rule policy; `SyncPerformanceProfile` model |
| [09-phase9-devex-and-cicd-automation.md](./09-phase9-devex-and-cicd-automation.md) | 9 | CI pipeline: rule catalog lint, mapping diff as PR artifact, defaults profile export/import, `SyncCiGateResult` model |
| [10-phase10-rollout-governance-and-kpi-gates.md](./10-phase10-rollout-governance-and-kpi-gates.md) | 10 | Rollout KPI gate rules; mapping approval state enforcement per phase; defaults profile coverage audit; `RolloutReadinessReport` model |

---

## Integration Vectors at a Glance

| Phase | Rule Engine | Defaults Manager | Mapping Manager |
|---|:---:|:---:|:---:|
| 1 — Contracts & Foundation | ✅ `sync.plan.validate`, `sync.plan.approval-gate` | ✅ Stamp plan CreatedAt/CreatedBy/SchemaVersion | ✅ Quality preflight gate, `MinQualityScore` |
| 2 — Schema Governance | ✅ `sync.schema.promotion-gate` | ✅ Stamp SavedAt/SavedBy | ✅ Co-promote ApprovalState |
| 3 — Incremental / CDC | ✅ `sync.cdc.filter`, `sync.cdc.late-arrival`, `sync.cdc.tombstone` | ✅ `:MIN_DATETIME` watermark seed | ✅ Drift detection on watermark field |
| 4 — Conflict Resolution | ✅ `sync.conflict.*` | ✅ Stamp winning record | ✅ Field-level override rules |
| 5 — Reliability / Retry | ✅ `sync.retry.*`, `sync.checkpoint.resume-safe` | ✅ Stamp CheckpointCreatedAt | ✅ Compiled plan ID in checkpoint |
| 6 — Data Quality | ✅ `sync.dq.*`, `sync.dq.batch-threshold` | ✅ Fill missing; stamp RejectedAt/Reason | ✅ Quality score as hard gate |
| 7 — Observability / SLO | ✅ Alert trigger rules, `sync.slo.classify-run` | ✅ Stamp EmittedAt/EmittedBy on alerts | ✅ MappingPlanVersion in SyncMetrics |
| 8 — Performance & Scale | ✅ FastPath/DefaultSafe profiles | ✅ `EntityDefaultsProfile` TTL cache | ✅ Compiled plan reuse per batch |
| 9 — DevEx / CI-CD | ✅ Catalog lint, key existence, lifecycle check | ✅ Export/import for env promotion | ✅ Diff as PR artifact, approval gate |
| 10 — Rollout Governance | ✅ KPI gate, auto-rollback | ✅ Profile coverage audit, resolver check | ✅ Approval state per phase, version alignment |

---

## Architecture Constraints (Summary)

1. **Apply order is fixed**: MappingManager field map → DefaultsManager fill → Rule Engine DQ/business → destination write.
2. **Data movement stays in `DataImportManager`**: all integration hooks live at the BeepSync orchestration boundary.
3. **All integrations are opt-in and null-safe**: no integration context = original behaviour unchanged.
4. **Governance scope wrapping**: all mapping saves during a sync run are wrapped in `MappingManager.BeginGovernanceScope(syncPlanId)`.
5. **Compiled plan is immutable within a run**: reuse the same compiled plan for all batches; invalidate only on `MappingManager.MappingUpdated` event.
6. **Rule keys are contracts**: registered in `RuleCatalog` before any sync schema references them; linted in CI.

---

## New Models Introduced Across Phases

| Model | Phase | Purpose |
|---|---|---|
| `SyncIntegrationContext` | 1 | Container for optional Rule Engine, Defaults Manager, Mapping Manager instances + policies |
| `SyncPreflightReport` / `SyncPreflightIssue` | 1 | Structured preflight check results |
| `SyncRulePolicy` | 1 | Rule engine wiring config: enabled flag, catalog version, execution policy |
| `SyncDefaultsPolicy` | 1 | Defaults wiring config: enabled, apply-before-write, audit field names |
| `SyncMappingPolicy` | 1 | Mapping wiring config: governance scope, min quality score, required approval state |
| `SyncPlanMetadata` | 1 | Plan identity, team, environment, review chain |
| `SyncSchemaVersion` | 2 | Schema version with SHA256, author, MappingVersion, RuleCatalogVersion |
| `WatermarkPolicy` / `CdcFilterContext` | 3 | CDC incremental mode config + active filter result |
| `ConflictPolicy` / `ConflictEvidence` | 4 | Conflict resolution policy + per-record evidence |
| `SyncCheckpoint` / `RetryPolicy` | 5 | Resume-safe checkpoint + retry configuration |
| `DqGateResult` / `SyncReconciliationReport` (enhanced) | 6 | Per-rule DQ gate result + reconciliation report |
| `SyncAlertRecord` / `SloProfile` | 7 | Alert emission record + SLO threshold set |
| `SyncPerformanceProfile` | 8 | Batch size, parallelism, rule mode, cache TTL config |
| `SyncCiGateResult` / `CiGateItem` | 9 | CI pipeline gate output |
| `RolloutReadinessReport` / `RolloutGateLine` / `SyncRolloutPhase` | 10 | Rollout promotion gate result |

---

## Superseded Plans (Original `.plans/` folder)

The files in this folder supersede the corresponding originals in `../.plans/`:

| New (`.newplans/`) | Supersedes |
|---|---|
| `00-overview-enhanced-integration-gap-matrix.md` | `../00-overview-and-gap-analysis.md` |
| `01-phase1-contracts-and-sync-plan-foundation.md` | `../01-phase1-contracts-and-sync-plan-foundation.md` |
| `02-phase2-schema-governance-and-versioning.md` | `../02-phase2-schema-governance-and-versioning.md` |
| `03-phase3-incremental-sync-and-cdc-strategy.md` | `../03-phase3-incremental-sync-and-cdc-strategy.md` |
| `04-phase4-bidirectional-conflict-resolution.md` | `../04-phase4-bidirectional-conflict-resolution.md` |
| `05-phase5-reliability-retry-and-idempotency.md` | `../05-phase5-reliability-retry-and-idempotency.md` |
| `06-phase6-data-quality-and-reconciliation.md` | `../06-phase6-data-quality-and-reconciliation.md` |
| `07-phase7-observability-slo-and-alerting.md` | `../07-phase7-observability-slo-and-alerting.md` |
| `08-phase8-performance-and-scale-strategy.md` | `../08-phase8-performance-and-scale-strategy.md` |
| `09-phase9-devex-and-cicd-automation.md` | `../09-phase9-devex-and-cicd-automation.md` |
| `10-phase10-rollout-governance-and-kpi-gates.md` | `../10-phase10-rollout-governance-and-kpi-gates.md` |
