using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Seeker request / result types ─────────────────────────────────────────

    /// <summary>
    /// Describes a seek operation using an <see cref="OffsetResetTarget"/> strategy.
    /// </summary>
    public sealed record SeekRequest(
        string              Topic,
        string              ConsumerGroup,
        IReadOnlyList<int>  Partitions,
        OffsetResetTarget   Target,
        long?               SpecificOffset = null,
        DateTimeOffset?     Timestamp      = null);

    /// <summary>Describes a seek-to-timestamp operation across a set of partitions.</summary>
    public sealed record SeekToTimestampRequest(
        string              Topic,
        string              ConsumerGroup,
        DateTimeOffset      Timestamp,
        IReadOnlyList<int>  Partitions);

    /// <summary>Result of a single (topic, partition) seek attempt.</summary>
    public sealed record SeekResult(
        string  Topic,
        int     Partition,
        long    SeekToOffset,
        long    PreviousOffset,
        bool    Successful,
        string? FailureReason = null);

    // ── Interface ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Controls the read position (offset) of a consumer group on specific topic-partitions.
    /// <para>
    /// Unlike <see cref="IBrokerAdminClient.ResetOffsetsAsync"/> (which talks directly to the broker),
    /// <c>IOffsetSeeker</c> is intended to adjust the <em>in-process</em> consumer's position without
    /// restarting the consumer group — useful for real-time reruns, time-travel replays, and
    /// error-recovery scenarios.
    /// </para>
    /// </summary>
    public interface IOffsetSeeker
    {
        /// <summary>
        /// Seeks partitions to the position described by <paramref name="request"/>.
        /// </summary>
        Task SeekAsync(SeekRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Seeks each requested partition to the first offset whose timestamp is ≥
        /// <see cref="SeekToTimestampRequest.Timestamp"/>.
        /// </summary>
        Task<IReadOnlyList<SeekResult>> SeekToTimestampAsync(
            SeekToTimestampRequest request, CancellationToken cancellationToken = default);

        /// <summary>Seeks all specified partitions to the beginning (earliest available offset).</summary>
        Task SeekToBeginningAsync(
            string topic, IReadOnlyList<int> partitions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Seeks all specified partitions to the end (latest/high-water-mark offset).
        /// Returns the resolved offsets so callers know exactly where consumption will resume.
        /// </summary>
        Task<IReadOnlyList<SeekResult>> SeekToEndAsync(
            string topic, IReadOnlyList<int> partitions, CancellationToken cancellationToken = default);
    }
}
