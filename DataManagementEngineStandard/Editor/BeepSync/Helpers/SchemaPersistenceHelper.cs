using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor.BeepSync;
using TheTechIdea.Beep.Editor.BeepSync.Interfaces;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.BeepSync.Helpers
{
    /// <summary>
    /// Helper class for sync schema persistence operations
    /// Based on persistence patterns from DataSyncManager.SaveSchemas and LoadSchemas
    /// </summary>
    public class SchemaPersistenceHelper : ISchemaPersistenceHelper
    {
        private readonly IDMEEditor _editor;
        private readonly string _filePath;
        private readonly string _directoryPath;

        /// <summary>
        /// Initialize the schema persistence helper with editor and file paths
        /// </summary>
        /// <param name="editor">DME Editor instance</param>
        public SchemaPersistenceHelper(IDMEEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            
            // Set up file paths similar to DataSyncManager
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _directoryPath = Path.Combine(appDataPath, "TheTechIdea", "Beep", "BeepSyncManager");
            _filePath = Path.Combine(_directoryPath, "SyncSchemas.json");

            // Ensure directory exists
            EnsureDirectoryExists();
        }

        /// <summary>
        /// Save sync schemas to storage asynchronously
        /// Based on DataSyncManager.SaveSchemas method
        /// </summary>
        /// <param name="schemas">Collection of schemas to save</param>
        public async Task SaveSchemasAsync(IEnumerable<DataSyncSchema> schemas)
        {
            try
            {
                if (schemas == null)
                {
                    _editor.AddLogMessage("BeepSync", "Cannot save schemas: schemas collection is null", DateTime.Now, -1, "", Errors.Failed);
                    return;
                }

                EnsureDirectoryExists();

                // Convert to ObservableBindingList if needed (to maintain compatibility)
                ObservableBindingList<DataSyncSchema> schemaList;
                if (schemas is ObservableBindingList<DataSyncSchema> observableList)
                {
                    schemaList = observableList;
                }
                else
                {
                    schemaList = new ObservableBindingList<DataSyncSchema>();
                    foreach (var schema in schemas)
                        schemaList.Add(schema);
                }

                // Serialize to JSON with indented formatting
                var json = await Task.Run(() => JsonConvert.SerializeObject(schemaList, Formatting.Indented));
                
                // Write to file
                await File.WriteAllTextAsync(_filePath, json);

                _editor.AddLogMessage("BeepSync", $"Successfully saved {schemaList.Count} sync schema(s) to {_filePath}", DateTime.Now, -1, "", Errors.Ok);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error saving sync schemas: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Load sync schemas from storage asynchronously
        /// Based on DataSyncManager.LoadSchemas method
        /// </summary>
        /// <returns>Observable collection of loaded schemas</returns>
        public async Task<ObservableBindingList<DataSyncSchema>> LoadSchemasAsync()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    _editor.AddLogMessage("BeepSync", $"Sync schemas file not found at {_filePath}. Starting with empty collection.", DateTime.Now, -1, "", Errors.Ok);
                    return new ObservableBindingList<DataSyncSchema>();
                }

                // Read file content
                var json = await File.ReadAllTextAsync(_filePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    _editor.AddLogMessage("BeepSync", "Sync schemas file is empty. Starting with empty collection.", DateTime.Now, -1, "", Errors.Ok);
                    return new ObservableBindingList<DataSyncSchema>();
                }

                // Deserialize from JSON
                var schemas = await Task.Run(() => 
                    JsonConvert.DeserializeObject<ObservableBindingList<DataSyncSchema>>(json));

                if (schemas == null)
                {
                    _editor.AddLogMessage("BeepSync", "Failed to deserialize sync schemas. Starting with empty collection.", DateTime.Now, -1, "", Errors.Failed);
                    return new ObservableBindingList<DataSyncSchema>();
                }

                _editor.AddLogMessage("BeepSync", $"Successfully loaded {schemas.Count} sync schema(s) from {_filePath}", DateTime.Now, -1, "", Errors.Ok);
                return schemas;
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error loading sync schemas: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                
                // Return empty collection on error rather than throwing
                return new ObservableBindingList<DataSyncSchema>();
            }
        }

        /// <summary>
        /// Save a single schema to storage
        /// </summary>
        /// <param name="schema">Schema to save</param>
        public async Task SaveSchemaAsync(DataSyncSchema schema)
        {
            try
            {
                if (schema == null)
                {
                    _editor.AddLogMessage("BeepSync", "Cannot save schema: schema is null", DateTime.Now, -1, "", Errors.Failed);
                    return;
                }

                // Load existing schemas
                var schemas = await LoadSchemasAsync();

                // Find and replace existing schema or add new one
                var existingSchema = schemas.FirstOrDefault(s => s.Id == schema.Id);
                if (existingSchema != null)
                {
                    var index = schemas.IndexOf(existingSchema);
                    schemas[index] = schema;
                    _editor.AddLogMessage("BeepSync", $"Updated existing schema '{schema.Id}'", DateTime.Now, -1, "", Errors.Ok);
                }
                else
                {
                    schemas.Add(schema);
                    _editor.AddLogMessage("BeepSync", $"Added new schema '{schema.Id}'", DateTime.Now, -1, "", Errors.Ok);
                }

                // Save all schemas
                await SaveSchemasAsync(schemas);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error saving single schema '{schema?.Id}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Delete schema from storage
        /// </summary>
        /// <param name="schemaId">Id of schema to delete</param>
        public async Task DeleteSchemaAsync(string schemaId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(schemaId))
                {
                    _editor.AddLogMessage("BeepSync", "Cannot delete schema: schema Id is null or empty", DateTime.Now, -1, "", Errors.Failed);
                    return;
                }

                // Load existing schemas
                var schemas = await LoadSchemasAsync();

                // Find and remove schema
                var schemaToRemove = schemas.FirstOrDefault(s => s.Id == schemaId);
                if (schemaToRemove != null)
                {
                    schemas.Remove(schemaToRemove);
                    await SaveSchemasAsync(schemas);
                    _editor.AddLogMessage("BeepSync", $"Successfully deleted schema '{schemaId}'", DateTime.Now, -1, "", Errors.Ok);
                }
                else
                {
                    _editor.AddLogMessage("BeepSync", $"Schema '{schemaId}' not found for deletion", DateTime.Now, -1, "", Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error deleting schema '{schemaId}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Create backup of current schemas
        /// </summary>
        /// <returns>True if backup was successful</returns>
        public async Task<bool> CreateBackupAsync()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    _editor.AddLogMessage("BeepSync", "No schemas file exists to backup", DateTime.Now, -1, "", Errors.Ok);
                    return true;
                }

                var backupFileName = $"SyncSchemas_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var backupFilePath = Path.Combine(_directoryPath, backupFileName);

                await Task.Run(() => File.Copy(_filePath, backupFilePath));

                _editor.AddLogMessage("BeepSync", $"Successfully created backup: {backupFilePath}", DateTime.Now, -1, "", Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error creating backup: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Get file path where schemas are stored
        /// </summary>
        public string GetSchemasFilePath()
        {
            return _filePath;
        }

        /// <summary>
        /// Check if schemas file exists
        /// </summary>
        public bool SchemasFileExists()
        {
            return File.Exists(_filePath);
        }

        // ── Phase 2: Schema Versioning ────────────────────────────────────────────

        /// <inheritdoc cref="ISchemaPersistenceHelper.SaveVersionedSchemaAsync"/>
        public async Task SaveVersionedSchemaAsync(DataSyncSchema schema, SyncSchemaVersion version)
        {
            try
            {
                if (schema == null || version == null) return;

                var versionsDir = Path.Combine(_directoryPath, "versions", schema.Id);
                Directory.CreateDirectory(versionsDir);

                var fileName = $"v{version.Version:D4}.json";
                var filePath = Path.Combine(versionsDir, fileName);

                var json = await Task.Run(() => JsonConvert.SerializeObject(version, Formatting.Indented));
                await File.WriteAllTextAsync(filePath, json);

                _editor.AddLogMessage("BeepSync", $"Schema '{schema.Id}' version {version.Version} saved to {filePath}.", DateTime.Now, -1, "", Errors.Ok);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error saving versioned schema: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                throw;
            }
        }

        /// <inheritdoc cref="ISchemaPersistenceHelper.LoadSchemaVersionsAsync"/>
        public async Task<List<SyncSchemaVersion>> LoadSchemaVersionsAsync(string schemaId)
        {
            try
            {
                var versionsDir = Path.Combine(_directoryPath, "versions", schemaId);
                if (!Directory.Exists(versionsDir))
                    return new List<SyncSchemaVersion>();

                var versions = new List<SyncSchemaVersion>();
                foreach (var file in Directory.GetFiles(versionsDir, "v*.json").OrderByDescending(f => f))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var v = JsonConvert.DeserializeObject<SyncSchemaVersion>(json);
                        if (v != null) versions.Add(v);
                    }
                    catch { /* skip corrupt version entries */ }
                }

                return versions;
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error loading schema versions for '{schemaId}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return new List<SyncSchemaVersion>();
            }
        }

        /// <inheritdoc cref="ISchemaPersistenceHelper.DiffSchemaToPersistedAsync"/>
        public async Task<string> DiffSchemaToPersistedAsync(DataSyncSchema schema)
        {
            try
            {
                if (schema == null) return "Schema is null.";

                var versions = await LoadSchemaVersionsAsync(schema.Id);
                if (versions.Count == 0)
                    return "No persisted version found — this is a new schema.";

                var latest      = versions[0]; // newest first
                var currentHash = ComputeSchemaHash(schema);

                if (currentHash == latest.SchemaHash)
                    return string.Empty; // unchanged

                return $"Schema '{schema.Id}' has changed since v{latest.Version} " +
                       $"(saved {latest.SavedAt:u} by {latest.SavedBy}). " +
                       $"Current: {currentHash.Substring(0, 8)}…, persisted: {(latest.SchemaHash?.Length >= 8 ? latest.SchemaHash.Substring(0, 8) : latest.SchemaHash)}…";
            }
            catch (Exception ex)
            {
                return $"Diff error: {ex.Message}";
            }
        }

        private string ComputeSchemaHash(DataSyncSchema schema)
        {
            try
            {
                var fingerprint =
                    $"{schema.SourceDataSourceName}|{schema.DestinationDataSourceName}" +
                    $"|{schema.SourceEntityName}|{schema.DestinationEntityName}" +
                    $"|{schema.SyncDirection}|{schema.SyncType}";

                if (schema.MappedFields != null)
                    fingerprint += "|" + string.Join(",",
                        schema.MappedFields
                            .Select(f => $"{f.SourceField}:{f.DestinationField}")
                            .OrderBy(x => x));

                using var sha   = SHA256.Create();
                var bytes       = sha.ComputeHash(Encoding.UTF8.GetBytes(fingerprint));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
            catch
            {
                return Guid.NewGuid().ToString(); // fallback: always treat as changed
            }
        }

        /// <summary>
        /// Ensure the directory exists for storing schemas
        /// </summary>
        private void EnsureDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_directoryPath))
                {
                    Directory.CreateDirectory(_directoryPath);
                    _editor.AddLogMessage("BeepSync", $"Created schemas directory: {_directoryPath}", DateTime.Now, -1, "", Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error creating schemas directory: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                throw;
            }
        }

        // ── Phase 5: Checkpoint persistence ──────────────────────────────────────

        /// <inheritdoc cref="ISchemaPersistenceHelper.SaveCheckpointAsync"/>
        public async Task SaveCheckpointAsync(SyncCheckpoint checkpoint)
        {
            if (checkpoint == null) return;
            try
            {
                var dir  = Path.Combine(_directoryPath, "checkpoints");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var path = Path.Combine(dir, $"{checkpoint.SchemaId}.json");
                checkpoint.SavedAt = DateTime.UtcNow;
                var json = await Task.Run(() => JsonConvert.SerializeObject(checkpoint, Formatting.Indented));
                await File.WriteAllTextAsync(path, json);

                _editor.AddLogMessage("BeepSync",
                    $"Checkpoint saved for schema '{checkpoint.SchemaId}' at offset {checkpoint.ProcessedOffset}.",
                    DateTime.Now, -1, "", Errors.Ok);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync",
                    $"Error saving checkpoint for schema '{checkpoint?.SchemaId}': {ex.Message}",
                    DateTime.Now, -1, "", Errors.Failed);
            }
        }

        /// <inheritdoc cref="ISchemaPersistenceHelper.LoadCheckpointAsync"/>
        public async Task<SyncCheckpoint> LoadCheckpointAsync(string schemaId)
        {
            if (string.IsNullOrWhiteSpace(schemaId)) return null;
            try
            {
                var path = Path.Combine(_directoryPath, "checkpoints", $"{schemaId}.json");
                if (!File.Exists(path)) return null;

                var json = await File.ReadAllTextAsync(path);
                return await Task.Run(() => JsonConvert.DeserializeObject<SyncCheckpoint>(json));
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync",
                    $"Error loading checkpoint for schema '{schemaId}': {ex.Message}",
                    DateTime.Now, -1, "", Errors.Failed);
                return null;
            }
        }

        /// <inheritdoc cref="ISchemaPersistenceHelper.ClearCheckpointAsync"/>
        public async Task ClearCheckpointAsync(string schemaId)
        {
            if (string.IsNullOrWhiteSpace(schemaId)) return;
            try
            {
                var path = Path.Combine(_directoryPath, "checkpoints", $"{schemaId}.json");
                if (File.Exists(path))
                {
                    await Task.Run(() => File.Delete(path));
                    _editor.AddLogMessage("BeepSync",
                        $"Checkpoint cleared for schema '{schemaId}'.",
                        DateTime.Now, -1, "", Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync",
                    $"Error clearing checkpoint for schema '{schemaId}': {ex.Message}",
                    DateTime.Now, -1, "", Errors.Failed);
            }
        }
    }
}
