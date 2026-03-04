using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync.Helpers;
using TheTechIdea.Beep.Editor.BeepSync.Interfaces;
using TheTechIdea.Beep.Editor.Importing;
using TheTechIdea.Beep.Editor.Importing.ErrorStore;
using TheTechIdea.Beep.Editor.Importing.History;
using TheTechIdea.Beep.Editor.Importing.Interfaces;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor
{
    /// <summary>
    /// Modern sync orchestrator that delegates data transfer to DataImportManager.
    /// Manages DataSyncSchema objects and translates them to DataImportConfiguration for execution.
    /// </summary>
    public class BeepSyncManager : IDisposable
    {
        private bool _disposedValue;
        private readonly IDMEEditor _editor;
        private readonly ISyncValidationHelper _validationHelper;
        private readonly ISchemaPersistenceHelper _persistenceHelper;

        public string Filepath { get; set; }
        public IDMEEditor Editor => _editor;
        public ObservableBindingList<DataSyncSchema> SyncSchemas { get; set; }

        public BeepSyncManager(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            SyncSchemas = new ObservableBindingList<DataSyncSchema>();
            Filepath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TheTechIdea", "Beep", "BeepSyncManager");

            _validationHelper = new SyncValidationHelper(_editor);
            _persistenceHelper = new SchemaPersistenceHelper(_editor);

            LoadSchemas();
        }

        /// <summary>
        /// Synchronizes data for the given schema using DataImportManager.
        /// </summary>
        public async Task<IErrorsInfo> SyncDataAsync(
            DataSyncSchema schema,
            CancellationToken token = default,
            IProgress<PassedArgs> progress = null,
            IImportErrorStore errorStore = null,
            IImportRunHistoryStore historyStore = null)
        {
            var validation = _validationHelper.ValidateSyncOperation(schema);
            if (validation.Flag == Errors.Failed)
            {
                if (schema != null)
                {
                    schema.SyncStatus = "Failed";
                    schema.SyncStatusMessage = validation.Message ?? "Schema validation failed.";
                }
                return validation;
            }

            return await ErrorHandlingHelper.ExecuteWithErrorHandlingAsync(async () =>
            {
                var config = SyncSchemaTranslator.ToImportConfiguration(schema, errorStore, historyStore);
                IProgress<IPassedArgs> importProgress = CreateProgressAdapter(progress);

                using var importMgr = new DataImportManager(_editor);
                var result = await importMgr.RunImportAsync(config, importProgress, token);

                if (result.Flag == Errors.Failed)
                {
                    schema.SyncStatus = "Failed";
                    schema.SyncStatusMessage = result.Message ?? "Import failed.";
                    return result;
                }

                if (string.Equals(schema.SyncDirection, "Bidirectional", StringComparison.OrdinalIgnoreCase))
                {
                    var reverseConfig = SyncSchemaTranslator.ToReverseImportConfiguration(schema, errorStore, historyStore);
                    var reverseResult = await importMgr.RunImportAsync(reverseConfig, importProgress, token);
                    if (reverseResult.Flag == Errors.Failed)
                    {
                        schema.SyncStatus = "Failed";
                        schema.SyncStatusMessage = $"Reverse sync failed: {reverseResult.Message}";
                        return reverseResult;
                    }
                }

                schema.LastSyncDate = DateTime.Now;
                schema.SyncStatus = "Success";
                schema.SyncStatusMessage = $"Synchronization completed for {schema.DestinationEntityName}";
                LogSyncRun(schema);
                return result;
            }, $"SyncDataAsync:{schema?.SourceEntityName}", _editor, new ErrorsInfo { Flag = Errors.Failed });
        }

        /// <summary>
        /// Synchronous overload for backward compatibility.
        /// </summary>
        public void SyncData(DataSyncSchema schema)
        {
            if (schema == null) return;
            SyncDataAsync(schema).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Synchronous overload with progress and cancellation.
        /// </summary>
        public void SyncData(DataSyncSchema schema, CancellationToken token, IProgress<PassedArgs> progress)
        {
            SyncDataAsync(schema, token, progress).GetAwaiter().GetResult();
        }

        public async Task SyncAllDataAsync(CancellationToken token, IProgress<PassedArgs> progress)
        {
            foreach (var schema in SyncSchemas.ToList())
            {
                await SyncDataAsync(schema, token, progress);
                if (token.IsCancellationRequested) break;
            }
        }

        public void SyncAllData()
        {
            foreach (var schema in SyncSchemas.ToList())
                SyncData(schema);
        }

        public void SyncData(string schemaId)
        {
            var s = FindSchema(schemaId);
            if (s != null) SyncData(s);
        }
        public void SyncData(string schemaId, string sourceEntity, string destEntity)
        {
            var s = FindSchema(schemaId);
            if (s != null) { s.SourceEntityName = sourceEntity; s.DestinationEntityName = destEntity; SyncData(s); }
        }
        public void SyncData(string schemaId, string sourceEntity, string destEntity, string sourceSyncField)
        {
            var s = FindSchema(schemaId);
            if (s != null) { s.SourceEntityName = sourceEntity; s.DestinationEntityName = destEntity; s.SourceSyncDataField = sourceSyncField; SyncData(s); }
        }
        public void SyncData(string schemaId, string sourceEntity, string destEntity, string sourceSyncField, string destSyncField)
        {
            var s = FindSchema(schemaId);
            if (s != null) { s.SourceEntityName = sourceEntity; s.DestinationEntityName = destEntity; s.SourceSyncDataField = sourceSyncField; s.DestinationSyncDataField = destSyncField; SyncData(s); }
        }
        public void SyncData(string schemaId, string sourceEntity, string destEntity, string sourceSyncField, string destSyncField, string syncType)
        {
            var s = FindSchema(schemaId);
            if (s != null) { s.SourceEntityName = sourceEntity; s.DestinationEntityName = destEntity; s.SourceSyncDataField = sourceSyncField; s.DestinationSyncDataField = destSyncField; s.SyncType = syncType; SyncData(s); }
        }
        public void SyncData(string schemaId, string sourceEntity, string destEntity, string sourceSyncField, string destSyncField, string syncType, string syncDirection)
        {
            var s = FindSchema(schemaId);
            if (s != null) { s.SourceEntityName = sourceEntity; s.DestinationEntityName = destEntity; s.SourceSyncDataField = sourceSyncField; s.DestinationSyncDataField = destSyncField; s.SyncType = syncType; s.SyncDirection = syncDirection; SyncData(s); }
        }

        public void AddSyncSchema(DataSyncSchema schema) => SyncSchemas.Add(schema);
        public void RemoveSyncSchema(string schemaId)
        {
            var schema = FindSchema(schemaId);
            if (schema != null) SyncSchemas.Remove(schema);
        }
        public void UpdateSyncSchema(DataSyncSchema schema)
        {
            var existing = FindSchema(schema?.Id);
            if (existing != null) SyncSchemas[SyncSchemas.IndexOf(existing)] = schema;
        }
        public void AddFilter(string schemaId, AppFilter filter) => FindSchema(schemaId)?.Filters.Add(filter);
        public void RemoveFilter(string schemaId, string fieldName)
        {
            var schema = FindSchema(schemaId);
            var filter = schema?.Filters?.FirstOrDefault(f => f.FieldName == fieldName);
            if (filter != null) schema.Filters.Remove(filter);
        }
        public void AddFieldMapping(string schemaId, FieldSyncData field) => FindSchema(schemaId)?.MappedFields.Add(field);

        public IErrorsInfo ValidateSchema(DataSyncSchema schema) => _validationHelper.ValidateSchema(schema);

        public async Task SaveSchemasAsync() => await _persistenceHelper.SaveSchemasAsync(SyncSchemas);
        public void SaveSchemas() => SaveSchemasAsync().GetAwaiter().GetResult();
        public async Task<ObservableBindingList<DataSyncSchema>> LoadSchemasAsync() => SyncSchemas = await _persistenceHelper.LoadSchemasAsync();
        public void LoadSchemas() => LoadSchemasAsync().GetAwaiter().GetResult();

        private DataSyncSchema FindSchema(string id) => string.IsNullOrEmpty(id) ? null : SyncSchemas?.FirstOrDefault(s => s.Id == id);
        private static IProgress<IPassedArgs> CreateProgressAdapter(IProgress<PassedArgs> progress) =>
            progress == null ? null : new Progress<IPassedArgs>(p => progress.Report(p as PassedArgs ?? new PassedArgs { Messege = p?.Messege }));

        private void LogSyncRun(DataSyncSchema schema)
        {
            if (schema?.SyncRuns == null) return;
            var run = new SyncRunData
            {
                SyncSchemaId = schema.Id,
                SyncDate = schema.LastSyncDate,
                SyncStatus = schema.SyncStatus,
                SyncStatusMessage = schema.SyncStatusMessage
            };
            schema.SyncRuns.Add(run);
            schema.LastSyncRunData = run;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;
            if (disposing) SyncSchemas?.Clear();
            _disposedValue = true;
        }
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
    }
}
