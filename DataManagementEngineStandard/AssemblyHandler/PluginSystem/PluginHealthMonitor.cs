using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Manages health monitoring and resource tracking for plugins
    /// </summary>
    public class PluginHealthMonitor : IDisposable
    {
        private readonly ConcurrentDictionary<string, PluginHealthInfo> _pluginHealthInfo = new();
        private readonly ConcurrentDictionary<string, Timer> _healthCheckTimers = new();
        private readonly ConcurrentDictionary<string, PluginResourceUsage> _resourceUsage = new();
        private readonly ConcurrentDictionary<string, PluginResourceLimits> _resourceLimits = new();
        private readonly PluginLifecycleManager _lifecycleManager;
        private readonly IDMLogger _logger;
        private bool _disposed = false;

        // Events
        /// <summary>
        /// Raised when a plugin's computed health state changes.
        /// </summary>
        public event EventHandler<PluginHealthEventArgs> HealthStatusChanged;
        /// <summary>
        /// Raised when a plugin exceeds one or more configured resource limits.
        /// </summary>
        public event EventHandler<PluginResourceEventArgs> ResourceLimitExceeded;

        /// <summary>
        /// Initializes a monitor that tracks plugin health and resource usage.
        /// </summary>
        /// <param name="lifecycleManager">Lifecycle manager used to correlate monitored plugins with loaded plugin state.</param>
        /// <param name="logger">Logger used for monitoring diagnostics.</param>
        public PluginHealthMonitor(PluginLifecycleManager lifecycleManager, IDMLogger logger)
        {
            _lifecycleManager = lifecycleManager;
            _logger = logger;
        }

        /// <summary>
        /// Starts health monitoring for a plugin
        /// </summary>
        public void StartHealthMonitoring(string pluginId, TimeSpan interval)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return;

            try
            {
                // Stop existing monitoring if any
                StopHealthMonitoring(pluginId);

                // Initialize health info
                _pluginHealthInfo[pluginId] = new PluginHealthInfo
                {
                    PluginId = pluginId,
                    Status = PluginHealth.Healthy,
                    LastCheckTime = DateTime.UtcNow,
                    CheckInterval = interval,
                    IsMonitoring = true
                };

                // Create timer for health checks
                var timer = new Timer(async _ => await PerformHealthCheckAsync(pluginId), 
                                    null, TimeSpan.Zero, interval);
                
                _healthCheckTimers[pluginId] = timer;

                _logger?.LogWithContext($"Health monitoring started for plugin: {pluginId} (interval: {interval})", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to start health monitoring for plugin: {pluginId}", ex);
            }
        }

        /// <summary>
        /// Stops health monitoring for a plugin
        /// </summary>
        public void StopHealthMonitoring(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return;

            try
            {
                // Stop timer
                if (_healthCheckTimers.TryRemove(pluginId, out var timer))
                {
                    timer.Dispose();
                }

                // Update health info
                if (_pluginHealthInfo.TryGetValue(pluginId, out var healthInfo))
                {
                    healthInfo.IsMonitoring = false;
                }

                _logger?.LogWithContext($"Health monitoring stopped for plugin: {pluginId}", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to stop health monitoring for plugin: {pluginId}", ex);
            }
        }

        /// <summary>
        /// Gets health status for all plugins
        /// </summary>
        public Dictionary<string, PluginHealth> GetPluginHealthStatuses()
        {
            var statuses = new Dictionary<string, PluginHealth>();

            try
            {
                foreach (var kvp in _pluginHealthInfo)
                {
                    statuses[kvp.Key] = kvp.Value.Status;
                }

                // Include plugins from lifecycle manager that aren't being monitored
                var allPlugins = _lifecycleManager.GetPlugins();
                foreach (var plugin in allPlugins)
                {
                    if (!statuses.ContainsKey(plugin.Id))
                    {
                        statuses[plugin.Id] = plugin.Health;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext("Failed to get plugin health statuses", ex);
            }

            return statuses;
        }

        /// <summary>
        /// Gets detailed health information for a plugin
        /// </summary>
        public PluginHealthInfo GetPluginHealthInfo(string pluginId)
        {
            return _pluginHealthInfo.GetValueOrDefault(pluginId);
        }

        /// <summary>
        /// Sets resource limits for a plugin
        /// </summary>
        public bool SetPluginResourceLimits(string pluginId, Dictionary<string, object> limits)
        {
            if (string.IsNullOrWhiteSpace(pluginId) || limits == null)
                return false;

            try
            {
                var resourceLimits = new PluginResourceLimits
                {
                    PluginId = pluginId,
                    MaxMemoryMB = GetLimitValue<long>(limits, "MaxMemoryMB", -1),
                    MaxCpuPercent = GetLimitValue<double>(limits, "MaxCpuPercent", -1),
                    MaxThreads = GetLimitValue<int>(limits, "MaxThreads", -1),
                    MaxFileHandles = GetLimitValue<int>(limits, "MaxFileHandles", -1),
                    MaxNetworkConnections = GetLimitValue<int>(limits, "MaxNetworkConnections", -1)
                };

                _resourceLimits[pluginId] = resourceLimits;

                _logger?.LogWithContext($"Resource limits set for plugin: {pluginId}", resourceLimits);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to set resource limits for plugin: {pluginId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets resource usage for all plugins
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> GetPluginResourceUsage()
        {
            var usage = new Dictionary<string, Dictionary<string, object>>();

            try
            {
                foreach (var kvp in _resourceUsage)
                {
                    var pluginUsage = kvp.Value;
                    usage[kvp.Key] = new Dictionary<string, object>
                    {
                        ["MemoryMB"] = pluginUsage.MemoryMB,
                        ["CpuPercent"] = pluginUsage.CpuPercent,
                        ["ThreadCount"] = pluginUsage.ThreadCount,
                        ["FileHandles"] = pluginUsage.FileHandles,
                        ["NetworkConnections"] = pluginUsage.NetworkConnections,
                        ["LastUpdated"] = pluginUsage.LastUpdated
                    };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext("Failed to get plugin resource usage", ex);
            }

            return usage;
        }

        /// <summary>
        /// Gets performance metrics for a plugin
        /// </summary>
        public Dictionary<string, object> GetPluginMetrics(string pluginId)
        {
            var metrics = new Dictionary<string, object>();

            try
            {
                // Get health info
                if (_pluginHealthInfo.TryGetValue(pluginId, out var healthInfo))
                {
                    metrics["HealthStatus"] = healthInfo.Status.ToString();
                    metrics["LastHealthCheck"] = healthInfo.LastCheckTime;
                    metrics["IsMonitoring"] = healthInfo.IsMonitoring;
                    metrics["CheckInterval"] = healthInfo.CheckInterval;
                    metrics["ErrorCount"] = healthInfo.ErrorCount;
                    metrics["WarningCount"] = healthInfo.WarningCount;
                }

                // Get resource usage
                if (_resourceUsage.TryGetValue(pluginId, out var usage))
                {
                    metrics["MemoryUsageMB"] = usage.MemoryMB;
                    metrics["CpuUsagePercent"] = usage.CpuPercent;
                    metrics["ThreadCount"] = usage.ThreadCount;
                    metrics["FileHandles"] = usage.FileHandles;
                    metrics["NetworkConnections"] = usage.NetworkConnections;
                }

                // Get resource limits
                if (_resourceLimits.TryGetValue(pluginId, out var limits))
                {
                    metrics["MaxMemoryMB"] = limits.MaxMemoryMB;
                    metrics["MaxCpuPercent"] = limits.MaxCpuPercent;
                    metrics["MaxThreads"] = limits.MaxThreads;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to get plugin metrics: {pluginId}", ex);
            }

            return metrics;
        }

        /// <summary>
        /// Forces an immediate health check for a plugin
        /// </summary>
        public async Task<PluginHealth> CheckPluginHealthAsync(string pluginId)
        {
            return await PerformHealthCheckAsync(pluginId);
        }

        // Private helper methods
        private async Task<PluginHealth> PerformHealthCheckAsync(string pluginId)
        {
            try
            {
                var plugin = _lifecycleManager.GetPlugin(pluginId);
                if (plugin == null)
                {
                    UpdateHealthStatus(pluginId, PluginHealth.Unknown, "Plugin not found");
                    return PluginHealth.Unknown;
                }

                // Update resource usage
                await UpdateResourceUsageAsync(pluginId);

                // Check resource limits
                CheckResourceLimits(pluginId);

                // Get health from plugin if it implements IModernPlugin
                var health = _lifecycleManager.CheckPluginHealth(pluginId);

                // Update health info
                var healthInfo = _pluginHealthInfo.GetOrAdd(pluginId, new PluginHealthInfo { PluginId = pluginId });
                
                var previousHealth = healthInfo.Status;
                healthInfo.Status = health;
                healthInfo.LastCheckTime = DateTime.UtcNow;

                // Fire event if health status changed
                if (previousHealth != health)
                {
                    HealthStatusChanged?.Invoke(this, new PluginHealthEventArgs
                    {
                        PluginId = pluginId,
                        PreviousHealth = previousHealth,
                        CurrentHealth = health,
                        Timestamp = DateTime.UtcNow
                    });
                }

                return health;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Health check failed for plugin: {pluginId}", ex);
                UpdateHealthStatus(pluginId, PluginHealth.Critical, $"Health check failed: {ex.Message}");
                return PluginHealth.Critical;
            }
        }

        private async Task UpdateResourceUsageAsync(string pluginId)
        {
            try
            {
                // TODO: Implement actual resource monitoring
                // For now, simulate resource usage
                var usage = new PluginResourceUsage
                {
                    PluginId = pluginId,
                    MemoryMB = GetRandomValue(10, 100),
                    CpuPercent = GetRandomValue(0, 25),
                    ThreadCount = (int)GetRandomValue(1, 10),
                    FileHandles = (int)GetRandomValue(5, 50),
                    NetworkConnections = (int)GetRandomValue(0, 5),
                    LastUpdated = DateTime.UtcNow
                };

                _resourceUsage[pluginId] = usage;

                await Task.CompletedTask; // Placeholder for async operations
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to update resource usage for plugin: {pluginId}", ex);
            }
        }

        private void CheckResourceLimits(string pluginId)
        {
            try
            {
                if (!_resourceLimits.TryGetValue(pluginId, out var limits) ||
                    !_resourceUsage.TryGetValue(pluginId, out var usage))
                {
                    return;
                }

                var violations = new List<string>();

                if (limits.MaxMemoryMB > 0 && usage.MemoryMB > limits.MaxMemoryMB)
                {
                    violations.Add($"Memory usage ({usage.MemoryMB} MB) exceeds limit ({limits.MaxMemoryMB} MB)");
                }

                if (limits.MaxCpuPercent > 0 && usage.CpuPercent > limits.MaxCpuPercent)
                {
                    violations.Add($"CPU usage ({usage.CpuPercent}%) exceeds limit ({limits.MaxCpuPercent}%)");
                }

                if (limits.MaxThreads > 0 && usage.ThreadCount > limits.MaxThreads)
                {
                    violations.Add($"Thread count ({usage.ThreadCount}) exceeds limit ({limits.MaxThreads})");
                }

                if (violations.Any())
                {
                    UpdateHealthStatus(pluginId, PluginHealth.Warning, string.Join("; ", violations));
                    
                    ResourceLimitExceeded?.Invoke(this, new PluginResourceEventArgs
                    {
                        PluginId = pluginId,
                        Violations = violations,
                        Usage = usage,
                        Limits = limits,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to check resource limits for plugin: {pluginId}", ex);
            }
        }

        private void UpdateHealthStatus(string pluginId, PluginHealth status, string message = null)
        {
            var healthInfo = _pluginHealthInfo.GetOrAdd(pluginId, new PluginHealthInfo { PluginId = pluginId });
            
            var previousHealth = healthInfo.Status;
            healthInfo.Status = status;
            healthInfo.LastCheckTime = DateTime.UtcNow;
            
            if (!string.IsNullOrWhiteSpace(message))
            {
                healthInfo.LastMessage = message;
                
                if (status == PluginHealth.Critical)
                    healthInfo.ErrorCount++;
                else if (status == PluginHealth.Warning)
                    healthInfo.WarningCount++;
            }

            // Fire event if status changed
            if (previousHealth != status)
            {
                HealthStatusChanged?.Invoke(this, new PluginHealthEventArgs
                {
                    PluginId = pluginId,
                    PreviousHealth = previousHealth,
                    CurrentHealth = status,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        private T GetLimitValue<T>(Dictionary<string, object> limits, string key, T defaultValue)
        {
            if (limits.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        private static readonly Random _random = new Random();
        private double GetRandomValue(double min, double max)
        {
            return min + _random.NextDouble() * (max - min);
        }

        /// <summary>
        /// Stops all monitoring timers and clears cached health and resource state.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // Stop all timers
                foreach (var timer in _healthCheckTimers.Values)
                {
                    timer.Dispose();
                }

                _healthCheckTimers.Clear();
                _pluginHealthInfo.Clear();
                _resourceUsage.Clear();
                _resourceLimits.Clear();

                _disposed = true;
            }
        }
    }

    // Supporting classes
    /// <summary>
    /// Snapshot of the current monitoring state for a plugin.
    /// </summary>
    public class PluginHealthInfo
    {
        /// <summary>Gets or sets the plugin identifier.</summary>
        public string PluginId { get; set; }
        /// <summary>Gets or sets the latest evaluated health state.</summary>
        public PluginHealth Status { get; set; } = PluginHealth.Unknown;
        /// <summary>Gets or sets the timestamp of the last completed health check.</summary>
        public DateTime LastCheckTime { get; set; }
        /// <summary>Gets or sets the configured interval between health checks.</summary>
        public TimeSpan CheckInterval { get; set; }
        /// <summary>Gets or sets whether active monitoring is currently enabled.</summary>
        public bool IsMonitoring { get; set; }
        /// <summary>Gets or sets the last status or diagnostic message captured for the plugin.</summary>
        public string LastMessage { get; set; }
        /// <summary>Gets or sets the cumulative error count observed while monitoring the plugin.</summary>
        public int ErrorCount { get; set; }
        /// <summary>Gets or sets the cumulative warning count observed while monitoring the plugin.</summary>
        public int WarningCount { get; set; }
    }

    /// <summary>
    /// Captures the latest measured resource usage for a plugin.
    /// </summary>
    public class PluginResourceUsage
    {
        /// <summary>Gets or sets the plugin identifier.</summary>
        public string PluginId { get; set; }
        /// <summary>Gets or sets the current memory footprint in megabytes.</summary>
        public double MemoryMB { get; set; }
        /// <summary>Gets or sets the current CPU usage percentage.</summary>
        public double CpuPercent { get; set; }
        /// <summary>Gets or sets the current number of threads owned by the plugin process.</summary>
        public int ThreadCount { get; set; }
        /// <summary>Gets or sets the current number of open file handles.</summary>
        public int FileHandles { get; set; }
        /// <summary>Gets or sets the current number of tracked network connections.</summary>
        public int NetworkConnections { get; set; }
        /// <summary>Gets or sets the timestamp when the usage sample was captured.</summary>
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Stores configured resource ceilings for a plugin.
    /// </summary>
    public class PluginResourceLimits
    {
        /// <summary>Gets or sets the plugin identifier.</summary>
        public string PluginId { get; set; }
        /// <summary>Gets or sets the maximum allowed memory footprint in megabytes, or <c>-1</c> when unlimited.</summary>
        public long MaxMemoryMB { get; set; } = -1; // -1 means no limit
        /// <summary>Gets or sets the maximum allowed CPU usage percentage, or <c>-1</c> when unlimited.</summary>
        public double MaxCpuPercent { get; set; } = -1;
        /// <summary>Gets or sets the maximum allowed thread count, or <c>-1</c> when unlimited.</summary>
        public int MaxThreads { get; set; } = -1;
        /// <summary>Gets or sets the maximum allowed file handle count, or <c>-1</c> when unlimited.</summary>
        public int MaxFileHandles { get; set; } = -1;
        /// <summary>Gets or sets the maximum allowed network connection count, or <c>-1</c> when unlimited.</summary>
        public int MaxNetworkConnections { get; set; } = -1;
    }

    /// <summary>
    /// Event payload describing a plugin health-state transition.
    /// </summary>
    public class PluginHealthEventArgs : EventArgs
    {
        /// <summary>Gets or sets the plugin identifier.</summary>
        public string PluginId { get; set; }
        /// <summary>Gets or sets the health state before the transition.</summary>
        public PluginHealth PreviousHealth { get; set; }
        /// <summary>Gets or sets the health state after the transition.</summary>
        public PluginHealth CurrentHealth { get; set; }
        /// <summary>Gets or sets the message describing the transition.</summary>
        public string Message { get; set; }
        /// <summary>Gets or sets when the transition was observed.</summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Event payload describing a resource-limit violation for a plugin.
    /// </summary>
    public class PluginResourceEventArgs : EventArgs
    {
        /// <summary>Gets or sets the plugin identifier.</summary>
        public string PluginId { get; set; }
        /// <summary>Gets or sets the list of limit violations detected for the plugin.</summary>
        public List<string> Violations { get; set; }
        /// <summary>Gets or sets the resource usage sample that triggered the violation.</summary>
        public PluginResourceUsage Usage { get; set; }
        /// <summary>Gets or sets the configured limits evaluated against the sample.</summary>
        public PluginResourceLimits Limits { get; set; }
        /// <summary>Gets or sets when the violation was observed.</summary>
        public DateTime Timestamp { get; set; }
    }
}