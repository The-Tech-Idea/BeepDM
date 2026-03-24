# Phase 3 — Workflow Framework Redesign

**Version:** 1.0  
**Date:** 2026-03-13  
**Status:** Design  
**Depends on:** Phase 1 (Plugin Architecture), Phase 2 (Pipeline Engine)

---

## 1. Objective

The existing `WorkFlow` / `WorkFlowStep` / `WorkFlowAction` / `WorkFlowRule` types are solid but lack a runtime engine. They carry state definitions but nothing executes them. This phase:

1. **Enhances the existing models** with execution metadata and serialization without breaking the `IWorkFlow` / `IWorkFlowStep` / `IWorkFlowAction` interfaces.
2. **Builds a `WorkFlowEngine`** that can execute any `WorkFlow` definition — including ETL pipelines, which become a special subtype of `WorkFlow`.
3. **Unifies ETL and general workflow**: an ETL copy job is `WorkFlow` where each step maps to a `PipelineDefinition` or a `PipelineStepDef`. Non-ETL workflows (approve-and-notify, data-quality-check-and-route, etc.) are also `WorkFlow`s.

---

## 2. Enhanced Model Overview

### Existing (kept, unchanged public shape)
```
IWorkFlow               → WorkFlow
IWorkFlowStep           → WorkFlowStep  
IWorkFlowAction         → WorkFlowAction
IWorkFlowRule           → WorkFlowRule
IWorkFlowEditor         → (replaced by WorkFlowEngine)
WorkFlow_Run_Result     → extended
Workflow_Step_Run_result→ extended
LoadDataLogResult       → kept as-is
```

### New additions (no breaking changes)
```
WorkFlowDefinition      → serializable wrapper around WorkFlow + connection graph
WorkFlowRunContext      → runtime state passed to all steps (mirrors PipelineRunContext)
WorkFlowRunResult       → structured run result (extends WorkFlow_Run_Result)
WorkFlowEngine          → replaces / supersedes IWorkFlowEditor
StepConnection          → directed edge between steps (enables branching, merging, loops)
RetryPolicy             → per-step retry config
StepExecutionRecord     → per-step audit entry
```

---

## 3. Unified Execution Model

```
WorkFlowDefinition
 ├── Id, Name, Description, Tags, Version
 ├── List<WorkFlowStepDef>       ← ordered steps with metadata
 ├── List<StepConnection>        ← edges in the step graph
 ├── List<WorkFlowParameter>     ← named pipeline parameters
 ├── WorkFlowTrigger             ← how this workflow is started
 └── RetryPolicy                 ← default retry for all steps

WorkFlowStepDef
 ├── (extends WorkFlowStep)
 ├── StepActionKind              ← ETLPipeline | Script | Notification | Approval | Wait | SubWorkflow
 ├── PipelineId?                 ← if ETLPipeline: references PipelineDefinition
 ├── ScriptBody?                 ← if Script: inline C# or expression
 ├── RetryPolicy                 ← overrides WorkFlowDefinition.RetryPolicy
 ├── TimeoutSeconds
 └── OnFailure                   ← Fail | Skip | Route(stepId)

StepConnection
 ├── FromStepId
 ├── ToStepId
 └── Condition?                  ← evaluated on step output (e.g. "records > 0")
                                    true = follow this edge
```

---

## 4. Step Action Kinds

| Kind | Description | Example |
|------|-------------|---------|
| `ETLPipeline` | Executes a `PipelineDefinition` via `PipelineEngine` | Copy Orders from SQLite to SQL Server |
| `Script` | Executes a C# snippet via Roslyn scripting | `ctx.State["total"] = ctx.Records.Count` |
| `Notification` | Sends email / Slack / Teams alert | "Pipeline complete: {total} rows" |
| `Approval` | Pause run until human approves (persisted) | Review flagged records before load |
| `Wait` | Pause N seconds or until condition | Throttle between steps |
| `SubWorkflow` | Execute a nested `WorkFlowDefinition` | Re-use common validation workflow |
| `DataQuality` | Run a DQ rule set on an in-memory result | Check all required fields before load |
| `SchemaSync` | Sync entity schemas between two data sources | Migrate schema before data copy |
| `Merge` | Join two incoming step results | Combine Orders + Shippings into single stream |
| `Split` | Fan-out one result to multiple next steps | Route EU and US data to different sinks |

---

## 5. WorkFlowEngine — Full Design

