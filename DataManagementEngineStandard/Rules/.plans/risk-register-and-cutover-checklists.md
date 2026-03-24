# Risk Register and Cutover Checklists

## Risk Register

| Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|
| Parser behavior drift breaks existing rules | High | Medium | Golden tests + compatibility gates (Phase 2/7) |
| Inconsistent type coercion causes silent data issues | High | Medium | Explicit coercion matrix + strict diagnostics (Phase 3) |
| Unbounded caching increases memory pressure | Medium | Medium | Capacity limits + deterministic eviction (Phase 6) |
| Unsafe runtime execution path | High | Low-Med | Sandbox policy + governance states (Phase 5) |
| Cross-module invocation inconsistencies | Medium | Medium | Shared context contract + integration tests (Phase 8) |
| Operational blind spots in production | High | Medium | KPI dashboards + rollout gates (Phase 9) |

## Cutover Checklist (Per Environment)
- [ ] Baseline compatibility tests pass.
- [ ] Golden parse/eval outputs match approved snapshots.
- [ ] Sandbox policy profile reviewed and enabled.
- [ ] Cache capacity settings validated under load.
- [ ] Integration tests for Defaults/Mapping/ETL/Forms pass.
- [ ] KPI thresholds configured and monitored.
- [ ] Rollback switch and runbook validated.

## Post-Cutover Checklist
- [ ] p95 evaluation latency within threshold.
- [ ] Parse/evaluation error rates within threshold.
- [ ] No unapproved rules executing in production scope.
- [ ] Audit trail entries flowing for rule lifecycle events.
