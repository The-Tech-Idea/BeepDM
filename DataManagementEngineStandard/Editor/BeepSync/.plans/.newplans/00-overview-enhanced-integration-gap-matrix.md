# BeepSyncManager Enhanced Integration Gap Matrix

## Purpose
This document supersedes the baseline gap matrix in `../00-overview-beepsyncmanager-gap-matrix.md`
with three new integration dimensions added across all 10 phases:

| Integration Vector | Component | Key Entry Points |
|---|---|---|
| **Rule Engine** | `RuleEngine`, `RuleCatalog`, `IRule`, `RuleExecutionPolicy` | `SolveRule(...)`, `RegisterRule(...)`, `RuleEvaluated` |
| **Defaults Manager** | `DefaultsManager`, `IDefaultsManager`, `EntityDefaultsProfile` | `Apply(...)`, `SetColumnDefault(...)`, `ResolveDefaultValue(...)` |
| **Mapping Manager** | `MappingManager` (static facade + partials) | `AutoMapByConventionWithScoring(...)`, `MapObjectGraph(...)`, `SaveEntityMap(...)`, governance scope |

---

## Combined Integration Gap Matrix

| Area | Existing Gap | Rule Engine Integration | Defaults Manager Integration | Mapping Manager Integration | Priority |
|---|---|---|---|---|---|
| Sync Planning | No policy-first validation | Pre-plan validation rules; plan approval rules | Seed destination schema defaults on plan creation | Scored mapping quality gate before plan approval | P0 |
| Schema Governance | No versioning | N/A | Set default schema version/owner via `:USERNAME`/`:NOW` | Versioned mapping artifacts; approval lifecycle tied to sync plan | P0 |
| Incremental / CDC | No formal watermark contract | CDC filter rules (IRule) per schema; late-arrival rules | Defaults for audit watermark fields (`:NOW`, sequence seed) | Drift detection against saved watermark field maps | P0 |
| Conflict Resolution | Basic bidirectional | `SolveRule` to pick winner; custom resolver IRule | Defaults applied to winning record (audit trail fields) | Mapping precedence rules for field-level conflict arbitration | P0 |
| Reliability | No idempotency contract | Retry categorization via rule (transient/non-retry/policy-driven) | Default retry metadata stamped per checkpoint | Compiled mapping plan cached across checkpoint resumes | P1 |
| Data Quality | Validation helper only | Per-field/per-entity DQ gate rules in `RuleCatalog` | Missing-field defaults filled before DQ check | Mapping quality score threshold enforced pre-sync | P1 |
| Observability | Status string only | Rules-based alert trigger evaluation; event subscription via `RuleEvaluated` | Defaults for alert metadata (`:NOW`, `:USERNAME`, severity literals) | Mapping plan version in metrics/trace context | P1 |
| Performance | Import delegation only | Rule evaluation caching; compiled policy profiles | Cached `EntityDefaultsProfile` per schema pair | Compiled mapping plans (`MappingManager.PerformanceCaching.cs`) reused across batches | P2 |
| DevEx / CI/CD | No schema validation pipeline | Rule catalog linting in CI; rule key uniqueness checks | Default profile export/import for environment promotion | Mapping governance diff in PR pipeline; threshold enforcement in CI | P2 |
| Rollout Governance | No KPI gates | Rule-based KPI gate evaluation; promote/demote rule states | N/A | Mapping approval state must be `Approved` before rollout | P1 |

---

## Architecture Constraints (Do Not Break)

1. **Delegation boundary**: data movement remains in `DataImportManager`; rules/defaults/mapping are applied at sync-layer boundary — never inside the import execution kernel.
2. **Rule execution policy**: always provide an explicit `RuleExecutionPolicy` (depth, token limits, lifecycle minimum). Never use default permissive policy in production paths.
3. **Defaults apply-order**: `DefaultsManager.Apply(...)` is called *after* field mapping, *before* destination write — ensuring mapped values win over defaults.
4. **Mapping governance scope**: wrap mapping saves in `MappingManager.BeginGovernanceScope(...)` to preserve author/reason/state traceability linked to the sync plan id.
5. **Fail-safe defaults**: if `DefaultsManager` or `RuleEngine` is unavailable (null `IDMEEditor`), sync falls through to the existing behaviour — integration is additive, not mandatory.

---

## New Integration Types / DTOs to Add Across Phases

| DTO / Interface | Home File | Description |
|---|---|---|
| `SyncRulePolicy` | `Interfaces/ISyncHelpers.cs` | Per-schema rule policy: catalog name, execution policy, rule keys by stage |
| `SyncDefaultsPolicy` | `Interfaces/ISyncHelpers.cs` | Per-schema defaults policy: `EntityDefaultsProfile` reference, apply stages |
| `SyncMappingPolicy` | `Interfaces/ISyncHelpers.cs` | Per-schema mapping policy: quality threshold, governance state requirement, drift action |
| `SyncIntegrationContext` | `Models/` (new) | Runtime context carrying resolved rule engine, defaults manager, and mapping plan references |
| `SyncPreflightReport` | `Models/` (new) | Composite preflight output: rule validation results, defaults profile status, mapping quality score |

---

## Enhanced Phase Summary

| Phase | Primary Transport Integration | Rule Engine Role | Defaults Role | Mapping Role |
|---|---|---|---|---|
| 1 – Contracts | Plan foundation | Validate plan fields via rules | Seed plan metadata defaults | Mapping quality gate on plan creation |
| 2 – Schema Governance | Schema versioning | N/A | Stamp author/timestamp via expressions | Versioned mapping artifact; approval state |
| 3 – CDC/Incremental | Watermark | CDC filter rule; late-arrival rule | Watermark field default seed | Watermark field map drift detection |
| 4 – Conflict | Bidirectional | `SolveRule` conflict resolver | Audit fields on winning record | Field-level mapping precedence rules |
| 5 – Reliability | Retry/checkpoint | Retry category rule | Retry metadata defaults | Cached compiled mapping plan |
| 6 – Data Quality | DQ controls | DQ gate rules per field/entity | Fill missing fields before DQ | Mapping score threshold before sync |
| 7 – Observability | Metrics/SLOs | Alert trigger rules; `RuleEvaluated` telemetry | Alert metadata defaults | Mapping plan version in trace |
| 8 – Performance | Throughput | Evaluator caching; lightweight policy | Cached `EntityDefaultsProfile` | Compiled accessor plans; batch reuse |
| 9 – DevEx / CI | CI pipeline | Catalog linting; rule key checks | Profile export for env promotion | Mapping governance diff; CI gate |
| 10 – Rollout | KPI gates | KPI gate rule evaluation | N/A | Mapping approval pre-rollout check |
