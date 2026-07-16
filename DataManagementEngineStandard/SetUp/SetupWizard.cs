using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.SetUp.State;
using static TheTechIdea.Beep.SetUp.StepErrorHelpers;

namespace TheTechIdea.Beep.SetUp
{
    public class SetupWizard : ISetupWizard
    {
        private readonly string _wizardId;
        private readonly List<ISetupStep> _steps;
        private readonly ILogger<SetupWizard>? _logger;
        private readonly ISetupWizardAdapter? _adapter;
        private readonly ISetupStateStore? _injectedStore;
        private readonly Rollback.IRollbackOrchestrator _rollback;
        private readonly Security.ISetupPrincipal _principal;
        private readonly Security.ISetupAuthorizer _authorizer;
        private readonly Audit.ISetupAuditSink _audit;
        private readonly string _definitionHash;
        private readonly string _appId;
        private SetupReport _lastReport;

        /// <summary>How long a run holds the setup lease before it is reclaimable after a crash.</summary>
        private static readonly TimeSpan LeaseTtl = TimeSpan.FromMinutes(30);

        // Run-scoped bindings — set at the start of Run, cleared in its finally. Safe as fields
        // because a wizard instance runs one setup at a time.
        private ISetupStateStore? _runStore;
        private SetupStateKey? _runKey;
        private ISetupStateLease? _runLease;
        private bool _forceLoadOnNextRun;

        public IReadOnlyList<ISetupStep> Steps => _steps.AsReadOnly();
        public SetupState State { get; private set; } = new SetupState();
        public SetupOptions Options { get; }

        public SetupWizard(string wizardId, IEnumerable<ISetupStep> steps, SetupOptions options,
            ILogger<SetupWizard>? logger = null, ISetupWizardAdapter? adapter = null,
            ISetupStateStore? stateStore = null, Rollback.IRollbackOrchestrator? rollback = null,
            Security.ISetupPrincipal? principal = null, Security.ISetupAuthorizer? authorizer = null,
            Audit.ISetupAuditSink? audit = null, string? definitionHash = null, string? appId = null)
        {
            _wizardId = wizardId ?? "default-setup";
            _appId = appId;
            _steps = new List<ISetupStep>(steps);
            Options = options ?? new SetupOptions();
            _logger = logger;
            _adapter = adapter;
            _injectedStore = stateStore;
            _rollback = rollback ?? new Rollback.RollbackOrchestrator(logger);
            // Solo defaults: anonymous local user, allow everything. Zero-config setup is unaffected.
            _principal = principal ?? new Security.AnonymousSetupPrincipal();
            _authorizer = authorizer ?? new Security.AllowAllAuthorizer();
            _audit = audit ?? Audit.NullSetupAuditSink.Instance;
            _definitionHash = definitionHash;
        }

        /// <inheritdoc/>
        public IErrorsInfo Run(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            if (context == null)
                return Fail("SetupContext must not be null.");

            var runOptions = context.Options ?? Options;
            WarnOnSilentOptionOverride(runOptions);
            context.Options = runOptions;

            _runStore = ResolveStore(runOptions);
            // appId scopes state per app in a multi-app solution — the SetupStateKey slot P3 added.
            _runKey = new SetupStateKey(_wizardId, runOptions.Environment, _appId);

            // Checkpointing is a silent no-op without a store — say so rather than letting a caller
            // believe a long run is resumable when it isn't.
            if (_runStore == null)
                _logger?.LogWarning(
                    "No setup state store (SetupOptions.StateFilePath unset and no store injected) — " +
                    "checkpointing is disabled and this run cannot be resumed.");

            // Acquire the exclusive lease. A held lease means another runner owns this key; refuse
            // rather than interleave writes to shared state.
            if (_runStore != null)
            {
                _runLease = Bridge(() => _runStore.TryAcquireLeaseAsync(_runKey, LeaseTtl));
                if (_runLease == null)
                    return Fail(
                        $"Another runner holds the setup lock for '{_runKey}'. Wait for it to finish " +
                        "or, if it crashed, retry after the lease expires.");
            }

            try
            {
                return RunGuarded(context, progress, runOptions);
            }
            finally
            {
                if (_runLease != null)
                {
                    try { Bridge(() => _runLease.DisposeAsync().AsTask()); }
                    catch (Exception ex) { _logger?.LogWarning(ex, "Failed to release setup lease."); }
                }
                _runStore = null;
                _runKey = null;
                _runLease = null;
            }
        }

