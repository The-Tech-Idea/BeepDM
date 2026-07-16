using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.SetUp.Definition
{
    /// <summary>
    /// A setup wizard expressed as <b>data</b> rather than a C# object graph.
    ///
    /// <para>
    /// This is the artifact the rest of the roadmap hangs off: a definition can be versioned in Git,
    /// diffed in review, shipped to a second app, driven from a CLI, validated in CI, stored
    /// remotely, and authorized. A <c>SetupWizardBuilder</c> call chain can be none of those things.
    /// </para>
    /// </summary>
    public sealed class SetupDefinition
    {
        /// <summary>
        /// Schema version of THIS document. Bump on any breaking shape change and teach
        /// <c>ISetupDefinitionUpgrader</c> how to migrate the previous version.
        /// </summary>
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>The version this build writes and understands.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>Wizard id. Also keys persisted state, so it must be stable across runs.</summary>
        public string Id { get; set; } = "default-setup";

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; }

        /// <summary>
        /// Free-form environment label, matching <see cref="SetupOptions.Environment"/>.
        /// Promoted to a resolved environment in Phase 7.
        /// </summary>
        public string Environment { get; set; } = "Development";

        public List<SetupStepDefinition> Steps { get; set; } = new();

        /// <summary>
        /// Stable SHA-256 over the canonical JSON, excluding this field. Audit (Phase 6) binds
        /// "what was applied" to this value. Computed by <c>ISetupDefinitionSerializer</c>.
        /// </summary>
        public string ContentHash { get; set; }
    }

    /// <summary>One step in a <see cref="SetupDefinition"/>.</summary>
    public sealed class SetupStepDefinition
    {
        /// <summary>
        /// Unique within the definition; matches <c>ISetupStep.StepId</c>. Steps of the same type
        /// must qualify their id (e.g. <c>"driver-provision:SQLite"</c>).
        /// </summary>
        public string StepId { get; set; }

        /// <summary>
        /// Registered step-type key (e.g. <c>"driver-provision"</c>) — <b>not</b> an
        /// assembly-qualified type name.
        /// <para>
        /// An AQN here would leak internal namespaces into a user-editable file, break on any
        /// refactor or version bump, and become an arbitrary-type-instantiation vector once
        /// definitions can arrive from a shared store. <c>ISetupStepFactory</c> is the allow-list.
        /// </para>
        /// </summary>
        public string Type { get; set; }

        public List<string> DependsOn { get; set; } = new();

        /// <summary>When false the step is omitted from the built wizard entirely.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Step-specific options; the shape is owned by the step type named in <see cref="Type"/>.
        /// Non-serializable options (e.g. <c>SeedingStepOptions.Registry</c>) are never carried
        /// here — the factory injects those from DI.
        /// </summary>
        [JsonConverter(typeof(JsonElementNullableConverter))]
        public JsonElement? Options { get; set; }
    }

    /// <summary>
    /// Round-trips a nullable <see cref="JsonElement"/>. The default converter cannot write
    /// <c>JsonElement?</c>, which would silently drop every step's options on serialize.
    /// </summary>
    internal sealed class JsonElementNullableConverter : JsonConverter<JsonElement?>
    {
        public override JsonElement? Read(ref Utf8JsonReader reader, System.Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            using var doc = JsonDocument.ParseValue(ref reader);
            return doc.RootElement.Clone();
        }

        public override void Write(Utf8JsonWriter writer, JsonElement? value, JsonSerializerOptions options)
        {
            if (value.HasValue) value.Value.WriteTo(writer);
            else writer.WriteNullValue();
        }
    }
}
