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
    public class ConsoleSetupWizardAdapter : SetupWizardAdapterBase
    {
        /// <summary>
        /// Prints the first step before the run. Note this always shows step 1 regardless of resume
        /// position — see P1-10 in the setup plan.
        /// </summary>
        protected override Task OnRunStartingAsync(ISetupWizard wizard, SetupContext context)
        {
            ShowStep(wizard.Steps.FirstOrDefault(), 0, wizard.Steps.Count);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override void ShowStep(ISetupStep step, int stepIndex, int totalSteps)
        {
            if (step == null) return;
            Console.WriteLine();
            Console.WriteLine($"  Step {stepIndex + 1}/{totalSteps}: {step.StepName}");
            if (!string.IsNullOrEmpty(step.Description))
                Console.WriteLine($"  {step.Description}");
        }

        /// <inheritdoc/>
        public override void ShowProgress(string stepId, int percentComplete, string message) =>
            Console.WriteLine($"    [{percentComplete,3}%] {message}");

        /// <inheritdoc/>
        public override void ShowResult(SetupReport report)
        {
            if (report == null) return;
            Console.WriteLine();
            Console.WriteLine(new string('─', 72));
            Console.WriteLine($"  {"STEP",-28}  {"RESULT",-8}  {"ELAPSED",-10}  MESSAGE");
            Console.WriteLine(new string('─', 72));

            foreach (var r in report.StepResults ?? Array.Empty<SetupStepResult>())
            {
                var result = r.Skipped ? "SKIP" : (r.Succeeded ? "OK" : "FAIL");
                var elapsed = r.Elapsed.ToString(@"mm\:ss\.fff");
                var message = r.Message?.Length > 30 ? r.Message[..30] + "…" : r.Message ?? "";
                Console.WriteLine($"  {r.StepName,-28}  {result,-8}  {elapsed,-10}  {message}");
            }

            Console.WriteLine(new string('─', 72));
            Console.WriteLine(report.Succeeded
                ? $"  Setup SUCCEEDED  (hash: {ShortHash(report.ContentHash)})"
                : $"  Setup FAILED");
            Console.WriteLine();
        }

        /// <summary>Hash prefix for display. Guards a short/absent hash on a partial report.</summary>
        private static string ShortHash(string hash)
            => string.IsNullOrEmpty(hash)
                ? "n/a"
                : (hash.Length <= 12 ? hash : hash[..12] + "…");
    }
}
