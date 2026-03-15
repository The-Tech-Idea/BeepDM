# Phase 5 — Scheduling & Orchestration

**Version:** 1.0  
**Date:** 2026-03-13  
**Status:** Design  
**Depends on:** Phase 2 (Engine), Phase 3 (Workflow Framework)

---

## 1. Objective

Make pipelines and workflows **self-scheduling**. Instead of the caller manually calling `RunAsync`, a `PipelineScheduler` monitors trigger conditions and fires runs automatically. Scheduling is itself a plugin type (`IPipelineScheduler`) so new trigger mechanisms can be added by dropping a DLL.

Advanced orchestration features covered here:
- Dependency chains (run B only when A succeeds)
- Concurrency controls (max N simultaneous runs of the same pipeline)
- Priority queuing
- Rate limiting & throttling
- Distributed locks (prevent duplicate runs in multi-instance deployments)

---

## 2. Scheduler Architecture

```
SchedulerHost (singleton, long-lived)
 ├── SchedulerRegistry               ← holds all active IPipelineScheduler instances
 ├── PipelineQueue                   ← bounded in-memory queue of pending runs
 ├── PipelineRunDispatcher           ← dequeues and runs pipelines via PipelineEngine
 ├── ConcurrencyGate                 ← limits parallel runs per pipeline
 ├── DependencyGraph                 ← enforces run-B-after-A ordering
 └── ScheduleStorage                 ← persists schedule definitions
```

---

## 3. IPipelineScheduler Interface

Already defined in Phase 1. Summary:

```csharp
public interface IPipelineScheduler : IPipelinePlugin
{
    event EventHandler<PipelineTriggerArgs> Triggered;
    Task StartAsync(CancellationToken token);
    Task StopAsync();
}

public class PipelineTriggerArgs : EventArgs
{
    public string PipelineId       { get; init; } = string.Empty;
    public string TriggerSource    { get; init; } = string.Empty;   // "cron", "file", "event"
    public string TriggerDetail    { get; init; } = string.Empty;   // expression or filename
    public IReadOnlyDictionary<string, object>? OverrideParams { get; init; }
    public DateTime TriggeredAtUtc { get; } = DateTime.UtcNow;
    public string CorrelationId    { get; } = Guid.NewGuid().ToString();
}
```

---

## 4. Built-in Scheduler Plugins

### 4.1 CronScheduler
```
PluginId: "beep.schedule.cron"

Config:
  CronExpression: "0 2 * * *"      // CRON: seconds optional (6-part for sub-minute)
  TimeZone:       "UTC"
  PipelineId:     "abc123"
  MaxMissedRuns:  1                 // if host was down, run at most 1 missed execution

CRON format (6-part with optional seconds):
  ┌─ sec (0-59)
  │ ┌── min (0-59)
  │ │ ┌─── hr  (0-23)
  │ │ │ ┌──── day-of-month (1-31)
  │ │ │ │ ┌───── month (1-12)
  │ │ │ │ │ ┌────── day-of-week (0-7, Sun=0|7)
  * * * * * *

Examples:
  "0 */6 * * *"   = every 6 hours
  "0 9 * * 1-5"   = weekdays at 09:00
  "*/30 * * * *"  = every 30 minutes
```

### 4.2 FileWatchScheduler
```
PluginId: "beep.schedule.filewatch"

Config:
  WatchPath:    "C:/Import/Drop"
  FilePattern:  "*.csv"
  Recursive:    false
  TriggerOn:    Created | Changed | Created|Changed
  Debounce_ms:  2000               // wait 2 sec after last event before firing
  StabilityMs:  500                // ensure file is not still being written
  PassFilePath: true               // expose file path as param "__trigger_file"

Special params injected into run:
  __trigger_file    = full path to triggering file
  __trigger_filename= filename only
```

### 4.3 EventBusScheduler
```
PluginId: "beep.schedule.eventbus"

Config:
  EventTopic:   "order.completed"
  Filter:       "payload.country == 'US'"    // optional filter expression

Subscribes to BeepDM's internal EventBus.
When a matching event is published (by any code), triggers the pipeline.
Event payload fields are injected as run params.
```

### 4.4 ManualScheduler
```
PluginId: "beep.schedule.manual"

No automatic trigger. Pipeline only runs when explicitly called:
  await schedulerHost.TriggerManualAsync("pipelineId", overrideParams)
```

### 4.5 WebhookScheduler
```
PluginId: "beep.schedule.webhook"

Exposes a lightweight HTTP endpoint (ASP.NET Core minimal API or HttpListener).
POST to /beep/trigger/{pipelineId} fires the pipeline.
Supports HMAC signature verification for security.
```

