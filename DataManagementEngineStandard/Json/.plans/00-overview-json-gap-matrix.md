# 00 - Overview: Json Gap Matrix

## Objective
Baseline current Json module capabilities and define a phased path to enterprise-grade behavior.

## In Scope
- `JsonDataSource.cs`, `JsonDataSourceAdvanced.cs`, `JsonExtensions.cs`
- `Helpers/*` in `DataManagementEngineStandard/Json/Helpers`

## Gap Matrix

| Capability | Current State | Gap | Target |
|---|---|---|---|
| Contracts/datasource API | Core datasource available | Capability boundaries not explicit | Clear contract profile + lifecycle controls |
| Query/filter correctness | Helper support exists | Complex path/filter semantics risk drift | Deterministic JSONPath/filter behavior |
| Schema governance | Inference/sync helpers exist | Version policy and drift workflow limited | Versioned schema + drift checks |
| Graph hydration | Advanced helpers available | Deep relation handling needs guardrails | Predictable graph hydration with limits |
| CRUD consistency | CRUD helper exists | Conflict/consistency handling limited | Safer write semantics and reconciliation |
| Performance | Cache + async helpers exist | Throughput/memory tuning policy incomplete | Bounded caches + async backpressure |
| Security/governance | Minimal policy surface | Missing masking and policy enforcement | Policy profiles + audit-ready controls |
| Scale strategy | Works for normal docs | Large docs/collections strategy unclear | Partition/index/document-size strategy |
| Integrations | Usable in datasource ecosystem | No formal integration contracts | ETL/Mapping/Rules/Forms tested contracts |
| Operations | Minimal KPI controls | No rollout/canary guidance | KPI-driven staged rollout and rollback |
