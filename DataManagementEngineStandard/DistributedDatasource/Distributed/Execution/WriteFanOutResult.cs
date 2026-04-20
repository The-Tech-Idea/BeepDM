using System;

namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// Per-shard outcome of a single write leg inside a fan-out. One
    /// <see cref="WriteFanOutResult"/> is produced for every target
    /// shard, even when the whole fan-out ultimately succeeds, so
    /// audit / telemetry can show which replicas were contacted and
    /// how they responded.
    /// </summary>
    /// <remarks>
    /// Values are immutable. <see cref="Succeeded"/> is derived from
    /// <see cref="Error"/> being <c>null</c>; the factories below keep
    /// construction symmetric and explicit.
    /// </remarks>
    public sealed class WriteFanOutResult
    {
        private WriteFanOutResult(
            string    shardId,
            Exception error,
            TimeSpan  duration)
        {
            if (string.IsNullOrWhiteSpace(shardId))
                throw new ArgumentException("Shard id cannot be null or whitespace.", nameof(shardId));

            ShardId  = shardId;
            Error    = error;
            Duration = duration;
        }

        /// <summary>Target shard id.</summary>
        public string    ShardId   { get; }

        /// <summary><c>null</c> on success; the thrown exception otherwise.</summary>
        public Exception Error     { get; }

        /// <summary>Wall-clock duration of the per-shard call.</summary>
        public TimeSpan  Duration  { get; }

        /// <summary><c>true</c> when <see cref="Error"/> is <c>null</c>.</summary>
        public bool      Succeeded => Error == null;

        /// <summary>Records a successful per-shard write.</summary>
        public static WriteFanOutResult Success(string shardId, TimeSpan duration)
            => new WriteFanOutResult(shardId, error: null, duration: duration);

        /// <summary>Records a failed per-shard write.</summary>
        public static WriteFanOutResult Failure(string shardId, Exception error, TimeSpan duration)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));
            return new WriteFanOutResult(shardId, error, duration);
        }

        /// <inheritdoc/>
        public override string ToString()
            => Succeeded
                ? $"{ShardId}: OK ({Duration.TotalMilliseconds:F1}ms)"
                : $"{ShardId}: FAIL ({Duration.TotalMilliseconds:F1}ms) — {Error.Message}";
    }
}
