using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Published when the canonical ISetupWizard completes execution (success or failure).
    /// Carries the derived summary fields so callers (e.g. the shell form) don't need to
    /// re-run the wizard to get a status string, plus the full canonical
    /// <see cref="SetupReport"/> for callers that need per-step detail.
    /// </summary>
    public sealed class SetupCompletedEventArgs : EventArgs
    {
        public bool Succeeded { get; init; }
        public string Summary { get; init; } = string.Empty;
        public string ExecutionPath { get; init; } = string.Empty;

        /// <summary>
        /// The canonical report produced by the run, or null when the wizard never executed.
        /// <see cref="SetupReport"/> ships in this same assembly and namespace, so callers get
        /// the per-step results without re-running the wizard.
        /// </summary>
        public SetupReport? Report { get; init; }

        /// <summary>
        /// Driver packages staged from ConfigEditor for this run. Empty when none were staged.
        /// </summary>
        public IReadOnlyList<string> StagedDrivers { get; init; } = Array.Empty<string>();
    }
}
