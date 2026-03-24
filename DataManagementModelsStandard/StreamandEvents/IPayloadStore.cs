using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Payload store entry ───────────────────────────────────────────────────

    /// <summary>Metadata record stored alongside a large payload in the claim-check store.</summary>
    public sealed class PayloadStoreEntry
    {
        /// <summary>Unique claim ID used to retrieve this payload. Defaults to a new GUID.</summary>
        public string ClaimId { get; init; } = Guid.NewGuid().ToString("N");

        /// <summary>MIME content type of the stored bytes (e.g. <c>application/json</c>).</summary>
        public string ContentType { get; init; }

        /// <summary>Size of the stored payload in bytes.</summary>
        public long SizeBytes { get; init; }

        /// <summary>Hex-encoded SHA-256 checksum of the stored bytes.</summary>
        public string Checksum { get; init; }

        /// <summary>When the payload was stored.</summary>
        public DateTimeOffset StoredAt { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>When the payload expires and may be purged. <c>null</c> = no expiry.</summary>
        public DateTimeOffset? ExpiresAt { get; init; }

        /// <summary>Additional caller-supplied metadata (e.g. original message ID, topic).</summary>
        public IReadOnlyDictionary<string, string> Metadata { get; init; }
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    // ── Payload store ─────────────────────────────────────────────────────────

    /// <summary>
    /// Content-addressable store for large event payloads (claim-check pattern).
    /// The producer stores the payload here, embeds the <see cref="PayloadStoreEntry.ClaimId"/>
    /// in the event headers, and the consumer retrieves the bytes on demand.
    /// </summary>
    public interface IPayloadStore
    {
        /// <summary>
        /// Stores <paramref name="payload"/> and returns a <see cref="PayloadStoreEntry"/>
        /// that can be embedded in an event as a claim ticket.
        /// Computes a SHA-256 checksum automatically.
        /// </summary>
        Task<PayloadStoreEntry> StoreAsync(
            ReadOnlyMemory<byte> payload,
            string contentType,
            TimeSpan? ttl = null,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves the raw bytes for <paramref name="claimId"/>.
        /// Returns <c>null</c> if the entry does not exist or has expired.
        /// </summary>
        Task<ReadOnlyMemory<byte>?> RetrieveAsync(string claimId, CancellationToken ct = default);

        /// <summary>
        /// Returns the <see cref="PayloadStoreEntry"/> metadata without retrieving the payload bytes.
        /// Returns <c>null</c> if the entry does not exist or has expired.
        /// </summary>
        Task<PayloadStoreEntry?> RetrieveMetadataAsync(string claimId, CancellationToken ct = default);

        /// <summary>Deletes the payload for <paramref name="claimId"/>. No-op if not found.</summary>
        Task DeleteAsync(string claimId, CancellationToken ct = default);

        /// <summary>Returns <c>true</c> if a non-expired entry exists for <paramref name="claimId"/>.</summary>
        Task<bool> ExistsAsync(string claimId, CancellationToken ct = default);

        /// <summary>Streams all entries whose <see cref="PayloadStoreEntry.ExpiresAt"/> is in the past.</summary>
        IAsyncEnumerable<PayloadStoreEntry> ListExpiredAsync(CancellationToken ct = default);

        /// <summary>Deletes all expired entries and returns the count of deleted records.</summary>
        Task<int> PurgeExpiredAsync(CancellationToken ct = default);
    }
}
