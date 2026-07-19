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
        VersionCheck,
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

        /// <summary>Database version before the upgrade pass ran (null on first run / when no gate ran).</summary>
        public string? MigratedFrom { get; init; }

        /// <summary>Database version after the upgrade pass ran (equals <see cref="MigratedFrom"/> when up to date).</summary>
        public string? MigratedTo { get; init; }

        public static BootstrapResult Success(BootstrapPhase phase, bool wasFirstRun, TimeSpan elapsed,
            SetupReport? report = null, string? migratedFrom = null, string? migratedTo = null)
            => new() { Succeeded = true, CompletedPhase = phase, WasFirstRun = wasFirstRun, TotalElapsed = elapsed,
                       Report = report, MigratedFrom = migratedFrom, MigratedTo = migratedTo };

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
        private readonly Func<IDMEEditor, (ISetupWizard wizard, SetupContext context)?>? _upgradeWizardFactory;

        public event Action<string, BootstrapPhase>? ProgressChanged;

        /// <param name="upgradeWizardFactory">
        /// Optional. Builds the per-startup <em>upgrade pass</em> (typically a single
        /// <c>VersionGateStep</c>) run when setup was already completed. Its wizard should use its own
        /// state key so its state starts empty each launch — otherwise the wizard's skip-completed-steps
        /// guard would suppress the gate. Return null to skip the upgrade pass for a given editor.
        /// When this is null, a completed setup starts normally with no version check (legacy behaviour).
        /// </param>
        public BeepBootstrapper(
            IFirstRunDetector firstRunDetector,
            ISetupWizardFactory wizardFactory,
            Func<IDMEEditor> editorAccessor,
            ISetupWizardAdapter adapter,
            ILogger<BeepBootstrapper>? logger = null,
            Func<IDMEEditor, (ISetupWizard wizard, SetupContext context)?>? upgradeWizardFactory = null)
        {
            _firstRunDetector = firstRunDetector ?? throw new ArgumentNullException(nameof(firstRunDetector));
            _wizardFactory = wizardFactory ?? throw new ArgumentNullException(nameof(wizardFactory));
            _editorAccessor = editorAccessor ?? throw new ArgumentNullException(nameof(editorAccessor));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _logger = logger;
            _upgradeWizardFactory = upgradeWizardFactory;
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
                    // Setup was done before. Instead of the old unconditional early-return, run a
                    // lightweight upgrade pass (version gate) when one is configured — this is what
                    // makes migrate-on-startup work after a model/version bump.
                    if (_upgradeWizardFactory != null)
                        return await RunUpgradePassAsync(started, cancellationToken).ConfigureAwait(false);

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

        /// <summary>
        /// Runs the per-startup upgrade pass: resolves the editor, builds the configured upgrade wizard
        /// (version gate), executes it, and reports any version movement. A null wizard for this editor
        /// means "nothing to upgrade" and starts normally.
        /// </summary>
        private async Task<BootstrapResult> RunUpgradePassAsync(DateTimeOffset started, CancellationToken cancellationToken)
        {
            ProgressChanged?.Invoke("Checking database version…", BootstrapPhase.VersionCheck);

            IDMEEditor editor;
            try { editor = _editorAccessor(); }
            catch (Exception ex)
            {
                var msg = $"Editor accessor threw during version check: {ex.Message}";
                _logger?.LogWarning(ex, msg);
                ProgressChanged?.Invoke(msg, BootstrapPhase.Failed);
                return BootstrapResult.Failed(BootstrapPhase.VersionCheck, msg, DateTimeOffset.UtcNow - started);
            }

            var built = _upgradeWizardFactory!(editor);
            if (built == null)
            {
                ProgressChanged?.Invoke("No version check configured. Starting normally.", BootstrapPhase.Ready);
                return BootstrapResult.Success(BootstrapPhase.Verification, false, DateTimeOffset.UtcNow - started);
            }

            var (wizard, context) = built.Value;
            var report = await _adapter.RunAsync(wizard, context, cancellationToken).ConfigureAwait(false);

            var from = context.Properties.TryGetValue(Steps.VersionGateStep.MigratedFromKey, out var f) ? f?.ToString() : null;
            var to = context.Properties.TryGetValue(Steps.VersionGateStep.MigratedToKey, out var t) ? t?.ToString() : null;

            if (!report.Succeeded)
            {
                ProgressChanged?.Invoke("Version check failed.", BootstrapPhase.Failed);
                return BootstrapResult.Failed(BootstrapPhase.VersionCheck, "Version check / migration failed.",
                    DateTimeOffset.UtcNow - started, report);
            }

            var moved = !string.IsNullOrEmpty(to) && !string.Equals(from, to, StringComparison.Ordinal);
            ProgressChanged?.Invoke(
                moved ? $"Database upgraded {from} → {to}." : "Database up to date. Application ready.",
                BootstrapPhase.Ready);
            return BootstrapResult.Success(BootstrapPhase.Ready, false, DateTimeOffset.UtcNow - started, report, from, to);
        }

        public async Task ResetAsync()
        {
            await _firstRunDetector.ClearSetupFlagAsync();
        }
    }
}
