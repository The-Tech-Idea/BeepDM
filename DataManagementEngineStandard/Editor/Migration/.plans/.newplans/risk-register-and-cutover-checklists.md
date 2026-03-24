# Risk Register and Cutover Checklists (Focused Scope)

## Risk Register

| Risk ID | Risk | Impact | Mitigation |
|---|---|---|---|
| R-01 | Explicit/discovery path drift remains after refactor | inconsistent migration outcomes | enforce shared pipeline and parity tests |
| R-02 | Readiness report schema breaks downstream automation | failed CI gates or false pass | version report schema and keep backward-compatible adapter |
| R-03 | DDL outcome classification incomplete on some operations | unsafe rollout decisions | add coverage tests for all entity/column/index operations |
| R-04 | Assembly scan ordering changes silently | nondeterministic entity discovery | introduce deterministic ordering and snapshot checks |
| R-05 | Capability probes and guidance go out of sync | incorrect operator recommendations | map recommendations directly to probe evidence ids |

## Pre-Cutover Checklist
- [ ] Path parity tests pass for explicit and discovery modes.
- [ ] Structured readiness report is generated and validated in CI.
- [ ] DDL operation outcomes are classified and audited in logs/artifacts.
- [ ] Discovery evidence artifact includes scanned/skipped assemblies and loader issues.
- [ ] Recommendation profile version is pinned for rollout.

## Cutover Checklist
- [ ] Run readiness gate in target environment with production policy.
- [ ] Review blocking issues and recommendation ids.
- [ ] Execute migration in staged mode with evidence capture enabled.
- [ ] Validate post-run summary against expected decision counts.
- [ ] Approve rollout only if no unclassified operation outcomes exist.

## Rollback Checklist
- [ ] Preserve pre-cutover readiness and summary artifacts.
- [ ] Revert profile version if recommendation overlay caused policy drift.
- [ ] Re-run discovery evidence capture to confirm assembly/source stability.
- [ ] Re-run parity validation before retry.
