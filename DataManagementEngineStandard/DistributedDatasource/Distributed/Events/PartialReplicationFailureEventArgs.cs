using System;
using TheTechIdea.Beep.Distributed.Execution;

namespace TheTechIdea.Beep.Distributed.Events
{
    /// <summary>
    /// Raised by <see cref="DistributedDataSource"/> after a replicated
    /// / broadcast write satisfied its quorum but one or more target
    /// shards still failed. Operators subscribe to reconcile the
    /// divergent replicas (e.g. replay the missing write, invalidate a
    /// stale cache, alert).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Full failure (quorum NOT satisfied) is reported via the normal
    /// <see cref="IErrorsInfo"/> return path and does NOT raise this
    /// event — this signal is specifically about the "quorum met but
    /// not every replica ack'd" case that benefits from a lightweight
    /// reconciliation hook rather than a hard error.
    /// </para>
    /// <para>
    /// The full per-shard breakdown is exposed via
    /// <see cref="Outcome"/>; subscribers should not mutate the
    /// outcome.
    /// </para>
    /// </remarks>
    public sealed class PartialReplicationFailureEventArgs : EventArgs
    {
        /// <summary>Creates a new partial-failure event.</summary>
        /// <param name="outcome">Write outcome produced by the executor. Must have <see cref="WriteOutcome.IsPartial"/> == <c>true</c>.</param>
        public PartialReplicationFailureEventArgs(WriteOutcome outcome)
        {
            Outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
            RaisedUtc = DateTime.UtcNow;
        }

        /// <summary>Full write outcome — per-shard results, quorum state, first error.</summary>
        public WriteOutcome Outcome   { get; }

        /// <summary>UTC timestamp captured when the event was raised.</summary>
        public DateTime     RaisedUtc { get; }

        /// <summary>Convenience accessor for <see cref="WriteOutcome.EntityName"/>.</summary>
        public string       EntityName => Outcome.EntityName;

        /// <summary>Convenience accessor for <see cref="WriteOutcome.Operation"/>.</summary>
        public string       Operation  => Outcome.Operation;

        /// <inheritdoc/>
        public override string ToString()
            => $"PartialReplication({EntityName}/{Operation}, " +
               $"failed={Outcome.FailureCount}/{Outcome.PerShard.Count}, raised={RaisedUtc:O})";
    }
}
