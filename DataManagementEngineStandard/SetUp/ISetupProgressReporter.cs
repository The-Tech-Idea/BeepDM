namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Unified progress surface used by all platform adapters to surface wizard progress
    /// without coupling to platform-specific UI APIs.
    /// </summary>
    public interface ISetupProgressReporter
    {
        /// <summary>Called when a step is about to begin execution.</summary>
        void ReportStepStart(string stepId, string stepName, int stepIndex, int totalSteps);

        /// <summary>Called by a step to report incremental progress (0–100).</summary>
        void ReportStepProgress(string stepId, int percentComplete, string message);

        /// <summary>Called when a step finishes (succeeded or failed).</summary>
        void ReportStepComplete(string stepId, bool succeeded, string message);

        /// <summary>Called when the entire wizard finishes.</summary>
        void ReportWizardComplete(SetupReport report);
    }
}
