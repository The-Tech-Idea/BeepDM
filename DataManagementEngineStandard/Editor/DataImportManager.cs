using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.ETL;
using TheTechIdea.Beep.Mapping;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Editor
{
    public class Importlogdata
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Message { get; set; }
        public int RecordNumber { get; set; }
    }

    public partial class DataImportManager
    {
        public string SourceEntityName { get; set; } = string.Empty;
        public string DestEntityName { get; set; } = string.Empty;
        public string SourceDataSourceName { get; set; } = string.Empty;
        public string DestDataSourceName { get; set; } = string.Empty;
        public EntityStructure SourceEntityStructure { get; set; }
        public EntityStructure DestEntityStructure { get; set; }
        public IDataSource SourceData { get; set; }
        public IDataSource DestData { get; set; }

        /// <summary>
        /// Optional mapping configuration. If provided and MappedEntities are set, fields are mapped accordingly.
        /// </summary>
        public EntityDataMap Mapping { get; set; }

        /// <summary>
        /// Optional filters for fetching source data. If set, only filtered data is retrieved.
        /// </summary>
        public List<AppFilter> SourceFilters { get; set; } = new List<AppFilter>();

        public IDMEEditor DMEEditor { get; }

        private bool IsEntitychanged = false;
        private readonly ETLValidator _validator;
        private ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true);
        private CancellationTokenSource _cancellationTokenSource;
        private Task _importTask;

        public List<Importlogdata> ImportLogData { get; private set; } = new List<Importlogdata>();

        public DataImportManager(IDMEEditor dMEEditor)
        {
            DMEEditor = dMEEditor ?? throw new ArgumentNullException(nameof(dMEEditor));
            _validator = new ETLValidator(dMEEditor);
        }

        public IErrorsInfo LoadDestEntityStructure(string destEntityName, string destDataSourceName)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                DestData = DMEEditor.GetDataSource(destDataSourceName);
                DestEntityName = destEntityName;
                DestDataSourceName = destDataSourceName;

                if (DestData != null && DestData.ConnectionStatus == System.Data.ConnectionState.Open)
                {
                    DestEntityStructure = (EntityStructure)DestData.GetEntityStructure(destEntityName, false)?.Clone();
                }
            }
            catch (Exception ex)
            {
                LogError("Error Loading Destination Entity", ex);
            }
            return DMEEditor.ErrorObject;
        }

        public void StartImportAsync(
            IProgress<IPassedArgs> progress = null,
            Func<object, object> transformation = null,
            int batchSize = 50)
        {
            ImportLogData.Clear();

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            _importTask = Task.Run(() => RunImportAsync(progress, token, transformation, batchSize), token);
        }

        public async Task<IErrorsInfo> RunImportAsync(
            IProgress<IPassedArgs> progress,
            CancellationToken token,
            Func<object, object> transformation = null,
            int batchSize = 50)
        {
            try
            {
                if (Mapping != null)
                {
                    var validation = _validator.ValidateEntityMapping(Mapping);
                    if (validation.Flag == Errors.Failed)
                        return validation;
                }

                var sourceData = await FetchSourceDataAsync(token);
                if (sourceData == null || !sourceData.Any())
                    return DMEEditor.ErrorObject;

                int batchNumber = 0;
                foreach (var batch in sourceData.Batch(batchSize))
                {
                    batchNumber++;
                    _pauseEvent.Wait(token);
                    token.ThrowIfCancellationRequested();

                    var transformedBatch = transformation != null
                        ? batch.Select(transformation)
                        : batch;

                    await InsertBatchAsync(transformedBatch, progress, token);

                    LogImport($"Batch {batchNumber} completed.", 0);
                }

                DMEEditor.AddLogMessage("ETL", "Import completed successfully", DateTime.Now, 0, null, Errors.Ok);
                LogImport("Import completed successfully.", 0);
            }
            catch (OperationCanceledException)
            {
                LogImport("Import was stopped by the user.", 0);
            }
            catch (Exception ex)
            {
                LogError("Error Running Import", ex);
            }
            return DMEEditor.ErrorObject;
        }

        private async Task<IEnumerable<object>> FetchSourceDataAsync(CancellationToken token)
        {
            // Use SourceFilters to limit the data retrieved
            var result = await Task.Run(() => SourceData.GetEntity(SourceEntityName, SourceFilters), token);
            IEnumerable<object> data = null;

            if (result is DataTable table)
            {
                data = DMEEditor.Utilfunction.GetListByDataTable(table, null, SourceEntityStructure);
            }
            else if (result is IEnumerable<object> enumerableData)
            {
                data = enumerableData;
            }

            if (data != null)
            {
                LogImport($"Fetched {data.Count()} source records using applied filters.", 0);
            }

            return data;
        }

        private async Task InsertBatchAsync(IEnumerable<object> batch, IProgress<IPassedArgs> progress, CancellationToken token)
        {
            int processed = 0;

            bool hasMappings = Mapping?.MappedEntities != null && Mapping.MappedEntities.Any(p => p.EntityName == DestEntityName && p.EntityDataSource == DestDataSourceName);
            var mapDetail = hasMappings ? Mapping.MappedEntities.FirstOrDefault(p => p.EntityName == DestEntityName && p.EntityDataSource == DestDataSourceName) : null;

            foreach (var record in batch)
            {
                _pauseEvent.Wait(token);
                token.ThrowIfCancellationRequested();

                var finalRecord = hasMappings ? ApplyMapping(record, mapDetail) : record;

                await Task.Run(() => DestData.InsertEntity(DestEntityName, finalRecord), token);
                processed++;

                string message = $"Inserted {processed} records into {DestEntityName}.";
                progress?.Report(new PassedArgs
                {
                    Messege = message,
                    ParameterInt1 = processed
                });

                LogImport(message, processed);
            }
        }

        private object ApplyMapping(object sourceRecord, EntityDataMap_DTL mapDetail)
        {
            if (mapDetail == null || mapDetail.FieldMapping == null || !mapDetail.FieldMapping.Any())
                return sourceRecord;

            var destObject = CreateDestinationObject();

            foreach (var fieldMap in mapDetail.FieldMapping)
            {
                object sourceValue = GetPropertyValue(sourceRecord, fieldMap.FromFieldName);
                SetPropertyValue(destObject, fieldMap.ToFieldName, sourceValue);
            }

            return destObject;
        }

        private object CreateDestinationObject()
        {
            return new System.Dynamic.ExpandoObject();
        }

        private object GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName))
                return null;

            if (obj is IDictionary<string, object> dict)
            {
                return dict.ContainsKey(propertyName) ? dict[propertyName] : null;
            }

            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return prop?.GetValue(obj);
        }

        private void SetPropertyValue(object obj, string propertyName, object value)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName))
                return;

            if (obj is IDictionary<string, object> dict)
            {
                dict[propertyName] = value;
                return;
            }

            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value);
            }
        }

        private void LogError(string message, Exception ex)
        {
            DMEEditor.AddLogMessage("Beep", $"{message}: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            LogImport($"{message}: {ex.Message}", 0);
        }

        private void LogImport(string message, int recordNumber)
        {
            ImportLogData.Add(new Importlogdata
            {
                Timestamp = DateTime.Now,
                Message = message,
                RecordNumber = recordNumber
            });
        }

        public void PauseImport()
        {
            _pauseEvent.Reset();
            DMEEditor.AddLogMessage("ETL", "Import paused.", DateTime.Now, 0, null, Errors.Ok);
            LogImport("Import paused by user.", 0);
        }

        public void ResumeImport()
        {
            _pauseEvent.Set();
            DMEEditor.AddLogMessage("ETL", "Import resumed.", DateTime.Now, 0, null, Errors.Ok);
            LogImport("Import resumed by user.", 0);
        }

        public void StopImport()
        {
            _cancellationTokenSource?.Cancel();
            DMEEditor.AddLogMessage("ETL", "Import stop requested.", DateTime.Now, 0, null, Errors.Ok);
            LogImport("Import stop requested by user.", 0);
        }
    }

    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int size)
        {
            T[] bucket = null;
            var count = 0;

            foreach (var item in source)
            {
                if (bucket == null)
                    bucket = new T[size];

                bucket[count++] = item;
                if (count != size)
                    continue;

                yield return bucket;
                bucket = null;
                count = 0;
            }

            if (bucket != null && count > 0)
                yield return bucket.Take(count);
        }
    }
}
