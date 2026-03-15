using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Arguments passed to <see cref="TheTechIdea.Beep.Pipelines.Interfaces.IPipelineScheduler.Triggered"/>
    /// when a scheduler fires.
    /// </summary>
    public class PipelineTriggerArgs : EventArgs
    {
        /// <summary>ID of the pipeline the scheduler is configured for.</summary>
        public string PipelineId { get; init; } = string.Empty;

        /// <summary>UTC time the trigger fired.</summary>
        public DateTime TriggeredAt { get; init; } = DateTime.UtcNow;

        /// <summary>Human-readable description of what caused this trigger.</summary>
        public string TriggerSource { get; init; } = string.Empty;

        /// <summary>Optional parameters the scheduler adds to the run context.</summary>
        public IReadOnlyDictionary<string, object> Parameters { get; init; }
            = new Dictionary<string, object>();
    }
}
