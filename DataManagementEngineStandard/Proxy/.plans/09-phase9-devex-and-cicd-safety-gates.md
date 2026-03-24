# Phase 9 - DevEx and CI/CD Safety Gates

## Balanced Alignment
This phase is aligned with [Balanced -> Proxy adoption mapping](../../Balanced/.plans/proxydatasource-adoption-mapping.md).

## Objective
Enable safe and repeatable proxy policy changes through automated validation and release gating.

## Scope
- Policy linting and simulation checks in CI.
- Developer tooling for policy testing.

## File Targets
- `Proxy/README.md`
- `Proxy/HOWTO_Add_ProxyDataSource.md`
- `Proxy/ProxyDataSource.cs`

## Planned Enhancements
- CI gates:
  - policy schema validation
  - routing simulation checks
  - resilience profile compliance checks
- Dev tooling:
  - profile diff
  - failover simulation scripts
  - benchmark baseline compare

## Audited Hotspots
- Retry wrapper family around `IDataSource` methods in `ProxyDataSource.cs`
- `RetryPolicy(...)` usage patterns (`.Result`/`.Wait()`)
- constructor policy initialization path

## Real Constraints to Address
- CI must block regressions that reintroduce duplicate operation execution.
- Simulation tooling must include write-idempotency and failover path assertions.
- Policy drift between runtime properties and `_options` must be test-gated.

## Acceptance Criteria
- Unsafe profile changes are blocked before production.
- Developers can run deterministic local simulations.
