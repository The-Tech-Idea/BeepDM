using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Pipelines.Engine;
using TheTechIdea.Beep.Pipelines.Interfaces;
using TheTechIdea.Beep.Pipelines.Models;
using TheTechIdea.Beep.Pipelines.Registry;
using TheTechIdea.Beep.Workflows.Engine;

namespace TheTechIdea.Beep.Pipelines.Scheduling
{
    /// <summary>
    /// Singleton service that manages all scheduler plugins, handles triggers,
    /// priority-queues runs, and dispatches them to <see cref="PipelineEngine"/> /
    /// <see cref="WorkFlowEngine"/> with concurrency, dependency, circuit-breaker,
    /// watermark/CDC, backfill, and audit enforcement.
    /// </summary>
    public sealed class SchedulerHost : IAsyncDisposable
    {
        private readonly IDMEEditor             _editor;
        private readonly PipelineEngine         _pipelineEngine;
        private readonly PipelineManager        _pipelineManager;
        private readonly WorkFlowEngine         _workFlowEngine;
        private readonly WorkFlowStorage        _workFlowStorage;
        private readonly PipelinePluginRegistry _registry;
        private readonly PipelineRunQueue       _queue;
        private readonly ConcurrencyGate        _gate;
        private readonly DependencyGraph        _deps;

        private readonly ConcurrentDictionary<string, ScheduleDefinition>     _schedules  = new();
        private readonly ConcurrentDictionary<string, IPipelineScheduler>     _schedulers = new();
        private readonly ConcurrentDictionary<string, (string PipelineId, RunStatus Status)> _active = new();

        // Rate-limit state: scheduleId → (lastRun, Window of run times)
        private readonly ConcurrentDictionary<string, RateLimitState> _rateLimitStates = new();

        private CancellationTokenSource? _cts;
        private Task?                    _dispatchTask;
        private Task?                    _governanceTask;

        // Max parallel pipelines across the host
        private const int MaxParallel = 8;

        // Governance loop interval (how often to check for dependency timeouts, fail-fast, etc.)
        private const int GovernanceIntervalMs = 15_000;

        // ── Public surface ─────────────────────────────────────────────────────

        public ScheduleStorage Storage   { get; }
        public WatermarkTracker Watermarks { get; }
        public BackfillManager  Backfills  { get; }
        public ScheduleAuditLog AuditLog   { get; }

        /// <summary>Fired when a run is pulled from the queue and started.</summary>
        public event EventHandler<SchedulerRunEventArgs>? RunStarted;

        /// <summary>Fired when a run finishes successfully.</summary>
        public event EventHandler<SchedulerRunEventArgs>? RunCompleted;

        /// <summary>Fired when a run fails (after all retries).</summary>
        public event EventHandler<SchedulerRunEventArgs>? RunFailed;

        /// <summary>Fired when a circuit breaker trips or resets.</summary>
        public event EventHandler<SchedulerRunEventArgs>? CircuitBreakerChanged;

        // ── Constructor ────────────────────────────────────────────────────────

