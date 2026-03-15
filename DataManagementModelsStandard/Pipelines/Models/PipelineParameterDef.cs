namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Defines a single configuration parameter exposed by a pipeline plugin.
    /// Consumed by the Designer UI to auto-generate the property panel.
    /// </summary>
    public class PipelineParameterDef
    {
        /// <summary>Parameter key used in the config dictionary.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Human-readable label for the Designer UI.</summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>Tooltip / help text shown next to the input.</summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>Expected value type — drives the UI input control.</summary>
        public ParamType Type { get; init; } = ParamType.String;

        /// <summary>Whether a value must be provided before the pipeline can run.</summary>
        public bool IsRequired { get; init; } = false;

        /// <summary>Default value applied when the parameter is omitted.</summary>
        public object? DefaultValue { get; init; }

        /// <summary>Optional group label for organizing parameters in the UI panel.</summary>
        public string Group { get; init; } = string.Empty;

        /// <summary>Display order within the group (lower = first).</summary>
        public int Order { get; init; } = 0;

        /// <summary>Optional regex for validating string values.</summary>
        public string? ValidationPattern { get; init; }
    }
}
