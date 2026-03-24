# Phase 7 - Security, Audit, and Compliance

## Balanced Alignment
This phase is aligned with [Balanced -> Proxy adoption mapping](../../Balanced/.plans/proxydatasource-adoption-mapping.md).

## Objective
Add proxy-layer governance for secure routing, auditable decisions, and compliance-sensitive controls.

## Scope
- Security policy for routing and credentials.
- Audit records for failover/routing decisions.

## File Targets
- `Proxy/ProxyDataSource.cs`
- `Proxy/README.md`

## Planned Enhancements
- Security controls:
  - sensitive query/log redaction
  - policy-based datasource allowlist
  - protected operation routing rules
- Audit model:
  - route decision reason
  - circuit decision reason
  - policy/profile version at execution time

## Audited Hotspots
- `ProxyDataSource` log calls that include query and exception text
- `OnFailover` event payload and logging surface
- datasource selection methods (`Current`, `Failover`, routing helpers)

## Real Constraints to Address
- Current logs can expose sensitive operation details without redaction policy.
- No immutable audit envelope for route and failover decisions.
- No policy version stamp attached to runtime routing decisions.

## Acceptance Criteria
- Security-sensitive details are redacted from logs.
- Routing and failover decisions are audit-traceable.
