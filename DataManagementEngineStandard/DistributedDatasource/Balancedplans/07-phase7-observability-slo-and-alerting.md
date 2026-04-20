# Phase 7 - Observability, SLO, and Alerting

## Objective
Define operational telemetry and SLO-based alerting for BalancedDataSource.

## Scope
- Standard metrics and trace correlation.
- SLO profiles and alert triggers.

## File Targets (planned)
- `Balanced/BalancedDataSource.cs`
- `Balanced/Observability/BalancedMetrics.cs`

## Planned Enhancements
- Core metrics:
  - request count
  - success/failure rate
  - failover rate
  - circuit-open time
  - p95 latency
  - cache hit ratio
- SLO tiers:
  - critical
  - standard
  - best-effort
- Alert triggers:
  - failover storms
  - latency breach
  - error spikes
  - sustained unhealthy pool size

## Acceptance Criteria
- Metrics emitted consistently with correlation id linkage.
- SLO breaches produce actionable alerts.
