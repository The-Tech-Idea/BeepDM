using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Per-step outcome record included in <see cref="SetupReport"/>.
    /// </summary>
    public class SetupStepResult
    {
        public string StepId { get; init; }
        public string StepName { get; init; }
        public bool Succeeded { get; init; }
        public bool Skipped { get; init; }
        public string Message { get; init; }
        public TimeSpan Elapsed { get; init; }
        public DateTimeOffset ExecutedAt { get; init; }
    }

    /// <summary>
    /// Immutable outcome record produced after a wizard run.
    /// Contains per-step results, timing, and a SHA-256 content hash.
    /// </summary>
    public class SetupReport
    {
        public string WizardId { get; init; }
        public string RunId { get; init; }
        public bool Succeeded { get; init; }
        public IReadOnlyList<SetupStepResult> StepResults { get; init; }
        public DateTimeOffset StartedAt { get; init; }
        public DateTimeOffset FinishedAt { get; init; }
        public TimeSpan TotalElapsed => FinishedAt - StartedAt;
        public string ContentHash { get; init; }
        public string RollbackReportJson { get; init; }
        public string DryRunReportJson { get; init; }
        public string Environment { get; init; }

        /// <summary>Principal that ran the setup (Phase 5).</summary>
        public string ActorId { get; init; }
        public bool ActorAuthenticated { get; init; }
    }
}
