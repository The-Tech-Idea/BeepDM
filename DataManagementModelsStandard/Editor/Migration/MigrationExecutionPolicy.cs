using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Migration
{
    /// <summary>
    /// Execution-time retry/abort policy applied when a migration plan runs.
    /// Pure contract — lives in the models assembly so both the engine's
    /// <c>IMigrationManager</c> and the Studio's migration facade can share it
    /// without the models project referencing the engine.
    /// </summary>
    public class MigrationExecutionPolicy
    {
        public int MaxTransientRetries { get; set; } = 2;
        public int RetryDelayMilliseconds { get; set; } = 250;
        public List<string> TransientErrorMarkers { get; set; } = new List<string> { "timeout", "deadlock", "temporar", "lock wait", "transient" };
        public List<string> HardFailMarkers { get; set; } = new List<string> { "permission", "syntax", "unsupported", "not supported", "invalid object" };
        public bool RequireOperatorInterventionOnHardFail { get; set; } = true;
        public string OperatorInterventionHint { get; set; } = "Review checkpoint, apply compensation runbook, then resume from token.";

        /// <summary>
        /// When true (default), the plan aborts on the first non-transient step failure.
        /// When false, the plan continues to the next step on failure, recording the
        /// failed step in <c>MigrationExecutionResult.FailedSteps</c>. Dependency
        /// blocks and pre-flight gates always abort regardless of this flag.
        /// </summary>
        public bool AbortOnStepFailure { get; set; } = true;
    }
}
