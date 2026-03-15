using System;

namespace TheTechIdea.Beep.Pipelines.Attributes
{
    /// <summary>
    /// Decorate any <c>IPipelinePlugin</c> implementation with this attribute.
    /// <c>PipelinePluginRegistry</c> discovers it at startup via AssemblyHandler —
    /// the same pattern as <c>[AddinAttribute]</c> connectors.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PipelinePluginAttribute : Attribute
    {
        /// <summary>Stable unique identifier, e.g. "beep.source.sqlite".</summary>
        public string PluginId { get; }

        /// <summary>Human-readable name shown in Designer UI, e.g. "SQLite Source".</summary>
        public string DisplayName { get; }

        /// <summary>Functional role of this plugin: Source, Sink, Transformer, etc.</summary>
        public PipelinePluginType PluginType { get; }

        /// <summary>Optional grouping label, e.g. "Database", "File", "Cloud".</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Semantic version string, e.g. "1.2.0".</summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>Author or team name.</summary>
        public string Author { get; set; } = "The-Tech-Idea";

        /// <summary>Optional path to an icon displayed in the Designer palette.</summary>
        public string IconPath { get; set; } = string.Empty;

        public PipelinePluginAttribute(
            string pluginId,
            string displayName,
            PipelinePluginType pluginType)
        {
            PluginId    = pluginId;
            DisplayName = displayName;
            PluginType  = pluginType;
        }
    }
}
