using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Proxy
{
    public partial class ProxyDataSource : IProxyDataSource
    {
        private readonly IDMEEditor _dmeEditor;

        // ── Policy (single source of truth — Phase 1) ─────────────────
        private ProxyPolicy _policy;

        // ── Routing state ─────────────────────────────────────────────
        private readonly ConcurrentDictionary<string, bool> _healthStatus   = new();
        private int     _currentIndex        = 0;
        private bool    _disposed            = false;
        private string  _currentDataSourceName;
        private readonly ConcurrentDictionary<string, int>  _dataSourceWeights = new();
        private System.Timers.Timer                _healthCheckTimer;
        private readonly ConcurrentDictionary<string, IDataSource> _activeConnections = new();
        private readonly List<string>                        _dataSourceNames;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<PooledConnection>> _connectionPools = new();
        // ── Circuit state (P1: swappable backend — see ICircuitStateStore) ──────
        private ICircuitStateStore _circuitStateStore;

        // ── Audit sink (P1-10) ────────────────────────────────────────
        private IProxyAuditSink _auditSink;
        private readonly ConcurrentDictionary<string, DateTime>       _circuitOpenTimes = new();
        private readonly object _balancingLock        = new object();
        private int             _currentBalancingIndex = -1;

        // ── Metrics ───────────────────────────────────────────────────
        private readonly ConcurrentDictionary<string, DataSourceMetrics> _metrics = new();

        // ── Role separation (GAP-004) ─────────────────────────────────
        private readonly ConcurrentDictionary<string, ProxyDataSourceRole> _roles = new();

        // ── Pool limits ───────────────────────────────────────────────
        private readonly int      MaxPoolSize        = 10;
        private readonly TimeSpan ConnectionTimeout  = TimeSpan.FromMinutes(5);

        // ── Backward-compat public knobs (kept in sync with _policy) ──
        public int MaxRetries                      { get; set; } = 3;
        public int RetryDelayMilliseconds          { get; set; } = 500;
        public int HealthCheckIntervalMilliseconds { get; set; } = 30_000;

        // ── Events ────────────────────────────────────────────────────
        public event EventHandler<PassedArgs>        PassEvent;
        public event EventHandler<FailoverEventArgs>  OnFailover;
        public event EventHandler<RecoveryEventArgs>  OnRecovery;

        // ── IDataSource properties ────────────────────────────────────
        public string           GuidID            { get; set; }
        public DataSourceType   DatasourceType    { get; set; }
        public DatasourceCategory Category        { get; set; }
        public IDataConnection  Dataconnection    { get; set; }
        public string           DatasourceName    { get; set; }
        public IErrorsInfo      ErrorObject       { get; set; }
        public string           Id                { get; set; }
        public IDMLogger        Logger            { get; set; }
        public List<string>     EntitiesNames     { get; set; } = new List<string>();
        public List<EntityStructure> Entities     { get; set; } = new List<EntityStructure>();
        public IDMEEditor       DMEEditor         { get { return _dmeEditor; } set { } }
        public ConnectionState  ConnectionStatus  { get; set; }
        public string           ColumnDelimiter   { get; set; }
        public string           ParameterDelimiter { get; set; }

        // ── Audit (P1-10) ─────────────────────────────────────────────
        public IProxyAuditSink AuditSink
        {
            get => _auditSink;
            set => _auditSink = value ?? NullProxyAuditSink.Instance;
        }

        // ─────────────────────────────────────────────────────────────
        //  Constructors
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a proxy with an explicit <see cref="ProxyPolicy"/> as the single source of truth.
        /// Pass a custom <see cref="ICircuitStateStore"/> to use a distributed circuit-state backend;
        /// omit to use the default in-process store.
        /// </summary>
        public ProxyDataSource(IDMEEditor dmeEditor, List<string> dataSourceNames, ProxyPolicy policy,
            ICircuitStateStore circuitStateStore = null,
            IProxyAuditSink auditSink = null)
        {
            _dmeEditor         = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _dataSourceNames   = dataSourceNames ?? throw new ArgumentNullException(nameof(dataSourceNames));
            _policy            = policy ?? ProxyPolicy.Default;
            _circuitStateStore = circuitStateStore ?? new InProcessCircuitStateStore();
            _auditSink         = auditSink ?? NullProxyAuditSink.Instance;

            InitializeFromPolicy();
        }

        /// <summary>
        /// Backward-compatible constructor: accepts individual override values and wraps them
        /// into a <see cref="ProxyPolicy"/> so that <em>_policy</em> remains the single source of truth.
        /// </summary>
        public ProxyDataSource(
            IDMEEditor dmeEditor,
            List<string> dataSourceNames,
            int? maxRetries      = null,
            int? retryDelay      = null,
            int? healthCheckInterval = null)
        {
            _dmeEditor       = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _dataSourceNames = dataSourceNames ?? throw new ArgumentNullException(nameof(dataSourceNames));

            // Build policy from overrides
            var resilience = new ProxyResilienceProfile
            {
                ProfileType           = ProxyResilienceProfileType.Custom,
                MaxRetries            = maxRetries      ?? ProxyResilienceProfile.Balanced.MaxRetries,
                RetryBaseDelayMs      = retryDelay      ?? ProxyResilienceProfile.Balanced.RetryBaseDelayMs,
                RetryMaxDelayMs       = ProxyResilienceProfile.Balanced.RetryMaxDelayMs,
                UseExponentialBackoff = ProxyResilienceProfile.Balanced.UseExponentialBackoff,
                UseJitter             = ProxyResilienceProfile.Balanced.UseJitter,
                FailureThreshold      = ProxyResilienceProfile.Balanced.FailureThreshold,
                CircuitResetTimeout   = ProxyResilienceProfile.Balanced.CircuitResetTimeout,
                ConsecutiveSuccessesToClose = ProxyResilienceProfile.Balanced.ConsecutiveSuccessesToClose
            };

            _policy = new ProxyPolicy
            {
                HealthCheckIntervalMs = healthCheckInterval ?? 30_000,
                Resilience            = resilience
            };

            _circuitStateStore = new InProcessCircuitStateStore();
            _auditSink         = NullProxyAuditSink.Instance;
            InitializeFromPolicy();
        }

        private void InitializeFromPolicy()
        {
            _currentIndex = 0;
            SetCurrentDataSource(_currentIndex);

            // Sync public knobs from policy
            MaxRetries                      = _policy.Resilience.MaxRetries;
            RetryDelayMilliseconds          = _policy.Resilience.RetryBaseDelayMs;
            HealthCheckIntervalMilliseconds = _policy.HealthCheckIntervalMs;

            foreach (var ds in _dataSourceNames)
            {
                _healthStatus[ds] = false;
                _circuitStateStore.Initialize(
                    ds,
                    _policy.Resilience.FailureThreshold,
                    _policy.Resilience.CircuitResetTimeout,
                    _policy.Resilience.ConsecutiveSuccessesToClose);
            }

            OnFailover += (_, args) =>
                _dmeEditor.AddLogMessage($"Failover: {args.FromDataSource} → {args.ToDataSource}. Reason: {args.Reason}");

            _healthCheckTimer = new System.Timers.Timer(_policy.HealthCheckIntervalMs);
            _healthCheckTimer.Elapsed += PerformHealthCheck;
            _healthCheckTimer.AutoReset = true;
            _healthCheckTimer.Start();

            // Initialise the per-proxy CacheScope (uses CacheManager, not a raw dict)
            if (_policy.Cache.Enabled)
                InitializeCacheProvider();
        }

        // ─────────────────────────────────────────────────────────────
        //  Internal ExecAsync helper (async-first hotpath)
        // ─────────────────────────────────────────────────────────────

        private async Task<TResult> ExecAsync<TResult>(
            string methodName,
            Func<IDataSource, Task<TResult>> operation,
            params object[] args)
        {
            return (await ExecuteReadWithPolicyAsync(methodName, operation).ConfigureAwait(false)).Result;
        }

        // ─────────────────────────────────────────────────────────────
        //  IDataSource — read operations  (use ExecuteReadWithPolicy)
        // ─────────────────────────────────────────────────────────────

        public IEnumerable<string> GetEntitesList()
        {
            var r = ExecuteReadWithPolicy("GetEntitesList", ds => ds.GetEntitesList(), result => result != null);
            return r.Success ? r.Result : throw new Exception("GetEntitesList failed after all retries.");
        }

        public IEnumerable<object> RunQuery(string qrystr)
        {
            var r = ExecuteReadWithPolicy("RunQuery", ds => ds.RunQuery(qrystr), result => result != null);
            return r.Success ? r.Result : null;
        }

        public bool CreateEntityAs(EntityStructure entity)
        {
            var r = ExecuteReadWithPolicy("CreateEntityAs", ds => ds.CreateEntityAs(entity));
            return r.Success && r.Result;
        }

        public Type GetEntityType(string EntityName)  => Current.GetEntityType(EntityName);
        public int  GetEntityIdx(string entityName)   => Current.GetEntityIdx(entityName);

        public bool CheckEntityExist(string EntityName)
        {
            var r = ExecuteReadWithPolicy("CheckEntityExist", ds => ds.CheckEntityExist(EntityName));
            return r.Success && r.Result;
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            var r = ExecuteReadWithPolicy("GetChildTablesList",
                ds => ds.GetChildTablesList(tablename, SchemaName, Filterparamters), result => result != null);
            return r.Success ? r.Result : new List<ChildRelation>();
        }

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            var r = ExecuteReadWithPolicy("GetEntityforeignkeys",
                ds => ds.GetEntityforeignkeys(entityname, SchemaName), result => result != null);
            return r.Success ? r.Result : new List<RelationShipKeys>();
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            var r = ExecuteReadWithPolicy("GetEntityStructure",
                ds => ds.GetEntityStructure(EntityName, refresh), result => result != null);
            return r.Result;
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            try   { return Current.GetEntityStructure(fnd, refresh); }
            catch { Failover(); return Current.GetEntityStructure(fnd, refresh); }
        }

        public IErrorsInfo RunScript(ETLScriptDet script)
        {
            var r = ExecuteReadWithPolicy("RunScript", ds => ds.RunScript(script),
                result => result?.Flag == Errors.Ok);
            return r.Success ? r.Result : _dmeEditor.ErrorObject;
        }

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            var r = ExecuteReadWithPolicy("GetCreateEntityScript",
                ds => ds.GetCreateEntityScript(entities), result => result != null);
            return r.Success ? r.Result : new List<ETLScriptDet>();
        }

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            var r = ExecuteReadWithPolicy("CreateEntities", ds => ds.CreateEntities(entities),
                result => result?.Flag == Errors.Ok);
            return r.Success ? r.Result : _dmeEditor.ErrorObject;
        }

        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var r = ExecuteReadWithPolicy("GetEntity", ds => ds.GetEntity(EntityName, filter));
            return r.Result;
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            var r = ExecuteReadWithPolicy("GetEntityPaged",
                ds => ds.GetEntity(EntityName, filter, pageNumber, pageSize));
            return r.Result;
        }

        public async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            var r = await ExecuteReadWithPolicyAsync("GetEntityAsync",
                ds => ds.GetEntityAsync(EntityName, Filter)).ConfigureAwait(false);
            return r.Result;
        }

        public async Task<double> GetScalarAsync(string query)
        {
            var r = await ExecuteReadWithPolicyAsync("GetScalarAsync",
                ds => ds.GetScalarAsync(query)).ConfigureAwait(false);
            return r.Result;
        }

        public double GetScalar(string query)
        {
            var r = ExecuteReadWithPolicy("GetScalar", ds => ds.GetScalar(query));
            return r.Result;
        }

        // ─────────────────────────────────────────────────────────────
        //  IDataSource — write operations  (use ExecuteWriteWithPolicy)
        //  Writes are NonIdempotent by default — executed exactly once.
        // ─────────────────────────────────────────────────────────────

        public IErrorsInfo ExecuteSql(string sql)
        {
            // SQL can have side effects; treat as non-idempotent
            var r = ExecuteWriteWithPolicy("ExecuteSql", ds => ds.ExecuteSql(sql),
                result => result?.Flag == Errors.Ok,
                ProxyOperationSafety.NonIdempotentWrite);
            return r.Success ? r.Result : new ErrorsInfo();
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            var r = ExecuteWriteWithPolicy("InsertEntity",
                ds => ds.InsertEntity(EntityName, InsertedData),
                result => result?.Flag == Errors.Ok,
                ProxyOperationSafety.NonIdempotentWrite);
            InvalidateCacheOnWrite(EntityName);
            return r.Success ? r.Result : _dmeEditor.ErrorObject;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            var r = ExecuteWriteWithPolicy("UpdateEntity",
                ds => ds.UpdateEntity(EntityName, UploadDataRow),
                result => result?.Flag == Errors.Ok,
                ProxyOperationSafety.NonIdempotentWrite);
            InvalidateCacheOnWrite(EntityName);
            return r.Success ? r.Result : _dmeEditor.ErrorObject;
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            var r = ExecuteWriteWithPolicy("UpdateEntities",
                ds => ds.UpdateEntities(EntityName, UploadData, progress),
                result => result?.Flag == Errors.Ok,
                ProxyOperationSafety.NonIdempotentWrite);
            InvalidateCacheOnWrite(EntityName);
            return r.Success ? r.Result : _dmeEditor.ErrorObject;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            var r = ExecuteWriteWithPolicy("DeleteEntity",
                ds => ds.DeleteEntity(EntityName, UploadDataRow),
                result => result?.Flag == Errors.Ok,
                ProxyOperationSafety.NonIdempotentWrite);
            InvalidateCacheOnWrite(EntityName);
            return r.Success ? r.Result : _dmeEditor.ErrorObject;
        }

        // ─────────────────────────────────────────────────────────────
        //  Connection management
        // ─────────────────────────────────────────────────────────────

        public ConnectionState Openconnection()
        {
            var r = ExecuteReadWithPolicy("Openconnection", ds => ds.Openconnection(),
                result => result == ConnectionState.Open);
            return r.Success ? ConnectionState.Open : ConnectionState.Broken;
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                if (Current != null)
                    return Current.Closeconnection();
            }
            catch (Exception ex)
            {
                _dmeEditor.AddLogMessage(
                    $"Failed to close connection for {Current?.DatasourceName}: {ex.Message}. Attempting failover.");
                Failover();
                return Current?.Closeconnection() ?? ConnectionState.Closed;
            }
            return ConnectionState.Closed;
        }

        // ─────────────────────────────────────────────────────────────
        //  Dispose
        // ─────────────────────────────────────────────────────────────

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                StopWatchdog();
                _dmeEditor?.AddLogMessage("Disposing ProxyDataSource...");
                foreach (var dsName in _dataSourceNames)
                {
                    var ds = _dmeEditor.GetDataSource(dsName);
                    if (ds == null) continue;
                    try { ds.Closeconnection(); ds.Dispose(); }
                    catch (Exception ex)
                    { _dmeEditor.AddLogMessage($"Error disposing '{dsName}': {ex.Message}"); }
                }
                _healthCheckTimer?.Stop();
                _healthCheckTimer?.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ProxyDataSource() => Dispose(false);
    }
}

  