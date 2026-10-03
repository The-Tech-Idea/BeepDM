using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync.Interfaces;
using TheTechIdea.Beep.Services.Persistence;

namespace TheTechIdea.Beep.Editor.BeepSync.Helpers
{
    /// <summary>Versioned local snapshots for cooperating sync writers. Corruption is never fresh state.</summary>
    public partial class SchemaPersistenceHelper : ISchemaPersistenceHelper, ISyncPersistenceAcknowledgement
    {
        private readonly IDMEEditor _editor;
        private readonly string _filePath;
        private readonly string _directoryPath;

        public SchemaPersistenceHelper(IDMEEditor editor) : this(editor, DefaultDirectory(editor)) { }
        public SchemaPersistenceHelper(IDMEEditor editor, string storageDirectory)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);
            _directoryPath = Path.GetFullPath(storageDirectory);
            _filePath = Path.Combine(_directoryPath, "SyncSchemas.json");
            Directory.CreateDirectory(_directoryPath);
        }
        private static string DefaultDirectory(IDMEEditor editor)
        {
            ArgumentNullException.ThrowIfNull(editor);
            var legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TheTechIdea", "Beep", "BeepSyncManager");
            var configured = editor.ConfigEditor?.ConfigPath;
            if (string.IsNullOrWhiteSpace(configured)) return legacy;
            var root = Path.Combine(Path.GetFullPath(configured), "BeepSync");
            if (!Directory.Exists(root) && Directory.Exists(legacy) && Directory.EnumerateFiles(legacy, "*.json", SearchOption.AllDirectories).Any())
                throw new InvalidDataException("A shared legacy sync store exists. Select its ownership and migration explicitly before starting a configured store.");
            return root;
        }
        public Task SaveSchemasAsync(IEnumerable<DataSyncSchema> schemas) => SaveSchemasAsync(schemas, default);
        public Task SaveSchemasAsync(IEnumerable<DataSyncSchema> schemas, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(schemas);
            var snapshot = Clone(schemas.ToList());
            ValidateSchemas(snapshot);
            var serialized = WriteSchemas(snapshot);
            return ObserveAsync(() => AtomicFileStore.UpdateTextAsync(_filePath, current =>
            {
                if (current != null) ReadSchemas(current);
                return serialized; // Explicit replacement, not a merge of stale snapshots.
            }, token));
        }
        public Task<ObservableBindingList<DataSyncSchema>> LoadSchemasAsync() => LoadSchemasAsync(default);
        public async Task<ObservableBindingList<DataSyncSchema>> LoadSchemasAsync(CancellationToken token)
        {
            var json = await AtomicFileStore.ReadTextAsync(_filePath, token).ConfigureAwait(false);
            return new ObservableBindingList<DataSyncSchema>(json == null ? new List<DataSyncSchema>() : ReadSchemas(json));
        }
        public Task SaveSchemaAsync(DataSyncSchema schema) => SaveSchemaAsync(schema, default);
        public Task SaveSchemaAsync(DataSyncSchema schema, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(schema);
            var snapshot = Clone(schema);
            ValidateSchemas(new[] { snapshot });
            return ObserveAsync(() => AtomicFileStore.UpdateTextAsync(_filePath, current =>
            {
                var schemas = current == null ? new List<DataSyncSchema>() : ReadSchemas(current);
                var index = schemas.FindIndex(item => item.Id == snapshot.Id);
                if (index < 0) schemas.Add(snapshot); else schemas[index] = snapshot;
                return WriteSchemas(schemas);
            }, token));
        }
        public Task DeleteSchemaAsync(string schemaId) => DeleteSchemaAsync(schemaId, default);
        public Task DeleteSchemaAsync(string schemaId, CancellationToken token)
        {
            ValidateId(schemaId);
            return ObserveAsync(() => AtomicFileStore.UpdateTextAsync(_filePath, current =>
            {
                if (current == null) return null;
                var schemas = ReadSchemas(current);
                return schemas.RemoveAll(item => item.Id == schemaId) == 0 ? current : WriteSchemas(schemas);
            }, token));
        }
        public Task<bool> CreateBackupAsync() => CreateBackupAsync(default);
        public async Task<bool> CreateBackupAsync(CancellationToken token)
        {
            try
            {
                var json = await AtomicFileStore.ReadTextAsync(_filePath, token).ConfigureAwait(false);
                if (json == null) return true;
                ReadSchemas(json);
                var path = Path.Combine(_directoryPath, $"SyncSchemas_Backup_{DateTime.UtcNow:yyyyMMdd_HHmmss_fffffff}_{Guid.NewGuid():N}.json");
                await AtomicFileStore.WriteTextAsync(path, json, token).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { Report(ex); return false; }
        }
        public string GetSchemasFilePath() => _filePath;
        public bool SchemasFileExists() => File.Exists(_filePath);
        public Task<PersistenceWriteResult> SaveSchemasAcknowledgedAsync(IEnumerable<DataSyncSchema> schemas, CancellationToken token = default) =>
            AcknowledgeAsync(() => SaveSchemasAsync(schemas, token));
        public Task<PersistenceWriteResult> SaveSchemaAcknowledgedAsync(DataSyncSchema schema, CancellationToken token = default) =>
            AcknowledgeAsync(() => SaveSchemaAsync(schema, token));
        public Task<PersistenceWriteResult> SaveCheckpointAcknowledgedAsync(SyncCheckpoint checkpoint, CancellationToken token = default) =>
            AcknowledgeAsync(() => SaveCheckpointAsync(checkpoint, token));
        private async Task<PersistenceWriteResult> AcknowledgeAsync(Func<Task> operation)
        {
            try { await operation().ConfigureAwait(false); return new PersistenceWriteResult(PersistenceWriteStatus.Saved); }
            catch (OperationCanceledException ex) { return new PersistenceWriteResult(PersistenceWriteStatus.Cancelled, ex); }
            catch (Exception ex) { Report(ex); return new PersistenceWriteResult(ex is NotSupportedException ? PersistenceWriteStatus.Unsupported : PersistenceWriteStatus.Failed, ex); }
        }
        private async Task ObserveAsync(Func<Task> operation)
        {
            try { await operation().ConfigureAwait(false); }
            catch (Exception ex) { Report(ex); throw; }
        }
        private void Report(Exception ex)
        {
            try { _editor.AddLogMessage("BeepSync", $"Sync persistence failed ({ex.GetType().Name}).", DateTime.Now, -1, "", Errors.Failed); }
            catch (Exception loggerError) { System.Diagnostics.Debug.WriteLine($"Sync persistence logging failed ({loggerError.GetType().Name})."); }
        }
    }
}
