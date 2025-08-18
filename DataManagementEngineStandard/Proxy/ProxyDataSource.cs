using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Proxy
{
    public class ProxyDataSource : IDataSource,IDisposable
    {
        private readonly IDMEEditor _dmeEditor;


        #region Properties for Proxy
        private readonly Dictionary<string, bool> _healthStatus = new();
        private int _currentIndex = 0;
        private bool _disposed = false;
        private string _currentDataSourceName;
        private readonly Dictionary<string, int> _dataSourceWeights = new Dictionary<string, int>();
        private readonly System.Timers.Timer _healthCheckTimer;
        public event EventHandler<PassedArgs> PassEvent;
        private readonly Dictionary<string, int> _failureCounts = new();
        private readonly int FailureThreshold = 5;
        // Add these fields to the class
        private readonly ConcurrentDictionary<string, DataSourceMetrics> _metrics =
            new ConcurrentDictionary<string, DataSourceMetrics>();
        private readonly ConcurrentDictionary<string, CacheEntry> _entityCache =
            new ConcurrentDictionary<string, CacheEntry>();


        public event EventHandler<FailoverEventArgs> OnFailover;
     
        private readonly ConcurrentDictionary<string, IDataSource> _activeConnections = new();

        private readonly List<string> _dataSourceNames;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<PooledConnection>> _connectionPools =
  new ConcurrentDictionary<string, ConcurrentQueue<PooledConnection>>();
        public int MaxRetries { get; set; } = 3; // Default: 3 retries
        public int RetryDelayMilliseconds { get; set; } = 1000; // Default: 1 second delay
        public int HealthCheckIntervalMilliseconds { get; set; } = 30000; // Default: 30 seconds
    //    private readonly ConcurrentDictionary<string, DateTime> _circuitOpenTimes =
    //new ConcurrentDictionary<string, DateTime>();
    //    private readonly TimeSpan _circuitResetTimeout = TimeSpan.FromMinutes(5);
       
        private readonly TimeSpan _defaultCacheExpiration = TimeSpan.FromMinutes(5);

        // Replace the simple dictionary tracking with dedicated circuit breakers
        private readonly ConcurrentDictionary<string, CircuitBreaker> _circuitBreakers =
            new ConcurrentDictionary<string, CircuitBreaker>();
        private readonly object _balancingLock = new object();
        private int _currentBalancingIndex = -1;
        private readonly ProxyDataSourceOptions _options;
        private readonly ConcurrentDictionary<string, DateTime> _circuitOpenTimes =
    new ConcurrentDictionary<string, DateTime>();
        private readonly int MaxPoolSize = 10;
        private readonly TimeSpan ConnectionTimeout = TimeSpan.FromMinutes(5);

        #endregion Properties for Proxy

        #region IDataSource Properties
        public string GuidID { get; set; }
        public DataSourceType DatasourceType { get; set; }
        public DatasourceCategory Category { get; set; }
        public IDataConnection Dataconnection { get; set; }
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public string Id { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new List<string>();
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();
        public IDMEEditor DMEEditor { get { return _dmeEditor; } set { } }
        public ConnectionState ConnectionStatus { get; set; }

        public string ColumnDelimiter { get; set; }
        public string ParameterDelimiter { get; set; }
        #endregion IDataSource Properties


        public ProxyDataSource(IDMEEditor dmeEditor, List<string> dataSourceNames, int? maxRetries = null, int? retryDelay = null, int? healthCheckInterval = null)
        {
            _options =  new ProxyDataSourceOptions();
            _dmeEditor = dmeEditor;
            _dataSourceNames = dataSourceNames;
            _currentIndex = 0; // Start with the first data source
            SetCurrentDataSource(_currentIndex); // Initialize metadata
                                                 // Initialize health status and circuit breakers
            foreach (var ds in dataSourceNames)
            {
                _healthStatus[ds] = false;
                _circuitBreakers[ds] = new CircuitBreaker(
                    _options.FailureThreshold,
                    _options.CircuitResetTimeout);
            }
          
            if (maxRetries.HasValue) MaxRetries = maxRetries.Value;
            if (retryDelay.HasValue) RetryDelayMilliseconds = retryDelay.Value;
            if (healthCheckInterval.HasValue) HealthCheckIntervalMilliseconds = healthCheckInterval.Value;
            // Subscribe to the OnFailover event
            OnFailover += (sender, args) =>
            {
                _dmeEditor.AddLogMessage($"Failover occurred: From {args.FromDataSource} to {args.ToDataSource}.");
            };

            _healthCheckTimer = new System.Timers.Timer(HealthCheckIntervalMilliseconds);
            _healthCheckTimer.Elapsed += PerformHealthCheck;
            _healthCheckTimer.Start();

        }
        // Retrieves current data source from DMEEditor by name

        #region Proxy DataSource Functions
        private async Task<TResult> ExecAsync<TResult>(
    string methodName,
    Func<IDataSource, Task<TResult>> operation,
    params object[] args)
        {
            // Audit: start
            var argsText = string.Join(", ", args.Select(a => a?.ToString() ?? "<null>"));
            DMEEditor.Logger.LogTrace(
              $"[START] {methodName}({argsText})");

            Exception lastEx = null;
            var sw = Stopwatch.StartNew();

            // Weighted, health-checked, circuit-aware pick
            var candidates = _dataSourceNames
                .Where(n => IsHealthy(n) && !IsCircuitOpen(n))
                .OrderByDescending(n => _dataSourceWeights.GetValueOrDefault(n, 1))
                .ToList();
            if (!candidates.Any()) candidates = _dataSourceNames.ToList();

            foreach (var dsName in candidates)
            {
                var ds = GetPooledConnection(dsName);
                for (int attempt = 1; attempt <= MaxRetries; attempt++)
                {
                    try
                    {
                        var result = await operation(ds).ConfigureAwait(false);
                        sw.Stop();
                        RecordSuccess(dsName, sw.Elapsed);

                        // Audit: success
                        DMEEditor.Logger.LogTrace(
                          $"[END]   {methodName} on {dsName} succeeded in {sw.ElapsedMilliseconds} ms → {result}");
                        ReturnConnection(dsName, ds);
                        return result;
                    }
                    catch (Exception ex) when (ex is TimeoutException || ex is IOException)
                    {
                        lastEx = ex;
                        RecordFailure(dsName);
                        DMEEditor.Logger.LogWarning(
                          $"Transient error in {methodName} on {dsName} (attempt {attempt}): {ex.Message}");
                        await Task.Delay(RetryDelayMilliseconds * attempt).ConfigureAwait(false);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        lastEx = ex;
                        RecordFailure(dsName);
                        DMEEditor.Logger.LogError(
                            $"Persistent error in {methodName} on {dsName}: {ex.Message}");
                        OnFailover?.Invoke(this, new FailoverEventArgs
                        {
                            FromDataSource = dsName,
                            ToDataSource = null
                        });
                        throw;
                    }
                }
            }

            throw new AggregateException($"All retries failed for {methodName}", lastEx);
        }


        public IDataSource Current
        {
            get
            {
                var dataSource = _dmeEditor.GetDataSource(_dataSourceNames[_currentIndex]);
                if (dataSource == null)
                {
                    _dmeEditor.AddLogMessage("Warning", $"Current data source at index {_currentIndex} ({_dataSourceNames[_currentIndex]}) is null. Attempting failover.",
                                            DateTime.Now, 0, null, Errors.Warning);
                    Failover();
                    return _dmeEditor.GetDataSource(_dataSourceNames[_currentIndex]);
                }
                return dataSource;
            }
        }

        private void SetCurrentDataSource(int index)
        {
            _currentIndex = index;
            var ds = Current;
            if (ds != null)
            {
                _currentDataSourceName = ds.DatasourceName; // Store name for logging
                this.DatasourceName = ds.DatasourceName;
                this.DatasourceType = ds.DatasourceType;
                this.Category = ds.Category;
                this.ConnectionStatus = ds.ConnectionStatus;
                this.EntitiesNames = ds.EntitiesNames?.ToList() ?? new List<string>(); // Defensive copy
                this.Entities = ds.Entities?.ToList() ?? new List<EntityStructure>(); // Defensive copy
                this.Dataconnection = ds.Dataconnection;

                // Refresh metrics
                var metrics = _metrics.GetOrAdd(ds.DatasourceName, _ => new DataSourceMetrics());
                metrics.LastChecked = DateTime.UtcNow;
            }
        }

        private bool IsDataSourceHealthy(string dsName)
        {
            try
            {
                // Use timed execution with a cancellation token to avoid hanging
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // 5-second timeout

                var task = Task.Run(() => {
                    var ds = GetPooledConnection(dsName); // Use connection pooling

                    if (ds == null) return false;

                    try
                    {
                        if (ds.ConnectionStatus != ConnectionState.Open)
                            ds.Openconnection();

                        bool isHealthy = ds.ConnectionStatus == ConnectionState.Open;

                        // Return connection to pool
                        ReturnConnection(dsName, ds);

                        return isHealthy;
                    }
                    catch
                    {
                        // Don't return failed connections to the pool
                        return false;
                    }
                }, cts.Token);

                // Wait for completion or timeout
                if (task.Wait(TimeSpan.FromSeconds(5)))
                    return task.Result;

                return false; // Timed out
            }
            catch (Exception ex)
            {
                _dmeEditor.AddLogMessage("Error", $"Health check for {dsName} failed: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }

        public async Task<T> ExecuteWithLoadBalancing<T>(Func<IDataSource, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            // Create a prioritized list of data sources
            var dataSourcePriorities = _dataSourceNames
                .Select(name => new {
                    Name = name,
                    IsHealthy = IsHealthy(name),
                    CircuitOpen = IsCircuitOpen(name),
                    Weight = _dataSourceWeights.GetValueOrDefault(name, 1),
                    Metrics = _metrics.GetOrAdd(name, _ => new DataSourceMetrics())
                })
                .Where(ds => ds.IsHealthy && !ds.CircuitOpen)
                .OrderByDescending(ds => ds.Weight)
                .ThenBy(ds => ds.Metrics.AverageResponseTime) // Prioritize faster data sources
                .ThenBy(ds => ds.Metrics.TotalRequests) // Basic load distribution
                .Select(ds => ds.Name)
                .ToList();

            if (dataSourcePriorities.Count == 0)
            {
                // Try any data source as a last resort
                dataSourcePriorities = _dataSourceNames.ToList();
            }

            var attemptedDataSources = new HashSet<string>();
            Exception lastException = null;

            foreach (var dsName in dataSourcePriorities)
            {
                // Skip if we've already tried this data source
                if (attemptedDataSources.Contains(dsName))
                    continue;

                attemptedDataSources.Add(dsName);
                var ds = GetPooledConnection(dsName);

                if (ds == null) continue;

                try
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    T result = await operation(ds).ConfigureAwait(false);
                    stopwatch.Stop();

                    // Record success metrics
                    RecordSuccess(dsName, stopwatch.Elapsed);

                    // Return connection to pool
                    ReturnConnection(dsName, ds);

                    return result;
                }
                catch (Exception ex) when (!(ex is OperationCanceledException)) // Don't catch cancellation
                {
                    lastException = ex;
                    RecordFailure(dsName);
                    _dmeEditor.AddLogMessage("Error", $"Operation failed on {dsName}: {ex.Message}",
                        DateTime.Now, 0, null, Errors.Failed);

                    // Don't return failed connections to pool
                    // Continue to next data source
                }

                // Check for cancellation between attempts
                cancellationToken.ThrowIfCancellationRequested();
            }

            throw new AggregateException($"Operation failed on all {attemptedDataSources.Count} attempted data sources", lastException);
        }

        public IDataSource GetPooledConnection(string dsName)
        {
            var pool = _connectionPools.GetOrAdd(dsName, _ => new ConcurrentQueue<PooledConnection>());

            // Remove stale connections
            CleanupConnectionPool(dsName);

            if (pool.TryDequeue(out var connection))
            {
                if (connection.DataSource.ConnectionStatus != ConnectionState.Open)
                {
                    try
                    {
                        connection.DataSource.Openconnection();
                    }
                    catch
                    {
                        // Connection failed to open, get a new one
                        return _dmeEditor.GetDataSource(dsName);
                    }
                }

                return connection.DataSource;
            }

            return _dmeEditor.GetDataSource(dsName);
        }

        public void ReturnConnection(string dsName, IDataSource connection)
        {
            if (connection == null) return;

            var pool = _connectionPools.GetOrAdd(dsName, _ => new ConcurrentQueue<PooledConnection>());

            // Check if pool is full
            if (pool.Count < MaxPoolSize && connection.ConnectionStatus == ConnectionState.Open)
            {
                pool.Enqueue(new PooledConnection
                {
                    DataSource = connection,
                    LastUsed = DateTime.UtcNow
                });
            }
            else
            {
                // Dispose connection if pool is full
                try
                {
                    connection.Closeconnection();
                }
                catch
                {
                    // Ignore close errors
                }
            }
        }

        private void CleanupConnectionPool(string dsName)
        {
            var pool = _connectionPools.GetOrAdd(dsName, _ => new ConcurrentQueue<PooledConnection>());
            var now = DateTime.UtcNow;
            var newPool = new ConcurrentQueue<PooledConnection>();

            // Keep only non-expired connections
            while (pool.TryDequeue(out var connection))
            {
                if (now - connection.LastUsed < ConnectionTimeout)
                {
                    newPool.Enqueue(connection);
                }
                else
                {
                    try
                    {
                        connection.DataSource.Closeconnection();
                    }
                    catch
                    {
                        // Ignore close errors
                    }
                }
            }

            _connectionPools[dsName] = newPool;
        }
        private string GetNextBalancedDataSource()
        {
            lock (_balancingLock)
            {
                // Filter to healthy data sources with closed circuits
                var candidates = _dataSourceNames
                    .Where(ds => IsHealthy(ds) && !IsCircuitOpen(ds))
                    .ToList();

                if (!candidates.Any())
                {
                    // Fall back to any data source if all are unhealthy
                    candidates = _dataSourceNames.ToList();
                }

                if (!candidates.Any())
                {
                    throw new InvalidOperationException("No data sources available for selection");
                }

                // Implement weighted selection
                int totalWeight = candidates.Sum(ds => _dataSourceWeights.GetValueOrDefault(ds, 1));
                int selectionPoint = new Random().Next(totalWeight);
                int currentWeight = 0;

                foreach (var ds in candidates)
                {
                    currentWeight += _dataSourceWeights.GetValueOrDefault(ds, 1);
                    if (currentWeight > selectionPoint)
                        return ds;
                }

                // Fallback to simple round-robin if weights don't work out
                _currentBalancingIndex = (_currentBalancingIndex + 1) % candidates.Count;
                return candidates[_currentBalancingIndex];
            }
        }


        private void PerformHealthCheck(object sender, ElapsedEventArgs e)
        {
            foreach (var dsName in _dataSourceNames)
            {
                bool isHealthy = IsDataSourceHealthy(dsName);
                _healthStatus[dsName] = isHealthy;
                if (isHealthy)
                {
                    ResetFailureCount(dsName);
                }
                else
                {
                    RecordFailure(dsName);
                }
            }
        }


        // Update failover logic to respect circuit breakers
        private void Failover()
        {
            var originalIndex = _currentIndex;
            var originalDataSourceName = _dataSourceNames[originalIndex];

            for (int i = 1; i <= _dataSourceNames.Count; i++)
            {
                var nextIndex = (originalIndex + i) % _dataSourceNames.Count;
                var candidateDataSourceName = _dataSourceNames[nextIndex];

                // Skip unhealthy or open-circuit data sources
                if (!IsHealthy(candidateDataSourceName) || IsCircuitOpen(candidateDataSourceName))
                {
                    _dmeEditor.AddLogMessage(
                        $"Skipping {candidateDataSourceName} due to health or circuit issues.");
                    continue;
                }

                var candidateDataSource = _dmeEditor.GetDataSource(candidateDataSourceName);

                if (candidateDataSource != null && candidateDataSource.Openconnection() == ConnectionState.Open)
                {
                    _currentIndex = nextIndex;
                    SetCurrentDataSource(nextIndex);
                    _dmeEditor.AddLogMessage($"Failover successful to {candidateDataSourceName}.");
                    RaiseFailoverEvent(originalDataSourceName, candidateDataSourceName);
                    RecordSuccess(candidateDataSourceName, TimeSpan.Zero); // Mark as successful
                    return;
                }
                else
                {
                    _dmeEditor.AddLogMessage($"Failover attempt to {candidateDataSourceName} failed.");
                    RecordFailure(candidateDataSourceName);
                }
            }

            throw new Exception("Failover failed: No available data sources.");
        }
        // Update success recording logic
        private void RecordSuccess(string dsName, TimeSpan duration)
        {
            if (_circuitBreakers.TryGetValue(dsName, out var breaker))
            {
                breaker.RecordSuccess();

                // Update metrics
                // Update metrics
                var metrics = _metrics.GetOrAdd(dsName, _ => new DataSourceMetrics());
                metrics.IncrementTotalRequests();
                metrics.IncrementSuccessfulRequests();
                metrics.AverageResponseTime =
                    ((metrics.AverageResponseTime * (metrics.SuccessfulRequests - 1)) + duration.TotalMilliseconds)
                    / metrics.SuccessfulRequests;
                metrics.LastRequested = metrics.LastSuccessful = DateTime.UtcNow;
            }
        }

        // Update failure recording logic
        private void RecordFailure(string dsName)
        {
            if (_circuitBreakers.TryGetValue(dsName, out var breaker))
            {
                breaker.RecordFailure();

                // Update metrics
                var metrics = _metrics.GetOrAdd(dsName, _ => new DataSourceMetrics());
                metrics.IncrementTotalRequests();
                metrics.IncrementFailedRequests();
                metrics.LastRequested = DateTime.UtcNow;
            }
        }
        // Replace IsCircuitOpen with the CircuitBreaker's CanExecute
        private bool IsCircuitOpen(string dsName)
        {
            if (_circuitBreakers.TryGetValue(dsName, out var breaker))
            {
                return !breaker.CanExecute();
            }
            return false; // Default to allowing execution if no circuit breaker exists
        }

        // Modify ResetFailureCount to remove circuit open time
        private void ResetFailureCount(string dsName)
        {
            if (_failureCounts.ContainsKey(dsName))
                _failureCounts[dsName] = 0;

            _circuitOpenTimes.TryRemove(dsName, out _);
        }
        private async Task<T> ExecuteWithPolicy<T>(string dataSourceName, Func<Task<T>> operation)
        {
            if (_circuitBreakers.TryGetValue(dataSourceName, out var breaker) && !breaker.CanExecute())
            {
                _dmeEditor.AddLogMessage($"Circuit is open for {dataSourceName}. Finding alternative.");
                Failover();
                return await ExecuteWithPolicy(_dataSourceNames[_currentIndex], operation);
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int attempt = 1; attempt <= _options.MaxRetries; attempt++)
            {
                try
                {
                    T result = await operation();
                    stopwatch.Stop();

                    // Record successful operation
                    RecordSuccess(dataSourceName, stopwatch.Elapsed);
                    return result;
                }
                catch (Exception ex)
                {
                    bool isRetryable = ShouldRetry(ex);

                    // Record failure
                    RecordFailure(dataSourceName);

                    _dmeEditor.AddLogMessage($"Attempt {attempt} failed for {dataSourceName}: {ex.Message}");

                    // If not retryable or last attempt, rethrow or failover
                    if (!isRetryable || attempt >= _options.MaxRetries)
                    {
                        if (isRetryable)
                        {
                            _dmeEditor.AddLogMessage($"Maximum retries exceeded for {dataSourceName}. Attempting failover.");
                            Failover();

                            // Try one more time with the new data source
                            return await ExecuteWithPolicy(_dataSourceNames[_currentIndex], operation);
                        }
                        throw;
                    }

                    await Task.Delay(_options.RetryDelayMilliseconds * (1 << (attempt - 1))); // Exponential backoff
                }
            }

            // This shouldn't be reached, but required by compiler
            throw new InvalidOperationException("Unexpected execution path in retry policy");
        }
        private bool ShouldRetry(Exception ex)
        {
            // Add specific exception types that should trigger retry
            return ex is TimeoutException ||
                   ex is IOException; //||  (ex is SqlException sqlEx && IsTransientSqlError(sqlEx));
        }
        //private bool IsTransientSqlError(SqlException ex)
        //{
        //    // Common SQL Server transient error codes
        //    int[] transientErrorCodes = { 4060, 40197, 40501, 40613, 49918, 49919, 49920 };
        //    return transientErrorCodes.Contains(ex.Number);
        //}
        // Add a method for retrieving entities with caching support
        public object GetEntityWithCache(string entityName, List<AppFilter> filter, TimeSpan? expiration = null)
        {
            if (!_options.EnableCaching)
                return GetEntity(entityName, filter);

            string cacheKey = GenerateCacheKey(entityName, filter);

            // Try to get from cache first
            if (_entityCache.TryGetValue(cacheKey, out var entry) && entry.Expiration > DateTime.UtcNow)
            {
                _dmeEditor.AddLogMessage($"Cache hit for {entityName}");
                return entry.Data;
            }

            // Get from actual data source
            object result = GetEntity(entityName, filter);

            // Cache the result if not null
            if (result != null)
            {
                _entityCache[cacheKey] = new CacheEntry
                {
                    Data = result,
                    Expiration = DateTime.UtcNow.Add(expiration ?? _options.DefaultCacheExpiration)
                };
            }

            return result;
        }
        private string GenerateCacheKey(string entityName, List<AppFilter> filter)
        {
            if (filter == null || filter.Count == 0)
                return entityName;

            string filterString = string.Join(":",
                filter.OrderBy(f => f.FieldName)
                      .Select(f => $"{f.FieldName}:{f.Operator}:{f.FilterValue}"));
            return $"{entityName}:{filterString}";
        }

        // Add cache management methods
        public void InvalidateCache(string entityName = null)
        {
            if (string.IsNullOrEmpty(entityName))
            {
                _entityCache.Clear();
                _dmeEditor.AddLogMessage("Cache cleared for all entities");
            }
            else
            {
                var keysToRemove = _entityCache.Keys
                    .Where(k => k.StartsWith($"{entityName}:") || k == entityName)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _entityCache.TryRemove(key, out _);
                }

                _dmeEditor.AddLogMessage($"Cache cleared for entity {entityName}");
            }
        }

        // Add metrics retrieval method
        public IDictionary<string, DataSourceMetrics> GetMetrics()
        {
            return new Dictionary<string, DataSourceMetrics>(_metrics);
        }

        #endregion Proxy DataSource Functions
        #region IDataSource Functions
        public List<string> GetEntitesList()
        {
            int retryCount = 0;
            while (retryCount < MaxRetries)
            {
                try
                {
                    return Current.GetEntitesList();
                }
                catch (Exception ex)
                {
                    if (!ShouldRetry(ex))
                        throw;

                    retryCount++;
                    Task.Delay(RetryDelayMilliseconds).Wait();
                    Failover();
                }
            }
            throw new Exception("Maximum retry attempts exceeded.");
        }
        public IBindingList RunQuery(string qrystr)
        {
            return RetryPolicy(async () =>
            {
                var dataSource = Current;
                if (dataSource != null)
                {
                    return await Task.Run(() => dataSource.RunQuery(qrystr) != null);
                }
                return false;
            }).Result
               ? Current.RunQuery(qrystr)
               : null;
        }
        public IErrorsInfo ExecuteSql(string sql)
        {
            return RetryPolicy(async () =>
            {
                var dataSource = Current;
                if (dataSource != null)
                {
                    return await Task.Run(() => dataSource.ExecuteSql(sql).Flag == Errors.Ok);
                }
                return false;
            }).Result
              ? Current.ExecuteSql(sql)
              : new ErrorsInfo();

        }
        public bool CreateEntityAs(EntityStructure entity)
        {
            return RetryPolicy(async () =>
            {
                var dataSource = Current;
                if (dataSource != null)
                {
                    return await Task.Run(() => dataSource.CreateEntityAs(entity));
                }
                return false;
            }).Result
             ? Current.CreateEntityAs(entity)
             : false;

        }
        public Type GetEntityType(string EntityName)
        {
            return Current.GetEntityType(EntityName);
        }
        public bool CheckEntityExist(string EntityName)
        {
            return RetryPolicy(async () =>
            {
                var dataSource = Current;
                if (dataSource != null)
                {
                    return await Task.Run(() => dataSource.CheckEntityExist(EntityName));
                }
                return false;
            }).Result;
        }
        public int GetEntityIdx(string entityName)
        {
            return Current.GetEntityIdx(entityName);
        }
        public List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            return RetryPolicy(async () =>
            {
                var dataSource = Current;
                if (dataSource != null)
                {
                    return await Task.Run(() => dataSource.GetChildTablesList(tablename, SchemaName, Filterparamters) != null);
                }
                return false;
            }).Result
            ? Current.GetChildTablesList(tablename, SchemaName, Filterparamters)
            : new List<ChildRelation>();

        }
        public List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            return RetryPolicy(async () =>
            {
                var dataSource = Current;
                if (dataSource != null)
                {
                    return await Task.Run(() => dataSource.GetEntityforeignkeys(entityname, SchemaName) != null);
                }
                return false;
            }).Result
           ? Current.GetEntityforeignkeys(entityname, SchemaName)
           : new List<RelationShipKeys>();

        }
        public EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            EntityStructure result = null;
            RetryPolicy(async () =>
            {
                var dataSource = Current;
                if (dataSource != null)
                {
                    result = await Task.Run(() => dataSource.GetEntityStructure(EntityName, refresh));
                    return result != null;
                }
                return false;
            }).Wait();

            return result;
        }
        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            try { return Current.GetEntityStructure(fnd, refresh); }
            catch { Failover(); return Current.GetEntityStructure(fnd, refresh); }
        }
        public IErrorsInfo RunScript(ETLScriptDet script)
        {
            return RetryPolicy(async () =>
            {
                var dataSource = Current;
                if (dataSource != null)
                {
                    return await Task.Run(() => dataSource.RunScript(script).Flag == Errors.Ok);
                }
                return false;
            }).Result
                ? Current.RunScript(script)
                : _dmeEditor.ErrorObject;
        }
        public List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null)
        {
            return RetryPolicy(async () =>
            {
                var dataSource = Current;
                if (dataSource != null)
                {
                    return await Task.Run(() => dataSource.GetCreateEntityScript(entities) != null);
                }
                return false;
            }).Result
           ? Current.GetCreateEntityScript(entities)
           : new List<ETLScriptDet>();

        }
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            return RetryPolicy(async () =>
            {
                var dataSource = Current;
                if (dataSource != null)
                {
                    return await Task.Run(() => dataSource.CreateEntities(entities).Flag == Errors.Ok);
                }
                return false;
            }).Result
                ? Current.CreateEntities(entities)
                : _dmeEditor.ErrorObject;
        }
        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            return RetryPolicy(async () =>
            {
                try
                {
                    return await Task.Run(() => Current.UpdateEntities(EntityName, UploadData, progress).Flag == Errors.Ok);
                }
                catch
                {
                    Failover();
                    return await Task.Run(() => Current.UpdateEntities(EntityName, UploadData, progress).Flag == Errors.Ok);
                }
            }).Result
                ? Current.UpdateEntities(EntityName, UploadData, progress)
                : _dmeEditor.ErrorObject;
        }
        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            return RetryPolicy(async () =>
            {
                try
                {
                    return await Task.Run(() => Current.UpdateEntity(EntityName, UploadDataRow).Flag == Errors.Ok);
                }
                catch
                {
                    Failover();
                    return await Task.Run(() => Current.UpdateEntity(EntityName, UploadDataRow).Flag == Errors.Ok);
                }
            }).Result
                ? Current.UpdateEntity(EntityName, UploadDataRow)
                : _dmeEditor.ErrorObject;
        }
        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            return RetryPolicy(async () =>
            {
                try
                {
                    return await Task.Run(() => Current.DeleteEntity(EntityName, UploadDataRow).Flag == Errors.Ok);
                }
                catch
                {
                    Failover();
                    return await Task.Run(() => Current.DeleteEntity(EntityName, UploadDataRow).Flag == Errors.Ok);
                }
            }).Result
                ? Current.DeleteEntity(EntityName, UploadDataRow)
                : _dmeEditor.ErrorObject;
        }
        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            return RetryPolicy(async () =>
            {
                try
                {
                    return await Task.Run(() => Current.InsertEntity(EntityName, InsertedData).Flag == Errors.Ok);
                }
                catch
                {
                    Failover();
                    return await Task.Run(() => Current.InsertEntity(EntityName, InsertedData).Flag == Errors.Ok);
                }
            }).Result
                ? Current.InsertEntity(EntityName, InsertedData)
                : _dmeEditor.ErrorObject;
        }
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            var result = RetryPolicy(async () =>
            {
                try
                {
                    _dmeEditor.AddLogMessage($"Attempting to begin transaction on {Current.DatasourceName}.");
                    var beginResult = Current.BeginTransaction(args);
                    if (beginResult.Flag == Errors.Ok)
                    {
                        _dmeEditor.AddLogMessage($"Transaction started successfully on {Current.DatasourceName}.");
                        return true;
                    }
                    else
                    {
                        _dmeEditor.AddLogMessage($"Failed to start transaction on {Current.DatasourceName}. Error: {beginResult.Message}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _dmeEditor.AddLogMessage($"Transaction begin failed on {Current.DatasourceName}. Exception: {ex.Message}");
                    Failover(); // Switch to the next available data source
                    return false;
                }
            }).Result;

            if (!result)
            {
                _dmeEditor.AddLogMessage("Failed to start transaction after all retries. Please check data source health.");
                throw new Exception("Transaction begin failed across all retries and failovers.");
            }

            return Current.ErrorObject;
        }
        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            var result = RetryPolicy(async () =>
            {
                try
                {
                    _dmeEditor.AddLogMessage($"Attempting to end transaction on {Current.DatasourceName}.");
                    var endResult = Current.EndTransaction(args);
                    if (endResult.Flag == Errors.Ok)
                    {
                        _dmeEditor.AddLogMessage($"Transaction ended successfully on {Current.DatasourceName}.");
                        return true;
                    }
                    else
                    {
                        _dmeEditor.AddLogMessage($"Failed to end transaction on {Current.DatasourceName}. Error: {endResult.Message}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _dmeEditor.AddLogMessage($"Transaction end failed on {Current.DatasourceName}. Exception: {ex.Message}");
                    return false; // No failover for `EndTransaction`
                }
            }).Result;

            if (!result)
            {
                _dmeEditor.AddLogMessage("Failed to end transaction after all retries. Please check data source health.");
                throw new Exception("Transaction end failed across all retries.");
            }

            return Current.ErrorObject;
        }
        public IErrorsInfo Commit(PassedArgs args)
        {
            var result = RetryPolicy(async () =>
            {
                try
                {
                    _dmeEditor.AddLogMessage($"Attempting to commit transaction on {Current.DatasourceName}.");
                    var commitResult = Current.Commit(args);
                    if (commitResult.Flag == Errors.Ok)
                    {
                        _dmeEditor.AddLogMessage($"Transaction committed successfully on {Current.DatasourceName}.");
                        return true;
                    }
                    else
                    {
                        _dmeEditor.AddLogMessage($"Failed to commit transaction on {Current.DatasourceName}. Error: {commitResult.Message}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _dmeEditor.AddLogMessage($"Transaction commit failed on {Current.DatasourceName}. Exception: {ex.Message}");
                    return false; // No failover for `Commit`
                }
            }).Result;

            if (!result)
            {
                _dmeEditor.AddLogMessage("Failed to commit transaction after all retries. Please check data source health.");
                throw new Exception("Transaction commit failed across all retries.");
            }

            return Current.ErrorObject;
        }
        public IBindingList GetEntity(string EntityName, List<AppFilter> filter)
        {
            IBindingList result = null;
            RetryPolicy(async () =>
            {
                try
                {
                    result = Current.GetEntity(EntityName, filter);
                    return true;
                }
                catch
                {
                    Failover();
                    return false;
                }
            }).Wait(); // Wait to ensure synchronous execution

            return result;
        }
        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            PagedResult result = null;
            RetryPolicy(async () =>
            {
                try
                {
                    result = Current.GetEntity(EntityName, filter, pageNumber, pageSize);
                    return true;
                }
                catch
                {
                    Failover();
                    return false;
                }
            }).Wait(); // Wait to ensure synchronous execution

            return result;
        }
        public async Task<IBindingList> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            IBindingList result = null;
            await RetryPolicy(async () =>
            {
                try
                {
                    result = await Current.GetEntityAsync(EntityName, Filter);
                    return true;
                }
                catch
                {
                    Failover();
                    return false;
                }
            });

            return result;
        }
        public async Task<double> GetScalarAsync(string query)
        {
            async Task<(bool success, double result)> ExecuteScalarAsync(Func<Task<double>> action)
            {
                try
                {
                    double result = await action();
                    return (true, result);
                }
                catch (Exception ex)
                {
                    _dmeEditor.AddLogMessage($"Error in GetScalarAsync: {ex.Message}. Retrying...");
                    return (false, 0);
                }
            }

            double scalarValue = 0;

            var success = await RetryPolicy(async () =>
            {
                var (isSuccessful, result) = await ExecuteScalarAsync(() => Current.GetScalarAsync(query));
                if (isSuccessful)
                {
                    scalarValue = result;
                }
                return isSuccessful;
            });

            if (!success)
            {
                _dmeEditor.AddLogMessage($"GetScalarAsync failed on all retries for query: {query}. Attempting failover...");
                Failover();
                scalarValue = await Current.GetScalarAsync(query);
            }

            return scalarValue;
        }
        public double GetScalar(string query)
        {
            (bool success, double result) ExecuteScalar(Func<double> action)
            {
                try
                {
                    double result = action();
                    return (true, result);
                }
                catch (Exception ex)
                {
                    _dmeEditor.AddLogMessage($"Error in GetScalar: {ex.Message}. Retrying...");
                    return (false, 0);
                }
            }

            double scalarValue = 0;

            var success = RetryPolicy(async () =>
            {
                var (isSuccessful, result) = ExecuteScalar(() => Current.GetScalar(query));
                if (isSuccessful)
                {
                    scalarValue = result;
                }
                return isSuccessful;
            }).Result;

            if (!success)
            {
                _dmeEditor.AddLogMessage($"GetScalar failed on all retries for query: {query}. Attempting failover...");
                Failover();
                scalarValue = Current.GetScalar(query);
            }

            return scalarValue;
        }
        public ConnectionState Openconnection()
        {
            return RetryPolicy(async () =>
            {
                var dataSource = Current;
                if (dataSource != null)
                {
                    return await Task.Run(() => dataSource.Openconnection() == ConnectionState.Open);
                }
                return false;
            }).Result
               ? ConnectionState.Open
               : ConnectionState.Broken;
        }
        public ConnectionState Closeconnection()
        {
            try
            {
                if (Current != null)
                {
                    _dmeEditor.AddLogMessage($"Attempting to close connection for data source: {Current.DatasourceName}...");
                    return Current.Closeconnection();
                }
            }
            catch (Exception ex)
            {
                _dmeEditor.AddLogMessage($"Failed to close connection for data source {Current?.DatasourceName}. Error: {ex.Message}. Attempting failover...");
                Failover();
                return Current?.Closeconnection() ?? ConnectionState.Closed;
            }

            _dmeEditor.AddLogMessage($"Closeconnection called, but no active data source was found. Returning ConnectionState.Closed.");
            return ConnectionState.Closed;
        }
        #endregion IDataSource Functions

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Dispose managed resources here.
                if (_dmeEditor != null)
                {
                    _dmeEditor.AddLogMessage("Disposing ProxyDataSource...");
                }

                // Close any open connections.
                foreach (var dataSourceName in _dataSourceNames)
                {
                    var dataSource = _dmeEditor.GetDataSource(dataSourceName);
                    if (dataSource != null)
                    {
                        try
                        {
                            dataSource.Closeconnection();
                            dataSource.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _dmeEditor.AddLogMessage($"Error disposing data source '{dataSourceName}': {ex.Message}");
                        }
                    }
                }
            }

            // Dispose unmanaged resources here (if any).
            _healthCheckTimer.Stop();
            _disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Suppress finalization since cleanup has already been done.
        }
        // Finalizer (optional, only if unmanaged resources are used).
        ~ProxyDataSource()
        {
            Dispose(false);
        }
        #region "Improvments"
        //private bool ShouldRetry(Exception ex)
        //{
        //    // Identify retryable exceptions
        //    return ex is TimeoutException || ex is IOException;
        //}
        private void MonitorHealth()
        {
            Task.Run(() =>
            {
                while (!_disposed)
                {
                    foreach (var dsName in _dataSourceNames)
                    {
                        var wasHealthy = _healthStatus.TryGetValue(dsName, out var currentHealth) && currentHealth;
                        var isHealthy = IsDataSourceHealthy(dsName);

                        if (wasHealthy != isHealthy)
                        {
                            _healthStatus[dsName] = isHealthy;
                            var status = isHealthy ? "healthy" : "unhealthy";
                            _dmeEditor.AddLogMessage($"Data source {dsName} is now {status}.");
                        }
                    }

                    Task.Delay(HealthCheckIntervalMilliseconds).Wait();
                }
            });
        }
        private string GetNextDataSource()
        {
            return _dataSourceNames
                .Where(dsName => IsHealthy(dsName))
                .OrderByDescending(dsName => _dataSourceWeights.GetValueOrDefault(dsName, 1))
                .FirstOrDefault();
        }
        private void LogFailover(string fromDs, string toDs)
        {
            _dmeEditor.Logger.WriteLog($"Failover occurred from {fromDs} to {toDs}");
        }
        private void LogRetry(string dsName, int retryCount)
        {
            _dmeEditor.Logger.WriteLog($"Retry attempt {retryCount} for {dsName}");
        }
        //private bool IsCircuitOpen(string dsName)
        //{
        //    return _failureCounts.TryGetValue(dsName, out var failures) && failures >= FailureThreshold;
        //}
        //private void RecordFailure(string dsName)
        //{
        //    _failureCounts[dsName] = _failureCounts.GetValueOrDefault(dsName, 0) + 1;
        //}
        //private void ResetFailureCount(string dsName)
        //{
        //    if (_failureCounts.ContainsKey(dsName))
        //        _failureCounts[dsName] = 0;
        //}
        public void AddDataSource(string dsName, int weight = 1)
        {
            if (!_dataSourceNames.Contains(dsName))
            {
                _dataSourceNames.Add(dsName);
                _dataSourceWeights[dsName] = weight;
            }
        }
        public void RemoveDataSource(string dsName)
        {
            _dataSourceNames.Remove(dsName);
            _dataSourceWeights.Remove(dsName);
        }
        private void RaiseFailoverEvent(string fromDs, string toDs)
        {
            OnFailover?.Invoke(this, new FailoverEventArgs { FromDataSource = fromDs, ToDataSource = toDs });
        }
        public IDataSource GetConnection(string dsName)
        {
            return _activeConnections.GetOrAdd(dsName, name => _dmeEditor.GetDataSource(name));
        }
        //private async void PerformHealthCheck(object sender, ElapsedEventArgs e)
        //{
        //    var tasks = _dataSourceNames.Select(async dataSourceName =>
        //    {
        //        var dataSource = _dmeEditor.GetDataSource(dataSourceName);

        //        if (dataSource == null || dataSource.Openconnection() != ConnectionState.Open)
        //        {
        //            _dmeEditor.AddLogMessage($"Health check failed for data source: {dataSourceName}");
        //            _healthStatus[dataSourceName] = false;
        //        }
        //        else
        //        {
        //            _dmeEditor.AddLogMessage($"Health check passed for data source: {dataSourceName}");
        //            _healthStatus[dataSourceName] = true;
        //        }
        //    });

        //    await Task.WhenAll(tasks);
        //}
        private async Task<bool> RetryPolicy(Func<Task<bool>> action)
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                if (await action()) return true;

                _dmeEditor.AddLogMessage($"Attempt {attempt} failed. Retrying in {RetryDelayMilliseconds} ms...");
                await Task.Delay(RetryDelayMilliseconds);
            }

            return false;
        }
        private bool IsHealthy(string dsName) => _healthStatus.TryGetValue(dsName, out var healthy) && healthy;
        #endregion "Improvments"
    }


}
