using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Distributed.Events;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Transactions
{
    /// <summary>
    /// Default implementation of
    /// <see cref="IDistributedTransactionCoordinator"/>. Root partial:
    /// ctor, scope registry, shared helpers. Strategy-specific work
    /// lives in <c>DistributedTransactionCoordinator.SingleShard.cs</c>,
    /// <c>.TwoPhaseCommit.cs</c>, and <c>.Saga.cs</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The coordinator is intentionally decoupled from
    /// <c>DistributedDataSource</c>: it receives the shard map via a
    /// delegate (<see cref="ResolveShards"/>) and the in-doubt event
    /// via an <see cref="Action{T}"/>. That way the coordinator can
    /// be used in tests with synthetic clusters without a full
    /// datasource.
    /// </para>
    /// <para>
    /// Scopes are tracked in an in-memory registry keyed by
    /// correlation id. The registry is only used to reject stale
    /// <c>CommitScope</c>/<c>RollbackScope</c> calls (scope already
    /// closed) — it is not authoritative for recovery, which is
    /// Phase 13's job.
    /// </para>
    /// </remarks>
    public sealed partial class DistributedTransactionCoordinator : IDistributedTransactionCoordinator
    {
        private readonly Func<IReadOnlyDictionary<string, IProxyCluster>> _resolveShards;
        private readonly Action<TransactionInDoubtEventArgs>              _raiseInDoubt;
        private readonly IDistributedTransactionLog                       _log;
        private readonly bool                                             _preferSagaDefault;

        private readonly ConcurrentDictionary<string, DistributedTransactionScope> _scopes
            = new ConcurrentDictionary<string, DistributedTransactionScope>(StringComparer.Ordinal);

        /// <summary>Creates a new coordinator.</summary>
        /// <param name="resolveShards">
        /// Returns the live shard map. Called on every scope open so
        /// the coordinator always sees the current topology — no
        /// stale snapshots.
        /// </param>
        /// <param name="raiseInDoubt">
        /// Callback invoked when a 2PC scope enters
        /// <see cref="DistributedTransactionStatus.InDoubt"/>. Typical
        /// binding is <c>DistributedDataSource.RaiseTransactionInDoubt</c>.
        /// May be <c>null</c> to silently log only.
        /// </param>
        /// <param name="log">
        /// Optional log instance. Defaults to a new
        /// <see cref="InMemoryTransactionLog"/>.
        /// </param>
        /// <param name="preferSagaOverTwoPhaseCommit">
        /// Default policy used when the caller does not override it
        /// at <see cref="Begin"/> time.
        /// </param>
        public DistributedTransactionCoordinator(
            Func<IReadOnlyDictionary<string, IProxyCluster>>  resolveShards,
            Action<TransactionInDoubtEventArgs>               raiseInDoubt              = null,
            IDistributedTransactionLog                        log                       = null,
            bool                                              preferSagaOverTwoPhaseCommit = false)
        {
            _resolveShards     = resolveShards ?? throw new ArgumentNullException(nameof(resolveShards));
            _raiseInDoubt      = raiseInDoubt  ?? (_ => { });
            _log               = log           ?? new InMemoryTransactionLog();
            _preferSagaDefault = preferSagaOverTwoPhaseCommit;
        }

        /// <inheritdoc/>
        public IDistributedTransactionLog Log => _log;

        /// <inheritdoc/>
        public DistributedTransactionScope Begin(
            IReadOnlyList<string>   shardIds,
            string                  label                        = null,
            bool                    preferSagaOverTwoPhaseCommit = false)
        {
            if (shardIds == null) throw new ArgumentNullException(nameof(shardIds));
            if (shardIds.Count == 0)
                throw new ArgumentException(
                    "A distributed transaction must enlist at least one shard.",
                    nameof(shardIds));

            var shards   = _resolveShards() ?? EmptyShards;
            var distinct = shardIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (distinct.Length == 0)
                throw new ArgumentException(
                    "No valid shard ids supplied.", nameof(shardIds));

            var preferSaga = preferSagaOverTwoPhaseCommit || _preferSagaDefault;
            var strategy   = TransactionDecisionResolver.Resolve(distinct, shards, preferSaga);

            var correlationId = Guid.NewGuid().ToString("N");
            var scope         = new DistributedTransactionScope(
                correlationId:  correlationId,
                strategy:       strategy,
                shardIds:       distinct,
                label:          label,
                openedUtc:      DateTime.UtcNow);

            _scopes[correlationId] = scope;

            _log.Append(TransactionLogEntry.Now(
                correlationId:  correlationId,
                kind:           TransactionLogKind.Begin,
                message:        $"strategy={strategy}; shards={string.Join(",", distinct)}"
                              + (string.IsNullOrEmpty(label) ? string.Empty : $"; label={label}")));

            return scope;
        }

        /// <inheritdoc/>
        public IErrorsInfo CommitScope(DistributedTransactionScope scope)
        {
            EnsureScopeOpen(scope);

            switch (scope.Strategy)
            {
                case TransactionStrategy.SingleShardFastPath:
                    return CommitSingleShard(scope);

                case TransactionStrategy.TwoPhaseCommit:
                    return CommitTwoPhaseCommit(scope);

                case TransactionStrategy.Saga:
                    // Saga scopes commit implicitly via RunSaga. A
                    // direct commit on a saga scope with no completed
                    // forward steps is a no-op: treat as success and
                    // close the scope. If steps already ran and
                    // succeeded, RunSaga already closed the scope, so
                    // EnsureScopeOpen will have thrown above.
                    return FinalizeAsCommitted(scope, "saga-commit-noop");

                default:
                    return new ErrorsInfo
                    {
                        Flag    = Errors.Failed,
                        Message = $"Unknown transaction strategy: {scope.Strategy}",
                    };
            }
        }

        /// <inheritdoc/>
        public IErrorsInfo RollbackScope(DistributedTransactionScope scope)
        {
            EnsureScopeOpen(scope);

            switch (scope.Strategy)
            {
                case TransactionStrategy.SingleShardFastPath:
                    return RollbackSingleShard(scope);

                case TransactionStrategy.TwoPhaseCommit:
                    return RollbackTwoPhaseCommit(scope);

                case TransactionStrategy.Saga:
                    // Saga rollback on a scope with no steps run is a
                    // no-op; with steps already run and completed, the
                    // scope would have been closed by RunSaga itself.
                    return FinalizeAsAborted(scope, "saga-rollback-noop");

                default:
                    return new ErrorsInfo
                    {
                        Flag    = Errors.Failed,
                        Message = $"Unknown transaction strategy: {scope.Strategy}",
                    };
            }
        }

        // ── Shared helpers ────────────────────────────────────────────────

        private static readonly IReadOnlyDictionary<string, IProxyCluster> EmptyShards
            = new Dictionary<string, IProxyCluster>(0);

        /// <summary>
        /// Returns the live cluster for the given shard id or throws
        /// <see cref="InvalidOperationException"/> when the shard has
        /// been removed between <c>Begin</c> and the current step.
        /// </summary>
        private IProxyCluster ResolveCluster(string shardId)
        {
            var shards = _resolveShards() ?? EmptyShards;
            if (!shards.TryGetValue(shardId, out var cluster) || cluster == null)
            {
                throw new InvalidOperationException(
                    $"Shard '{shardId}' is not registered in the distributed datasource; " +
                    "did the topology change mid-transaction?");
            }
            return cluster;
        }

        private void EnsureScopeOpen(DistributedTransactionScope scope)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (scope.IsClosed)
            {
                throw new InvalidOperationException(
                    $"Distributed transaction scope {scope.CorrelationId} is already " +
                    $"in terminal state {scope.Status}; open a new scope to continue.");
            }
            if (!_scopes.ContainsKey(scope.CorrelationId))
            {
                throw new InvalidOperationException(
                    $"Distributed transaction scope {scope.CorrelationId} is not " +
                    "owned by this coordinator.");
            }
        }

        private IErrorsInfo FinalizeAsCommitted(
            DistributedTransactionScope scope,
            string                      reason)
        {
            if (!scope.IsClosed)
            {
                scope.ForceStatus(DistributedTransactionStatus.Committed, reason);
            }
            _log.Append(TransactionLogEntry.Now(
                correlationId: scope.CorrelationId,
                kind:          TransactionLogKind.Closed,
                message:       $"committed ({reason})"));
            _scopes.TryRemove(scope.CorrelationId, out _);
            _log.Close(scope.CorrelationId);
            return Ok($"Distributed commit ({scope.Strategy}) — {reason}");
        }

        private IErrorsInfo FinalizeAsAborted(
            DistributedTransactionScope scope,
            string                      reason,
            Exception                   error = null)
        {
            if (!scope.IsClosed)
            {
                scope.ForceStatus(DistributedTransactionStatus.Aborted, reason);
            }
            _log.Append(TransactionLogEntry.Now(
                correlationId: scope.CorrelationId,
                kind:          TransactionLogKind.Closed,
                message:       $"aborted ({reason})",
                error:         error));
            _scopes.TryRemove(scope.CorrelationId, out _);
            _log.Close(scope.CorrelationId);
            return Fail($"Distributed rollback ({scope.Strategy}) — {reason}", error);
        }

        private void FinalizeAsInDoubt(
            DistributedTransactionScope scope,
            TransactionInDoubtEventArgs args)
        {
            scope.ForceStatus(DistributedTransactionStatus.InDoubt, args.ToString());
            _log.Append(TransactionLogEntry.Now(
                correlationId: scope.CorrelationId,
                kind:          TransactionLogKind.InDoubt,
                message:       args.ToString(),
                error:         args.FirstCommitError));
            try { _raiseInDoubt(args); }
            catch { /* never propagate from in-doubt notifier */ }
            _scopes.TryRemove(scope.CorrelationId, out _);
            _log.Close(scope.CorrelationId);
        }

        private static IErrorsInfo Ok(string message)
            => new ErrorsInfo { Flag = Errors.Ok, Message = message };

        private static IErrorsInfo Fail(string message, Exception error = null)
            => new ErrorsInfo { Flag = Errors.Failed, Message = message, Ex = error };
    }
}
