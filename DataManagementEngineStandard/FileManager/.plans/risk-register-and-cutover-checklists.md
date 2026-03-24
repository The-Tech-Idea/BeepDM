# Risk Register and Cutover Checklists

## Risk Register

| Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|
| Header mapping defaults to wrong column (`FirstOrDefault().Key`) | High | High | Replace with explicit lookup and missing-map handling tests |
| Unsafe list/entity guards (`Entities.Count==0` then `Entities[0]`) | High | Medium | Refactor guards and add empty-entity regression tests |
| Full-file rewrite on insert/bulk operations causes memory pressure | High | Medium | Streaming append/temp-file strategy and load tests |
| Inference instability due to `GetFieldsbyTableScan` loop defects | High | Medium | Fix loop logic, add deterministic type-scan tests |
| Parse/validation errors swallowed in catch blocks | High | High | Structured diagnostics with row/column/code and strict mode |
| Transaction semantics mismatch (snapshot flag vs atomic unit) | Medium | Medium | Documented semantics or true staged transaction implementation |
| Header validation ignores quoted delimiters | Medium | Medium | Parser-based header validation and quoted-header vectors |
| Type conversion fallback gaps (enum/culture handling) | Medium | Medium | Harden `CSVTypeMapper` conversions with explicit culture policy |

## Pre-Cutover Checklist
- [ ] `GetEntity` header-to-field mapping regression tests green.
- [ ] Parser regression suite (quoted delimiter + multiline + malformed rows) green.
- [ ] Large-file insert/query memory tests within thresholds.
- [ ] Validation and diagnostic contract validated by ETL/Mapping/Rules consumers.
- [ ] Security/masking policy profiles reviewed and enabled.
- [ ] ETL/Mapping/Rules integration tests green.
- [ ] KPI dashboards and alerts configured.
- [ ] Rollback plan validated in non-prod.

## Post-Cutover Checklist
- [ ] Parse error rate within allowed threshold.
- [ ] Reject ratio and throughput stable against baseline.
- [ ] No critical security policy violations observed.
- [ ] Canary/promoted environments pass KPI gates.
