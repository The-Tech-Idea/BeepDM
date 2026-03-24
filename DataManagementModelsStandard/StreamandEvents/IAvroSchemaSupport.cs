using System;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Avro schema provider ──────────────────────────────────────────────────

    /// <summary>
    /// Provides Avro JSON schema strings for CLR types.
    /// Implementations may use reflection, a static dictionary, or the Apache.Avro SDK.
    /// Placed in Models so consumer code can reference it without taking an Avro SDK
    /// dependency at the Models layer.
    /// </summary>
    public interface IAvroSchemaProvider
    {
        /// <summary>
        /// Returns the Avro JSON schema for <paramref name="type"/>.
        /// The returned string is the full Avro JSON schema (e.g. <c>{"type":"record",...}</c>).
        /// </summary>
        string GetAvroSchema(Type type);

        /// <summary>
        /// Parses an Avro JSON schema definition string and returns the parsed schema object.
        /// The return type is <c>object</c> to avoid forcing a dependency on the Apache.Avro
        /// assembly in the models layer — callers that need the typed schema must cast.
        /// </summary>
        object GetAvroSchemaFromDefinition(string avroJsonSchema);
    }

    // ── Attribute ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Decorates an event class with its Avro JSON schema so that the schema registry
    /// interceptor can register the schema automatically on first publish.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class AvroSchemaAttribute : Attribute
    {
        /// <summary>Full Avro JSON schema for the decorated type.</summary>
        public string SchemaJson { get; }

        /// <summary>
        /// Initialises the attribute with a literal Avro JSON schema string.
        /// </summary>
        public AvroSchemaAttribute(string schemaJson)
        {
            if (string.IsNullOrWhiteSpace(schemaJson))
                throw new ArgumentException("Schema JSON must not be null or empty.", nameof(schemaJson));

            SchemaJson = schemaJson;
        }
    }
}
