using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ConfigUtil.Managers
{
    /// <summary>
    /// Manages migration history stored in the central configuration/metadata store.
    /// </summary>
    public class MigrationHistoryManager
    {
        private readonly IDMLogger _logger;
        private readonly IJsonLoader _jsonLoader;
        private readonly ConfigPathManager _pathManager;
        private ConfigandSettings _config;

        public ConfigandSettings Config
        {
            get => _config;
            set => _config = value;
        }

        public MigrationHistoryManager(IDMLogger logger, IJsonLoader jsonLoader, ConfigandSettings config, ConfigPathManager pathManager)
        {
            _logger = logger;
            _jsonLoader = jsonLoader;
            _config = config;
            _pathManager = pathManager;
        }

        public MigrationHistory Load(string dataSourceName)
        {
            try
            {
                var path = GetFilePath(dataSourceName);
                if (File.Exists(path))
                {
                    var loaded = _jsonLoader.DeserializeSingleObject<MigrationHistory>(path);
                    if (loaded != null)
                        return loaded;
                }

                return new MigrationHistory
                {
                    DataSourceName = dataSourceName ?? string.Empty,
                    Migrations = new List<MigrationRecord>()
                };
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error loading migration history for {dataSourceName}: {ex.Message}");
                return new MigrationHistory
                {
                    DataSourceName = dataSourceName ?? string.Empty,
                    Migrations = new List<MigrationRecord>()
                };
            }
        }

        public void Save(MigrationHistory history)
        {
            if (history == null)
                return;

            try
            {
                var path = GetFilePath(history.DataSourceName);
                _jsonLoader.Serialize(path, history);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error saving migration history for {history?.DataSourceName}: {ex.Message}");
            }
        }

        public void AppendRecord(string dataSourceName, DataSourceType dataSourceType, MigrationRecord record)
        {
            if (record == null)
                return;

            try
            {
                var history = Load(dataSourceName);
                history.DataSourceName = dataSourceName ?? string.Empty;
                history.DataSourceType = dataSourceType;
                history.Migrations ??= new List<MigrationRecord>();
                history.Migrations.Add(record);
                Save(history);
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error appending migration record for {dataSourceName}: {ex.Message}");
            }
        }

        private string GetFilePath(string dataSourceName)
        {
            var basePath = _config?.ConfigPath;
            if (string.IsNullOrWhiteSpace(basePath))
                basePath = _config?.ExePath;

            if (string.IsNullOrWhiteSpace(basePath))
                basePath = AppDomain.CurrentDomain.BaseDirectory;

            var migrationsDir = Path.Combine(basePath, "Migrations");
            if (_pathManager != null)
            {
                _pathManager.CreateDir(migrationsDir);
            }
            else
            {
                Directory.CreateDirectory(migrationsDir);
            }

            var name = SanitizeFileName(string.IsNullOrWhiteSpace(dataSourceName) ? "default" : dataSourceName);
            return Path.Combine(migrationsDir, $"{name}_migrations.json");
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "default";

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (var ch in name)
            {
                sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
            }
            return sb.ToString();
        }
    }
}
