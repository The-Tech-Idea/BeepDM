using System;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.SetUp.Adapters
{
    /// <summary>
    /// Bridges the setup wizard to a WinForms/WPF progress surface.
    ///
    /// The caller provides an <see cref="Action{PassedArgs}"/> callback that updates the
    /// application's wait/progress form, plus an optional completion callback.
    /// Wizard execution runs on a thread-pool thread so the UI remains responsive.
    ///
    /// For DI registration, use the parameterless constructor and call
    /// <see cref="OnProgress"/> and/or wire the callbacks after construction.
    /// </summary>
    public class DesktopSetupWizardAdapter : ISetupWizardAdapter
    {
        private readonly Action<PassedArgs> _progressCallback;
        private readonly Action<SetupReport> _completedCallback;

        public event Action<PassedArgs>? OnProgress;
        public event Action<SetupReport>? OnCompleted;

        /// <summary>Parameterless constructor for DI. Wire <see cref="OnProgress"/> event after resolution.</summary>
        public DesktopSetupWizardAdapter()
        {
            _progressCallback = args =>
            {
                OnProgress?.Invoke(args);
            };
            _completedCallback = report => OnCompleted?.Invoke(report);
        }

        /// <param name="progressCallback">
        /// Called on the thread-pool each time a step reports progress.
        /// Marshal to the UI thread if needed (e.g. <c>Invoke</c> / <c>Dispatcher.Invoke</c>).
        /// </param>
        /// <param name="completedCallback">Called once when the wizard finishes.</param>
        public DesktopSetupWizardAdapter(
            Action<PassedArgs> progressCallback,
            Action<SetupReport> completedCallback = null)
        {
            _progressCallback = progressCallback ?? throw new ArgumentNullException(nameof(progressCallback));
            _completedCallback = completedCallback;
        }

        /// <inheritdoc/>
        public async Task<SetupReport> RunAsync(
            ISetupWizard wizard, SetupContext context,
            CancellationToken cancellationToken = default)
        {
            var progress = new Progress<PassedArgs>(args => _progressCallback.Invoke(args));

            try
            {
                await Task.Run(() => wizard.Run(context, progress), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _progressCallback.Invoke(new PassedArgs
                {
                    ParameterInt1 = 0,
                    Messege = "Setup wizard cancelled."
                });
                // Fall through — wizard already built a partial report before re-throwing.
            }

            var report = wizard.GetReport();
            _completedCallback?.Invoke(report);
            return report;
        }

        /// <inheritdoc/>
        public void ShowStep(ISetupStep step, int stepIndex, int totalSteps) =>
            _progressCallback.Invoke(new PassedArgs
            {
                Messege = $"Step {stepIndex + 1}/{totalSteps}: {step.StepName}",
                ParameterInt1 = totalSteps > 0 ? (int)(stepIndex * 100.0 / totalSteps) : 0
            });

        /// <inheritdoc/>
        public void ShowProgress(string stepId, int percentComplete, string message) =>
            _progressCallback.Invoke(new PassedArgs
            {
                Messege = message,
                ParameterInt1 = percentComplete
            });

        /// <inheritdoc/>
        public void ShowResult(SetupReport report) =>
            _completedCallback?.Invoke(report);
    }
}
