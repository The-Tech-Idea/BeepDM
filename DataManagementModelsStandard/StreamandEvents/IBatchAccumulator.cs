using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Trigger flags ─────────────────────────────────────────────────────────

    /// <summary>Flags describing what caused a batch flush.</summary>
    [Flags]
    public enum BatchTrigger
    {
        /// <summary>Batch was flushed because <see cref="BatchAccumulatorOptions.MaxCount"/> was reached.</summary>
        MaxCount     = 1,

        /// <summary>Batch was flushed because <see cref="BatchAccumulatorOptions.MaxSizeBytes"/> was reached.</summary>
        MaxSizeBytes = 2,

        /// <summary>Batch was flushed because <see cref="BatchAccumulatorOptions.MaxAge"/> elapsed.</summary>
        MaxAge       = 4,

        /// <summary>Batch was flushed because the accumulator was idle for <see cref="BatchAccumulatorOptions.IdleFlushAfter"/>.</summary>
        Idle         = 8
    }

    // ── Options ───────────────────────────────────────────────────────────────

    /// <summary>Configuration for <c>InMemoryBatchAccumulator&lt;T&gt;</c>.</summary>
    public sealed class BatchAccumulatorOptions
    {
        /// <summary>Maximum number of items in a batch before an automatic flush. Default: 500.</summary>
        public int MaxCount { get; set; } = 500;

        /// <summary>Maximum accumulated byte size before an automatic flush. Default: 4 MB.</summary>
        public long MaxSizeBytes { get; set; } = 4_000_000;

        /// <summary>Maximum wall-clock age of the oldest item before an automatic flush. Default: 5 seconds.</summary>
        public TimeSpan MaxAge { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Flush the batch after this period of inactivity (no new items).
        /// <c>null</c> = idle flush disabled.
        /// </summary>
        public TimeSpan? IdleFlushAfter { get; set; }

        /// <summary>
        /// Bitmask of enabled flush triggers.
        /// Default: all four triggers are active.
        /// </summary>
        public BatchTrigger Triggers { get; set; } = BatchTrigger.MaxCount | BatchTrigger.MaxSizeBytes
                                                    | BatchTrigger.MaxAge   | BatchTrigger.Idle;
    }

    // ── Event args ────────────────────────────────────────────────────────────

    /// <summary>Event arguments raised when a batch is ready for processing.</summary>
    public sealed class BatchReadyArgs<T> : EventArgs
    {
        /// <summary>The flushed batch items.</summary>
        public IReadOnlyList<T> Batch { get; init; }

        /// <summary>What triggered this flush.</summary>
        public BatchTrigger TriggerReason { get; init; }

        /// <summary>Total byte count of all items in the batch.</summary>
        public long AccumulatedBytes { get; init; }

        /// <summary>Time between the oldest item being added and the flush.</summary>
        public TimeSpan BatchAge { get; init; }

        /// <summary>When the flush occurred.</summary>
        public DateTimeOffset FlushTime { get; init; }
    }

    // ── Interface ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Accumulates items until one of the configured flush conditions is met, then delivers the
    /// batch via the <see cref="OnBatchReady"/> event.
    /// The in-memory implementation is <c>InMemoryBatchAccumulator&lt;T&gt;</c>.
    /// </summary>
    public interface IBatchAccumulator<T>
    {
        /// <summary>
        /// Adds <paramref name="item"/> to the current batch.
        /// Returns <c>true</c> if adding this item triggered an immediate flush.
        /// <para>
        /// Uses <c>WaitToWriteAsync</c> pattern internally — callers should await this method
        /// rather than fire-and-forget.
        /// </para>
        /// </summary>
        Task<bool> AccumulateAsync(T item, long sizeBytes, CancellationToken ct = default);

        /// <summary>
        /// Forces an immediate flush of all pending items regardless of the trigger conditions.
        /// Returns the flushed batch (may be empty).
        /// </summary>
        Task<IReadOnlyList<T>> FlushAsync(CancellationToken ct = default);

        /// <summary>Fired once per batch flush with the accumulated items and flush metadata.</summary>
        event EventHandler<BatchReadyArgs<T>>? OnBatchReady;

        /// <summary>Number of items currently waiting to be flushed.</summary>
        int PendingCount { get; }

        /// <summary>Total byte count of items currently waiting to be flushed.</summary>
        long PendingBytes { get; }
    }
}
