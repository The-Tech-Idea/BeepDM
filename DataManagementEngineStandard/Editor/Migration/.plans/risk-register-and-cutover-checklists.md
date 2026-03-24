# MigrationManager Risk Register and Cutover Checklists

## Risk Register

| ID | Risk | Impact | Mitigation | Phase |
|---|---|---|---|---|
| MG1 | Destructive change causes data loss | High | Policy blocker + backup/rollback readiness | 2, 6 |
| MG2 | Provider capability mismatch fails runtime apply | High | Capability matrix + preflight portability checks | 3, 4 |
| MG3 | Schema drift between planning and execution | Medium/High | Pre-apply drift detection + plan hash validation | 4, 5 |
| MG4 | Mid-run failure leaves partial state | High | Checkpointing + resumable orchestration | 5 |
| MG5 | Rollback unavailable for certain operations | High | Compensation strategy + forward-fix playbook | 6 |
| MG6 | Migration diagnostics insufficient for incident response | Medium | Structured logs + correlation IDs | 7 |
| MG7 | Large migrations cause lock contention/outage | High | Lock-aware scheduling + throttled execution | 8 |
| MG8 | Unsafe migration reaches production via CI gaps | High | CI policy gates + release evidence | 9 |
| MG9 | Rollout advances despite KPI degradation | High | Wave gates + hard-stop criteria | 10 |

## Pre-Cutover Checklist
- Migration plan generated and approved.
- Risk score and policy verdict reviewed.
- Dry-run and preflight checks passed.
- Backup/snapshot and restore test validated.
- Rollback/compensation plan documented.

## Cutover Checklist
- Freeze window confirmed.
- Correlation id tracking enabled.
- Execute by approved runbook sequence.
- Monitor step outcomes and checkpoint updates.
- Halt on hard-stop trigger conditions.

## Post-Cutover Checklist
- Verify schema state and migration summary parity.
- Validate application smoke tests against migrated datasource.
- Review diagnostics, warnings, and policy exceptions.
- Publish outcome report and KPI deltas.
