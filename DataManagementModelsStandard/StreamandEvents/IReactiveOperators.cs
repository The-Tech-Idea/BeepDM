using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;

namespace TheTechIdea.Beep.StreamandEvents
{
    /// <summary>
    /// Fluent, operator-rich reactive stream over event payloads.
    /// All operators are lazy; materialisation happens at <see cref="ToAsyncEnumerable"/> /
    /// <see cref="ToChannelReader"/>.
    /// </summary>
    public interface IReactiveEventStream<T>
    {
        // ── Filtering / Mapping ───────────────────────────────────────────────

        IReactiveEventStream<T> Where(Func<T, bool> predicate);

        IReactiveEventStream<TOut> Select<TOut>(Func<T, TOut> selector);

        /// <summary>Projects each element to an async sequence and flattens the results.</summary>
        IReactiveEventStream<TOut> SelectMany<TOut>(Func<T, IAsyncEnumerable<TOut>> selector);

        // ── Windowing ─────────────────────────────────────────────────────────

        /// <summary>Groups consecutive events into windows according to <paramref name="policy"/>.</summary>
        IReactiveEventStream<IList<T>> Buffer(RxWindowPolicy policy);

        // ── Rate-limiting ─────────────────────────────────────────────────────

        IReactiveEventStream<T> Throttle(RxThrottlePolicy policy);

        IReactiveEventStream<T> DistinctUntilChanged(Func<T, object>? keySelector = null);

        // ── Combining ─────────────────────────────────────────────────────────

        /// <summary>Merges events from <paramref name="other"/> into this stream.</summary>
        IReactiveEventStream<T> Merge(IReactiveEventStream<T> other);

        /// <summary>
        /// Pairs each element of this stream with the corresponding element from
        /// <paramref name="other"/> and projects them through <paramref name="resultSelector"/>.
        /// </summary>
        IReactiveEventStream<TOut> Zip<TOther, TOut>(
            IReactiveEventStream<TOther> other,
            Func<T, TOther, TOut> resultSelector);

        /// <summary>
        /// Flattens a stream of inner streams by switching to the latest inner stream on each
        /// outer emission. The caller must ensure that <typeparamref name="T"/> is
        /// <see cref="IReactiveEventStream{TInner}"/>; an <see cref="InvalidCastException"/> is
        /// thrown at runtime if it is not.
        /// </summary>
        IReactiveEventStream<TInner> Switch<TInner>();

        // ── Error handling ────────────────────────────────────────────────────

        /// <summary>Retries the upstream on error with exponential back-off.</summary>
        IReactiveEventStream<T> Retry(int maxRetries, TimeSpan backoff);

        /// <summary>Substitutes a fallback stream on error.</summary>
        IReactiveEventStream<T> Catch(Func<Exception, IReactiveEventStream<T>> fallback);

        // ── Materialisation ───────────────────────────────────────────────────

        /// <summary>Materialises the stream as a pull-based async sequence.</summary>
        IAsyncEnumerable<T> ToAsyncEnumerable(CancellationToken cancellationToken = default);

        /// <summary>
        /// Materialises the stream into a bounded channel; the caller reads from
        /// <see cref="ChannelReader{T}"/>.
        /// </summary>
        ChannelReader<T> ToChannelReader(int capacity, CancellationToken cancellationToken = default);
    }
}
