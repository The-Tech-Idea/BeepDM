# Phase 6 - Observability, SLO, and Alerting

## Balanced Alignment
This phase is aligned with [Balanced -> Proxy adoption mapping](../../Balanced/.plans/proxydatasource-adoption-mapping.md).

## Objective
Standardize telemetry and alerting for proxy-layer reliability and performance.

## Scope
- Metrics and trace context.
- SLO targets and alert conditions.

## File Targets
- `Proxy/ProxyDataSource.cs`
- `Proxy/README.md`

## Planned Enhancements
- Core metrics:
  - request rate
  - success/failure rate
  - failover count
  - circuit-open duration
  - p95 latency
  - cache hit ratio
- SLO profiles by workload criticality.
- Alerting thresholds:
  - failover storms
  - sustained circuit-open state
  - latency SLO breach
  - error-rate spike

## Audited Hotspots
- `ProxyDataSource.RecordSuccess(...)` / `RecordFailure(...)`
- `ProxyotherClasses.DataSourceMetrics` field update model
- `ProxyDataSource` logging in retry/failover paths

## Real Constraints to Address
- `AverageResponseTime` updates are non-atomic and can race under concurrency.
- Observability is log-heavy; few structured event contracts exist for downstream alerting.
- Failed attempt history (datasource, exception class, retry index) is not emitted consistently.

## Acceptance Criteria
- Proxy emits consistent telemetry and correlation IDs.
- SLO breaches are detectable with actionable context.
