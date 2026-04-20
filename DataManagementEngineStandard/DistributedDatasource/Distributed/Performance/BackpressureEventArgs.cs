using System;

namespace TheTechIdea.Beep.Distributed.Performance
{
    /// <summary>
    /// Payload for <see cref="DistributedDataSource"/>.OnBackpressure —
    /// raised when a Phase 14 capacity gate rejects a call. Lets
    /// operators instrument the event without catching the exception
    /// upstream (e.g. emit a metric, update a dashboard, trigger
    /// auto-scale).
    /// </summary>
    public sealed class BackpressureEventArgs : EventArgs
    {
        /// <summary>Creates a new event payload.</summary>
        public BackpressureEventArgs(
            string               gateName,
            string               shardId,
            string               entityName,
            string               operation,
            TimeSpan             retryAfter,
            BackpressureException exception)
        {
            GateName    = gateName   ?? string.Empty;
            ShardId     = shardId    ?? string.Empty;
            EntityName  = entityName ?? string.Empty;
            Operation   = operation  ?? string.Empty;
            RetryAfter  = retryAfter;
            Exception   = exception;
            TimestampUtc = DateTime.UtcNow;
        }

        /// <summary>Gate that rejected the call.</summary>
        public string GateName { get; }

        /// <summary>Shard id when the rejection came from a per-shard gate; empty otherwise.</summary>
        public string ShardId { get; }

        /// <summary>Entity being accessed (best-effort).</summary>
        public string EntityName { get; }

        /// <summary>Operation name (best-effort).</summary>
        public string Operation { get; }

        /// <summary>Retry hint published by the gate.</summary>
        public TimeSpan RetryAfter { get; }

        /// <summary>Underlying exception that will be thrown to the caller.</summary>
        public BackpressureException Exception { get; }

        /// <summary>UTC timestamp when the rejection was observed.</summary>
        public DateTime TimestampUtc { get; }
    }
}
