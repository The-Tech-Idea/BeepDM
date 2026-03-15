namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Terminal status of a pipeline run or individual step.
    /// </summary>
    public enum RunStatus
    {
        /// <summary>Run is currently executing.</summary>
        Running,

        /// <summary>All records processed without errors.</summary>
        Success,

        /// <summary>Run stopped due to a fatal error.</summary>
        Failed,

        /// <summary>Run was cancelled by the caller or CancellationToken.</summary>
        Cancelled,

        /// <summary>Run completed but some records were rejected or skipped.</summary>
        Partial,

        /// <summary>This step was skipped (e.g. disabled, condition not met).</summary>
        Skipped
    }
}
