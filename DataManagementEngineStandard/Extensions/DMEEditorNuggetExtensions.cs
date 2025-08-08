using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;
using System.Data;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// DMEEditor extension for enhanced package management capabilities
    /// Provides extension methods that work with existing virtual methods
    /// </summary>
    public static class DMEEditorNuggetExtensions
    {
        private static readonly Dictionary<IDMEEditor, object> _extensionData = new();

        /// <summary>
        /// Enhanced data source creation with better error handling
        /// </summary>
        public static async Task<IDataSource> CreateEnhancedDataSourceConnectionAsync(
            this IDMEEditor dmeEditor,
            ConnectionProperties connectionProperties,
            string datasourceName,
            bool retryOnFailure = true)
        {
            try
            {
                // First, try the standard creation (leverages virtual method)
                var dataSource = dmeEditor.CreateNewDataSourceConnection(connectionProperties, datasourceName);
                
                if (dataSource != null)
                {
                    return dataSource;
                }

                if (retryOnFailure)
                {
                    // If standard creation failed, log and suggest package installation
                    dmeEditor.AddLogMessage($"Standard creation failed for {datasourceName}. Consider installing the appropriate data source package.");
                    
                    // Attempt to find if there's a missing driver configuration
                    var missingDriverType = connectionProperties.DatabaseType.ToString();
                    dmeEditor.AddLogMessage($"Missing driver for type: {missingDriverType}");
                }

                return null;
            }
            catch (Exception ex)
            {
                dmeEditor.AddLogMessage($"Enhanced data source creation failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Enhanced data source retrieval with better diagnostics
        /// </summary>
        public static IDataSource GetEnhancedDataSource(
            this IDMEEditor dmeEditor,
            string datasourceName,
            bool includeDiagnostics = true)
        {
            try
            {
                // Try standard retrieval first (leverages virtual method)
                var dataSource = dmeEditor.GetDataSource(datasourceName);
                
                if (dataSource != null)
                {
                    return dataSource;
                }

                if (includeDiagnostics)
                {
                    // Provide diagnostic information
                    var connectionConfig = dmeEditor.ConfigEditor.DataConnections
                        .FirstOrDefault(c => c.ConnectionName.Equals(datasourceName, StringComparison.OrdinalIgnoreCase));
                    
                    if (connectionConfig == null)
                    {
                        dmeEditor.AddLogMessage($"No connection configuration found for '{datasourceName}'");
                    }
                    else
                    {
                        dmeEditor.AddLogMessage($"Connection config found but data source creation failed for '{datasourceName}' (Type: {connectionConfig.DatabaseType})");
                        
                        // Check if driver class exists
                        var driverConfig = dmeEditor.Utilfunction.LinkConnection2Drivers(connectionConfig);
                        if (driverConfig == null)
                        {
                            dmeEditor.AddLogMessage($"No driver configuration found for database type: {connectionConfig.DatabaseType}");
                        }
                        else
                        {
                            var classExists = dmeEditor.ConfigEditor.DataSourcesClasses
                                .Any(cls => cls.className == driverConfig.classHandler);
                            
                            if (!classExists)
                            {
                                dmeEditor.AddLogMessage($"Driver class '{driverConfig.classHandler}' not found in loaded assemblies");
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                dmeEditor.AddLogMessage($"Enhanced data source retrieval failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Enhanced connection opening with automatic diagnostics
        /// </summary>
        public static ConnectionState OpenEnhancedDataSource(
            this IDMEEditor dmeEditor,
            string datasourceName,
            bool includeDiagnostics = true)
        {
            try
            {
                // Try standard opening (leverages virtual method)
                var connectionState = dmeEditor.OpenDataSource(datasourceName);
                
                if (connectionState == ConnectionState.Open)
                {
                    return connectionState;
                }

                if (includeDiagnostics)
                {
                    // Provide diagnostic information for failed connections
                    dmeEditor.AddLogMessage($"Failed to open data source '{datasourceName}', attempting enhanced diagnostics");
                    
                    var dataSource = dmeEditor.GetEnhancedDataSource(datasourceName, true);
                    if (dataSource == null)
                    {
                        dmeEditor.AddLogMessage($"Data source '{datasourceName}' could not be created");
                    }
                    else
                    {
                        dmeEditor.AddLogMessage($"Data source '{datasourceName}' created but connection failed");
                    }
                }

                return ConnectionState.Broken;
            }
            catch (Exception ex)
            {
                dmeEditor.AddLogMessage($"Enhanced connection opening failed: {ex.Message}");
                return ConnectionState.Broken;
            }
        }

        /// <summary>
        /// Get comprehensive data source diagnostics
        /// </summary>
        public static DataSourceDiagnostics GetDataSourceDiagnostics(this IDMEEditor dmeEditor, string datasourceName = null)
        {
            var diagnostics = new DataSourceDiagnostics();
            
            try
            {
                // Overall statistics
                diagnostics.TotalConnections = dmeEditor.ConfigEditor.DataConnections.Count;
                diagnostics.TotalDataSources = dmeEditor.DataSources.Count;
                diagnostics.LoadedDriverClasses = dmeEditor.ConfigEditor.DataSourcesClasses.Count;
                diagnostics.LoadedDriverConfigs = dmeEditor.ConfigEditor.DataDriversClasses.Count;

                // Connection state analysis
                foreach (var ds in dmeEditor.DataSources)
                {
                    switch (ds.ConnectionStatus)
                    {
                        case ConnectionState.Open:
                            diagnostics.OpenConnections++;
                            break;
                        case ConnectionState.Closed:
                            diagnostics.ClosedConnections++;
                            break;
                        case ConnectionState.Broken:
                            diagnostics.BrokenConnections++;
                            break;
                    }
                }

                // Driver type analysis
                var driverTypes = dmeEditor.ConfigEditor.DataConnections
                    .GroupBy(c => c.DatabaseType)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count());
                
                diagnostics.DriverTypeDistribution = driverTypes;

                // Missing driver analysis
                foreach (var connection in dmeEditor.ConfigEditor.DataConnections)
                {
                    var driverConfig = dmeEditor.Utilfunction.LinkConnection2Drivers(connection);
                    if (driverConfig == null)
                    {
                        diagnostics.MissingDriverConfigs.Add(connection.ConnectionName);
                    }
                    else
                    {
                        var classExists = dmeEditor.ConfigEditor.DataSourcesClasses
                            .Any(cls => cls.className == driverConfig.classHandler);
                        
                        if (!classExists)
                        {
                            diagnostics.MissingDriverClasses.Add($"{connection.ConnectionName} -> {driverConfig.classHandler}");
                        }
                    }
                }

                // Specific datasource analysis if requested
                if (!string.IsNullOrEmpty(datasourceName))
                {
                    diagnostics.SpecificDataSourceName = datasourceName;
                    
                    var connection = dmeEditor.ConfigEditor.DataConnections
                        .FirstOrDefault(c => c.ConnectionName.Equals(datasourceName, StringComparison.OrdinalIgnoreCase));
                    
                    if (connection != null)
                    {
                        diagnostics.SpecificDataSourceExists = true;
                        diagnostics.SpecificDataSourceType = connection.DatabaseType.ToString();
                        
                        var dataSource = dmeEditor.DataSources
                            .FirstOrDefault(ds => ds.DatasourceName.Equals(datasourceName, StringComparison.OrdinalIgnoreCase));
                        
                        if (dataSource != null)
                        {
                            diagnostics.SpecificDataSourceLoaded = true;
                            diagnostics.SpecificDataSourceStatus = dataSource.ConnectionStatus.ToString();
                        }
                    }
                }

                diagnostics.HealthScore = CalculateHealthScore(diagnostics);
                
                dmeEditor.AddLogMessage($"Data source diagnostics completed. Health Score: {diagnostics.HealthScore:F1}/10");
            }
            catch (Exception ex)
            {
                diagnostics.Errors.Add($"Diagnostics failed: {ex.Message}");
                dmeEditor.AddLogMessage($"Data source diagnostics failed: {ex.Message}");
            }

            return diagnostics;
        }

        /// <summary>
        /// Enhanced bulk connection testing
        /// </summary>
        public static async Task<ConnectionTestResults> TestAllConnectionsAsync(this IDMEEditor dmeEditor)
        {
            var results = new ConnectionTestResults();
            
            foreach (var connection in dmeEditor.ConfigEditor.DataConnections)
            {
                var testResult = new ConnectionTestResult
                {
                    ConnectionName = connection.ConnectionName,
                    DatabaseType = connection.DatabaseType.ToString(),
                    TestStartTime = DateTime.UtcNow
                };

                try
                {
                    var dataSource = dmeEditor.CreateNewDataSourceConnection(connection, connection.ConnectionName);
                    
                    if (dataSource != null)
                    {
                        var connectionState = dataSource.Openconnection();
                        testResult.Success = connectionState == ConnectionState.Open;
                        testResult.ConnectionState = connectionState.ToString();
                        
                        if (testResult.Success)
                        {
                            results.SuccessfulConnections++;
                            
                            // Try to get entity count for additional validation
                            try
                            {
                                testResult.EntityCount = dataSource.Entities?.Count ?? 0;
                            }
                            catch
                            {
                                testResult.Notes = "Connected but entity enumeration failed";
                            }
                            
                            dataSource.Closeconnection();
                        }
                        else
                        {
                            results.FailedConnections++;
                        }
                    }
                    else
                    {
                        testResult.Success = false;
                        testResult.ErrorMessage = "Data source creation failed";
                        results.FailedConnections++;
                    }
                }
                catch (Exception ex)
                {
                    testResult.Success = false;
                    testResult.ErrorMessage = ex.Message;
                    results.FailedConnections++;
                }
                
                testResult.TestEndTime = DateTime.UtcNow;
                testResult.TestDuration = testResult.TestEndTime - testResult.TestStartTime;
                results.TestResults.Add(testResult);
            }

            results.TotalConnections = dmeEditor.ConfigEditor.DataConnections.Count;
            results.TestCompleted = DateTime.UtcNow;
            
            dmeEditor.AddLogMessage($"Connection testing completed. {results.SuccessfulConnections}/{results.TotalConnections} connections successful");
            
            return results;
        }

        /// <summary>
        /// Enhanced logging with context
        /// </summary>
        public static void AddEnhancedLogMessage(this IDMEEditor dmeEditor, string message, string context = null, LogLevel level = LogLevel.Info)
        {
            var enhancedMessage = level switch
            {
                LogLevel.Error => $"? [ERROR]",
                LogLevel.Warning => $"?? [WARNING]",
                LogLevel.Info => $"?? [INFO]",
                LogLevel.Debug => $"?? [DEBUG]",
                _ => $"?? [LOG]"
            };

            if (!string.IsNullOrEmpty(context))
            {
                enhancedMessage += $" [{context}]";
            }

            enhancedMessage += $" {message}";

            if (level == LogLevel.Error)
            {
                dmeEditor.AddLogMessage("Error", enhancedMessage, DateTime.Now, 0, context, Errors.Failed);
            }
            else
            {
                dmeEditor.AddLogMessage(enhancedMessage);
            }
        }

        /// <summary>
        /// Get suggested package names for missing data sources
        /// </summary>
        public static List<string> GetSuggestedPackages(this IDMEEditor dmeEditor)
        {
            var suggestions = new List<string>();
            var diagnostics = dmeEditor.GetDataSourceDiagnostics();

            foreach (var missingConfig in diagnostics.MissingDriverConfigs)
            {
                var connection = dmeEditor.ConfigEditor.DataConnections
                    .FirstOrDefault(c => c.ConnectionName == missingConfig);
                
                if (connection != null)
                {
                    var suggestedPackage = InferPackageIdFromDataSourceType(connection.DatabaseType);
                    if (!string.IsNullOrEmpty(suggestedPackage))
                    {
                        suggestions.Add(suggestedPackage);
                    }
                }
            }

            return suggestions.Distinct().ToList();
        }

        /// <summary>
        /// Store extension data for this DMEEditor instance
        /// </summary>
        public static void SetExtensionData(this IDMEEditor dmeEditor, string key, object value)
        {
            if (!_extensionData.ContainsKey(dmeEditor))
            {
                _extensionData[dmeEditor] = new Dictionary<string, object>();
            }

            var data = _extensionData[dmeEditor] as Dictionary<string, object>;
            data[key] = value;
        }

        /// <summary>
        /// Get extension data for this DMEEditor instance
        /// </summary>
        public static T GetExtensionData<T>(this IDMEEditor dmeEditor, string key, T defaultValue = default(T))
        {
            if (_extensionData.TryGetValue(dmeEditor, out var instanceData))
            {
                var data = instanceData as Dictionary<string, object>;
                if (data.TryGetValue(key, out var value) && value is T typedValue)
                {
                    return typedValue;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Cleanup extension resources when DMEEditor is disposed
        /// This method is called from the partial class OnDisposing method
        /// </summary>
        public static void CleanupNuggetExtensions(this IDMEEditor dmeEditor)
        {
            try
            {
                // Remove any stored extension data for this instance
                if (_extensionData.ContainsKey(dmeEditor))
                {
                    _extensionData.Remove(dmeEditor);
                }

                // Log cleanup if possible
                try
                {
                    dmeEditor.AddLogMessage("DMEEditor nugget extensions data cleaned up");
                }
                catch
                {
                    // Ignore logging errors during cleanup
                }
            }
            catch (Exception ex)
            {
                // Fallback logging if cleanup fails
                Console.WriteLine($"Warning: Error during nugget extension cleanup: {ex.Message}");
            }
        }

        #region Private Helper Methods

        private static double CalculateHealthScore(DataSourceDiagnostics diagnostics)
        {
            double score = 10.0;
            
            // Deduct for missing configurations
            score -= diagnostics.MissingDriverConfigs.Count * 0.5;
            score -= diagnostics.MissingDriverClasses.Count * 1.0;
            score -= diagnostics.Errors.Count * 2.0;
            
            // Deduct for broken connections
            if (diagnostics.TotalDataSources > 0)
            {
                double brokenRatio = (double)diagnostics.BrokenConnections / diagnostics.TotalDataSources;
                score -= brokenRatio * 3.0;
            }

            return Math.Max(0, Math.Min(10, score));
        }

        private static string InferPackageIdFromDataSourceType(DataSourceType dataSourceType)
        {
            return dataSourceType switch
            {
                DataSourceType.SqlServer => "TheTechIdea.Beep.DataSource.SqlServer",
                DataSourceType.Mysql => "TheTechIdea.Beep.DataSource.MySQL",
                DataSourceType.Postgre => "TheTechIdea.Beep.DataSource.PostgreSQL",
                DataSourceType.MongoDB => "TheTechIdea.Beep.DataSource.MongoDB",
                DataSourceType.Redis => "TheTechIdea.Beep.DataSource.Redis",
                DataSourceType.Oracle => "TheTechIdea.Beep.DataSource.Oracle",
                _ => null
            };
        }

        #endregion
    }

    #region Supporting Types

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public class DataSourceDiagnostics
    {
        public int TotalConnections { get; set; }
        public int TotalDataSources { get; set; }
        public int LoadedDriverClasses { get; set; }
        public int LoadedDriverConfigs { get; set; }
        
        public int OpenConnections { get; set; }
        public int ClosedConnections { get; set; }
        public int BrokenConnections { get; set; }
        
        public Dictionary<string, int> DriverTypeDistribution { get; set; } = new();
        public List<string> MissingDriverConfigs { get; set; } = new();
        public List<string> MissingDriverClasses { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        
        public string SpecificDataSourceName { get; set; }
        public bool SpecificDataSourceExists { get; set; }
        public string SpecificDataSourceType { get; set; }
        public bool SpecificDataSourceLoaded { get; set; }
        public string SpecificDataSourceStatus { get; set; }
        
        public double HealthScore { get; set; }
    }

    public class ConnectionTestResults
    {
        public DateTime TestCompleted { get; set; }
        public int TotalConnections { get; set; }
        public int SuccessfulConnections { get; set; }
        public int FailedConnections { get; set; }
        public List<ConnectionTestResult> TestResults { get; set; } = new();
        
        public double SuccessRate => TotalConnections > 0 ? (double)SuccessfulConnections / TotalConnections : 0.0;
    }

    public class ConnectionTestResult
    {
        public string ConnectionName { get; set; }
        public string DatabaseType { get; set; }
        public bool Success { get; set; }
        public string ConnectionState { get; set; }
        public string ErrorMessage { get; set; }
        public string Notes { get; set; }
        public int EntityCount { get; set; }
        public DateTime TestStartTime { get; set; }
        public DateTime TestEndTime { get; set; }
        public TimeSpan TestDuration { get; set; }
    }

    #endregion
}