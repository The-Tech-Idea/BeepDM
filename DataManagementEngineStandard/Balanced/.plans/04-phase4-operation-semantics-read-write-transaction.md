# Phase 4 - Operation Semantics (Read/Write/Transaction)

## Objective
Define safe semantics for reads, writes, and transaction behavior across multiple datasources.

## Scope
- Read/write split policies.
- Transaction safety constraints.

## File Targets (planned)
- `Balanced/BalancedDataSource.cs`
- `Balanced/Policies/OperationPolicy.cs`

## Planned Enhancements
- Read policy:
  - read-mostly to replicas/secondary
  - fallback to primary on consistency-demanded calls
- Write policy:
  - primary-only writes by default
  - optional synchronous mirror mode (explicit)
- Transaction policy:
  - single-source transaction affinity
  - block/guard unsupported distributed transaction patterns

## Acceptance Criteria
- Operation type controls routing/failover behavior deterministically.
- Transaction behavior is explicitly documented and policy-gated.