```csharp
namespace TheTechIdea.Beep.Workflows.Engine
{
    /// <summary>
    /// Executes WorkFlowDefinitions.
    /// Supports sequential, parallel, branching, looping, and sub-workflow patterns.
    /// Integrates with PipelineEngine for ETL steps.
    /// 
    /// Usage:
    ///     var engine = new WorkFlowEngine(editor);
    ///     var result = await engine.RunAsync(definition, progress, token);
    /// </summary>
    public class WorkFlowEngine
    {
        private readonly IDMEEditor       _editor;
        private readonly PipelineEngine   _pipelineEngine;
        private readonly WorkFlowStorage  _storage;

        public WorkFlowEngine(IDMEEditor editor)
        {
            _editor         = editor;
            _pipelineEngine = new PipelineEngine(editor);
            _storage        = new WorkFlowStorage(editor);
        }

        // ── Public API ──────────────────────────────────────────────────────

        public Task<WorkFlowRunResult> RunAsync(
            WorkFlowDefinition definition,
            IProgress<PassedArgs>? progress,
            CancellationToken token,
            IReadOnlyDictionary<string, object>? overrideParams = null);

        /// <summary>
        /// Resume a paused or failed workflow run from last committed step.
        /// </summary>
        public Task<WorkFlowRunResult> ResumeAsync(
            string workflowRunId,
            IProgress<PassedArgs>? progress,
            CancellationToken token);

        /// <summary>
        /// An Approval step pauses execution. This resumes it.
        /// </summary>
        public Task ApproveAsync(string workflowRunId, string stepId, string approverNote);
        public Task RejectAsync(string workflowRunId, string stepId, string rejectionNote);

        // ── Internal execution ───────────────────────────────────────────────

        private async Task<WorkFlowRunResult> ExecuteAsync(
            WorkFlowDefinition def,
            WorkFlowRunContext ctx)
        {
            // Topological sort of steps via StepConnection graph
            // Build execution order respecting dependencies
            // Execute step-by-step (or parallel where edges allow)
            //
            // For each step:
            //   1. Check entry condition (StepConnection.Condition)
            //   2. Execute action (dispatch to correct handler by StepActionKind)
            //   3. Capture StepExecutionRecord
            //   4. Evaluate OnFailure policy
            //   5. Advance to next steps
        }

        private async Task<StepExecutionRecord> ExecuteStepAsync(
            WorkFlowStepDef step,
            WorkFlowRunContext ctx)
        {
            return step.Kind switch
            {
                StepActionKind.ETLPipeline  => await RunEtlStepAsync(step, ctx),
                StepActionKind.Script       => await RunScriptStepAsync(step, ctx),
                StepActionKind.Notification => await RunNotificationStepAsync(step, ctx),
                StepActionKind.Approval     => await RunApprovalStepAsync(step, ctx),
                StepActionKind.Wait         => await RunWaitStepAsync(step, ctx),
                StepActionKind.SubWorkflow  => await RunSubWorkflowStepAsync(step, ctx),
                StepActionKind.SchemaSync   => await RunSchemaSyncStepAsync(step, ctx),
                StepActionKind.DataQuality  => await RunDQStepAsync(step, ctx),
                _                           => throw new NotSupportedException($"Unknown step kind: {step.Kind}")
            };
        }

        private async Task<StepExecutionRecord> RunEtlStepAsync(WorkFlowStepDef step, WorkFlowRunContext ctx)
        {
            // Load PipelineDefinition by step.PipelineId
            // Pass ctx.Parameters down as override params
            // Run via _pipelineEngine
            // Map PipelineRunResult → StepExecutionRecord
        }
    }
}
```

---

## 6. Step Connection Graph

Enables complex topologies beyond simple sequential:

```
Parallel Fan-Out:
    Extract ──► Transform-A ──► Load-A
              └─► Transform-B ──► Load-B

Conditional Branch:
    Extract ──► Validate ──[pass]──► Load
                         └─[fail]──► Quarantine

Loop (re-process failed):
    Load ──[retry condition]──► Transform

Merge:
    Source-A ──►
               Merge ──► Load
    Source-B ──►
```

```csharp
public class StepConnection
{
    public string Id         { get; set; } = Guid.NewGuid().ToString();
    public string FromStepId { get; set; } = string.Empty;
    public string ToStepId   { get; set; } = string.Empty;

    /// <summary>
    /// Expression evaluated against the previous step's StepExecutionRecord.
    /// Null = unconditional (always follow this edge).
    /// E.g. "result.RecordsProcessed > 0", "result.Success == true"
    /// </summary>
    public string? Condition  { get; set; }

    /// <summary>Priority when multiple connections leave the same step (lower = higher priority).</summary>
    public int Priority { get; set; } = 0;
}
```

