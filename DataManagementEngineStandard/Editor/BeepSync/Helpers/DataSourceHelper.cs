using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync.Interfaces;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.BeepSync.Helpers
{
    /// <summary>
    /// Helper class for data source operations in sync processes
    /// Based on patterns from DataSyncManager and DataSyncService
    /// </summary>
    public class DataSourceHelper : IDataSourceHelper
    {
        private readonly IDMEEditor _editor;

        /// <summary>
        /// Initializes a new instance of the DataSourceHelper class
        /// </summary>
        /// <param name="editor">The DME editor instance</param>
        public DataSourceHelper(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>
        /// Get data source by name with validation and error logging
        /// </summary>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <returns>IDataSource instance or null if not found</returns>
        public IDataSource GetDataSource(string dataSourceName)
        {
            if (string.IsNullOrWhiteSpace(dataSourceName))
            {
                _editor.AddLogMessage("BeepSync", "Data source name cannot be null or empty", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }

            var dataSource = _editor.GetDataSource(dataSourceName);
            if (dataSource == null)
            {
                _editor.AddLogMessage("BeepSync", $"Data source '{dataSourceName}' not found", DateTime.Now, -1, "", Errors.Failed);
            }

            return dataSource;
        }

        /// <summary>
        /// Validate that a data source exists and is accessible
        /// </summary>
        /// <param name="dataSourceName">Name of the data source to validate</param>
        /// <returns>True if data source is valid and accessible</returns>
        public bool ValidateDataSourceConnection(string dataSourceName)
        {
            try
            {
                var dataSource = GetDataSource(dataSourceName);
                if (dataSource == null)
                    return false;

                // Try to connect to validate accessibility
                var connectionResult = dataSource.Openconnection();
                if (connectionResult != ConnectionState.Open)
                {
                    _editor.AddLogMessage("BeepSync", $"Cannot connect to data source '{dataSourceName}'", DateTime.Now, -1, "", Errors.Failed);
                    return false;
                }

                dataSource.Closeconnection();
                return true;
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error validating data source '{dataSourceName}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Get entity data from source with filters - async version
        /// </summary>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="entityName">Name of the entity</param>
        /// <param name="filters">Optional filters to apply</param>
        /// <returns>Entity data or null if not found</returns>
        public async Task<object> GetEntityDataAsync(string dataSourceName, string entityName, List<AppFilter> filters = null)
        {
            try
            {
                var dataSource = GetDataSource(dataSourceName);
                if (dataSource == null)
                    return null;

                _editor.AddLogMessage("BeepSync", $"Retrieving data from {dataSourceName}.{entityName}", DateTime.Now, -1, "", Errors.Ok);

                // Use synchronous method wrapped in Task.Run for async behavior
                return await Task.Run(() => dataSource.GetEntity(entityName, filters));
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error retrieving data from {dataSourceName}.{entityName}: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Insert entity data into destination - async version
        /// </summary>
        /// <param name="dataSourceName">Name of the destination data source</param>
        /// <param name="entityName">Name of the entity</param>
        /// <param name="entity">Entity data to insert</param>
        /// <returns>Operation result</returns>
        public async Task<IErrorsInfo> InsertEntityAsync(string dataSourceName, string entityName, object entity)
        {
            try
            {
                var dataSource = GetDataSource(dataSourceName);
                if (dataSource == null)
                    return CreateErrorResult($"Data source '{dataSourceName}' not found");

                _editor.AddLogMessage("BeepSync", $"Inserting record into {dataSourceName}.{entityName}", DateTime.Now, -1, "", Errors.Ok);

                // Use synchronous method wrapped in Task.Run for async behavior
                var result = await Task.Run(() => dataSource.InsertEntity(entityName, entity));

                if (result?.Flag == Errors.Failed)
                {
                    _editor.AddLogMessage("BeepSync", $"Failed to insert record into {dataSourceName}.{entityName}: {result.Message}", DateTime.Now, -1, "", Errors.Failed);
                }

                return result ?? CreateSuccessResult("Insert completed successfully");
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error inserting into {dataSourceName}.{entityName}: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return CreateErrorResult($"Insert failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Update entity data in destination - async version
        /// </summary>
        /// <param name="dataSourceName">Name of the destination data source</param>
        /// <param name="entityName">Name of the entity</param>
        /// <param name="entity">Entity data to update</param>
        /// <returns>Operation result</returns>
        public async Task<IErrorsInfo> UpdateEntityAsync(string dataSourceName, string entityName, object entity)
        {
            try
            {
                var dataSource = GetDataSource(dataSourceName);
                if (dataSource == null)
                    return CreateErrorResult($"Data source '{dataSourceName}' not found");

                _editor.AddLogMessage("BeepSync", $"Updating record in {dataSourceName}.{entityName}", DateTime.Now, -1, "", Errors.Ok);

                // Use synchronous method wrapped in Task.Run for async behavior
                var result = await Task.Run(() => dataSource.UpdateEntity(entityName, entity));

                if (result?.Flag == Errors.Failed)
                {
                    _editor.AddLogMessage("BeepSync", $"Failed to update record in {dataSourceName}.{entityName}: {result.Message}", DateTime.Now, -1, "", Errors.Failed);
                }

                return result ?? CreateSuccessResult("Update completed successfully");
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error updating {dataSourceName}.{entityName}: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return CreateErrorResult($"Update failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if entity exists in destination based on filters
        /// </summary>
        /// <param name="dataSourceName">Name of the data source</param>
        /// <param name="entityName">Name of the entity</param>
        /// <param name="filters">Filters to identify the entity</param>
        /// <returns>True if entity exists</returns>
        public async Task<bool> EntityExistsAsync(string dataSourceName, string entityName, List<AppFilter> filters)
        {
            try
            {
                var data = await GetEntityDataAsync(dataSourceName, entityName, filters);
                return data != null;
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error checking entity existence in {dataSourceName}.{entityName}: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Create error result for failed operations
        /// </summary>
        private IErrorsInfo CreateErrorResult(string message)
        {
            return new ErrorsInfo { Flag = Errors.Failed, Message = message };
        }

        /// <summary>
        /// Create success result for successful operations
        /// </summary>
        private IErrorsInfo CreateSuccessResult(string message)
        {
            return new ErrorsInfo { Flag = Errors.Ok, Message = message };
        }
    }
}
