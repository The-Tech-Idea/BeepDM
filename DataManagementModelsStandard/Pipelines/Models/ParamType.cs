namespace TheTechIdea.Beep.Pipelines.Models
{
    /// <summary>
    /// Data type of a <see cref="PipelineParameterDef"/> value.
    /// Used by the Designer UI to render the appropriate input control.
    /// </summary>
    public enum ParamType
    {
        /// <summary>Free-text string.</summary>
        String,

        /// <summary>32-bit integer.</summary>
        Integer,

        /// <summary>64-bit integer.</summary>
        Long,

        /// <summary>Floating-point decimal.</summary>
        Decimal,

        /// <summary>Boolean (true / false).</summary>
        Boolean,

        /// <summary>Date and time value.</summary>
        DateTime,

        /// <summary>File-system path (Designer may show a file picker).</summary>
        FilePath,

        /// <summary>Registered BeepDM data source connection name.</summary>
        ConnectionName,

        /// <summary>JSON fragment for advanced plugin configuration.</summary>
        Json,

        /// <summary>Single-line credential / secret — masked in the UI.</summary>
        Secret
    }
}
