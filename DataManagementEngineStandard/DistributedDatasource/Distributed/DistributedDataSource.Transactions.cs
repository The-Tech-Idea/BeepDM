using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Distributed.Execution;
using TheTechIdea.Beep.Distributed.Transactions;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// <see cref="DistributedDataSource"/> partial — Phase 09
    /// transaction surface. Implements the
    /// <see cref="IDataSource"/> transaction triple
    /// (<c>BeginTransaction</c> / <c>Commit</c> / <c>EndTransaction</c>)
    /// as a thin wrapper around the
    /// <see cref="IDistributedTransactionCoordinator"/> and exposes
    /// the richer distributed-scope API for callers that need 2PC or
    /// saga semantics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="IDataSource"/> triple only targets a single
    /// shard (derived from <see cref="PassedArgs.CurrentEntity"/> or,
    /// when the datasource owns exactly one shard, that shard). This
    /// matches Oracle Forms / classic Beep expectations: plug the
    /// distributed datasource into existing <c>IDataSource</c>
    /// consumers without behaviour changes as long as their work
    /// lives on one shard. Multi-shard transactions MUST go through
    /// the explicit
    /// <see cref="BeginDistributedTransaction(IReadOnlyList{string}, string, bool)"/>
    /// API so the caller opts into atomic 2PC or saga semantics.
    /// </para>
    /// <para>
    /// The single-flight model keeps one implicit
    /// <see cref="DistributedTransactionScope"/> per datasource
    /// instance: a second <c>BeginTransaction</c> without a prior
    /// commit/rollback is rejected. The explicit distributed API
    /// returns a fresh scope token and is therefore safe to use
    /// concurrently when callers own their own scopes.
    /// </para>
    /// </remarks>
    public partial class DistributedDataSource
    {
        private DistributedTransactionScope _currentImplicitScope;
        private readonly object             _txGate = new object();

        /// <summary>
        /// Coordinator instance. Exposed so advanced callers can
        /// swap the log (Phase 13), inspect active scopes, or plug
        /// in a custom implementation.
        /// </summary>
        public IDistributedTransactionCoordinator TransactionCoordinator
        {
            get => Volatile.Read(ref _txCoordinator);
            set => Volatile.Write(ref _txCoordinator,
                value ?? throw new ArgumentNullException(nameof(value)));
        }

        // ── IDataSource transaction triple ────────────────────────────────

        /// <inheritdoc/>
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ThrowIfDisposed();

            lock (_txGate)
            {
                if (_currentImplicitScope != null && !_currentImplicitScope.IsClosed)
                {
                    return Fail(
                        $"An implicit transaction is already open for this datasource "
                       + $"(correlation id {_currentImplicitScope.CorrelationId}). " +
                        "Commit or rollback the current scope before starting a new one.");
                }

                string shardId;
                try
                {
                    shardId = ResolveImplicitShardId(args);
                }
                catch (Exception ex)
                {
                    return Fail("Distributed BeginTransaction failed to resolve a shard.", ex);
                }

                var scope = _txCoordinator.Begin(
                    shardIds: new[] { shardId },
                    label:    args?.CurrentEntity);

                if (_txCoordinator is DistributedTransactionCoordinator concrete)
                {
                    var beginResult = concrete.BeginSingleShard(scope, args ?? new PassedArgs());
                    if (beginResult == null || beginResult.Flag != Errors.Ok)
                    {
                        _currentImplicitScope = null;
                        return beginResult ?? Fail("Single-shard BeginTransaction returned no result.");
                    }
                }

                _currentImplicitScope = scope;
                return Ok($"Distributed BeginTransaction opened on shard '{shardId}'.");
            }
        }

        /// <inheritdoc/>
        public IErrorsInfo Commit(PassedArgs args)
        {
            ThrowIfDisposed();

            DistributedTransactionScope scope;
            lock (_txGate)
            {
                scope = _currentImplicitScope;
                _currentImplicitScope = null;
            }

            if (scope == null)
            {
                return Fail(
                    "Distributed Commit called without an active implicit transaction. " +
                    "Use BeginDistributedTransaction + CommitDistributedTransaction for " +
                    "explicit multi-shard work.");
            }

            return _txCoordinator.CommitScope(scope);
        }

        /// <inheritdoc/>
        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            ThrowIfDisposed();

            DistributedTransactionScope scope;
            lock (_txGate)
            {
                scope = _currentImplicitScope;
                _currentImplicitScope = null;
            }

            if (scope == null)
            {
                // EndTransaction-without-begin is a common idempotent
                // cleanup pattern in Beep callers — treat as success.
                return Ok("Distributed EndTransaction: no active implicit transaction (noop).");
            }

            return _txCoordinator.RollbackScope(scope);
        }

        // ── Explicit distributed-scope API ────────────────────────────────

        /// <summary>
        /// Opens a multi-shard transaction scope explicitly. The
        /// coordinator selects
        /// <see cref="TransactionStrategy.TwoPhaseCommit"/> or
        /// <see cref="TransactionStrategy.Saga"/> based on the
        /// capability of the enlisted shards and the options /
        /// <paramref name="preferSaga"/> override.
        /// </summary>
        /// <param name="shardIds">Shards the scope will touch.</param>
        /// <param name="label">Optional human-readable label.</param>
        /// <param name="preferSaga">When <c>true</c>, force saga over 2PC for this scope.</param>
        public DistributedTransactionScope BeginDistributedTransaction(
            IReadOnlyList<string>  shardIds,
            string                 label       = null,
            bool                   preferSaga  = false)
        {
            ThrowIfDisposed();
            if (shardIds == null) throw new ArgumentNullException(nameof(shardIds));

            var scope = _txCoordinator.Begin(shardIds, label, preferSaga);

            RaiseAuditEvent(
                kind:          Audit.DistributedAuditEventKind.TransactionBegan,
                operation:     "BeginDistributedTransaction",
                shardIds:      shardIds,
                correlationId: scope?.CorrelationId,
                message:       $"strategy={scope?.Strategy}; label={label}; preferSaga={preferSaga}");

            if (scope.Strategy == TransactionStrategy.TwoPhaseCommit
             && _txCoordinator is DistributedTransactionCoordinator concrete)
            {
                var prepareResult = concrete.BeginTwoPhaseCommit(scope, new PassedArgs { Messege = label });
                if (prepareResult == null || prepareResult.Flag != Errors.Ok)
                {
                    // BeginTwoPhaseCommit already finalised the scope as aborted
                    // on failure; surface the error via an exception so callers
                    // don't silently commit an unprepared scope.
                    throw new InvalidOperationException(
                        $"Distributed 2PC prepare failed for scope {scope.CorrelationId}: "
                      + (prepareResult?.Message ?? "no result"),
                        prepareResult?.Ex);
                }
            }

            return scope;
        }

        /// <summary>Commits an explicitly-opened distributed scope.</summary>
        public IErrorsInfo CommitDistributedTransaction(DistributedTransactionScope scope)
        {
            ThrowIfDisposed();
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            var result = _txCoordinator.CommitScope(scope);
            RaiseAuditEvent(
                kind:          Audit.DistributedAuditEventKind.TransactionCommit,
                operation:     "CommitDistributedTransaction",
                shardIds:      scope.ShardIds,
                correlationId: scope.CorrelationId,
                message:       $"strategy={scope.Strategy}; flag={result?.Flag}",
                error:         result?.Ex);
            return result;
        }

        /// <summary>Rolls back an explicitly-opened distributed scope.</summary>
        public IErrorsInfo RollbackDistributedTransaction(DistributedTransactionScope scope)
        {
            ThrowIfDisposed();
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            var result = _txCoordinator.RollbackScope(scope);
            RaiseAuditEvent(
                kind:          Audit.DistributedAuditEventKind.TransactionRollback,
                operation:     "RollbackDistributedTransaction",
                shardIds:      scope.ShardIds,
                correlationId: scope.CorrelationId,
                message:       $"strategy={scope.Strategy}; flag={result?.Flag}",
                error:         result?.Ex);
            return result;
        }

        /// <summary>
        /// Runs a saga: executes forwards in order on their target
        /// shards; compensations replay on failure in reverse. Valid
        /// only on saga-strategy scopes.
        /// </summary>
        public IErrorsInfo RunSaga(
            DistributedTransactionScope  scope,
            IReadOnlyList<SagaStep>      steps)
        {
            ThrowIfDisposed();
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            return _txCoordinator.RunSaga(scope, steps);
        }

        // ── Helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the single shard backing an implicit
        /// <see cref="IDataSource.BeginTransaction"/>. Uses the
        /// entity hint from <see cref="PassedArgs.CurrentEntity"/>
        /// when provided and routable; falls back to the sole shard
        /// when the datasource owns exactly one. Throws otherwise.
        /// </summary>
        private string ResolveImplicitShardId(PassedArgs args)
        {
            var entityName = args?.CurrentEntity;
            if (!string.IsNullOrWhiteSpace(entityName))
            {
                var ctx      = DistributedExecutionContext.New(
                                   operationName: "BeginTransaction",
                                   entityName:    entityName,
                                   isWrite:       true);
                var decision = SnapshotRouter().RouteWrite(
                                   entityName:  entityName,
                                   record:      null,
                                   structure:   null,
                                   context:     ctx);

                if (decision.ShardIds.Count == 1) return decision.ShardIds[0];

                throw new InvalidOperationException(
                    $"Entity '{entityName}' resolves to {decision.ShardIds.Count} shards; " +
                    "implicit BeginTransaction only supports single-shard work. Use " +
                    "BeginDistributedTransaction for multi-shard transactions.");
            }

            var singleShard = _shards.Count == 1 ? _shards.Keys.First() : null;
            if (singleShard != null) return singleShard;

            throw new InvalidOperationException(
                "Distributed BeginTransaction requires a PassedArgs.CurrentEntity hint " +
                "when the datasource owns more than one shard. Supply the entity name " +
                "or use BeginDistributedTransaction for explicit multi-shard scopes.");
        }

        private static IErrorsInfo Ok(string message)
            => new ErrorsInfo { Flag = Errors.Ok, Message = message };

        private static IErrorsInfo Fail(string message, Exception error = null)
            => new ErrorsInfo { Flag = Errors.Failed, Message = message, Ex = error };
    }
}
