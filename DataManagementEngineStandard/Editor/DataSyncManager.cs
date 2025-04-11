using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TheTechIdea.Beep.Editor;

using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Editor
{
    public partial class DataSyncManager : IDisposable
    {
        private bool disposedValue;
        private int Errorcount;
        private int CurrentScriptRecord;
        private int StopErrorCount;
        private bool Stoprun;
        private int ScriptCount;

        public DataSyncManager(IDMEEditor dME)
        {
            Editor = dME;
            SyncSchemas = new ObservableBindingList<DataSyncSchema>();
            string appdatapath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            filepath = Path.Combine(appdatapath, "TheTechIdea", "Beep", "DataSyncManager");

            if (!Directory.Exists(filepath))
            {
                Directory.CreateDirectory(filepath);
            }

            LoadSchemas();
            pauseEvent = new ManualResetEventSlim(true);
            cancellationTokenSource = new CancellationTokenSource();
        }

        public string filepath { get; set; }
        public IDMEEditor Editor { get; }
        public ObservableBindingList<DataSyncSchema> SyncSchemas { get; set; }
        private Dictionary<DateTime, EntityUpdateInsertLog> UpdateLog = new Dictionary<DateTime, EntityUpdateInsertLog>();
        private ManualResetEventSlim pauseEvent;
        private CancellationTokenSource cancellationTokenSource;

        // Added Sync Metrics tracking
        public SyncMetrics Metrics { get; private set; } = new SyncMetrics();
        /// <summary>
        /// Retrieves records from the source data based on the schema's LastSyncDate and the specified filter operator.
        /// </summary>
        /// <param name="schema">The DataSyncSchema defining the synchronization process.</param>
        /// <param name="filterOperator">The filter operator to use for comparing the LastSyncDate.</param>
        /// <returns>A collection of records from the source data.</returns>
        private async Task<object> GetRecordsFromSourceData(DataSyncSchema schema, string filterOperator)
        {
            IDataSource SourceData = GetDataSource(schema.SourceDataSourceName);
            if (SourceData == null)
            {
                Editor.AddLogMessage("Beep", "Source data source not found.", DateTime.Now, -1, "", Errors.Failed);
                return null;
            }

            var sourceFilters = schema.Filters.ToList();
            sourceFilters.Add(new AppFilter
            {
                FieldName = schema.SourceSyncDataField,
                Operator = filterOperator,
                FilterValue = schema.LastSyncDate.ToString("yyyy-MM-dd HH:mm:ss") // Format date as needed
            });

            return await SourceData.GetEntityAsync(schema.SourceEntityName, sourceFilters);
        }

        /// <summary>
        /// Retrieves new records from the source data based on the schema's LastSyncDate.
        /// </summary>
        /// <param name="schema">The DataSyncSchema defining the synchronization process.</param>
        /// <returns>A collection of new records from the source data.</returns>
        private Task<object> GetNewRecordsFromSourceData(DataSyncSchema schema)
        {
            return GetRecordsFromSourceData(schema, ">");
        }

        /// <summary>
        /// Retrieves updated records from the source data based on the schema's LastSyncDate.
        /// </summary>
        /// <param name="schema">The DataSyncSchema defining the synchronization process.</param>
        /// <returns>A collection of updated records from the source data.</returns>
        private Task<object> GetUpdatedRecordsFromSourceData(DataSyncSchema schema)
        {
            return GetRecordsFromSourceData(schema, ">=");
        }

       

        /// <summary>
        /// Asynchronously synchronizes data based on the provided schema, cancellation token, and progress reporter.
        /// </summary>
        /// <param name="schema">The DataSyncSchema defining the synchronization process.</param>
        /// <param name="token">Cancellation token to handle task cancellation.</param>
        /// <param name="progress">Progress reporter to report synchronization progress.</param>
        public async Task SyncDataAsync(DataSyncSchema schema, CancellationToken token, IProgress<PassedArgs> progress)
        {
            if (ValidateSchema(schema).Flag == Errors.Failed)
            {
                schema.SyncStatus = "Failed";
                schema.SyncStatusMessage = "Schema Validation failed.";
                Editor.AddLogMessage("Beep", "Schema Validation failed.", DateTime.Now, -1, "", Errors.Failed);
                return;
            }

            IDataSource SourceData = GetDataSource(schema.SourceDataSourceName);
            IDataSource DestinationData = GetDataSource(schema.DestinationDataSourceName);

            if (SourceData != null && DestinationData != null && schema != null)
            {
                try
                {
                    // Retrieve data from SourceData using schema.SourceSyncDataField
                    var sourceFilters = schema.Filters.ToList();
                    SendMessege(progress, token, $"Getting Source Entity Data {schema.SourceEntityName}...");
                    var sourceData = await Task.Run(() => SourceData.GetEntity(schema.SourceEntityName, sourceFilters), token);

                    if (sourceData == null)
                    {
                        schema.SyncStatus = "Failed";
                        schema.SyncStatusMessage = "Source data retrieval failed.";
                        SendMessege(progress, token, $"Failed: Getting Source Entity Data {schema.SourceEntityName}!!!");
                        Editor.AddLogMessage("Beep", "Source data retrieval failed.", DateTime.Now, -1, "", Errors.Failed);
                        LogSyncRun(schema);
                        return;
                    }

                    // Check if record exists in DestinationData
                    SendMessege(progress, token, $"Checking if record exists in Destination Data {schema.DestinationEntityName}...");
                    List<AppFilter> ExistFilters = new List<AppFilter>
                    {
                        new AppFilter
                        {
                            FieldName = schema.SourceSyncDataField,
                            Operator = "=",
                            FilterValue = sourceData.GetType().GetProperty(schema.SourceSyncDataField).GetValue(sourceData).ToString()
                        }
                    };
                    var destinationData = await Task.Run(() => DestinationData.GetEntity(schema.DestinationEntityName, ExistFilters), token);
                    bool isNewRecord = destinationData == null;

                    // Map source fields to destination fields and perform update or insert
                    var destEntity = CreateDestinationEntity(schema, sourceData, isNewRecord);

                    if (isNewRecord)
                    {
                        SendMessege(progress, token, $"Inserting New Record in Destination Data {schema.DestinationEntityName}...");
                        await Task.Run(() => DestinationData.InsertEntity(schema.DestinationEntityName, destEntity), token);
                    }
                    else
                    {
                        SendMessege(progress, token, $"Updating Record in Destination Data {schema.DestinationEntityName}...");
                        await Task.Run(() => DestinationData.UpdateEntity(schema.DestinationEntityName, destEntity), token);
                    }

                    SendMessege(progress, token, $"Synchronization completed for {schema.DestinationEntityName}...");
                    // Update sync status and log
                    schema.LastSyncDate = DateTime.Now;
                    schema.SyncStatus = "Success";
                    schema.SyncStatusMessage = $"Synchronization completed successfully for {schema.DestinationEntityName}";
                    Editor.AddLogMessage("Beep", $"Synchronization completed successfully for {schema.DestinationEntityName}", DateTime.Now, -1, "", Errors.Ok);
                    LogSyncRun(schema);
                }
                catch (Exception ex)
                {
                    // Handle exceptions and update sync status
                    schema.SyncStatus = "Failed";
                    schema.SyncStatusMessage = $"Synchronization failed: {ex.Message}";
                    Editor.AddLogMessage("Beep", $"Synchronization failed: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                    LogSyncRun(schema);
                }
            }
            else
            {
                schema.SyncStatus = "Failed";
                schema.SyncStatusMessage = "Data Source not found";
                Editor.AddLogMessage("Beep", "Data Source not found", DateTime.Now, -1, "", Errors.Failed);
            }
        }

        /// <summary>
        /// Creates the destination entity by mapping fields from the source entity based on the schema.
        /// </summary>
        /// <param name="schema">The DataSyncSchema defining the synchronization process.</param>
        /// <param name="sourceData">The source data entity.</param>
        /// <param name="isNewRecord">Indicates whether the record is new or an update.</param>
        /// <returns>The mapped destination entity.</returns>
        private object CreateDestinationEntity(DataSyncSchema schema, object sourceData, bool isNewRecord)
        {
            IDataSource destinationData = GetDataSource(schema.DestinationDataSourceName);
            var destEntityType = destinationData.GetEntityType(schema.DestinationEntityName);
            var destEntity = Activator.CreateInstance(destEntityType);

            MapFields(sourceData, destEntity, schema.MappedFields);

            if (isNewRecord)
            {
                var keyFieldValue = sourceData.GetType().GetProperty(schema.SourceKeyField)?.GetValue(sourceData);
                destEntityType.GetProperty(schema.DestinationKeyField)?.SetValue(destEntity, keyFieldValue);
            }

            return destEntity;
        }

        /// <summary>
        /// Logs the synchronization run data.
        /// </summary>
        /// <param name="schema">The DataSyncSchema defining the synchronization process.</param>
        private void LogSyncRun(DataSyncSchema schema)
        {
            var syncRunData = new SyncRunData
            {
                SyncSchemaID = schema.ID,
                SyncDate = schema.LastSyncDate,
                SyncStatus = schema.SyncStatus,
                SyncStatusMessage = schema.SyncStatusMessage
            };
            schema.SyncRuns.Add(syncRunData);
            schema.LastSyncRunData = syncRunData;
        }

        /// <summary>
        /// Retrieves the IDataSource instance based on the name.
        /// </summary>
        /// <param name="dataSourceName">The name of the data source.</param>
        /// <returns>The IDataSource instance.</returns>
        private IDataSource GetDataSource(string dataSourceName)
        {
            IDataSource ds = Editor.GetDataSource(dataSourceName);
            if (ds == null)
            {
                Editor.AddLogMessage("Beep", $"Data Source {dataSourceName} not found", DateTime.Now, -1, "", Errors.Failed);
            }
            return ds;
        }

        /// <summary>
        /// Asynchronously synchronizes all data based on the loaded schemas, cancellation token, and progress reporter.
        /// </summary>
        /// <param name="token">Cancellation token to handle task cancellation.</param>
        /// <param name="progress">Progress reporter to report synchronization progress.</param>
        public async Task SyncAllDataAsync(CancellationToken token, IProgress<PassedArgs> progress)
        {
            foreach (var schema in SyncSchemas)
            {
                if (token.IsCancellationRequested)
                {
                    progress?.Report(new PassedArgs { Messege = "Synchronization canceled.", EventType = "Cancel" });
                    break;
                }

                await SyncDataAsync(schema, token, progress);
            }
        }

        /// <summary>
        /// Synchronizes all data based on the loaded schemas.
        /// </summary>
        public void SyncAllData()
        {
            foreach (var schema in SyncSchemas)
            {
                SyncData(schema);
            }
        }
        /// <summary>
        /// Synchronizes data based on the provided schema, cancellation token, and progress reporter.
        /// </summary>
        /// <param name="schema">The DataSyncSchema defining the synchronization process.</param>
        /// <param name="token">Cancellation token to handle task cancellation.</param>
        /// <param name="progress">Progress reporter to report synchronization progress.</param>
        public void SyncData(DataSyncSchema schema, CancellationToken token, Progress<PassedArgs> progress)
        {
            if (ValidateSchema(schema).Flag == Errors.Failed)
            {
                schema.SyncStatus = "Failed";
                schema.SyncStatusMessage = "Schema Validation failed.";
                Editor.AddLogMessage("Beep", "Schema Validation failed.", DateTime.Now, -1, "", Errors.Failed);
                return;
            }
            IDataSource SourceData = GetDataSource(schema.SourceDataSourceName);
            IDataSource DestinationData = GetDataSource(schema.DestinationDataSourceName);

            if (SourceData != null && DestinationData != null && schema != null)
            {
                try
                {
                    // Retrieve data from SourceData using schema.SourceSyncDataField
                    var sourceFilters = schema.Filters.ToList();
                    SendMessege(progress, token, $"Getting Source Entity Data {schema.DestinationEntityName}...");
                    var sourceData = SourceData.GetEntity(schema.SourceEntityName, sourceFilters);

                    if (sourceData == null)
                    {
                        schema.SyncStatus = "Failed";
                        schema.SyncStatusMessage = "Source data retrieval failed.";
                        SendMessege(progress, token, $"Failed : Getting Source Entity Data {schema.SourceEntityName} !!!");
                        Editor.AddLogMessage("Beep", "Source data retrieval failed.", DateTime.Now, -1, "", Errors.Failed);
                        LogSyncRun(schema);
                        return;
                    }

                    // Check if record exists in DestinationData
                    SendMessege(progress, token, $"Check if record exists in Destination Data {schema.DestinationEntityName}...");
                    List<AppFilter> ExistFilters = new List<AppFilter>
                    {
                        new AppFilter
                        {
                            FieldName = schema.SourceSyncDataField,
                            Operator = "=",
                            FilterValue = sourceData.GetType().GetProperty(schema.SourceSyncDataField).GetValue(sourceData).ToString()
                        }
                    };
                    var destinationData = DestinationData.GetEntity(schema.DestinationEntityName, ExistFilters);

                    bool isNewRecord = destinationData == null;

                    // Map source fields to destination fields and perform update or insert
                    var destEntity = CreateDestinationEntity(schema, sourceData, isNewRecord);

                    if (isNewRecord)
                    {
                        SendMessege(progress, token, $"Insert New Record in Destination Data {schema.DestinationEntityName}...");
                        DestinationData.InsertEntity(schema.DestinationEntityName, destEntity);
                    }
                    else
                    {
                        SendMessege(progress, token, $"Update Record in Destination Data {schema.DestinationEntityName}...");
                        DestinationData.UpdateEntity(schema.DestinationEntityName, destEntity);
                    }

                    SendMessege(progress, token, $"Synchronization completed for {schema.DestinationEntityName}...");
                    // Update sync status and log
                    schema.LastSyncDate = DateTime.Now;
                    schema.SyncStatus = "Success";
                    schema.SyncStatusMessage = $"Synchronization completed successfully {schema.DestinationEntityName}";
                    Editor.AddLogMessage("Beep", $"Synchronization completed successfully {schema.DestinationEntityName}", DateTime.Now, -1, "", Errors.Ok);
                    LogSyncRun(schema);
                }
                catch (Exception ex)
                {
                    // Handle exceptions and update sync status
                    schema.SyncStatus = "Failed";
                    schema.SyncStatusMessage = $"Synchronization failed: {ex.Message}";
                    Editor.AddLogMessage("Beep", $"Synchronization failed: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                    LogSyncRun(schema);
                }
            }
            else
            {
                schema.SyncStatus = "Failed";
                schema.SyncStatusMessage = "Data Source not found";
                Editor.AddLogMessage("Beep", "Data Source not found", DateTime.Now, -1, "", Errors.Failed);
            }
        }

        /// <summary>
        /// Synchronizes data based on the provided schema.
        /// </summary>
        /// <param name="schema">The DataSyncSchema defining the synchronization process.</param>
        public void SyncData(DataSyncSchema schema)
        {
            if (ValidateSchema(schema).Flag == Errors.Failed)
            {
                schema.SyncStatus = "Failed";
                schema.SyncStatusMessage = "Schema Validation failed.";
                Editor.AddLogMessage("Beep", "Schema Validation failed.", DateTime.Now, -1, "", Errors.Failed);
                return;
            }
            IDataSource SourceData = GetDataSource(schema.SourceDataSourceName);
            IDataSource DestinationData = GetDataSource(schema.DestinationDataSourceName);

            if (SourceData != null && DestinationData != null && schema != null)
            {
                try
                {
                    // Retrieve data from SourceData using schema.SourceSyncDataField
                    var sourceFilters = schema.Filters.ToList();
                    var sourceData = SourceData.GetEntity(schema.SourceEntityName, sourceFilters);

                    if (sourceData == null)
                    {
                        schema.SyncStatus = "Failed";
                        schema.SyncStatusMessage = "Source data retrieval failed.";
                        LogSyncRun(schema);
                        return;
                    }

                    // Check if record exists in DestinationData
                    List<AppFilter> ExistFilters = new List<AppFilter>
                    {
                        new AppFilter
                        {
                            FieldName = schema.SourceSyncDataField,
                            Operator = "=",
                            FilterValue = sourceData.GetType().GetProperty(schema.SourceSyncDataField).GetValue(sourceData).ToString()
                        }
                    };
                    var destinationData = DestinationData.GetEntity(schema.DestinationEntityName, ExistFilters);

                    bool isNewRecord = destinationData == null;

                    // Map source fields to destination fields and perform update or insert
                    var destEntity = CreateDestinationEntity(schema, sourceData, isNewRecord);

                    if (isNewRecord)
                    {
                        DestinationData.InsertEntity(schema.DestinationEntityName, destEntity);
                    }
                    else
                    {
                        DestinationData.UpdateEntity(schema.DestinationEntityName, destEntity);
                    }

                    // Update sync status and log
                    schema.LastSyncDate = DateTime.Now;
                    schema.SyncStatus = "Success";
                    schema.SyncStatusMessage = "Synchronization completed successfully";
                    Editor.AddLogMessage("Beep", "Synchronization completed successfully", DateTime.Now, -1, "", Errors.Ok);
                    LogSyncRun(schema);
                }
                catch (Exception ex)
                {
                    // Handle exceptions and update sync status
                    schema.SyncStatus = "Failed";
                    schema.SyncStatusMessage = $"Synchronization failed: {ex.Message}";
                    Editor.AddLogMessage("Beep", $"Synchronization failed: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                    LogSyncRun(schema);
                }
            }
            else
            {
                schema.SyncStatus = "Failed";
                schema.SyncStatusMessage = "Data Source not found";
                Editor.AddLogMessage("Beep", "Data Source not found", DateTime.Now, -1, "", Errors.Failed);
            }
        }

        public void SyncData(string SchemaID)
        {
            DataSyncSchema schema = SyncSchemas.Find(x => x.ID == SchemaID);
            SyncData(schema);
        }

        public void SyncData(string SchemaID, string SourceEntityName, string DestinationEntityName)
        {
            DataSyncSchema schema = SyncSchemas.Find(x => x.ID == SchemaID);
            schema.SourceEntityName = SourceEntityName;
            schema.DestinationEntityName = DestinationEntityName;
            SyncData(schema);
        }

        public void SyncData(string SchemaID, string SourceEntityName, string DestinationEntityName, string SourceSyncDataField)
        {
            DataSyncSchema schema = SyncSchemas.Find(x => x.ID == SchemaID);
            schema.SourceEntityName = SourceEntityName;
            schema.DestinationEntityName = DestinationEntityName;
            schema.SourceSyncDataField = SourceSyncDataField;
            SyncData(schema);
        }

        public void SyncData(string SchemaID, string SourceEntityName, string DestinationEntityName, string SourceSyncDataField, string DestinationyncDataField)
        {
            DataSyncSchema schema = SyncSchemas.Find(x => x.ID == SchemaID);
            schema.SourceEntityName = SourceEntityName;
            schema.DestinationEntityName = DestinationEntityName;
            schema.SourceSyncDataField = SourceSyncDataField;
            schema.DestinationSyncDataField = DestinationyncDataField;
            SyncData(schema);
        }

        public void SyncData(string SchemaID, string SourceEntityName, string DestinationEntityName, string SourceSyncDataField, string DestinationyncDataField, string SyncType)
        {
            DataSyncSchema schema = SyncSchemas.Find(x => x.ID == SchemaID);
            schema.SourceEntityName = SourceEntityName;
            schema.DestinationEntityName = DestinationEntityName;
            schema.SourceSyncDataField = SourceSyncDataField;
            schema.DestinationSyncDataField = DestinationyncDataField;
            schema.SyncType = SyncType;
            SyncData(schema);
        }

        public void SyncData(string SchemaID, string SourceEntityName, string DestinationEntityName, string SourceSyncDataField, string DestinationyncDataField, string SyncType, string SyncDirection)
        {
            DataSyncSchema schema = SyncSchemas.Find(x => x.ID == SchemaID);
            schema.SourceEntityName = SourceEntityName;
            schema.DestinationEntityName = DestinationEntityName;
            schema.SourceSyncDataField = SourceSyncDataField;
            schema.DestinationSyncDataField = DestinationyncDataField;
            schema.SyncType = SyncType;
            schema.SyncDirection = SyncDirection;
            SyncData(schema);
        }

        public void AddSyncSchema(DataSyncSchema schema)
        {
            SyncSchemas.Add(schema);
        }

        public void RemoveSyncSchema(string SchemaID)
        {
            DataSyncSchema schema = SyncSchemas.Find(x => x.ID == SchemaID);
            SyncSchemas.Remove(schema);
        }

        public void UpdateSyncSchema(DataSyncSchema schema)
        {
            DataSyncSchema schema1 = SyncSchemas.Find(x => x.ID == schema.ID);
            if (schema1 != null)
            {
                var index = SyncSchemas.IndexOf(schema1);
                SyncSchemas[index] = schema;
            }
        }

        public void AddFilter(string SchemaID, AppFilter filter)
        {
            DataSyncSchema schema = SyncSchemas.Find(x => x.ID == SchemaID);
            schema.Filters.Add(filter);
        }

        public void RemoveFilter(string SchemaID, string FieldName)
        {
            DataSyncSchema schema = SyncSchemas.Find(x => x.ID == SchemaID);
            AppFilter filter = schema.Filters.Find(x => x.FieldName == FieldName);
            schema.Filters.Remove(filter);
        }

        public void AddFieldMapping(string SchemaID, FieldSyncData field)
        {
            DataSyncSchema schema = SyncSchemas.Find(x => x.ID == SchemaID);
            schema.MappedFields.Add(field);
        }

        public void SaveSchemas()
        {
            string json = JsonConvert.SerializeObject(SyncSchemas, Formatting.Indented);
            File.WriteAllText(Path.Combine(filepath, "SyncSchemas.json"), json);
        }

        public void LoadSchemas()
        {
            string filePath = Path.Combine(filepath, "SyncSchemas.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                SyncSchemas = JsonConvert.DeserializeObject<ObservableBindingList<DataSyncSchema>>(json);
            }
        }
        /// <summary>
        /// Synchronizes changes from the update log to the destination data source.
        /// </summary>
        /// <param name="sourceList">The source ObservableBindingList containing the changes.</param>
        /// <param name="destinationDataSource">The destination IDataSource to apply the changes to.</param>
        public void UpdateDataSourceUsingUpdateLog(Dictionary<DateTime, EntityUpdateInsertLog> updateLog, IProgress<PassedArgs> progress = null)
        {
            Parallel.ForEach(updateLog.Values, logEntry =>
            {
                var tracking = logEntry.TrackingRecord;
                var schema = SyncSchemas.Find(x => x.ID == tracking.UniqueId.ToString());

                if (schema != null)
                {
                    IDataSource destinationData = GetDataSource(schema.DestinationDataSourceName);
                    var destEntityType = destinationData.GetEntityType(tracking.EntityName);

                    var filters = new List<AppFilter>
            {
                new AppFilter
                {
                    FieldName = tracking.PKFieldName,
                    Operator = "=",
                    FilterValue = tracking.PKFieldValue
                }
            };

                    var existingRecord = destinationData.GetEntity(tracking.EntityName, filters);

                    if (existingRecord != null)
                    {
                        MapFields(logEntry.UpdatedFields, existingRecord, schema.MappedFields); // Use schema.MappedFields for field mappings
                        destinationData.UpdateEntity(tracking.EntityName, existingRecord);
                    }
                    else if (logEntry.LogAction == LogAction.Insert)
                    {
                        var newRecord = Activator.CreateInstance(destEntityType);
                        MapFields(logEntry.UpdatedFields, newRecord, schema.MappedFields); // Use schema.MappedFields for field mappings
                        destinationData.InsertEntity(tracking.EntityName, newRecord);
                    }

                    progress?.Report(new PassedArgs { Messege = $"Processed entity: {tracking.EntityName}" });
                }
            });
        }
        private void MapFields(object source, object destination, IEnumerable<FieldSyncData> mappedFields)
        {
            foreach (var field in mappedFields)
            {
                // Get the value of the source field
                var sourceValue = source.GetType().GetProperty(field.SourceField)?.GetValue(source);

                // Set the value of the destination field
                destination.GetType().GetProperty(field.DestinationField)?.SetValue(destination, sourceValue);
            }
        }


        private async Task RetryAsync(Func<Task> operation, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await operation();
                    return;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    Editor.AddLogMessage("Beep", $"Retry {attempt}: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                    await Task.Delay(1000); // Delay between retries
                }
            }
        }

        private void LoadUpdateLog(string datasourcename)
        {
            string filePath = Path.Combine(filepath, "UpdateLog.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                UpdateLog = JsonConvert.DeserializeObject<Dictionary<DateTime, EntityUpdateInsertLog>>(json);
            }
        }
        /// <summary>
        /// Validates the provided schema to ensure all required fields are populated.
        /// </summary>
        /// <param name="schema">The DataSyncSchema to validate.</param>
        /// <returns>An IErrorsInfo object containing validation results.</returns>
        public IErrorsInfo ValidateSchema(DataSyncSchema schema)
        {
            IErrorsInfo err = new ErrorsInfo();
            err.Flag = Errors.Ok;
            if (string.IsNullOrEmpty(schema.SourceDataSourceName))
            {
                err.Flag = Errors.Failed;
                err.Errors.Add(new ErrorsInfo { Message = "Source Data Source Name is empty" });
            }
            if (string.IsNullOrEmpty(schema.DestinationDataSourceName))
            {
                err.Flag = Errors.Failed;
                err.Errors.Add(new ErrorsInfo { Message = "Destination Data Source Name is empty" });
            }
            if (string.IsNullOrEmpty(schema.SourceEntityName))
            {
                err.Flag = Errors.Failed;
                err.Errors.Add(new ErrorsInfo { Message = "Source Entity Name is empty" });
            }
            if (string.IsNullOrEmpty(schema.DestinationEntityName))
            {
                err.Flag = Errors.Failed;
                err.Errors.Add(new ErrorsInfo { Message = "Destination Entity Name is empty" });
            }
            if (string.IsNullOrEmpty(schema.SourceSyncDataField))
            {
                err.Flag = Errors.Failed;
                err.Errors.Add(new ErrorsInfo { Message = "Source Sync Data Field is empty" });
            }
            if (string.IsNullOrEmpty(schema.DestinationSyncDataField))
            {
                err.Flag = Errors.Failed;
                err.Errors.Add(new ErrorsInfo { Message = "Destination Sync Data Field is empty" });
            }
            if (string.IsNullOrEmpty(schema.SyncType))
            {
                err.Flag = Errors.Failed;
                err.Errors.Add(new ErrorsInfo { Message = "Sync Type is empty" });
            }
            if (string.IsNullOrEmpty(schema.SyncDirection))
            {
                err.Flag = Errors.Failed;
                err.Errors.Add(new ErrorsInfo { Message = "Sync Direction is empty" });
            }
            return err;
        }

        /// <summary>Sends a message and updates progress based on the result.</summary>
        /// <param name="progress">An object that reports progress updates.</param>
        /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
        /// <param name="refentity">An optional reference to an entity structure.</param>
        /// <param name="sc">An optional ETL script detail.</param>
        /// <param name="messege">An optional message to send.</param>
        /// <remarks>
        /// If the error flag is set to "Failed" in the DMEEditor.ErrorObject, a SyncErrorsandTracking object is created and the error count is incremented.
        /// </remarks>
        private void SendMessege(IProgress<PassedArgs> progress, CancellationToken token, string messege = null)
        {
            if (Editor.ErrorObject.Flag == Errors.Failed)
            {
                SyncErrorsandTracking tr = new SyncErrorsandTracking
                {
                    errormessage = Editor.ErrorObject.Message
                };
                Errorcount++;

                if (progress != null)
                {
                    PassedArgs ps = new PassedArgs { EventType = "Update", ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount, Messege = Editor.ErrorObject.Message };
                    progress.Report(ps);
                }
                if (Errorcount > StopErrorCount)
                {
                    Stoprun = true;
                    PassedArgs ps = new PassedArgs { EventType = "Stop", ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount, Messege = Editor.ErrorObject.Message };
                    progress.Report(ps);
                }
            }
            else
            {
                if (progress != null)
                {
                    PassedArgs ps = new PassedArgs { EventType = "Update", ParameterInt1 = CurrentScriptRecord, ParameterInt2 = ScriptCount, Messege = Editor.ErrorObject.Message };
                    progress.Report(ps);
                }
            }
        }
        #region Pause, Resume, Stop

        public void PauseSync()
        {
            pauseEvent.Reset();
            Editor.AddLogMessage("Beep", "Synchronization paused.", DateTime.Now, -1, "", Errors.Ok);
        }

        public void ResumeSync()
        {
            pauseEvent.Set();
            Editor.AddLogMessage("Beep", "Synchronization resumed.", DateTime.Now, -1, "", Errors.Ok);
        }

        public void StopSync()
        {
            cancellationTokenSource.Cancel();
            Editor.AddLogMessage("Beep", "Synchronization stopped.", DateTime.Now, -1, "", Errors.Ok);
        }

        #endregion
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                    SyncSchemas?.Clear();
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class SyncMetrics
    {
        public string SchemaID { get; set; }
        public int TotalRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }
        public DateTime SyncDate { get; set; }
    }
}
