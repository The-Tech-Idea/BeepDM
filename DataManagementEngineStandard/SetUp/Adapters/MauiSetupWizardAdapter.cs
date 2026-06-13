using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.SetUp.Adapters
{
    /// <summary>
    /// Base adapter for .NET MAUI applications.
    ///
    /// This class compiles without MAUI references and provides thread-safe progress
    /// dispatch via an overridable <see cref="InvokeOnMainThreadAsync"/> hook.
    ///
    /// In your MAUI project, subclass this to dispatch to the main thread:
    ///
    /// <code>
    /// public class MyMauiSetupAdapter : MauiSetupWizardAdapter
    /// {
    ///     public MyMauiSetupAdapter(Action&lt;int, string&gt; progressAction,
    ///                               Action&lt;SetupReport&gt; completedAction = null)
    ///         : base(progressAction, completedAction) { }
    ///
    ///     protected override Task InvokeOnMainThreadAsync(Action action)
    ///         => MainThread.InvokeOnMainThreadAsync(action);
    /// }
    /// </code>
    /// </summary>
    public class MauiSetupWizardAdapter : ISetupWizardAdapter
    {
        private readonly Action<int, string> _progressAction;
        private readonly Action<SetupReport> _completedAction;

        /// <param name="progressAction">Invoked with (percentComplete, message) for each progress event.</param>
        /// <param name="completedAction">Invoked with the final report when the wizard finishes.</param>
        public MauiSetupWizardAdapter(
            Action<int, string> progressAction,
            Action<SetupReport> completedAction = null)
        {
            _progressAction = progressAction ?? throw new ArgumentNullException(nameof(progressAction));
            _completedAction = completedAction;
        }

        /// <inheritdoc/>
        public async Task<SetupReport> RunAsync(
            ISetupWizard wizard, SetupContext context,
            CancellationToken cancellationToken = default)
        {
            var progress = new Progress<PassedArgs>(args =>
                _ = InvokeOnMainThreadAsync(
                    () => _progressAction.Invoke(args.ParameterInt1, args.Messege)));

            try
            {
                await Task.Run(() => wizard.Run(context, progress), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await InvokeOnMainThreadAsync(() => _progressAction.Invoke(0, "Setup wizard cancelled."));
                // Fall through — wizard already built a partial report.
            }

            var report = wizard.GetReport();
            await InvokeOnMainThreadAsync(() => _completedAction?.Invoke(report));
            return report;
        }

        /// <inheritdoc/>
        public void ShowStep(ISetupStep step, int stepIndex, int totalSteps) =>
            _ = InvokeOnMainThreadAsync(() =>
                _progressAction.Invoke(
                    totalSteps > 0 ? (int)(stepIndex * 100.0 / totalSteps) : 0,
                    step?.StepName ?? string.Empty));

        /// <inheritdoc/>
        public void ShowProgress(string stepId, int percentComplete, string message) =>
            _ = InvokeOnMainThreadAsync(() => _progressAction.Invoke(percentComplete, message));

        /// <inheritdoc/>
        public void ShowResult(SetupReport report) =>
            _ = InvokeOnMainThreadAsync(() => _completedAction?.Invoke(report));

        // ── Extension point ──────────────────────────────────────────────────

        /// <summary>
        /// Override in your MAUI project to dispatch <paramref name="action"/> to the UI thread.
        /// Default implementation runs the action inline (safe for unit tests and non-MAUI use).
        /// </summary>
        protected virtual Task InvokeOnMainThreadAsync(Action action)
        {
            action();
            return Task.CompletedTask;
        }
    }
}
