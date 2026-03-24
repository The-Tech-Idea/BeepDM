using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── Window policy ─────────────────────────────────────────────────────────

    public enum RxWindowMode { Tumbling, Hopping, Session, Count, CountOrTime }

    /// <summary>Controls how events are grouped into buffered windows by Rx operators.</summary>
    public sealed class RxWindowPolicy
    {
        public RxWindowMode Mode       { get; init; }
        public TimeSpan     WindowSize { get; init; }
        public TimeSpan?    HopSize    { get; init; }   // Hopping only
        public int?         MaxCount   { get; init; }   // Count / CountOrTime
        public TimeSpan?    SessionGap { get; init; }   // Session only

        public static RxWindowPolicy Tumbling(TimeSpan size)
            => new() { Mode = RxWindowMode.Tumbling, WindowSize = size };

        public static RxWindowPolicy Count(int maxCount)
            => new() { Mode = RxWindowMode.Count, MaxCount = maxCount, WindowSize = TimeSpan.Zero };
    }

    // ── Throttle policy ───────────────────────────────────────────────────────

    public enum RxThrottleMode
    {
        /// <summary>Emit the first item; suppress subsequent items for the duration.</summary>
        Throttle,
        /// <summary>Emit the last item after the source has been silent for the duration.</summary>
        Debounce,
        /// <summary>Emit one item per period regardless of arrival rate.</summary>
        Sample
    }

    /// <summary>Controls leading-edge, trailing-edge, or periodic rate-limiting of a stream.</summary>
    public sealed class RxThrottlePolicy
    {
        public RxThrottleMode Mode    { get; init; }
        public TimeSpan       DueTime { get; init; }

        public static RxThrottlePolicy Debounce(TimeSpan dueTime)
            => new() { Mode = RxThrottleMode.Debounce, DueTime = dueTime };

        public static RxThrottlePolicy Throttle(TimeSpan dueTime)
            => new() { Mode = RxThrottleMode.Throttle, DueTime = dueTime };
    }

    // ── Merge policy ──────────────────────────────────────────────────────────

    public enum RxMergeOverflow { Queue, Drop, Fail }

    /// <summary>Controls fan-in behaviour when merging multiple observable/async-enumerable sources.</summary>
    public sealed class RxMergePolicy
    {
        public int             MaxConcurrency { get; init; } = int.MaxValue;
        public RxMergeOverflow OverflowMode   { get; init; } = RxMergeOverflow.Queue;
    }

    // ── IObservableStreamSource ───────────────────────────────────────────────

    /// <summary>Exposes an underlying async stream as a hot observable source.</summary>
    public interface IObservableStreamSource<T>
    {
        /// <summary>Returns a hot <see cref="IObservable{T}"/> backed by the underlying stream.</summary>
        IObservable<T> ToObservable();

        /// <summary>Subscribes the given observer directly.</summary>
        IDisposable Subscribe(IObserver<T> observer);
    }

    // ── IObservableStreamSink ─────────────────────────────────────────────────

    /// <summary>
    /// Accepts <see cref="IObserver{T}"/> notifications and writes them to an underlying channel.
    /// </summary>
    public interface IObservableStreamSink<T>
    {
        /// <summary>Returns an observer that bridges push notifications into the underlying channel.</summary>
        IObserver<T> AsObserver();

        void OnNext(T value);
        void OnError(Exception error);
        void OnCompleted();
    }
}
