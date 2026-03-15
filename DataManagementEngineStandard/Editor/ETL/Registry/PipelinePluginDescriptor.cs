using TheTechIdea.Beep.Pipelines.Attributes;

namespace TheTechIdea.Beep.Pipelines.Registry
{
    /// <summary>
    /// Describes a discovered pipeline plugin: the discovery attribute and the concrete type.
    /// Immutable record — created by <see cref="PipelinePluginRegistry"/> and never mutated.
    /// </summary>
    public record PipelinePluginDescriptor(
        PipelinePluginAttribute Attribute,
        System.Type ImplementationType);
}
