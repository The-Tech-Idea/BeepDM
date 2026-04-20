using System;

namespace TheTechIdea.Beep.Distributed.Events
{
    /// <summary>
    /// Raised when a shard transitions from healthy to unhealthy from
    /// the distribution tier's perspective (Phase 10). The event is
    /// emitted once per transition — subsequent failures are suppressed
    /// until the shard recovers (see <see cref="ShardRestoredEventArgs"/>).
    /// </summary>
    public sealed class ShardDownEventArgs : EventArgs
    {
        /// <summary>Creates a new shard-down event.</summary>
        /// <param name="shardId">Shard that went down. Required.</param>
        /// <param name="reason">Human-readable reason (threshold breached, circuit opened, etc.). Required.</param>
        /// <param name="consecutiveFailures">Failure count that tripped the classification.</param>
        /// <param name="firstError">First error observed during the streak; may be <c>null</c> for circuit-opened transitions.</param>
        public ShardDownEventArgs(
            string    shardId,
            string    reason,
            int       consecutiveFailures,
            Exception firstError)
        {
            if (string.IsNullOrWhiteSpace(shardId))
                throw new ArgumentException("Shard id cannot be null or whitespace.", nameof(shardId));

            ShardId             = shardId;
            Reason              = reason ?? string.Empty;
            ConsecutiveFailures = Math.Max(0, consecutiveFailures);
            FirstError          = firstError;
            TimestampUtc        = DateTime.UtcNow;
        }

        /// <summary>Shard id that went down.</summary>
        public string   ShardId             { get; }

        /// <summary>Reason captured by the monitor.</summary>
        public string   Reason              { get; }

        /// <summary>Consecutive failure count at the moment of transition.</summary>
        public int      ConsecutiveFailures { get; }

        /// <summary>First error observed during the streak; may be <c>null</c>.</summary>
        public Exception FirstError         { get; }

        /// <summary>UTC timestamp the event was raised.</summary>
        public DateTime TimestampUtc        { get; }

        /// <inheritdoc/>
        public override string ToString()
            => $"ShardDown({ShardId}, fails={ConsecutiveFailures}, reason='{Reason}', at={TimestampUtc:O})";
    }
}
