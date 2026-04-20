using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Distributed.Execution
{
    /// <summary>
    /// Aggregate outcome of a distributed write: the per-shard
    /// <see cref="WriteFanOutResult"/>s, whether the quorum was
    /// satisfied, and the first error observed (for quick
    /// <c>IErrorsInfo</c> conversion).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The write executor always produces a <see cref="WriteOutcome"/>
    /// — it never throws on per-shard failure. The caller inspects
    /// <see cref="QuorumSatisfied"/> and decides whether to treat
    /// the write as success, partial success, or failure.
    /// </para>
    /// <para>
    /// <see cref="IsPartial"/> distinguishes "everything succeeded"
    /// from "quorum met but some replicas failed" so the datasource
    /// can raise
    /// <see cref="Events.PartialReplicationFailureEventArgs"/>
    /// for the latter case.
    /// </para>
    /// </remarks>
    public sealed class WriteOutcome
    {
        /// <summary>Creates a new outcome from the per-shard leg list.</summary>
        /// <param name="entityName">Target entity.</param>
        /// <param name="operation">Operation name ("InsertEntity", "UpdateEntity", …).</param>
        /// <param name="perShard">Per-shard results in attempt order.</param>
        /// <param name="requiredAckCount">Minimum successful shards to satisfy quorum.</param>
        /// <param name="quorumPolicy">Resolved quorum policy at execution time.</param>
        public WriteOutcome(
            string                            entityName,
            string                            operation,
            IReadOnlyList<WriteFanOutResult>  perShard,
            int                               requiredAckCount,
            QuorumPolicy                      quorumPolicy)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));
            if (perShard == null)
                throw new ArgumentNullException(nameof(perShard));

            EntityName       = entityName;
            Operation        = operation ?? string.Empty;
            PerShard         = perShard;
            RequiredAckCount = requiredAckCount < 0 ? 0 : requiredAckCount;
            QuorumPolicy     = quorumPolicy;

            int ok = 0;
            Exception firstError = null;
            for (int i = 0; i < perShard.Count; i++)
            {
                var leg = perShard[i];
                if (leg.Succeeded) ok++;
                else if (firstError == null) firstError = leg.Error;
            }
            SuccessCount = ok;
            FirstError   = firstError;
        }

        /// <summary>Entity the write targeted.</summary>
        public string EntityName { get; }

        /// <summary>Operation name ("InsertEntity", "UpdateEntity", …).</summary>
        public string Operation { get; }

        /// <summary>Per-shard results in attempt order.</summary>
        public IReadOnlyList<WriteFanOutResult> PerShard { get; }

        /// <summary>Minimum successful shards needed to satisfy the quorum.</summary>
        public int RequiredAckCount { get; }

        /// <summary>Quorum policy evaluated for this write.</summary>
        public QuorumPolicy QuorumPolicy { get; }

        /// <summary>Number of successful per-shard legs.</summary>
        public int SuccessCount { get; }

        /// <summary>Number of failed per-shard legs.</summary>
        public int FailureCount => PerShard.Count - SuccessCount;

        /// <summary>First failure observed; <c>null</c> when every leg succeeded.</summary>
        public Exception FirstError { get; }

        /// <summary><c>true</c> when <see cref="SuccessCount"/> is &gt;= <see cref="RequiredAckCount"/>.</summary>
        public bool QuorumSatisfied => SuccessCount >= RequiredAckCount;

        /// <summary><c>true</c> when the quorum was met but at least one leg still failed.</summary>
        public bool IsPartial => QuorumSatisfied && FailureCount > 0;

        /// <summary><c>true</c> when every leg succeeded.</summary>
        public bool IsFullySuccessful => FailureCount == 0;

        /// <inheritdoc/>
        public override string ToString()
            => $"WriteOutcome({EntityName}/{Operation}, " +
               $"ack={SuccessCount}/{PerShard.Count}, required={RequiredAckCount}, " +
               $"policy={QuorumPolicy}, quorum={QuorumSatisfied}, partial={IsPartial})";
    }
}
