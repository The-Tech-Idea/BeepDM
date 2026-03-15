namespace TheTechIdea.Beep.Pipelines.Attributes
{
    /// <summary>
    /// Classifies the role of a pipeline plugin.
    /// Used by <see cref="PipelinePluginAttribute"/> and the <c>PipelinePluginRegistry</c>.
    /// </summary>
    public enum PipelinePluginType
    {
        Source,
        Sink,
        Transformer,
        Validator,
        Filter,
        Aggregator,
        Join,
        Lookup,
        Notifier,
        Scheduler
    }
}
