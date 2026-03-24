using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Schema format ─────────────────────────────────────────────────────────

    /// <summary>Wire serialization format used for a schema subject.</summary>
    public enum StreamSchemaFormat
    {
        /// <summary>JSON Schema draft-07 or later.</summary>
        Json,

        /// <summary>Apache Avro IDL / Avro JSON schema.</summary>
        Avro,

        /// <summary>Protocol Buffers (.proto) descriptor.</summary>
        Protobuf,

        /// <summary>MessagePack — no formal schema language; use type-based reflection.</summary>
        MessagePack
    }

    // ── Extended compatibility modes ──────────────────────────────────────────

    /// <summary>
    /// Full set of schema compatibility modes aligned with the Confluent Schema Registry.
    /// The four transitive variants enforce the constraint against ALL prior versions, not
    /// just the latest.
    /// </summary>
    public enum SchemaRegistryCompatibilityMode
    {
        /// <summary>No compatibility checked.</summary>
        None,
        /// <summary>New schema can be read by the consumer using the latest schema.</summary>
        Backward,
        /// <summary>New schema can be used to read data written with the latest schema.</summary>
        Forward,
        /// <summary>Both backward and forward compatible.</summary>
        Full,
        /// <summary>Backward compatible against all prior versions.</summary>
        BackwardTransitive,
        /// <summary>Forward compatible against all prior versions.</summary>
        ForwardTransitive,
        /// <summary>Fully compatible against all prior versions.</summary>
        FullTransitive
    }

    // ── Registry entry ────────────────────────────────────────────────────────

    /// <summary>
    /// Immutable record of one schema version stored in the registry.
    /// Uses an integer schema ID matching the Confluent / AWS Glue / Azure architecture.
    /// </summary>
    public sealed class SchemaEntry
    {
        /// <summary>Globally unique numeric schema ID (assigned by the registry on registration).</summary>
        public int SchemaId { get; init; }

        /// <summary>
        /// Registry subject name.  Convention: <c>&lt;topic&gt;-value</c> or <c>&lt;topic&gt;-key</c>.
        /// </summary>
        public string Subject { get; init; }

        /// <summary>Per-subject monotonically increasing version number (1-based).</summary>
        public int Version { get; init; }

        /// <summary>Serialization format for this schema.</summary>
        public StreamSchemaFormat Format { get; init; }

        /// <summary>Raw schema definition text (JSON Schema, Avro JSON, .proto, etc.).</summary>
        public string SchemaDefinition { get; init; }

        /// <summary>UTC timestamp of registration.</summary>
        public DateTimeOffset RegisteredAt { get; init; } = DateTimeOffset.UtcNow;
    }

    // ── Validation result ─────────────────────────────────────────────────────

    /// <summary>Outcome of a schema compatibility check.</summary>
    public sealed class SchemaValidationResult
    {
        public bool IsCompatible { get; init; }
        public SchemaRegistryCompatibilityMode Mode { get; init; }
        public IReadOnlyList<string> Violations { get; init; } = Array.Empty<string>();

        public static SchemaValidationResult Compatible(SchemaRegistryCompatibilityMode mode) =>
            new() { IsCompatible = true, Mode = mode };

        public static SchemaValidationResult Incompatible(
            SchemaRegistryCompatibilityMode mode,
            IReadOnlyList<string> violations) =>
            new() { IsCompatible = false, Mode = mode, Violations = violations };
    }

    // ── Interface ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Broker-neutral schema registry contract.  Aligns with the subject/version/ID model
    /// used by Confluent Schema Registry, AWS Glue Schema Registry, and Azure Schema Registry.
    /// Implementations may be in-process (<see cref="InMemorySchemaRegistry"/>), or remote adapters
    /// that proxy to a real registry service.
    /// </summary>
    public interface ISchemaRegistry
    {
        /// <summary>
        /// Registers a new schema version for <paramref name="subject"/>.
        /// If an identical schema already exists for the subject, returns the existing entry.
        /// </summary>
        Task<SchemaEntry> RegisterSchemaAsync(
            string subject,
            string schemaDefinition,
            StreamSchemaFormat format,
            CancellationToken cancellationToken = default);

        /// <summary>Resolves a schema by its globally-unique numeric ID.</summary>
        Task<SchemaEntry?> GetSchemaByIdAsync(int schemaId, CancellationToken cancellationToken = default);

        /// <summary>Returns the latest registered version for a subject, or null if not found.</summary>
        Task<SchemaEntry?> GetLatestSchemaAsync(string subject, CancellationToken cancellationToken = default);

        /// <summary>Returns a specific version of a subject's schema, or null if not found.</summary>
        Task<SchemaEntry?> GetSchemaByVersionAsync(
            string subject,
            int version,
            CancellationToken cancellationToken = default);

        /// <summary>Lists all registered version numbers for a subject.</summary>
        Task<IReadOnlyList<int>> ListVersionsAsync(string subject, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether <paramref name="schemaDefinition"/> is compatible with the schemas
        /// already registered under <paramref name="subject"/> given <paramref name="mode"/>.
        /// </summary>
        Task<SchemaValidationResult> CheckCompatibilityAsync(
            string subject,
            string schemaDefinition,
            SchemaRegistryCompatibilityMode mode,
            CancellationToken cancellationToken = default);

        /// <summary>Deletes a specific version of a subject's schema.</summary>
        Task DeleteSchemaAsync(string subject, int version, CancellationToken cancellationToken = default);

        /// <summary>Sets the compatibility enforcement mode for a subject.</summary>
        Task SetCompatibilityModeAsync(
            string subject,
            SchemaRegistryCompatibilityMode mode,
            CancellationToken cancellationToken = default);
    }
}
