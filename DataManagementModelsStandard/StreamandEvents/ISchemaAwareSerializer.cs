using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Schema-aware serializer ───────────────────────────────────────────────

    /// <summary>
    /// Extends <see cref="IEventSerializer"/> with schema-registry-aware serialization
    /// that stamps the Confluent wire-format header (<c>0x00 + 4-byte schema ID</c>) onto
    /// every payload so consumers can resolve the exact schema version at deserialization time.
    /// </summary>
    public interface ISchemaAwareSerializer<T> : IEventSerializer
    {
        /// <summary>
        /// Serializes <paramref name="value"/>, registers (or resolves) the schema in
        /// <paramref name="registry"/>, and prefixes the Confluent wire-format header.
        /// Returns the full framed bytes and the assigned/resolved schema ID.
        /// </summary>
        Task<(byte[] Bytes, int SchemaId)> SerializeWithSchemaAsync(
            T value,
            ISchemaRegistry registry,
            string subject,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the Confluent wire-format prefix from <paramref name="bytes"/>, resolves the
        /// schema from <paramref name="registry"/>, and deserializes the payload.
        /// </summary>
        Task<T> DeserializeWithSchemaAsync(
            ReadOnlyMemory<byte> bytes,
            ISchemaRegistry registry,
            CancellationToken cancellationToken = default);
    }

    // ── Wire format ───────────────────────────────────────────────────────────

    /// <summary>
    /// Confluent Schema Registry wire format helpers.
    /// <para>
    /// Layout:
    /// <code>
    /// Byte 0:     0x00  (magic byte)
    /// Bytes 1–4:  schema ID (big-endian int32)
    /// Bytes 5+:   serialized payload
    /// </code>
    /// All major schema registries (Confluent, AWS Glue, Azure Schema Registry) support this
    /// framing for Avro payloads; it can be adopted for Protobuf and JSON Schema too.
    /// </para>
    /// </summary>
    public static class SchemaWireFormat
    {
        public const byte MagicByte = 0x00;
        public const int HeaderLength = 5; // 1 magic + 4 schema ID bytes

        /// <summary>
        /// Writes the 5-byte wire-format prefix into <paramref name="destination"/>.
        /// <paramref name="destination"/> must have at least 5 bytes of capacity.
        /// </summary>
        public static void WriteSchemaId(Span<byte> destination, int schemaId)
        {
            if (destination.Length < HeaderLength)
                throw new ArgumentException($"Destination must be at least {HeaderLength} bytes.", nameof(destination));

            destination[0] = MagicByte;
            BinaryPrimitives.WriteInt32BigEndian(destination.Slice(1), schemaId);
        }

        /// <summary>
        /// Reads the wire-format prefix from <paramref name="source"/> and returns the schema ID
        /// together with the payload slice that follows the header.
        /// </summary>
        /// <exception cref="FormatException">
        /// Thrown when the magic byte is absent or less than <see cref="HeaderLength"/> bytes are available.
        /// </exception>
        public static (int SchemaId, ReadOnlyMemory<byte> Payload) ReadSchemaId(ReadOnlyMemory<byte> source)
        {
            if (source.Length < HeaderLength)
                throw new FormatException($"Source is too short ({source.Length} bytes) to contain a wire-format header.");

            var span = source.Span;
            if (span[0] != MagicByte)
                throw new FormatException($"Missing Confluent wire-format magic byte. Expected 0x00, got 0x{span[0]:X2}.");

            var schemaId = BinaryPrimitives.ReadInt32BigEndian(span.Slice(1));
            return (schemaId, source.Slice(HeaderLength));
        }

        /// <summary>
        /// Returns a new byte array containing the wire-format header followed by
        /// <paramref name="payload"/>.
        /// </summary>
        public static byte[] Frame(int schemaId, ReadOnlySpan<byte> payload)
        {
            var result = new byte[HeaderLength + payload.Length];
            WriteSchemaId(result.AsSpan(), schemaId);
            payload.CopyTo(result.AsSpan(HeaderLength));
            return result;
        }
    }
}
