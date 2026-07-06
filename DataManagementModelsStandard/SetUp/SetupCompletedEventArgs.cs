using System;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Published when the canonical ISetupWizard completes execution (success or failure).
    /// Carries the derived summary fields so callers (e.g. the shell form) don't need to
    /// re-run the wizard to get a status string. The full canonical SetupReport lives
    /// in the engine project; this is the contract-side projection.
    /// </summary>
    public sealed class SetupCompletedEventArgs : EventArgs
    {
        public bool Succeeded { get; init; }
        public string Summary { get; init; } = string.Empty;
        public string ExecutionPath { get; init; } = string.Empty;
    }
}
