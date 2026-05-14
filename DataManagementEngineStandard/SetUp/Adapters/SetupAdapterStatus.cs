namespace TheTechIdea.Beep.SetUp.Adapters
{
    /// <summary>
    /// Mutable status object that <see cref="WebApiSetupWizardAdapter"/> updates in real-time.
    /// Expose this via a polling endpoint (e.g. <c>GET /api/setup/status</c>).
    /// </summary>
    public class SetupAdapterStatus
    {
        /// <summary>
        /// Lifecycle state: <c>Idle</c> → <c>Running</c> → <c>Completed</c> | <c>Failed</c> | <c>Cancelled</c>.
        /// </summary>
        public string State { get; set; } = "Idle";

        /// <summary>Name of the step currently executing.</summary>
        public string CurrentStepName { get; set; }

        /// <summary>Zero-based index of the step currently executing.</summary>
        public int CurrentStepIndex { get; set; }

        /// <summary>Total number of steps in the wizard.</summary>
        public int TotalSteps { get; set; }

        /// <summary>Overall percent-complete across all steps (0–100).</summary>
        public int PercentComplete { get; set; }

        /// <summary>Latest progress message.</summary>
        public string CurrentMessage { get; set; }

        /// <summary>Populated after the wizard finishes (<see cref="State"/> == Completed or Failed).</summary>
        public SetupReport Report { get; set; }
    }
}
