using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ── IProjection ───────────────────────────────────────────────────────────

    /// <summary>
    /// Transforms an incoming domain event into an updated read-model.
    /// Projections are pure functions: given the same event + current model, they produce the same result.
    /// </summary>
    /// <typeparam name="TEvent">The domain event type this projection handles.</typeparam>
    /// <typeparam name="TReadModel">The denormalized read / query model being built.</typeparam>
    public interface IProjection<TEvent, TReadModel>
    {
        /// <summary>
        /// Projects <paramref name="receivedEvent"/> onto <paramref name="currentModel"/>
        /// and returns the updated model.
        /// </summary>
        Task<TReadModel> ProjectAsync(
            ReceivedEvent<TEvent> receivedEvent,
            TReadModel            currentModel,
            CancellationToken     cancellationToken = default);
    }

    // ── IProjectionStore ──────────────────────────────────────────────────────

    /// <summary>Read/write store for projection read-models.</summary>
    /// <typeparam name="TId">Primary key type (e.g. <c>string</c>, <c>Guid</c>).</typeparam>
    /// <typeparam name="TReadModel">The read-model type persisted in the store.</typeparam>
    public interface IProjectionStore<TId, TReadModel>
    {
        /// <summary>Returns the read-model for <paramref name="id"/>, or <c>default</c> if not found.</summary>
        Task<TReadModel?> GetAsync(TId id, CancellationToken cancellationToken = default);

        /// <summary>Upserts the read-model.</summary>
        Task SaveAsync(TId id, TReadModel model, CancellationToken cancellationToken = default);

        /// <summary>Removes the read-model for <paramref name="id"/> if it exists.</summary>
        Task DeleteAsync(TId id, CancellationToken cancellationToken = default);
    }

    // ── ProjectionCheckpointKey ───────────────────────────────────────────────

    /// <summary>
    /// Uniquely identifies the last-processed position (offset/version) for a projection
    /// on a specific (topic, consumer-group) pair. Used for resumable projection runs.
    /// </summary>
    public sealed record ProjectionCheckpointKey(
        string ProjectionName,
        string Topic,
        string ConsumerGroup);

    // ── IProjectionRunner ─────────────────────────────────────────────────────

    /// <summary>
    /// Drives a projection by reading events from a source and writing to a store,
    /// maintaining a durable checkpoint for resumable, at-least-once processing.
    /// </summary>
    /// <typeparam name="TEvent">Domain event type consumed from the source.</typeparam>
    /// <typeparam name="TId">Read-model primary key type.</typeparam>
    /// <typeparam name="TReadModel">Read-model type produced by the projection.</typeparam>
    public interface IProjectionRunner<TEvent, TId, TReadModel>
    {
        /// <summary>
        /// Starts consuming events, projecting them via the supplied <see cref="IProjection{TEvent,TReadModel}"/>,
        /// and persisting results via <see cref="IProjectionStore{TId,TReadModel}"/>.
        /// </summary>
        /// <param name="topic">Source topic.</param>
        /// <param name="consumerGroup">Consumer group used for the projection run.</param>
        /// <param name="projection">Stateless projection function.</param>
        /// <param name="store">Read-model store.</param>
        /// <param name="resolveId">
        ///   Extracts the read-model primary key from a received event.
        ///   The runner uses this to load the current model before projecting.
        /// </param>
        /// <param name="checkpointKey">
        ///   Key used to load/save the last-processed offset so the runner can resume.
        /// </param>
        /// <param name="cancellationToken">Stop token — the runner exits gracefully when cancelled.</param>
        Task RunAsync(
            string                              topic,
            string                              consumerGroup,
            IProjection<TEvent, TReadModel>     projection,
            IProjectionStore<TId, TReadModel>   store,
            Func<ReceivedEvent<TEvent>, TId>    resolveId,
            ProjectionCheckpointKey             checkpointKey,
            CancellationToken                   cancellationToken = default);
    }
}
