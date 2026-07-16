using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.AppMap;
using TheTechIdea.Beep.SetUp.Solution;

namespace TheTechIdea.Beep.SetUp.Solution
{
    /// <summary>Default <see cref="ISolutionSetupOrchestrator"/>.</summary>
    public sealed class SolutionSetupOrchestrator : ISolutionSetupOrchestrator
    {
        private readonly ISetupWizardResolver _resolver;
        private readonly State.ISetupStateStore _stateStore;
        private readonly ILogger _logger;

        public SolutionSetupOrchestrator(ISetupWizardResolver resolver,
            State.ISetupStateStore stateStore = null, ILogger logger = null)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _stateStore = stateStore;
            _logger = logger;
        }

        public async Task<SolutionSetupStatus> GetStatusAsync(
            IReadOnlyList<AppDefinition> apps, string environmentId, CancellationToken token = default)
        {
            if (apps == null) throw new ArgumentNullException(nameof(apps));

            var status = new SolutionSetupStatus { EnvironmentId = environmentId };

            foreach (var app in apps)
            {
                var appStatus = new AppSetupStatus { AppId = app.Id, AppName = app.Name };

                var key = await _resolver.GetStateKeyAsync(app, environmentId, token).ConfigureAwait(false);
                var state = key != null && _stateStore != null
                    ? await _stateStore.LoadAsync(key, token).ConfigureAwait(false)
                    : null;

                if (key == null)
                    appStatus.Progress = SetupProgress.NotSetUp;   // no definition
                else if (state == null)
                    appStatus.Progress = SetupProgress.NotSetUp;   // never run
                else
                {
                    appStatus.CompletedSteps = state.CompletedStepIds?.ToList() ?? new List<string>();
                    appStatus.FailedStepId = state.FailedStepId;
                    appStatus.Progress = !string.IsNullOrEmpty(state.FailedStepId)
                        ? SetupProgress.Failed
                        : (state.CompletedStepIds?.Count > 0 ? SetupProgress.Complete : SetupProgress.InProgress);
                }

                status.Apps.Add(appStatus);
            }

            return status;
        }

        public async Task<SolutionSetupReport> SetupSolutionAsync(
            IReadOnlyList<AppDefinition> apps,
            string environmentId,
            Func<AppDefinition, SetupContext> contextFactory,
            SolutionSetupOptions options = null,
            IProgress<PassedArgs> progress = null,
            CancellationToken token = default)
        {
            if (apps == null) throw new ArgumentNullException(nameof(apps));
            if (contextFactory == null) throw new ArgumentNullException(nameof(contextFactory));
            options ??= new SolutionSetupOptions();

            var report = new SolutionSetupReport
            {
                EnvironmentId = environmentId,
                StartedAt = DateTimeOffset.UtcNow,
                Succeeded = true
            };

            var ordered = OrderApps(apps, options.Dependencies);
            var deps = options.Dependencies ?? new Dictionary<string, IReadOnlyList<string>>();

            // Outcome per app id, so a later app can see whether its dependencies succeeded.
            var outcomeById = new Dictionary<string, AppSetupOutcome>(StringComparer.Ordinal);

            foreach (var app in ordered)
            {
                token.ThrowIfCancellationRequested();

                // Skip when any dependency didn't succeed — don't set up on top of a broken prereq.
                var failedDep = deps.TryGetValue(app.Id, out var appDeps)
                    ? appDeps.FirstOrDefault(d => !outcomeById.TryGetValue(d, out var o) || o != AppSetupOutcome.Succeeded)
                    : null;

                if (failedDep != null)
                {
                    report.Succeeded = false;
                    outcomeById[app.Id] = AppSetupOutcome.SkippedDependencyFailed;
                    report.Apps.Add(new AppSetupResult
                    {
                        AppId = app.Id, AppName = app.Name,
                        Outcome = AppSetupOutcome.SkippedDependencyFailed,
                        Message = $"Skipped: dependency '{failedDep}' did not succeed."
                    });
                    continue;
                }

                progress?.Report(new PassedArgs { Messege = $"Setting up app '{app.Name}'…" });

                var result = await SetupOneAsync(app, environmentId, contextFactory, token).ConfigureAwait(false);
                report.Apps.Add(result);
                outcomeById[app.Id] = result.Outcome;

                if (result.Outcome == AppSetupOutcome.Failed)
                {
                    report.Succeeded = false;
                    if (options.StopOnFirstFailure)
                    {
                        _logger?.LogWarning("Stopping solution setup after '{App}' failed (StopOnFirstFailure).", app.Id);
                        break;
                    }
                }
            }

            report.FinishedAt = DateTimeOffset.UtcNow;
            return report;
        }

        private async Task<AppSetupResult> SetupOneAsync(AppDefinition app, string environmentId,
            Func<AppDefinition, SetupContext> contextFactory, CancellationToken token)
        {
            var result = new AppSetupResult { AppId = app.Id, AppName = app.Name };
            try
            {
                var wizard = await _resolver.ResolveAsync(app, environmentId, token).ConfigureAwait(false);
                if (wizard == null)
                {
                    result.Outcome = AppSetupOutcome.SkippedNoDefinition;
                    result.Message = "App has no setup definition.";
                    return result;
                }

                var context = contextFactory(app);
                var error = await wizard.RunAsync(context, null, token).ConfigureAwait(false);
                result.Report = wizard.GetReport();
                result.Message = error?.Message;
                result.Outcome = error != null && error.Flag == Errors.Failed
                    ? AppSetupOutcome.Failed
                    : AppSetupOutcome.Succeeded;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Setup of app '{App}' threw.", app.Id);
                result.Outcome = AppSetupOutcome.Failed;
                result.Message = $"Threw: {ex.Message}";
            }
            return result;
        }

        /// <summary>
        /// Topological order over inter-app dependencies (Kahn). Falls back to input order for apps
        /// not in the graph, and — for a cycle — leaves the remaining apps in input order rather than
        /// dropping them (the wizard-level dependency checks still guard correctness).
        /// </summary>
        private static List<AppDefinition> OrderApps(
            IReadOnlyList<AppDefinition> apps,
            IReadOnlyDictionary<string, IReadOnlyList<string>> dependencies)
        {
            if (dependencies == null || dependencies.Count == 0)
                return apps.ToList();

            var byId = apps.ToDictionary(a => a.Id, a => a, StringComparer.Ordinal);
            var inDegree = apps.ToDictionary(a => a.Id, _ => 0, StringComparer.Ordinal);
            var dependents = apps.ToDictionary(a => a.Id, _ => new List<string>(), StringComparer.Ordinal);

            foreach (var a in apps)
            {
                if (!dependencies.TryGetValue(a.Id, out var d)) continue;
                foreach (var dep in d.Where(x => byId.ContainsKey(x)))
                {
                    dependents[dep].Add(a.Id);
                    inDegree[a.Id]++;
                }
            }

            // Seed with dependency-free apps, preserving input order among them for determinism.
            var queue = new Queue<string>(apps.Where(a => inDegree[a.Id] == 0).Select(a => a.Id));
            var sorted = new List<AppDefinition>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                if (!seen.Add(id)) continue;
                sorted.Add(byId[id]);
                foreach (var dep in dependents[id])
                    if (--inDegree[dep] == 0) queue.Enqueue(dep);
            }

            // Any app left over is in a cycle — append in input order so it still gets attempted.
            foreach (var a in apps)
                if (seen.Add(a.Id)) sorted.Add(a);

            return sorted;
        }
    }
}