        private IErrorsInfo RunGuarded(SetupContext context, IProgress<PassedArgs> progress, SetupOptions runOptions)
        {
            // Load persisted checkpoint; Resume forces a reload even over in-memory state.
            LoadState(force: _forceLoadOnNextRun);
            _forceLoadOnNextRun = false;

            // Assign a fresh RunId on new runs; preserved on resume from checkpoint
            if (string.IsNullOrEmpty(State.RunId))
                State.RunId = Guid.NewGuid().ToString("N");

            // Record who is running this, so state and report (and Phase 6 audit) can say so.
            // Never inferred — an anonymous run records ActorAuthenticated=false.
            State.ActorId = _principal.Id;
            State.ActorAuthenticated = _principal.IsAuthenticated;

            // Up-front authorization: may this principal run setup at all?
            var runAuth = Bridge(() => _authorizer.AuthorizeAsync(_principal, Security.SetupPermission.RunSetup, context));
            if (!runAuth.Allowed)
            {
                EmitAudit(Audit.SetupAuditAction.Denied, null, false, runAuth.Reason);
                return Fail($"Not authorized to run setup: {runAuth.Reason}");
            }

            EmitAudit(Audit.SetupAuditAction.RunStarted, null, true, $"Setup started by {_principal.Id}.");

            // Merge any pre-populated context state (e.g. restored by caller from storage)
            SyncFromContext(context);

            // Ensure context reflects the latest merged/persisted state even before any step executes.
            // This is important when all steps are already completed and the loop does no work.
            SyncToContext(context);

            // Clear stale failure marker from a previous run so outcome reflects this run only
            State.FailedStepId = null;

            State.StartedAt ??= DateTimeOffset.UtcNow;
            var results = new List<SetupStepResult>();
            var started = DateTimeOffset.UtcNow;
            int total = _steps.Count;

            progress?.Report(new PassedArgs { ParameterInt1 = 0, Messege = "Starting setup wizard…" });

            // Validate step graph up-front so malformed step registrations fail cleanly
            // with persisted state/reporting instead of throwing during execution.
            var stepValidation = ValidateStepDefinitions(out var invalidStepId);
            if (stepValidation.Flag == Errors.Failed)
                return FailStep(context, results, started, runOptions, invalidStepId, stepValidation, syncFromContext: false);

            for (int i = 0; i < total; i++)
            {
                var step = _steps[i];
                if (State.IsStepCompleted(step.StepId)) continue;

                // ── Runtime DependsOn guard ──────────────────────────────────
                foreach (var dep in step.DependsOn ?? Array.Empty<string>())
                {
                    if (!State.IsStepCompleted(dep))
                    {
                        var depErr = Fail(
                            $"Step '{step.StepId}' requires '{dep}' to complete first, " +
                            $"but '{dep}' has not been completed or skipped. " +
                            "Check step registration order.");
                        return FailStep(context, results, started, runOptions, step.StepId, depErr, syncFromContext: false);
                    }
                }

                try
                {
                    // ── Validate before executing ────────────────────────────────
                    var validation = step.Validate(context);
                    if (validation.Flag == Errors.Failed)
                        return FailStep(context, results, started, runOptions, step.StepId, validation, syncFromContext: false);

                    // ── Check CanSkip ────────────────────────────────────────────
                    if (step.CanSkip(context))
                    {
                        State.SkippedStepIds.Add(step.StepId);
                        State.LastUpdatedAt = DateTimeOffset.UtcNow;
                        results.Add(new SetupStepResult
                        {
                            StepId = step.StepId,
                            StepName = step.StepName,
                            Succeeded = true,
                            Skipped = true,
                            ExecutedAt = DateTimeOffset.UtcNow
                        });
                        context.ProgressReporter?.ReportStepComplete(step.StepId, true, "Skipped");
                        EmitAudit(Audit.SetupAuditAction.StepSkipped, step.StepId, true, "Skipped");
                        SyncToContext(context);
                        PersistState();
                        continue;
                    }

                    // ── Per-step authorization (only for steps that will actually run) ──
                    var stepAuth = Bridge(() =>
                        _authorizer.AuthorizeAsync(_principal, step.RequiredPermission, context));
                    if (!stepAuth.Allowed)
                    {
                        EmitAudit(Audit.SetupAuditAction.Denied, step.StepId, false, stepAuth.Reason);
                        var denied = Fail($"Not authorized for '{step.StepId}' " +
                                          $"({step.RequiredPermission}): {stepAuth.Reason}");
                        return FailStep(context, results, started, runOptions, step.StepId, denied, syncFromContext: false);
                    }

                    // ── Execute ──────────────────────────────────────────────────
                    _adapter?.ShowStep(step, i, total);
                    context.ProgressReporter?.ReportStepStart(step.StepId, step.StepName, i + 1, total);
                    EmitAudit(Audit.SetupAuditAction.StepStarted, step.StepId, true, null);
                    progress?.Report(new PassedArgs
                    {
                        ParameterInt1 = (int)((i / (double)total) * 100),
                        Messege = $"Running: {step.StepName}"
                    });

                    // Ensure context.State is fully current before the step reads it.
                    // Critical for SeedingStep resume: CompletedSeederIds from the persisted
                    // checkpoint must be visible in context before Execute begins.
                    SyncToContext(context);

                    using var stepActivity = StartStepActivity(step);
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var result = step.Execute(context, progress);
                    sw.Stop();
                    stepActivity?.SetTag(Telemetry.SetupActivitySource.TagOutcome,
                        result.Flag == Errors.Ok ? "ok" : "failed");

                    var stepResult = new SetupStepResult
                    {
                        StepId = step.StepId,
                        StepName = step.StepName,
                        Succeeded = result.Flag == Errors.Ok,
                        Message = result.Message,
                        Elapsed = sw.Elapsed,
                        ExecutedAt = DateTimeOffset.UtcNow
                    };
                    results.Add(stepResult);

                    context.ProgressReporter?.ReportStepComplete(step.StepId, stepResult.Succeeded, result.Message);

                    if (result.Flag != Errors.Ok)
                    {
                        EmitAudit(Audit.SetupAuditAction.StepFailed, step.StepId, false, result.Message, sw.Elapsed);
                        return FailStep(context, results, started, runOptions, step.StepId, result, syncFromContext: true);
                    }

                    EmitAudit(Audit.SetupAuditAction.StepCompleted, step.StepId, true, result.Message, sw.Elapsed);
                    State.CompletedStepIds.Add(step.StepId);
                    State.LastUpdatedAt = DateTimeOffset.UtcNow;
                    SyncAndPersist(context, runOptions);
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogWarning("Setup wizard cancelled at step '{StepId}' ({StepName})",
                        step.StepId, step.StepName);
                    FailStep(context, results, started, runOptions, step.StepId,
                        Fail("Setup cancelled."), syncFromContext: true);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Step '{StepId}' ({StepName}) threw an unhandled exception",
                        step.StepId, step.StepName);
                    State.FailedStepId = step.StepId;
                    State.LastUpdatedAt = DateTimeOffset.UtcNow;

                    var thrownResult = new SetupStepResult
                    {
                        StepId = step.StepId,
                        StepName = step.StepName,
                        Succeeded = false,
                        Message = $"Unhandled exception: {ex.Message}",
                        ExecutedAt = DateTimeOffset.UtcNow
                    };
                    results.Add(thrownResult);

                    context.ProgressReporter?.ReportStepComplete(step.StepId, false, thrownResult.Message);

                    SyncAndPersist(context, runOptions);

                    return Fail($"Step '{step.StepId}' threw an unhandled exception.", ex);
                }
            }

