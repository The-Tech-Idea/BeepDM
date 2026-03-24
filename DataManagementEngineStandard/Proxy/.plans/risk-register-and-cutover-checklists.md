# ProxyDataSource Risk Register and Cutover Checklists

## Risk Register

| ID | Risk | Impact | Mitigation | Phase |
|---|---|---|---|---|
| PX1 | Duplicate operation execution from retry wrappers causes data corruption | Critical | single-invocation wrapper refactor + idempotency tests | 4, 9, 10 |
| PX2 | Misconfigured resilience profile causes outage amplification | High | policy validation + staged rollout | 1, 2, 10 |
| PX3 | Routing bias overloads a single backend | High | routing policy tests + capacity caps | 3, 8 |
| PX4 | Health map concurrency race causes unstable routing decisions | High | concurrent-safe health state + race tests | 3, 8 |
| PX5 | Cache staleness causes incorrect reads | Medium/High | consistency controls + invalidation policy | 5 |
| PX6 | Alert fatigue from noisy proxy events | Medium | SLO-focused alert tuning | 6 |
| PX7 | Sensitive data leakage in logs | High | redaction and audit policy | 7 |
| PX8 | Capacity collapse under burst traffic | High | throttling and pool tuning | 8 |
| PX9 | Unsafe policy change reaches production | High | CI policy gates | 9 |
| PX10 | KPI degradation ignored during rollout | High | hard-stop and wave gates | 10 |

## Pre-Cutover Checklist
- [ ] Duplicate-invocation regression suite green for write operations.
- [ ] Proxy policy version reviewed and approved.
- [ ] Profile simulation and failover test passed.
- [ ] Capacity profile selected for target environment.
- [ ] Alert/SLO thresholds reviewed.
- [ ] Security redaction checks validated.

## Cutover Checklist
- [ ] Deployment window confirmed.
- [ ] Correlation id/metrics enabled.
- [ ] Start with pilot traffic slice.
- [ ] Verify KPI health before increasing traffic.

## Post-Cutover Checklist
- [ ] Compare failover/error/latency vs baseline.
- [ ] Review circuit behavior and saturation signals.
- [ ] Verify no duplicate side-effect incidents.
- [ ] Confirm no security/audit regressions.
- [ ] Record lessons learned and next tuning actions.
