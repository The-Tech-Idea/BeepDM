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
    public class BlazorServerSetupWizardAdapter : SetupWizardAdapterBase
    {
        /// <summary>Routes progress to this adapter's <see cref="OnProgress"/> extension point.</summary>
        protected override void ReportProgress(ISetupWizard wizard, SetupContext context, PassedArgs args)
            => OnProgress(args);

        /// <inheritdoc/>
        protected override Task OnCancelledAsync(SetupContext context)
        {
            OnProgress(new PassedArgs { ParameterInt1 = 0, Messege = "Setup wizard cancelled." });
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override Task OnFailedAsync(Exception ex, SetupContext context)
        {
            OnProgress(new PassedArgs { ParameterInt1 = 0, Messege = $"Setup wizard failed: {ex.Message}" });
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override Task OnCompletedAsync(SetupReport report, SetupContext context)
        {
            OnComplete(report);
            return Task.CompletedTask;
        }

        // ── Extension points ─────────────────────────────────────────────────

        /// <summary>Override to push progress updates to connected clients (e.g. SignalR).</summary>
        protected virtual void OnProgress(PassedArgs args) { }

        /// <summary>Override to notify clients when the wizard completes.</summary>
        protected virtual void OnComplete(SetupReport report) { }
    }
}