            progress?.Report(new PassedArgs { ParameterInt1 = 100, Messege = "Setup completed." });

            _lastReport = BuildReport(results, true, started, runOptions.Environment, context, runOptions);
            _adapter?.ShowResult(_lastReport);
            _logger?.LogInformation("Setup wizard '{WizardId}' completed successfully. RunId={RunId}, Steps={StepCount}",
                _wizardId, State.RunId, results.Count);
            context.ProgressReporter?.ReportWizardComplete(_lastReport);

            // Persist final state/report outcome even when no step executed in this run
            // (for example: all steps already completed when resuming from checkpoint).
            SyncToContext(context);
            PersistState();

            EmitAudit(Audit.SetupAuditAction.RunCompleted, null, true,
                $"Setup completed ({results.Count} step(s)).", DateTimeOffset.UtcNow - started);
            return Ok("Setup completed successfully.");
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Loads the persisted checkpoint from <see cref="SetupOptions.StateFilePath"/> before
        /// re-entering <see cref="Run"/>, so a wizard recreated after a crash will correctly
        /// skip already-completed steps.
        /// </remarks>
        public IErrorsInfo Resume(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            if (context == null)
                return Fail("SetupContext must not be null.");

            // Force a reload from the store inside Run, even if the wizard already has in-memory
            // state — the store isn't bound until Run acquires the lease, so this can't load here.
            _forceLoadOnNextRun = true;
            return Run(context, progress);
        }

