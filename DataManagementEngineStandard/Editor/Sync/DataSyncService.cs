using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.Sync
{
    public interface ISyncService
    {
        Task SyncSchemaAsync(DataSyncSchema schema, CancellationToken token, IProgress<PassedArgs> progress = null);
        void SyncSchema(DataSyncSchema schema, IProgress<PassedArgs> progress = null);
    }

    public partial class DataSyncService : ISyncService
    {
        private readonly IDMEEditor _editor;
        private readonly ISyncValidator _validator;
        private readonly IFieldMapper _mapper;
        private readonly ManualResetEventSlim _pauseEvent;

        public DataSyncService(IDMEEditor editor, ISyncValidator validator, IFieldMapper mapper, ManualResetEventSlim pauseEvent)
        {
            _editor = editor;
            _validator = validator;
            _mapper = mapper;
            _pauseEvent = pauseEvent;
        }

        public async Task SyncSchemaAsync(DataSyncSchema schema, CancellationToken token, IProgress<PassedArgs> progress = null)
        {
            var validationResults = _validator.ValidateSchema(schema);
            if (validationResults.Flag == Errors.Failed)
            {
                HandleSchemaError(schema, "Schema validation failed.", validationResults);
                return;
            }

            // Example: Suppose we get multiple records from the source
            var sourceRecords = GetSourceRecords(schema, token, progress);
            if (sourceRecords == null) return;

            var destDS = GetDataSource(schema.DestinationDataSourceName);
            if (destDS == null)
            {
                HandleSchemaError(schema, "Destination data source not found.");
                return;
            }

            int processedCount = 0;
            foreach (var sourceData in sourceRecords)
            {
                // Check pause/resume:
                _pauseEvent.Wait(token);
                token.ThrowIfCancellationRequested();

                var destinationData = GetDataFromDestination(schema, sourceData, token, progress);
                bool isNewRecord = (destinationData == null);

                var destEntity = CreateDestinationEntity(schema, sourceData, isNewRecord);
                if (destEntity == null) return;

                try
                {
                    // Check pause/resume again before a long operation:
                    _pauseEvent.Wait(token);
                    token.ThrowIfCancellationRequested();

                    if (isNewRecord)
                    {
                        ReportMessage(progress, $"Inserting new record into {schema.DestinationEntityName}...");
                        await Task.Run(() => destDS.InsertEntity(schema.DestinationEntityName, destEntity), token);
                    }
                    else
                    {
                        ReportMessage(progress, $"Updating record in {schema.DestinationEntityName}...");
                        await Task.Run(() => destDS.UpdateEntity(schema.DestinationEntityName, destEntity), token);
                    }

                    processedCount++;
                    ReportMessage(progress, $"Processed {processedCount} records.");
                }
                catch (OperationCanceledException)
                {
                    HandleSchemaError(schema, "Synchronization canceled by user.");
                    return;
                }
                catch (Exception ex)
                {
                    HandleSchemaError(schema, $"Synchronization failed: {ex.Message}");
                    return;
                }

                // Check pause/resume/stop again as needed:
                _pauseEvent.Wait(token);
                token.ThrowIfCancellationRequested();
            }

            MarkSchemaSuccess(schema, processedCount);
        }

        public void SyncSchema(DataSyncSchema schema, IProgress<PassedArgs> progress = null)
        {
            // Similar logic for synchronous scenario.
            // The key is calling _pauseEvent.Wait() and token.ThrowIfCancellationRequested() in loops.
        }

        private IDataSource GetDataSource(string dataSourceName)
        {
            IDataSource ds = _editor.GetDataSource(dataSourceName);
            if (ds == null)
            {
                _editor.AddLogMessage("Beep", $"Data Source {dataSourceName} not found", DateTime.Now, -1, "", Errors.Failed);
            }
            return ds;
        }

        private IEnumerable<object> GetSourceRecords(DataSyncSchema schema, CancellationToken token, IProgress<PassedArgs> progress)
        {
            var sourceDS = GetDataSource(schema.SourceDataSourceName);
            if (sourceDS == null)
            {
                HandleSchemaError(schema, "Source data source not found.");
                return null;
            }

            ReportMessage(progress, "Fetching source data...");
            token.ThrowIfCancellationRequested();

            var sourceData = sourceDS.GetEntity(schema.SourceEntityName, schema.Filters.ToList());
            if (sourceData == null)
            {
                HandleSchemaError(schema, "Source data retrieval failed.");
                return null;
            }

            // Ensure sourceData is enumerable. If single object, wrap in a list.
            if (sourceData is IEnumerable<object> sourceEnumerable)
            {
                return sourceEnumerable;
            }
            else
            {
                return new List<object> { sourceData };
            }
        }

        private object GetDataFromDestination(DataSyncSchema schema, object sourceData, CancellationToken token, IProgress<PassedArgs> progress)
        {
            var destDS = GetDataSource(schema.DestinationDataSourceName);
            if (destDS == null)
            {
                HandleSchemaError(schema, "Destination data source not found.");
                return null;
            }

            var pkValue = sourceData.GetType().GetProperty(schema.SourceSyncDataField)?.GetValue(sourceData)?.ToString();
            if (string.IsNullOrEmpty(pkValue))
            {
                HandleSchemaError(schema, "Unable to extract source primary key value.");
                return null;
            }

            var existFilters = new List<AppFilter>
        {
            new AppFilter { FieldName = schema.SourceSyncDataField, Operator = "=", FilterValue = pkValue }
        };

            ReportMessage(progress, "Checking if record exists in destination...");
            token.ThrowIfCancellationRequested();

            return destDS.GetEntity(schema.DestinationEntityName, existFilters);
        }

        private object CreateDestinationEntity(DataSyncSchema schema, object sourceData, bool isNewRecord)
        {
            var destinationDS = GetDataSource(schema.DestinationDataSourceName);
            if (destinationDS == null) return null;

            var destEntityType = destinationDS.GetEntityType(schema.DestinationEntityName);
            var destEntity = Activator.CreateInstance(destEntityType);

            _mapper.MapFields(sourceData, destEntity, schema.MappedFields);

            if (isNewRecord && !string.IsNullOrEmpty(schema.SourceKeyField) && !string.IsNullOrEmpty(schema.DestinationKeyField))
            {
                var keyFieldValue = sourceData.GetType().GetProperty(schema.SourceKeyField)?.GetValue(sourceData);
                var destKeyProp = destEntityType.GetProperty(schema.DestinationKeyField);
                destKeyProp?.SetValue(destEntity, keyFieldValue);
            }

            return destEntity;
        }

        private void MarkSchemaSuccess(DataSyncSchema schema, int processedCount)
        {
            schema.LastSyncDate = DateTime.Now;
            schema.SyncStatus = "Success";
            schema.SyncStatusMessage = $"Synchronization completed successfully. Processed {processedCount} records.";
            _editor.AddLogMessage("Beep", schema.SyncStatusMessage, DateTime.Now, -1, "", Errors.Ok);
            LogSyncRun(schema);
        }

        private void HandleSchemaError(DataSyncSchema schema, string message, IErrorsInfo validationResults = null)
        {
            schema.SyncStatus = "Failed";
            schema.SyncStatusMessage = message;
            _editor.AddLogMessage("Beep", message, DateTime.Now, -1, "", Errors.Failed);

            if (validationResults != null && validationResults.Errors.Any())
            {
                foreach (var err in validationResults.Errors)
                {
                    _editor.AddLogMessage("Beep", err.Message, DateTime.Now, -1, "", Errors.Failed);
                }
            }
            LogSyncRun(schema);
        }

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

        private void ReportMessage(IProgress<PassedArgs> progress, string message)
        {
            progress?.Report(new PassedArgs { Messege = message, EventType = "Update" });
        }
    }



}
