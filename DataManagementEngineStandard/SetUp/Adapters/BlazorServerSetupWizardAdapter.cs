using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.SetUp.Adapters
{
    /// <summary>
    /// Base adapter for Blazor Server applications.
    ///
    /// This class compiles without Blazor references and provides the core run logic.
    /// In your Blazor Server project, subclass this and inject
    /// <c>IHubContext&lt;SetupProgressHub&gt;</c> to push real-time progress via SignalR:
    ///
    /// <code>
    /// public class MyBlazorSetupAdapter : BlazorServerSetupWizardAdapter
    /// {
    ///     private readonly IHubContext&lt;SetupProgressHub&gt; _hub;
    ///     public MyBlazorSetupAdapter(IHubContext&lt;SetupProgressHub&gt; hub) => _hub = hub;
    ///
    ///     protected override void OnProgress(PassedArgs args)
    ///         => _ = _hub.Clients.All.SendAsync("SetupProgress",
    ///                new { pct = args.ParameterInt1, msg = args.Messege });
    ///
    ///     protected override void OnComplete(SetupReport report)
    ///         => _ = _hub.Clients.All.SendAsync("SetupComplete",
    ///                new { succeeded = report.Succeeded, hash = report.ContentHash });
    /// }
    /// </code>
    /// </summary>
    public class BlazorServerSetupWizardAdapter : ISetupWizardAdapter
    {
        /// <inheritdoc/>
        public async Task<SetupReport> RunAsync(
            ISetupWizard wizard, SetupContext context,
            CancellationToken cancellationToken = default)
        {
            var progress = new Progress<PassedArgs>(args => OnProgress(args));

            try
            {
                await Task.Run(() => wizard.Run(context, progress), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                OnProgress(new PassedArgs { ParameterInt1 = 0, Messege = "Setup wizard cancelled." });
                throw;
            }

            var report = wizard.GetReport();
            OnComplete(report);
            return report;
        }

        /// <inheritdoc/>
        public void ShowStep(ISetupStep step, int stepIndex, int totalSteps) { }

        /// <inheritdoc/>
        public void ShowProgress(string stepId, int percentComplete, string message) { }

        /// <inheritdoc/>
        public void ShowResult(SetupReport report) { }

        // ── Extension points ─────────────────────────────────────────────────

        /// <summary>Override to push progress updates to connected clients (e.g. SignalR).</summary>
        protected virtual void OnProgress(PassedArgs args) { }

        /// <summary>Override to notify clients when the wizard completes.</summary>
        protected virtual void OnComplete(SetupReport report) { }
    }
}
