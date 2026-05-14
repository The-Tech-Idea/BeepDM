using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Per-step outcome record included in <see cref="SetupReport"/>.
    /// </summary>
    public class SetupStepResult
    {
        public string StepId { get; set; }
        public string StepName { get; set; }
        public bool Succeeded { get; set; }
        public bool Skipped { get; set; }
        public string Message { get; set; }
        public TimeSpan Elapsed { get; set; }
        public DateTimeOffset ExecutedAt { get; set; }
    }

    /// <summary>
    /// Immutable outcome record produced after a wizard run.
    /// Contains per-step results, timing, and a SHA-256 content hash.
    /// </summary>
    public class SetupReport
    {
        /// <summary>Wizard identifier from <see cref="SetupWizardBuilder.WithId"/>.</summary>
        public string WizardId { get; set; }

        /// <summary>Unique run identifier generated at report creation time.</summary>
        public string RunId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>True when all steps succeeded or were intentionally skipped.</summary>
        public bool Succeeded { get; set; }

        /// <summary>Per-step results in execution order.</summary>
        public IReadOnlyList<SetupStepResult> StepResults { get; set; }

        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset FinishedAt { get; set; }
        public TimeSpan TotalElapsed => FinishedAt - StartedAt;

        /// <summary>SHA-256 hex digest of the serialized StepResults for tamper detection.</summary>
        public string ContentHash { get; set; }

        /// <summary>Populated when a rollback was executed (Phase 6).</summary>
        public string RollbackReportJson { get; set; }

        /// <summary>Populated in dry-run mode (Phase 3/8).</summary>
        public string DryRunReportJson { get; set; }

        /// <summary>Target environment label at run time.</summary>
        public string Environment { get; set; }
    }
}
