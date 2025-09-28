using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.ETL;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Editor.Importing.Interfaces
{
    /// <summary>
    /// Interface for managing data import operations
    /// </summary>
    public interface IDataImportManager : IDisposable
    {
        /// <summary>
        /// Gets the data validation helper instance
        /// </summary>
        IDataImportValidationHelper ValidationHelper { get; }

        /// <summary>
        /// Gets the data transformation helper instance
        /// </summary>
        IDataImportTransformationHelper TransformationHelper { get; }

        /// <summary>
        /// Gets the batch processing helper instance
        /// </summary>
        IDataImportBatchHelper BatchHelper { get; }

        /// <summary>
        /// Gets the progress monitoring helper instance
        /// </summary>
        IDataImportProgressHelper ProgressHelper { get; }
    }

    /// <summary>
    /// Interface for data import validation operations
    /// </summary>
    public interface IDataImportValidationHelper
    {
        /// <summary>
        /// Validates import configuration before execution
        /// </summary>
        /// <param name="config">Import configuration to validate</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateImportConfiguration(DataImportConfiguration config);

        /// <summary>
        /// Validates entity mapping configuration
        /// </summary>
        /// <param name="mapping">Entity mapping to validate</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateEntityMapping(EntityDataMap mapping);

        /// <summary>
        /// Validates source and destination entity compatibility
        /// </summary>
        /// <param name="sourceEntity">Source entity structure</param>
        /// <param name="destEntity">Destination entity structure</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateEntityCompatibility(EntityStructure sourceEntity, EntityStructure destEntity);

        /// <summary>
        /// Validates data source connections
        /// </summary>
        /// <param name="sourceDataSource">Source data source</param>
        /// <param name="destDataSource">Destination data source</param>
        /// <returns>Validation result</returns>
        IErrorsInfo ValidateDataSources(IDataSource sourceDataSource, IDataSource destDataSource);
    }

    /// <summary>
    /// Interface for data import transformation operations
    /// </summary>
    public interface IDataImportTransformationHelper
    {
        /// <summary>
        /// Applies field filtering to a record
        /// </summary>
        /// <param name="record">Source record</param>
        /// <param name="selectedFields">Fields to include</param>
        /// <returns>Filtered record</returns>
        object ApplyFieldFiltering(object record, List<string> selectedFields);

        /// <summary>
        /// Applies entity mapping transformations
        /// </summary>
        /// <param name="record">Source record</param>
        /// <param name="mapping">Entity mapping configuration</param>
        /// <param name="targetEntityName">Target entity name</param>
        /// <returns>Transformed record</returns>
        object ApplyEntityMapping(object record, EntityDataMap mapping, string targetEntityName);

        /// <summary>
        /// Applies default values to a record
        /// </summary>
        /// <param name="record">Target record</param>
        /// <param name="defaultValues">Default values to apply</param>
        /// <param name="entityStructure">Entity structure</param>
        /// <param name="dataSourceName">Data source name for context</param>
        /// <returns>Record with applied defaults</returns>
        object ApplyDefaultValues(object record, List<DefaultValue> defaultValues, EntityStructure entityStructure, string dataSourceName);

        /// <summary>
        /// Applies custom transformation function
        /// </summary>
        /// <param name="record">Source record</param>
        /// <param name="transformationFunction">Custom transformation function</param>
        /// <returns>Transformed record</returns>
        object ApplyCustomTransformation(object record, Func<object, object> transformationFunction);

        /// <summary>
        /// Applies complete transformation pipeline
        /// </summary>
        /// <param name="record">Source record</param>
        /// <param name="config">Import configuration</param>
        /// <returns>Fully transformed record</returns>
        object ApplyTransformationPipeline(object record, DataImportConfiguration config);
    }

    /// <summary>
    /// Interface for batch processing operations
    /// </summary>
    public interface IDataImportBatchHelper
    {
        /// <summary>
        /// Calculates optimal batch size based on data characteristics
        /// </summary>
        /// <param name="totalRecords">Total number of records</param>
        /// <param name="estimatedRecordSize">Estimated size per record in bytes</param>
        /// <param name="availableMemory">Available memory for processing</param>
        /// <returns>Optimal batch size</returns>
        int CalculateOptimalBatchSize(int totalRecords, long estimatedRecordSize, long? availableMemory = null);

        /// <summary>
        /// Processes a batch of records
        /// </summary>
        /// <param name="batch">Records to process</param>
        /// <param name="config">Import configuration</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Batch processing result</returns>
        Task<IErrorsInfo> ProcessBatchAsync(IEnumerable<object> batch, DataImportConfiguration config, 
            IProgress<PassedArgs> progress, CancellationToken token);

        /// <summary>
        /// Splits source data into batches
        /// </summary>
        /// <param name="sourceData">Source data to split</param>
        /// <param name="batchSize">Size of each batch</param>
        /// <returns>Enumerable of batches</returns>
        IEnumerable<IEnumerable<object>> SplitIntoBatches(IEnumerable<object> sourceData, int batchSize);
    }

    /// <summary>
    /// Interface for progress monitoring and logging operations
    /// </summary>
    public interface IDataImportProgressHelper
    {
        /// <summary>
        /// Gets the import log data
        /// </summary>
        List<Importlogdata> ImportLogData { get; }

        /// <summary>
        /// Logs an import operation
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="recordNumber">Associated record number</param>
        void LogImport(string message, int recordNumber);

        /// <summary>
        /// Logs an error
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="exception">Exception details</param>
        void LogError(string message, Exception exception);

        /// <summary>
        /// Reports progress to the progress reporter
        /// </summary>
        /// <param name="progress">Progress reporter</param>
        /// <param name="message">Progress message</param>
        /// <param name="recordsProcessed">Number of records processed</param>
        /// <param name="totalRecords">Total records to process</param>
        void ReportProgress(IProgress<PassedArgs> progress, string message, int recordsProcessed, int? totalRecords = null);

        /// <summary>
        /// Calculates and reports performance metrics
        /// </summary>
        /// <param name="startTime">Import start time</param>
        /// <param name="recordsProcessed">Records processed so far</param>
        /// <param name="totalRecords">Total records to process</param>
        /// <returns>Performance metrics</returns>
        ImportPerformanceMetrics CalculatePerformanceMetrics(DateTime startTime, int recordsProcessed, int totalRecords);

        /// <summary>
        /// Clears the import log
        /// </summary>
        void ClearLog();
    }

    /// <summary>
    /// Configuration class for data import operations
    /// </summary>
    public class DataImportConfiguration
    {
        public string SourceEntityName { get; set; } = string.Empty;
        public string DestEntityName { get; set; } = string.Empty;
        public string SourceDataSourceName { get; set; } = string.Empty;
        public string DestDataSourceName { get; set; } = string.Empty;
        public EntityStructure SourceEntityStructure { get; set; }
        public EntityStructure DestEntityStructure { get; set; }
        public IDataSource SourceData { get; set; }
        public IDataSource DestData { get; set; }
        public EntityDataMap Mapping { get; set; }
        public List<AppFilter> SourceFilters { get; set; } = new List<AppFilter>();
        public List<string> SelectedFields { get; set; }
        public List<DefaultValue> DefaultValues { get; set; } = new List<DefaultValue>();
        public Func<object, object> CustomTransformation { get; set; }
        public int BatchSize { get; set; } = 50;
        public bool CreateDestinationIfNotExists { get; set; } = true;
        public bool ApplyDefaults { get; set; } = true;
    }

    /// <summary>
    /// Performance metrics for import operations
    /// </summary>
    public class ImportPerformanceMetrics
    {
        public TimeSpan ElapsedTime { get; set; }
        public double RecordsPerSecond { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
        public double PercentageComplete { get; set; }
        public int RecordsProcessed { get; set; }
        public int TotalRecords { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Import log data structure
    /// </summary>
    public class Importlogdata
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Message { get; set; }
        public int RecordNumber { get; set; }
        public ImportLogLevel Level { get; set; } = ImportLogLevel.Info;
        public string Category { get; set; } = "Import";
    }

    /// <summary>
    /// Import log levels
    /// </summary>
    public enum ImportLogLevel
    {
        Info,
        Warning,
        Error,
        Debug,
        Success
    }
}