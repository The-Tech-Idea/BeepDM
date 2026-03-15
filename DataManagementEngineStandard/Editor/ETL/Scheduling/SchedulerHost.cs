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
    /// <see cref="WorkFlowEngine"/> with concurrency and dependency enforcement.
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

        // Max parallel pipelines across the host
        private const int MaxParallel = 8;

        // ── Public surface ─────────────────────────────────────────────────────

        public ScheduleStorage Storage { get; }

        /// <summary>Fired when a run is pulled from the queue and started.</summary>
        public event EventHandler<SchedulerRunEventArgs>? RunStarted;

        /// <summary>Fired when a run finishes successfully.</summary>
        public event EventHandler<SchedulerRunEventArgs>? RunCompleted;

        /// <summary>Fired when a run fails (after all retries).</summary>
        public event EventHandler<SchedulerRunEventArgs>? RunFailed;

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

            await LoadAndWireSchedulesAsync().ConfigureAwait(false);

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
                OverrideParams = overrideParams
            };

            _queue.Enqueue(run);
            return Task.FromResult(run.RunId);
        }

        /// <summary>Snapshot of all currently executing runs.</summary>
        public IReadOnlyList<(string RunId, string PipelineId, RunStatus Status)> GetActiveRuns()
            => _active.Select(kv => (kv.Key, kv.Value.PipelineId, kv.Value.Status)).ToList();

        // ── Private methods ────────────────────────────────────────────────────

        private async Task LoadAndWireSchedulesAsync()
        {
            var defs = await Storage.LoadAllAsync().ConfigureAwait(false);

            foreach (var def in defs)
            {
                _schedules[def.Id] = def;
                if (!def.IsEnabled) continue;

                if (def.DependsOn.Count > 0)
                {
                    string condition = def.SchedulerConfig.TryGetValue("Condition", out var cv)
                        ? cv?.ToString() ?? "ALL_SUCCESS" : "ALL_SUCCESS";
                    _deps.RegisterDependency(def.Id, def.DependsOn, condition);
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
            if (!CheckRateLimit(def)) return;

            var run = new QueuedRun
            {
                ScheduleId     = def.Id,
                PipelineId     = def.PipelineId,
                IsWorkflow     = def.IsWorkflow,
                Priority       = def.Priority,
                TriggerSource  = args.TriggerSource,
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
                    finally { semaphore.Release(); }
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

            bool success = false;
            string? error = null;

            try
            {
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

                            var overrides = run.OverrideParams != null
                                ? new Dictionary<string, object>(run.OverrideParams)
                                : null;

                            var result = await _pipelineEngine.RunAsync(
                                pipeDef, null, linkedCts.Token, overrides).ConfigureAwait(false);

                            success = result.Status is RunStatus.Success or RunStatus.Partial;
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

                // Persist last-run status
                if (def != null)
                {
                    def.LastRunAt     = DateTime.UtcNow;
                    def.LastRunStatus = success ? "Success" : "Failed";
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
                            TriggerSource = "dependency"
                        });
                    }
                }

                if (success)
                    RunCompleted?.Invoke(this, new SchedulerRunEventArgs(run.RunId, run.PipelineId, run.ScheduleId));
                else
                    RunFailed?.Invoke(this,
                        new SchedulerRunEventArgs(run.RunId, run.PipelineId, run.ScheduleId, error));
            }
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
