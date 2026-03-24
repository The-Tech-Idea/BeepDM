# Risk Register and Cutover Checklists

## Risk Register

| Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|
| Notification suppression leak in filter path | High | Medium | Fix/guard `Filter.ApplyFilter` state restoration |
| Deleted item tracking lookup mismatch | High | Medium | Rework `GetTrackingItem` delete path and tests |
| Recursive predicate bug in advanced search | Medium | Medium | Refactor `Search.AdvancedSearch` composition |
| Concurrency defects under contention | High | Medium | Internal lock policy for core mutators (Phase 3) |
| Freeze guard bypass in batch add | Medium | Medium | Align `AddRange` with freeze invariant checks |
| Virtual page handler accumulation | Medium | Low-Med | Explicit unhook strategy in virtual loading |
| Export fidelity loss for complex values | Medium | Medium | Versioned export schema and typed serialization |
| Poor operational visibility | Medium | Medium | Structured telemetry and KPI rollout gates |

## Pre-Cutover Checklist
- [ ] Core lifecycle and tracking tests pass.
- [ ] Filter/search known hotspot tests pass (suppression + predicate composition).
- [ ] Concurrency and performance tests within thresholds.
- [ ] Validation/computed consistency checks green.
- [ ] Undo/redo and navigation reliability verified.
- [ ] Integration and KPI gates configured.
- [ ] Rollback procedure rehearsed in non-prod.

## Post-Cutover Checklist
- [ ] Error rate and latency within agreed SLO.
- [ ] Memory profile stable under sustained workloads.
- [ ] No critical tracking/navigation regressions observed.
