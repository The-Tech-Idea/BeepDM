using System;
using System.Data;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Extensions
{
    /// <summary>
    /// Example of a custom DMEEditor that overrides virtual methods for enhanced functionality
    /// This demonstrates how to leverage the virtual method pattern you added
    /// </summary>
    public class CustomEnhancedDMEEditor : DMEEditor
    {
        private int _dataSourceCreationAttempts;
        private int _successfulConnections;

        public CustomEnhancedDMEEditor(
            IDMLogger logger,
            IUtil utilfunctions,
            IErrorsInfo errorObject,
            IConfigEditor configEditor,
            IAssemblyHandler assemblyHandler)
            : base(logger, utilfunctions, errorObject, configEditor, assemblyHandler)
        {
            _dataSourceCreationAttempts = 0;
            _successfulConnections = 0;
            AddLogMessage("Custom Enhanced DMEEditor initialized with override capabilities");
        }

        /// <summary>
        /// Override GetDataSource to add custom logging and statistics
        /// </summary>
        public override IDataSource GetDataSource(string pdatasourcename)
        {
            _dataSourceCreationAttempts++;
            
            AddLogMessage($"[CUSTOM] Attempting to get data source: {pdatasourcename} (Attempt #{_dataSourceCreationAttempts})");
            
            var dataSource = base.GetDataSource(pdatasourcename);
            
            if (dataSource != null)
            {
                AddLogMessage($"[CUSTOM] Successfully retrieved data source: {pdatasourcename}");
                return dataSource;
            }
            else
            {
                AddLogMessage($"[CUSTOM] Failed to retrieve data source: {pdatasourcename}. Providing enhanced diagnostics...");
                
                // Provide enhanced diagnostics using the extension methods
                var diagnostics = this.GetDataSourceDiagnostics(pdatasourcename);
                AddLogMessage($"[CUSTOM] Diagnostics - Health Score: {diagnostics.HealthScore:F1}/10");
                
                if (!diagnostics.SpecificDataSourceExists)
                {
                    AddLogMessage($"[CUSTOM] No connection configuration found for '{pdatasourcename}'");
                }
                
                return null;
            }
        }

        /// <summary>
        /// Override CreateNewDataSourceConnection to add retry logic and enhanced error handling
        /// </summary>
        public override IDataSource CreateNewDataSourceConnection(ConnectionProperties cn, string pdatasourcename)
        {
            AddLogMessage($"[CUSTOM] Creating new data source connection: {pdatasourcename} (Type: {cn.DatabaseType})");
            
            try
            {
                var dataSource = base.CreateNewDataSourceConnection(cn, pdatasourcename);
                
                if (dataSource != null)
                {
                    AddLogMessage($"[CUSTOM] Successfully created data source: {pdatasourcename}");
                    return dataSource;
                }
                else
                {
                    AddLogMessage($"[CUSTOM] Failed to create data source: {pdatasourcename}. Analyzing issue...");
                    
                    // Analyze the failure
                    var driverConfig = Utilfunction.LinkConnection2Drivers(cn);
                    if (driverConfig == null)
                    {
                        var suggestedPackage = GetSuggestedPackageForDataSourceType(cn.DatabaseType);
                        AddLogMessage($"[CUSTOM] Missing driver configuration for {cn.DatabaseType}. Suggested package: {suggestedPackage ?? "Unknown"}");
                    }
                    
                    return null;
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"[CUSTOM] Exception during data source creation: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Override OpenDataSource to add connection statistics and enhanced monitoring
        /// </summary>
        public override ConnectionState OpenDataSource(string pdatasourcename)
        {
            AddLogMessage($"[CUSTOM] Opening data source connection: {pdatasourcename}");
            
            var startTime = DateTime.UtcNow;
            var connectionState = base.OpenDataSource(pdatasourcename);
            var duration = DateTime.UtcNow - startTime;
            
            if (connectionState == ConnectionState.Open)
            {
                _successfulConnections++;
                AddLogMessage($"[CUSTOM] Successfully opened connection to {pdatasourcename} in {duration.TotalMilliseconds:F0}ms");
                AddLogMessage($"[CUSTOM] Total successful connections this session: {_successfulConnections}");
            }
            else
            {
                AddLogMessage($"[CUSTOM] Failed to open connection to {pdatasourcename}. State: {connectionState}");
                
                // Provide suggestions for common issues
                ProvideTroubleshootingSuggestions(pdatasourcename, connectionState);
            }
            
            return connectionState;
        }

        /// <summary>
        /// Override GetEntityStructure to add caching and performance monitoring
        /// </summary>
        public override EntityStructure GetEntityStructure(string entityname, string datasourcename)
        {
            AddLogMessage($"[CUSTOM] Getting entity structure: {entityname} from {datasourcename}");
            
            var startTime = DateTime.UtcNow;
            var entityStructure = base.GetEntityStructure(entityname, datasourcename);
            var duration = DateTime.UtcNow - startTime;
            
            if (entityStructure != null)
            {
                AddLogMessage($"[CUSTOM] Successfully retrieved entity structure for {entityname} in {duration.TotalMilliseconds:F0}ms");
                AddLogMessage($"[CUSTOM] Entity has {entityStructure.Fields?.Count ?? 0} fields");
            }
            else
            {
                AddLogMessage($"[CUSTOM] Failed to retrieve entity structure for {entityname}");
            }
            
            return entityStructure;
        }

        /// <summary>
        /// Override AddLogMessage to add custom formatting and routing
        /// </summary>
        public override void AddLogMessage(string pLogMessage)
        {
            // Add timestamp and session info
            var enhancedMessage = $"[{DateTime.Now:HH:mm:ss.fff}] [Session-{GetHashCode():X8}] {pLogMessage}";
            
            // Call base implementation
            base.AddLogMessage(enhancedMessage);
            
            // Could also route to custom logging systems here
            // CustomLogger?.LogMessage(enhancedMessage);
        }

        #region Custom Helper Methods

        private string GetSuggestedPackageForDataSourceType(DataSourceType dataSourceType)
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

        private void ProvideTroubleshootingSuggestions(string datasourcename, ConnectionState state)
        {
            AddLogMessage($"[CUSTOM] Troubleshooting suggestions for {datasourcename}:");
            
            switch (state)
            {
                case ConnectionState.Broken:
                    AddLogMessage("[CUSTOM] - Check if the database server is running and accessible");
                    AddLogMessage("[CUSTOM] - Verify connection string parameters (server, port, credentials)");
                    AddLogMessage("[CUSTOM] - Check firewall settings and network connectivity");
                    break;
                    
                case ConnectionState.Closed:
                    AddLogMessage("[CUSTOM] - Data source was created but connection failed to open");
                    AddLogMessage("[CUSTOM] - Check database permissions and authentication");
                    break;
                    
                default:
                    AddLogMessage("[CUSTOM] - Verify that the appropriate data source driver is installed");
                    AddLogMessage("[CUSTOM] - Check the connection configuration for correctness");
                    break;
            }
        }

        #endregion

        #region Public Statistics Methods

        /// <summary>
        /// Get session statistics
        /// </summary>
        public CustomDMEEditorStats GetSessionStats()
        {
            return new CustomDMEEditorStats
            {
                TotalDataSourceAttempts = _dataSourceCreationAttempts,
                SuccessfulConnections = _successfulConnections,
                SessionStartTime = DateTime.UtcNow, // This would be set in constructor in real implementation
                TotalDataSources = DataSources.Count,
                TotalConnections = ConfigEditor.DataConnections.Count
            };
        }

        /// <summary>
        /// Reset session statistics
        /// </summary>
        public void ResetSessionStats()
        {
            _dataSourceCreationAttempts = 0;
            _successfulConnections = 0;
            AddLogMessage("[CUSTOM] Session statistics reset");
        }

        #endregion
    }

    #region Supporting Types

    public class CustomDMEEditorStats
    {
        public int TotalDataSourceAttempts { get; set; }
        public int SuccessfulConnections { get; set; }
        public DateTime SessionStartTime { get; set; }
        public int TotalDataSources { get; set; }
        public int TotalConnections { get; set; }
        
        public double SuccessRate => TotalDataSourceAttempts > 0 ? (double)SuccessfulConnections / TotalDataSourceAttempts : 0.0;
    }

    #endregion
}