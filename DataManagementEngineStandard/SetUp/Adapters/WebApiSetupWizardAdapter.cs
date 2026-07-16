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
    public class WebApiSetupWizardAdapter : SetupWizardAdapterBase
    {
        /// <summary>Live status updated throughout the wizard run.</summary>
        public SetupAdapterStatus Status { get; } = new SetupAdapterStatus();

        /// <summary>
        /// Wraps the shared run to preserve this adapter's never-null contract — a polling HTTP
        /// endpoint must always get a report body, even when the wizard produced none.
        /// </summary>
        public override async Task<SetupReport> RunAsync(ISetupWizard wizard, SetupContext context,
            CancellationToken cancellationToken = default)
        {
            var report = await base.RunAsync(wizard, context, cancellationToken).ConfigureAwait(false);
            return report ?? new SetupReport { Succeeded = false };
        }

        /// <inheritdoc/>
        protected override Task OnRunStartingAsync(ISetupWizard wizard, SetupContext context)
        {
            Status.State = "Running";
            Status.TotalSteps = wizard.Steps.Count;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override void ReportProgress(ISetupWizard wizard, SetupContext context, PassedArgs args)
        {
            Status.CurrentMessage = args?.Messege;
            Status.PercentComplete = args?.ParameterInt1 ?? 0;
        }

        /// <inheritdoc/>
        protected override Task OnCancelledAsync(SetupContext context)
        {
            Status.State = "Cancelled";
            Status.CurrentMessage = "Setup wizard cancelled.";
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override Task OnFailedAsync(Exception ex, SetupContext context)
        {
            Status.State = "Failed";
            Status.CurrentMessage = ex.Message;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override Task OnCompletedAsync(SetupReport report, SetupContext context)
        {
            // Only resolve the terminal state if a hook hasn't already set one.
            if (Status.State == "Running")
                Status.State = report?.Succeeded == true ? "Completed" : "Failed";
            Status.Report = report;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override void ShowStep(ISetupStep step, int stepIndex, int totalSteps)
        {
            Status.CurrentStepName = step?.StepName;
            Status.CurrentStepIndex = stepIndex;
            Status.TotalSteps = totalSteps;
        }

        /// <inheritdoc/>
        public override void ShowProgress(string stepId, int percentComplete, string message)
        {
            Status.PercentComplete = percentComplete;
            Status.CurrentMessage = message;
        }

        /// <inheritdoc/>
        public override void ShowResult(SetupReport report) => Status.Report = report;
    }
}
