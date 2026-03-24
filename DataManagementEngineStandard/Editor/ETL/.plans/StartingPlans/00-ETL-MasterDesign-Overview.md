# BeepDM ETL & Workflow Framework — Master Design Document

**Version:** 2.0  
**Date:** 2026-03-13  
**Status:** Design / Planning  
**Owner:** The-Tech-Idea

---

## 1. Executive Summary

This document defines the architecture, design decisions, and phased implementation plan for an enterprise-grade **ETL (Extract, Transform, Load) and Workflow Framework** built on top of BeepDM. The goal is to evolve the current `ETLEditor` / `ETLScriptManager` partial implementation into a full-featured, extensible, plugin-driven ETL platform comparable to industry leaders such as Apache NiFi, Talend Open Studio, Azure Data Factory, and SSIS — while remaining lightweight, embeddable, and .NET-native.

---

## 2. Goals

| Goal | Description |
|------|-------------|
| **Enterprise-Ready** | Support production ETL workloads: large volumes, parallel execution, fault tolerance, retry, audit trail |
| **Plugin Architecture** | Every component (source connector, transformer, sink, loader, validator) is a discoverable plugin via `[AddinAttribute]` and `AssemblyHandler` |
| **Workflow-First** | ETL runs ARE workflows — each ETL job is a `WorkFlow` composed of typed `WorkFlowStep`s, connected by rules |
| **Visual Design** | Every workflow/pipeline can be designed visually (graph-based) and serialised to JSON |
| **Observable** | First-class support for structured run logs, metrics, lineage tracking, and alerting |
| **Testable** | Every step, transformer, and rule can be unit-tested in isolation |
| **Backward-Compatible** | Existing `ETLScriptHDR` / `ETLScriptDet` models are preserved and extended, not broken |

---

## 3. Comparison with Industry Systems

| Feature | SSIS | Talend | Apache NiFi | **BeepDM ETL v2** |
|---------|------|--------|-------------|-------------------|
| Visual Designer | ✅ | ✅ | ✅ | ✅ (Phase 7) |
| Plugin Sources/Sinks | Limited | ✅ | ✅ | ✅ (AddinAttribute) |
| Typed Transformers | ✅ | ✅ | ✅ | ✅ (Phase 4) |
| Workflow Orchestration | ✅ | ✅ | ✅ | ✅ (Phase 3) |
| Data Quality / DQ Rules | ✅ | ✅ | ✅ | ✅ (Phase 4) |
| Scheduling | ✅ | ✅ | ✅ | ✅ (Phase 5) |
| Lineage & Audit | Partial | ✅ | ✅ | ✅ (Phase 6) |
| Real-time / Streaming | ❌ | Partial | ✅ | Partial (Phase 6) |
| .NET Native / Embeddable | ✅ | ❌ | ❌ | ✅ |

---

## 4. High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        BeepDM ETL Platform v2                       │
│                                                                     │
│  ┌──────────────────────┐    ┌──────────────────────────────────┐  │
│  │  Visual Designer UI  │    │   REST / CLI / Programmatic API  │  │
│  │   (Phase 7)          │    │   (IDMEEditor.ETL / workflow)    │  │
│  └──────────────────────┘    └──────────────────────────────────┘  │
│               │                             │                       │
│  ┌────────────▼─────────────────────────────▼──────────────────┐   │
│  │              WorkFlow Orchestration Engine (Phase 3)         │   │
│  │  WorkFlow → WorkFlowStep[] → WorkFlowAction[] → Rules[]      │   │
│  │  Scheduler (Phase 5) ─────────────────────────────────────   │   │
│  └──────────────────────────────┬──────────────────────────────┘   │
│                                 │                                   │
│  ┌──────────────────────────────▼──────────────────────────────┐   │
│  │                    ETL Pipeline Engine (Phase 2)             │   │
│  │                                                             │   │
│  │  Source Plugin   Transformer Chain    Sink Plugin           │   │
│  │  (IETLSource)  ──► (IETLTransformer)─► (IETLSink)          │   │
│  │                   ┌───────────────┐                         │   │
│  │                   │  DQ / Rules   │  (Phase 4)              │   │
│  │                   └───────────────┘                         │   │
│  └──────────────────────────────┬──────────────────────────────┘   │
│                                 │                                   │
│  ┌──────────────────────────────▼──────────────────────────────┐   │
│  │                Plugin Registry (Phase 1)                     │   │
│  │  AssemblyHandler + AddinAttribute discovery                  │   │
│  │  IETLSource  IETLTransformer  IETLSink  IETLScheduler        │   │
│  └──────────────────────────────┬──────────────────────────────┘   │
│                                 │                                   │
│  ┌──────────────────────────────▼──────────────────────────────┐   │
│  │           Existing BeepDM Infrastructure                     │   │
│  │   IDMEEditor  IDataSource  IConfigEditor  AssemblyHandler    │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                  Observability Layer (Phase 6)               │   │
│  │  Run Logs  Data Lineage  Metrics  Alerts  Audit Trail        │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 5. Core Design Principles

### 5.1 Everything is a Plugin
Every source connector, transformer, sink, validator, scheduler, and notifier is a .NET class decorated with `[AddinAttribute]` and discovered at runtime by `AssemblyHandler`. No hard-coded registrations. Drop a DLL into the plugins folder and it becomes available.

