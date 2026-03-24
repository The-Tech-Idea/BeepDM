# ProxyDataSource Enhancement Plans

Phased enterprise enhancement roadmap for `ProxyDataSource` in `DataManagementEngineStandard/Proxy`, revised from direct audit of:
- `ProxyDataSource.cs`
- `ProxyotherClasses.cs`
- `CircuitBreaker.cs`

## Balanced Alignment
- Cross-module adoption reference: [Balanced -> Proxy adoption mapping](../../Balanced/.plans/proxydatasource-adoption-mapping.md)
- This `Proxy/.plans` set is the implementation-facing track; the Balanced mapping is the architecture alignment source.

## Execution Order
1. [00-overview-proxydatasource-gap-matrix.md](./00-overview-proxydatasource-gap-matrix.md)
2. [01-phase1-contracts-and-policy-foundation.md](./01-phase1-contracts-and-policy-foundation.md)
3. [02-phase2-resilience-profiles-and-error-taxonomy.md](./02-phase2-resilience-profiles-and-error-taxonomy.md)
4. [03-phase3-advanced-routing-and-load-distribution.md](./03-phase3-advanced-routing-and-load-distribution.md)
5. [04-phase4-retry-idempotency-and-failover-semantics.md](./04-phase4-retry-idempotency-and-failover-semantics.md)
6. [05-phase5-cache-strategy-and-consistency-controls.md](./05-phase5-cache-strategy-and-consistency-controls.md)
7. [06-phase6-observability-slo-and-alerting.md](./06-phase6-observability-slo-and-alerting.md)
8. [07-phase7-security-audit-and-compliance.md](./07-phase7-security-audit-and-compliance.md)
9. [08-phase8-performance-and-capacity-engineering.md](./08-phase8-performance-and-capacity-engineering.md)
10. [09-phase9-devex-and-cicd-safety-gates.md](./09-phase9-devex-and-cicd-safety-gates.md)
11. [10-phase10-rollout-governance-and-kpi-gates.md](./10-phase10-rollout-governance-and-kpi-gates.md)
12. [implementation-hotspots-change-plan.md](./implementation-hotspots-change-plan.md)
13. [proxy-policy-template.md](./proxy-policy-template.md)
14. [standards-traceability-matrix.md](./standards-traceability-matrix.md)
15. [risk-register-and-cutover-checklists.md](./risk-register-and-cutover-checklists.md)

## Primary Outcomes
- More deterministic resilience behavior under failure.
- Safer and auditable policy/routing changes.
- Better operational performance and rollout governance.

## Audited Hotspots
- `ProxyDataSource` retry wrappers that execute the same operation twice (`RunQuery`, `ExecuteSql`, `CreateEntityAs`, multiple CRUD methods)
- `RetryPolicy(...)` + pervasive `.Result`/`.Wait()` blocking
- `_healthStatus` dictionary read/write from timer and request paths
- constructor option mismatch (`MaxRetries`/`RetryDelayMilliseconds` vs `_options`)
- transaction wrappers (`BeginTransaction`, `Commit`, `EndTransaction`) result propagation
