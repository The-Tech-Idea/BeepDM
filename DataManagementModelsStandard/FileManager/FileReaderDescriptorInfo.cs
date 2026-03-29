using System;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Shared metadata for a discoverable file reader implementation.
    /// This type is models-only and does not depend on EngineStandard internals.
    /// </summary>
    public sealed class FileReaderDescriptorInfo
    {
        /// <summary>Stable identifier for the reader implementation.</summary>
        public Guid ReaderId { get; init; }

        /// <summary>The datasource type handled by the reader.</summary>
        public DataSourceType FormatType { get; init; }

        /// <summary>Human-readable reader name.</summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>Primary file extension without leading dot.</summary>
        public string DefaultExtension { get; init; } = string.Empty;

        /// <summary>Optional fully qualified implementation type name.</summary>
        public string ImplementationTypeName { get; init; } = string.Empty;
    }
}
