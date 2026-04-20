using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Distributed.Events;

namespace TheTechIdea.Beep.Distributed.Transactions
{
    /// <summary>
    /// <see cref="DistributedTransactionCoordinator"/> partial — two
    /// phase commit across the enlisted shards. The coordinator runs
    /// a prepare round (every shard must ack) followed by a commit
    /// round. If any prepare fails, an abort round rolls back every
    /// prepared shard. If every shard acked prepare but at least one
    /// commit fails, the scope is declared
    /// <see cref="DistributedTransactionStatus.InDoubt"/> and the
    /// configured handler is invoked.
    /// </summary>
    /// <remarks>
    /// V1 uses the existing <see cref="IDataSource"/> transaction
    /// triple — <c>BeginTransaction</c> is assumed to have been
    /// issued per shard by <see cref="BeginTwoPhaseCommit"/>;
    /// <c>Commit</c> stands in for the commit phase and
    /// <c>EndTransaction</c> stands in for rollback. Drivers that do
    /// not model an explicit prepare still benefit from the fan-out
    /// ordering (commit only after every shard's
    /// <c>BeginTransaction</c> succeeds), but fail-stop atomicity is
    /// best-effort without a durable prepare log (Phase 13).
    /// </remarks>
    public sealed partial class DistributedTransactionCoordinator
    {
        /// <summary>
        /// Opens a local transaction on every enlisted shard. Called
        /// by <c>DistributedDataSource.BeginDistributedTransaction</c>
        /// right after <see cref="Begin"/> when the strategy is
        /// <see cref="TransactionStrategy.TwoPhaseCommit"/>.
        /// </summary>
        internal IErrorsInfo BeginTwoPhaseCommit(
            DistributedTransactionScope scope,
            PassedArgs                  args)
        {
            if (!scope.TryTransition(
                    DistributedTransactionStatus.Active,
                    DistributedTransactionStatus.Preparing,
                    "2PC begin/prepare start"))
            {
                return Fail(
                    $"Scope {scope.CorrelationId} cannot enter prepare from status {scope.Status}");
            }

            var prepared = new List<string>(scope.ShardIds.Count);
            foreach (var shardId in scope.ShardIds)
            {
                _log.Append(TransactionLogEntry.Now(
                    correlationId: scope.CorrelationId,
                    kind:          TransactionLogKind.PrepareSent,
                    shardId:       shardId,
                    message:       "2PC BeginTransaction (prepare proxy)"));

                try
                {
                    var cluster = ResolveCluster(shardId);
                    var result  = cluster.BeginTransaction(args ?? new PassedArgs());

                    if (result != null && result.Flag == Errors.Ok)
                    {
                        prepared.Add(shardId);
                        _log.Append(TransactionLogEntry.Now(
                            correlationId: scope.CorrelationId,
                            kind:          TransactionLogKind.PrepareAck,
                            shardId:       shardId,
                            message:       "2PC prepare ack"));
                    }
                    else
                    {
                        _log.Append(TransactionLogEntry.Now(
                            correlationId: scope.CorrelationId,
                            kind:          TransactionLogKind.PrepareNack,
                            shardId:       shardId,
                            message:       result?.Message ?? "2PC prepare nack",
                            error:         result?.Ex));
                        AbortPreparedShards(scope, prepared, $"prepare nack on {shardId}", result?.Ex);
                        return FinalizeAsAborted(
                            scope,
                            $"2PC prepare failed on {shardId}",
                            result?.Ex);
                    }
                }
                catch (Exception ex)
                {
                    _log.Append(TransactionLogEntry.Now(
                        correlationId: scope.CorrelationId,
                        kind:          TransactionLogKind.PrepareNack,
                        shardId:       shardId,
                        message:       "2PC prepare threw",
                        error:         ex));
                    AbortPreparedShards(scope, prepared, $"prepare threw on {shardId}", ex);
                    return FinalizeAsAborted(scope, $"2PC prepare threw on {shardId}", ex);
                }
            }

            if (!scope.TryTransition(
                    DistributedTransactionStatus.Preparing,
                    DistributedTransactionStatus.Prepared,
                    "2PC prepare complete"))
            {
                // Scope was cancelled mid-prepare — safety net.
                AbortPreparedShards(scope, prepared, "scope cancelled mid-prepare", null);
                return FinalizeAsAborted(scope, "2PC scope cancelled during prepare");
            }

            return Ok($"2PC prepared {prepared.Count} shard(s)");
        }

