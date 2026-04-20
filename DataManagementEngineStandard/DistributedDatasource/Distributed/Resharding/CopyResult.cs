using System;

namespace TheTechIdea.Beep.Distributed.Resharding
{
    /// <summary>
    /// Outcome of an <see cref="IEntityCopyService"/> copy run.
    /// </summary>
    public sealed class CopyResult
    {
        /// <summary>Initialises a new copy result.</summary>
        /// <param name="reshardId">Governing reshard id.</param>
        /// <param name="entityName">Logical entity that was copied.</param>
        /// <param name="fromShardId">Source shard.</param>
        /// <param name="toShardId">Target shard.</param>
        /// <param name="rowsCopied">Total rows successfully copied (including checkpoint restart credit).</param>
        /// <param name="elapsed">Wall-clock duration of the run.</param>
        /// <param name="cancelled"><c>true</c> when the caller cancelled the run.</param>
        /// <param name="error">Final error when the run aborted; <c>null</c> on success.</param>
        public CopyResult(
            string    reshardId,
            string    entityName,
            string    fromShardId,
            string    toShardId,
            long      rowsCopied,
            TimeSpan  elapsed,
            bool      cancelled,
            Exception error)
        {
            ReshardId    = reshardId   ?? string.Empty;
            EntityName   = entityName  ?? string.Empty;
            FromShardId  = fromShardId ?? string.Empty;
            ToShardId    = toShardId   ?? string.Empty;
            RowsCopied   = rowsCopied;
            Elapsed      = elapsed;
            Cancelled    = cancelled;
            Error        = error;
        }

        /// <summary>Governing reshard id.</summary>
        public string ReshardId { get; }

        /// <summary>Logical entity that was copied.</summary>
        public string EntityName { get; }

        /// <summary>Source shard.</summary>
        public string FromShardId { get; }

        /// <summary>Target shard.</summary>
        public string ToShardId { get; }

        /// <summary>Total rows successfully copied (including checkpoint restart credit).</summary>
        public long RowsCopied { get; }

        /// <summary>Wall-clock duration of the run.</summary>
        public TimeSpan Elapsed { get; }

        /// <summary><c>true</c> when the caller cancelled the run.</summary>
        public bool Cancelled { get; }

        /// <summary>Final error when the run aborted; <c>null</c> on success.</summary>
        public Exception Error { get; }

        /// <summary><c>true</c> when the run completed successfully and the source is fully drained.</summary>
        public bool Success => Error == null && !Cancelled;

        /// <inheritdoc/>
        public override string ToString()
            => $"CopyResult(reshard={ReshardId}, entity={EntityName}, " +
               $"{FromShardId} -> {ToShardId}, rows={RowsCopied}, elapsed={Elapsed.TotalMilliseconds:n0} ms, " +
               $"success={Success}, cancelled={Cancelled}, error={Error?.GetType().Name ?? "-"})";
    }
}