---

## 7. WorkFlowStepDef — Enhanced Step Model

```csharp
public class WorkFlowStepDef : WorkFlowStep   // extends existing WorkFlowStep
{
    public StepActionKind Kind          { get; set; } = StepActionKind.ETLPipeline;

    // ── ETL step ─────────────────────────────────────────────────────────
    public string? PipelineId           { get; set; }  // reference to PipelineDefinition.Id
    public Dictionary<string, object> PipelineParams   { get; set; } = new();

    // ── Script step ───────────────────────────────────────────────────────
    public string? ScriptBody           { get; set; }  // C# snippet
    public string  ScriptLanguage       { get; set; } = "csharp";

    // ── Notification step ─────────────────────────────────────────────────
    public string? NotifierPluginId     { get; set; }
    public string? NotificationTemplate { get; set; }

    // ── Approval step ─────────────────────────────────────────────────────
    public List<string> Approvers       { get; set; } = new(); // user IDs or email
    public int ApprovalTimeoutHours     { get; set; } = 24;

    // ── Sub-workflow step ─────────────────────────────────────────────────
    public string? SubWorkflowId        { get; set; }

    // ── Execution policy ──────────────────────────────────────────────────
    public RetryPolicy RetryPolicy      { get; set; } = new();
    public int TimeoutSeconds           { get; set; } = 0;    // 0 = no timeout
    public OnFailureBehavior OnFailure  { get; set; } = OnFailureBehavior.Fail;
    public string? OnFailureRouteToStepId { get; set; }       // used with Route behaviour

    // ── Visual position (Phase 7) ─────────────────────────────────────────
    public float CanvasX { get; set; }
    public float CanvasY { get; set; }
}

public enum OnFailureBehavior { Fail, Skip, Route, Retry }
```

---

## 8. WorkFlowRunContext

```csharp
public class WorkFlowRunContext
{
    public string      RunId              { get; } = Guid.NewGuid().ToString();
    public string      WorkFlowId         { get; init; } = string.Empty;
    public string      WorkFlowName       { get; init; } = string.Empty;
    public DateTime    StartedAtUtc       { get; } = DateTime.UtcNow;
    public IDMEEditor  DMEEditor          { get; init; } = null!;
    public IProgress<PassedArgs>? Progress { get; init; }
    public CancellationToken Token        { get; init; }

    /// <summary>Resolved parameters available to all steps.</summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; }
        = new Dictionary<string, object>();

    /// <summary>Shared state bag. Steps communicate results through here.</summary>
    public Dictionary<string, object> State { get; } = new();

    /// <summary>Completed step results, keyed by step ID.</summary>
    public Dictionary<string, StepExecutionRecord> StepResults { get; } = new();

    // ── Telemetry ──────────────────────────────────────────────────────────
    public int StepsTotal     { get; set; }
    public int StepsCompleted { get; set; }
    public int StepsFailed    { get; set; }
    public int StepsSkipped   { get; set; }
    public long TotalRecordsProcessed { get; set; }

    /// <summary>Current step that is executing.</summary>
    public string? CurrentStepId { get; set; }

    public void ReportProgress(string message, int pct = -1) =>
        Progress?.Report(new PassedArgs { Messege = message, ParameterInt1 = pct });
}
```

---

## 9. StepExecutionRecord

```csharp
public class StepExecutionRecord
{
    public string   StepId          { get; set; } = Guid.NewGuid().ToString();
    public string   StepName        { get; set; } = string.Empty;
    public StepActionKind Kind      { get; set; }
    public bool     Success         { get; set; }
    public string?  ErrorMessage    { get; set; }
    public DateTime StartedAtUtc    { get; set; }
    public DateTime FinishedAtUtc   { get; set; }
    public TimeSpan Duration        => FinishedAtUtc - StartedAtUtc;
    public long     RecordsRead     { get; set; }
    public long     RecordsWritten  { get; set; }
    public long     RecordsRejected { get; set; }
    public int      RetryCount      { get; set; }

    /// <summary>Step-specific output data (pipeline run result, script output, etc.)</summary>
    public Dictionary<string, object> Output { get; set; } = new();

    /// <summary>Row-level log entries from ETL/data steps.</summary>
    public List<LoadDataLogResult> DataLogs { get; set; } = new();
}
```

---

## 10. WorkFlowStorage — Persistence

