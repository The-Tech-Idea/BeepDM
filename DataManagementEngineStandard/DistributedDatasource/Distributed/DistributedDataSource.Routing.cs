using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Distributed.Placement;
using TheTechIdea.Beep.Distributed.Plan;
using TheTechIdea.Beep.Distributed.Routing;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// <see cref="DistributedDataSource"/> partial — entity-name
    /// placement resolution and routing diagnostics. Wraps the Phase 03
    /// <see cref="EntityPlacementResolver"/> with safety checks
    /// (disposal, unmapped policy enforcement) and event emission
    /// (<see cref="DistributedDataSource.OnShardSelected"/>,
    /// <see cref="DistributedDataSource.OnPlacementViolation"/>).
    /// </summary>
    public partial class DistributedDataSource
    {
        /// <summary>
        /// Currently active placement resolver. Rebuilt automatically
        /// whenever the active plan changes via
        /// <see cref="ApplyDistributionPlan(DistributionPlan)"/>.
        /// </summary>
        public EntityPlacementResolver PlacementResolver
        {
            get
            {
                lock (_planSwapLock)
                {
                    return _resolver;
                }
            }
        }

        /// <summary>
        /// Currently active placement map. Exposed for diagnostics; do
        /// not mutate. Rebuilt with the resolver on plan changes.
        /// </summary>
        public EntityPlacementMap PlacementMap
        {
            get
            {
                lock (_planSwapLock)
                {
                    return _placementMap;
                }
            }
        }

        /// <summary>
        /// Currently active shard router (Phase 05). Combines the
        /// <see cref="PlacementResolver"/> with the entity's
        /// partition function to produce concrete
        /// <see cref="RoutingDecision"/>s. Rebuilt automatically
        /// whenever the active plan changes.
        /// </summary>
        public IShardRouter Router
        {
            get
            {
                lock (_planSwapLock)
                {
                    return _router;
                }
            }
        }

        /// <summary>
        /// Installs (or replaces) the routing hook used by
        /// <see cref="Router"/> and rebuilds the router so the new
        /// hook applies from the next call onward. Pass <c>null</c>
        /// to restore <see cref="NullShardRoutingHook.Instance"/>.
        /// </summary>
        /// <param name="hook">New hook; <c>null</c> resets to the default no-op.</param>
        /// <exception cref="ObjectDisposedException">Datasource has been disposed.</exception>
        public void SetRoutingHook(IShardRoutingHook hook)
        {
            ThrowIfDisposed();
            lock (_planSwapLock)
            {
                _routingHook = hook ?? NullShardRoutingHook.Instance;
                RebuildShardRouter();
            }
        }

        /// <summary>
        /// Resolves <paramref name="entityName"/> for a read operation.
        /// Equivalent to <c>ResolvePlacement(name, isWrite: false)</c>.
        /// </summary>
        public PlacementResolution ResolvePlacement(string entityName)
            => ResolvePlacement(entityName, isWrite: false, context: null);

        /// <summary>
        /// Resolves <paramref name="entityName"/> for a write operation.
        /// Equivalent to <c>ResolvePlacement(name, isWrite: true)</c>.
        /// </summary>
        public PlacementResolution ResolvePlacementForWrite(string entityName)
            => ResolvePlacement(entityName, isWrite: true, context: null);

        /// <summary>
        /// Resolves <paramref name="entityName"/> against the active
        /// plan and live shard map, raises
        /// <see cref="DistributedDataSource.OnShardSelected"/> for
        /// every targeted shard, and (when applicable) raises
        /// <see cref="DistributedDataSource.OnPlacementViolation"/>
        /// before throwing for unmapped + reject policies.
        /// </summary>
        /// <param name="entityName">Entity to resolve.</param>
        /// <param name="isWrite">Hint that the resolution is for a write; recorded for downstream phases.</param>
        /// <param name="context">Optional execution context; one is auto-created when omitted.</param>
        /// <exception cref="ObjectDisposedException">Datasource has been disposed.</exception>
        /// <exception cref="ArgumentException"><paramref name="entityName"/> is null/whitespace.</exception>
        /// <exception cref="InvalidOperationException">Placement is unmapped and policy is <see cref="UnmappedEntityPolicy.RejectUnmapped"/>, or fallback shard is unavailable.</exception>
        public PlacementResolution ResolvePlacement(
            string                       entityName,
            bool                         isWrite,
            DistributedExecutionContext  context)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));

            EntityPlacementResolver resolver;
            lock (_planSwapLock)
            {
                resolver = _resolver;
            }

            var ctx = context ?? DistributedExecutionContext.New(
                operationName: isWrite ? "ResolvePlacementForWrite" : "ResolvePlacement",
                entityName:    entityName,
                isWrite:       isWrite);

            var resolution = resolver.Resolve(entityName, isWrite);

            // Unmapped + Reject → raise violation, then throw.
            if (resolution.IsUnmapped)
            {
                RaisePlacementViolation(
                    entityName: entityName,
                    shardId:    "(unmapped)",
                    reason:     $"No placement matches entity '{entityName}' and UnmappedPolicy is RejectUnmapped.");
                throw new InvalidOperationException(
                    $"Entity '{entityName}' is not mapped to any shard and the unmapped policy rejects it.");
            }

            // Default-route fallback that resolved to an unavailable shard.
            if (resolution.MatchKind == PlacementMatchKind.DefaultRoute &&
                resolution.TargetShardIds.Count == 0)
            {
                RaisePlacementViolation(
                    entityName: entityName,
                    shardId:    resolver.DefaultShardId ?? "(none)",
                    reason:     $"DefaultShardId '{resolver.DefaultShardId}' is not present in the live shard catalog.");
                throw new InvalidOperationException(
                    $"DefaultShardId '{resolver.DefaultShardId}' is not registered in the catalog.");
            }

            // Placement matched but every target shard was filtered out — surface a violation.
            if (resolution.TargetShardIds.Count == 0)
            {
                RaisePlacementViolation(
                    entityName: entityName,
                    shardId:    "(none)",
                    reason:     $"Placement for '{entityName}' resolved to zero live shards (mode={resolution.Mode}).");
            }

            // Emit one OnShardSelected per targeted shard so downstream
            // observability tooling sees the full fan-out plan.
            EmitShardSelected(resolution, ctx);

            return resolution;
        }

        // ── Internal: build and rebuild the resolver ──────────────────────

        /// <summary>
        /// (Re)builds the <see cref="EntityPlacementMap"/> and
        /// <see cref="EntityPlacementResolver"/> from
        /// <paramref name="plan"/> using the current options. Caller
        /// MUST hold <see cref="_planSwapLock"/> when invoked from
        /// <see cref="ApplyDistributionPlan(DistributionPlan)"/>; the
        /// constructor calls it before any other thread can observe
        /// the instance.
        /// </summary>
        private void RebuildPlacementResolver(DistributionPlan plan)
        {
            var map = plan == null || plan.IsEmpty
                ? EntityPlacementMap.Empty
                : EntityPlacementMap.FromPlan(plan);

            _placementMap = map;
            _resolver = new EntityPlacementResolver(
                map,
                liveShardSupplier: SnapshotLiveShardIds,
                unmappedPolicy:    _options.UnmappedPolicy,
                defaultShardId:    _options.DefaultShardIdForUnmapped);

            // Phase 08 — rebuild the broadcast-join rewriter alongside
            // the resolver so its placement view always matches the
            // live plan.
            _broadcastJoinRewriter = new Query.BroadcastJoinRewriter(_resolver);

            RebuildShardRouter();
        }

        /// <summary>
        /// Rebuilds the Phase 05 <see cref="IShardRouter"/> from the
        /// current resolver, hook, and options. Caller MUST hold
        /// <see cref="_planSwapLock"/>.
        /// </summary>
        private void RebuildShardRouter()
        {
            _router = new ShardRouter(
                resolver:          _resolver,
                hook:              _routingHook,
                allowScatterWrite: _options.AllowScatterWrite);
        }

        /// <summary>
        /// Returns the current shard-id snapshot used by the resolver
        /// to expand Broadcast placements and the broadcast-unmapped
        /// fallback. Defensive copy to prevent the resolver from
        /// accidentally observing later mutations.
        /// </summary>
        private IReadOnlyList<string> SnapshotLiveShardIds()
            => _shards.Keys
                      .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                      .ToList();

        private void EmitShardSelected(
            PlacementResolution         resolution,
            DistributedExecutionContext ctx)
        {
            if (resolution == null || resolution.TargetShardIds.Count == 0) return;

            var operation = ctx?.OperationName ?? string.Empty;
            var reason    = $"matchKind={resolution.MatchKind}; mode={resolution.Mode}; corr={ctx?.CorrelationId}";

            foreach (var shardId in resolution.TargetShardIds)
            {
                RaiseShardSelected(
                    entityName:   resolution.EntityName,
                    shardId:      shardId,
                    operation:    operation,
                    partitionKey: null,
                    reason:       reason);
            }
        }

        // ── Phase 05: Routing convenience surface ─────────────────────────

        /// <summary>
        /// Routes a read using a list of <see cref="AppFilter"/>s
        /// (the standard Beep query-filter shape) and emits one
        /// <see cref="OnShardSelected"/> per chosen shard.
        /// </summary>
        public RoutingDecision RouteRead(
            string                       entityName,
            List<AppFilter>              filters,
            EntityStructure              structure = null,
            DistributedExecutionContext  context   = null)
        {
            ThrowIfDisposed();

            var ctx     = context ?? DistributedExecutionContext.New("RouteRead", entityName, isWrite: false);
            var router  = SnapshotRouter();
            var decision = router.RouteRead(entityName, filters, structure, ctx);
            EmitDecisionSelected(decision, ctx);
            return decision;
        }

        /// <summary>
        /// Routes a read using positional primary-key values (the
        /// shape expected by <c>GetEntity(string, object[])</c>).
        /// </summary>
        public RoutingDecision RouteRead(
            string                       entityName,
            object[]                     positionalKeys,
            EntityStructure              structure,
            DistributedExecutionContext  context = null)
        {
            ThrowIfDisposed();

            var ctx      = context ?? DistributedExecutionContext.New("RouteRead", entityName, isWrite: false);
            var router   = SnapshotRouter();
            var decision = router.RouteRead(entityName, positionalKeys, structure, ctx);
            EmitDecisionSelected(decision, ctx);
            return decision;
        }

        /// <summary>
        /// Routes a write using an entity instance (POCO,
        /// <see cref="IDictionary{TKey,TValue}"/>, or anonymous
        /// object) and emits one <see cref="OnShardSelected"/> per
        /// targeted shard.
        /// </summary>
        public RoutingDecision RouteWrite(
            string                       entityName,
            object                       record,
            EntityStructure              structure = null,
            DistributedExecutionContext  context   = null)
        {
            ThrowIfDisposed();

            var ctx      = context ?? DistributedExecutionContext.New("RouteWrite", entityName, isWrite: true);
            var router   = SnapshotRouter();
            var decision = router.RouteWrite(entityName, record, structure, ctx);
            EmitDecisionSelected(decision, ctx);
            return decision;
        }

        /// <summary>
        /// Low-level catch-all that takes a fully-prepared key map.
        /// </summary>
        public RoutingDecision Route(
            string                              entityName,
            IReadOnlyDictionary<string, object> keyValues,
            bool                                isWrite,
            DistributedExecutionContext         context = null)
        {
            ThrowIfDisposed();

            var ctx      = context ?? DistributedExecutionContext.New(
                                          isWrite ? "RouteWrite" : "RouteRead",
                                          entityName,
                                          isWrite);
            var router   = SnapshotRouter();
            var decision = router.Route(entityName, keyValues, isWrite, ctx);
            EmitDecisionSelected(decision, ctx);
            return decision;
        }

        private IShardRouter SnapshotRouter()
        {
            lock (_planSwapLock)
            {
                return _router;
            }
        }

        private void EmitDecisionSelected(
            RoutingDecision             decision,
            DistributedExecutionContext ctx)
        {
            if (decision == null || decision.ShardIds.Count == 0) return;

            var operation    = ctx?.OperationName ?? string.Empty;
            var partitionKey = decision.KeyValues.Count == 0
                ? null
                : string.Join("|", decision.KeyValues.Select(kv => $"{kv.Key}={kv.Value}"));
            var reason       = $"matchKind={decision.MatchKind}; mode={decision.Mode}" +
                               $"; scatter={decision.IsScatter}; fanOut={decision.IsFanOut}" +
                               $"; hook={decision.HookOverridden}; corr={ctx?.CorrelationId}";

            foreach (var shardId in decision.ShardIds)
            {
                RaiseShardSelected(
                    entityName:   decision.EntityName,
                    shardId:      shardId,
                    operation:    operation,
                    partitionKey: partitionKey,
                    reason:       reason);
            }

            // Phase 13: audit the full placement decision once per
            // dispatch (distinct from per-shard RaiseShardSelected so
            // downstream tooling can correlate on CorrelationId).
            RaiseAuditEvent(
                kind:          Audit.DistributedAuditEventKind.PlacementDecided,
                operation:     operation,
                entityName:    decision.EntityName,
                mode:          decision.Mode.ToString(),
                shardIds:      decision.ShardIds,
                partitionKey:  partitionKey,
                principal:     ResolvePrincipal(ctx),
                correlationId: ctx?.CorrelationId,
                message:       reason);

            // Phase 13: a scatter read or a fan-out write is a
            // distinct operational concern — emit a second event so
            // dashboards can count them independently without having
            // to re-parse every PlacementDecided message.
            if (decision.IsScatter)
            {
                RaiseAuditEvent(
                    kind:          Audit.DistributedAuditEventKind.Scattered,
                    operation:     operation,
                    entityName:    decision.EntityName,
                    mode:          decision.Mode.ToString(),
                    shardIds:      decision.ShardIds,
                    partitionKey:  partitionKey,
                    principal:     ResolvePrincipal(ctx),
                    correlationId: ctx?.CorrelationId,
                    message:       reason);
            }
            else if (decision.IsFanOut)
            {
                RaiseAuditEvent(
                    kind:          Audit.DistributedAuditEventKind.FannedOut,
                    operation:     operation,
                    entityName:    decision.EntityName,
                    mode:          decision.Mode.ToString(),
                    shardIds:      decision.ShardIds,
                    partitionKey:  partitionKey,
                    principal:     ResolvePrincipal(ctx),
                    correlationId: ctx?.CorrelationId,
                    message:       reason);
            }
        }
    }
}