        /// <inheritdoc/>
        public SetupReport GetReport() => _lastReport;

        public Task<IErrorsInfo> RunAsync(
            SetupContext context,
            IProgress<PassedArgs>? progress = null,
            CancellationToken token = default)
        {
            return Task.Run(() => Run(context, progress), token);
        }

        // ── Audit & telemetry (Phase 6) ───────────────────────────────────────

        /// <summary>Emits a setup audit event. Auditing never fails the run — errors are swallowed.</summary>
        private void EmitAudit(Audit.SetupAuditAction action, string stepId, bool succeeded,
            string message, TimeSpan elapsed = default)
        {
            try
            {
                Bridge(() => _audit.RecordAsync(new Audit.SetupAuditEvent
                {
                    RunId = State.RunId,
                    WizardId = _wizardId,
                    AppId = _runKey?.AppId,
                    Environment = Options.Environment,
                    DefinitionHash = _definitionHash,
                    StepId = stepId,
                    Action = action,
                    ActorId = _principal.Id,
                    ActorAuthenticated = _principal.IsAuthenticated,
                    Succeeded = succeeded,
                    Message = message,
                    Elapsed = elapsed
                }));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Setup audit emit failed (continuing).");
            }
        }

        /// <summary>Starts a per-step telemetry span, tagged for dashboards. Zero-cost when unlistened.</summary>
        private System.Diagnostics.Activity StartStepActivity(ISetupStep step)
        {
            var act = Telemetry.SetupActivitySource.Source.StartActivity($"setup.step.{step.StepId}");
            act?.SetTag(Telemetry.SetupActivitySource.TagWizardId, _wizardId);
            act?.SetTag(Telemetry.SetupActivitySource.TagStepId, step.StepId);
            act?.SetTag(Telemetry.SetupActivitySource.TagEnvironment, Options.Environment);
            act?.SetTag(Telemetry.SetupActivitySource.TagDefinitionHash, _definitionHash);
            act?.SetTag(Telemetry.SetupActivitySource.TagActorId, _principal.Id);
            return act;
        }

        // ── State store plumbing (Phase 3) ────────────────────────────────────

        /// <summary>
        /// Chooses the state store for this run: an injected store wins; otherwise a legacy explicit
        /// <see cref="SetupOptions.StateFilePath"/> maps to a local file store; otherwise null
        /// (checkpointing disabled, matching the historical no-path behaviour).
        /// </summary>
        private ISetupStateStore? ResolveStore(SetupOptions runOptions)
        {
            if (_injectedStore != null) return _injectedStore;
            if (!string.IsNullOrWhiteSpace(runOptions.StateFilePath))
                return LocalJsonSetupStateStore.ForExplicitFile(runOptions.StateFilePath, _logger);
            return null;
        }

        /// <summary>Loads persisted state through the run store and merges it into <see cref="State"/>.</summary>
        private void LoadState(bool force)
        {
            if (_runStore == null || _runKey == null) return;
            if (!force && State.CompletedStepIds.Count > 0) return;

            var loaded = Bridge(() => _runStore.LoadAsync(_runKey));
            if (loaded == null) return;
            if (!AcceptStateVersion(loaded)) return;

            State.SchemaVersion = loaded.SchemaVersion;
            State.RunId = loaded.RunId;
            State.Revision = loaded.Revision;
            foreach (var id in loaded.CompletedStepIds) State.CompletedStepIds.Add(id);
            foreach (var id in loaded.SkippedStepIds) State.SkippedStepIds.Add(id);
            State.FailedStepId = loaded.FailedStepId;
            State.SchemaHash = loaded.SchemaHash;
            if (loaded.StartedAt.HasValue) State.StartedAt = loaded.StartedAt;
            State.LastUpdatedAt = loaded.LastUpdatedAt;
            if (loaded.CompletedSeederIds != null) State.CompletedSeederIds = loaded.CompletedSeederIds;
            if (loaded.Metadata != null)
                foreach (var kv in loaded.Metadata) State.Metadata[kv.Key] = kv.Value;
        }