```csharp
namespace TheTechIdea.Beep.Workflows.Engine
{
    /// <summary>
    /// Persists WorkFlowDefinitions and run results.
    /// Storage: ExePath/Workflows/{id}.workflow.json
    ///          ExePath/WorkflowRuns/{runId}.run.json
    /// </summary>
    public class WorkFlowStorage
    {
        // CRUD for WorkFlowDefinitions
        Task<IErrorsInfo> SaveDefinitionAsync(WorkFlowDefinition def);
        Task<WorkFlowDefinition?> LoadDefinitionAsync(string id);
        Task<IReadOnlyList<WorkFlowDefinition>> LoadAllDefinitionsAsync();
        Task<IErrorsInfo> DeleteDefinitionAsync(string id);

        // Run result persistence
        Task SaveRunResultAsync(WorkFlowRunResult result);
        Task<WorkFlowRunResult?> LoadRunResultAsync(string runId);
        Task<IReadOnlyList<WorkFlowRunResult>> GetRunHistoryAsync(string workflowId, int limit = 50);

        // Approval state persistence (for Approval steps)
        Task SaveApprovalStateAsync(string runId, string stepId, ApprovalState state);
        Task<ApprovalState?> LoadApprovalStateAsync(string runId, string stepId);
    }
}
```

---

## 11. WorkFlow Templates (built-in definitions)

| Template Name | Steps | Description |
|--------------|-------|-------------|
| `FullCopy` | Extract → Validate → Load | Copy all entities from source to sink |
| `IncrementalSync` | Extract(delta) → Merge → Load | Sync deltas using timestamp/id watermark |
| `SchemaAndData` | SchemaSync → Extract → Load | Ensure schema, then copy data |
| `ValidateAndQuarantine` | Extract → DQ → Route → Load/Quarantine | DQ-first load pattern |
| `ETLWithNotification` | Full pipeline + Notification | Runs ETL then alerts on completion |
| `MultiSourceMerge` | Extract A + Extract B (parallel) → Merge → Load | Two sources into one target |

---

## 12. Backward Compatibility Bridge

```csharp
/// <summary>
/// Converts existing IWorkFlow / WorkFlow instances to WorkFlowDefinition.
/// Allows all previously persisted workflows to be executed by WorkFlowEngine.
/// </summary>
public static class WorkFlowMigration
{
    public static WorkFlowDefinition FromLegacy(IWorkFlow wf)
    {
        var def = new WorkFlowDefinition
        {
            Id   = wf.GuidID ?? Guid.NewGuid().ToString(),
            Name = wf.DataWorkFlowName,
            Description = wf.Description
        };

        int seq = 0;
        WorkFlowStepDef? prev = null;
        foreach (var step in wf.Datasteps ?? Enumerable.Empty<IWorkFlowStep>())
        {
            var stepDef = new WorkFlowStepDef
            {
                Id       = step.ID,
                Name     = step.Name,
                Sequence = seq++,
                Kind     = StepActionKind.Script,  // default; user refines
                Code     = step.Code
            };
            def.Steps.Add(stepDef);
            if (prev != null)
                def.Connections.Add(new StepConnection { FromStepId = prev.Id, ToStepId = stepDef.Id });
            prev = stepDef;
        }

        return def;
    }
}
```

---

## 13. Deliverables (Implementation Checklist)

- [ ] `WorkFlowDefinition.cs` — new serializable definition model
- [ ] `WorkFlowStepDef.cs` — extends `WorkFlowStep` with execution metadata
- [ ] `StepConnection.cs`
- [ ] `WorkFlowRunContext.cs`
- [ ] `StepExecutionRecord.cs`
- [ ] `WorkFlowRunResult.cs` — extends `WorkFlow_Run_Result`
- [ ] `WorkFlowEngine.cs` — full runner with topological sort + all step kinds
- [ ] `WorkFlowStorage.cs` — JSON persistence
- [ ] `WorkFlowMigration.cs` — legacy adapter
- [ ] Built-in templates (JSON definitions in `Resources/WorkflowTemplates/`)
- [ ] Integration tests: sequential, parallel fan-out, conditional branch
- [ ] Unit tests: `WorkFlowMigration.FromLegacy()`, `StepConnection` graph traversal

---

## 14. Estimated Effort

| Task | Days |
|------|------|
| New models (5 files) | 1.5 |
| WorkFlowEngine core | 4 |
| WorkFlowStorage | 1 |
| WorkFlowMigration | 0.5 |
| Built-in templates (5 JSON) | 1 |
| Integration tests | 2 |
| **Total** | **10 days** |
