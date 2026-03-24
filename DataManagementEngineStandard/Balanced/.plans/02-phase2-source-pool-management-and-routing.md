# Phase 2 - Source Pool Management and Routing

## Objective
Implement backend source pool management and operation-aware routing policies.

## Scope
- Register/manage multiple underlying `IDataSource` instances.
- Route selection by policy and health.

## File Targets (planned)
- `Balanced/BalancedDataSource.cs`
- `Balanced/Routing/RouteSelector.cs`
- `Balanced/Routing/RoutingPolicy.cs`

## Planned Enhancements
- Pool registration with source metadata (role, weight, capability tags).
- Routing strategies:
  - weighted random
  - latency-aware
  - least-outstanding
  - sticky-key
- Route explainability payload (chosen source + reason).

## Acceptance Criteria
- Multiple datasources can be configured and selected deterministically.
- Routing is configurable by operation type and workload profile.
