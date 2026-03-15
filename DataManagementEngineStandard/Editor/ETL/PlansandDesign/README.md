# BeepDM Pipeline & Workflow Framework — Design Documents Index

Enterprise-grade ETL, Workflow Orchestration, and Scheduling platform built on the BeepDM plugin ecosystem.

---

## Documents

| # | Document | Description | Est. Effort | Status |
|---|----------|-------------|-------------|--------|
| 00 | [Master Design Overview](./00-ETL-MasterDesign-Overview.md) | Goals, architecture overview, comparison vs SSIS/Talend/NiFi, model evolution, backward-compat strategy | — | ✅ Done |
| 01 | [Phase 1 — Plugin Architecture](./01-Phase1-Plugin-Architecture.md) | Core interfaces (`IPipelineSource`, `IPipelineSink`, `IPipelineTransformer`, `IPipelineValidator`, `IPipelineScheduler`), plugin registry, `PipelineRecord`, `PipelineDefinition`, `PipelineRunContext` | 6 days | ✅ Done |
| 02 | [Phase 2 — ETL Engine Core](./02-Phase2-ETL-Engine-Core.md) | `PipelineEngine` with streaming `IAsyncEnumerable<PipelineRecord>`, built-in source/sink adapters, retry policy, checkpointing, `PipelineManager` CRUD & legacy import | 14 days | ✅ Done |
| 03 | [Phase 3 — Workflow Framework](./03-Phase3-Workflow-Framework.md) | `WorkFlowDefinition`, `WorkFlowEngine`, parallel fan-out/conditional branching/human approval, built-in workflow templates, backward compat migration | 10 days | ✅ Done |
| 04 | [Phase 4 — Transform & Data Quality](./04-Phase4-Transform-DataQuality.md) | 9 transformer plugins, 6 validator plugins, expression grammar (EBNF), Roslyn engine, `DQReport`, column-level lineage, fluent builder API | 18 days | ✅ Done |
| 05 | [Phase 5 — Scheduling & Orchestration](./05-Phase5-Scheduling-Orchestration.md) | `SchedulerHost`, 6 scheduler plugins (cron, file-watch, event bus, webhook, dependency), priority run queue, DAG dependencies, rate limiting | 12 days | ✅ Done |
| 06 | [Phase 6 — Monitoring & Observability](./06-Phase6-Monitoring-Observability.md) | `PipelineRunLog`, `DataLineageStore`, `MetricsEngine`, `AlertingEngine`, notifier plugins (email/webhook/log), audit trail, `ObservabilityStore`, dashboard data API | 16.5 days | ✅ Done |
| 07 | [Phase 7 — Visual Designer & Runtime](./07-Phase7-Designer-Runtime.md) | **Separate UI project** (`TheTechIdea.Beep.Pipelines.Designer`). WinForms canvas designer, drag-drop plugin palette, auto-gen property panel, field mapping grid, live run monitor, schedule calendar, alert dashboard, lineage viewer | 38 days | ✅ Done |

---

## Architecture Layers

```
┌──────────────────────────────────────────────────────────────────┐
│  Layer 7 — Visual Designer (Phase 7) — **SEPARATE PROJECT**        │
│  TheTechIdea.Beep.Pipelines.Designer solution                        │
│  Depends on Phases 1–6 via NuGet only                               │
├──────────────────────────────────────────────────────────────────┤
│  Layer 6 — Observability (Phase 6)                               │
│  ObservabilityStore · MetricsEngine · AlertingEngine             │
│  DataLineageStore · AuditTrail · DashboardApi                    │
├──────────────────────────────────────────────────────────────────┤
│  Layer 5 — Scheduling (Phase 5)                                  │
│  SchedulerHost · PipelineRunQueue · DependencyGraph              │
│  CronScheduler · FileWatchScheduler · WebhookScheduler           │
├──────────────────────────────────────────────────────────────────┤
│  Layer 4 — Transform & Data Quality (Phase 4)                    │
│  9 Transformer Plugins · 6 Validator Plugins                     │
│  RoslynExpressionEvaluator · DQReport                            │
├──────────────────────────────────────────────────────────────────┤
│  Layer 3 — Workflow Orchestration (Phase 3)                      │
│  WorkFlowEngine · WorkFlowDefinition · WorkFlowStorage           │
│  Conditional Branching · Human Approval · Sub-Workflows          │
├──────────────────────────────────────────────────────────────────┤
│  Layer 2 — ETL Engine Core (Phase 2)                             │
│  PipelineEngine · DataSourcePlugin · DataSinkPlugin              │
│  RetryPolicy · CheckpointManager · PipelineManager               │
├──────────────────────────────────────────────────────────────────┤
│  Layer 1 — Plugin Architecture (Phase 1)                         │
│  IPipelineSource · IPipelineSink · IPipelineTransformer          │
│  PipelineRecord · PipelineDefinition · PipelinePluginRegistry    │
├──────────────────────────────────────────────────────────────────┤
│  Layer 0 — BeepDM Core (existing)                                │
│  IDMEEditor · IDataSource · AssemblyHandler · ConfigEditor       │
└──────────────────────────────────────────────────────────────────┘
```

