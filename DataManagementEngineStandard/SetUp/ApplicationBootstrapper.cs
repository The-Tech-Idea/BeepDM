using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.SetUp
{
    public enum BootstrapPhase
    {
        NotStarted,
        FirstRunDetection,
        SetupWizard,
        Verification,
        Ready,
        Failed
    }

    public sealed class BootstrapResult
    {
        public bool Succeeded { get; set; }
        public BootstrapPhase CompletedPhase { get; set; }
        public string? FailureMessage { get; set; }
        public TimeSpan TotalElapsed { get; set; }
        public bool WasFirstRun { get; set; }

        public static BootstrapResult Success(BootstrapPhase phase, bool wasFirstRun, TimeSpan elapsed)
            => new() { Succeeded = true, CompletedPhase = phase, WasFirstRun = wasFirstRun, TotalElapsed = elapsed };

        public static BootstrapResult Failed(BootstrapPhase phase, string message, TimeSpan elapsed)
            => new() { Succeeded = false, CompletedPhase = phase, FailureMessage = message, TotalElapsed = elapsed };
    }

    public sealed class ApplicationBootstrapper
    {
        private readonly IFirstRunDetector _firstRunDetector;
        private readonly ISetupWizard _wizard;
        private readonly SetupContext _context;
        private readonly ISetupWizardAdapter _adapter;

        public ApplicationBootstrapper(
            IFirstRunDetector firstRunDetector,
            ISetupWizard wizard,
            SetupContext context,
            ISetupWizardAdapter adapter)
        {
            _firstRunDetector = firstRunDetector ?? throw new ArgumentNullException(nameof(firstRunDetector));
            _wizard = wizard ?? throw new ArgumentNullException(nameof(wizard));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        public event Action<string, BootstrapPhase>? ProgressChanged;

        public async Task<BootstrapResult> BootstrapAsync(CancellationToken cancellationToken = default)
        {
            var started = DateTimeOffset.UtcNow;

            try
            {
                ProgressChanged?.Invoke("Checking if this is the first run...", BootstrapPhase.FirstRunDetection);
                var isFirstRun = await _firstRunDetector.IsFirstRunAsync();
                cancellationToken.ThrowIfCancellationRequested();

                if (!isFirstRun && _firstRunDetector.WasSetupCompleted)
                {
                    ProgressChanged?.Invoke("Setup already completed. Starting normally.", BootstrapPhase.Ready);
                    return BootstrapResult.Success(BootstrapPhase.Verification, false, DateTimeOffset.UtcNow - started);
                }

                ProgressChanged?.Invoke("First run detected. Starting setup wizard...", BootstrapPhase.SetupWizard);
                var report = await _adapter.RunAsync(_wizard, _context, cancellationToken);

                if (!report.Succeeded)
                {
                    var message = $"Setup wizard failed. Check the setup report for details.";
                    ProgressChanged?.Invoke(message, BootstrapPhase.Failed);
                    return BootstrapResult.Failed(BootstrapPhase.SetupWizard, message, DateTimeOffset.UtcNow - started);
                }

                ProgressChanged?.Invoke("Setup wizard completed successfully.", BootstrapPhase.Verification);

                await _firstRunDetector.MarkSetupCompleteAsync();
                cancellationToken.ThrowIfCancellationRequested();

                ProgressChanged?.Invoke("Application ready.", BootstrapPhase.Ready);
                return BootstrapResult.Success(BootstrapPhase.Ready, isFirstRun, DateTimeOffset.UtcNow - started);
            }
            catch (OperationCanceledException)
            {
                ProgressChanged?.Invoke("Bootstrap cancelled.", BootstrapPhase.Failed);
                return BootstrapResult.Failed(BootstrapPhase.Failed, "Bootstrap cancelled.", DateTimeOffset.UtcNow - started);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApplicationBootstrapper] Bootstrap failed: {ex}");
                ProgressChanged?.Invoke($"Bootstrap error: {ex.Message}", BootstrapPhase.Failed);
                return BootstrapResult.Failed(BootstrapPhase.Failed, ex.Message, DateTimeOffset.UtcNow - started);
            }
        }

        public async Task ResetAsync()
        {
            await _firstRunDetector.ClearSetupFlagAsync();
            ProgressChanged = null;
        }
    }
}

