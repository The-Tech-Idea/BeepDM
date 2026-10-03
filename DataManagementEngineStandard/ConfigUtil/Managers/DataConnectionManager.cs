using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.Security;
using TheTechIdea.Beep.Services.Persistence;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ConfigUtil.Managers
{
    /// <summary>
    /// Manages data connections persistence and operations.
    /// When a <see cref="IConnectionCatalogRepository"/> is set (by the host),
    /// all CRUD and persistence delegates to it — making the catalog the
    /// single source of truth. When null (legacy/design-time mode), the
    /// file-based DataConnections.json store is used directly.
    /// </summary>
    public class DataConnectionManager
    {
        private readonly IDMLogger _logger;
        private readonly IJsonLoader _jsonLoader;
        private readonly string _configPath;
        private readonly IConnectionSecretProtector _protector;

        public List<ConnectionProperties> DataConnections { get; set; }

        /// <summary>
        /// When set by the host (BeepService/DMEEditor), all CRUD and persistence
        /// operations delegate to this catalog repository. When null, the legacy
        /// file-based store (DataConnections.json) is used.
        /// </summary>
        public IConnectionCatalogRepository? CatalogRepository { get; set; }

        public DataConnectionManager(IDMLogger logger, IJsonLoader jsonLoader, string configPath)
            : this(logger, jsonLoader, configPath, ConnectionCredentialProtection.Default)
        { }

        public DataConnectionManager(IDMLogger logger, IJsonLoader jsonLoader, string configPath, IConnectionSecretProtector protector)
        {
            _logger = logger;
            _jsonLoader = jsonLoader;
            _configPath = configPath;
            _protector = protector ?? throw new ArgumentNullException(nameof(protector));
            DataConnections = new List<ConnectionProperties>();
        }

        /// <summary>
        /// Checks if a data connection exists.
        /// </summary>
        public bool DataConnectionExist(ConnectionProperties cn)
        {
            if (DataConnections == null)
            {
                DataConnections = new List<ConnectionProperties>();
                return false;
            }

            if (cn != null)
            {
                if (cn.Category == DatasourceCategory.FILE)
                {
                    string filepath = Path.Combine(cn.FilePath, cn.FileName);
                    return DataConnections.Any(x => x.Category == DatasourceCategory.FILE && 
                        !string.IsNullOrEmpty(x.FilePath) && !string.IsNullOrEmpty(x.FileName) && 
                        Path.Combine(x.FilePath, x.FileName).Equals(filepath, StringComparison.InvariantCultureIgnoreCase));
                }
                else
                {
                    return DataConnections.Any(x => x.ConnectionName.Equals(cn.ConnectionName, StringComparison.InvariantCultureIgnoreCase));
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a data connection with the specified name exists.
        /// </summary>
        public bool DataConnectionExist(string connectionName)
        {
            if (DataConnections == null)
            {
                DataConnections = new List<ConnectionProperties>();
                return false;
            }

            return DataConnections.Any(x => !string.IsNullOrEmpty(x.ConnectionName) && 
                x.ConnectionName.Equals(connectionName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Checks if a data connection with the specified GUID exists.
        /// </summary>
        public bool DataConnectionGuidExist(string guidId)
        {
            if (DataConnections == null)
            {
                DataConnections = new List<ConnectionProperties>();
                return false;
            }

            return DataConnections.Any(x => x.GuidID.Equals(guidId, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Adds a data connection. Delegates to the catalog repository when set.
        /// </summary>
        public bool AddDataConnection(ConnectionProperties cn)
        {
            try
            {
                if (cn == null || string.IsNullOrEmpty(cn.ConnectionName)) 
                    return false;

                if (CatalogRepository != null)
                {
                    var changed = CatalogRepository.AddOrUpdate(cn, persist: true);
                    if (changed)
                    {
                        SyncFromRepository();
                    }
                    return changed;
                }

                if (DataConnections == null)
                {
                    DataConnections = new List<ConnectionProperties>();
                }

                if (!DataConnectionExist(cn.ConnectionName))
                {
                    if (cn.ID <= 0)
                    {
                        cn.ID = DataConnections.Count == 0 ? 1 : DataConnections.Max(p => p.ID) + 1;
                    }

                    DataConnections.Add(cn);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error adding data connection: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates an existing data connection. Delegates to the catalog repository when set.
        /// </summary>
        public bool UpdateDataConnection(ConnectionProperties source, string targetGuidId)
        {
            try
            {
                if (source == null || string.IsNullOrWhiteSpace(source.ConnectionName))
                    return false;

                if (CatalogRepository != null)
                {
                    var changed = CatalogRepository.AddOrUpdate(source, persist: true);
                    if (changed)
                    {
                        SyncFromRepository();
                    }
                    return changed;
                }

                if (DataConnections == null)
                    DataConnections = new List<ConnectionProperties>();

                var existing = DataConnections.Find(conn =>
                    conn.GuidID.Equals(targetGuidId, StringComparison.InvariantCultureIgnoreCase));

                if (existing != null)
                {
                    CopyConnectionProperties(source, existing);
                }
                else
                {
                    DataConnections.Add(source);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error updating data connection: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes a connection by name. Delegates to the catalog repository when set.
        /// </summary>
        public bool RemoveDataConnection(string connectionName)
        {
            try
            {
                if (CatalogRepository != null)
                {
                    var removed = CatalogRepository.Remove(connectionName, persist: true);
                    if (removed)
                    {
                        SyncFromRepository();
                    }
                    return removed;
                }

                if (DataConnections == null)
                {
                    DataConnections = new List<ConnectionProperties>();
                    return false;
                }

                var connection = DataConnections.FirstOrDefault(x => 
                    !string.IsNullOrEmpty(x.ConnectionName) && 
                    x.ConnectionName.Equals(connectionName, StringComparison.InvariantCultureIgnoreCase));

                if (connection != null)
                {
                    DataConnections.Remove(connection);
                    SaveDataConnectionsValues();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error removing data connection: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes a connection by GUID. Delegates to the catalog repository when set.
        /// </summary>
        public bool RemoveConnByGuidID(string guidId)
        {
            if (CatalogRepository != null)
            {
                var existing = DataConnections?.FirstOrDefault(c =>
                    c.GuidID.Equals(guidId, StringComparison.InvariantCultureIgnoreCase));
                if (existing == null) return false;
                var removed = CatalogRepository.Remove(existing.ConnectionName, persist: true);
                if (removed) SyncFromRepository();
                return removed;
            }

            if (DataConnections == null || string.IsNullOrEmpty(guidId))
                return false;

            int index = DataConnections.FindIndex(x => x.GuidID.Equals(guidId, StringComparison.InvariantCultureIgnoreCase));
            if (index < 0)
                return false;

            return DataConnections.Remove(DataConnections[index]);
        }

        /// <summary>
        /// Removes a connection by ID. Delegates to the catalog repository when set.
        /// </summary>
        public bool RemoveConnByID(int id)
        {
            if (CatalogRepository != null)
            {
                var existing = DataConnections?.FirstOrDefault(c => c.ID == id);
                if (existing == null) return false;
                var removed = CatalogRepository.Remove(existing.ConnectionName, persist: true);
                if (removed) SyncFromRepository();
                return removed;
            }

            if (DataConnections == null)
            {
                DataConnections = new List<ConnectionProperties>();
                return false;
            }

            int index = DataConnections.FindIndex(x => x.ID == id);
            return index >= 0 && DataConnections.Remove(DataConnections[index]);
        }

        /// <summary>
        /// Removes a connection by name (simple version). Delegates to the catalog repository when set.
        /// </summary>
        public bool RemoveConnByName(string name)
        {
            if (CatalogRepository != null)
            {
                var removed = CatalogRepository.Remove(name, persist: true);
                if (removed) SyncFromRepository();
                return removed;
            }

            if (DataConnections == null)
            {
                DataConnections = new List<ConnectionProperties>();
                return false;
            }

            int index = DataConnections.FindIndex(x => x.ConnectionName.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return index >= 0 && DataConnections.Remove(DataConnections[index]);
        }

        /// <summary>Legacy signature retained; failed saves now propagate instead of masquerading as success.</summary>
        public void SaveDataConnectionsValues() => SaveDataConnectionsAcknowledged().ThrowIfNotSaved();

        public PersistenceWriteResult SaveDataConnectionsAcknowledged(CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                var connections = (DataConnections ?? new List<ConnectionProperties>()).ToArray();
                if (CatalogRepository != null)
                {
                    return CatalogRepository.Save(connections)
                        ? new PersistenceWriteResult(PersistenceWriteStatus.Saved)
                        : new PersistenceWriteResult(PersistenceWriteStatus.Failed, new IOException("Connection catalog did not acknowledge the save."));
                }

                var codec = _jsonLoader as IJsonSnapshotCodec
                    ?? throw new NotSupportedException("Protected connection persistence requires an IJsonSnapshotCodec loader.");
                var protectedConnections = connections.Select(connection =>
                {
                    ValidateConnection(connection);
                    return _protector.Protect(connection);
                }).ToList();
                var serialized = codec.SerializeSnapshot(protectedConnections);
                string path = Path.Combine(_configPath, "DataConnections.json");
                AtomicFileStore.UpdateText(path, current =>
                {
                    // A stale snapshot is explicit replacement, never permission to reset corrupt evidence.
                    if (current != null) ParseConnections(current, codec);
                    return serialized;
                }, token);
                return new PersistenceWriteResult(PersistenceWriteStatus.Saved);
            }
            catch (OperationCanceledException ex)
            {
                return new PersistenceWriteResult(PersistenceWriteStatus.Cancelled, ex);
            }
            catch (Exception ex)
            {
                ReportPersistenceError(ex);
                return new PersistenceWriteResult(ex is NotSupportedException
                    ? PersistenceWriteStatus.Unsupported : PersistenceWriteStatus.Failed, ex);
            }
        }

        /// <summary>Refreshes only after successful validation/decryption; corruption never resets live state.</summary>
        public List<ConnectionProperties> LoadDataConnectionsValues()
        {
            try
            {
                if (CatalogRepository != null)
                {
                    var loaded = CatalogRepository.LoadConnections()
                        ?? throw new InvalidDataException("Connection catalog returned no snapshot.");
                    DataConnections = loaded.ToList();
                    return DataConnections;
                }

                string path = Path.Combine(_configPath, "DataConnections.json");
                var json = AtomicFileStore.ReadText(path);
                var connections = json == null ? new List<ConnectionProperties>() :
                    ParseConnections(json, _jsonLoader as IJsonSnapshotCodec
                        ?? throw new NotSupportedException("Protected connection persistence requires an IJsonSnapshotCodec loader."));
                DataConnections = connections;
                return DataConnections;
            }
            catch (Exception ex)
            {
                ReportPersistenceError(ex);
                throw;
            }
        }

        private List<ConnectionProperties> ParseConnections(string json, IJsonSnapshotCodec codec)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("Existing connection configuration is empty.");
            using var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = 64, DateParseHandling = DateParseHandling.None };
            var document = JArray.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (reader.Read()) throw new InvalidDataException("Connection configuration contains additional JSON content.");
            var connections = codec.DeserializeSnapshot<List<ConnectionProperties>>(json)
                ?? throw new InvalidDataException("Connection configuration is missing its records.");
            var result = new List<ConnectionProperties>(connections.Count);
            foreach (var connection in connections)
            {
                ValidateConnection(connection);
                result.Add(_protector.Unprotect(connection));
            }
            return result;
        }

        private static void ValidateConnection(ConnectionProperties connection)
        {
            if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionName))
                throw new InvalidDataException("A connection record must have an identity.");
        }

        private void ReportPersistenceError(Exception ex)
        {
            try { _logger?.WriteLog($"Connection persistence failed ({ex.GetType().Name})."); }
            catch (Exception logError) { System.Diagnostics.Debug.WriteLine($"Connection persistence logging failed ({logError.GetType().Name})."); }
        }

        /// <summary>
        /// Syncs the in-memory list from the catalog repository.
        /// </summary>
        private void SyncFromRepository()
        {
            if (CatalogRepository == null) return;
            DataConnections = CatalogRepository.LoadConnections().ToList();
        }

        /// <summary>
        /// Copies properties from source to target connection.
        /// </summary>
        private void CopyConnectionProperties(ConnectionProperties source, ConnectionProperties target)
        {
            var properties = typeof(ConnectionProperties).GetProperties()
                .Where(p => p.CanWrite && p.CanRead);

            foreach (var prop in properties)
            {
                var value = prop.GetValue(source);
                prop.SetValue(target, value);
            }
        }
    }
}
