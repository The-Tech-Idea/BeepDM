# Caching Risk Register and Cutover Checklists

## Risk Register

| Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|
| `default(T)` treated as miss causes correctness bugs | High | High | explicit hit/miss result model + regression suite |
| Non-atomic lock helpers allow race conditions | High | High | atomic provider operations + contention tests |
| Stats/memory counter drift under concurrency | Medium/High | High | synchronized accounting and reconciliation checks |
| Hybrid parallel operations produce racey success status | Medium/High | Medium | race-safe aggregation and deterministic consistency policy |
| Redis provider placeholder behavior used in production | High | Medium | hard rollout gate + explicit non-prod guard |
| Datasource duplication introduces divergence regressions | Medium | High | shared helper extraction and parity tests |
| Pattern clear over-removes keys | Medium | Medium | safer pattern semantics and key-scope constraints |
| Silent swallow-catch hides outages | High | Medium | structured diagnostics and alerted failure telemetry |

## Pre-Cutover Checklist
- [ ] Correctness suite green (`default(T)` and atomic helpers).
- [ ] Concurrency and memory accounting tests within thresholds.
- [ ] Hybrid consistency tests green under tier-failure scenarios.
- [ ] Datasource parity tests green.
- [ ] Redis provider gate status explicitly approved per environment.
- [ ] Health/SLO alerts configured and validated.

## Post-Cutover Checklist
- [ ] Hit ratio and latency remain within expected bands.
- [ ] No lock contention anomalies or duplicate execution incidents.
- [ ] Memory usage and item counts remain reconciliation-safe.
- [ ] No critical provider-failure events without alerting.
