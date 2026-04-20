using System;
using TheTechIdea.Beep.Distributed.Events;
using TheTechIdea.Beep.Distributed.Execution;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// <see cref="DistributedDataSource"/> partial — events declared by
    /// <see cref="IDataSource"/> and <see cref="IDistributedDataSource"/>
    /// plus internal <c>Raise*</c> helpers used by sibling partials.
    /// </summary>
    /// <remarks>
    /// All <c>Raise*</c> helpers swallow handler exceptions: the
    /// distributed datasource sits on the read/write hot path and a
    /// faulty subscriber must never be able to break a query. Failures
    /// are surfaced through <see cref="IDataSource.PassEvent"/> with a
    /// well-known message so they remain observable.
    /// </remarks>
    public partial class DistributedDataSource
    {
        /// <inheritdoc/>
        public event EventHandler<PassedArgs> PassEvent;

        /// <inheritdoc/>
        public event EventHandler<ShardSelectedEventArgs> OnShardSelected;

        /// <inheritdoc/>
        public event EventHandler<PlacementViolationEventArgs> OnPlacementViolation;

        /// <inheritdoc/>
        public event EventHandler<ReshardEventArgs> OnReshardStarted;

        /// <inheritdoc/>
        public event EventHandler<ReshardEventArgs> OnReshardCompleted;

        /// <inheritdoc/>
        public event EventHandler<ReshardProgressEventArgs> OnReshardProgress;

        /// <inheritdoc/>
        public event EventHandler<PartialReplicationFailureEventArgs> OnPartialReplicationFailure;

        /// <inheritdoc/>
        public event EventHandler<TransactionInDoubtEventArgs> OnTransactionInDoubt;

        /// <inheritdoc/>
        public event EventHandler<ShardDownEventArgs> OnShardDown;

        /// <inheritdoc/>
        public event EventHandler<ShardRestoredEventArgs> OnShardRestored;

        /// <inheritdoc/>
        public event EventHandler<DegradedModeEventArgs> OnDegradedMode;

        /// <inheritdoc/>
        public event EventHandler<PartialBroadcastEventArgs> OnPartialBroadcast;

        // ── Raise helpers ─────────────────────────────────────────────────

        internal void RaiseShardSelected(
            string entityName,
            string shardId,
            string operation,
            object partitionKey = null,
            string reason       = null)
        {
            var handler = OnShardSelected;
            if (handler == null) return;
            try
            {
                handler(this, new ShardSelectedEventArgs(
                    entityName:   entityName,
                    shardId:      shardId,
                    operation:    operation ?? string.Empty,
                    partitionKey: partitionKey,
                    reason:       reason ?? string.Empty));
            }
            catch (Exception ex)
            {
                RaisePassEventSafe("OnShardSelected handler failed: " + ex.Message);
            }
        }

        internal void RaisePlacementViolation(
            string                       entityName,
            string                       shardId,
            string                       reason,
            PlacementViolationSeverity   severity = PlacementViolationSeverity.Error)
        {
            var handler = OnPlacementViolation;
            if (handler == null) return;
            try
            {
                handler(this, new PlacementViolationEventArgs(entityName, shardId, reason, severity));
            }
            catch (Exception ex)
            {
                RaisePassEventSafe("OnPlacementViolation handler failed: " + ex.Message);
            }
        }

        internal void RaiseReshardStarted(string reshardId, string reason, int? affectedEntities = null)
        {
            RaiseAuditEvent(
                kind:          Audit.DistributedAuditEventKind.ReshardStarted,
                operation:     "Reshard",
                correlationId: reshardId,
                message:       reason + (affectedEntities.HasValue ? $"; entities={affectedEntities.Value}" : string.Empty));

            var handler = OnReshardStarted;
            if (handler == null) return;
            try
            {
                handler(this, new ReshardEventArgs(reshardId, ReshardPhase.Started, reason, affectedEntities));
            }
            catch (Exception ex)
            {
                RaisePassEventSafe("OnReshardStarted handler failed: " + ex.Message);
            }
        }

        internal void RaiseReshardCompleted(
            string    reshardId,
            string    reason,
            int?      affectedEntities = null,
            Exception error            = null)
        {
            RaiseAuditEvent(
                kind:          Audit.DistributedAuditEventKind.ReshardCompleted,
                operation:     "Reshard",
                correlationId: reshardId,
                message:       reason + (affectedEntities.HasValue ? $"; entities={affectedEntities.Value}" : string.Empty),
                error:         error);

            var handler = OnReshardCompleted;
            if (handler == null) return;
            try
            {
                var phase = error == null ? ReshardPhase.Completed : ReshardPhase.Failed;
                handler(this, new ReshardEventArgs(reshardId, phase, reason, affectedEntities, error));
            }
            catch (Exception ex)
            {
                RaisePassEventSafe("OnReshardCompleted handler failed: " + ex.Message);
            }
        }

        internal void RaiseReshardProgress(ReshardProgressEventArgs args)
        {
            var handler = OnReshardProgress;
            if (handler == null || args == null) return;
            try
            {
                handler(this, args);
            }
            catch (Exception ex)
            {
                RaisePassEventSafe("OnReshardProgress handler failed: " + ex.Message);
            }
        }

        internal void RaisePartialReplicationFailure(WriteOutcome outcome)
        {
            var handler = OnPartialReplicationFailure;
            if (handler == null || outcome == null) return;
            try
            {
                handler(this, new PartialReplicationFailureEventArgs(outcome));
            }
            catch (Exception ex)
            {
                RaisePassEventSafe("OnPartialReplicationFailure handler failed: " + ex.Message);
            }
        }

        internal void RaiseTransactionInDoubt(TransactionInDoubtEventArgs args)
        {
            var handler = OnTransactionInDoubt;
            if (handler == null || args == null) return;
            try
            {
                handler(this, args);
            }
            catch (Exception ex)
            {
                RaisePassEventSafe("OnTransactionInDoubt handler failed: " + ex.Message);
            }
        }

        internal void RaiseShardDown(ShardDownEventArgs args)
        {
            var handler = OnShardDown;
            if (handler == null || args == null) return;
            try
            {
                handler(this, args);
            }
            catch (Exception ex)
            {
                RaisePassEventSafe("OnShardDown handler failed: " + ex.Message);
            }
        }

        internal void RaiseShardRestored(ShardRestoredEventArgs args)
        {
            var handler = OnShardRestored;
            if (handler == null || args == null) return;
            try
            {
                handler(this, args);
            }
            catch (Exception ex)
            {
                RaisePassEventSafe("OnShardRestored handler failed: " + ex.Message);
            }
        }

        internal void RaiseDegradedMode(DegradedModeEventArgs args)
        {
            var handler = OnDegradedMode;
            if (handler == null || args == null) return;
            try
            {
                handler(this, args);
            }
            catch (Exception ex)
            {
                RaisePassEventSafe("OnDegradedMode handler failed: " + ex.Message);
            }
        }

        internal void RaisePartialBroadcast(PartialBroadcastEventArgs args)
        {
            var handler = OnPartialBroadcast;
            if (handler == null || args == null) return;
            try
            {
                handler(this, args);
            }
            catch (Exception ex)
            {
                RaisePassEventSafe("OnPartialBroadcast handler failed: " + ex.Message);
            }
        }

        private void RaisePassEventSafe(string message)
        {
            try
            {
                PassEvent?.Invoke(this, new PassedArgs { Messege = message });
            }
            catch
            {
                // Final guard: never propagate from event raise on the hot path.
            }
        }
    }
}
