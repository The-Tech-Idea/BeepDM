---
name: beepdm-etl
description: Use when designing, running, scheduling, or observing BeepDM ETL pipelines — extract/transform/load across datasources, with built-in transformers, validators, schedulers, notifiers, and observability. Hands off to Configuration (mappings), Migration (target schema), and UoW (per-entity transactions) skills.
---

# beepdm-etl

The ETL engine in BeepDM is **pipeline-based**: a `PipelineDefinition` is a DAG of steps, each backed by a plugin (source, transformer, validator, sink, scheduler, notifier). The engine executes the pipeline, persists checkpoints, and emits observability data.

## When to use this skill

- Moving data between two datasources (RDBMS, files, REST, NoSQL, …).
- Building reusable source / sink / transformer plugins.
- Scheduling pipelines (cron, file-watch, event bus, dependency, manual).
- Adding validation steps (uniqueness, range, regex, referential integrity, expression).
- Wiring notifications (webhook, email, log file) for pipeline events.
- Observing pipeline runs (metrics, dashboards, cost models, redaction, alerts).

## Do NOT use this skill for

- First-run data load on a brand-new database → use **beepdm-setup** (it can hand off to ETL for non-trivial seed data).
- Schema changes on an existing datasource → use **beepdm-migration**.
- Transactional CRUD in app code → use **beepdm-unitofwork**.

## File Locations

`DataManagementEngineStandard/Editor/ETL/`:

- `Engine/PipelineEngine.cs` — core executor
- `Engine/PipelineManager.cs` — high-level pipeline management
- `Engine/PipelineStepRunner.cs` — single-step execution
- `Engine/PipelineCheckpointManager.cs` — checkpoint + resume
- `Engine/PipelineRetryPolicy.cs` — retry/backoff
- `Engine/PipelineQualityGate.cs` — quality gates
- `Engine/PipelineLineageTracker.cs` — data lineage
- `Engine/PipelineChannelBridge.cs` — channel bridge
- `Engine/PipelineTestHarness.cs`, `TestDataGenerator.cs`, `ReleaseManager.cs`
- `Engine/BuiltIn/Sources/` — `DataSourcePlugin`, `CsvSourcePlugin`, …
- `Engine/BuiltIn/Transformers/` — `FieldMapTransformer`, `FilterTransformer`, `AggregateTransformer`, `DeDuplicateTransformer`, `LookupTransformer`, `SplitTransformer`, `TypeCastTransformer`, `ExpressionTransformer`, `ScriptTransformer`
- `Engine/BuiltIn/Validators/` — `NotNullValidator`, `RangeValidator`, `RegexValidator`, `UniquenessValidator`, `ReferentialIntegrityValidator`, `CustomExpressionValidator`
- `Engine/BuiltIn/Sinks/` — `DataSinkPlugin`, `ErrorLogSinkPlugin`
- `Engine/BuiltIn/Schedulers/` — `CronScheduler`, `FileWatchScheduler`, `EventBusScheduler`, `PipelineDependencyScheduler`, `ManualScheduler`
- `Engine/BuiltIn/Notifiers/` — `EmailNotifier`, `WebhookNotifier`, `LogFileNotifier`
- `Engine/Security/SecurityPolicyEngine.cs`
- `Engine/Workflow/WorkFlowEngine.cs`, `WorkFlowStorage.cs`, `WorkFlowMigration.cs`
- `Engine/Expressions/SimpleExpressionEvaluator.cs`
- `Engine/MigrationManager.cs` — ETL-side migration helper
- `Registry/PipelinePluginRegistry.cs`, `PipelinePluginDescriptor.cs`
- `Scheduling/SchedulerHost.cs`, `WatermarkTracker.cs`, `PipelineRunQueue.cs`, …
- `Observability/MetricsEngine.cs`, `ObservabilityStore.cs`, `PipelineDashboardApi.cs`, `CostModel.cs`, `FieldRedactor.cs`, `AlertingEngine.cs`
- `ETL/Engine/MigrationManager.cs` — ETL-internal migration helper

## Core Concepts

- **PipelineDefinition** — DAG of steps with input/output channels.
- **Step** — one node; backed by an `IPipelineStep` plugin (source/transformer/validator/sink).
- **Scheduler** — when the pipeline runs (cron, file-watch, event, dependency, manual).
- **Notifier** — who gets told on success/failure (email, webhook, log).
- **Checkpoint** — persisted state so a failed run resumes, not restarts.
- **Quality Gate** — pass/fail thresholds; failing a gate halts the pipeline.

## Typical Workflow

1. **Define** a `PipelineDefinition` (sources, transformers, validators, sinks, scheduler, notifier).
2. **Validate** with `PipelineTestHarness` against test data.
3. **Register** the pipeline in `PipelineManager` (persisted).
4. **Execute** via `PipelineEngine`; checkpoints written as it runs.
5. **Observe** via `MetricsEngine` / `PipelineDashboardApi`.
6. **Promote** via `ReleaseManager` once quality gates pass.

## How this skill works with the rest of the data-management layer

| Handoff | Direction | What flows |
|---|---|---|
| **beepdm-configuration** | ← Config | ETL reads `EntityDataMap` from `EntityMappingManager` to translate field names. It does **not** invent its own mapping store. |
| **beepdm-migration** | ← Migration | ETL assumes the target schema already exists. If a column is missing, ETL should fail fast and surface the gap — not auto-migrate. |
| **beepdm-schema** | ← Schema migration | `ETLEditor.TryRunImportingPreflightAsync` calls `ISchemaManager` directly. No more spinning up a full `DataImportManager` just for preflight. |
| **beepdm-unitofwork** | ↔ UoW | For per-record transactional work inside a sink, the sink can wrap each batch in a UoW. UoW is the per-entity API; ETL is the per-pipeline API. |
| **beepdm-setup** | ← Setup | On first run, the wizard's seeding phase may invoke a one-shot pipeline to load reference data. After that, ETL is the runtime mover. |
| **beepdm-forms** | ↔ Forms | ETL may produce entity records that Forms then display. Forms do not trigger ETL runs. |

## Design Rules

- Pipelines are **durable**: a crashed run resumes from the last checkpoint, not from step 1.
- Built-in plugins are reference implementations; **the plugin registry is the extension surface**.
- Validators short-circuit — a failed `NotNullValidator` does not silently produce partial output.
- Use `IDataSourceHelper` for dialect-specific DML inside sink plugins. Do not hardcode SQL.
- Observability is not optional: emit metrics, record lineage, redact PII.
- Schedulers are independent of engines — `SchedulerHost` owns the tick; the engine owns the work.

## Cross-references

- See **beepdm-configuration** for the mapping store ETL reads from.
- See **beepdm-migration** for schema changes that gate ETL runs.
- See **beepdm-schema** for the preflight service ETL calls.
- See **beepdm-unitofwork** for per-record transactions inside sinks.
- See **beepdm-workflow** for higher-level orchestration that can invoke pipelines.
- See **beepdm-retry** for the shared `IRetryPipeline` primitive. ETL pipeline-run retry should compose it (see `Editor/ETL/Scheduling/SchedulerHost.cs:484` for the current manual-retry loop and the why-not-yet-pipeline comment); the existing 7 manual retry loops in ETL/Setup/Proxy/WebAPI are documented in the retry skill's boundary table.
- See `.cursor/etl/SKILL.md` for the deep-dive implementation details.
