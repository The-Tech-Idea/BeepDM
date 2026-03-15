using System;

namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Records column-level data lineage for a pipeline run.
    /// Captured by the engine during execution and persisted to the lineage store.
    /// </summary>
    public class DataLineageRecord
    {
        /// <summary>Correlation identifier of the pipeline run this record belongs to.</summary>
        public string RunId { get; init; } = string.Empty;

        /// <summary>Identifier of the step that produced this lineage entry.</summary>
        public string StepId { get; init; } = string.Empty;

        /// <summary>Display name of the step for reporting.</summary>
        public string StepName { get; init; } = string.Empty;

        // ── Source ────────────────────────────────────────────────────────
        public string SourceDataSource { get; init; } = string.Empty;
        public string SourceEntity     { get; init; } = string.Empty;
        public string SourceField      { get; init; } = string.Empty;

        // ── Destination ───────────────────────────────────────────────────
        public string DestDataSource   { get; init; } = string.Empty;
        public string DestEntity       { get; init; } = string.Empty;
        public string DestField        { get; init; } = string.Empty;

        /// <summary>
        /// Optional expression applied during transformation, e.g. "UPPER(FirstName)".
        /// Empty string if it's a direct pass-through mapping.
        /// </summary>
        public string TransformExpression { get; init; } = string.Empty;

        /// <summary>UTC time this lineage entry was captured.</summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}
