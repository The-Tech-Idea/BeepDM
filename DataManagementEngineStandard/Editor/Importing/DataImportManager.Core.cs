using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor.Importing
{
    /// <summary>
    /// Core functionality partial class for DataImportManager
    /// Contains data source operations, entity management, and data fetching logic
    /// </summary>
    public partial class DataImportManager
    {
        #region Data Source Operations

        /// <summary>
        /// Initializes data sources if not already configured
        /// </summary>
        /// <param name="config">Import configuration</param>
        /// <returns>Task for async operation</returns>
        protected async Task InitializeDataSources(DataImportConfiguration config)
        {
            await Task.Run(() =>
            {
                // Initialize source data source if not set
                if (config.SourceData == null && !string.IsNullOrEmpty(config.SourceDataSourceName))
                {
                    config.SourceData = _editor.GetDataSource(config.SourceDataSourceName);
                    if (config.SourceData?.ConnectionStatus != ConnectionState.Open)
                    {
                        _editor.OpenDataSource(config.SourceDataSourceName);
                    }
                }

                // Initialize destination data source if not set
                if (config.DestData == null && !string.IsNullOrEmpty(config.DestDataSourceName))
                {
                    config.DestData = _editor.GetDataSource(config.DestDataSourceName);
                    if (config.DestData?.ConnectionStatus != ConnectionState.Open)
                    {
                        _editor.OpenDataSource(config.DestDataSourceName);
                    }
                }

                // Load entity structures if not set
                if (config.SourceEntityStructure == null && config.SourceData != null && !string.IsNullOrEmpty(config.SourceEntityName))
                {
                    config.SourceEntityStructure = config.SourceData.GetEntityStructure(config.SourceEntityName, false);
                }

                if (config.DestEntityStructure == null && config.DestData != null && !string.IsNullOrEmpty(config.DestEntityName))
                {
                    config.DestEntityStructure = config.DestData.GetEntityStructure(config.DestEntityName, false);
                }

                // Load default values if not set and defaults should be applied
                if (config.ApplyDefaults && (config.DefaultValues == null || !config.DefaultValues.Any()) && 
                    !string.IsNullOrEmpty(config.DestDataSourceName))
                {
                    try
                    {
                        config.DefaultValues = DefaultsManager.GetDefaults(_editor, config.DestDataSourceName);
                        _progressHelper.LogImport($"Loaded {config.DefaultValues?.Count ?? 0} default values from DefaultsManager", 0);
                    }
                    catch (Exception ex)
                    {
                        _progressHelper.LogError("Error loading default values", ex);
                        config.DefaultValues = new List<DefaultValue>();
                    }
                }
            });
        }

        /// <summary>
        /// Ensures the destination entity exists, creating it if necessary
        /// </summary>
        /// <param name="config">Import configuration</param>
        /// <returns>Task for async operation</returns>
        protected async Task EnsureDestinationEntityExists(DataImportConfiguration config)
        {
            if (config.DestData == null)
            {
                throw new InvalidOperationException("Destination data source is not initialized");
            }

            if (!config.CreateDestinationIfNotExists)
            {
                return; // Skip creation if not configured
            }

            if (!config.DestData.CheckEntityExist(config.DestEntityName))
            {
                _progressHelper.LogImport($"Destination entity '{config.DestEntityName}' does not exist. Creating it...", 0);

                // Ensure source entity structure is loaded
                if (config.SourceEntityStructure == null)
                {
                    config.SourceEntityStructure = await LoadSourceEntityStructure(config);
                    if (config.SourceEntityStructure == null)
                    {
                        throw new InvalidOperationException($"Source entity structure could not be loaded for '{config.SourceEntityName}'");
                    }
                }

                // Create the destination entity
                var creationSuccess = await Task.Run(() => 
                {
                    try
                    {
                        return config.DestData.CreateEntityAs(config.SourceEntityStructure);
                    }
                    catch (Exception ex)
                    {
                        _progressHelper.LogError($"Error creating entity '{config.DestEntityName}'", ex);
                        return false;
                    }
                });

                if (creationSuccess)
                {
                    _progressHelper.LogImport($"Successfully created destination entity '{config.DestEntityName}'", 0);
                    
                    // Reload the destination entity structure after creation
                    config.DestEntityStructure = config.DestData.GetEntityStructure(config.DestEntityName, true);
                }
                else
                {
                    throw new Exception($"Failed to create destination entity '{config.DestEntityName}'");
                }
            }
            else
            {
                _progressHelper.LogImport($"Destination entity '{config.DestEntityName}' already exists", 0);
                
                // Ensure we have the current structure
                if (config.DestEntityStructure == null)
                {
                    config.DestEntityStructure = config.DestData.GetEntityStructure(config.DestEntityName, false);
                }
            }
        }

        /// <summary>
        /// Loads the source entity structure
        /// </summary>
        /// <param name="config">Import configuration</param>
        /// <returns>Source entity structure</returns>
        protected async Task<EntityStructure> LoadSourceEntityStructure(DataImportConfiguration config)
        {
            return await Task.Run(() =>
            {
                if (config.SourceData == null)
                {
                    config.SourceData = _editor.GetDataSource(config.SourceDataSourceName);
                }

                if (config.SourceData == null || !config.SourceData.CheckEntityExist(config.SourceEntityName))
                {
                    throw new InvalidOperationException($"Source entity '{config.SourceEntityName}' does not exist in source data source '{config.SourceDataSourceName}'");
                }

                return config.SourceData.GetEntityStructure(config.SourceEntityName, false);
            });
        }

        #endregion

        #region Data Fetching Operations

        /// <summary>
        /// Fetches source data based on configuration
        /// </summary>
        /// <param name="config">Import configuration</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Source data enumerable</returns>
        protected async Task<IEnumerable<object>> FetchSourceDataAsync(DataImportConfiguration config, CancellationToken token)
        {
            if (config.SourceData == null)
            {
                throw new InvalidOperationException("Source data source is not initialized");
            }

            try
            {
                _progressHelper.LogImport("Fetching source data...", 0);

                var result = await Task.Run(() => 
                {
                    try
                    {
                        return config.SourceData.GetEntity(config.SourceEntityName, config.SourceFilters ?? new List<AppFilter>());
                    }
                    catch (Exception ex)
                    {
                        _progressHelper.LogError("Error fetching source data", ex);
                        throw;
                    }
                }, token);

                // Convert different result types to enumerable
                var convertedResult = await ConvertToEnumerable(result, config, token);

                var resultCount = convertedResult?.Count() ?? 0;
                _progressHelper.LogImport($"Fetched {resultCount} records from source", resultCount);

                return convertedResult;
            }
            catch (Exception ex)
            {
                _progressHelper.LogError("Error in FetchSourceDataAsync", ex);
                throw;
            }
        }

        /// <summary>
        /// Converts various data result types to enumerable
        /// </summary>
        /// <param name="result">Raw data result</param>
        /// <param name="config">Import configuration</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Converted enumerable</returns>
        protected async Task<IEnumerable<object>> ConvertToEnumerable(object result, DataImportConfiguration config, CancellationToken token)
        {
            if (result == null)
                return null;

            return await Task.Run(() =>
            {
                try
                {
                    // Handle DataTable
                    if (result is DataTable table)
                    {
                        return ConvertDataTableToEnumerable(table, config);
                    }

                    // Handle already enumerable data
                    if (result is IEnumerable<object> enumerableData)
                    {
                        return enumerableData;
                    }

                    // Handle single object
                    if (!(result is IEnumerable<object>))
                    {
                        return new[] { result };
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    _progressHelper.LogError("Error converting data to enumerable", ex);
                    throw;
                }
            }, token);
        }

        /// <summary>
        /// Converts DataTable to enumerable objects
        /// </summary>
        /// <param name="table">DataTable to convert</param>
        /// <param name="config">Import configuration</param>
        /// <returns>Enumerable of objects</returns>
        protected IEnumerable<object> ConvertDataTableToEnumerable(DataTable table, DataImportConfiguration config)
        {
            try
            {
                // Try to get a specific type if available
                if (config.SourceEntityStructure?.Fields != null)
                {
                    var entityType = _editor.Utilfunction.GetEntityType(_editor, config.SourceEntityName, config.SourceEntityStructure.Fields);
                    if (entityType != null)
                    {
                        return _editor.Utilfunction.GetListByDataTable(table, entityType, config.SourceEntityStructure);
                    }
                }

                // Convert each row to a dictionary
                return table.AsEnumerable().Select(row =>
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn column in table.Columns)
                    {
                        dict[column.ColumnName] = row[column] == DBNull.Value ? null : row[column];
                    }
                    return (object)dict;
                });
            }
            catch (Exception ex)
            {
                _progressHelper.LogError("Error converting DataTable to enumerable", ex);
                throw;
            }
        }

        #endregion

        #region Control Operations

        /// <summary>
        /// Pauses the import operation
        /// </summary>
        public void PauseImport()
        {
            try
            {
                _pauseEvent.Reset();
                _progressHelper.LogImport("Import operation paused", 0);
            }
            catch (Exception ex)
            {
                _progressHelper.LogError("Error pausing import", ex);
            }
        }

        /// <summary>
        /// Resumes the import operation
        /// </summary>
        public void ResumeImport()
        {
            try
            {
                _pauseEvent.Set();
                _progressHelper.LogImport("Import operation resumed", 0);
            }
            catch (Exception ex)
            {
                _progressHelper.LogError("Error resuming import", ex);
            }
        }

        /// <summary>
        /// Cancels the import operation
        /// </summary>
        public void CancelImport()
        {
            try
            {
                _internalCancellationTokenSource?.Cancel();
                _progressHelper.LogImport("Import operation cancellation requested", 0);
            }
            catch (Exception ex)
            {
                _progressHelper.LogError("Error cancelling import", ex);
            }
        }

        /// <summary>
        /// Gets the current import status
        /// </summary>
        /// <returns>Import status information</returns>
        public ImportStatus GetImportStatus()
        {
            try
            {
                return new ImportStatus
                {
                    IsRunning = _importTask?.Status == TaskStatus.Running,
                    IsPaused = !_pauseEvent.IsSet,
                    IsCancelled = _internalCancellationTokenSource?.Token.IsCancellationRequested ?? false,
                    IsCompleted = _importTask?.IsCompleted ?? false,
                    HasErrors = _progressHelper.ImportLogData.Any(log => log.Level == ImportLogLevel.Error)
                };
            }
            catch (Exception ex)
            {
                _progressHelper.LogError("Error getting import status", ex);
                return new ImportStatus { HasErrors = true };
            }
        }

        #endregion

        #region Validation and Testing

        /// <summary>
        /// Tests the import configuration without executing the import
        /// </summary>
        /// <param name="config">Configuration to test</param>
        /// <returns>Validation result</returns>
        public async Task<IErrorsInfo> TestImportConfigurationAsync(DataImportConfiguration config)
        {
            try
            {
                _progressHelper.LogImport("Testing import configuration...", 0);

                // Validate configuration
                var configValidation = _validationHelper.ValidateImportConfiguration(config);
                if (configValidation.Flag == Errors.Failed)
                    return configValidation;

                // Test data source connections
                await InitializeDataSources(config);

                var sourceValidation = _validationHelper.ValidateDataSources(config.SourceData, config.DestData);
                if (sourceValidation.Flag == Errors.Failed)
                    return sourceValidation;

                // Test entity compatibility if both structures are available
                if (config.SourceEntityStructure != null && config.DestEntityStructure != null)
                {
                    var entityValidation = _validationHelper.ValidateEntityCompatibility(
                        config.SourceEntityStructure, config.DestEntityStructure);
                    if (entityValidation.Flag == Errors.Failed)
                        return entityValidation;
                }

                // Test data fetch (limit to small sample)
                var originalFilters = config.SourceFilters;
                try
                {
                    // Add a limit filter for testing
                    var testFilters = new List<AppFilter>(originalFilters ?? new List<AppFilter>());
                    // Note: This would need to be adapted based on the data source type's limit syntax
                    
                    config.SourceFilters = testFilters;
                    var testData = await FetchSourceDataAsync(config, CancellationToken.None);
                    
                    if (testData != null && testData.Any())
                    {
                        _progressHelper.LogImport($"Successfully fetched test data sample", testData.Count());
                    }
                    else
                    {
                        _progressHelper.LogImport("No test data available, but connection is valid", 0);
                    }
                }
                finally
                {
                    config.SourceFilters = originalFilters;
                }

                _progressHelper.LogImport("Import configuration test completed successfully", 0);
                return CreateErrorsInfo(Errors.Ok, "Import configuration test passed");
            }
            catch (Exception ex)
            {
                _progressHelper.LogError("Import configuration test failed", ex);
                return CreateErrorsInfo(Errors.Failed, $"Import configuration test failed: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Import status information
    /// </summary>
    public class ImportStatus
    {
        public bool IsRunning { get; set; }
        public bool IsPaused { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsCompleted { get; set; }
        public bool HasErrors { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}