### 4.6 PipelineDependencyScheduler
```
PluginId: "beep.schedule.dependency"

Config:
  DependsOn: ["pipelineId-A", "pipelineId-B"]
  Condition: "ALL_SUCCESS"   // ALL_SUCCESS | ANY_SUCCESS | ALL_COMPLETE

Watches the run history. When all dependencies complete with required status,
fires this pipeline within the same day window.
```

---

## 5. ScheduleDefinition Model

```csharp
public class ScheduleDefinition
{
    public string Id              { get; set; } = Guid.NewGuid().ToString();
    public string Name            { get; set; } = string.Empty;
    public string PipelineId      { get; set; } = string.Empty;       // or WorkFlowId
    public bool   IsWorkflow      { get; set; } = false;
    public string SchedulerPluginId { get; set; } = string.Empty;
    public Dictionary<string, object> SchedulerConfig { get; set; } = new();
    public bool   IsEnabled       { get; set; } = true;
    public int    Priority        { get; set; } = 5;                   // 1 (high) – 10 (low)
    public int    MaxConcurrentRuns { get; set; } = 1;
    public int    TimeoutSeconds  { get; set; } = 0;
    public RetryPolicy RetryPolicy { get; set; } = new();
    public List<string> DependsOn { get; set; } = new();               // other schedule IDs
    public DateTime? NextRunAt    { get; set; }                        // computed by scheduler
    public DateTime? LastRunAt    { get; set; }
    public string?   LastRunStatus { get; set; }
}
```

---

## 6. SchedulerHost

```csharp
namespace TheTechIdea.Beep.Pipelines.Scheduling
{
    /// <summary>
    /// Singleton service that manages all scheduler plugins, handles triggers,
    /// queues runs, and dispatches to PipelineEngine / WorkFlowEngine.
    /// Typically registered as a singleton in DI and started with the application.
    /// </summary>
    public class SchedulerHost : IAsyncDisposable
    {
        private readonly IDMEEditor         _editor;
        private readonly PipelineEngine     _pipelineEngine;
        private readonly WorkFlowEngine     _workflowEngine;
        private readonly PipelinePluginRegistry _registry;
        private readonly ScheduleStorage    _storage;
        private readonly PipelineRunQueue   _queue;
        private readonly ConcurrencyGate    _gate;
        private readonly DependencyGraph    _deps;

        // ── Public API ──────────────────────────────────────────────────────

        /// <summary>Load all schedules from storage and start all enabled schedulers.</summary>
        public Task StartAsync(CancellationToken token);

        /// <summary>Stop all schedulers gracefully, finish in-flight runs.</summary>
        public Task StopAsync();

        /// <summary>Reload schedules from storage (hot-reload, no restart needed).</summary>
        public Task ReloadAsync();

        /// <summary>Manually trigger a pipeline/workflow run immediately.</summary>
        public Task<string> TriggerManualAsync(
            string pipelineOrWorkflowId,
            IReadOnlyDictionary<string, object>? overrideParams = null);

        /// <summary>Get the status of all active runs.</summary>
        public IReadOnlyList<RunStatus> GetActiveRuns();

        /// <summary>Cancel a running pipeline/workflow by run ID.</summary>
        public Task CancelRunAsync(string runId);

        // ── Events ─────────────────────────────────────────────────────────
        public event EventHandler<RunStartedArgs>   RunStarted;
        public event EventHandler<RunCompletedArgs> RunCompleted;
        public event EventHandler<RunFailedArgs>    RunFailed;
    }
}
```

---

## 7. PipelineRunQueue

Implements priority queuing for pending runs:

```csharp
public class PipelineRunQueue
{
    // Priority min-heap backed by Channel<QueuedRun>
    // Dequeue order: Priority (1=highest) then TriggeredAt (FIFO within same priority)

    public Task EnqueueAsync(QueuedRun run, CancellationToken token);
    public Task<QueuedRun> DequeueAsync(CancellationToken token);
    public int Count { get; }
}

public class QueuedRun
{
    public string RunId             { get; } = Guid.NewGuid().ToString();
    public string PipelineId        { get; init; } = string.Empty;
    public bool   IsWorkflow        { get; init; }
    public int    Priority          { get; init; } = 5;
    public DateTime TriggeredAtUtc  { get; } = DateTime.UtcNow;
    public string TriggerSource     { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object>? OverrideParams { get; init; }
    public CancellationTokenSource Cts { get; } = new();
}
```

---

## 8. ConcurrencyGate

Prevents overwhelming a data source with too many simultaneous runs:

