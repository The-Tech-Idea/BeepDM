using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Distributed.Partitioning;
using TheTechIdea.Beep.Distributed.Placement;
using TheTechIdea.Beep.Distributed.Plan;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Distributed.Routing
{
    /// <summary>
    /// Phase 05 shard router. Combines the
    /// <see cref="EntityPlacementResolver"/> with the partition
    /// functions from
    /// <see cref="PartitionFunctionFactory"/> to turn an entity name
    /// + key values into a concrete <see cref="RoutingDecision"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lifecycle: a router is constructed once per
    /// <see cref="DistributionPlan"/> version and replaced atomically
    /// whenever the plan changes (Phase 11). The internal partition-
    /// function cache is keyed by <see cref="EntityPlacement"/>
    /// reference; a new plan produces new placement instances and
    /// the cache is naturally discarded.
    /// </para>
    /// <para>
    /// Liveness handling: hash partition functions are built from the
    /// placement's full shard list (so the consistent-hash ring stays
    /// stable across transient liveness churn). The router then
    /// intersects the function's output with the resolver's live-
    /// filtered shard list. If the intersection is empty,
    /// <see cref="ShardRoutingException"/> is raised with
    /// <c>Reason = "NoLiveShard"</c>.
    /// </para>
    /// <para>
    /// Implementation is split across partials:
    /// </para>
    /// <list type="bullet">
    ///   <item><c>ShardRouter.cs</c> — root: ctor, fields, route methods, hook plumbing.</item>
    ///   <item><c>ShardRouter.KeyExtraction.cs</c> — filter / positional / instance extractors.</item>
    /// </list>
    /// </remarks>
    public sealed partial class ShardRouter : IShardRouter
    {
        private readonly EntityPlacementResolver _resolver;
        private readonly IShardRoutingHook       _hook;
        private readonly bool                    _allowScatterWrite;

        private readonly ConcurrentDictionary<EntityPlacement, IPartitionFunction> _functionCache
            = new ConcurrentDictionary<EntityPlacement, IPartitionFunction>();

        /// <summary>Initialises a new router.</summary>
        /// <param name="resolver">Active placement resolver. Required.</param>
        /// <param name="hook">Optional override hook; defaults to <see cref="NullShardRoutingHook.Instance"/>.</param>
        /// <param name="allowScatterWrite">When <c>false</c> (default), writes against a sharded entity without a partition key throw <see cref="ShardRoutingException"/>; when <c>true</c>, the write is fanned out to every live shard.</param>
        public ShardRouter(
            EntityPlacementResolver resolver,
            IShardRoutingHook       hook              = null,
            bool                    allowScatterWrite = false)
        {
            _resolver          = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _hook              = hook     ?? NullShardRoutingHook.Instance;
            _allowScatterWrite = allowScatterWrite;
        }

        /// <inheritdoc/>
        public IShardRoutingHook Hook => _hook;

        // ── IShardRouter — the public API ─────────────────────────────────

        /// <inheritdoc/>
        public RoutingDecision RouteRead(
            string                       entityName,
            List<AppFilter>              filters,
            EntityStructure              structure = null,
            DistributedExecutionContext  context   = null)
        {
            ValidateEntityName(entityName);

            var placement = ResolveOrThrow(entityName, isWrite: false);
            var keyValues = TryExtractFromFilters(placement, filters, structure);
            return BuildAndOverride(entityName, placement, keyValues, isWrite: false, context);
        }

        /// <inheritdoc/>
        public RoutingDecision RouteRead(
            string                       entityName,
            object[]                     positionalKeys,
            EntityStructure              structure,
            DistributedExecutionContext  context = null)
        {
            ValidateEntityName(entityName);

            var placement = ResolveOrThrow(entityName, isWrite: false);
            var keyValues = TryExtractFromPositionalKeys(placement, positionalKeys, structure);
            return BuildAndOverride(entityName, placement, keyValues, isWrite: false, context);
        }

        /// <inheritdoc/>
        public RoutingDecision RouteWrite(
            string                       entityName,
            object                       record,
            EntityStructure              structure = null,
            DistributedExecutionContext  context   = null)
        {
            ValidateEntityName(entityName);

            var placement = ResolveOrThrow(entityName, isWrite: true);
            var keyValues = TryExtractFromEntityInstance(placement, record);
            return BuildAndOverride(entityName, placement, keyValues, isWrite: true, context);
        }

        /// <inheritdoc/>
        public RoutingDecision Route(
            string                              entityName,
            IReadOnlyDictionary<string, object> keyValues,
            bool                                isWrite,
            DistributedExecutionContext         context = null)
        {
            ValidateEntityName(entityName);

            var placement = ResolveOrThrow(entityName, isWrite);
            return BuildAndOverride(entityName, placement, keyValues, isWrite, context);
        }

        // ── Core decision-building pipeline ───────────────────────────────

        /// <summary>
        /// Resolves the placement for <paramref name="entityName"/>
        /// and rejects unmapped placements with a
        /// <see cref="ShardRoutingException"/>. Returns the resolution
        /// untouched otherwise.
        /// </summary>
        private PlacementResolution ResolveOrThrow(string entityName, bool isWrite)
        {
            var resolution = _resolver.Resolve(entityName, isWrite);

            if (resolution.IsUnmapped)
            {
                throw new ShardRoutingException(
                    entityName: entityName,
                    reason:     "Unmapped",
                    message:    $"Entity '{entityName}' is not mapped to any shard and the unmapped policy rejects routing.");
            }

            return resolution;
        }

        /// <summary>
        /// Builds the baseline decision from the resolution + key
        /// values, then runs the hook. Centralises the validation
        /// rules so every public route method behaves identically.
        /// </summary>
        private RoutingDecision BuildAndOverride(
            string                              entityName,
            PlacementResolution                 resolution,
            IReadOnlyDictionary<string, object> keyValues,
            bool                                isWrite,
            DistributedExecutionContext         context)
        {
            var baseline = BuildDecision(entityName, resolution, keyValues, isWrite);

            var hookCtx = new ShardRoutingHookContext(
                entityName: entityName,
                isWrite:    isWrite,
                keyValues:  keyValues,
                execution:  context);

            var overridden = _hook.OnRouteResolved(baseline, hookCtx) ?? baseline;
            if (overridden.ShardIds.Count == 0)
            {
                throw new ShardRoutingException(
                    entityName: entityName,
                    reason:     "HookOverrideEmpty",
                    message:    $"IShardRoutingHook '{_hook.GetType().FullName}' returned a decision with zero shards.");
            }

            // Phase 11: fan writes out to dual-write windows when one is active.
            return ApplyDualWriteFanOut(entityName, isWrite, overridden);
        }

        private RoutingDecision BuildDecision(
            string                              entityName,
            PlacementResolution                 resolution,
            IReadOnlyDictionary<string, object> keyValues,
            bool                                isWrite)
        {
            var liveTargets = resolution.TargetShardIds;

            switch (resolution.Mode)
            {
                case DistributionMode.Sharded:
                    return BuildShardedDecision(entityName, resolution, keyValues, isWrite, liveTargets);

                case DistributionMode.Routed:
                case DistributionMode.Replicated:
                case DistributionMode.Broadcast:
                    return BuildModeDecision(entityName, resolution, keyValues, isWrite, liveTargets);

                default:
                    throw new ShardRoutingException(
                        entityName: entityName,
                        reason:     "UnknownMode",
                        message:    $"Unsupported distribution mode '{resolution.Mode}' for entity '{entityName}'.");
            }
        }

        private RoutingDecision BuildShardedDecision(
            string                              entityName,
            PlacementResolution                 resolution,
            IReadOnlyDictionary<string, object> keyValues,
            bool                                isWrite,
            IReadOnlyList<string>               liveTargets)
        {
            var placement = resolution.Source
                ?? throw new ShardRoutingException(
                    entityName, "MissingPlacement",
                    $"Sharded resolution for '{entityName}' is missing its source placement.");

            // Determine which key column(s) the partition function consumes.
            var keyColumns = placement.PartitionFunction.KeyColumns;
            if (keyColumns == null || keyColumns.Count == 0)
            {
                throw new ShardRoutingException(
                    entityName: entityName,
                    reason:     "NoPartitionKeyColumns",
                    message:    $"Sharded entity '{entityName}' has no partition-key columns declared.");
            }

            // Detect "any key supplied at all?" — Sharded scatter triggers when no key column has a value.
            bool hasAnyKeyValue = keyValues != null
                && keyColumns.Any(c => keyValues.ContainsKey(c) && keyValues[c] != null);

            if (!hasAnyKeyValue)
            {
                if (isWrite && !_allowScatterWrite)
                {
                    throw new ShardRoutingException(
                        entityName: entityName,
                        reason:     "ScatterWriteRejected",
                        message:    $"Write to sharded entity '{entityName}' supplied no partition key " +
                                    $"(columns: {string.Join(",", keyColumns)}) and AllowScatterWrite is false.");
                }

                return new RoutingDecision(
                    entityName:        entityName,
                    mode:              resolution.Mode,
                    matchKind:         resolution.MatchKind,
                    shardIds:          liveTargets,
                    isWrite:           isWrite,
                    isScatter:         true,
                    isFanOut:          isWrite && _allowScatterWrite,
                    writeQuorum:       resolution.WriteQuorum,
                    replicationFactor: resolution.ReplicationFactor,
                    keyValues:         keyValues,
                    hookOverridden:    false,
                    source:            resolution);
            }

            // Multi-value (IN-list) on a single key column → union shards.
            var function = GetOrCreatePartitionFunction(placement);
            var multi    = TryGetMultiValues(keyColumns, keyValues);
            var liveSet  = new HashSet<string>(liveTargets, StringComparer.OrdinalIgnoreCase);
            var picked   = new List<string>();
            var seen     = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (multi != null)
            {
                foreach (var value in multi)
                {
                    var single = BuildSingleValueDictionary(keyColumns[0], value, keyValues);
                    AddPickedShards(entityName, function, single, liveSet, picked, seen);
                }
            }
            else
            {
                AddPickedShards(entityName, function, keyValues, liveSet, picked, seen);
            }

            if (picked.Count == 0)
            {
                throw new ShardRoutingException(
                    entityName: entityName,
                    reason:     "NoLiveShard",
                    message:    $"Sharded routing for '{entityName}' produced no live shard. " +
                                "All target shards are offline or filtered out.");
            }

            return new RoutingDecision(
                entityName:        entityName,
                mode:              resolution.Mode,
                matchKind:         resolution.MatchKind,
                shardIds:          picked,
                isWrite:           isWrite,
                isScatter:         false,
                isFanOut:          picked.Count > 1,
                writeQuorum:       resolution.WriteQuorum,
                replicationFactor: resolution.ReplicationFactor,
                keyValues:         keyValues,
                hookOverridden:    false,
                source:            resolution);
        }

        private static RoutingDecision BuildModeDecision(
            string                              entityName,
            PlacementResolution                 resolution,
            IReadOnlyDictionary<string, object> keyValues,
            bool                                isWrite,
            IReadOnlyList<string>               liveTargets)
        {
            if (liveTargets.Count == 0)
            {
                throw new ShardRoutingException(
                    entityName: entityName,
                    reason:     "NoLiveShard",
                    message:    $"Resolution for '{entityName}' (mode={resolution.Mode}) produced no live shard.");
            }

            // Fan-out semantics:
            //   Routed     → single shard, never fan-out.
            //   Replicated → write fans out to quorum; read picks one shard (executor's choice).
            //   Broadcast  → write fans out to every live shard; read picks one.
            bool isFanOut = isWrite && (resolution.Mode == DistributionMode.Replicated
                                     || resolution.Mode == DistributionMode.Broadcast);

            return new RoutingDecision(
                entityName:        entityName,
                mode:              resolution.Mode,
                matchKind:         resolution.MatchKind,
                shardIds:          liveTargets,
                isWrite:           isWrite,
                isScatter:         false,
                isFanOut:          isFanOut,
                writeQuorum:       resolution.WriteQuorum,
                replicationFactor: resolution.ReplicationFactor,
                keyValues:         keyValues,
                hookOverridden:    false,
                source:            resolution);
        }

        // ── Helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the partition function for <paramref name="placement"/>,
        /// constructing it lazily on first use. The cache is keyed by
        /// <see cref="EntityPlacement"/> reference; replacing the
        /// active plan produces new placements and a clean cache.
        /// </summary>
        private IPartitionFunction GetOrCreatePartitionFunction(EntityPlacement placement)
        {
            return _functionCache.GetOrAdd(placement, p =>
                PartitionFunctionFactory.Create(p.PartitionFunction, p.ShardIds));
        }

        /// <summary>
        /// Looks for an <c>IN</c>-shaped value list on the primary key
        /// column. Returns <c>null</c> when only a scalar value is
        /// present (the common case).
        /// </summary>
        private static IEnumerable<object> TryGetMultiValues(
            IReadOnlyList<string>               keyColumns,
            IReadOnlyDictionary<string, object> keyValues)
        {
            if (keyValues == null || keyValues.Count == 0) return null;
            if (!keyValues.TryGetValue(keyColumns[0], out var value) || value == null) return null;

            // Surface enumerable shapes (List<>, object[], etc.) but never raw strings
            // (a string is enumerable but is conceptually a single key value).
            if (value is string) return null;
            if (value is System.Collections.IEnumerable enumerable)
            {
                var list = new List<object>();
                foreach (var item in enumerable)
                    if (item != null) list.Add(item);
                return list.Count > 1 ? list : null;
            }
            return null;
        }

        private static IReadOnlyDictionary<string, object> BuildSingleValueDictionary(
            string                              keyColumn,
            object                              value,
            IReadOnlyDictionary<string, object> baseValues)
        {
            // Inherit additional key columns (composite functions) verbatim so the
            // partition function still sees every column it expects.
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (baseValues != null)
            {
                foreach (var kv in baseValues)
                    dict[kv.Key] = kv.Value;
            }
            dict[keyColumn] = value;
            return dict;
        }

        private static void AddPickedShards(
            string                              entityName,
            IPartitionFunction                  function,
            IReadOnlyDictionary<string, object> keyValues,
            HashSet<string>                     liveSet,
            List<string>                        picked,
            HashSet<string>                     seen)
        {
            var input  = new PartitionInput(entityName, keyValues);
            var output = function.Resolve(input) ?? Array.Empty<string>();

            for (int i = 0; i < output.Count; i++)
            {
                var shardId = output[i];
                if (string.IsNullOrEmpty(shardId)) continue;
                if (!liveSet.Contains(shardId))   continue;     // stale ring entry — drop silently
                if (!seen.Add(shardId))           continue;
                picked.Add(shardId);
            }
        }

        private static void ValidateEntityName(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be null or whitespace.", nameof(entityName));
        }
    }
}
