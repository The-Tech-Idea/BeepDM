using System;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Distributed.Transactions
{
    /// <summary>
    /// <see cref="DistributedTransactionCoordinator"/> partial —
    /// single-shard fast path. When every enlisted entity maps to the
    /// same shard the coordinator hands off <c>BeginTransaction</c> /
    /// <c>Commit</c> / <c>EndTransaction</c> to that shard's cluster
    /// verbatim, so there is zero added round-trip versus calling
    /// the cluster directly.
    /// </summary>
    public sealed partial class DistributedTransactionCoordinator
    {
        /// <summary>
        /// Eagerly begins a transaction on the single enlisted shard.
        /// Called by <c>DistributedDataSource.BeginTransaction</c>
        /// right after <see cref="Begin"/> when the resolved strategy
        /// is <see cref="TransactionStrategy.SingleShardFastPath"/>.
        /// </summary>
        internal IErrorsInfo BeginSingleShard(
            DistributedTransactionScope scope,
            PassedArgs                  args)
        {
            var shardId = scope.ShardIds[0];
            try
            {
                var cluster = ResolveCluster(shardId);
                var result  = cluster.BeginTransaction(args ?? new PassedArgs());

                if (result != null && result.Flag != Errors.Ok)
                {
                    FinalizeAsAborted(scope, $"single-shard begin failed on {shardId}", result.Ex);
                    return result;
                }

                _log.Append(TransactionLogEntry.Now(
                    correlationId: scope.CorrelationId,
                    kind:          TransactionLogKind.Begin,
                    shardId:       shardId,
                    message:       "single-shard BeginTransaction ack"));
                return Ok($"Single-shard transaction opened on {shardId}");
            }
            catch (Exception ex)
            {
                FinalizeAsAborted(scope, $"single-shard begin threw on {shardId}", ex);
                return Fail($"Single-shard BeginTransaction threw on {shardId}", ex);
            }
        }

        /// <summary>
        /// Commits the single-shard scope by forwarding
        /// <see cref="IDataSource.Commit"/> to the target cluster.
        /// </summary>
        private IErrorsInfo CommitSingleShard(DistributedTransactionScope scope)
        {
            if (!scope.TryTransition(
                    DistributedTransactionStatus.Active,
                    DistributedTransactionStatus.Committing,
                    "single-shard commit start"))
            {
                return Fail(
                    $"Scope {scope.CorrelationId} cannot commit from status {scope.Status}");
            }

            var shardId = scope.ShardIds[0];
            try
            {
                var cluster = ResolveCluster(shardId);
                var result  = cluster.Commit(new PassedArgs());

                if (result != null && result.Flag == Errors.Ok)
                {
                    _log.Append(TransactionLogEntry.Now(
                        correlationId: scope.CorrelationId,
                        kind:          TransactionLogKind.CommitAck,
                        shardId:       shardId,
                        message:       "single-shard Commit ack"));
                    return FinalizeAsCommitted(scope, $"single-shard commit on {shardId}");
                }

                var err = result?.Ex;
                _log.Append(TransactionLogEntry.Now(
                    correlationId: scope.CorrelationId,
                    kind:          TransactionLogKind.CommitFailed,
                    shardId:       shardId,
                    message:       result?.Message ?? "single-shard Commit nack",
                    error:         err));
                return FinalizeAsAborted(scope, $"single-shard commit failed on {shardId}", err);
            }
            catch (Exception ex)
            {
                _log.Append(TransactionLogEntry.Now(
                    correlationId: scope.CorrelationId,
                    kind:          TransactionLogKind.CommitFailed,
                    shardId:       shardId,
                    message:       "single-shard Commit threw",
                    error:         ex));
                return FinalizeAsAborted(scope, $"single-shard commit threw on {shardId}", ex);
            }
        }

        /// <summary>
        /// Rolls back the single-shard scope by forwarding
        /// <see cref="IDataSource.EndTransaction"/> to the target
        /// cluster. <c>EndTransaction</c> is the Beep convention for
        /// discard/rollback — matches the Phase 09 doc.
        /// </summary>
        private IErrorsInfo RollbackSingleShard(DistributedTransactionScope scope)
        {
            if (!scope.TryTransition(
                    DistributedTransactionStatus.Active,
                    DistributedTransactionStatus.Aborting,
                    "single-shard rollback start"))
            {
                return Fail(
                    $"Scope {scope.CorrelationId} cannot rollback from status {scope.Status}");
            }

            var shardId = scope.ShardIds[0];
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
                        message:       "single-shard EndTransaction ack"));
                    return FinalizeAsAborted(scope, $"single-shard rollback on {shardId}");
                }

                var err = result?.Ex;
                _log.Append(TransactionLogEntry.Now(
                    correlationId: scope.CorrelationId,
                    kind:          TransactionLogKind.RollbackFailed,
                    shardId:       shardId,
                    message:       result?.Message ?? "single-shard EndTransaction nack",
                    error:         err));
                return FinalizeAsAborted(
                    scope,
                    $"single-shard rollback failed on {shardId}",
                    err);
            }
            catch (Exception ex)
            {
                _log.Append(TransactionLogEntry.Now(
                    correlationId: scope.CorrelationId,
                    kind:          TransactionLogKind.RollbackFailed,
                    shardId:       shardId,
                    message:       "single-shard EndTransaction threw",
                    error:         ex));
                return FinalizeAsAborted(scope, $"single-shard rollback threw on {shardId}", ex);
            }
        }
    }
}
