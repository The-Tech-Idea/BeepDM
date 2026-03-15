namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Describes the functional role of a pipeline execution step.
    /// </summary>
    public enum StepKind
    {
        /// <summary>Reads records from a source plugin.</summary>
        Extract,

        /// <summary>Maps or converts field values.</summary>
        Transform,

        /// <summary>Drops records not matching a predicate.</summary>
        Filter,

        /// <summary>Applies data-quality rules; rejects route to the error sink.</summary>
        Validate,

        /// <summary>Enriches records via lookup or join against reference data.</summary>
        Enrich,

        /// <summary>Groups and aggregates records (group-by / sum / count).</summary>
        Aggregate,

        /// <summary>Writes records to a sink plugin.</summary>
        Load,

        /// <summary>Sends a notification (email / webhook / event bus).</summary>
        Notify,

        /// <summary>Executes a user C# snippet (sandboxed Roslyn expression).</summary>
        Script
    }
}
