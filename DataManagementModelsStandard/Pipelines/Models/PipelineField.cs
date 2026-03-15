namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Describes a single field in a <see cref="PipelineSchema"/>.
    /// Immutable — created by source plugins and consumed throughout the pipeline.
    /// </summary>
    public record PipelineField(
        string  Name,
        System.Type   DataType,
        bool    IsKey        = false,
        bool    IsNullable   = true,
        int     MaxLength    = -1,
        string? Description  = null);
}
