using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Result of validating a single record.
    /// Immutable — created by the validator and consumed by the engine.
    /// </summary>
    public record ValidationResult(
        ValidationOutcome Outcome,
        string RuleName,
        string Message,
        PipelineRecord? Record = null);
}
