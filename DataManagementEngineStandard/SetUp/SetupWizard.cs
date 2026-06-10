using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Core orchestrator — runs registered <see cref="ISetupStep"/>s in sequence.
    /// Checkpoints state after each step so execution can be resumed with <see cref="Resume"/>.
    ///
    /// State management
    /// ─────────────────
    /// The wizard owns <see cref="State"/> (used for step-level bookkeeping) and keeps it
    /// in sync with <see cref="SetupContext.State"/> at the start of each run and after
    /// every step.  Steps may write to <c>context.State</c> freely; the wizard merges
    /// those writes back into its own <see cref="State"/> before persisting.
    /// </summary>
    public class SetupWizard : ISetupWizard
    {
        private const int StateIoRetryCount = 5;
        private const int StateIoRetryDelayMs = 30;
        private static readonly ConcurrentDictionary<string, object> _stateFileLocks =
            new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private readonly string _wizardId;
        private readonly List<ISetupStep> _steps;
        private SetupReport _lastReport;

        /// <inheritdoc/>
        public IReadOnlyList<ISetupStep> Steps => _steps.AsReadOnly();

        /// <inheritdoc/>
        public SetupState State { get; private set; } = new SetupState();

        /// <inheritdoc/>
        public SetupOptions Options { get; }

        public SetupWizard(string wizardId, IEnumerable<ISetupStep> steps, SetupOptions options)
        {
            _wizardId = wizardId ?? "default-setup";
            _steps = new List<ISetupStep>(steps);
            Options = options ?? new SetupOptions();
        }

        /// <inheritdoc/>
        public IErrorsInfo Run(SetupContext context, IProgress<PassedArgs> progress = null)
        {
            if (context == null)
                return new ErrorsInfo { Flag = Errors.Failed, Message = "SetupContext must not be null." };

            var runOptions = context.Options ?? Options;
            context.Options = runOptions;

            // Load persisted checkpoint if state is empty and a file path is configured
            LoadPersistedState(runOptions);

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
            {
                State.FailedStepId = invalidStepId;
                SyncToContext(context);
                _lastReport = BuildReport(results, false, started, runOptions.Environment);
                PersistState(runOptions);
                return stepValidation;
            }

            for (int i = 0; i < total; i++)
            {
                var step = _steps[i];
                if (State.IsStepCompleted(step.StepId)) continue;

                // ── Runtime DependsOn guard ──────────────────────────────────
                foreach (var dep in step.DependsOn ?? Array.Empty<string>())
                {
                    if (!State.IsStepCompleted(dep))
                    {
                        var depErr = new ErrorsInfo
                        {
                            Flag = Errors.Failed,
                            Message = $"Step '{step.StepId}' requires '{dep}' to complete first, " +
                                       $"but '{dep}' has not been completed or skipped. " +
                                       "Check step registration order."
                        };
                        State.FailedStepId = step.StepId;
                        SyncToContext(context);
                        _lastReport = BuildReport(results, false, started, runOptions.Environment);
                        PersistState(runOptions);
                        return depErr;
                    }
                }

                try
                {
                    // ── Validate before executing ────────────────────────────────
                    var validation = step.Validate(context);
                    if (validation.Flag == Errors.Failed)
                    {
                        State.FailedStepId = step.StepId;
                        SyncToContext(context);
                        _lastReport = BuildReport(results, false, started, runOptions.Environment);
                        PersistState(runOptions);
                        return validation;
                    }

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
                        SyncToContext(context);
                        PersistState(runOptions);
                        continue;
                    }

                    // ── Execute ──────────────────────────────────────────────────
                    context.ProgressReporter?.ReportStepStart(step.StepId, step.StepName, i + 1, total);
                    progress?.Report(new PassedArgs
                    {
                        ParameterInt1 = (int)((i / (double)total) * 100),
                        Messege = $"Running: {step.StepName}"
                    });

                    // Ensure context.State is fully current before the step reads it.
                    // Critical for SeedingStep resume: CompletedSeederIds from the persisted
                    // checkpoint must be visible in context before Execute begins.
                    SyncToContext(context);

                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var result = step.Execute(context, progress);
                    sw.Stop();

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
                        State.FailedStepId = step.StepId;
                        // Pull any partial progress written by the failing step (e.g. seeder IDs)
                        SyncFromContext(context);
                        SyncToContext(context);
                        _lastReport = BuildReport(results, false, started, runOptions.Environment);
                        PersistState(runOptions);
                        return result;
                    }

                    State.CompletedStepIds.Add(step.StepId);
                    State.LastUpdatedAt = DateTimeOffset.UtcNow;
                    // Pull state changes written by the step (e.g. CompletedSeederIds from SeedingStep)
                    // before pushing the merged state back to context.
                    SyncFromContext(context);
                    SyncToContext(context);
                    PersistState(runOptions);
                }
                catch (OperationCanceledException)
                {
                    State.FailedStepId = step.StepId;
                    SyncFromContext(context);
                    SyncToContext(context);
                    _lastReport = BuildReport(results, false, started, runOptions.Environment);
                    PersistState(runOptions);
                    throw;
                }
                catch (Exception ex)
                {
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

                    SyncFromContext(context);
                    SyncToContext(context);
                    _lastReport = BuildReport(results, false, started, runOptions.Environment);
                    PersistState(runOptions);

                    return new ErrorsInfo
                    {
                        Flag = Errors.Failed,
                        Message = $"Step '{step.StepId}' threw an unhandled exception.",
                        Ex = ex
                    };
                }
            }

            progress?.Report(new PassedArgs { ParameterInt1 = 100, Messege = "Setup completed." });

            _lastReport = BuildReport(results, true, started, runOptions.Environment);
            context.ProgressReporter?.ReportWizardComplete(_lastReport);

            // Persist final state/report outcome even when no step executed in this run
            // (for example: all steps already completed when resuming from checkpoint).
            SyncToContext(context);
            PersistState(runOptions);

            return new ErrorsInfo { Flag = Errors.Ok, Message = "Setup completed successfully." };
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
                return new ErrorsInfo { Flag = Errors.Failed, Message = "SetupContext must not be null." };

            // Force-load persisted state even if the wizard already has some in-memory state
            LoadPersistedState(context.Options, force: true);
            return Run(context, progress);
        }

        /// <inheritdoc/>
        public SetupReport GetReport() => _lastReport;

        // ── Helpers ──────────────────────────────────────────────────────────

        private SetupReport BuildReport(
            List<SetupStepResult> results,
            bool succeeded,
            DateTimeOffset started,
            string environment)
        {
            var finished = DateTimeOffset.UtcNow;
            var json = JsonSerializer.Serialize(results);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));

            return new SetupReport
            {
                WizardId = _wizardId,
                Succeeded = succeeded,
                StepResults = results.AsReadOnly(),
                StartedAt = started,
                FinishedAt = finished,
                ContentHash = hash,
                Environment = environment
            };
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
                    return new ErrorsInfo
                    {
                        Flag = Errors.Failed,
                        Message = $"Step at index {i} is null. Ensure all registered steps are valid instances."
                    };

                if (string.IsNullOrWhiteSpace(step.StepId))
                    return new ErrorsInfo
                    {
                        Flag = Errors.Failed,
                        Message = $"Step '{step.StepName ?? "(unnamed)"}' has an empty StepId."
                    };

                if (!knownStepIds.Add(step.StepId))
                {
                    failedStepId = step.StepId;
                    return new ErrorsInfo
                    {
                        Flag = Errors.Failed,
                        Message = $"Duplicate StepId '{step.StepId}' detected. Step IDs must be unique."
                    };
                }

                stepIndexById[step.StepId] = i;

                foreach (var dep in step.DependsOn ?? Array.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(dep))
                    {
                        failedStepId = step.StepId;
                        return new ErrorsInfo
                        {
                            Flag = Errors.Failed,
                            Message = $"Step '{step.StepId}' contains an empty dependency ID."
                        };
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
                        return new ErrorsInfo
                        {
                            Flag = Errors.Failed,
                            Message = $"Step '{step.StepId}' cannot depend on itself."
                        };
                    }

                    if (!knownStepIds.Contains(dep))
                    {
                        failedStepId = step.StepId;
                        return new ErrorsInfo
                        {
                            Flag = Errors.Failed,
                            Message = $"Step '{step.StepId}' depends on unknown step '{dep}'."
                        };
                    }

                    // SetupWizard executes in registration order and requires dependencies
                    // to be already completed before a step runs. Reject forward dependencies
                    // up-front to avoid runtime dependency failures on fresh runs.
                    if (stepIndexById.TryGetValue(dep, out var depIdx) &&
                        stepIndexById.TryGetValue(step.StepId, out var stepIdx) &&
                        depIdx > stepIdx)
                    {
                        failedStepId = step.StepId;
                        return new ErrorsInfo
                        {
                            Flag = Errors.Failed,
                            Message = $"Step '{step.StepId}' depends on '{dep}', but '{dep}' is registered after it. Reorder steps so dependencies appear first."
                        };
                    }
                }
            }

            return new ErrorsInfo { Flag = Errors.Ok, Message = "Step definitions valid." };
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

        private void LoadPersistedState(SetupOptions opts, bool force = false)
        {
            if (string.IsNullOrEmpty(opts?.StateFilePath)) return;
            if (!force && State.CompletedStepIds.Count > 0) return; // already have in-memory state

            var statePath = opts.StateFilePath;
            var stateLock = _stateFileLocks.GetOrAdd(statePath, _ => new object());

            lock (stateLock)
            {
                // NOTE: Manual retry loop. NOT migrated to IRetryPipeline because the loop
                // discriminates between three exception types with different control flow
                // (retry on IOException, retry on UnauthorizedAccessException, return-without-
                // throw on any other exception to "start fresh"). The classifier would have to
                // inspect the exception type, decide retry/giveup, AND distinguish "giveup =
                // return" from "giveup = throw" — that's not what IRetryPipeline models. The
                // `when (attempt < StateIoRetryCount - 1)` clause on the first two catches is
                // also load-bearing: it lets the final attempt's IOException fall through to
                // the generic catch. Translating that into pipeline hooks would obscure it.
                for (int attempt = 0; attempt < StateIoRetryCount; attempt++)
                {
                    try
                    {
                        if (!File.Exists(statePath)) return;

                        // Open with Delete sharing so concurrent atomic replace/move writes are not blocked.
                        var json = ReadAllTextWithSharedDelete(statePath);
                        var loaded = JsonSerializer.Deserialize<SetupState>(json);
                        if (loaded == null) return;

                        // Replace the wizard's state with the persisted snapshot
                        State = loaded;
                        return;
                    }
                    catch (IOException) when (attempt < StateIoRetryCount - 1)
                    {
                        Thread.Sleep(StateIoRetryDelayMs);
                    }
                    catch (UnauthorizedAccessException) when (attempt < StateIoRetryCount - 1)
                    {
                        Thread.Sleep(StateIoRetryDelayMs);
                    }
                    catch
                    {
                        // Corrupt / unreadable checkpoint — start fresh.
                        return;
                    }
                }
            }
        }

        private void PersistState(SetupOptions opts)
        {
            if (string.IsNullOrEmpty(opts?.StateFilePath)) return;

            var statePath = opts.StateFilePath;
            var stateLock = _stateFileLocks.GetOrAdd(statePath, _ => new object());

            lock (stateLock)
            {
                var dir = Path.GetDirectoryName(statePath);
                var targetDir = string.IsNullOrEmpty(dir) ? "." : dir;
                Directory.CreateDirectory(targetDir);

                var tmp = Path.Combine(targetDir, Path.GetRandomFileName() + ".tmp");

                try
                {
                    var json = JsonSerializer.Serialize(State,
                        new JsonSerializerOptions { WriteIndented = false });

                    // Write the new snapshot to a temp file first.
                    File.WriteAllText(tmp, json, new UTF8Encoding(false));

                    // Retry replace/move to tolerate short-lived sharing violations when
                    // multiple processes target the same state file concurrently.
                    //
                    // NOTE: Manual retry loop. NOT migrated to IRetryPipeline for the same
                    // reason as LoadPersistedState's loop above: the `when` clauses distinguish
                    // retry-eligible exceptions from "give up silently" cases, and the
                    // try/finally block around it (which deletes the temp file regardless of
                    // retry outcome) is cleaner inline than as pipeline hooks. The persist
                    // path is best-effort, so a pipeline "giveup = rethrow" semantic would
                    // actually change behavior here (the outer try/catch swallows throws).
                    for (int attempt = 0; attempt < StateIoRetryCount; attempt++)
                    {
                        try
                        {
                            File.Move(tmp, statePath, overwrite: true);
                            return;
                        }
                        catch (IOException) when (attempt < StateIoRetryCount - 1)
                        {
                            Thread.Sleep(StateIoRetryDelayMs);
                        }
                        catch (UnauthorizedAccessException) when (attempt < StateIoRetryCount - 1)
                        {
                            Thread.Sleep(StateIoRetryDelayMs);
                        }
                    }
                }
                catch
                {
                    // State persistence is best-effort; do not propagate file-system errors.
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tmp)) File.Delete(tmp);
                    }
                    catch
                    {
                        // Ignore temp cleanup failures.
                    }
                }
            }
        }

        private static string ReadAllTextWithSharedDelete(string path)
        {
            using var fs = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return sr.ReadToEnd();
        }
    }
}
