# Phase 2 - Query Defaults and DataSource Resolver

## Objective
Enhance query-driven defaults with safer execution patterns, richer query modes, and predictable fallback behavior.

## Scope
- Expand `DataSourceResolver` query capabilities and safety controls.
- Add structured query rule modes (`scalar`, `first`, `exists`, `aggregate`).
- Add parameter binding from runtime context (`IPassedArgs`/dictionary).

## File Targets
- `Defaults/Resolvers/DataSourceResolver.cs`
- `Defaults/Resolvers/BaseDefaultValueResolver.cs`
- `Defaults/Resolvers/DefaultValueResolverManager.cs`
- `Defaults/Helpers/DefaultValueValidationHelper.cs`

## Planned Enhancements
- New rule forms:
  - `QUERY.scalar.<sqlOrTemplate>`
  - `QUERY.first.<entity>.<field>.<predicate>`
  - `QUERY.exists.<entity>.<predicate>`
  - `QUERY.aggregate.<func>.<entity>.<field>.<predicate>`
- Parameterized placeholder model:
  - `@FieldName` and context tokens from `IPassedArgs`.
- Query execution guardrails:
  - allowlist of commands (default `SELECT` only),
  - max rows / timeout options,
  - optional read-only datasource policy.
- Fallback contract:
  - `ONERROR.static.<value>`
  - `ONEMPTY.static.<value>`

## Implementation Rules (Skill Constraints)
- Execute query defaults through `IDataSource` contract surfaces (`idatasource`), keeping behavior datasource-agnostic.
- Maintain `ErrorObject`-style failure handling and avoid exception-only control flow for routine resolver failures (`idatasource`).
- Resolve datasource instances via `IDMEEditor` and validated open-state patterns (`beepdm` + `beepservice`), not direct ad-hoc connections.
- Keep datasource/query configuration persistence under `ConfigEditor` ownership (`configeditor`).
- Any file/folder location used for query-policy profiles must be created via `EnvironmentService` helpers (`environmentservice`).

## Acceptance Criteria
- Query defaults support deterministic parameter binding.
- Unsafe query patterns are blocked by validator.
- Timeout/empty/error cases return configured fallback.
- Existing `GETSCALAR/LOOKUP/COUNT/MAX/MIN/SUM/AVG` remain supported.

## Risks and Mitigations
- Risk: SQL injection by rule text.
  - Mitigation: parameterization, command allowlist, and query validator.
- Risk: cross-datasource behavior differences.
  - Mitigation: datasource capability checks and normalized result handling.

## Test Plan
- Unit tests for each query mode.
- Injection and unsafe-command negative tests.
- Integration tests on at least two datasource types.
