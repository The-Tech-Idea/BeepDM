using System;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Transactions
{
    /// <summary>
    /// Immutable descriptor for one step in a saga-mode
    /// <see cref="DistributedTransactionScope"/>. Each step pairs a
    /// forward operation with an idempotent compensation that undoes
    /// the forward on failure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The coordinator treats <paramref name="shardId"/> as the
    /// shard against which <see cref="Forward"/> and
    /// <see cref="Compensation"/> are executed. Both delegates
    /// receive the resolved <see cref="IProxyCluster"/> for that
    /// shard so callers don't need to look it up themselves.
    /// </para>
    /// <para>
    /// Compensations MUST be idempotent and tolerant of being called
    /// after a partial forward — the runner cannot tell whether a
    /// step partially succeeded before it threw. V1 runs
    /// compensations once; Phase 13 adds retry with back-off.
    /// </para>
    /// </remarks>
    public sealed class SagaStep
    {
        /// <summary>Creates a new step.</summary>
        public SagaStep(
            string                              name,
            string                              shardId,
            Func<IProxyCluster, IErrorsInfo>    forward,
            Func<IProxyCluster, IErrorsInfo>    compensation)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Saga step name cannot be null or whitespace.", nameof(name));
            if (string.IsNullOrWhiteSpace(shardId))
                throw new ArgumentException("Saga step shardId cannot be null or whitespace.", nameof(shardId));

            Name          = name;
            ShardId       = shardId;
            Forward       = forward       ?? throw new ArgumentNullException(nameof(forward));
            Compensation  = compensation  ?? throw new ArgumentNullException(nameof(compensation));
        }

        /// <summary>Unique (within the scope) step name — appears in log entries.</summary>
        public string Name { get; }

        /// <summary>Shard the step targets.</summary>
        public string ShardId { get; }

        /// <summary>Forward delegate — runs during the commit phase.</summary>
        public Func<IProxyCluster, IErrorsInfo> Forward { get; }

        /// <summary>Compensation delegate — runs during rollback in reverse order.</summary>
        public Func<IProxyCluster, IErrorsInfo> Compensation { get; }

        /// <inheritdoc/>
        public override string ToString()
            => $"SagaStep({Name} @ {ShardId})";
    }
}
