using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
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
                var existingSchema = schemas.FirstOrDefault(s => s.ID == schema.ID);
                if (existingSchema != null)
                {
                    var index = schemas.IndexOf(existingSchema);
                    schemas[index] = schema;
                    _editor.AddLogMessage("BeepSync", $"Updated existing schema '{schema.ID}'", DateTime.Now, -1, "", Errors.Ok);
                }
                else
                {
                    schemas.Add(schema);
                    _editor.AddLogMessage("BeepSync", $"Added new schema '{schema.ID}'", DateTime.Now, -1, "", Errors.Ok);
                }

                // Save all schemas
                await SaveSchemasAsync(schemas);
            }
            catch (Exception ex)
            {
                _editor.AddLogMessage("BeepSync", $"Error saving single schema '{schema?.ID}': {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Delete schema from storage
        /// </summary>
        /// <param name="schemaId">ID of schema to delete</param>
        public async Task DeleteSchemaAsync(string schemaId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(schemaId))
                {
                    _editor.AddLogMessage("BeepSync", "Cannot delete schema: schema ID is null or empty", DateTime.Now, -1, "", Errors.Failed);
                    return;
                }

                // Load existing schemas
                var schemas = await LoadSchemasAsync();

                // Find and remove schema
                var schemaToRemove = schemas.FirstOrDefault(s => s.ID == schemaId);
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
    }
}