---

## Key Namespace Map

| Namespace | Purpose |
|-----------|---------|
| `TheTechIdea.Beep.Pipelines` | Top-level: common interfaces, models, registry |
| `TheTechIdea.Beep.Pipelines.Engine` | `PipelineEngine`, `PipelineManager`, checkpoint, retry |
| `TheTechIdea.Beep.Pipelines.Workflow` | `WorkFlowEngine`, `WorkFlowDefinition`, templates |
| `TheTechIdea.Beep.Pipelines.Transforms` | All transformer + validator plugins |
| `TheTechIdea.Beep.Pipelines.Scheduling` | `SchedulerHost`, scheduler plugins, run queue |
| `TheTechIdea.Beep.Pipelines.Observability` | Run logs, metrics, alerting, lineage, audit |
| `TheTechIdea.Beep.Pipelines.Designer` | **Separate solution** — WinForms designer and monitor controls |

---

## Effort Summary

| Phase | Days | Priority |
|-------|------|----------|
| 1 — Plugin Architecture | 6 | P0 — must do first |
| 2 — ETL Engine Core | 14 | P0 — core value |
| 3 — Workflow Framework | 10 | P0 — pairs with Phase 2 |
| 4 — Transform & Data Quality | 18 | P1 — high value |
| 5 — Scheduling & Orchestration | 12 | P1 — needed for automation |
| 6 — Monitoring & Observability | 16.5 | P1 — needed for ops |
| 7 — Visual Designer & Runtime | 38 | P2 — high impact, high effort, **separate project** |
| **Total (BeepDM engine)** | **76.5 days** | — |
| **Total (incl. Designer)** | **114.5 days** | — |

> Phases 1–3 form the **Minimum Viable Rewrite** (~30 dev-days) that replaces existing ETL and delivers backward compat.  
> Phases 4–6 deliver enterprise-grade quality and operability.  
> Phase 7 is the showpiece UI layer — implemented in a **separate solution** (`TheTechIdea.Beep.Pipelines.Designer`) that only references Phases 1–6 as NuGet packages. No UI dependency enters `BeepDM`.

---

## Backward Compatibility

The existing ETL classes are **preserved** and wrapped:

| Old Class | New Equivalent | Compat Bridge |
|-----------|---------------|---------------|
| `ETLScriptHDR` | `PipelineDefinition` | `PipelineDefinition.FromLegacyScript(hdr)` |
| `ETLScriptDet` | `PipelineStepDef` | `PipelineStepDef.FromLegacyScriptDet(det)` |
| `ETLScriptManager` | `PipelineManager` | `PipelineManager.ImportFromLegacyScriptAsync()` |
| `ETLEditor.CopyEntityData()` | `PipelineEngine.RunAsync()` | `ETLEditor` kept as convenience shim |
| `IWorkFlow` | `WorkFlowDefinition` | `WorkFlowMigration.FromLegacy(wf)` |
| `WorkFlowStep` | `WorkFlowStepDef` | Extended (not replaced) |
| `WorkFlowAction` | `StepActionKind` enum | `WorkFlowMigration` converts actions |

---

## Design Principles

1. **Plugin-First** — every component discovered at runtime, zero hard-coded registration
2. **Streaming** — data flows as `IAsyncEnumerable<PipelineRecord>`, never buffered fully in memory
3. **Workflow IS ETL** — ETL jobs are `WorkFlowDefinition`s with `ETLPipeline` step kind
4. **Checkpointing** — pipelines resume from last committed batch after failure
5. **Column-Level Lineage** — every transform emits a `DataLineageRecord`
6. **Observability by Default** — every run is logged, every alert evaluated; not optional
7. **No Vendor Lock-in** — all notifiers, schedulers, and stores are swappable plugins
8. **Backward Compat** — no existing BeepDM ETL integration breaks

---

## Clean Code Standards (all phases)

Full rules in [Phase 1 §3](./01-Phase1-Plugin-Architecture.md). Summary:

| Rule | Requirement |
|------|-------------|
| **Folder = Namespace** | Every folder maps 1:1 to its sub-namespace; no exceptions |
| **One class per file** | Each class, interface, record, enum in its own `.cs` file |
| **Partial classes** | Any class >250 lines or with mixed concerns split into `Class.Responsibility.cs` partials |
| **SOLID** | S: one responsibility per plugin; O: extend via new class; D: engine depends on interfaces |
| **No magic strings** | All config keys, plugin IDs, defaults in `PipelineConstants` static class |
| **Async all the way** | Every I/O method async, accepts `CancellationToken`, uses `ConfigureAwait(false)` |
| **Immutable models** | `PipelineRecord`, `PipelineField` — `init`-only setters, clone not mutate |
| **Error reporting** | Return `IErrorsInfo` / populate `PipelineRunResult`; never throw from plugin internals |
| **UI separation** | `TheTechIdea.Beep.Pipelines.*` has zero WinForms/WPF references; Phase 7 is a separate solution |
