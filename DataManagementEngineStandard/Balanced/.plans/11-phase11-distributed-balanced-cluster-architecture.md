# Phase 11 - Distributed Balanced Cluster Architecture

## Objective
Define how multiple `BalancedDataSource` nodes run across servers, with concrete architecture options and selection criteria.

## Scope
- Compare external load balancer only vs coordinator-based cluster.
- Define operational, consistency, and failover implications.

## Option A - External Load Balancer Only (Stateless Node Model)

### Architecture
- Multiple app nodes host `BalancedDataSource` instances independently.
- External LB (NGINX/HAProxy/K8s ingress/service mesh) distributes incoming traffic.
- Each node manages local routing/failover/circuit/cache against backend datasource pool.

### Pros
- Simple to deploy and operate.
- Mature tooling and clear ownership boundaries.
- No distributed consensus logic in app layer.
- Easiest rollback path.

### Cons
- Node-level decisions are independent (no shared circuit/cache state).
- Policy updates may propagate eventually, not instantly.

### Best For
- Most production scenarios where infrastructure LB is already available.
- Teams prioritizing operational simplicity.

## Option B - Coordinator-Based Cluster (Shared Control Plane)

### Architecture
- Keep external LB for traffic distribution.
- Add a `DistributedBalancedCoordinator` (new class/service) for control-plane features:
  - shared policy/version distribution
  - node membership and health registry
  - optional global circuit/open-state hints
  - drain/failback orchestration
- Use durable shared store (Redis/DB/etcd) for coordinator state.

### Pros
- Better global consistency of routing/resilience behavior.
- Faster coordinated policy rollout and controlled failback.
- Supports advanced multi-node traffic engineering.

### Cons
- Higher complexity and operational risk.
- Requires distributed state correctness and failure-mode design.
- Additional moving parts and observability burden.

### Best For
- Large clusters with strict coordination requirements.
- Advanced traffic governance and centralized policy control needs.

## Decision Matrix

| Criterion | Option A: External LB Only | Option B: Coordinator-Based |
|---|---|---|
| Complexity | Low | High |
| Time to Implement | Fast | Medium/Slow |
| Operational Overhead | Low | Medium/High |
| Cross-Node Consistency | Medium | High |
| Rollback Simplicity | High | Medium |
| Advanced Traffic Control | Medium | High |

## Recommended Sequence
1. Implement **Option A** first (baseline production architecture).
2. Add policy versioning and metrics to assess if cluster coordination is truly needed.
3. Introduce **Option B** only when clear requirements justify complexity.

## Design Guardrails
- Keep data-plane logic in `BalancedDataSource`.
- Keep cluster coordination in a separate class/service.
- Do not couple request execution path to coordinator availability (fail-open control plane where possible).
- Maintain compatibility mode for single-node deployments.

## Acceptance Criteria
- Architecture decision is documented with rationale.
- Target option chosen per environment tier.
- Failure-mode and rollback runbook defined for chosen option.
