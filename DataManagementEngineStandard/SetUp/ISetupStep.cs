using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// A single unit of work within a setup wizard.
    /// Implementations must not throw — surface all errors via <see cref="IErrorsInfo"/>.
    /// </summary>
    public interface ISetupStep
    {
        /// <summary>Stable, unique identifier for this step (e.g. "connection-config").</summary>
        string StepId { get; }

        /// <summary>Human-readable display name shown in progress UI.</summary>
        string StepName { get; }

        /// <summary>Optional description shown in wizard UI and reports.</summary>
        string Description { get; }

        /// <summary>Ordered list of StepIds that must complete before this step runs.</summary>
        IReadOnlyList<string> DependsOn { get; }

        /// <summary>
        /// Returns true when this step detects it has already been applied and can be
        /// safely skipped (idempotency guard).
        /// </summary>
        bool CanSkip(SetupContext context);

        /// <summary>
        /// Validate pre-conditions before <see cref="Execute"/> is called.
        /// Returns <c>Errors.Failed</c> if the step cannot safely run.
        /// </summary>
        IErrorsInfo Validate(SetupContext context);

        /// <summary>
        /// Execute the step.  Must return <c>Errors.Ok</c> on success.
        /// Must NOT throw; surface errors via <see cref="IErrorsInfo"/>.
        /// </summary>
        IErrorsInfo Execute(SetupContext context, System.IProgress<PassedArgs> progress = null);
    }
}
