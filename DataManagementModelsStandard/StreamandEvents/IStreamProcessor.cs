using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── StreamProcessorResult ─────────────────────────────────────────────────

    /// <summary>
    /// Decision token returned by <see cref="IStreamProcessor{TIn,TOut}.ProcessAsync"/>.
    /// Exactly one of <see cref="ShouldForward"/>, <see cref="ShouldDrop"/>,
    /// or <see cref="ShouldDeadLetter"/> is true for any given result.
    /// </summary>
    public sealed class StreamProcessorResult<TOut>
    {
        public TOut?   Output          { get; init; }
        public bool    ShouldForward   { get; init; }
        public bool    ShouldDrop      { get; init; }
        public bool    ShouldDeadLetter { get; init; }
        public string? FailureReason   { get; init; }

        public static StreamProcessorResult<TOut> Forward(TOut output)
            => new() { Output = output, ShouldForward = true };

        public static StreamProcessorResult<TOut> Drop()
            => new() { ShouldDrop = true };

        public static StreamProcessorResult<TOut> DeadLetter(string reason)
            => new() { ShouldDeadLetter = true, FailureReason = reason };
    }

    // ── IStreamProcessor ──────────────────────────────────────────────────────

    /// <summary>
    /// General-purpose stream processing stage that transforms one event type to another
    /// and signals the pipeline how to handle the result.
    /// </summary>
    public interface IStreamProcessor<TIn, TOut>
    {
        Task<StreamProcessorResult<TOut>> ProcessAsync(
            ReceivedEvent<TIn> incomingEvent,
            CancellationToken  cancellationToken = default);
    }

    // ── IStreamFilter ─────────────────────────────────────────────────────────

    /// <summary>Stateless predicate that decides whether an event should flow downstream.</summary>
    public interface IStreamFilter<T>
    {
        Task<bool> ShouldPassAsync(
            ReceivedEvent<T>  incomingEvent,
            CancellationToken cancellationToken = default);
    }

    // ── IStreamMapper ─────────────────────────────────────────────────────────

    /// <summary>Stateless 1-to-1 event transformation.</summary>
    public interface IStreamMapper<TIn, TOut>
    {
        Task<TOut> MapAsync(
            ReceivedEvent<TIn> incomingEvent,
            CancellationToken  cancellationToken = default);
    }

    // ── IStreamFlatMapper ─────────────────────────────────────────────────────

    /// <summary>Stateless 1-to-many event transformation (flat-map / selectMany).</summary>
    public interface IStreamFlatMapper<TIn, TOut>
    {
        IAsyncEnumerable<TOut> FlatMapAsync(
            ReceivedEvent<TIn> incomingEvent,
            CancellationToken  cancellationToken = default);
    }

    // ── WindowBoundary ────────────────────────────────────────────────────────

    /// <summary>Closed time-range describing the bounds of an aggregation window.</summary>
    public sealed record WindowBoundary(DateTimeOffset WindowStart, DateTimeOffset WindowEnd);

    // ── IStreamAggregator ─────────────────────────────────────────────────────

    /// <summary>
    /// Stateful aggregation operator. The pipeline engine calls
    /// <see cref="AggregateAsync"/> for each event within a window and
    /// <see cref="FinalizeAsync"/> when the window closes.
    /// </summary>
    public interface IStreamAggregator<TIn, TState, TOut>
    {
        /// <summary>Returns the initial (empty) state for a new window.</summary>
        TState InitializeState();

        /// <summary>Folds one event into the current window state.</summary>
        Task<TState> AggregateAsync(
            TState             currentState,
            ReceivedEvent<TIn> incomingEvent,
            CancellationToken  cancellationToken = default);

        /// <summary>Produces the final output for a closed window.</summary>
        Task<TOut> FinalizeAsync(
            TState            finalState,
            WindowBoundary    window,
            CancellationToken cancellationToken = default);
    }
}