        /// <summary>
        /// Version gate: an older document is accepted (and upgraded in place when a migration is
        /// added); a newer/unknown one is refused rather than reinterpreted — guessing at a newer
        /// shape risks re-running completed migrations. Single place so every store is consistent.
        /// </summary>
        private bool AcceptStateVersion(SetupState loaded)
        {
            if (loaded.SchemaVersion == 0) loaded.SchemaVersion = 1; // pre-versioning documents
            if (loaded.SchemaVersion == SetupState.CurrentSchemaVersion) return true;

            _logger?.LogError(
                "Persisted setup state has schemaVersion {Found}, {Rel} this build supports " +
                "({Supported}). Refusing to use it; remove the checkpoint or upgrade BeepDM.",
                loaded.SchemaVersion,
                loaded.SchemaVersion > SetupState.CurrentSchemaVersion ? "newer than" : "older and unmigratable by",
                SetupState.CurrentSchemaVersion);
            return false;
        }

        /// <summary>Persists <see cref="State"/> through the run store under the held lease.</summary>
        private void PersistState()
        {
            if (_runStore == null || _runKey == null) return;
            try
            {
                Bridge(() => _runStore.SaveAsync(_runKey, State, _runLease));
            }
            catch (SetupStateConflictException ex)
            {
                // Another runner advanced the shared state under us. Surface it; don't overwrite.
                _logger?.LogError(ex, "Could not persist setup state: concurrent modification detected.");
            }
        }

        /// <summary>
        /// Bridges an async store call from the synchronous wizard. <see cref="Task.Run{TResult}(System.Func{Task{TResult}})"/>
        /// moves the await off any caller SynchronizationContext, avoiding the sync-over-async
        /// deadlock (same pattern as DriverProvisionStep's NuGet load).
        /// </summary>
        private static T Bridge<T>(Func<Task<T>> f) => Task.Run(f).GetAwaiter().GetResult();
        private static void Bridge(Func<Task> f) => Task.Run(f).GetAwaiter().GetResult();

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// The context's options win outright over the wizard's own. A caller who sets DryRun on
        /// the builder but passes a context carrying a default SetupOptions gets a live run — the
        /// most expensive silent failure in this framework. Warn loudly when the two differ.
        /// </summary>
        private void WarnOnSilentOptionOverride(SetupOptions runOptions)
        {
            if (Options == null || runOptions == null || ReferenceEquals(Options, runOptions))
                return;

            if (Options.DryRun != runOptions.DryRun)
                _logger?.LogWarning(
                    "SetupContext.Options overrides the wizard's own options: DryRun={ContextDryRun} " +
                    "will be used, not DryRun={WizardDryRun}. Pass the SAME SetupOptions instance to " +
                    "the builder and the context.",
                    runOptions.DryRun, Options.DryRun);
            else
                _logger?.LogDebug(
                    "SetupContext.Options overrides the wizard's own options instance.");
        }

        /// <summary>
        /// Handles the common step-failure path: marks failed step, syncs state,
        /// builds report, persists checkpoint, and returns the error.
        /// </summary>
        private IErrorsInfo FailStep(SetupContext context, List<SetupStepResult> results,
            DateTimeOffset started, SetupOptions runOptions, string stepId,
            IErrorsInfo error, bool syncFromContext)
        {
            State.FailedStepId = stepId;
            if (syncFromContext) SyncFromContext(context);
            SyncToContext(context);

            // Auto-rollback is opt-in: undoing a partial setup can destroy the state a human needs
            // to diagnose the failure. When it runs, its outcome is recorded on the report.
            if (runOptions.AutoRollbackOnFailure)
                RunRollback(context, progress: null);

            _lastReport = BuildReport(results, false, started, runOptions.Environment, context, runOptions);
            PersistState();
            EmitAudit(Audit.SetupAuditAction.RunFailed, stepId, false, error?.Message,
                DateTimeOffset.UtcNow - started);
            return error;
        }

