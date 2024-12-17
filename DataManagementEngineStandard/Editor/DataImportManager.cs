using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.ETL;
using TheTechIdea.Beep.Mapping;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Manages the process of importing data between source and destination entities.
    /// Supports entity structure loading, batch inserts, validation, and custom transformations.
    /// </summary>
    public partial class DataImportManager
    {
        /// <summary>Gets or sets the source entity name.</summary>
        public string SourceEntityName { get; set; } = string.Empty;

        /// <summary>Gets or sets the destination entity name.</summary>
        public string DestEntityName { get; set; } = string.Empty;

        /// <summary>Gets or sets the source data source name.</summary>
        public string SourceDataSourceName { get; set; } = string.Empty;

        /// <summary>Gets or sets the destination data source name.</summary>
        public string DestDataSourceName { get; set; } = string.Empty;

        /// <summary>Gets or sets the structure of the source entity.</summary>
        public EntityStructure SourceEntityStructure { get; set; }

        /// <summary>Gets or sets the structure of the destination entity.</summary>
        public EntityStructure DestEntityStructure { get; set; }

        /// <summary>Gets or sets the source data source object.</summary>
        public IDataSource SourceData { get; set; }

        /// <summary>Gets or sets the destination data source object.</summary>
        public IDataSource DestData { get; set; }

        /// <summary>Gets or sets the mapping between source and destination entities.</summary>
        public EntityDataMap Mapping { get; set; }

        /// <summary>Gets or sets the unit of work for managing mapping data.</summary>
        public UnitofWork<EntityDataMap_DTL> MappingunitofWork { get; set; }

        /// <summary>Gets the DME editor instance.</summary>
        public IDMEEditor DMEEditor { get; }

        private bool IsEntitychanged = false;
        private readonly ETLValidator _validator;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataImportManager"/> class.
        /// </summary>
        /// <param name="dMEEditor">The DME editor instance for data management.</param>
        public DataImportManager(IDMEEditor dMEEditor)
        {
            DMEEditor = dMEEditor ?? throw new ArgumentNullException(nameof(dMEEditor));
            _validator = new ETLValidator(dMEEditor);
        }

        /// <summary>
        /// Loads the structure of the destination entity from the specified data source.
        /// </summary>
        /// <param name="destEntityName">The name of the destination entity.</param>
        /// <param name="destDataSourceName">The name of the destination data source.</param>
        /// <returns>An <see cref="IErrorsInfo"/> object containing any errors that occur.</returns>
        public IErrorsInfo LoadDestEntityStructure(string destEntityName, string destDataSourceName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                DestData = DMEEditor.GetDataSource(destDataSourceName);
                DestEntityName = destEntityName;
                DestDataSourceName = destDataSourceName;

                if (DestData != null && DestData.ConnectionStatus == ConnectionState.Open)
                {
                    DestEntityStructure = (EntityStructure)DestData.GetEntityStructure(destEntityName, false).Clone();
                }
            }
            catch (Exception ex)
            {
                LogError("Error Loading Destination Entity", ex);
            }
            return DMEEditor.ErrorObject;
        }

        /// <summary>
        /// Runs the import process asynchronously with optional transformations and batching.
        /// </summary>
        /// <param name="progress">An object for reporting progress during the operation.</param>
        /// <param name="token">A cancellation token to cancel the operation.</param>
        /// <param name="transformation">An optional function to transform records before insertion.</param>
        /// <param name="batchSize">The number of records to insert in each batch.</param>
        /// <returns>An <see cref="IErrorsInfo"/> object containing the result of the operation.</returns>
        public async Task<IErrorsInfo> RunImportAsync(
            IProgress<IPassedArgs> progress,
            CancellationToken token,
            Func<object, object> transformation = null,
            int batchSize = 50)
        {
            try
            {
                // Validate mapping before starting the import
                var validation = _validator.ValidateEntityMapping(Mapping);
                if (validation.Flag == Errors.Failed)
                    return validation;

                // Fetch data from the source
                var sourceData = await FetchSourceDataAsync();
                if (sourceData == null || !sourceData.Any())
                    return DMEEditor.ErrorObject;

                // Process data in batches
                foreach (var batch in sourceData.Batch(batchSize))
                {
                    var transformedBatch = transformation != null
                        ? batch.Select(transformation)
                        : batch;

                    await InsertBatchAsync(transformedBatch, progress, token);
                }

                DMEEditor.AddLogMessage("ETL", "Import completed successfully", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                LogError("Error Running Import", ex);
            }
            return DMEEditor.ErrorObject;
        }

        /// <summary>
        /// Fetches source data from the configured source entity and data source.
        /// </summary>
        /// <returns>An enumerable collection of objects representing the source data.</returns>
        private async Task<IEnumerable<object>> FetchSourceDataAsync()
        {
            var result = await Task.Run(() => SourceData.GetEntity(SourceEntityName, null));
            return result is DataTable table
                ? DMEEditor.Utilfunction.GetListByDataTable(table, null, SourceEntityStructure)
                : result as IEnumerable<object>;
        }

        /// <summary>
        /// Inserts a batch of records into the destination data source asynchronously.
        /// </summary>
        /// <param name="batch">The batch of records to insert.</param>
        /// <param name="progress">An object for reporting progress during the operation.</param>
        /// <param name="token">A cancellation token to cancel the operation.</param>
        private async Task InsertBatchAsync(IEnumerable<object> batch, IProgress<IPassedArgs> progress, CancellationToken token)
        {
            int processed = 0;
            foreach (var record in batch)
            {
                if (token.IsCancellationRequested) break;

                await Task.Run(() => DestData.InsertEntity(DestEntityName, record), token);
                processed++;
                progress?.Report(new PassedArgs
                {
                    Messege = $"Inserted {processed} records into {DestEntityName}.",
                    ParameterInt1 = processed
                });
            }
        }

        /// <summary>
        /// Logs an error message to the DME editor log.
        /// </summary>
        /// <param name="message">A brief description of the error.</param>
        /// <param name="ex">The exception that was thrown.</param>
        private void LogError(string message, Exception ex)
        {
            DMEEditor.AddLogMessage("Beep", $"{message}: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
        }
    }

   
}
