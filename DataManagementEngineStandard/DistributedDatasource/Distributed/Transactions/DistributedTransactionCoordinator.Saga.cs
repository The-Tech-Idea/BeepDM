using System;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Distributed.Transactions
{
    /// <summary>
    /// <see cref="DistributedTransactionCoordinator"/> partial — saga
    /// runner. Executes the caller-supplied
    /// <see cref="SagaStep"/> list in order against their target
    /// shards; on any failure, runs the successful steps'
    /// compensations in reverse and finalises the scope as aborted.
    /// </summary>
    /// <remarks>
    /// V1 compensations run sequentially without retries. Phase 13
    /// introduces retry with exponential back-off and persists the
    /// step log so a crashed coordinator can resume compensations.
    /// </remarks>
    public sealed partial class DistributedTransactionCoordinator
    {
        /// <inheritdoc/>
        public IErrorsInfo RunSaga(
            DistributedTransactionScope  scope,
            IReadOnlyList<SagaStep>      steps)
        {
            EnsureScopeOpen(scope);

            if (scope.Strategy != TransactionStrategy.Saga)
            {
                return Fail(
                    $"Scope {scope.CorrelationId} strategy is {scope.Strategy}; " +
                    "RunSaga is only valid on saga scopes.");
            }
            if (steps == null || steps.Count == 0)
            {
                return FinalizeAsCommitted(scope, "saga-empty-steps");
            }

            if (!scope.TryTransition(
                    DistributedTransactionStatus.Active,
                    DistributedTransactionStatus.Committing,
                    "saga forward start"))
            {
                return Fail(
                    $"Scope {scope.CorrelationId} cannot run saga from status {scope.Status}");
            }

            var completed = new List<SagaStep>(steps.Count);

            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step == null)
                {
                    var err = Fail($"Saga step at index {i} is null.");
                    RollbackCompensations(scope, completed, $"null-step-at-{i}", null);
                    return FinalizeAsAborted(scope, $"saga null step @ {i}");
                }

                IErrorsInfo forwardResult = null;
                Exception   forwardEx     = null;
                try
                {
                    var cluster    = ResolveCluster(step.ShardId);
                    forwardResult  = step.Forward(cluster);
                }
                catch (Exception ex)
                {
                    forwardEx = ex;
                }

                if (forwardEx != null)
                {
                    _log.Append(TransactionLogEntry.Now(
                        correlationId: scope.CorrelationId,
                        kind:          TransactionLogKind.SagaForwardFailed,
                        shardId:       step.ShardId,
                        message:       $"saga step '{step.Name}' threw",
                        error:         forwardEx));
                    RollbackCompensations(scope, completed, $"forward-threw@{step.Name}", forwardEx);
                    return FinalizeAsAborted(
                        scope,
                        $"saga step '{step.Name}' threw",
                        forwardEx);
                }

                if (forwardResult == null || forwardResult.Flag != Errors.Ok)
                {
                    var reason = forwardResult?.Message ?? "saga step returned null / failed";
                    _log.Append(TransactionLogEntry.Now(
                        correlationId: scope.CorrelationId,
                        kind:          TransactionLogKind.SagaForwardFailed,
                        shardId:       step.ShardId,
                        message:       $"saga step '{step.Name}' failed: {reason}",
                        error:         forwardResult?.Ex));
                    RollbackCompensations(
                        scope,
                        completed,
                        $"forward-nack@{step.Name}",
                        forwardResult?.Ex);
                    return FinalizeAsAborted(
                        scope,
                        $"saga step '{step.Name}' failed: {reason}",
                        forwardResult?.Ex);
                }

                completed.Add(step);
                scope.RecordSagaForward(step.Name);
                _log.Append(TransactionLogEntry.Now(
                    correlationId: scope.CorrelationId,
                    kind:          TransactionLogKind.SagaForwardAck,
                    shardId:       step.ShardId,
                    message:       $"saga step '{step.Name}' ack"));
            }

            return FinalizeAsCommitted(scope, $"saga forward complete ({completed.Count} steps)");
        }

        /// <summary>
        /// Runs compensation delegates for the completed steps in
        /// reverse order. Never throws — compensation errors are
        /// logged so the scope continues to finalise.
        /// </summary>
        private void RollbackCompensations(
            DistributedTransactionScope scope,
            List<SagaStep>              completed,
            string                      reason,
            Exception                   originalError)
        {
            if (completed == null || completed.Count == 0) return;

            scope.ForceStatus(DistributedTransactionStatus.Compensating, $"saga compensation ({reason})");

            for (int i = completed.Count - 1; i >= 0; i--)
            {
                var step = completed[i];
                try
                {
                    var cluster = ResolveCluster(step.ShardId);
                    var result  = step.Compensation(cluster);

                    if (result != null && result.Flag == Errors.Ok)
                    {
                        _log.Append(TransactionLogEntry.Now(
                            correlationId: scope.CorrelationId,
                            kind:          TransactionLogKind.SagaCompensationAck,
                            shardId:       step.ShardId,
                            message:       $"saga compensation '{step.Name}' ack ({reason})"));
                    }
                    else
                    {
                        _log.Append(TransactionLogEntry.Now(
                            correlationId: scope.CorrelationId,
                            kind:          TransactionLogKind.SagaCompensationFailed,
                            shardId:       step.ShardId,
                            message:       result?.Message
                                              ?? $"saga compensation '{step.Name}' nack",
                            error:         result?.Ex ?? originalError));
                    }
                }
                catch (Exception ex)
                {
                    _log.Append(TransactionLogEntry.Now(
                        correlationId: scope.CorrelationId,
                        kind:          TransactionLogKind.SagaCompensationFailed,
                        shardId:       step.ShardId,
                        message:       $"saga compensation '{step.Name}' threw ({reason})",
                        error:         ex));
                }
            }
        }
    }
}
