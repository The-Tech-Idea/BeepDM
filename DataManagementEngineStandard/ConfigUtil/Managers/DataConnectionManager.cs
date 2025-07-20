using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ConfigUtil.Managers
{
    /// <summary>
    /// Manages data connections persistence and operations
    /// </summary>
    public class DataConnectionManager
    {
        private readonly IDMLogger _logger;
        private readonly IJsonLoader _jsonLoader;
        private readonly string _configPath;

        public List<ConnectionProperties> DataConnections { get; set; }

        public DataConnectionManager(IDMLogger logger, IJsonLoader jsonLoader, string configPath)
        {
            _logger = logger;
            _jsonLoader = jsonLoader;
            _configPath = configPath;
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
        /// Adds a data connection to the list.
        /// </summary>
        public bool AddDataConnection(ConnectionProperties cn)
        {
            try
            {
                if (cn == null || string.IsNullOrEmpty(cn.ConnectionName)) 
                    return false;

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
        /// Updates an existing data connection.
        /// </summary>
        public bool UpdateDataConnection(ConnectionProperties source, string targetGuidId)
        {
            try
            {
                if (source == null || string.IsNullOrWhiteSpace(source.ConnectionName))
                    return false;

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
        /// Removes a connection by name.
        /// </summary>
        public bool RemoveDataConnection(string connectionName)
        {
            try
            {
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
        /// Removes a connection by GUID.
        /// </summary>
        public bool RemoveConnByGuidID(string guidId)
        {
            if (DataConnections == null || string.IsNullOrEmpty(guidId))
                return false;

            int index = DataConnections.FindIndex(x => x.GuidID.Equals(guidId, StringComparison.InvariantCultureIgnoreCase));
            if (index < 0)
                return false;

            return DataConnections.Remove(DataConnections[index]);
        }

        /// <summary>
        /// Removes a connection by ID.
        /// </summary>
        public bool RemoveConnByID(int id)
        {
            if (DataConnections == null)
            {
                DataConnections = new List<ConnectionProperties>();
                return false;
            }

            int index = DataConnections.FindIndex(x => x.ID == id);
            return index >= 0 && DataConnections.Remove(DataConnections[index]);
        }

        /// <summary>
        /// Removes a connection by name (simple version).
        /// </summary>
        public bool RemoveConnByName(string name)
        {
            if (DataConnections == null)
            {
                DataConnections = new List<ConnectionProperties>();
                return false;
            }

            int index = DataConnections.FindIndex(x => x.ConnectionName.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return index >= 0 && DataConnections.Remove(DataConnections[index]);
        }

        /// <summary>
        /// Saves data connections to JSON file.
        /// </summary>
        public void SaveDataConnectionsValues()
        {
            try
            {
                string path = Path.Combine(_configPath, "DataConnections.json");
                _jsonLoader.Serialize(path, DataConnections);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error saving data connections: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads data connections from JSON file.
        /// </summary>
        public List<ConnectionProperties> LoadDataConnectionsValues()
        {
            try
            {
                string path = Path.Combine(_configPath, "DataConnections.json");
                if (File.Exists(path))
                {
                    DataConnections = _jsonLoader.DeserializeObject<ConnectionProperties>(path);
                }

                if (DataConnections == null)
                {
                    DataConnections = new List<ConnectionProperties>();
                }

                return DataConnections;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error loading data connections: {ex.Message}");
                DataConnections = new List<ConnectionProperties>();
                return DataConnections;
            }
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