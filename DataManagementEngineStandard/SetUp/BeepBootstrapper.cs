using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.Editor;

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
        public bool Succeeded { get; init; }
        public BootstrapPhase CompletedPhase { get; init; }
        public string? FailureMessage { get; init; }
        public TimeSpan TotalElapsed { get; init; }
        public bool WasFirstRun { get; init; }
        public SetupReport? Report { get; init; }

        public static BootstrapResult Success(BootstrapPhase phase, bool wasFirstRun, TimeSpan elapsed, SetupReport? report = null)
            => new() { Succeeded = true, CompletedPhase = phase, WasFirstRun = wasFirstRun, TotalElapsed = elapsed, Report = report };

        public static BootstrapResult Failed(BootstrapPhase phase, string message, TimeSpan elapsed, SetupReport? report = null)
            => new() { Succeeded = false, CompletedPhase = phase, FailureMessage = message, TotalElapsed = elapsed, Report = report };
    }

    public sealed class BeepBootstrapper
    {
        private readonly IFirstRunDetector _firstRunDetector;
        private readonly ISetupWizardFactory _wizardFactory;
        private readonly Func<IDMEEditor> _editorAccessor;
        private readonly ISetupWizardAdapter _adapter;
        private readonly ILogger<BeepBootstrapper>? _logger;

        public event Action<string, BootstrapPhase>? ProgressChanged;

        public BeepBootstrapper(
            IFirstRunDetector firstRunDetector,
            ISetupWizardFactory wizardFactory,
            Func<IDMEEditor> editorAccessor,
            ISetupWizardAdapter adapter,
            ILogger<BeepBootstrapper>? logger = null)
        {
            _firstRunDetector = firstRunDetector ?? throw new ArgumentNullException(nameof(firstRunDetector));
            _wizardFactory = wizardFactory ?? throw new ArgumentNullException(nameof(wizardFactory));
            _editorAccessor = editorAccessor ?? throw new ArgumentNullException(nameof(editorAccessor));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _logger = logger;
        }

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

                IDMEEditor editor;
                try { editor = _editorAccessor(); }
                catch (ObjectDisposedException ex)
                {
                    var msg = $"Editor accessor returned a disposed object.";
                    _logger?.LogWarning(ex, msg);
                    ProgressChanged?.Invoke(msg, BootstrapPhase.Failed);
                    return BootstrapResult.Failed(BootstrapPhase.FirstRunDetection, msg, DateTimeOffset.UtcNow - started);
                }
                catch (Exception ex)
                {
                    var msg = $"Editor accessor threw: {ex.Message}";
                    ProgressChanged?.Invoke(msg, BootstrapPhase.Failed);
                    return BootstrapResult.Failed(BootstrapPhase.FirstRunDetection, msg, DateTimeOffset.UtcNow - started);
                }

                var (wizard, context) = _wizardFactory.CreateDefault(editor);

                var report = await _adapter.RunAsync(wizard, context, cancellationToken);

                if (!report.Succeeded)
                {
                    var message = "Setup wizard failed.";
                    ProgressChanged?.Invoke(message, BootstrapPhase.Failed);
                    return BootstrapResult.Failed(BootstrapPhase.SetupWizard, message, DateTimeOffset.UtcNow - started, report);
                }

                ProgressChanged?.Invoke("Setup wizard completed.", BootstrapPhase.Verification);

                await _firstRunDetector.MarkSetupCompleteAsync();
                cancellationToken.ThrowIfCancellationRequested();

                ProgressChanged?.Invoke("Application ready.", BootstrapPhase.Ready);
                return BootstrapResult.Success(BootstrapPhase.Ready, isFirstRun, DateTimeOffset.UtcNow - started, report);
            }
            catch (OperationCanceledException)
            {
                ProgressChanged?.Invoke("Bootstrap cancelled.", BootstrapPhase.Failed);
                return BootstrapResult.Failed(BootstrapPhase.Failed, "Bootstrap cancelled.", DateTimeOffset.UtcNow - started);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Bootstrap failed");
                Debug.WriteLine($"[BeepBootstrapper] Bootstrap failed: {ex}");
                ProgressChanged?.Invoke($"Bootstrap error: {ex.Message}", BootstrapPhase.Failed);
                return BootstrapResult.Failed(BootstrapPhase.Failed, ex.Message, DateTimeOffset.UtcNow - started);
            }
        }

        public async Task ResetAsync()
        {
            await _firstRunDetector.ClearSetupFlagAsync();
        }
    }
}
