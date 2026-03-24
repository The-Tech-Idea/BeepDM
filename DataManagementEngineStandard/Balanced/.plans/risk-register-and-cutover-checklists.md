# BalancedDataSource Risk Register and Cutover Checklists

## Risk Register

| ID | Risk | Impact | Mitigation | Phase |
|---|---|---|---|---|
| BD1 | Incorrect routing policy overloads one backend | High | profile validation + simulation + capacity caps | 2, 8, 9 |
| BD2 | Retry on unsafe writes causes duplicate side effects | High | operation-aware retry and idempotency guards | 4 |
| BD3 | Circuit breaker churn causes instability | Medium/High | hysteresis and threshold tuning | 3 |
| BD3b | Failed datasource never re-enters service after outage | High | automated probe/rejoin state machine + pool rehydration | 3, 8 |
| BD4 | Stale cache returns inconsistent data | Medium/High | consistency mode + invalidation policy | 5 |
| BD5 | Sensitive route or query data leaked in logs | High | redaction and audit policy controls | 6 |
| BD6 | Alert noise masks critical outages | Medium | SLO-tuned alert policies | 7 |
| BD7 | Capacity collapse under burst traffic | High | throttling and overload behavior controls | 8 |
| BD8 | Unsafe profile reaches production | High | CI policy gates + approval workflow | 9 |
| BD9 | Rollout advances despite KPI degradation | High | wave gates + hard-stop criteria | 10 |

## Pre-Cutover Checklist
- [ ] Policy profile validated and approved.
- [ ] Route/failover simulation passed.
- [ ] Retry/idempotency policy reviewed for write operations.
- [ ] SLO/alert thresholds confirmed.
- [ ] Capacity profile and pool sizing verified.

## Cutover Checklist
- [ ] Deployment window confirmed.
- [ ] Correlation IDs and metrics enabled.
- [ ] Pilot traffic slice enabled first.
- [ ] KPI checks passed before widening traffic.

## Post-Cutover Checklist
- [ ] Baseline vs current KPI comparison complete.
- [ ] Failover and circuit behavior reviewed.
- [ ] Source recovery/rejoin path validated (open -> half-open -> closed).
- [ ] Security/audit checks passed.
- [ ] Follow-up tuning actions logged.
