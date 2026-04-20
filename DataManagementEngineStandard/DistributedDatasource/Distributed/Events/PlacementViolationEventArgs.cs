using System;

namespace TheTechIdea.Beep.Distributed.Events
{
    /// <summary>
    /// Raised when a routing or plan-application step encounters a
    /// configuration that violates the active distribution plan — for
    /// example a missing entity placement, a placement that points to
    /// an unknown shard ID, or a row whose partition key cannot be
    /// extracted. Phase 02 is the first phase to actually raise this;
    /// Phase 01 only declares the event so observability/log wiring can
    /// be set up early.
    /// </summary>
    public sealed class PlacementViolationEventArgs : EventArgs
    {
        /// <summary>Initialises a new placement-violation event payload.</summary>
        /// <param name="entityName">Logical entity name involved in the violation. Never <c>null</c>.</param>
        /// <param name="shardId">Shard that was requested but missing / invalid; may be <c>null</c> if the entity itself is unmapped.</param>
        /// <param name="reason">Human-readable description suitable for logs and audit.</param>
        /// <param name="severity">Severity classification used by listeners to decide between log / alert / fail-fast.</param>
        public PlacementViolationEventArgs(
            string entityName,
            string shardId,
            string reason,
            PlacementViolationSeverity severity = PlacementViolationSeverity.Error)
        {
            EntityName   = entityName ?? throw new ArgumentNullException(nameof(entityName));
            ShardId      = shardId;
            Reason       = reason ?? string.Empty;
            Severity     = severity;
            TimestampUtc = DateTime.UtcNow;
        }

        /// <summary>Logical entity name involved in the violation.</summary>
        public string EntityName { get; }

        /// <summary>Shard identifier that the plan referenced, when applicable. <c>null</c> when the entity itself has no placement.</summary>
        public string ShardId { get; }

        /// <summary>Human-readable description of the violation.</summary>
        public string Reason { get; }

        /// <summary>Severity classification.</summary>
        public PlacementViolationSeverity Severity { get; }

        /// <summary>UTC timestamp the violation was detected.</summary>
        public DateTime TimestampUtc { get; }
    }

    /// <summary>
    /// Severity classification for a <see cref="PlacementViolationEventArgs"/>
    /// payload. Listeners (log/audit/alert) decide what to do per level.
    /// </summary>
    public enum PlacementViolationSeverity
    {
        /// <summary>Informational — placement was repaired automatically (e.g. fallback shard chosen).</summary>
        Info = 0,

        /// <summary>Recoverable — operation continued but the plan should be reviewed.</summary>
        Warning = 1,

        /// <summary>Operation failed because the plan is invalid for this entity / key.</summary>
        Error = 2
    }
}
