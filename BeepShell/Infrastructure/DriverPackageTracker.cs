using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace BeepShell.Infrastructure
{
    /// <summary>
    /// Tracks installed driver packages to disk for persistence
    /// </summary>
    public class DriverPackageTracker
    {
        private readonly string _trackingFilePath;
        private Dictionary<DataSourceType, InstalledDriverInfo> _installedDrivers = new Dictionary<DataSourceType, InstalledDriverInfo>();

        public DriverPackageTracker(string appDataPath)
        {
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            
            _trackingFilePath = Path.Combine(appDataPath, "installed_drivers.json");
            LoadTracking();
        }

        public class InstalledDriverInfo
        {
            public string DataSourceTypeName { get; set; } = string.Empty;
            public string PackageName { get; set; } = string.Empty;
            public string PackageVersion { get; set; } = string.Empty;
            public string SourcePath { get; set; } = string.Empty;
            public DateTime InstalledDate { get; set; }
            public DateTime? LastUpdated { get; set; }
            public bool IsLoaded { get; set; }
        }

        private void LoadTracking()
        {
            try
            {
                if (File.Exists(_trackingFilePath))
                {
                    var json = File.ReadAllText(_trackingFilePath);
                    var storedDrivers = JsonSerializer.Deserialize<Dictionary<string, InstalledDriverInfo>>(json) 
                        ?? new Dictionary<string, InstalledDriverInfo>();
                    
                    // Convert string keys to DataSourceType enum
                    _installedDrivers = new Dictionary<DataSourceType, InstalledDriverInfo>();
                    foreach (var kvp in storedDrivers)
                    {
                        if (Enum.TryParse<DataSourceType>(kvp.Key, true, out var dsType))
                        {
                            _installedDrivers[dsType] = kvp.Value;
                        }
                    }
                }
                else
                {
                    _installedDrivers = new Dictionary<DataSourceType, InstalledDriverInfo>();
                }
            }
            catch
            {
                _installedDrivers = new Dictionary<DataSourceType, InstalledDriverInfo>();
            }
        }

        private void SaveTracking()
        {
            try
            {
                var directory = Path.GetDirectoryName(_trackingFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Convert enum keys to strings for JSON serialization
                var storedDrivers = new Dictionary<string, InstalledDriverInfo>();
                foreach (var kvp in _installedDrivers)
                {
                    storedDrivers[kvp.Key.ToString()] = kvp.Value;
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(storedDrivers, options);
                File.WriteAllText(_trackingFilePath, json);
            }
            catch
            {
                // Silent fail - tracking is not critical
            }
        }

        public void MarkAsInstalled(DataSourceType dataSourceType, string packageName, string sourcePath, string version = "")
        {
            if (_installedDrivers.ContainsKey(dataSourceType))
            {
                _installedDrivers[dataSourceType].LastUpdated = DateTime.UtcNow;
                _installedDrivers[dataSourceType].SourcePath = sourcePath;
                _installedDrivers[dataSourceType].PackageVersion = version;
                _installedDrivers[dataSourceType].IsLoaded = true;
            }
            else
            {
                _installedDrivers[dataSourceType] = new InstalledDriverInfo
                {
                    DataSourceTypeName = dataSourceType.ToString(),
                    PackageName = packageName,
                    PackageVersion = version,
                    SourcePath = sourcePath,
                    InstalledDate = DateTime.UtcNow,
                    IsLoaded = true
                };
            }

            SaveTracking();
        }

        public void MarkAsRemoved(DataSourceType dataSourceType)
        {
            if (_installedDrivers.ContainsKey(dataSourceType))
            {
                _installedDrivers[dataSourceType].IsLoaded = false;
                _installedDrivers[dataSourceType].LastUpdated = DateTime.UtcNow;
            }

            SaveTracking();
        }

        public bool IsInstalled(DataSourceType dataSourceType)
        {
            return _installedDrivers.ContainsKey(dataSourceType) && _installedDrivers[dataSourceType].IsLoaded;
        }

        public InstalledDriverInfo? GetInfo(DataSourceType dataSourceType)
        {
            return _installedDrivers.ContainsKey(dataSourceType) ? _installedDrivers[dataSourceType] : null;
        }

        public List<InstalledDriverInfo> GetAllInstalled()
        {
            return _installedDrivers.Values.Where(d => d.IsLoaded).ToList();
        }

        public List<InstalledDriverInfo> GetAll()
        {
            return _installedDrivers.Values.ToList();
        }

        public void Clear()
        {
            _installedDrivers.Clear();
            SaveTracking();
        }
    }
}
