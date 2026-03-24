using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Claim ticket ──────────────────────────────────────────────────────────

    /// <summary>
    /// Compact reference to a large payload stored in an <see cref="IPayloadStore"/>.
    /// Serialised to JSON and embedded in the <see cref="StreamHeaderNames.ClaimTicket"/> header.
    /// </summary>
    public sealed class ClaimTicket
    {
        [JsonPropertyName("cid")]
        public string ClaimId { get; init; }

        [JsonPropertyName("uri")]
        public string? StoreUri { get; init; }

        [JsonPropertyName("ct")]
        public string ContentType { get; init; }

        [JsonPropertyName("sz")]
        public long SizeBytes { get; init; }

        [JsonPropertyName("chk")]
        public string Checksum { get; init; }

        [JsonPropertyName("exp")]
        public DateTimeOffset? ExpiresAt { get; init; }

        // ── Factory ───────────────────────────────────────────────────────────

        /// <summary>Creates a <see cref="ClaimTicket"/> from a stored <see cref="PayloadStoreEntry"/>.</summary>
        public static ClaimTicket FromEntry(PayloadStoreEntry entry) => new ClaimTicket
        {
            ClaimId     = entry.ClaimId,
            ContentType = entry.ContentType,
            SizeBytes   = entry.SizeBytes,
            Checksum    = entry.Checksum,
            ExpiresAt   = entry.ExpiresAt
        };

        // ── Serialisation ─────────────────────────────────────────────────────

        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        /// <summary>Serialises this ticket to a compact JSON string suitable for use as a header value.</summary>
        public string ToHeaderValue() => JsonSerializer.Serialize(this, _jsonOptions);

        /// <summary>
        /// Deserialises a <see cref="ClaimTicket"/> from a JSON header value produced by
        /// <see cref="ToHeaderValue"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid JSON.</exception>
        public static ClaimTicket FromHeaderValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Claim ticket header value must not be empty.", nameof(value));

            return JsonSerializer.Deserialize<ClaimTicket>(value, _jsonOptions)
                   ?? throw new ArgumentException("Claim ticket header value deserialised to null.", nameof(value));
        }
    }

    // ── Options ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Configuration for the claim-check interceptors.
    /// Shared between <c>ClaimCheckProducerInterceptor</c> and <c>ClaimCheckConsumerInterceptor</c>.
    /// </summary>
    public sealed class PayloadStoreOptions
    {
        /// <summary>Payloads larger than this threshold (in bytes) are offloaded to the store. Default: 256 KB.</summary>
        public int SizeThresholdBytes { get; set; } = 256_000;

        /// <summary>How long stored payloads are retained before expiry. <c>null</c> = never expires.</summary>
        public TimeSpan? DefaultTtl { get; set; } = TimeSpan.FromHours(24);

        /// <summary>Informational name of the backing store provider. Default: <c>"in-memory"</c>.</summary>
        public string StoreProviderName { get; set; } = "in-memory";

        /// <summary>
        /// When <c>true</c>, the consumer interceptor verifies the SHA-256 checksum of the
        /// retrieved payload against the value in the claim ticket.
        /// </summary>
        public bool EnableChecksumVerification { get; set; } = true;

        /// <summary>
        /// When <c>true</c>, the consumer interceptor deletes the payload from the store
        /// after successful retrieval (auto-cleanup for at-most-once retrieval scenarios).
        /// </summary>
        public bool EnableAutoDelete { get; set; }
    }
}
