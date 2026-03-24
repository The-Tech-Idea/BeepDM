using System;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.FileManager.Attributes
{
    /// <summary>
    /// Decorate any <c>IFileFormatReader</c> implementation with this attribute.
    /// <c>FileReaderRegistry</c> discovers it at startup via AssemblyHandler —
    /// the same pattern as <c>[AddinAttribute]</c> connectors and
    /// <c>[PipelinePluginAttribute]</c> pipeline plugins.
    /// </summary>
    /// <example>
    /// <code>
    /// [FileReader(DataSourceType.Parquet, "Parquet File Reader", "parquet")]
    /// public class ParquetFileReader : IFileFormatReader { ... }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class FileReaderAttribute : Attribute
    {
        /// <summary>
        /// The <see cref="DataSourceType"/> enum value this reader handles,
        /// e.g. <c>DataSourceType.Parquet</c> or <c>DataSourceType.DuckDB</c>.
        /// </summary>
        public DataSourceType FormatType { get; }

        /// <summary>Human-readable name shown in tooling UI, e.g. "DuckDB File Reader".</summary>
        public string DisplayName { get; }

        /// <summary>Primary file extension (without dot), e.g. "parquet" or "db".</summary>
        public string DefaultExtension { get; }

        /// <summary>Optional description of the reader's capabilities.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Author or team name.</summary>
        public string Author { get; set; } = "The-Tech-Idea";

        /// <summary>Semantic version string, e.g. "1.0.0".</summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>Optional path to an icon (used by designer tooling).</summary>
        public string IconPath { get; set; } = string.Empty;

        public FileReaderAttribute(
            DataSourceType formatType,
            string displayName,
            string defaultExtension)
        {
            FormatType       = formatType;
            DisplayName      = displayName ?? throw new ArgumentNullException(nameof(displayName));
            DefaultExtension = defaultExtension ?? string.Empty;
        }
    }
}