        /// <summary>
        /// Rolls back completed steps and stashes the report JSON on the context so
        /// <see cref="BuildReport"/> surfaces it via <see cref="SetupReport.RollbackReportJson"/>.
        /// </summary>
        private void RunRollback(SetupContext context, IProgress<PassedArgs> progress)
        {
            EmitAudit(Audit.SetupAuditAction.RollbackStarted, null, true, null);
            try
            {
                var report = Bridge(() => _rollback.RollbackAsync(_steps.AsReadOnly(), context, progress));
                context.Properties["RollbackReportJson"] =
                    JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
                _logger?.LogInformation("Auto-rollback {Outcome}: {Count} step(s) processed.",
                    report.Succeeded ? "succeeded" : "completed with failures", report.StepResults.Count);
                EmitAudit(Audit.SetupAuditAction.RollbackCompleted, null, report.Succeeded,
                    $"{report.StepResults.Count} step(s) processed.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Auto-rollback itself failed.");
                EmitAudit(Audit.SetupAuditAction.RollbackCompleted, null, false, ex.Message);
            }
        }

        /// <summary>
        /// Pulls state from context into wizard, then pushes back and persists.
        /// Used after every step that writes state (success, partial failure).
        /// </summary>
        private void SyncAndPersist(SetupContext context, SetupOptions runOptions)
        {
            SyncFromContext(context);
            SyncToContext(context);
            PersistState();
        }

        private SetupReport BuildReport(
            List<SetupStepResult> results,
            bool succeeded,
            DateTimeOffset started,
            string environment,
            SetupContext context = null,
            SetupOptions runOptions = null)
        {
            var finished = DateTimeOffset.UtcNow;
            var json = JsonSerializer.Serialize(results);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));

            var report = new SetupReport
            {
                WizardId = _wizardId,
                RunId = State.RunId,
                Succeeded = succeeded,
                StepResults = results.AsReadOnly(),
                StartedAt = started,
                FinishedAt = finished,
                ContentHash = hash,
                Environment = environment,
                // Steps record these on the context; without this bridge both fields are always
                // null on every report the wizard produces.
                DryRunReportJson = context?.TryGetDryRunReport(),
                RollbackReportJson = context?.TryGetProperty<string>("RollbackReportJson"),
                ActorId = State.ActorId,
                ActorAuthenticated = State.ActorAuthenticated
            };