        public SchedulerHost(IDMEEditor editor)
        {
            _editor          = editor ?? throw new ArgumentNullException(nameof(editor));
            _pipelineEngine  = new PipelineEngine(editor);
            _pipelineManager = new PipelineManager(editor);
            _workFlowEngine  = new WorkFlowEngine(editor);
            _workFlowStorage = new WorkFlowStorage(editor);
            _registry        = new PipelinePluginRegistry(editor);
            _registry.Discover();
            Storage          = new ScheduleStorage(editor);
            Watermarks       = new WatermarkTracker(editor);
            Backfills        = new BackfillManager(editor, Watermarks);
            AuditLog         = new ScheduleAuditLog(editor);
            _queue           = new PipelineRunQueue();
            _gate            = new ConcurrencyGate();
            _deps            = new DependencyGraph();
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        /// <summary>Load schedules from storage and start all enabled schedulers.</summary>
        public async Task StartAsync(CancellationToken token = default)
        {
            _cts          = CancellationTokenSource.CreateLinkedTokenSource(token);
            _dispatchTask = Task.Run(() => DispatchLoopAsync(_cts.Token), CancellationToken.None);
            _governanceTask = Task.Run(() => GovernanceLoopAsync(_cts.Token), CancellationToken.None);

            await LoadAndWireSchedulesAsync().ConfigureAwait(false);

            // Validate dependency graph for cycles at startup
            var cycles = _deps.DetectCycles();
            if (cycles.Count > 0)
            {
                _editor.AddLogMessage(nameof(SchedulerHost),
                    $"WARNING: Dependency cycle detected among schedules: {string.Join(" → ", cycles)}. " +
                    "These schedules may never fire.",
                    DateTime.Now, -1, null, Errors.Failed);
            }

            _editor.AddLogMessage(nameof(SchedulerHost),
                $"Started — {_schedules.Count} schedule(s).", DateTime.Now, -1, null, Errors.Ok);
        }

        /// <summary>Stop all schedulers gracefully and wait for in-flight runs to finish.</summary>
        public async Task StopAsync()
        {
            _cts?.Cancel();

            foreach (var s in _schedulers.Values)
                try { await s.StopAsync().ConfigureAwait(false); } catch { }

            if (_dispatchTask != null)
                try { await _dispatchTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { }

            if (_governanceTask != null)
                try { await _governanceTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
        }

        /// <summary>Reload schedule definitions without restarting the host.</summary>
        public async Task ReloadAsync()
        {
            foreach (var s in _schedulers.Values)
                try { await s.StopAsync().ConfigureAwait(false); } catch { }

            _schedulers.Clear();
            _schedules.Clear();

            await LoadAndWireSchedulesAsync().ConfigureAwait(false);

            _editor.AddLogMessage(nameof(SchedulerHost),
                $"Reloaded — {_schedules.Count} schedule(s).", DateTime.Now, -1, null, Errors.Ok);
        }

        /// <summary>Manually trigger a pipeline or workflow immediately.</summary>
        public Task<string> TriggerManualAsync(string pipelineOrWorkflowId,
            IReadOnlyDictionary<string, object>? overrideParams = null)
        {
            var def = _schedules.Values.FirstOrDefault(s => s.PipelineId == pipelineOrWorkflowId);

            var run = new QueuedRun
            {
                ScheduleId     = def?.Id ?? string.Empty,
                PipelineId     = pipelineOrWorkflowId,
                IsWorkflow     = def?.IsWorkflow ?? false,
                Priority       = def?.Priority ?? 5,
                TriggerSource  = "manual",
                WorkloadClass  = def?.WorkloadClass ?? "standard",
                OverrideParams = overrideParams
            };

            _queue.Enqueue(run);
            return Task.FromResult(run.RunId);
        }

        /// <summary>
        /// Submit a backfill request. The next pending window is queued immediately;
        /// subsequent windows are queued as each completes.
        /// </summary>
        public async Task<string> SubmitBackfillAsync(
            string scheduleId, string fromValue, string toValue,
            string reason = "", string requestedBy = "",
            int windowSizeSeconds = 86400)
        {
            if (!_schedules.TryGetValue(scheduleId, out var def))
                throw new InvalidOperationException($"Schedule '{scheduleId}' not found.");

            var request = await Backfills.CreateRequestAsync(
                scheduleId, def.PipelineId,
                fromValue, toValue,
                def.Watermark.WatermarkType, def.Watermark.WatermarkColumn,
                reason, requestedBy, windowSizeSeconds).ConfigureAwait(false);

            // Queue the first window
            EnqueueNextBackfillWindow(request, def);

            return request.Id;
        }

        /// <summary>
        /// Reset a circuit breaker for a schedule, re-enabling it for execution.
        /// </summary>
        public async Task ResetCircuitBreakerAsync(string scheduleId, string resetBy = "")
        {
            if (!_schedules.TryGetValue(scheduleId, out var def)) return;

            def.ConsecutiveFailures  = 0;
            def.CircuitBreakerTripped = false;
            def.IsEnabled            = true;
            _schedules[scheduleId]   = def;
            _deps.SetCircuitBroken(scheduleId, false);

            await Storage.SaveAsync(def).ConfigureAwait(false);
            await AuditLog.LogCircuitBreakerAsync(scheduleId, def.Name, false, 0).ConfigureAwait(false);

            CircuitBreakerChanged?.Invoke(this,
                new SchedulerRunEventArgs(string.Empty, def.PipelineId, scheduleId));

            _editor.AddLogMessage(nameof(SchedulerHost),
                $"Circuit breaker reset for schedule '{def.Name}' by {resetBy}.",
                DateTime.Now, -1, null, Errors.Ok);
        }

        /// <summary>Snapshot of all currently executing runs.</summary>
        public IReadOnlyList<(string RunId, string PipelineId, RunStatus Status)> GetActiveRuns()
            => _active.Select(kv => (kv.Key, kv.Value.PipelineId, kv.Value.Status)).ToList();

        // ── Private: Loading & wiring ──────────────────────────────────────────

        private async Task LoadAndWireSchedulesAsync()
        {
            var defs = await Storage.LoadAllAsync().ConfigureAwait(false);

            foreach (var def in defs)
            {
                _schedules[def.Id] = def;

                // Apply circuit breaker state from persisted data
                if (def.CircuitBreakerTripped)
                    _deps.SetCircuitBroken(def.Id, true);

                if (!def.IsEnabled) continue;

                if (def.DependsOn.Count > 0)
                {
                    string condition = def.SchedulerConfig.TryGetValue("Condition", out var cv)
                        ? cv?.ToString() ?? "ALL_SUCCESS" : "ALL_SUCCESS";
                    _deps.RegisterDependency(def.Id, def.DependsOn, condition);

                    if (def.DependencyMaxWaitSeconds > 0)
                        _deps.SetMaxWait(def.Id, def.DependencyMaxWaitSeconds);
                }

                // Dependency schedulers are triggered by DependencyGraph, not a plugin
                if (def.SchedulerPluginId == "beep.schedule.dependency") continue;

                if (!string.IsNullOrEmpty(def.SchedulerPluginId))
                    await StartSchedulerAsync(def).ConfigureAwait(false);
            }
        }

        private async Task StartSchedulerAsync(ScheduleDefinition def)
        {
            try
            {
                var scheduler = _registry.Create<IPipelineScheduler>(def.SchedulerPluginId);

                // Inject PipelineId into config if not already present
                if (!def.SchedulerConfig.ContainsKey("PipelineId"))
                    def.SchedulerConfig["PipelineId"] = def.PipelineId;

                scheduler.Configure(def.SchedulerConfig);
                scheduler.Triggered += (_, args) => OnTriggered(def, args);
                _schedulers[def.Id] = scheduler;

                if (_cts != null)
                    await scheduler.StartAsync(_cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage(nameof(SchedulerHost),
                    $"Failed to start plugin '{def.SchedulerPluginId}' for schedule '{def.Name}': {ex.Message}",
                    DateTime.Now, -1, null, Errors.Failed);
            }
        }

        private void OnTriggered(ScheduleDefinition def, PipelineTriggerArgs args)
        {
            // Block triggers if circuit breaker is tripped
            if (def.CircuitBreakerTripped) return;

            if (!CheckRateLimit(def)) return;

            var run = new QueuedRun
            {
                ScheduleId     = def.Id,
                PipelineId     = def.PipelineId,
                IsWorkflow     = def.IsWorkflow,
                Priority       = def.Priority,
                TriggerSource  = args.TriggerSource,
                WorkloadClass  = def.WorkloadClass,
                OverrideParams = args.Parameters
            };
            _queue.Enqueue(run);
        }

        // ── Rate limiting ──────────────────────────────────────────────────────

        private bool CheckRateLimit(ScheduleDefinition def)
        {
            var rl = def.RateLimitPolicy;
            if (rl.MaxRuns <= 0 && rl.MinGapSeconds <= 0) return true;

            var state = _rateLimitStates.GetOrAdd(def.Id, _ => new RateLimitState());
            var now   = DateTime.UtcNow;

            lock (state)
            {
                if (rl.MinGapSeconds > 0 && state.LastRun != DateTime.MinValue &&
                    (now - state.LastRun).TotalSeconds < rl.MinGapSeconds)
                    return false;

                if (rl.MaxRuns > 0)
                {
                    // Evict entries outside window
                    while (state.Window.Count > 0 &&
                           (now - state.Window.Peek()).TotalSeconds > rl.WindowSeconds)
                        state.Window.Dequeue();

                    if (state.Window.Count >= rl.MaxRuns) return false;
                }

                state.Window.Enqueue(now);
                state.LastRun = now;
                return true;
            }
        }

        // ── Governance loop (dependency timeouts, fail-fast, day-window reset) ─

        private async Task GovernanceLoopAsync(CancellationToken token)
        {
            DateTime lastDayReset = DateTime.UtcNow.Date;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(GovernanceIntervalMs, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }

                // Day-window reset at midnight UTC
                var today = DateTime.UtcNow.Date;
                if (today > lastDayReset)
                {
                    _deps.ResetDayWindow();
                    lastDayReset = today;
                    _editor.AddLogMessage(nameof(SchedulerHost),
                        "Dependency graph day-window reset.", DateTime.Now, -1, null, Errors.Ok);
                }

                // Fail-fast propagation
                foreach (var failFastId in _deps.GetFailFastSchedules())
                {
                    if (_schedules.TryGetValue(failFastId, out var ffDef))
                    {
                        _editor.AddLogMessage(nameof(SchedulerHost),
                            $"Fail-fast: schedule '{ffDef.Name}' ({failFastId}) cannot execute — upstream dependency failed.",
                            DateTime.Now, -1, null, Errors.Failed);

                        PipelineEventBus.PublishPipelineEvent("fail_fast", string.Empty,
                            ffDef.PipelineId, failFastId, "Upstream dependency failed");

                        // Propagate: notify graph so downstream of this schedule also fail-fast
                        _deps.NotifyCompletion(failFastId, false, DateTime.UtcNow);
                    }
                }

                // Dependency timeout handling
                foreach (var timedOutId in _deps.GetTimedOutSchedules())
                {
                    if (_schedules.TryGetValue(timedOutId, out var toDef))
                    {
                        _editor.AddLogMessage(nameof(SchedulerHost),
                            $"Dependency timeout: schedule '{toDef.Name}' ({timedOutId}) exceeded max-wait of {toDef.DependencyMaxWaitSeconds}s.",
                            DateTime.Now, -1, null, Errors.Failed);

                        PipelineEventBus.PublishPipelineEvent("dependency_timeout", string.Empty,
                            toDef.PipelineId, timedOutId, "Dependency wait exceeded max-wait");

                        _deps.NotifyCompletion(timedOutId, false, DateTime.UtcNow);
                    }
                }

                // Freshness SLA check for active schedules
                foreach (var def in _schedules.Values)
                {
                    if (def.FreshnessSlaSeconds > 0 && def.LastRunAt.HasValue)
                    {
                        var age = (DateTime.UtcNow - def.LastRunAt.Value).TotalSeconds;
                        if (age > def.FreshnessSlaSeconds)
                        {
                            _editor.AddLogMessage(nameof(SchedulerHost),
                                $"Freshness SLA breach: schedule '{def.Name}' data age {age:F0}s exceeds SLA of {def.FreshnessSlaSeconds}s.",
                                DateTime.Now, -1, null, Errors.Failed);

                            PipelineEventBus.PublishPipelineEvent("freshness_sla_breach", string.Empty,
                                def.PipelineId, def.Id,
                                $"Data age {age:F0}s exceeds SLA of {def.FreshnessSlaSeconds}s");
                        }
                    }
                }
            }
        }

        // ── Dispatcher loop ────────────────────────────────────────────────────

        private async Task DispatchLoopAsync(CancellationToken token)
        {
            using var semaphore = new SemaphoreSlim(MaxParallel, MaxParallel);

            while (!token.IsCancellationRequested)
            {
                QueuedRun run;
                try
                {
                    run = await _queue.DequeueAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }

                await semaphore.WaitAsync(token).ConfigureAwait(false);

                _ = Task.Run(async () =>
                {
                    try   { await ExecuteRunAsync(run, token).ConfigureAwait(false); }
                    finally
                    {
                        semaphore.Release();
                        _queue.NotifyRunCompleted(run.WorkloadClass);
                    }
                }, CancellationToken.None);
            }
        }

        private async Task ExecuteRunAsync(QueuedRun run, CancellationToken hostToken)
        {
            _schedules.TryGetValue(run.ScheduleId, out var def);
            int maxConc     = def?.MaxConcurrentRuns ?? 1;
            int timeoutSec  = def?.TimeoutSeconds ?? 0;
            int maxAttempts = (def?.RetryPolicy.MaxRetries ?? 0) + 1;

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                hostToken, run.Cts.Token);
            if (timeoutSec > 0)
                linkedCts.CancelAfter(TimeSpan.FromSeconds(timeoutSec));

            using var slot = await _gate.AcquireAsync(run.PipelineId, maxConc, linkedCts.Token)
                .ConfigureAwait(false);

            _active[run.RunId] = (run.PipelineId, RunStatus.Running);
            RunStarted?.Invoke(this, new SchedulerRunEventArgs(run.RunId, run.PipelineId, run.ScheduleId));
            PipelineEventBus.PublishPipelineEvent("run_started", run.RunId, run.PipelineId, run.ScheduleId);

            bool success = false;
            string? error = null;
            PipelineRunResult? lastResult = null;

            try
            {
                // Prepare watermark overrides for incremental runs
                Dictionary<string, object>? watermarkOverrides = null;
                if (def != null && def.RunMode == "incremental" &&
                    !string.IsNullOrEmpty(def.Watermark.WatermarkColumn))
                {
                    var wmState = await Watermarks.LoadAsync(def.Id).ConfigureAwait(false);
                    var window  = Watermarks.ComputeWindow(def.Watermark, wmState);

                    watermarkOverrides = new Dictionary<string, object>
                    {
                        ["__watermark_column"]   = window.WatermarkColumn,
                        ["__watermark_type"]     = window.WatermarkType,
                        ["__watermark_from"]     = window.FromValue ?? string.Empty,
                        ["__watermark_is_first"] = window.IsFirstRun
                    };
                    if (window.ToValue != null)
                        watermarkOverrides["__watermark_to"] = window.ToValue;
                }

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        if (run.IsWorkflow)
                        {
                            var wfDef = await _workFlowStorage.LoadDefinitionAsync(run.PipelineId)
                                .ConfigureAwait(false);
                            if (wfDef == null)
                                throw new InvalidOperationException(
                                    $"Workflow '{run.PipelineId}' not found.");

                            await _workFlowEngine.RunAsync(wfDef, null, linkedCts.Token,
                                run.OverrideParams).ConfigureAwait(false);
                        }
                        else
                        {
                            var pipeDef = await _pipelineManager.LoadAsync(run.PipelineId)
                                .ConfigureAwait(false);
                            if (pipeDef == null)
                                throw new InvalidOperationException(
                                    $"Pipeline '{run.PipelineId}' not found.");

                            // Merge watermark + trigger overrides
                            var overrides = new Dictionary<string, object>();
                            if (watermarkOverrides != null)
                                foreach (var kv in watermarkOverrides) overrides[kv.Key] = kv.Value;
                            if (run.OverrideParams != null)
                                foreach (var kv in run.OverrideParams) overrides[kv.Key] = kv.Value;

                            lastResult = await _pipelineEngine.RunAsync(
                                pipeDef, null, linkedCts.Token,
                                overrides.Count > 0 ? overrides : null).ConfigureAwait(false);

                            success = lastResult.Status is RunStatus.Success or RunStatus.Partial;
                            if (success) break;
                        }

                        success = true;
                        break;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        error = ex.Message;
                        if (attempt >= maxAttempts) break;

                        int delay = def == null ? 60_000
                            : (int)(def.RetryPolicy.BaseDelayMs *
                                    Math.Pow(def.RetryPolicy.BackoffFactor, attempt - 1));
                        await Task.Delay(delay, linkedCts.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                error   = "Cancelled";
                success = false;
            }
            catch (Exception ex)
            {
                error   = ex.Message;
                success = false;
            }
            finally
            {
                _active.TryRemove(run.RunId, out _);

                // Persist last-run status & handle circuit breaker
                if (def != null)
                {
                    def.LastRunAt     = DateTime.UtcNow;
                    def.LastRunStatus = success ? "Success" : "Failed";

                    if (success)
                    {
                        def.ConsecutiveFailures = 0;

                        // Advance watermark on successful incremental run
                        if (def.RunMode == "incremental" &&
                            !string.IsNullOrEmpty(def.Watermark.WatermarkColumn) &&
                            lastResult != null)
                        {
                            // The pipeline should set __watermark_new_value in run context
                            // For now, update the last watermark with current UTC as fallback
                            var wmState = new WatermarkState
                            {
                                ScheduleId          = def.Id,
                                PipelineId          = def.PipelineId,
                                LastWatermarkValue  = DateTime.UtcNow.ToString("O"),
                                LastRunId           = run.RunId,
                                LastRecordsProcessed = lastResult.RecordsWritten
                            };
                            await Watermarks.SaveAsync(def.Id, wmState).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // Circuit breaker logic
                        def.ConsecutiveFailures++;
                        if (def.CircuitBreakerThreshold > 0 &&
                            def.ConsecutiveFailures >= def.CircuitBreakerThreshold &&
                            !def.CircuitBreakerTripped)
                        {
                            def.CircuitBreakerTripped = true;
                            def.IsEnabled            = false;
                            _deps.SetCircuitBroken(def.Id, true);

                            await AuditLog.LogCircuitBreakerAsync(def.Id, def.Name, true,
                                def.ConsecutiveFailures).ConfigureAwait(false);

                            CircuitBreakerChanged?.Invoke(this,
                                new SchedulerRunEventArgs(run.RunId, def.PipelineId, def.Id, error));

                            _editor.AddLogMessage(nameof(SchedulerHost),
                                $"Circuit breaker TRIPPED for schedule '{def.Name}' " +
                                $"after {def.ConsecutiveFailures} consecutive failures.",
                                DateTime.Now, -1, null, Errors.Failed);
                        }
                    }

                    _schedules[def.Id] = def;
                    await Storage.SaveAsync(def).ConfigureAwait(false);
                }

                // Notify dependency graph
                if (!string.IsNullOrEmpty(run.ScheduleId))
                    _deps.NotifyCompletion(run.ScheduleId, success, DateTime.UtcNow);

                // Enqueue any now-unblocked dependents
                foreach (var unblockedId in _deps.GetUnblockedSchedules())
                {
                    if (_schedules.TryGetValue(unblockedId, out var depDef) && depDef.IsEnabled)
                    {
                        _queue.Enqueue(new QueuedRun
                        {
                            ScheduleId    = depDef.Id,
                            PipelineId    = depDef.PipelineId,
                            IsWorkflow    = depDef.IsWorkflow,
                            Priority      = depDef.Priority,
                            TriggerSource = "dependency",
                            WorkloadClass = depDef.WorkloadClass
                        });
                    }
                }

                // Publish lifecycle events
                if (success)
                {
                    RunCompleted?.Invoke(this, new SchedulerRunEventArgs(run.RunId, run.PipelineId, run.ScheduleId));
                    PipelineEventBus.PublishPipelineEvent("run_completed", run.RunId, run.PipelineId, run.ScheduleId);
                }
                else
                {
                    RunFailed?.Invoke(this,
                        new SchedulerRunEventArgs(run.RunId, run.PipelineId, run.ScheduleId, error));
                    PipelineEventBus.PublishPipelineEvent("run_failed", run.RunId, run.PipelineId, run.ScheduleId, error);
                }
            }
        }

        // ── Backfill helpers ───────────────────────────────────────────────────

        private void EnqueueNextBackfillWindow(BackfillRequest request, ScheduleDefinition def)
        {
            var window = Backfills.GetNextPendingWindow(request);
            if (window == null) return;

            window.Status = BackfillWindowStatus.Running;
            var overrides = Backfills.BuildWindowOverrides(request, window);

            var run = new QueuedRun
            {
                ScheduleId     = def.Id,
                PipelineId     = def.PipelineId,
                IsWorkflow     = def.IsWorkflow,
                Priority       = 8, // Lower priority for backfill
                TriggerSource  = "backfill",
                WorkloadClass  = "backfill",
                OverrideParams = overrides
            };

            _queue.Enqueue(run);
        }

        // ── IAsyncDisposable ───────────────────────────────────────────────────

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
            _cts?.Dispose();
        }

        // ── Nested types ───────────────────────────────────────────────────────

        private sealed class RateLimitState
        {
            public DateTime       LastRun { get; set; } = DateTime.MinValue;
            public Queue<DateTime> Window  { get; }     = new();
        }
    }

    // ── Event args ─────────────────────────────────────────────────────────────

    public sealed class SchedulerRunEventArgs : EventArgs
    {
        public string  RunId      { get; }
        public string  PipelineId { get; }
        public string  ScheduleId { get; }
        public string? Error      { get; }

        public SchedulerRunEventArgs(string runId, string pipelineId, string scheduleId,
            string? error = null)
        {
            RunId      = runId;
            PipelineId = pipelineId;
            ScheduleId = scheduleId;
            Error      = error;
        }
    }
}
