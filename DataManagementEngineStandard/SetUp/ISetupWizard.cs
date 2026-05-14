using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Orchestration contract for running a sequence of <see cref="ISetupStep"/>s.
    /// </summary>
    public interface ISetupWizard
    {
        /// <summary>Ordered list of steps to execute.</summary>
        IReadOnlyList<ISetupStep> Steps { get; }

        /// <summary>Current runtime state (may be resumed from a saved checkpoint).</summary>
        SetupState State { get; }

        /// <summary>Options controlling dry-run, skip-seed, environment, etc.</summary>
        SetupOptions Options { get; }

        /// <summary>
        /// Run all pending steps in order. Idempotent: already-done steps are skipped.
        /// Returns <c>Errors.Ok</c> when all steps succeed.
        /// </summary>
        IErrorsInfo Run(SetupContext context, System.IProgress<PassedArgs> progress = null);

        /// <summary>
        /// Resume from the last persisted checkpoint.
        /// Equivalent to <see cref="Run"/> but skips steps recorded as done in <see cref="State"/>.
        /// </summary>
        IErrorsInfo Resume(SetupContext context, System.IProgress<PassedArgs> progress = null);

        /// <summary>Returns the report for the most recent Run/Resume call.</summary>
        SetupReport GetReport();
    }
}
