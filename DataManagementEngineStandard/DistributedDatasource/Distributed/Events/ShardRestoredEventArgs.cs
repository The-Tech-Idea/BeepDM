using System;

namespace TheTechIdea.Beep.Distributed.Events
{
    /// <summary>
    /// Raised when a previously unhealthy shard transitions back to
    /// healthy from the distribution tier's perspective (Phase 10).
    /// </summary>
    public sealed class ShardRestoredEventArgs : EventArgs
    {
        /// <summary>Creates a new shard-restored event.</summary>
        /// <param name="shardId">Shard that recovered. Required.</param>
        /// <param name="reason">Optional human-readable reason.</param>
        /// <param name="downtime">Duration the shard spent in the unhealthy state before recovery.</param>
        public ShardRestoredEventArgs(
            string   shardId,
            string   reason,
            TimeSpan downtime)
        {
            if (string.IsNullOrWhiteSpace(shardId))
                throw new ArgumentException("Shard id cannot be null or whitespace.", nameof(shardId));

            ShardId      = shardId;
            Reason       = reason ?? string.Empty;
            Downtime     = downtime < TimeSpan.Zero ? TimeSpan.Zero : downtime;
            TimestampUtc = DateTime.UtcNow;
        }

        /// <summary>Shard that recovered.</summary>
        public string   ShardId      { get; }

        /// <summary>Reason captured by the monitor.</summary>
        public string   Reason       { get; }

        /// <summary>Duration the shard spent in the unhealthy state.</summary>
        public TimeSpan Downtime     { get; }

        /// <summary>UTC timestamp the event was raised.</summary>
        public DateTime TimestampUtc { get; }

        /// <inheritdoc/>
        public override string ToString()
            => $"ShardRestored({ShardId}, downtime={Downtime}, reason='{Reason}', at={TimestampUtc:O})";
    }
}
