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
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers.ConnectionHelpers;

namespace TheTechIdea.Beep.Proxy
{
    public partial class ProxyDataSource
    {
        // ── Active data source ────────────────────────────────────────

        public IDataSource Current
        {
            get
            {
                var dsName = _dataSourceNames[_currentIndex];
                var dataSource = _dmeEditor.GetDataSource(dsName);
                if (dataSource == null)
                {
                    _dmeEditor.AddLogMessage("Warning",
                        $"Current data source at index {_currentIndex} ({dsName}) is null. Attempting failover.",
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
            if (ds == null) return;

            _currentDataSourceName = ds.DatasourceName;
            DatasourceName         = ds.DatasourceName;
            DatasourceType         = ds.DatasourceType;
            Category               = ds.Category;
            ConnectionStatus       = ds.ConnectionStatus;
            EntitiesNames          = ds.EntitiesNames?.ToList() ?? new List<string>();
            Entities               = ds.Entities?.ToList()     ?? new List<EntityStructure>();
            Dataconnection         = ds.Dataconnection;

            var metrics = _metrics.GetOrAdd(ds.DatasourceName, _ => new DataSourceMetrics());
            metrics.LastChecked = DateTime.UtcNow;
        }

        // ── Candidate selection  (Phase 3) ───────────────────────────

        /// <summary>
        /// Returns an ordered list of candidate data-source names for the current request,
        /// filtered by health and circuit state and ordered by the active routing strategy.
        /// Falls back to all sources when no healthy candidates remain.
        /// </summary>
        private IReadOnlyList<string> SelectCandidates()
        {
            var healthy = _dataSourceNames
                .Where(n => IsHealthy(n) && !IsCircuitOpen(n))
                .ToList();

            if (healthy.Count == 0)
                healthy = _dataSourceNames.ToList();  // degraded-mode fallback

            return _policy.RoutingStrategy switch
            {
                ProxyRoutingStrategy.RoundRobin               => SelectRoundRobin(healthy),
                ProxyRoutingStrategy.LeastOutstandingRequests => SelectLeastOutstanding(healthy),
                ProxyRoutingStrategy.HealthWeighted           => SelectHealthWeighted(healthy),
                _                                             => SelectWeightedLatency(healthy)
            };
        }

        private IReadOnlyList<string> SelectWeightedLatency(List<string> candidates)
        {
            // Sort by weight (desc) then average latency (asc)
            return candidates
                .OrderByDescending(n => _dataSourceWeights.GetValueOrDefault(n, 1))
                .ThenBy(n => _metrics.TryGetValue(n, out var m) ? m.AverageResponseTime : 0d)
                .ToList();
        }

        private IReadOnlyList<string> SelectRoundRobin(List<string> candidates)
        {
            lock (_balancingLock)
            {
                _currentBalancingIndex = (_currentBalancingIndex + 1) % candidates.Count;
                // Rotate the list so the selected index is first
                var ordered = new List<string>(candidates.Count);
                for (int i = 0; i < candidates.Count; i++)
                    ordered.Add(candidates[(_currentBalancingIndex + i) % candidates.Count]);
                return ordered;
            }
        }

        private IReadOnlyList<string> SelectLeastOutstanding(List<string> candidates)
        {
            return candidates
                .OrderBy(n => _metrics.TryGetValue(n, out var m) ? m.TotalRequests - m.SuccessfulRequests - m.FailedRequests : 0L)
                .ThenByDescending(n => _dataSourceWeights.GetValueOrDefault(n, 1))
                .ToList();
        }

        private IReadOnlyList<string> SelectHealthWeighted(List<string> candidates)
        {
            // Weight inversely proportional to failure rate
            return candidates
                .OrderBy(n =>
                {
                    if (!_metrics.TryGetValue(n, out var m) || m.TotalRequests == 0) return 0d;
                    return (double)m.FailedRequests / m.TotalRequests;
                })
                .ToList();
        }

        // ── Weighted-random pick (used by ExecuteWithLoadBalancing) ──

        private string GetNextBalancedDataSource()
        {
            lock (_balancingLock)
            {
                var candidates = _dataSourceNames
                    .Where(ds => IsHealthy(ds) && !IsCircuitOpen(ds))
                    .ToList();

                if (candidates.Count == 0)
                    candidates = _dataSourceNames.ToList();
                if (candidates.Count == 0)
                    throw new InvalidOperationException("No data sources available for selection.");

                int totalWeight = candidates.Sum(ds => _dataSourceWeights.GetValueOrDefault(ds, 1));
                int selectionPoint = _threadRandom.Value.Next(totalWeight);
                int cumulative = 0;

                foreach (var ds in candidates)
                {
                    cumulative += _dataSourceWeights.GetValueOrDefault(ds, 1);
                    if (cumulative > selectionPoint)
                        return ds;
                }

                // Fallback: deterministic round-robin
                _currentBalancingIndex = (_currentBalancingIndex + 1) % candidates.Count;
                return candidates[_currentBalancingIndex];
            }
        }

        // ── Load-balanced execution ───────────────────────────────────

        public async Task<T> ExecuteWithLoadBalancing<T>(
            Func<IDataSource, Task<T>> operation,
            bool isWrite = false,
            CancellationToken cancellationToken = default)
        {
            // P0 fix: respect roles — route writes to Primary candidates only
            var candidates = isWrite ? SelectWriteCandidates() : SelectCandidates();
            var attempted  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Exception lastException = null;

            foreach (var dsName in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (attempted.Contains(dsName)) continue;
                attempted.Add(dsName);

                var ds = GetPooledConnection(dsName);
                if (ds == null) continue;

                var sw = Stopwatch.StartNew();
                try
                {
                    T result = await operation(ds).ConfigureAwait(false);
                    sw.Stop();
                    RecordSuccess(dsName, sw.Elapsed);
                    ReturnConnection(dsName, ds);
                    return result;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    lastException = ex;
                    var (_, severity) = ProxyErrorClassifier.Classify(ex);
                    RecordFailure(dsName, severity);
                    LogSafe($"Operation failed on {dsName}.", ex);
                }
            }

            throw new AggregateException(
                $"Operation failed on all {attempted.Count} attempted data source(s).", lastException);
        }

        // ── Connection pool ───────────────────────────────────────────

        public IDataSource GetPooledConnection(string dsName)
        {
            var pool = _connectionPools.GetOrAdd(dsName, _ => new ConcurrentQueue<PooledConnection>());
            CleanupConnectionPool(dsName);

            if (pool.TryDequeue(out var connection))
            {
                var state = connection.DataSource.ConnectionStatus;

                // P0 fix: discard Broken connections immediately — Openconnection() on a
                // Broken socket can succeed silently while the underlying handle is dead.
                if (state == ConnectionState.Broken)
                {
                    try { connection.DataSource.Dispose(); } catch { /* best-effort */ }
                    return _dmeEditor.GetDataSource(dsName);
                }

                if (state != ConnectionState.Open)
                {
                    try
                    {
                        var opened = connection.DataSource.Openconnection();
                        if (opened == ConnectionState.Broken)
                        {
                            try { connection.DataSource.Dispose(); } catch { }
                            return _dmeEditor.GetDataSource(dsName);
                        }
                    }
                    catch { return _dmeEditor.GetDataSource(dsName); }
                }
                return connection.DataSource;
            }

            return _dmeEditor.GetDataSource(dsName);
        }

        public void ReturnConnection(string dsName, IDataSource connection)
        {
            if (connection == null) return;

            var pool = _connectionPools.GetOrAdd(dsName, _ => new ConcurrentQueue<PooledConnection>());
            if (pool.Count < MaxPoolSize && connection.ConnectionStatus == ConnectionState.Open)
            {
                pool.Enqueue(new PooledConnection { DataSource = connection, LastUsed = DateTime.UtcNow });
            }
            else
            {
                try { connection.Closeconnection(); } catch { /* best-effort */ }
            }
        }

        private void CleanupConnectionPool(string dsName)
        {
            var pool   = _connectionPools.GetOrAdd(dsName, _ => new ConcurrentQueue<PooledConnection>());
            var now    = DateTime.UtcNow;
            var newPool = new ConcurrentQueue<PooledConnection>();

            while (pool.TryDequeue(out var conn))
            {
                if (now - conn.LastUsed < ConnectionTimeout)
                    newPool.Enqueue(conn);
                else
                    try { conn.DataSource.Closeconnection(); } catch { /* best-effort */ }
            }

            _connectionPools[dsName] = newPool;
        }

        // ── Health checks  (hysteresis-based — Phase 3) ──────────────

        // Per-source consecutive health/unhealthy counters for hysteresis
        private readonly ConcurrentDictionary<string, int> _consecutiveHealthy   = new();
        private readonly ConcurrentDictionary<string, int> _consecutiveUnhealthy = new();

        private bool IsDataSourceHealthy(string dsName)
        {
            try
            {
                var ds = _dmeEditor.GetDataSource(dsName);
                return ProxyLivenessHelper.IsAlive(ds, _policy.HealthCheckTimeoutSecs * 1_000);
            }
            catch (Exception ex)
            {
                LogSafe($"Health check for {dsName} failed.", ex);
                return false;
            }
        }

        private void PerformHealthCheck(object sender, ElapsedEventArgs e)
        {
            foreach (var dsName in _dataSourceNames)
            {
                bool isHealthy = IsDataSourceHealthy(dsName);

                if (isHealthy)
                {
                    int count = _consecutiveHealthy.AddOrUpdate(dsName, 1, (_, v) => v + 1);
                    _consecutiveUnhealthy[dsName] = 0;

                    if (count >= _policy.HealthyThresholdCount)
                    {
                        bool wasUnhealthy = _healthStatus.TryGetValue(dsName, out var prev) && !prev;
                        _healthStatus[dsName] = true;
                        if (wasUnhealthy)
                            RaiseRecoveryEvent(dsName);
                    }
                }
                else
                {
                    int count = _consecutiveUnhealthy.AddOrUpdate(dsName, 1, (_, v) => v + 1);
                    _consecutiveHealthy[dsName] = 0;

                    if (count >= _policy.UnhealthyThresholdCount)
                    {
                        _healthStatus[dsName] = false;
                        RecordFailure(dsName);
                    }
                }
            }
        }

        // ── Failover ──────────────────────────────────────────────────

        private void Failover()
        {
            var originalIndex  = _currentIndex;
            var originalName   = _dataSourceNames[originalIndex];

            for (int i = 1; i <= _dataSourceNames.Count; i++)
            {
                int nextIndex  = (originalIndex + i) % _dataSourceNames.Count;
                var candidate  = _dataSourceNames[nextIndex];

                if (!IsHealthy(candidate) || IsCircuitOpen(candidate))
                {
                    LogSafe($"Skipping {candidate} — health or circuit issue.");
                    continue;
                }

                var ds = _dmeEditor.GetDataSource(candidate);
                if (ds != null && ds.Openconnection() == ConnectionState.Open)
                {
                    _currentIndex = nextIndex;
                    SetCurrentDataSource(nextIndex);
                    RecordSuccess(candidate, TimeSpan.Zero);
                    RaiseFailoverEvent(originalName, candidate, "Automatic failover");
                    LogSafe($"Failover successful: {originalName} → {candidate}.");
                    return;
                }

                LogSafe($"Failover attempt to {candidate} failed.");
                RecordFailure(candidate);
            }

            throw new Exception("Failover failed: no available data sources.");
        }

        // ── Metrics helpers ───────────────────────────────────────────

        private void RecordSuccess(string dsName, TimeSpan duration)
        {
            _circuitStateStore.RecordSuccess(dsName);

            var m = _metrics.GetOrAdd(dsName, _ => new DataSourceMetrics());
            m.IncrementTotalRequests();
            m.IncrementSuccessfulRequests();

            // Running average
            long successes = m.SuccessfulRequests;
            if (successes > 0)
                m.AverageResponseTime = ((m.AverageResponseTime * (successes - 1)) + duration.TotalMilliseconds) / successes;

            m.LastRequested  = DateTime.UtcNow;
            m.LastSuccessful = DateTime.UtcNow;
        }

        private void RecordFailure(string dsName, ProxyErrorSeverity severity = ProxyErrorSeverity.Medium)
        {
            _circuitStateStore.RecordFailure(dsName, severity);

            var m = _metrics.GetOrAdd(dsName, _ => new DataSourceMetrics());
            m.IncrementTotalRequests();
            m.IncrementFailedRequests();
            m.LastRequested = DateTime.UtcNow;
        }

        /// <summary>Records latency sample for SLO tracking (see Observability partial).</summary>
        partial void RecordLatency(string dsName, long elapsedMs);

        private bool IsCircuitOpen(string dsName)
            => !_circuitStateStore.CanExecute(dsName);

        private bool IsHealthy(string dsName)
            => _healthStatus.TryGetValue(dsName, out var h) && h;

        private void ResetFailureCount(string dsName)
        {
            _consecutiveUnhealthy[dsName] = 0;
            _circuitOpenTimes.TryRemove(dsName, out _);
        }

        // ── Data source management ────────────────────────────────────
        private IReadOnlyList<string> SelectWriteCandidates()
        {
            var primaries = _dataSourceNames
                .Where(n => _roles.GetValueOrDefault(n, ProxyDataSourceRole.Primary) == ProxyDataSourceRole.Primary
                         && IsHealthy(n) && !IsCircuitOpen(n))
                .ToList();
            if (primaries.Count == 0)
                primaries = _dataSourceNames.Where(n => IsHealthy(n) && !IsCircuitOpen(n)).ToList();
            if (primaries.Count == 0)
                primaries = _dataSourceNames.ToList();
            return primaries;
        }

        public void SetRole(string dsName, ProxyDataSourceRole role)
        {
            if (!string.IsNullOrWhiteSpace(dsName))
                _roles[dsName] = role;
        }
        public void AddDataSource(string dsName, int weight = 1)
        {
            if (string.IsNullOrWhiteSpace(dsName)) return;
            lock (_balancingLock)
            {
                if (_dataSourceNames.Contains(dsName)) return;
                _dataSourceNames.Add(dsName);
                _dataSourceWeights[dsName] = weight > 0 ? weight : 1;
                _healthStatus[dsName]      = false;
                _metrics.GetOrAdd(dsName, _ => new DataSourceMetrics());
                _connectionPools.GetOrAdd(dsName, _ => new ConcurrentQueue<PooledConnection>());
                _circuitStateStore.Initialize(
                    dsName,
                    _policy.Resilience.FailureThreshold,
                    _policy.Resilience.CircuitResetTimeout,
                    _policy.Resilience.ConsecutiveSuccessesToClose);
                _roles.TryAdd(dsName, ProxyDataSourceRole.Primary);
            }
        }

        public void RemoveDataSource(string dsName)
        {
            if (string.IsNullOrWhiteSpace(dsName)) return;
            lock (_balancingLock)
            {
                _dataSourceNames.Remove(dsName);
                _dataSourceWeights.TryRemove(dsName, out _);
                _healthStatus.TryRemove(dsName, out _);
                _metrics.TryRemove(dsName, out _);
                _connectionPools.TryRemove(dsName, out _);
                _activeConnections.TryRemove(dsName, out _);
                _circuitStateStore.Remove(dsName);
                _consecutiveHealthy.TryRemove(dsName, out _);
                _consecutiveUnhealthy.TryRemove(dsName, out _);
                _roles.TryRemove(dsName, out _);
            }
        }

        public IDataSource GetConnection(string dsName)
            => _activeConnections.GetOrAdd(dsName, name => _dmeEditor.GetDataSource(name));

        // ── Monitor / event helpers ───────────────────────────────────

        private void MonitorHealth()
        {
            Task.Run(() =>
            {
                while (!_disposed)
                {
                    foreach (var dsName in _dataSourceNames)
                    {
                        var wasHealthy = _healthStatus.TryGetValue(dsName, out var h) && h;
                        var isHealthy  = IsDataSourceHealthy(dsName);
                        if (wasHealthy != isHealthy)
                        {
                            _healthStatus[dsName] = isHealthy;
                            LogSafe($"Data source {dsName} is now {(isHealthy ? "healthy" : "unhealthy")}.");
                        }
                    }
                    Task.Delay(_policy.HealthCheckIntervalMs).Wait();
                }
            });
        }

        private void RaiseFailoverEvent(string fromDs, string toDs, string reason = null)
            => OnFailover?.Invoke(this, new FailoverEventArgs
            {
                FromDataSource = fromDs,
                ToDataSource   = toDs,
                Reason         = reason
            });

        private void RaiseRecoveryEvent(string dsName)
        {
            try { OnRecovery?.Invoke(this, new RecoveryEventArgs { DataSourceName = dsName, RecoveredAt = DateTime.UtcNow }); }
            catch { /* don't let handler exceptions break health-check loop */ }
        }
    }
}
