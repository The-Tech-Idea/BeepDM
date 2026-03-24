# Risk Register and Cutover Checklists

## Risk Register

| Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|
| Query/filter behavior drift | High | Medium | Deterministic query vectors + strict diagnostics |
| Schema inference misclassification | Medium | Medium | Confidence scores + fallback and drift checks |
| Deep graph recursion/cycle issues | High | Medium | Depth/cycle limits and guarded hydration options |
| Concurrent write conflicts | High | Medium | Version token strategy + retry/compensation |
| Cache inconsistency | Medium | Medium | Deterministic invalidation and cache tests |
| Sensitive data leakage in logs | High | Low-Med | Path-based masking and policy enforcement |
| Large document memory pressure | High | Medium | Chunking and bounded buffers |
| Integration contract drift | Medium | Medium | End-to-end integration test gates |

## Pre-Cutover Checklist
- [ ] Query/filter regression suite green.
- [ ] Schema sync/drift workflows validated.
- [ ] Graph hydration limits configured and tested.
- [ ] CRUD conflict/recovery tests pass.
- [ ] Security and masking policies enabled.
- [ ] KPI dashboards and alerts configured.
- [ ] Rollback plan tested in non-prod.

## Post-Cutover Checklist
- [ ] p95 query latency within threshold.
- [ ] write failure/conflict rates within threshold.
- [ ] cache hit ratio and memory profile stable.
- [ ] no critical policy/masking violations observed.
