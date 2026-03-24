using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Replay speed ──────────────────────────────────────────────────────────

    /// <summary>Controls the rate at which replayed events are delivered.</summary>
    public enum ReplaySpeedMode
    {
        /// <summary>Events are replayed preserving the original inter-event timing.</summary>
        Realtime,

        /// <summary>Events are replayed at <see cref="ReplayRequest.SpeedFactor"/> times real-time.</summary>
        Throttled,

        /// <summary>Events are replayed as fast as possible with no artificial delays.</summary>
        Maximum
    }

    // ── Request ───────────────────────────────────────────────────────────────

    /// <summary>Parameters for a replay session.</summary>
    public sealed class ReplayRequest
    {
        /// <summary>Topic to replay from.</summary>
        public string Topic { get; init; }

        /// <summary>Consumer group used during replay. Should be unique to avoid affecting production offsets.</summary>
        public string ReplayConsumerGroup { get; init; }

        /// <summary>Absolute offset to start from. Highest priority if set. Null = use <see cref="StartTimestamp"/>.</summary>
        public long? StartOffset { get; init; }

        /// <summary>Start from the earliest offset at or after this timestamp. Used when <see cref="StartOffset"/> is null.</summary>
        public DateTimeOffset? StartTimestamp { get; init; }

        /// <summary>Stop replaying at this offset (inclusive). Null = replay to end or <see cref="MaxMessages"/>.</summary>
        public long? EndOffset { get; init; }

        /// <summary>Maximum number of events to replay. Null = unlimited.</summary>
        public int? MaxMessages { get; init; }

        /// <summary>Event types to skip during replay (by <see cref="EventEnvelope{T}.EventType"/>).</summary>
        public IReadOnlyList<string>? ExcludedEventTypes { get; init; }

        /// <summary>
        /// Speed multiplier when <see cref="SpeedMode"/> is <see cref="ReplaySpeedMode.Throttled"/>.
        /// 1.0 = real-time; 2.0 = double speed; 0.5 = half speed.
        /// </summary>
        public double SpeedFactor { get; init; } = 1.0;

        /// <summary>Replay delivery rate strategy.</summary>
        public ReplaySpeedMode SpeedMode { get; init; } = ReplaySpeedMode.Maximum;

        /// <summary>
        /// When <c>true</c>, the replay consumer commits offsets after each message.
        /// Default is <c>false</c> — replay sessions do not affect permanent offsets.
        /// </summary>
        public bool CommitOnReplay { get; init; }
    }

    // ── Result ────────────────────────────────────────────────────────────────

    /// <summary>Summary returned by <see cref="IStreamReplayer.ReplayAsync{T}"/> after completion.</summary>
    public sealed class ReplayResult
    {
        /// <summary>Number of events delivered to the handler.</summary>
        public int MessagesReplayed { get; init; }

        /// <summary>Number of events skipped (e.g. excluded event types).</summary>
        public int MessagesSkipped { get; init; }

        /// <summary>When the replay session started.</summary>
        public DateTimeOffset StartedAt { get; init; }

        /// <summary>When the replay session ended.</summary>
        public DateTimeOffset CompletedAt { get; init; }

        /// <summary>The last offset consumed, or -1 if no messages were replayed.</summary>
        public long FinalOffset { get; init; }
    }

    // ── Interface ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Replays events from a previously persisted topic, optionally throttled to original timing.
    /// Useful for event sourcing audits, backfills, and testing failure scenarios.
    /// </summary>
    public interface IStreamReplayer
    {
        /// <summary>
        /// Replays events matching <paramref name="request"/> and invokes <paramref name="handler"/>
        /// for each message. Returns a <see cref="ReplayResult"/> when complete or when
        /// <paramref name="ct"/> is cancelled.
        /// </summary>
        Task<ReplayResult> ReplayAsync<T>(
            ReplayRequest request,
            Func<ReceivedEvent<T>, CancellationToken, Task> handler,
            CancellationToken ct = default);

        /// <summary>
        /// Returns an estimated count of messages that would be replayed for <paramref name="request"/>,
        /// based on broker watermarks. Returns −1 if the estimate cannot be determined.
        /// </summary>
        Task<long> EstimateReplayCountAsync(ReplayRequest request, CancellationToken ct = default);
    }
}
