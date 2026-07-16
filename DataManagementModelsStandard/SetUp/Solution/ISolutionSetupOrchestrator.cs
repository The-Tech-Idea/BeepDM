using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.AppMap;

namespace TheTechIdea.Beep.SetUp.Solution
{
    /// <summary>
    /// Sets up every app in a solution — the "single place" that provisions a whole solution rather
    /// than one app.
    /// </summary>
    /// <remarks>
    /// Apps run in dependency order. One app's failure does <b>not</b> abort the others; only apps
    /// that <em>depend</em> on a failed app are skipped. There is deliberately no cross-app
    /// distributed transaction — the report says honestly which apps succeeded and which need
    /// attention.
    /// </remarks>
    public interface ISolutionSetupOrchestrator
    {
        Task<SolutionSetupReport> SetupSolutionAsync(
            IReadOnlyList<AppDefinition> apps,
            string environmentId,
            Func<AppDefinition, SetupContext> contextFactory,
            SolutionSetupOptions options = null,
            IProgress<PassedArgs> progress = null,
            CancellationToken token = default);

        /// <summary>
        /// Reads each app's persisted setup state and reports where the solution stands — the
        /// "single place" status view. Does not run anything.
        /// </summary>
        Task<SolutionSetupStatus> GetStatusAsync(
            IReadOnlyList<AppDefinition> apps,
            string environmentId,
            CancellationToken token = default);
    }

    public sealed class SolutionSetupStatus
    {
        public string EnvironmentId { get; set; }
        public List<AppSetupStatus> Apps { get; set; } = new();
    }

    public sealed class AppSetupStatus
    {
        public string AppId { get; set; }
        public string AppName { get; set; }
        public SetupProgress Progress { get; set; }

        /// <summary>Completed step ids from the persisted checkpoint, if any.</summary>
        public IReadOnlyList<string> CompletedSteps { get; set; } = System.Array.Empty<string>();

        public string FailedStepId { get; set; }
    }

    public enum SetupProgress
    {
        NotSetUp,
        InProgress,
        Failed,
        Complete
    }

    public sealed class SolutionSetupOptions
    {
        /// <summary>
        /// Inter-app dependencies: appId → the appIds it depends on. Apps whose dependencies fail
        /// are skipped. When null/empty, apps run in the order supplied.
        /// </summary>
        /// <remarks>
        /// AppMap models project-level references, not inter-app order, so this is supplied
        /// explicitly rather than derived — honest about what the data actually contains.
        /// </remarks>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> Dependencies { get; set; }

        /// <summary>Stop the whole solution on the first app failure instead of continuing. Default false.</summary>
        public bool StopOnFirstFailure { get; set; }
    }

    public sealed class SolutionSetupReport
    {
        public string EnvironmentId { get; set; }
        public bool Succeeded { get; set; }
        public List<AppSetupResult> Apps { get; set; } = new();
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset FinishedAt { get; set; }
    }

    public sealed class AppSetupResult
    {
        public string AppId { get; set; }
        public string AppName { get; set; }
        public AppSetupOutcome Outcome { get; set; }
        public string Message { get; set; }

        /// <summary>The per-app wizard report, when the app actually ran.</summary>
        public SetupReport Report { get; set; }
    }

    public enum AppSetupOutcome
    {
        Succeeded,
        Failed,

        /// <summary>Skipped because a dependency failed or was itself skipped.</summary>
        SkippedDependencyFailed,

        /// <summary>Skipped because the app has no setup definition.</summary>
        SkippedNoDefinition
    }
}
