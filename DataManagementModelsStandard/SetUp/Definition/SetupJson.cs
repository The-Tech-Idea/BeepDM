using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.SetUp.Definition
{
    /// <summary>
    /// The single JSON configuration used everywhere a <see cref="SetupDefinition"/> or a step's
    /// options are read or written.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Enums are written as names, not numbers.</b> This is not cosmetic:
    /// </para>
    /// <list type="bullet">
    ///   <item>A definition is hand-authored and reviewed — <c>"SqlLite"</c> is meaningful in a diff,
    ///         <c>27</c> is not.</item>
    ///   <item>Numeric values are positional. Inserting a value into <c>DataSourceType</c> would
    ///         silently re-point every stored definition at a different datasource — a catastrophic,
    ///         invisible failure. Names are stable across enum edits.</item>
    ///   <item>System.Text.Json rejects string enums by default, so a hand-written definition would
    ///         fail to bind without this.</item>
    /// </list>
    /// <para>
    /// Any code that serializes step options must use these settings, or a definition written by one
    /// path won't be readable by another.
    /// </para>
    /// </remarks>
    public static class SetupJson
    {
        /// <summary>Canonical settings for reading and writing definitions and step options.</summary>
        public static JsonSerializerOptions Options { get; } = Create(indented: false);

        /// <summary>Same as <see cref="Options"/>, indented for the on-disk artifact.</summary>
        public static JsonSerializerOptions IndentedOptions { get; } = Create(indented: true);

        private static JsonSerializerOptions Create(bool indented) => new()
        {
            WriteIndented = indented,

            // camelCase on write to match the documented artifact and JSON convention;
            // case-insensitive on read so a hand-written PascalCase file still binds.
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,

            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

            // Enum VALUES keep their declared casing ("SqlLite", not "sqlLite") to match the
            // documented artifact; only property names are camelCased. allowIntegerValues keeps
            // older numeric definitions readable.
            Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: true) }
        };
    }
}
