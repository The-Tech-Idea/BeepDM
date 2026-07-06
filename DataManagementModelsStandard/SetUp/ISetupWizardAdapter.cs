using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Platform adapter that bridges <see cref="ISetupWizard"/> to a specific UI/runtime
    /// (WinForms, Blazor, CLI, MAUI, Web API).
    /// </summary>
    public interface ISetupWizardAdapter
    {
        /// <summary>Run the wizard within this platform's execution model.</summary>
        Task<SetupReport> RunAsync(ISetupWizard wizard, SetupContext context,
            CancellationToken cancellationToken = default);

        /// <summary>Show current step information to the user.</summary>
        void ShowStep(ISetupStep step, int stepIndex, int totalSteps);

        /// <summary>Update the progress indicator for the current step.</summary>
        void ShowProgress(string stepId, int percentComplete, string message);

        /// <summary>Display the final setup result to the user.</summary>
        void ShowResult(SetupReport report);
    }
}
