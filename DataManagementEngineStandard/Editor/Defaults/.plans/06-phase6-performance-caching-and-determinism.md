# Phase 6 - Performance, Caching, and Determinism

## Objective
Improve default-value resolution throughput and consistency with caching, pre-compilation, and deterministic controls.

## Scope
- Cache query and expression results where safe.
- Pre-parse rules to reduce per-record overhead.
- Add deterministic mode for repeatable results.

## File Targets
- `Defaults/Resolvers/DefaultValueResolverManager.cs`
- `Defaults/Resolvers/ExpressionResolver.cs`
- `Defaults/Resolvers/DataSourceResolver.cs`
- `Defaults/DefaultsManager.Extended.cs`

## Planned Enhancements
- Rule compile cache:
  - parse once, evaluate many.
- Value cache policy:
  - keying by rule + context hash,
  - TTL/size controls,
  - opt-out for non-deterministic operators (`RANDOM`, volatile timestamps).
- Query cache for idempotent lookup patterns with short TTL.
- Determinism controls:
  - explicit `volatile` operator classification.

## Implementation Rules (Skill Constraints)
- Keep cache execution datasource-agnostic and compatible with `IDataSource` contract behavior (`idatasource`).
- Route cache-config and deterministic-mode toggles via `ConfigEditor` persistence patterns (`configeditor`).
- Integrate cache lifecycle under `IDMEEditor` orchestration (single source of runtime truth) rather than static global side systems (`beepdm`).
- Store any cache artifacts only in sanctioned app/container locations from `EnvironmentService` (`environmentservice`).
- Ensure caching assumptions are safe for long-lived shared editor/service lifetimes (`beepservice`), including invalidation on connection/profile changes.

## Acceptance Criteria
- Reduced per-call latency for repeated rule evaluations.
- Cache bypass for volatile/non-deterministic rules.
- Memory growth bounded by cache policy limits.

## Risks and Mitigations
- Risk: stale cached values.
  - Mitigation: conservative TTL and invalidation hooks.
- Risk: cache key misses due to unstable context serialization.
  - Mitigation: canonical context key builder.

## Test Plan
- Benchmark tests before/after caching.
- Determinism tests with fixed input and seeded settings.
- Cache invalidation and TTL behavior tests.
