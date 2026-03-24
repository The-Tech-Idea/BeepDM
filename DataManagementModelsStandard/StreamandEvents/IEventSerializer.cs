using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>
    /// Pluggable payload serializer.
    /// Implementations must be stateless and thread-safe.
    /// </summary>
    public interface IEventSerializer
    {
        /// <summary>Content-type produced (e.g. "application/json", "application/avro").</summary>
        string ContentType { get; }

        /// <summary>Serialize payload to bytes (zero-copy preferred).</summary>
        ReadOnlyMemory<byte> Serialize<T>(T value);

        /// <summary>Deserialize bytes back to value.</summary>
        T Deserialize<T>(ReadOnlySpan<byte> bytes);

        /// <summary>Async serialize to a stream — preferred for large payloads.</summary>
        Task SerializeAsync<T>(T value, Stream target, CancellationToken cancellationToken = default);

        /// <summary>Async deserialize from a stream.</summary>
        Task<T> DeserializeAsync<T>(Stream source, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Schema registry abstraction.
    /// Allows brokers to validate schema compatibility before publishing.
    /// </summary>
    public interface IEventSchemaRegistry
    {
        /// <summary>Returns the registered schema entry for a given event type and version.</summary>
        Task<SchemaRegistryEntry> GetAsync(string eventType, int version, CancellationToken cancellationToken = default);

        /// <summary>Registers a new schema version. Returns the assigned schema ID.</summary>
        Task<string> RegisterAsync(SchemaRegistryEntry entry, CancellationToken cancellationToken = default);

        /// <summary>Checks compatibility of a candidate schema against the current registered version.</summary>
        Task<SchemaCompatibilityResult> CheckCompatibilityAsync(
            string eventType,
            string candidateSchemaJson,
            SchemaCompatibilityMode mode,
            CancellationToken cancellationToken = default);
    }

    public sealed class SchemaCompatibilityResult
    {
        public bool IsCompatible { get; init; }
        public string Reason { get; init; }
    }
}
