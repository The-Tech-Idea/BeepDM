# Phase 10 - Rollout, Observability, and KPIs

## Objective
Define safe rollout strategy and production observability for FileManager enhancements.

## Scope
- Staged rollout strategy (dev -> qa -> pilot -> prod).
- Ingestion metrics and operational dashboards.
- KPI gates and rollback criteria.

## File Targets
- `FileManager/CSVDataSource.cs`
- `FileManager/README.md`
- Ops dashboards/alerting and deployment scripts

## Planned Enhancements
- Emit metrics: row throughput, parse error rate, reject ratio, p95 ingest latency.
- Add feature flags/canary controls for parser and inference updates.
- Provide rollback playbook tied to KPI thresholds.

## Audited Hotspots
- `CSVDataSource.GetEntity(...)` / paged `GetEntity(...)` throughput + reject patterns
- `CSVDataSource.BeginTransaction/Commit/Rollback` behavior vs operational expectations
- `CSVDataSource` malformed-row catch/continue sections

## Real Constraints to Address
- Current observability is log-only and not metric-structured.
- Transaction naming implies guarantees not currently implemented.
- Rollout risk is concentrated in parser/filter correctness and mutation rewrite paths.

## Acceptance Criteria
- KPIs are visible and alertable per environment.
- Promotion gates enforce KPI criteria before rollout.
- Rollback procedure is tested and documented.