```csharp
public class ConcurrencyGate
{
    // Per-pipeline semaphores, keyed by pipeline ID
    // MaxConcurrentRuns from ScheduleDefinition

    public Task<IDisposable> AcquireAsync(string pipelineId, int maxConcurrency, CancellationToken token);
    // Returns IDisposable; when disposed, releases the slot
}
```

---

## 9. DependencyGraph

```csharp
public class DependencyGraph
{
    // Builds a DAG of schedule dependencies
    // On each run completion, checks if any downstream schedules are now unblocked
    // Enqueues unblocked schedules

    public void RegisterDependency(string scheduleId, IEnumerable<string> dependsOnIds);
    public void NotifyCompletion(string scheduleId, bool success, DateTime completedAt);
    public IReadOnlyList<string> GetUnblockedSchedules();  // to be enqueued
}
```

---

## 10. Schedule API Examples

### Define and register a nightly pipeline
```csharp
var schedule = new ScheduleDefinition
{
    Name          = "Nightly Orders Import",
    PipelineId    = "orders-etl-pipeline-id",
    SchedulerPluginId = "beep.schedule.cron",
    SchedulerConfig = new() { ["CronExpression"] = "0 2 * * *", ["TimeZone"] = "UTC" },
    Priority      = 3,
    MaxConcurrentRuns = 1,
    RetryPolicy   = new RetryPolicy { MaxRetries = 2, BaseDelayMs = 60000 }
};

await schedulerHost.Storage.SaveAsync(schedule);
await schedulerHost.ReloadAsync();
```

### File-triggered pipeline
```csharp
var schedule = new ScheduleDefinition
{
    Name       = "CSV Drop Import",
    PipelineId = "csv-import-pipeline-id",
    SchedulerPluginId = "beep.schedule.filewatch",
    SchedulerConfig = new()
    {
        ["WatchPath"]   = @"C:\Imports\Drop",
        ["FilePattern"] = "orders_*.csv",
        ["PassFilePath"] = true
    }
};
```

### Dependency chain
```csharp
// Run Quarantine Report ONLY after Validate step completes successfully
var schedule = new ScheduleDefinition
{
    Name       = "Quarantine Report",
    PipelineId = "quarantine-report-id",
    SchedulerPluginId = "beep.schedule.dependency",
    SchedulerConfig = new()
    {
        ["DependsOn"] = new[] { "validate-orders-schedule-id" },
        ["Condition"] = "ALL_SUCCESS"
    }
};
```

---

## 11. Rate Limiting & Throttling

Configurable per schedule:

```csharp
public class RateLimitPolicy
{
    /// <summary>Maximum runs per window.</summary>
    public int MaxRuns         { get; set; } = 0;  // 0 = unlimited

    /// <summary>Window size in seconds.</summary>
    public int WindowSeconds   { get; set; } = 3600;

    /// <summary>Minimum gap between runs in seconds.</summary>
    public int MinGapSeconds   { get; set; } = 0;
}
```

---

## 12. Deliverables (Implementation Checklist)

- [ ] `ScheduleDefinition.cs` model
- [ ] `PipelineTriggerArgs.cs`
- [ ] `SchedulerHost.cs` (core service)
- [ ] `PipelineRunQueue.cs` (priority queue)
- [ ] `ConcurrencyGate.cs`
- [ ] `DependencyGraph.cs`
- [ ] `ScheduleStorage.cs` (JSON persistence)
- [ ] `CronScheduler.cs` plugin (NCrontab library)
- [ ] `FileWatchScheduler.cs` plugin
- [ ] `EventBusScheduler.cs` plugin
- [ ] `ManualScheduler.cs` plugin
- [ ] `PipelineDependencyScheduler.cs` plugin
- [ ] `RateLimitPolicy.cs`
- [ ] Integration tests: cron trigger, file trigger, dependency chain
- [ ] Register `SchedulerHost` in `IDMEEditor` DI

---

## 13. Dependencies

| Library | Use |
|---------|-----|
| `NCrontab` | Cron expression parsing |
| `System.IO.FileSystemWatcher` | File system events (built-in) |
| `System.Threading.Channels` | Priority queue backing store |

---

## 14. Estimated Effort

| Task | Days |
|------|------|
| SchedulerHost + queue + gate + deps | 4 |
| CronScheduler | 1 |
| FileWatchScheduler | 1 |
| EventBus + Manual schedulers | 1 |
| DependencyScheduler | 1.5 |
| ScheduleStorage | 0.5 |
| RateLimit + concurrency | 1 |
| Integration tests | 2 |
| **Total** | **12 days** |
