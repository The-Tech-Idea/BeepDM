# ETL Risk Register and Cutover Checklists

## Purpose
Provide rollout-risk governance and execution checklists for safe migration to enterprise ETL standards.

## Risk Register

| Risk ID | Risk | Impact | Likelihood | Detection Signal | Mitigation | Owner Phase |
|---|---|---|---|---|---|---|
| R1 | Runtime policy enforcement changes break legacy pipelines | High | Medium | Post-upgrade failure spike | Compatibility mode, feature flags, phased activation | 2, 9 |
| R2 | Rollback behavior inconsistent across sinks | High | Medium | Failed runs leave partial writes | Enforce sink rollback contract, add rollback tests | 2 |
| R3 | DQ and lineage overhead degrades throughput | Medium | Medium | Latency and CPU increase after DQ rollout | Sampling tiers, configurable verbosity, retention controls | 3, 6 |
| R4 | Scheduler dependency chains cause cascade failures | High | Medium | Queue saturation, blocked dependents | Circuit breakers, deadlock detection, dependency timeout policies | 5 |
| R5 | Alert noise causes operational fatigue | Medium | High | High false-positive ratio, ignored alerts | Alert tuning, suppression windows, severity policy | 4 |
| R6 | Sensitive data leaks via logs/alerts | High | Low/Med | PII indicators in telemetry payloads | Redaction rules, secret scanning, secure logging policy | 7 |
| R7 | Performance optimization changes correctness | High | Low/Med | Data mismatches in benchmark datasets | Correctness-first validation and reference dataset tests | 6 |
| R8 | CI gates bypassed under delivery pressure | Medium | Medium | Unreviewed or untested prod changes | Protected branches and exception approval workflow | 8 |
| R9 | Migration wave advances before KPI stability | High | Medium | KPI variance above threshold | Hard stop criteria and governance review checkpoints | 9 |

## Pre-Cutover Checklist
- Governance
  - Pipeline owner, steward, and approver assigned.
  - Standards traceability row and phase ID linked in rollout ticket.
- Runtime
  - Target pipeline has policy compatibility review completed.
  - Retry/timeout/error-threshold settings validated in staging.
  - Rollback path validated for primary sink.
- Data Quality and Lineage
  - DQ baseline score captured for pre-cutover period.
  - Lineage query for critical fields validated.
- Observability and Alerts
  - SLO profile assigned.
  - Alert routes and on-call runbook links verified.
- Security and Compliance
  - Data classification attached.
  - Secret and connection policy checks passed.
- Release
  - CI quality gates passed.
  - Rollback execution dry-run performed.

## Cutover Execution Checklist
- Freeze window confirmed and stakeholders notified.
- Backout decision owner and timebox declared.
- Canary rollout:
  - Enable for pilot pipeline(s) only.
  - Monitor success rate, latency, DQ rejects, and alert signals.
- Promote in planned wave sequence:
  - Wave 1 non-critical.
  - Wave 2 standard.
  - Wave 3 critical.
- Confirm schedule/dependency behavior after each wave.

## Post-Cutover Checklist
- 24h, 72h, and 7-day health checks completed.
- KPI variance against baseline recorded.
- Incident and exception log reviewed.
- Rollout retrospective captured:
  - What changed
  - What failed
  - What to improve before next wave

## Hard Stop and Rollback Triggers
- Success rate drops below agreed threshold for critical tier.
- Freshness SLO breach sustained across two monitoring windows.
- DQ reject ratio exceeds threshold for business-critical entities.
- Security/compliance control failure detected.
- Dependency deadlock or queue starvation not resolved within SLA.

## KPI Gate Template (Per Wave)
- Reliability
  - Run success rate
  - Retry inflation ratio
  - Timeout incidence
- Quality
  - Reject ratio
  - Warn ratio
  - Lineage completeness
- Performance and Cost
  - p95 run latency
  - Throughput per minute
  - Cost-per-run proxy metric
- Operations
  - MTTD
  - MTTR
  - Alert false-positive ratio
