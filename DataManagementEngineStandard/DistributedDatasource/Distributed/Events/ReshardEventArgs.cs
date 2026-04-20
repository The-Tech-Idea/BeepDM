using System;

namespace TheTechIdea.Beep.Distributed.Events
{
    /// <summary>
    /// Payload for the <c>OnReshardStarted</c> and
    /// <c>OnReshardCompleted</c> events on
    /// <see cref="DistributedDataSource"/>. Phase 11 is the first phase
    /// to actually raise these events; Phase 01 declares the event
    /// shape so subscribers (audit, dashboards, runbooks) can be
    /// wired in early.
    /// </summary>
    public sealed class ReshardEventArgs : EventArgs
    {
        /// <summary>Initialises a new reshard lifecycle event payload.</summary>
        /// <param name="reshardId">Stable identifier for the reshard operation. Never <c>null</c>.</param>
        /// <param name="phase">Lifecycle phase of the operation.</param>
        /// <param name="reason">Human-readable description of why the reshard was triggered.</param>
        /// <param name="affectedEntities">Approximate count of distinct entities affected. <c>null</c> when not yet known.</param>
        /// <param name="error">Failure detail when <paramref name="phase"/> is <see cref="ReshardPhase.Failed"/>.</param>
        public ReshardEventArgs(
            string reshardId,
            ReshardPhase phase,
            string reason,
            int? affectedEntities = null,
            Exception error = null)
        {
            ReshardId         = reshardId ?? throw new ArgumentNullException(nameof(reshardId));
            Phase             = phase;
            Reason            = reason ?? string.Empty;
            AffectedEntities  = affectedEntities;
            Error             = error;
            TimestampUtc      = DateTime.UtcNow;
        }

        /// <summary>Stable identifier for the reshard operation.</summary>
        public string ReshardId { get; }

        /// <summary>Lifecycle phase the event represents.</summary>
        public ReshardPhase Phase { get; }

        /// <summary>Human-readable description of why the reshard was triggered.</summary>
        public string Reason { get; }

        /// <summary>Approximate number of distinct entities affected.</summary>
        public int? AffectedEntities { get; }

        /// <summary>Failure detail when <see cref="Phase"/> is <see cref="ReshardPhase.Failed"/>.</summary>
        public Exception Error { get; }

        /// <summary>UTC timestamp the event was raised.</summary>
        public DateTime TimestampUtc { get; }
    }

    /// <summary>
    /// Lifecycle phase carried by a <see cref="ReshardEventArgs"/>.
    /// </summary>
    public enum ReshardPhase
    {
        /// <summary>Reshard operation has been planned and is about to execute.</summary>
        Started = 0,

        /// <summary>Reshard operation completed successfully.</summary>
        Completed = 1,

        /// <summary>Reshard operation failed; <c>Error</c> contains the cause.</summary>
        Failed = 2
    }
}
