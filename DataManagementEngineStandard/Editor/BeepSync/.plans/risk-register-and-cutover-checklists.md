# BeepSync Risk Register and Cutover Checklists

## Risk Register

| ID | Risk | Impact | Mitigation | Phase |
|---|---|---|---|---|
| BS1 | Incorrect schema version used at runtime | High | Schema version governance + plan hash checks | 1, 2 |
| BS2 | Watermark/CDC drift causes data loss or duplicates | High | Replay windows + dedupe + drift checks | 3, 5 |
| BS3 | Bidirectional conflicts produce divergence | High | Deterministic conflict policies + quarantine path | 4 |
| BS4 | Retry loops on non-transient errors | Medium | Retry class policy and stop conditions | 5 |
| BS5 | Reconciliation mismatch not detected early | Medium/High | DQ thresholds + reconciliation report gating | 6 |
| BS6 | Freshness SLO breach in production | High | SLO alerts + hard-stop runbook | 7, 10 |
| BS7 | Large sync overloads destination/source | Medium/High | Batch/concurrency caps + throttle profiles | 8 |
| BS8 | Unsafe schema changes reach production via CI gaps | High | CI lint/policy gates | 9 |
| BS9 | Rollout progresses despite KPI degradation | High | KPI gate enforcement per wave | 10 |

## Pre-Cutover Checklist
- [ ] Sync plan approved and schema version locked.
- [ ] Dry-run translator validation completed.
- [ ] Watermark/CDC settings reviewed.
- [ ] Conflict policy selected for bidirectional schemas.
- [ ] DQ/reconciliation thresholds confirmed.

## Cutover Checklist
- [ ] Execution window confirmed.
- [ ] Correlation id and metrics enabled.
- [ ] Pilot schema(s) executed first.
- [ ] KPI checks reviewed before wave advancement.

## Post-Cutover Checklist
- [ ] Reconciliation report verified.
- [ ] SLO/freshness metrics within threshold.
- [ ] Conflicts and retries reviewed.
- [ ] Follow-up actions captured for next phase.