            WriteReportIfConfigured(report, runOptions);
            return report;
        }

        /// <summary>
        /// Writes the report to <see cref="SetupOptions.ReportOutputPath"/> when set.
        /// Timestamped per run so a later run cannot erase an earlier record.
        /// </summary>
        private void WriteReportIfConfigured(SetupReport report, SetupOptions runOptions)
        {
            var dir = runOptions?.ReportOutputPath;
            if (string.IsNullOrWhiteSpace(dir)) return;

            try
            {
                Directory.CreateDirectory(dir);
                var stamp = report.FinishedAt.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'");
                var file = Path.Combine(dir, $"{_wizardId}-{report.RunId}-{stamp}.report.json");
                File.WriteAllText(file,
                    JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
                _logger?.LogInformation("Setup report written to {File}", file);
            }
            catch (Exception ex)
            {
                // Reporting is observability, not the operation. A failed write must not fail a
                // setup that otherwise succeeded.
                _logger?.LogWarning(ex, "Could not write setup report to {Dir}", dir);
            }
        }

        private IErrorsInfo ValidateStepDefinitions(out string failedStepId)
        {
            failedStepId = null;

            var knownStepIds = new HashSet<string>(StringComparer.Ordinal);
            var stepIndexById = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < _steps.Count; i++)
            {
                var step = _steps[i];
                if (step == null)
                    return Fail($"Step at index {i} is null. Ensure all registered steps are valid instances.");

                if (string.IsNullOrWhiteSpace(step.StepId))
                    return Fail($"Step '{step.StepName ?? "(unnamed)"}' has an empty StepId.");

                if (!knownStepIds.Add(step.StepId))
                {
                    failedStepId = step.StepId;
                    return Fail($"Duplicate StepId '{step.StepId}' detected. Step IDs must be unique.");
                }

                stepIndexById[step.StepId] = i;

                foreach (var dep in step.DependsOn ?? Array.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(dep))
                    {
                        failedStepId = step.StepId;
                        return Fail($"Step '{step.StepId}' contains an empty dependency ID.");
                    }
                }
            }

            foreach (var step in _steps)
            {
                foreach (var dep in step.DependsOn ?? Array.Empty<string>())
                {
                    if (string.Equals(dep, step.StepId, StringComparison.Ordinal))
                    {
                        failedStepId = step.StepId;
                        return Fail($"Step '{step.StepId}' cannot depend on itself.");
                    }

                    if (!knownStepIds.Contains(dep))
                    {
                        failedStepId = step.StepId;
                        return Fail($"Step '{step.StepId}' depends on unknown step '{dep}'.");
                    }

                    if (stepIndexById.TryGetValue(dep, out var depIdx) &&
                        stepIndexById.TryGetValue(step.StepId, out var stepIdx) &&
                        depIdx > stepIdx)
                    {
                        failedStepId = step.StepId;
                        return Fail($"Step '{step.StepId}' depends on '{dep}', but '{dep}' is registered after it. Reorder steps so dependencies appear first.");
                    }
                }
            }
 
            return Ok("Step definitions valid.");
        }

        // ── State sync helpers ────────────────────────────────────────────

        /// <summary>
        /// Merges step-level progress from <paramref name="context"/>.State into the
        /// wizard's own <see cref="State"/> so previously completed steps are not re-run.
        /// </summary>
        private void SyncFromContext(SetupContext context)
        {
            var src = context?.State;
            if (src == null) return;

            if (src.RunId != null && State.RunId == null)
                State.RunId = src.RunId;

            foreach (var id in src.CompletedStepIds) State.CompletedStepIds.Add(id);
            foreach (var id in src.SkippedStepIds)   State.SkippedStepIds.Add(id);

            // Always take the context value when non-null — context is the "more recent" source
            // on the success path (a step may have just written a new hash).  Using the old
            // "only when null" guard prevented the hash from advancing after the first run.
            if (src.SchemaHash != null)
                State.SchemaHash = src.SchemaHash;

            if (src.StartedAt.HasValue && !State.StartedAt.HasValue)
                State.StartedAt = src.StartedAt;

            // Propagate seeder completions so the wizard's persisted state is complete
            foreach (var id in src.CompletedSeederIds)
                State.CompletedSeederIds.Add(id);

            // Overwrite (not TryAdd) so that step-written updates to existing keys
            // (e.g. LastCheckpointId, MigrationPlanId, ExecutionToken) propagate correctly.
            // context.State is always the freshest source immediately after a step runs.
            foreach (var kv in src.Metadata)
                State.Metadata[kv.Key] = kv.Value;
        }

        /// <summary>
        /// Pushes all step-level bookkeeping from <see cref="State"/> into
        /// <paramref name="context"/>.State so every step reads current data.
        /// Call <see cref="SyncFromContext"/> first on the success path to pull any
        /// state written by the just-completed step before pushing back.
        /// </summary>
        private void SyncToContext(SetupContext context)
        {
            context.State ??= new SetupState();
            context.State.RunId               = State.RunId;
            context.State.CompletedStepIds   = State.CompletedStepIds;
            context.State.SkippedStepIds     = State.SkippedStepIds;
            context.State.FailedStepId       = State.FailedStepId;
            context.State.SchemaHash         = State.SchemaHash;
            context.State.StartedAt          = State.StartedAt;
            context.State.LastUpdatedAt      = State.LastUpdatedAt;

            // CompletedSeederIds MUST be synced: on resume, the wizard loads these IDs from the
            // checkpoint file into this.State, and SeedingStep reads context.State to skip them.
            // If we omit this line the seeder IDs never reach context and every seeder re-runs.
            context.State.CompletedSeederIds = State.CompletedSeederIds;

            // Overwrite (not TryAdd) so that updated values in State (e.g. after SyncFromContext
            // merged a step's new checkpoint token) replace any stale values in context.State.
            foreach (var kv in State.Metadata)
                context.State.Metadata[kv.Key] = kv.Value;
        }
    }
}
