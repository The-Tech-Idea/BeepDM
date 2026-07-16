using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.SetUp.Adapters
{
    /// <summary>
    /// Shared <see cref="ISetupWizardAdapter.RunAsync"/> implementation.
    ///
    /// <para>
    /// Every adapter ran the same body — build an <see cref="IProgress{T}"/>, run the wizard on a
    /// worker thread, catch cancellation, return <c>GetReport()</c>. Only one of the six also caught
    /// unexpected exceptions, so an adapter's behavior on a throw depended on which platform you
    /// happened to be running. This base gives them one behavior; subclasses override only the hooks
    /// they genuinely differ on.
    /// </para>
    /// <para>
    /// The hooks are <see cref="Task"/>-returning on purpose: Maui must <c>await</c> marshalling its
    /// completion callback to the main thread, which a <c>void</c> hook would silently turn into
    /// fire-and-forget.
    /// </para>
    /// </summary>
    public abstract class SetupWizardAdapterBase : ISetupWizardAdapter
    {
        /// <inheritdoc/>
        public virtual async Task<SetupReport> RunAsync(ISetupWizard wizard, SetupContext context,
            CancellationToken cancellationToken = default)
        {
            if (wizard == null) throw new ArgumentNullException(nameof(wizard));
            if (context == null) throw new ArgumentNullException(nameof(context));

            await OnRunStartingAsync(wizard, context).ConfigureAwait(false);

            var progress = new Progress<PassedArgs>(args => ReportProgress(wizard, context, args));

            try
            {
                await Task.Run(() => wizard.Run(context, progress), cancellationToken)
                          .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // The wizard has already built a partial report; surface it rather than throwing,
                // so the caller can see how far the run got.
                await OnCancelledAsync(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await OnFailedAsync(ex, context).ConfigureAwait(false);
            }

            var report = wizard.GetReport();
            await OnCompletedAsync(report, context).ConfigureAwait(false);
            return report;
        }

        /// <summary>
        /// Routes a progress callback to <see cref="ShowProgress"/>, resolving the active step as
        /// the first not-yet-completed one. Override to attribute progress differently or to pass
        /// the raw <see cref="PassedArgs"/> through.
        /// </summary>
        protected virtual void ReportProgress(ISetupWizard wizard, SetupContext context, PassedArgs args)
        {
            var state = context?.State ?? wizard?.State;
            var activeStep = state == null
                ? null
                : wizard?.Steps?.FirstOrDefault(s => !state.IsStepCompleted(s.StepId));

            var message = args?.Messege;
            if (activeStep == null && string.IsNullOrEmpty(message)) return;

            ShowProgress(activeStep?.StepId ?? string.Empty, args?.ParameterInt1 ?? 0, message);
        }

        /// <summary>Called before the wizard starts. Default: no-op.</summary>
        protected virtual Task OnRunStartingAsync(ISetupWizard wizard, SetupContext context)
            => Task.CompletedTask;

        /// <summary>Called when the run was cancelled. Default: reports a progress message.</summary>
        protected virtual Task OnCancelledAsync(SetupContext context)
        {
            ShowProgress(string.Empty, 0, "Setup wizard cancelled.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the run threw an unexpected exception. Default: reports a progress message.
        /// The exception is deliberately not rethrown — the partial report is the result.
        /// </summary>
        protected virtual Task OnFailedAsync(Exception ex, SetupContext context)
        {
            ShowProgress(string.Empty, 0, $"Setup wizard failed: {ex.Message}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called once the report is available, before <c>RunAsync</c> returns.
        /// Default: hands it to <see cref="ShowResult"/>.
        /// </summary>
        protected virtual Task OnCompletedAsync(SetupReport report, SetupContext context)
        {
            ShowResult(report);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void ShowStep(ISetupStep step, int stepIndex, int totalSteps) { }

        /// <inheritdoc/>
        public virtual void ShowProgress(string stepId, int percentComplete, string message) { }

        /// <inheritdoc/>
        public virtual void ShowResult(SetupReport report) { }
    }
}