### 5.2 Workflow = ETL Pipeline
An ETL job IS a `WorkFlow`. There is no separate "ETL job" concept. A `WorkFlow` is composed of:
- `WorkFlowStep` — one stage in the pipeline (e.g., Extract, Validate, Transform, Load)
- `WorkFlowAction` — a single operation within a step (e.g., read table, map fields, write table)
- `WorkFlowRule` — a condition that gates or routes execution

### 5.3 Typed Data Records
Data flows between steps as typed `IDataRecord` objects with schema attached. No raw `DataTable` or `object[]` — this enables statically typed transformations and proper lineage tracking.

### 5.4 Streaming-Capable
Connectors expose `IAsyncEnumerable<IDataRecord>` so large datasets never load fully into memory. Sinks consume the same stream. Buffer/batch sizes are configurable per step.

### 5.5 Idempotency & Checkpoints
Long-running pipelines write checkpoint state so they can resume after failure without re-processing already completed steps (similar to NiFi FlowFiles or SSIS checkpoints).

---

## 6. Implementation Phases

| Phase | Document | Focus | Priority |
|-------|----------|-------|----------|
| **1** | `01-Phase1-Plugin-Architecture.md` | Plugin registry, interfaces, discovery | 🔴 Critical |
| **2** | `02-Phase2-ETL-Engine-Core.md` | Pipeline engine, source/sink/transformer base | 🔴 Critical |
| **3** | `03-Phase3-Workflow-Framework.md` | WorkFlow engine enhancement, step execution, routing | 🔴 Critical |
| **4** | `04-Phase4-Transform-Quality.md` | Transformer library, DQ rules, data cleansing | 🟠 High |
| **5** | `05-Phase5-Scheduling-Orchestration.md` | Cron/event triggers, dependency management | 🟠 High |
| **6** | `06-Phase6-Monitoring-Observability.md` | Logs, lineage, metrics, alerting | 🟡 Medium |
| **7** | `07-Phase7-Designer-Runtime.md` | Visual designer (WinForms/WPF), runtime canvas | 🟡 Medium |

---

## 7. Key Model Evolution

### Current Models (preserved, extended)
```
ETLScriptHDR    → becomes  PipelineDefinition   (superset, backward-compatible)
ETLScriptDet    → becomes  PipelineStepDef      (typed step definition)
WorkFlow        → enhanced with run engine, serialization, visual layout
WorkFlowStep    → enhanced with typed I/O, retry policy, timeout
WorkFlowAction  → enhanced with plugin binding, parameter schema
```

### New Models
```
IDataRecord         — typed row flowing through pipeline
IDataSchema         — schema descriptor for IDataRecord stream
IETLSource          — plugin interface: produces IAsyncEnumerable<IDataRecord>
IETLTransformer     — plugin interface: transforms IDataRecord streams
IETLSink            — plugin interface: consumes IDataRecord stream
IETLScheduler       — plugin interface: triggers pipeline runs
PipelineRunContext  — runtime state passed through all steps
PipelineRunLog      — structured audit record per run
DataLineageRecord   — tracks column-level data lineage
CheckpointState     — resumable execution state
```

---

## 8. Technology Stack

| Component | Technology |
|-----------|-----------|
| Language | C# 12 / .NET 8+ |
| Async | `IAsyncEnumerable<T>`, `Channel<T>`, `Task.WhenAll` |
| Serialization | System.Text.Json (pipeline definitions, run logs) |
| Plugin Discovery | Existing `AssemblyHandler` + `[AddinAttribute]` |
| DI | Autofac / MS.Extensions.DI (existing BeepDM pattern) |
| Storage | JSON files (existing `Scripts/` folder pattern), extensible to DB |
| Testing | xUnit + existing test projects |

---

## 9. Backward Compatibility Strategy

1. `ETLScriptHDR` / `ETLScriptDet` remain unchanged — new `PipelineDefinition` can be constructed from them.
2. `ETLScriptManager.ExecuteScriptAsync()` stays working — internally delegates to new Pipeline Engine.
3. `ETLEditor.CopyEntityData()` / `CreateScriptHeader()` stay as convenience wrappers.
4. All new interfaces are in new namespaces (`TheTechIdea.Beep.ETL.v2.*`) to avoid conflicts.

---

## 10. Success Criteria

- [ ] Any data source registered with BeepDM can act as an ETL source or sink with zero extra code
- [ ] A pipeline with 3 steps (Extract → Transform → Load) can be defined, saved, and run in < 50 lines of code
- [ ] A new custom transformer deployed as a DLL is auto-discovered at startup
- [ ] 1M-row copy between two data sources completes without OOM issues (streaming)
- [ ] Every row-level error is logged with source row identifier, step name, and timestamp
- [ ] A failed run at step 3 of 5 can be resumed from step 3 without re-executing steps 1–2
- [ ] Workflow can be scheduled via cron expression or triggered by file/event
- [ ] Full data lineage (source column → target column) can be queried after a run

---

## 11. Related Files

- [ETLEditor.cs](../ETLEditor.cs) — current implementation
- [ETLScriptManager.cs](../ETLScriptManager.cs) — current persistence
- [ETLScriptBuilder.cs](../ETLScriptBuilder.cs) — script generation helpers  
- [ETLValidator.cs](../ETLValidator.cs) — validation utilities
- [WorkFlow.cs](../../../../DataManagementModelsStandard/Workflow/WorkFlow.cs) — existing workflow model
- [IWorkFlowEditor.cs](../../../../DataManagementModelsStandard/Workflow/Interfaces/IWorkFlowEditor.cs) — existing workflow editor contract
