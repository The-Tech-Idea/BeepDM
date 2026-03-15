using System;

namespace TheTechIdea.Beep.Workflow
{
    /// <summary>
    /// A directed edge in the workflow step graph.
    /// An optional <see cref="Condition"/> expression is evaluated against the
    /// predecessor step's <see cref="StepExecutionRecord"/>; when null the edge
    /// is always followed.
    /// </summary>
    public class StepConnection
    {
        public string  Id         { get; set; } = Guid.NewGuid().ToString();
        public string  FromStepId { get; set; } = string.Empty;
        public string  ToStepId   { get; set; } = string.Empty;

        /// <summary>
        /// Simple expression evaluated as a boolean against the predecessor
        /// step result, e.g. "result.Success == true" or "result.RecordsWritten > 0".
        /// Null means unconditional.
        /// </summary>
        public string? Condition  { get; set; }

        /// <summary>Lower number = higher priority when multiple edges leave the same step.</summary>
        public int     Priority   { get; set; } = 0;
    }
}
