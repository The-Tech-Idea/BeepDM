using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
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
        private readonly List<string> _dataSourceNames;
        private readonly Dictionary<string, bool> _healthStatus = new();
        private int _currentIndex = 0;
        private bool _disposed = false;
        private string _currentDataSourceName;
        private readonly Dictionary<string, int> _dataSourceWeights = new Dictionary<string, int>();
        private readonly Timer _healthCheckTimer;
        public event EventHandler<PassedArgs> PassEvent;
        private readonly Dictionary<string, int> _failureCounts = new();
        private readonly int FailureThreshold = 5;

        public event EventHandler<FailoverEventArgs> OnFailover;
        public class FailoverEventArgs : EventArgs
        {
            public string FromDataSource { get; set; }
            public string ToDataSource { get; set; }
        }
        private readonly ConcurrentDictionary<string, IDataSource> _activeConnections = new();


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

        public ProxyDataSource(IDMEEditor dmeEditor, List<string> dataSourceNames, int? maxRetries = null, int? retryDelay = null, int? healthCheckInterval = null)
        {
            _dmeEditor = dmeEditor;
            _dataSourceNames = dataSourceNames;
            _currentIndex = 0; // Start with the first data source
            SetCurrentDataSource(_currentIndex); // Initialize metadata
            _healthStatus = dataSourceNames.ToDictionary(ds => ds, _ => false); // Default all to unhealthy
            if (maxRetries.HasValue) MaxRetries = maxRetries.Value;
            if (retryDelay.HasValue) RetryDelayMilliseconds = retryDelay.Value;
            if (healthCheckInterval.HasValue) HealthCheckIntervalMilliseconds = healthCheckInterval.Value;
            // Subscribe to the OnFailover event
            OnFailover += (sender, args) =>
            {
                _dmeEditor.AddLogMessage($"Failover occurred: From {args.FromDataSource} to {args.ToDataSource}.");
            };

            _healthCheckTimer = new Timer(HealthCheckIntervalMilliseconds);
            _healthCheckTimer.Elapsed += PerformHealthCheck;
            _healthCheckTimer.Start();

        }
        // Retrieves current data source from DMEEditor by name
        private IDataSource Current => _dmeEditor.GetDataSource(_dataSourceNames[_currentIndex]);
        public int MaxRetries { get;  set; } = 3; // Default: 3 retries
        public int RetryDelayMilliseconds { get;  set; } = 1000; // Default: 1 second delay
        public int HealthCheckIntervalMilliseconds { get;  set; } = 30000; // Default: 30 seconds
        private void SetCurrentDataSource(int index)
        {
            _currentIndex = index;
            var ds = Current;
            if (ds != null)
            {
                this.DatasourceName = ds.DatasourceName;
                this.DatasourceType = ds.DatasourceType;
                this.Category = ds.Category;
                this.ConnectionStatus = ds.ConnectionStatus;
                this.EntitiesNames = ds.EntitiesNames;
                this.Entities = ds.Entities;
                this.Dataconnection = ds.Dataconnection;
            }
        }
        private bool IsDataSourceHealthy(string dsName)
        {
            var ds = _dmeEditor.GetDataSource(dsName);
            if (ds == null) return false;
            try
            {
                if (ds.ConnectionStatus != ConnectionState.Open)
                    ds.Openconnection();
                return ds.ConnectionStatus == ConnectionState.Open;
            }
            catch
            {
                return false;
            }
        }
        private void Failover()
        {
            var originalIndex = _currentIndex;
            var originalDataSourceName = _dataSourceNames[originalIndex];

            for (int i = 1; i <= _dataSourceNames.Count; i++)
            {
                var nextIndex = (originalIndex + i) % _dataSourceNames.Count;
                var candidateDataSourceName = _dataSourceNames[nextIndex];

                if (!IsHealthy(candidateDataSourceName) || IsCircuitOpen(candidateDataSourceName))
                {
                    _dmeEditor.AddLogMessage($"Skipping {candidateDataSourceName} due to health or circuit issues.");
                    continue;
                }

                var candidateDataSource = _dmeEditor.GetDataSource(candidateDataSourceName);

                if (candidateDataSource != null && candidateDataSource.Openconnection() == ConnectionState.Open)
                {
                    _currentIndex = nextIndex;
                    SetCurrentDataSource(nextIndex); // Update ProxyDataSource metadata
                    _dmeEditor.AddLogMessage($"Failover successful to {candidateDataSourceName}.");
                    RaiseFailoverEvent(originalDataSourceName, candidateDataSourceName);
                    ResetFailureCount(candidateDataSourceName);
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
        public object RunQuery(string qrystr)
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
                    return await Task.Run(() => dataSource.ExecuteSql(sql).Flag== Errors.Ok);
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
                    return await Task.Run(() => dataSource.GetChildTablesList(tablename, SchemaName, Filterparamters)!=null);
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
        public object GetEntity(string EntityName, List<AppFilter> filter)
        {
            object result = null;
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
        public object GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            object result = null;
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
        public async Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            object result = null;
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
        private bool ShouldRetry(Exception ex)
        {
            // Identify retryable exceptions
            return ex is TimeoutException || ex is IOException;
        }
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
        private bool IsCircuitOpen(string dsName)
        {
            return _failureCounts.TryGetValue(dsName, out var failures) && failures >= FailureThreshold;
        }
        private void RecordFailure(string dsName)
        {
            _failureCounts[dsName] = _failureCounts.GetValueOrDefault(dsName, 0) + 1;
        }
        private void ResetFailureCount(string dsName)
        {
            if (_failureCounts.ContainsKey(dsName))
                _failureCounts[dsName] = 0;
        }
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
        private async void PerformHealthCheck(object sender, ElapsedEventArgs e)
        {
            var tasks = _dataSourceNames.Select(async dataSourceName =>
            {
                var dataSource = _dmeEditor.GetDataSource(dataSourceName);

                if (dataSource == null || dataSource.Openconnection() != ConnectionState.Open)
                {
                    _dmeEditor.AddLogMessage($"Health check failed for data source: {dataSourceName}");
                    _healthStatus[dataSourceName] = false;
                }
                else
                {
                    _dmeEditor.AddLogMessage($"Health check passed for data source: {dataSourceName}");
                    _healthStatus[dataSourceName] = true;
                }
            });

            await Task.WhenAll(tasks);
        }
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
