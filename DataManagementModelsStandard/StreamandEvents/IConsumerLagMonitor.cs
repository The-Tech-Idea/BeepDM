using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Lag trend ─────────────────────────────────────────────────────────────

    /// <summary>Direction of consumer group lag change between two consecutive samples.</summary>
    public enum LagTrend
    {
        /// <summary>Not enough samples to determine direction.</summary>
        Unknown,

        /// <summary>Lag is increasing — consumers are falling behind producers.</summary>
        Growing,

        /// <summary>Lag is decreasing — consumers are catching up.</summary>
        Shrinking,

        /// <summary>Lag has not changed between the last two samples.</summary>
        Stable
    }

    // ── Snapshot ──────────────────────────────────────────────────────────────

    /// <summary>Single-partition lag measurement for a consumer group.</summary>
    public sealed class LagSnapshot
    {
        public string Topic              { get; init; }
        public int    Partition          { get; init; }
        public string ConsumerGroup      { get; init; }

        /// <summary>Last offset committed by the consumer group on this partition.</summary>
        public long CommittedOffset      { get; init; }

        /// <summary>Latest offset written to the partition (high-water mark).</summary>
        public long HighWatermark        { get; init; }

        /// <summary>Number of unconsumed messages: <c>HighWatermark − CommittedOffset</c>.</summary>
        public long Lag                  => Math.Max(0L, HighWatermark - CommittedOffset);

        /// <summary>Lag trend compared to the previous sample for this partition.</summary>
        public LagTrend Trend            { get; init; } = LagTrend.Unknown;

        /// <summary>Previous lag value, populated when at least one prior sample exists.</summary>
        public long? PreviousLag         { get; init; }

        /// <summary>UTC time this snapshot was collected.</summary>
        public DateTimeOffset SampledAt  { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>True when lag is exactly zero (fully caught up).</summary>
        public bool IsHealthy            => Lag == 0;
    }

    // ── Group report ─────────────────────────────────────────────────────────

    /// <summary>Aggregated lag report for one consumer group across all its tracked partitions.</summary>
    public sealed class ConsumerGroupLagReport
    {
        public string ConsumerGroup                       { get; init; }
        public DateTimeOffset SampledAt                   { get; init; } = DateTimeOffset.UtcNow;
        public IReadOnlyList<LagSnapshot> Snapshots       { get; init; } = Array.Empty<LagSnapshot>();

        /// <summary>Sum of <see cref="LagSnapshot.Lag"/> across all partitions.</summary>
        public long TotalLag
        {
            get
            {
                long sum = 0;
                foreach (var s in Snapshots) sum += s.Lag;
                return sum;
            }
        }

        /// <summary>True when all partitions report zero lag.</summary>
        public bool IsFullyCaughtUp => TotalLag == 0;
    }

    // ── Interface ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Observes consumer group lag for registered topics and partitions.
    /// Implementations can delegate to the broker API, the Beep <see cref="StreamCheckpoint"/> store,
    /// or an <see cref="IDistributedStreamStateStore{T}"/>.
    /// </summary>
    public interface IConsumerLagMonitor
    {
        /// <summary>
        /// Returns the latest lag snapshot for every partition of <paramref name="topic"/>
        /// that is tracked by <paramref name="consumerGroup"/>.
        /// </summary>
        Task<ConsumerGroupLagReport> GetLagAsync(
            string topic,
            string consumerGroup,
            CancellationToken cancellationToken = default);

        /// <summary>Returns lag reports for every registered (topic, consumerGroup) pair.</summary>
        Task<IReadOnlyList<ConsumerGroupLagReport>> GetAllLagsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Continuously polls all registered groups at <paramref name="interval"/>.
        /// Each iteration emits one <see cref="ConsumerGroupLagReport"/> per registered group.
        /// Cancelling <paramref name="cancellationToken"/> completes the sequence.
        /// </summary>
        IAsyncEnumerable<ConsumerGroupLagReport> PollAsync(
            TimeSpan interval,
            CancellationToken cancellationToken = default);

        /// <summary>Registers a (topic, consumerGroup) pair for continuous lag tracking.</summary>
        void RegisterGroup(string topic, string consumerGroup);

        /// <summary>Removes a previously registered (topic, consumerGroup) pair.</summary>
        void UnregisterGroup(string topic, string consumerGroup);
    }
}
