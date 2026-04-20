using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Distributed.Transactions
{
    /// <summary>
    /// Phase 09 orchestrator for distributed transactions. Owns the
    /// lifecycle of a <see cref="DistributedTransactionScope"/>,
    /// picks the right <see cref="TransactionStrategy"/> via
    /// <see cref="TransactionDecisionResolver"/>, and runs the
    /// prepare / commit / rollback / saga sequences across the
    /// enlisted shards.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The coordinator is read-only from the caller's perspective:
    /// once a scope is opened it is threaded through
    /// <see cref="CommitScope"/> or <see cref="RollbackScope"/>
    /// verbatim. All state the coordinator needs lives either on the
    /// scope token or in the attached <see cref="IDistributedTransactionLog"/>.
    /// </para>
    /// <para>
    /// Strategies are implemented as partials of the concrete
    /// <c>DistributedTransactionCoordinator</c> class:
    /// <c>SingleShard.cs</c>, <c>TwoPhaseCommit.cs</c>, <c>Saga.cs</c>.
    /// </para>
    /// </remarks>
    public interface IDistributedTransactionCoordinator
    {
        /// <summary>Log used for prepare / commit / rollback entries.</summary>
        IDistributedTransactionLog Log { get; }

        /// <summary>
        /// Opens a new scope that enlists <paramref name="shardIds"/>.
        /// The coordinator picks the strategy at this moment; the
        /// choice is frozen on the returned scope.
        /// </summary>
        /// <param name="shardIds">Shards the scope will touch.</param>
        /// <param name="label">Optional human-readable label.</param>
        /// <param name="preferSagaOverTwoPhaseCommit">
        /// Override policy: when <c>true</c>, force saga for
        /// multi-shard work. Single-shard scopes always use the
        /// fast path regardless.
        /// </param>
        DistributedTransactionScope Begin(
            IReadOnlyList<string>   shardIds,
            string                  label                       = null,
            bool                    preferSagaOverTwoPhaseCommit = false);

        /// <summary>
        /// Commits the given scope according to the strategy captured
        /// at begin time. For saga mode, the caller must supply the
        /// ordered step list via
        /// <see cref="RunSaga"/> BEFORE invoking this method — a
        /// saga scope commit without prior step execution is a no-op
        /// returning success.
        /// </summary>
        IErrorsInfo CommitScope(DistributedTransactionScope scope);

        /// <summary>
        /// Rolls back the given scope. For single-shard / 2PC this
        /// translates to a cross-shard rollback/abort. For saga mode,
        /// the stored compensations (registered via
        /// <see cref="RunSaga"/>) are replayed in reverse order.
        /// </summary>
        IErrorsInfo RollbackScope(DistributedTransactionScope scope);

        /// <summary>
        /// Runs a saga: executes each step's <see cref="SagaStep.Forward"/>
        /// in order against its shard; on any failure, runs the
        /// completed steps' compensations in reverse. Only valid on
        /// scopes whose <see cref="DistributedTransactionScope.Strategy"/>
        /// is <see cref="TransactionStrategy.Saga"/>.
        /// </summary>
        IErrorsInfo RunSaga(DistributedTransactionScope scope, IReadOnlyList<SagaStep> steps);
    }
}
