using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Distributed.Execution;
using TheTechIdea.Beep.Distributed.Placement;
using TheTechIdea.Beep.Distributed.Query;
using TheTechIdea.Beep.Distributed.Routing;
using TheTechIdea.Beep.Distributed.Transactions;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Proxy;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Distributed
{
    /// <summary>
    /// First-class <see cref="IDataSource"/> that distributes entities
    /// (tables) and rows across multiple physical datasources. Each
    /// shard is itself an HA pool implemented by an existing
    /// <see cref="IProxyCluster"/>, so per-shard failover, circuit
    /// breaking, and load balancing are reused from the
    /// <c>Proxy/</c> tier rather than reinvented.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This file holds the partial-class root: private fields,
    /// constructors, <see cref="IDataSource"/> identity properties, and
    /// the <see cref="IDistributedDataSource"/> topology accessors.
    /// Behaviour is split across sibling partials:
    /// </para>
    /// <list type="bullet">
    ///   <item><c>DistributedDataSource.Events.cs</c> — events declared by <see cref="IDistributedDataSource"/>.</item>
    ///   <item><c>DistributedDataSource.Lifecycle.cs</c> — Open / Close / Dispose orchestration.</item>
    ///   <item><c>DistributedDataSource.Plan.cs</c> — plan management and shard-catalog validation (Phase 02).</item>
    ///   <item><c>DistributedDataSource.IDataSource.cs</c> — <see cref="IDataSource"/> stubs that throw <see cref="NotImplementedException"/> in Phase 01 and are filled in by Phase 05+.</item>
    ///   <item><c>DistributedDataSource.Routing.cs</c> — added in Phase 03 / 05.</item>
    ///   <item><c>DistributedDataSource.Reads.cs</c> / <c>.Writes.cs</c> / <c>.Transactions.cs</c> — added in Phase 06 / 07 / 09.</item>
    /// </list>
    /// </remarks>
    public partial class DistributedDataSource : IDistributedDataSource
    {
        // ─────────────────────────────────────────────────────────────────────
        //  Core state
        // ─────────────────────────────────────────────────────────────────────

        private readonly IDMEEditor                          _dmeEditor;
        private readonly DistributedDataSourceOptions        _options;
        private readonly ConcurrentDictionary<string, IProxyCluster> _shards;
        private readonly object                              _planSwapLock = new object();

        private DistributionPlan          _plan;
        private EntityPlacementMap        _placementMap;
        private EntityPlacementResolver   _resolver;
        private IShardRouter              _router;
        private IShardRoutingHook         _routingHook;
        private IDistributedReadExecutor  _readExecutor;
        private IDistributedWriteExecutor _writeExecutor;
        private IResultMerger             _resultMerger;
        private IQueryAwareResultMerger   _queryMerger;
        private IQueryPlanner             _queryPlanner;
        private BroadcastJoinRewriter     _broadcastJoinRewriter;
        private IDistributedTransactionCoordinator _txCoordinator;
        private volatile bool             _disposed;

        // ─────────────────────────────────────────────────────────────────────
        //  IDataSource identity properties
        // ─────────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public string             GuidID            { get; set; } = Guid.NewGuid().ToString();
        /// <inheritdoc/>
        public string             DatasourceName    { get; set; } = "DistributedDataSource";
        /// <inheritdoc/>
        public DataSourceType     DatasourceType    { get; set; } = DataSourceType.Other;
        /// <inheritdoc/>
        public DatasourceCategory Category          { get; set; } = DatasourceCategory.RDBMS;
        /// <inheritdoc/>
        public IErrorsInfo        ErrorObject       { get; set; }
        /// <inheritdoc/>
        public string             Id                { get; set; }
        /// <inheritdoc/>
        public IDMLogger          Logger            { get; set; }
        /// <inheritdoc/>
        public ConnectionState    ConnectionStatus  { get; set; } = ConnectionState.Closed;
        /// <inheritdoc/>
        public IDataConnection    Dataconnection    { get; set; }
        /// <inheritdoc/>
        public string             ColumnDelimiter    { get; set; }
        /// <inheritdoc/>
        public string             ParameterDelimiter { get; set; }
        /// <inheritdoc/>
        public List<string>       EntitiesNames     { get; set; } = new List<string>();
        /// <inheritdoc/>
        public List<EntityStructure> Entities       { get; set; } = new List<EntityStructure>();
        /// <inheritdoc/>
        public IDMEEditor         DMEEditor         { get; set; }

        // ─────────────────────────────────────────────────────────────────────
        //  IDistributedDataSource topology accessors
        // ─────────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public DistributionPlan DistributionPlan
        {
            get
            {
                lock (_planSwapLock)
                {
                    return _plan;
                }
            }
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, IProxyCluster> Shards => _shards;

        // ─────────────────────────────────────────────────────────────────────
        //  Constructors
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new distributed datasource bound to the supplied
        /// shard map and (optional) plan / options.
        /// </summary>
        /// <param name="dmeEditor">Engine editor used for logging, ConfigEditor access, and error reporting. Required.</param>
        /// <param name="shards">Shard ID → <see cref="IProxyCluster"/> map. Must contain at least one entry; the dictionary is copied internally so later mutations do not affect routing.</param>
        /// <param name="plan">Active distribution plan. Defaults to <see cref="DistributionPlan.Empty"/>; use <see cref="Plan.DistributionPlanBuilder"/> or <see cref="Plan.DistributionPlanStore"/> to materialise non-empty plans.</param>
        /// <param name="options">Optional tuning knobs; defaults are used when omitted.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dmeEditor"/> or <paramref name="shards"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="shards"/> is empty.</exception>
        public DistributedDataSource(
            IDMEEditor                                  dmeEditor,
            IReadOnlyDictionary<string, IProxyCluster>  shards,
            DistributionPlan                            plan    = null,
            DistributedDataSourceOptions                options = null)
        {
            if (dmeEditor == null) throw new ArgumentNullException(nameof(dmeEditor));
            if (shards    == null) throw new ArgumentNullException(nameof(shards));
            if (shards.Count == 0) throw new ArgumentException("At least one shard is required.", nameof(shards));

            _dmeEditor = dmeEditor;
            DMEEditor  = dmeEditor;
            Logger     = dmeEditor.Logger;
            ErrorObject= dmeEditor.ErrorObject;

            _options      = options ?? new DistributedDataSourceOptions();
            _plan         = plan    ?? DistributionPlan.Empty;
            _routingHook  = NullShardRoutingHook.Instance;

            // Phase 08 — upgrade the default merger to the query-aware
            // merger; it implements IResultMerger via BasicResultMerger
            // so Phase 06 semantics for simple reads are preserved.
            var queryMerger = new QueryAwareResultMerger();
            _resultMerger   = queryMerger;
            _queryMerger    = queryMerger;
            _queryPlanner   = Query.QueryPlanner.Instance;

            _shards = new ConcurrentDictionary<string, IProxyCluster>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in shards)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                    throw new ArgumentException("Shard ID cannot be null or whitespace.", nameof(shards));
                if (kv.Value == null)
                    throw new ArgumentException($"Shard '{kv.Key}' has a null cluster.", nameof(shards));
                _shards[kv.Key] = kv.Value;
            }

            // Cross-check plan against shard map (richer validation lives in DistributedDataSource.Plan.cs).
            ValidatePlanAgainstShards(_plan);

            // Build the Phase 03 placement map + resolver from the active plan.
            RebuildPlacementResolver(_plan);

            // Build the Phase 06 read executor once the shard map is ready.
            var invoker = new ShardInvokerAdapter(this);
            _readExecutor = new DistributedReadExecutor(
                shards:  invoker,
                merger:  _resultMerger,
                options: _options);

            // Build the Phase 07 write executor over the same invoker so
            // shard resolution and event emission stay consistent with
            // the read path.
            _writeExecutor = new DistributedWriteExecutor(
                shards:  invoker,
                options: _options);

            // Phase 08 broadcast-join rewriter is created inside
            // RebuildPlacementResolver so it always tracks the active
            // resolver — no extra work needed here.

            // Phase 09 — distributed transaction coordinator. Uses
            // delegates so it always sees the live shard map and
            // raises OnTransactionInDoubt through the datasource's
            // event pipeline. Phase 13 promotes the log to a
            // durable FileTransactionLog when the operator
            // configures DurableTransactionLogDirectory.
            IDistributedTransactionLog txLog = null;
            if (!string.IsNullOrWhiteSpace(_options.DurableTransactionLogDirectory))
            {
                try
                {
                    txLog = new FileTransactionLog(_options.DurableTransactionLogDirectory);
                }
                catch
                {
                    // Fallback silently to in-memory when the durable
                    // log can't be materialised; operators see the
                    // failure via audit/log on the first Append call.
                    txLog = _options.EnableInMemoryTransactionLog
                                ? new InMemoryTransactionLog()
                                : null;
                }
            }
            else if (_options.EnableInMemoryTransactionLog)
            {
                txLog = new InMemoryTransactionLog();
            }

            _txCoordinator = new DistributedTransactionCoordinator(
                resolveShards:                () => _shards,
                raiseInDoubt:                 RaiseTransactionInDoubt,
                log:                          txLog,
                preferSagaOverTwoPhaseCommit: _options.PreferSagaOverTwoPhaseCommit);

            // Phase 10 — health monitor + distributed circuit breaker.
            // Construction only; Open/Close toggle the polling loop.
            BuildResilience();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Internal helpers (general-purpose)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Throws when this instance has already been disposed.</summary>
        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DistributedDataSource));
        }
    }
}