        /// <summary>
        /// Runs the commit round across every enlisted shard. Only
        /// valid when <see cref="BeginTwoPhaseCommit"/> already moved
        /// the scope to <see cref="DistributedTransactionStatus.Prepared"/>.
        /// </summary>
        private IErrorsInfo CommitTwoPhaseCommit(DistributedTransactionScope scope)
        {
            if (!scope.TryTransition(
                    DistributedTransactionStatus.Prepared,
                    DistributedTransactionStatus.Committing,
                    "2PC commit start"))
            {
                if (scope.Status == DistributedTransactionStatus.Active)
                {
                    return Fail(
                        $"Scope {scope.CorrelationId} has not been prepared; call " +
                        "BeginDistributedTransaction before commit.");
                }
                return Fail(
                    $"Scope {scope.CorrelationId} cannot commit from status {scope.Status}");
            }

            _log.Append(TransactionLogEntry.Now(
                correlationId: scope.CorrelationId,
                kind:          TransactionLogKind.GlobalCommit,
                message:       "2PC global commit decision"));

            var committed   = new List<string>(scope.ShardIds.Count);
            var failed      = new List<string>();
            Exception firstErr = null;

            foreach (var shardId in scope.ShardIds)
            {
                try
                {
                    var cluster = ResolveCluster(shardId);
                    var result  = cluster.Commit(new PassedArgs());

                    if (result != null && result.Flag == Errors.Ok)
                    {
                        committed.Add(shardId);
                        _log.Append(TransactionLogEntry.Now(
                            correlationId: scope.CorrelationId,
                            kind:          TransactionLogKind.CommitAck,
                            shardId:       shardId,
                            message:       "2PC commit ack"));
                    }
                    else
                    {
                        failed.Add(shardId);
                        firstErr = firstErr ?? result?.Ex;
                        _log.Append(TransactionLogEntry.Now(
                            correlationId: scope.CorrelationId,
                            kind:          TransactionLogKind.CommitFailed,
                            shardId:       shardId,
                            message:       result?.Message ?? "2PC commit nack",
                            error:         result?.Ex));
                    }
                }
                catch (Exception ex)
                {
                    failed.Add(shardId);
                    firstErr = firstErr ?? ex;
                    _log.Append(TransactionLogEntry.Now(
                        correlationId: scope.CorrelationId,
                        kind:          TransactionLogKind.CommitFailed,
                        shardId:       shardId,
                        message:       "2PC commit threw",
                        error:         ex));
                }
            }

            if (failed.Count == 0)
            {
                return FinalizeAsCommitted(scope, $"2PC commit ({committed.Count} shards)");
            }

            // Mixed outcome after a successful prepare: classic
            // in-doubt state. We cannot safely reverse the committed
            // shards (durable on their end) and we cannot retroactively
            // commit the failed ones without external intervention.
            var inDoubt = new TransactionInDoubtEventArgs(
                correlationId:    scope.CorrelationId,
                committedShards:  committed,
                failedShards:     failed,
                firstCommitError: firstErr,
                timestampUtc:     DateTime.UtcNow);

            FinalizeAsInDoubt(scope, inDoubt);
            return Fail(
                $"2PC commit reached in-doubt state for {scope.CorrelationId}: " +
                $"committed={committed.Count}, failed={failed.Count}",
                firstErr);
        }

        /// <summary>
        /// Rolls back the prepared shards when the caller explicitly
        /// invokes rollback (rather than failing during prepare).
        /// </summary>
        private IErrorsInfo RollbackTwoPhaseCommit(DistributedTransactionScope scope)
        {
            if (!scope.TryTransition(
                    DistributedTransactionStatus.Prepared,
                    DistributedTransactionStatus.Aborting,
                    "2PC rollback start"))
            {
                if (scope.Status == DistributedTransactionStatus.Active)
                {
                    // Caller rolled back before BeginDistributedTransaction ran.
                    return FinalizeAsAborted(scope, "2PC rollback before prepare");
                }
                return Fail(
                    $"Scope {scope.CorrelationId} cannot rollback from status {scope.Status}");
            }

            _log.Append(TransactionLogEntry.Now(
                correlationId: scope.CorrelationId,
                kind:          TransactionLogKind.GlobalAbort,
                message:       "2PC global abort decision"));

            AbortPreparedShards(scope, scope.ShardIds, "caller-initiated rollback", null);
            return FinalizeAsAborted(scope, "2PC rollback complete");
        }

        /// <summary>
        /// Fan-outs <see cref="IDataSource.EndTransaction"/> across
        /// the given shard list, capturing per-shard log entries.
        /// Never throws — partial-abort failures are logged but the
        /// scope continues to finalise as aborted because leaving
        /// prepared state uncleared is worse than surfacing a
        /// cleanup error.
        /// </summary>
        private void AbortPreparedShards(
            DistributedTransactionScope scope,
            IEnumerable<string>         shardIds,
            string                      reason,
            Exception                   originalError)
        {
            if (shardIds == null) return;

            foreach (var shardId in shardIds)
            {
                try
                {
                    var cluster = ResolveCluster(shardId);
                    var result  = cluster.EndTransaction(new PassedArgs());

                    if (result != null && result.Flag == Errors.Ok)
                    {
                        _log.Append(TransactionLogEntry.Now(
                            correlationId: scope.CorrelationId,
                            kind:          TransactionLogKind.RollbackAck,
                            shardId:       shardId,
                            message:       $"2PC abort ack ({reason})"));
                    }
                    else
                    {
                        _log.Append(TransactionLogEntry.Now(
                            correlationId: scope.CorrelationId,
                            kind:          TransactionLogKind.RollbackFailed,
                            shardId:       shardId,
                            message:       result?.Message ?? $"2PC abort nack ({reason})",
                            error:         result?.Ex ?? originalError));
                    }
                }
                catch (Exception ex)
                {
                    _log.Append(TransactionLogEntry.Now(
                        correlationId: scope.CorrelationId,
                        kind:          TransactionLogKind.RollbackFailed,
                        shardId:       shardId,
                        message:       $"2PC abort threw ({reason})",
                        error:         ex));
                }
            }
        }
    }
}
