# DefaultsManager Risk Register and Rollout Checklist

## Risk Register

| ID | Risk | Impact | Mitigation | Phase |
|---|---|---|---|---|
| D1 | New DSL parsing breaks legacy rules | High | Compatibility mode + dual parser + warning period | 1, 7 |
| D2 | Query rule misuse leads to unsafe execution | High | Allowlist, parameterization, timeout, validation gates | 2, 5 |
| D3 | Expression semantics diverge across resolvers | Medium | Unified operator contract and equivalence tests | 3 |
| D4 | Resolver registration conflicts | Medium | Priority and duplicate-operator checks | 4 |
| D5 | Validation noise blocks adoption | Medium | Warning mode rollout before strict mode | 5, 7 |
| D6 | Caching introduces stale defaults | Medium | TTL controls and volatile-rule bypass | 6 |
| D7 | Migration overhead is high | Medium | Rule scanner/autofix and phased rollout | 7 |

## Pre-Rollout Checklist
- Inventory existing rules and classify by syntax/resolver.
- Enable compatibility mode and warning-only validation.
- Define operator allowlist for dot DSL v1.
- Define query policy (read-only, timeout, result-shape rules).

## Rollout Checklist
- Wave 1: enable new DSL for pilot entities.
- Wave 2: enable query enhancements and strict validation for pilots.
- Wave 3: expand to all entities and promote strict mode.
- Capture metrics each wave:
  - parse errors
  - resolver failures
  - query timeout count
  - fallback usage rate

## Post-Rollout Checklist
- Confirm legacy-rule pass rate remains acceptable.
- Review validation failures and tune rule docs.
- Publish migration completion summary.
- Lock down strict mode as default for new rules.
