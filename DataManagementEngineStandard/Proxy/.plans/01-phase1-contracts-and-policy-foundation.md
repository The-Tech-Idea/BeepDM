# Phase 1 - Contracts and Policy Foundation

## Balanced Alignment
This phase is aligned with [Balanced -> Proxy adoption mapping](../../Balanced/.plans/proxydatasource-adoption-mapping.md).

## Objective
Establish explicit proxy policy contracts and lifecycle model without breaking existing `IDataSource` behavior.

## Scope
- Policy model for routing/retry/cache/circuit behavior.
- Compatibility layer for current constructor/options.

## File Targets
- `Proxy/ProxyDataSource.cs`
- `Proxy/ProxyotherClasses.cs`

## Planned Enhancements
- Add policy schema:
  - routing profile
  - retry profile
  - cache profile
  - circuit profile
- Add policy metadata:
  - version
  - owner
  - environment scope
  - change reason
- Keep current API path as default compatibility mode.

## Audited Hotspots
- `ProxyDataSource` constructor option initialization and `_options` usage
- `ProxyotherClasses.ProxyDataSourceOptions`
- `ProxyDataSource.RetryPolicy(...)` shared wrapper contract

## Real Constraints to Address
- Runtime knobs (`MaxRetries`, `RetryDelayMilliseconds`, `HealthCheckIntervalMilliseconds`) are not a single source of truth with `_options`.
- Policy contract must preserve existing `IDataSource` API while removing ambiguous behavior.

## Implementation Rules (Skill Constraints)
- Preserve `IDataSource` contract behavior and error semantics.
- Keep orchestration anchored to `IDMEEditor` integration patterns.
- Persist proxy policy via `ConfigEditor`-aligned persistence flow when externalized.

## Acceptance Criteria
- Policies are loadable/configurable without API breakage.
- Existing proxy usages continue to execute unchanged in compatibility mode.
