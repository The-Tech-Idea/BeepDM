using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.SetUp.Adapters
{
    /// <summary>
    /// Runs the setup wizard on a background thread and exposes status via
    /// <see cref="Status"/> so a polling endpoint (e.g. <c>GET /api/setup/status</c>)
    /// can report progress to web clients.
    ///
    /// Register as <c>Singleton</c> in ASP.NET Core DI and expose <see cref="Status"/>
    /// through a minimal-API endpoint.
    /// </summary>
    public class WebApiSetupWizardAdapter : ISetupWizardAdapter
    {
        /// <summary>Live status updated throughout the wizard run.</summary>
        public SetupAdapterStatus Status { get; } = new SetupAdapterStatus();

        /// <inheritdoc/>
        public async Task<SetupReport> RunAsync(
            ISetupWizard wizard, SetupContext context,
            CancellationToken cancellationToken = default)
        {
            Status.State = "Running";
            Status.TotalSteps = wizard.Steps.Count;

            var progress = new Progress<PassedArgs>(args =>
            {
                Status.CurrentMessage = args.Messege;
                Status.PercentComplete = args.ParameterInt1;
            });

            try
            {
                await Task.Run(() => wizard.Run(context, progress), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Status.State = "Cancelled";
                Status.CurrentMessage = "Setup wizard cancelled.";
                // Fall through — wizard already built a partial report.
            }
            catch (Exception ex)
            {
                Status.State = "Failed";
                Status.CurrentMessage = ex.Message;
                // Fall through — wizard may have a partial report.
            }

            var report = wizard.GetReport();
            if (Status.State == "Running")
                Status.State = report?.Succeeded == true ? "Completed" : "Failed";
            Status.Report = report;
            return report ?? new SetupReport { Succeeded = false };
        }

        /// <inheritdoc/>
        public void ShowStep(ISetupStep step, int stepIndex, int totalSteps)
        {
            Status.CurrentStepName = step?.StepName;
            Status.CurrentStepIndex = stepIndex;
            Status.TotalSteps = totalSteps;
        }

        /// <inheritdoc/>
        public void ShowProgress(string stepId, int percentComplete, string message)
        {
            Status.PercentComplete = percentComplete;
            Status.CurrentMessage = message;
        }

        /// <inheritdoc/>
        public void ShowResult(SetupReport report) => Status.Report = report;
    }
}
