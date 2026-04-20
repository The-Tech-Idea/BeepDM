using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Distributed.Plan;
using TheTechIdea.Beep.Proxy;

namespace TheTechIdea.Beep.Distributed.Schema
{
    /// <summary>
    /// Default <see cref="IDistributedSchemaService"/> implementation.
    /// Composed of partial files for each operation
    /// (<c>Create</c>, <c>Alter</c>, <c>Drop</c>, <c>DriftDetection</c>)
    /// so every file owns one concern in the Phase 12 fan-out /
    /// drift pipeline.
    /// </summary>
    /// <remarks>
    /// The service is intentionally decoupled from
    /// <see cref="DistributedDataSource"/> via delegates so it can be
    /// unit-tested against fake shard maps. The Phase 12
    /// <c>DistributedDataSource.Schema.cs</c> partial constructs a
    /// default instance that forwards
    /// <see cref="IDataSource.CreateEntityAs"/> and friends into this
    /// surface.
    /// </remarks>
    public sealed partial class DistributedSchemaService : IDistributedSchemaService
    {
        private readonly Func<DistributionPlan>                _getCurrentPlan;
        private readonly Func<string, IProxyCluster>           _resolveShard;
        private readonly Action<string, string, string>        _raisePlacementViolation;
        private readonly Action<string>                        _raisePassEvent;
        private readonly IdentityColumnPolicy                  _identityPolicy;

        /// <summary>Initialises a new schema service with explicit delegates.</summary>
        /// <param name="getCurrentPlan">Returns the live <see cref="DistributionPlan"/>.</param>
        /// <param name="resolveShard">Returns the <see cref="IProxyCluster"/> for a shard id or <c>null</c> when missing.</param>
        /// <param name="raisePlacementViolation">Invoked as <c>(entityName, shardId, reason)</c> when placement validation fails.</param>
        /// <param name="raisePassEvent">Invoked with an audit-friendly string on non-terminal anomalies (e.g. identity warnings).</param>
        /// <param name="identityPolicy">Behaviour when a Sharded entity carries an identity column.</param>
        public DistributedSchemaService(
            Func<DistributionPlan>          getCurrentPlan,
            Func<string, IProxyCluster>     resolveShard,
            Action<string, string, string>  raisePlacementViolation,
            Action<string>                  raisePassEvent,
            IdentityColumnPolicy            identityPolicy = IdentityColumnPolicy.WarnOnly)
        {
            _getCurrentPlan           = getCurrentPlan           ?? throw new ArgumentNullException(nameof(getCurrentPlan));
            _resolveShard             = resolveShard             ?? throw new ArgumentNullException(nameof(resolveShard));
            _raisePlacementViolation  = raisePlacementViolation  ?? ((_, __, ___) => { });
            _raisePassEvent           = raisePassEvent           ?? (_ => { });
            _identityPolicy           = identityPolicy;
        }

        /// <summary>
        /// Identity-column policy enforced on create. Exposed so
        /// tests can inspect the active mode without reaching into
        /// private state.
        /// </summary>
        public IdentityColumnPolicy IdentityPolicy => _identityPolicy;

        // ── Shared helpers ────────────────────────────────────────────────

        /// <summary>Resolve the placement or raise a placement violation.</summary>
        private EntityPlacement ResolvePlacementOrThrow(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));

            var plan = _getCurrentPlan();
            if (plan == null || !plan.TryGetPlacement(entityName, out var placement))
            {
                _raisePlacementViolation(entityName, null,
                    $"No placement found for '{entityName}' in active plan.");
                throw new InvalidOperationException(
                    $"DistributedSchemaService: entity '{entityName}' has no placement in the active plan.");
            }
            return placement;
        }

        /// <summary>
        /// Returns the ordered list of shard ids targeted by a DDL
        /// operation for the supplied placement. For <c>Routed</c> the
        /// list has exactly one entry; for <c>Sharded</c> / <c>Replicated</c>
        /// / <c>Broadcast</c> every listed shard is targeted.
        /// </summary>
        private static IReadOnlyList<string> ComputeTargetShardIds(EntityPlacement placement)
        {
            if (placement == null) throw new ArgumentNullException(nameof(placement));
            return placement.ShardIds
                            .Where(id => !string.IsNullOrWhiteSpace(id))
                            .ToArray();
        }

        /// <summary>
        /// Runs <paramref name="perShardAction"/> sequentially against
        /// every shard in <paramref name="targetShardIds"/> and
        /// aggregates successes / failures into a
        /// <see cref="SchemaOperationOutcome"/>. Sequential execution
        /// keeps DDL logs deterministic and avoids overloading
        /// central-metadata systems that serialise DDL anyway.
        /// </summary>
        private async Task<SchemaOperationOutcome> RunDdlFanOutAsync(
            string                                        operation,
            string                                        entityName,
            IReadOnlyList<string>                         targetShardIds,
            Func<IProxyCluster, string, CancellationToken, Task> perShardAction,
            CancellationToken                             cancellationToken)
        {
            if (perShardAction == null) throw new ArgumentNullException(nameof(perShardAction));

            var succeeded = new List<string>(targetShardIds.Count);
            var errors    = new Dictionary<string, Exception>(
                                targetShardIds.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var shardId in targetShardIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cluster = _resolveShard(shardId);
                if (cluster == null)
                {
                    var missing = new InvalidOperationException(
                        $"Shard '{shardId}' is not registered in the distributed datasource.");
                    errors[shardId] = missing;
                    _raisePlacementViolation(entityName, shardId, missing.Message);
                    continue;
                }

                try
                {
                    await perShardAction(cluster, shardId, cancellationToken).ConfigureAwait(false);
                    succeeded.Add(shardId);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    errors[shardId] = ex;
                    _raisePassEvent(
                        $"DistributedSchemaService.{operation} shard '{shardId}' " +
                        $"on '{entityName}' failed: {ex.Message}");
                }
            }

            return new SchemaOperationOutcome(
                operation:         operation,
                entityName:        entityName,
                targetedShardIds:  targetShardIds,
                succeededShardIds: succeeded,
                errors:            errors);
        }
    }
}
