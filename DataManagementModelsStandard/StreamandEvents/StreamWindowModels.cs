using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── WindowType ────────────────────────────────────────────────────────────

    public enum WindowType { Tumbling, Hopping, Session }

    // ── IWindowSpec ───────────────────────────────────────────────────────────

    /// <summary>Describes the temporal or count-based behaviour of an aggregation window.</summary>
    public interface IWindowSpec
    {
        WindowType  Type              { get; }
        TimeSpan    Size              { get; }
        TimeSpan?   AdvanceBy         { get; }   // null for tumbling & session
        TimeSpan?   SessionGapTimeout { get; }   // only meaningful for Session
    }

    // ── Concrete window specs ─────────────────────────────────────────────────

    /// <summary>Non-overlapping fixed-size time windows (e.g. every 1 minute).</summary>
    public sealed class TumblingWindowSpec : IWindowSpec
    {
        public WindowType Type              => WindowType.Tumbling;
        public TimeSpan   Size              { get; init; }
        public TimeSpan?  AdvanceBy         => null;
        public TimeSpan?  SessionGapTimeout => null;
    }

    /// <summary>Overlapping fixed-size windows promoted by a smaller advance interval.</summary>
    public sealed class HoppingWindowSpec : IWindowSpec
    {
        public WindowType Type              => WindowType.Hopping;
        public TimeSpan   Size              { get; init; }
        public TimeSpan?  AdvanceBy         { get; init; }
        public TimeSpan?  SessionGapTimeout => null;
    }

    /// <summary>
    /// Variable-size windows that close after the specified idle gap with no new events.
    /// <see cref="Size"/> is a maximum cap on session length.
    /// </summary>
    public sealed class SessionWindowSpec : IWindowSpec
    {
        public WindowType Type              => WindowType.Session;
        /// <summary>Maximum session duration cap.</summary>
        public TimeSpan   Size              { get; init; }
        public TimeSpan?  AdvanceBy         => null;
        public TimeSpan?  SessionGapTimeout { get; init; }
    }

    // ── Windowed key / entry ──────────────────────────────────────────────────

    /// <summary>Composite key combining a domain key with its window boundaries.</summary>
    public sealed record WindowedKey<TKey>(TKey Key, DateTimeOffset WindowStart, DateTimeOffset WindowEnd);

    /// <summary>A single entry stored in a windowed store.</summary>
    public sealed record WindowedEntry<TKey, TValue>(
        WindowedKey<TKey> WindowedKey,
        TValue            Value,
        long              EventCount,
        DateTimeOffset    LastUpdated);

    // ── IWindowedStore ────────────────────────────────────────────────────────

    /// <summary>
    /// Key-value store scoped to time windows.
    /// Used by stream aggregation stages to accumulate per-window state.
    /// </summary>
    public interface IWindowedStore<TKey, TValue>
    {
        Task PutAsync(WindowedKey<TKey> key, TValue value, CancellationToken cancellationToken = default);

        Task<TValue?> GetAsync(WindowedKey<TKey> key, CancellationToken cancellationToken = default);

        IAsyncEnumerable<WindowedEntry<TKey, TValue>> FetchRangeAsync(
            TKey              key,
            DateTimeOffset    from,
            DateTimeOffset    to,
            CancellationToken cancellationToken = default);

        /// <summary>Removes all windows that started before <paramref name="before"/>.</summary>
        Task<int> ExpireOldWindowsAsync(DateTimeOffset before, CancellationToken cancellationToken = default);

        Task<long> CountAsync(CancellationToken cancellationToken = default);
    }

}
