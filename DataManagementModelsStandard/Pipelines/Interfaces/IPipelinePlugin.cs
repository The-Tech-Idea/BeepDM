using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Pipelines.Models;

namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Marker interface. All pipeline plugins implement this.
    /// Provides identity, discovery metadata, and parameter schema.
    /// </summary>
    public interface IPipelinePlugin
    {
        /// <summary>Unique plugin identity — matches PipelinePluginAttribute.PluginId.</summary>
        string PluginId { get; }

        /// <summary>Human-readable display name shown in Designer UI.</summary>
        string DisplayName { get; }

        /// <summary>Short description shown in tooltip / help.</summary>
        string Description { get; }

        /// <summary>
        /// Returns the parameter schema this plugin accepts.
        /// Used by Designer UI to auto-generate property panels.
        /// </summary>
        IReadOnlyList<PipelineParameterDef> GetParameterDefinitions();

        /// <summary>Apply a parameter bag at runtime before the pipeline starts.</summary>
        void Configure(IReadOnlyDictionary<string, object> parameters);
    }
}
