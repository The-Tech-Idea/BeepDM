using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.BeepSync.Interfaces
{
    /// <summary>
    /// Interface for data source operations in sync processes
    /// </summary>
    public interface IDataSourceHelper
    {
        /// <summary>
        /// Get data source by name with validation
        /// </summary>
        IDataSource GetDataSource(string dataSourceName);
        
        /// <summary>
        /// Validate that a data source exists and is accessible
        /// </summary>
        bool ValidateDataSourceConnection(string dataSourceName);
        
        /// <summary>
        /// Get entity data from source with filters
        /// </summary>
        Task<object> GetEntityDataAsync(string dataSourceName, string entityName, List<AppFilter> filters = null);
        
        /// <summary>
        /// Insert entity data into destination
        /// </summary>
        Task<IErrorsInfo> InsertEntityAsync(string dataSourceName, string entityName, object entity);
        
        /// <summary>
        /// Update entity data in destination
        /// </summary>
        Task<IErrorsInfo> UpdateEntityAsync(string dataSourceName, string entityName, object entity);
        
        /// <summary>
        /// Check if entity exists in destination
        /// </summary>
        Task<bool> EntityExistsAsync(string dataSourceName, string entityName, List<AppFilter> filters);
    }

    /// <summary>
    /// Interface for field mapping operations
    /// </summary>
    public interface IFieldMappingHelper
    {
        /// <summary>
        /// Map fields from source to destination object
        /// </summary>
        void MapFields(object source, object destination, IEnumerable<FieldSyncData> mappedFields);
        
        /// <summary>
        /// Create destination entity instance
        /// </summary>
        object CreateDestinationEntity(string dataSourceName, string entityName);
        
        /// <summary>
        /// Auto-map fields based on name matching
        /// </summary>
        List<FieldSyncData> AutoMapFields(string sourceDataSource, string sourceEntity, string destDataSource, string destEntity);
        
        /// <summary>
        /// Validate field mappings
        /// </summary>
        IErrorsInfo ValidateFieldMappings(IEnumerable<FieldSyncData> mappedFields);
    }

    /// <summary>
    /// Interface for sync validation operations
    /// </summary>
    public interface ISyncValidationHelper
    {
        /// <summary>
        /// Validate complete sync schema
        /// </summary>
        IErrorsInfo ValidateSchema(DataSyncSchema schema);
        
        /// <summary>
        /// Validate data source configuration
        /// </summary>
        IErrorsInfo ValidateDataSource(string dataSourceName);
        
        /// <summary>
        /// Validate entity exists in data source
        /// </summary>
        IErrorsInfo ValidateEntity(string dataSourceName, string entityName);
        
        /// <summary>
        /// Validate sync operation before execution
        /// </summary>
        IErrorsInfo ValidateSyncOperation(DataSyncSchema schema);
    }

    /// <summary>
    /// Interface for sync progress and logging
    /// </summary>
    public interface ISyncProgressHelper
    {
        /// <summary>
        /// Report progress with message
        /// </summary>
        void ReportProgress(IProgress<PassedArgs> progress, string message, int current = 0, int total = 0);
        
        /// <summary>
        /// Log sync operation message
        /// </summary>
        void LogMessage(string message, Errors errorLevel = Errors.Ok);
        
        /// <summary>
        /// Log sync run details
        /// </summary>
        void LogSyncRun(DataSyncSchema schema);
        
        /// <summary>
        /// Handle and log sync errors
        /// </summary>
        void LogError(DataSyncSchema schema, string message, Exception ex = null);
        
        /// <summary>
        /// Log cancellation of sync operation
        /// </summary>
        void LogCancellation(DataSyncSchema schema, IProgress<PassedArgs> progress);
        
        /// <summary>
        /// Log successful completion of sync operation
        /// </summary>
        void LogSuccess(DataSyncSchema schema, int recordsProcessed, IProgress<PassedArgs> progress);
    }

    /// <summary>
    /// Interface for sync schema persistence
    /// </summary>
    public interface ISchemaPersistenceHelper
    {
        /// <summary>
        /// Save sync schemas to storage
        /// </summary>
        Task SaveSchemasAsync(IEnumerable<DataSyncSchema> schemas);
        
        /// <summary>
        /// Load sync schemas from storage
        /// </summary>
        Task<ObservableBindingList<DataSyncSchema>> LoadSchemasAsync();
        
        /// <summary>
        /// Save single schema
        /// </summary>
        Task SaveSchemaAsync(DataSyncSchema schema);
        
        /// <summary>
        /// Delete schema from storage
        /// </summary>
        Task DeleteSchemaAsync(string schemaId);
    }
}
