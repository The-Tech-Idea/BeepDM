using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Events
{
    /// <summary>
    /// Raised when a broadcast write was issued with one or more
    /// shards excluded for being unhealthy (Phase 10). The write
    /// itself may still have satisfied its quorum — this event is
    /// strictly informational so operators (or a Phase 13 reconciler)
    /// can replay the missed shards after they recover.
    /// </summary>
    public sealed class PartialBroadcastEventArgs : EventArgs
    {
        /// <summary>Creates a new partial-broadcast event.</summary>
        /// <param name="entityName">Entity the broadcast targeted. Required.</param>
        /// <param name="operation">Operation name (e.g. "InsertEntity", "ExecuteSql"). Required.</param>
        /// <param name="attemptedShardIds">Shards the executor actually fired against.</param>
        /// <param name="skippedShardIds">Unhealthy shards that were skipped.</param>
        /// <param name="reason">Optional reason (e.g. "shard down").</param>
        public PartialBroadcastEventArgs(
            string                entityName,
            string                operation,
            IReadOnlyList<string> attemptedShardIds,
            IReadOnlyList<string> skippedShardIds,
            string                reason)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));
            if (string.IsNullOrWhiteSpace(operation))
                throw new ArgumentException("Operation name cannot be null or whitespace.", nameof(operation));

            EntityName        = entityName;
            Operation         = operation;
            AttemptedShardIds = attemptedShardIds ?? Array.Empty<string>();
            SkippedShardIds   = skippedShardIds   ?? Array.Empty<string>();
            Reason            = reason ?? string.Empty;
            TimestampUtc      = DateTime.UtcNow;
        }

        /// <summary>Entity the broadcast targeted.</summary>
        public string EntityName { get; }

        /// <summary>Operation name (e.g. "InsertEntity", "ExecuteSql").</summary>
        public string Operation  { get; }

        /// <summary>Shards the executor fired against.</summary>
        public IReadOnlyList<string> AttemptedShardIds { get; }

        /// <summary>Unhealthy shards that were skipped.</summary>
        public IReadOnlyList<string> SkippedShardIds   { get; }

        /// <summary>Optional reason captured by the raiser.</summary>
        public string Reason       { get; }

        /// <summary>UTC timestamp the event was raised.</summary>
        public DateTime TimestampUtc { get; }

        /// <inheritdoc/>
        public override string ToString()
            => $"PartialBroadcast({EntityName}/{Operation}, " +
               $"attempted={AttemptedShardIds.Count}, skipped={SkippedShardIds.Count}, " +
               $"reason='{Reason}', at={TimestampUtc:O})";
    }
}
