using Newtonsoft.Json.Linq;
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
using TheTechIdea.Beep.Editor.Defaults;
using TheTechIdea.Beep.Editor.ETL;
using TheTechIdea.Beep.Editor.Mapping;
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

        public EntityDataMap Mapping { get; set; }
        public List<AppFilter> SourceFilters { get; set; } = new List<AppFilter>();
        public List<string> SelectedFields { get; set; }

        public IDMEEditor DMEEditor { get; }
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

                if (DestData != null && DestData.ConnectionStatus == ConnectionState.Open)
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

                await EnsureDestinationEntityExists();

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

        private async Task EnsureDestinationEntityExists()
        {
            if (DestData == null)
            {
                throw new InvalidOperationException("Destination data source is not initialized.");
            }

            if (!DestData.CheckEntityExist(DestEntityName))
            {
                DMEEditor.AddLogMessage("ETL", $"Destination entity '{DestEntityName}' does not exist. Creating it...", DateTime.Now, 0, null, Errors.Failed);
                LogImport($"Destination entity '{DestEntityName}' does not exist. Creating it...", 0);

                if (SourceEntityStructure == null)
                {
                    SourceEntityStructure = LoadSourceEntityStructure();
                    if (SourceEntityStructure == null)
                    {
                        throw new InvalidOperationException($"Source entity structure could not be loaded for '{SourceEntityName}'.");
                    }
                }

                var creationSuccess = await Task.Run(() => DestData.CreateEntityAs(SourceEntityStructure));
                if (creationSuccess)
                {
                    DMEEditor.AddLogMessage("ETL", $"Successfully created destination entity '{DestEntityName}'.", DateTime.Now, 0, null, Errors.Ok);
                    LogImport($"Successfully created destination entity '{DestEntityName}'.", 0);
                }
                else
                {
                    throw new Exception($"Failed to create destination entity '{DestEntityName}'.");
                }
            }
        }

        private EntityStructure LoadSourceEntityStructure()
        {
            if (SourceData == null)
            {
                SourceData = DMEEditor.GetDataSource(SourceDataSourceName);
            }

            if (SourceData == null || !SourceData.CheckEntityExist(SourceEntityName))
            {
                throw new InvalidOperationException($"Source entity '{SourceEntityName}' does not exist in source data source '{SourceDataSourceName}'.");
            }

            return SourceData.GetEntityStructure(SourceEntityName, false);
        }

        private async Task<IEnumerable<object>> FetchSourceDataAsync(CancellationToken token)
        {
            var result = await Task.Run(() => SourceData.GetEntity(SourceEntityName, SourceFilters), token);

            if (result is DataTable table)
            {
                return table.AsEnumerable()
                            .Select(row => DMEEditor.Utilfunction.ConvertDataTableToObservableList(table, DMEEditor.Utilfunction.GetEntityType(DMEEditor,SourceEntityName, SourceEntityStructure.Fields)));
            }

            if (result is IEnumerable<object> enumerableData)
            {
                return enumerableData;
            }

            return null;
        }

        private async Task InsertBatchAsync(IEnumerable<object> batch, IProgress<IPassedArgs> progress, CancellationToken token)
        {
            int processed = 0;
            List<DefaultValue> defaultValues = new List<DefaultValue>();
            defaultValues=DefaultsManager.GetDefaults(DMEEditor, DestDataSourceName);
            foreach (var originalRecord in batch)
            {
                _pauseEvent.Wait(token);
                token.ThrowIfCancellationRequested();

                // Use a new variable to store the possibly filtered record
                var finalRecord = SelectedFields != null && SelectedFields.Any()
                    ? FilterFields(originalRecord)
                    : originalRecord;
                // Map the source record to the destination format using MappingManager
                if (finalRecord != null) { 
                    if(Mapping != null)
                    {
                        if (Mapping.MappedEntities.Any(p=>p.EntityName.Equals(DestEntityName, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            finalRecord=MappingManager.MapObjectToAnother(DMEEditor, DestEntityName, Mapping.MappedEntities.FirstOrDefault(), finalRecord);
                        }
                    }
                }
                if(defaultValues != null && defaultValues.Any())
                {
                    foreach (var def in defaultValues)
                    {
                        if ( DestEntityStructure.Fields.Any(p=>p.fieldname.Equals(def.PropertyName, StringComparison.InvariantCultureIgnoreCase)))
                        {
                           object retval= DefaultsManager.ResolveDefaultValue(DMEEditor, DestDataSourceName, def.PropertyName, new PassedArgs() { SentData=def,ObjectName="DefaultValue"});
                            DMEEditor.Utilfunction.SetFieldValueFromObject(def.PropertyName, finalRecord, retval);
                        }
                    }
                }
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


        private object FilterFields(object record)
        {
            if (record == null)
                return null;

            var filteredRecord = new Dictionary<string, object>();

            foreach (var field in SelectedFields)
            {
                if (record is IDictionary<string, object> dict && dict.ContainsKey(field))
                {
                    filteredRecord[field] = dict[field];
                }
            }

            return filteredRecord;
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

        private void LogError(string message, Exception ex)
        {
            DMEEditor.AddLogMessage("Beep", $"{message}: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            LogImport($"{message}: {ex.Message}", 0);
        }
    }
}
