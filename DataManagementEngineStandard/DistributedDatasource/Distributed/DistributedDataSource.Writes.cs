using System;
using System.Text;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Distributed.Execution;
using TheTechIdea.Beep.Distributed.Routing;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// <see cref="DistributedDataSource"/> partial — Phase 07 write
    /// dispatch. Each <see cref="IDataSource"/> write method routes a
    /// <see cref="RoutingDecision"/> via the Phase 05 router, hands
    /// the decision to the <see cref="IDistributedWriteExecutor"/>,
    /// and converts the resulting <see cref="WriteOutcome"/> into an
    /// <see cref="IErrorsInfo"/>. Partial failures (quorum met but a
    /// replica diverged) surface via
    /// <see cref="DistributedDataSource.OnPartialReplicationFailure"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dispatch rules — strictly derived from
    /// <see cref="RoutingDecision"/>:
    /// </para>
    /// <list type="bullet">
    ///   <item>Single target shard, no scatter → single-shard write
    ///   (quorum implicit).</item>
    ///   <item>Replicated / broadcast fan-out → parallel fan-out with
    ///   quorum resolved from <see cref="RoutingDecision.WriteQuorum"/>
    ///   and per-call <see cref="DistributedWriteOptions"/>.</item>
    ///   <item>Scatter sharded write (key missing, multiple shards,
    ///   <see cref="RoutingDecision.IsScatter"/> == <c>true</c>)
    ///   → opt-in scatter delete path (requires
    ///   <see cref="DistributedWriteOptions.AllowScatterWrite"/>).</item>
    /// </list>
    /// <para>
    /// <c>ExecuteSql</c> requires an entity hint via
    /// <see cref="DistributedDataSourceOptions.ExecuteSqlEntityHintProvider"/>
    /// (future phase) or a caller-supplied
    /// <see cref="DistributedWriteOptions.EntityNameHint"/>. Until the
    /// Phase 08 SQL parser lands, <c>ExecuteSql</c> without a hint
    /// broadcasts across every live shard under
    /// <see cref="QuorumPolicy.All"/> — safe for idempotent DDL-ish
    /// "catch-all" statements and noisy for anything else. Phase 12
    /// replaces this with a dedicated DDL broadcast helper.
    /// </para>
    /// </remarks>
    public partial class DistributedDataSource
    {
        // ── InsertEntity ──────────────────────────────────────────────────

        /// <inheritdoc/>
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            ThrowIfDisposed();
            var ctx      = DistributedExecutionContext.New("InsertEntity", EntityName, isWrite: true);
            var decision = RouteForWrite(EntityName, InsertedData, ctx);
            return DispatchWrite(
                decision:       decision,
                ctx:            ctx,
                writeOperation: cluster => cluster.InsertEntity(EntityName, InsertedData),
                options:        null);
        }

        // ── UpdateEntity ──────────────────────────────────────────────────

        /// <inheritdoc/>
        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            ThrowIfDisposed();
            var ctx      = DistributedExecutionContext.New("UpdateEntity", EntityName, isWrite: true);
            var decision = RouteForWrite(EntityName, UploadDataRow, ctx);
            return DispatchWrite(
                decision:       decision,
                ctx:            ctx,
                writeOperation: cluster => cluster.UpdateEntity(EntityName, UploadDataRow),
                options:        null);
        }

        // ── UpdateEntities (bulk) ─────────────────────────────────────────

        /// <inheritdoc/>
        public IErrorsInfo UpdateEntities(
            string                        EntityName,
            object                        UploadData,
            IProgress<PassedArgs>         progress)
        {
            ThrowIfDisposed();

            // Bulk uploads typically don't carry a single partition key,
            // so we route by entity metadata: replicated entities fan
            // out, broadcast entities reach every shard, sharded bulk
            // requires scatter opt-in (or a caller-supplied decision).
            var ctx      = DistributedExecutionContext.New("UpdateEntities", EntityName, isWrite: true);
            var decision = RouteForWrite(EntityName, record: null, ctx: ctx);
            return DispatchWrite(
                decision:       decision,
                ctx:            ctx,
                writeOperation: cluster => cluster.UpdateEntities(EntityName, UploadData, progress),
                options:        null);
        }

        // ── DeleteEntity ──────────────────────────────────────────────────

        /// <inheritdoc/>
        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            ThrowIfDisposed();
            var ctx      = DistributedExecutionContext.New("DeleteEntity", EntityName, isWrite: true);
            var decision = RouteForWrite(EntityName, UploadDataRow, ctx);
            return DispatchWrite(
                decision:       decision,
                ctx:            ctx,
                writeOperation: cluster => cluster.DeleteEntity(EntityName, UploadDataRow),
                options:        null);
        }

        // ── ExecuteSql ────────────────────────────────────────────────────

        /// <inheritdoc/>
        public IErrorsInfo ExecuteSql(string sql)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL cannot be null or whitespace.", nameof(sql));

            // Phase 07 has no SQL parser yet (Phase 08), so every
            // ExecuteSql is treated as a broadcast under QuorumPolicy.All.
            // This is deliberately conservative: partial SQL application
            // across shards is almost always wrong.
            var ctx      = DistributedExecutionContext.New(
                               operationName: "ExecuteSql",
                               entityName:    null,
                               isWrite:       true);

            var decision = BuildBroadcastWriteDecision(ctx.OperationName);

            var outcome = _writeExecutor.ExecuteFanOut(
                decision:       decision,
                writeOperation: cluster => cluster.ExecuteSql(sql),
                ctx:            ctx,
                options:        null);

            return OutcomeToErrors(outcome);
        }

        // ── Dispatch helpers ──────────────────────────────────────────────

        /// <summary>
        /// Picks the correct executor entry-point
        /// (single-shard / fan-out / scatter) based on
        /// <paramref name="decision"/> and converts the resulting
        /// <see cref="WriteOutcome"/> into <see cref="IErrorsInfo"/>.
        /// </summary>
        private IErrorsInfo DispatchWrite(
            RoutingDecision                                       decision,
            DistributedExecutionContext                           ctx,
            Func<Proxy.IProxyCluster, IErrorsInfo>                writeOperation,
            DistributedWriteOptions                               options)
        {
            WriteOutcome outcome;

            if (IsSingleShard(decision))
            {
                outcome = _writeExecutor.ExecuteSingleShard(decision, writeOperation, ctx);
            }
            else if (decision.IsScatter && decision.Mode == DistributionMode.Sharded)
            {
                outcome = _writeExecutor.ExecuteScatter(
                    decision, writeOperation, ctx, options ?? DistributedWriteOptions.Default);
            }
            else
            {
                outcome = _writeExecutor.ExecuteFanOut(decision, writeOperation, ctx, options);
            }

            return OutcomeToErrors(outcome);
        }

        /// <summary>
        /// Routes a write using the Phase 05 router; emits
        /// <see cref="OnShardSelected"/> for every targeted shard.
        /// </summary>
        private RoutingDecision RouteForWrite(
            string                      entityName,
            object                      record,
            DistributedExecutionContext ctx)
        {
            EnsureAccess(entityName, Security.DistributedAccessKind.Write, ResolvePrincipal(ctx));
            var router   = SnapshotRouter();
            var decision = router.RouteWrite(entityName, record, structure: null, context: ctx);
            EmitDecisionSelected(decision, ctx);
            return decision;
        }

        /// <summary>
        /// Builds a synthetic broadcast write decision for
        /// <see cref="ExecuteSql"/> (no entity hint available in
        /// Phase 07). Targets every live shard and runs under
        /// <see cref="QuorumPolicy.All"/>.
        /// </summary>
        private RoutingDecision BuildBroadcastWriteDecision(string operation)
        {
            var live = SnapshotLiveShardIds();
            if (live.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Distributed {operation} has no live shards to dispatch to.");
            }

            return new RoutingDecision(
                entityName:        "(ad-hoc)",
                mode:              DistributionMode.Broadcast,
                matchKind:         Placement.PlacementMatchKind.Broadcast,
                shardIds:          live,
                isWrite:           true,
                isScatter:         false,
                isFanOut:          live.Count > 1,
                writeQuorum:       0,
                replicationFactor: 1,
                keyValues:         null,
                hookOverridden:    false,
                source:            null);
        }

        /// <summary>
        /// Translates a <see cref="WriteOutcome"/> into the
        /// <see cref="IErrorsInfo"/> return shape expected by
        /// <see cref="IDataSource"/>. Raises
        /// <see cref="OnPartialReplicationFailure"/> when the quorum
        /// was satisfied but at least one replica diverged.
        /// </summary>
        private IErrorsInfo OutcomeToErrors(WriteOutcome outcome)
        {
            if (outcome == null)
            {
                return new ErrorsInfo
                {
                    Flag    = Errors.Failed,
                    Message = "Write executor returned no outcome.",
                };
            }

            if (outcome.QuorumSatisfied)
            {
                if (outcome.IsPartial)
                {
                    RaisePartialReplicationFailure(outcome);
                }
                return new ErrorsInfo
                {
                    Flag    = Errors.Ok,
                    Message = BuildOutcomeMessage(outcome),
                };
            }

            return new ErrorsInfo
            {
                Flag    = Errors.Failed,
                Message = BuildOutcomeMessage(outcome),
                Ex      = outcome.FirstError,
            };
        }

        private static string BuildOutcomeMessage(WriteOutcome outcome)
        {
            var sb = new StringBuilder();
            sb.Append(outcome.Operation);
            sb.Append(' ');
            sb.Append(outcome.EntityName);
            sb.Append(": ack=");
            sb.Append(outcome.SuccessCount);
            sb.Append('/');
            sb.Append(outcome.PerShard.Count);
            sb.Append(" (required=");
            sb.Append(outcome.RequiredAckCount);
            sb.Append(", policy=");
            sb.Append(outcome.QuorumPolicy);
            sb.Append(')');
            if (!outcome.QuorumSatisfied && outcome.FirstError != null)
            {
                sb.Append(" — firstError: ");
                sb.Append(outcome.FirstError.Message);
            }
            return sb.ToString();
        }
    }
}
