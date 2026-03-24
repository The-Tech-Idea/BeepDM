using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>
    /// Deduplication store contract.
    /// Implementations should use a fast, TTL-aware backend (Redis, DB, in-memory).
    /// </summary>
    public interface IIdempotencyStore
    {
        /// <summary>
        /// Attempts to claim the idempotency key.
        /// Returns true if the key is new (event should be processed).
        /// Returns false if it was already seen (duplicate — skip processing).
        /// </summary>
        Task<bool> TryClaimAsync(string idempotencyKey, TimeSpan window, CancellationToken cancellationToken = default);

        /// <summary>Explicitly releases a key (e.g. after a confirmed rollback).</summary>
        Task ReleaseAsync(string idempotencyKey, CancellationToken cancellationToken = default);

        /// <summary>Returns true if the key was already successfully processed.</summary>
        Task<bool> IsSeenAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    }

    /// <summary>Record stored per event for deduplication tracking.</summary>
    public sealed class IdempotencyRecord
    {
        public string IdempotencyKey { get; init; }
        public string EventId { get; init; }
        public string Topic { get; init; }
        public string ConsumerGroup { get; init; }
        public DateTime ClaimedAt { get; init; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; init; }
        public string Status { get; init; } = "Claimed"; // Claimed | Committed | Released
    }

    /// <summary>Action taken when a duplicate event is detected.</summary>
    public enum DuplicateEventAction
    {
        /// <summary>Silently skip the duplicate and ack. Default for most workloads.</summary>
        Skip = 0,

        /// <summary>Log the duplicate (via diagnostics) then skip and ack.</summary>
        Log = 1
    }

    /// <summary>
    /// Configurable policy for duplicate-event handling.
    /// Attached to a <see cref="SubscriptionDescriptor"/> to control idempotency behavior per consumer.
    /// </summary>
    public sealed class DuplicateEventPolicy
    {
        /// <summary>
        /// Whether idempotency checking is enabled.
        /// Defaults to <c>true</c> for <see cref="DeliverySemantics.AtLeastOnce"/> and <see cref="DeliverySemantics.ExactlyOnce"/>.
        /// Always <c>false</c> for <see cref="DeliverySemantics.AtMostOnce"/>.
        /// </summary>
        public bool Enabled { get; init; } = true;

        /// <summary>
        /// Claim key strategy: when <c>true</c>, prefers <c>IdempotencyKey</c> over <c>EventId</c>.
        /// Falls back to <c>EventId</c> when <c>IdempotencyKey</c> is null.
        /// Default: <c>true</c>.
        /// </summary>
        public bool PreferIdempotencyKey { get; init; } = true;

        /// <summary>
        /// Time-to-live window for claim entries.
        /// After this window expires, the same key can be reclaimed (event reprocessed).
        /// Default: 24 hours.
        /// </summary>
        public TimeSpan Window { get; init; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Action to take when a duplicate is detected.
        /// Default: <see cref="DuplicateEventAction.Skip"/>.
        /// </summary>
        public DuplicateEventAction Action { get; init; } = DuplicateEventAction.Skip;

        /// <summary>Singleton default policy (enabled, 24h window, skip action).</summary>
        public static DuplicateEventPolicy Default { get; } = new();

        /// <summary>Disabled policy — no dedup checking.</summary>
        public static DuplicateEventPolicy Disabled { get; } = new() { Enabled = false };
    }
}
