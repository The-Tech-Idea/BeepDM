using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.StreamandEvents
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Saga status
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Lifecycle state of a saga instance.</summary>
    public enum SagaStatus
    {
        NotStarted,
        Running,
        WaitingForReply,
        Compensating,
        Compensated,
        Completed,
        Failed,
        TimedOut
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ISagaState — marker
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Marker interface for all saga state types.
    /// Implementations must be default-constructible and serializable.
    /// </summary>
    public interface ISagaState { }

    // ═══════════════════════════════════════════════════════════════════════════
    // SagaStep
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Describes a single step in a saga, including its forward-handler, optional compensating
    /// handler, and the reply event it is waiting for before advancing.
    /// </summary>
    /// <typeparam name="TState">Saga state type.</typeparam>
    /// <typeparam name="TEvent">Event type that triggers this step's forward-handler.</typeparam>
    public sealed class SagaStep<TState, TEvent>
    {
        /// <summary>Unique name for this step (used for logging and idempotency tracking).</summary>
        public string StepName { get; init; } = string.Empty;

        /// <summary>
        /// Handles the incoming event, transforms the saga state, and returns the updated state.
        /// </summary>
        public Func<TState, TEvent, CancellationToken, Task<TState>> Handle { get; init; } = null!;

        /// <summary>
        /// Compensates the side-effects of this step during saga rollback.
        /// When <c>null</c> the step has no compensating action.
        /// </summary>
        public Func<TState, CancellationToken, Task>? Compensate { get; init; }

        /// <summary>
        /// Maximum time the saga will wait for the reply event before timing out.
        /// When <c>null</c> there is no saga-level timeout for this step.
        /// </summary>
        public TimeSpan? TimeoutAfter { get; init; }

        /// <summary>Topic name from which the reply event is expected.</summary>
        public string ExpectedReplyTopic { get; init; } = string.Empty;

        /// <summary>
        /// Fully-qualified CLR type name of the reply event (used to match
        /// incoming events to this step without a type-unsafe cast).
        /// </summary>
        public string ExpectedReplyType { get; init; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SagaInstance
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Runtime snapshot of a saga execution, persisted to and loaded from the
    /// <see cref="ISagaStateStore{TState}"/>.
    /// </summary>
    /// <typeparam name="TState">Saga state type.</typeparam>
    public sealed class SagaInstance<TState>
    {
        /// <summary>Unique saga instance identifier.</summary>
        public string SagaId { get; set; } = string.Empty;

        /// <summary>Human-readable name of the saga (e.g. "OrderFulfillment").</summary>
        public string SagaName { get; set; } = string.Empty;

        /// <summary>Current lifecycle status.</summary>
        public SagaStatus Status { get; set; }

        /// <summary>Domain-specific saga state.</summary>
        public TState State { get; set; } = default!;

        /// <summary>Zero-based index of the currently executing step.</summary>
        public int CurrentStepIndex { get; set; }

        /// <summary>Names of steps that have already been completed (used for idempotency).</summary>
        public IReadOnlyList<string> CompletedStepNames { get; set; } = Array.Empty<string>();

        /// <summary>When the saga was started.</summary>
        public DateTimeOffset StartedAt { get; set; }

        /// <summary>When the saga state was last persisted.</summary>
        public DateTimeOffset LastUpdatedAt { get; set; }

        /// <summary>When the saga reached a terminal state (Completed / Compensated / Failed / TimedOut).</summary>
        public DateTimeOffset? CompletedAt { get; set; }

        /// <summary>Human-readable reason for failure (when <see cref="Status"/> is Failed or TimedOut).</summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// Correlation ID propagated across all commands and replies belonging to this saga.
        /// Used to route incoming events back to the saga instance.
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;

        /// <summary>Optimistic-concurrency version; incremented on each save.</summary>
        public long Version { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ISagaStateStore
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Persistence contract for saga instances. Implementations must support
    /// optimistic concurrency via the <paramref name="expectedVersion"/> parameter
    /// on <see cref="SaveAsync"/>.
    /// </summary>
    /// <typeparam name="TState">Saga state type.</typeparam>
    public interface ISagaStateStore<TState>
    {
        /// <summary>Loads a saga instance by ID, or <c>null</c> if not found.</summary>
        Task<SagaInstance<TState>?> LoadAsync(string sagaId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists a saga instance.
        /// Throws <see cref="SagaConcurrencyException"/> when the stored version does not match
        /// <paramref name="expectedVersion"/>.
        /// </summary>
        Task SaveAsync(SagaInstance<TState> instance, long expectedVersion, CancellationToken cancellationToken = default);

        /// <summary>Removes a saga instance from the store.</summary>
        Task DeleteAsync(string sagaId, CancellationToken cancellationToken = default);

        /// <summary>Streams saga instances that are in the given <paramref name="status"/>.</summary>
        IAsyncEnumerable<SagaInstance<TState>> ListByStatusAsync(
            SagaStatus status,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams saga instances whose current step has a <c>TimeoutAfter</c> and whose
        /// <see cref="SagaInstance{TState}.LastUpdatedAt"/> is before <paramref name="before"/>.
        /// </summary>
        IAsyncEnumerable<SagaInstance<TState>> ListTimedOutAsync(
            DateTimeOffset before,
            CancellationToken cancellationToken = default);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Saga exceptions
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Thrown when a saga is started with an ID that already exists in the store.
    /// </summary>
    [Serializable]
    public sealed class SagaAlreadyExistsException : Exception
    {
        public string SagaId { get; }

        public SagaAlreadyExistsException(string sagaId)
            : base($"A saga with id '{sagaId}' already exists.")
            => SagaId = sagaId;

        private SagaAlreadyExistsException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
            => SagaId = info.GetString(nameof(SagaId)) ?? string.Empty;

        public override void GetObjectData(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(SagaId), SagaId);
        }
    }

    /// <summary>
    /// Thrown when the stored version of a saga does not match the expected version
    /// (optimistic-concurrency violation).
    /// </summary>
    [Serializable]
    public sealed class SagaConcurrencyException : Exception
    {
        public string SagaId          { get; }
        public long   ExpectedVersion { get; }
        public long   ActualVersion   { get; }

        public SagaConcurrencyException(string sagaId, long expectedVersion, long actualVersion)
            : base($"Concurrency conflict for saga '{sagaId}': expected version {expectedVersion}, found {actualVersion}.")
        {
            SagaId          = sagaId;
            ExpectedVersion = expectedVersion;
            ActualVersion   = actualVersion;
        }

        private SagaConcurrencyException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            SagaId          = info.GetString(nameof(SagaId))          ?? string.Empty;
            ExpectedVersion = info.GetInt64(nameof(ExpectedVersion));
            ActualVersion   = info.GetInt64(nameof(ActualVersion));
        }

        public override void GetObjectData(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(SagaId),          SagaId);
            info.AddValue(nameof(ExpectedVersion), ExpectedVersion);
            info.AddValue(nameof(ActualVersion),   ActualVersion);
        }
    }

    /// <summary>
    /// Thrown when a saga step times out while waiting for a reply event.
    /// </summary>
    [Serializable]
    public sealed class SagaTimeoutException : Exception
    {
        public string   SagaId       { get; }
        public string   StepName     { get; }
        public TimeSpan TimeoutAfter { get; }

        public SagaTimeoutException(string sagaId, string stepName, TimeSpan timeoutAfter)
            : base($"Saga '{sagaId}' timed out waiting for step '{stepName}' (timeout: {timeoutAfter}).")
        {
            SagaId       = sagaId;
            StepName     = stepName;
            TimeoutAfter = timeoutAfter;
        }

        private SagaTimeoutException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            SagaId       = info.GetString(nameof(SagaId))   ?? string.Empty;
            StepName     = info.GetString(nameof(StepName)) ?? string.Empty;
            TimeoutAfter = (TimeSpan)(info.GetValue(nameof(TimeoutAfter), typeof(TimeSpan)) ?? TimeSpan.Zero);
        }

        public override void GetObjectData(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(SagaId),       SagaId);
            info.AddValue(nameof(StepName),     StepName);
            info.AddValue(nameof(TimeoutAfter), TimeoutAfter);
        }
    }
}
