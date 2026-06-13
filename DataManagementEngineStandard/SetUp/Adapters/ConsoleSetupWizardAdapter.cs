using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.SetUp.Adapters
{
    /// <summary>
    /// Bridges the setup wizard to a plain-console (CLI / BeepShell) output surface.
    ///
    /// Progress is written to <see cref="Console.Out"/> using simple text formatting.
    /// For Spectre.Console / AnsiConsole styling, subclass this adapter in the CLI project
    /// and override <see cref="ShowProgress"/> and <see cref="ShowResult"/>.
    /// </summary>
    public class ConsoleSetupWizardAdapter : ISetupWizardAdapter
    {
        /// <inheritdoc/>
        public async Task<SetupReport> RunAsync(
            ISetupWizard wizard, SetupContext context,
            CancellationToken cancellationToken = default)
        {
            ShowStep(wizard.Steps.FirstOrDefault(), 0, wizard.Steps.Count);

            var progress = new Progress<PassedArgs>(args =>
            {
                // Find the currently executing step by consulting wizard state
                // Guard: context.State may be null before the wizard initialises it
                var state = context.State;
                var activeStep = state == null
                    ? null
                    : wizard.Steps.FirstOrDefault(s => !state.IsStepCompleted(s.StepId));
                if (activeStep != null)
                    ShowProgress(activeStep.StepId, args.ParameterInt1, args.Messege);
                else if (!string.IsNullOrEmpty(args.Messege))
                    ShowProgress(string.Empty, args.ParameterInt1, args.Messege);
            });

            try
            {
                await Task.Run(() => wizard.Run(context, progress), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                ShowProgress(string.Empty, 0, "Setup wizard cancelled.");
                // Fall through — wizard already built a partial report.
            }

            var report = wizard.GetReport();
            ShowResult(report);
            return report;
        }

        /// <inheritdoc/>
        public void ShowStep(ISetupStep step, int stepIndex, int totalSteps)
        {
            if (step == null) return;
            Console.WriteLine();
            Console.WriteLine($"  Step {stepIndex + 1}/{totalSteps}: {step.StepName}");
            if (!string.IsNullOrEmpty(step.Description))
                Console.WriteLine($"  {step.Description}");
        }

        /// <inheritdoc/>
        public void ShowProgress(string stepId, int percentComplete, string message) =>
            Console.WriteLine($"    [{percentComplete,3}%] {message}");

        /// <inheritdoc/>
        public void ShowResult(SetupReport report)
        {
            Console.WriteLine();
            Console.WriteLine(new string('─', 72));
            Console.WriteLine($"  {"STEP",-28}  {"RESULT",-8}  {"ELAPSED",-10}  MESSAGE");
            Console.WriteLine(new string('─', 72));

            foreach (var r in report.StepResults)
            {
                var result = r.Skipped ? "SKIP" : (r.Succeeded ? "OK" : "FAIL");
                var elapsed = r.Elapsed.ToString(@"mm\:ss\.fff");
                var message = r.Message?.Length > 30 ? r.Message[..30] + "…" : r.Message ?? "";
                Console.WriteLine($"  {r.StepName,-28}  {result,-8}  {elapsed,-10}  {message}");
            }

            Console.WriteLine(new string('─', 72));
            Console.WriteLine(report.Succeeded
                ? $"  Setup SUCCEEDED  (hash: {report.ContentHash?[..12]}…)"
                : $"  Setup FAILED");
            Console.WriteLine();
        }
    }
}
