using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Workflow
{
    /// <summary>
    /// Serializable, self-contained definition of a workflow. Replaces the old
    /// JSON-persisted <see cref="WorkFlow"/> for new engine usage while the
    /// legacy type remains untouched.
    /// </summary>
    public class WorkFlowDefinition
    {
        public string  Id          { get; set; } = Guid.NewGuid().ToString();
        public string  Name        { get; set; } = string.Empty;
        public string  Description { get; set; } = string.Empty;
        public string  Version     { get; set; } = "1.0";
        public List<string> Tags   { get; set; } = new();

        /// <summary>Ordered step definitions (graph nodes).</summary>
        public List<WorkFlowStepDef>    Steps       { get; set; } = new();

        /// <summary>Directed edges between steps (graph edges).</summary>
        public List<StepConnection>     Connections { get; set; } = new();

        /// <summary>Declared input parameters for this workflow.</summary>
        public List<WorkFlowParameter>  Parameters  { get; set; } = new();

        /// <summary>When / how the workflow is triggered.</summary>
        public WorkFlowTrigger          Trigger     { get; set; } = new();

        /// <summary>Default retry policy applied to all steps (can be overridden per step).</summary>
        public WorkFlowRetryPolicy      RetryPolicy { get; set; } = WorkFlowRetryPolicy.None;

        public DateTime CreatedAtUtc  { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
