using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools
{
    /// <summary>
    /// AssemblyHandler partial class - Driver Package Tracking
    /// Tracks which NuGet packages provide which drivers.
    /// </summary>
    public partial class AssemblyHandler
    {
        #region Driver Tracking Fields

        private List<DriverPackageMapping> _driverPackageMappings;
        private string _driverMappingsFilePath;
        private readonly object _driverMappingsLock = new object();

        private const string DriverMappingsFileName = "driver_packages.json";

        #endregion

        #region Driver Package Tracking

        /// <summary>
        /// Tracks a driver to its NuGet package source.
        /// </summary>
        public void TrackDriverPackage(string packageId, string version, string driverClassName, DataSourceType dsType)
        {
            if (string.IsNullOrWhiteSpace(packageId)) return;
            if (string.IsNullOrWhiteSpace(driverClassName)) return;

            EnsureDriverMappingsLoaded();
            lock (_driverMappingsLock)
            {
                var existing = _driverPackageMappings.FirstOrDefault(m =>
                    m.DriverClassName.Equals(driverClassName, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.PackageId = packageId;
                    existing.Version = version;
                    existing.DataSourceType = dsType;
                }
                else
                {
                    _driverPackageMappings.Add(new DriverPackageMapping
                    {
                        PackageId = packageId,
                        Version = version,
                        DriverClassName = driverClassName,
                        DataSourceType = dsType,
                        InstalledDate = DateTime.UtcNow
                    });
                }
            }
            SaveDriverMappings();
        }

        /// <summary>
        /// Removes tracking for a NuGet package.
        /// </summary>
        public void UntrackDriverPackage(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId)) return;

            EnsureDriverMappingsLoaded();
            lock (_driverMappingsLock)
            {
                _driverPackageMappings.RemoveAll(m =>
                    m.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase));
            }
            SaveDriverMappings();
        }

        /// <summary>
        /// Gets the package mapping for a specific driver class.
        /// </summary>
        public DriverPackageMapping GetDriverPackageMapping(string driverClassName)
        {
            if (string.IsNullOrWhiteSpace(driverClassName)) return null;

            EnsureDriverMappingsLoaded();
            lock (_driverMappingsLock)
            {
                return _driverPackageMappings.FirstOrDefault(m =>
                    m.DriverClassName.Equals(driverClassName, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Gets all driver-to-package mappings.
        /// </summary>
        public List<DriverPackageMapping> GetAllDriverPackageMappings()
        {
            EnsureDriverMappingsLoaded();
            lock (_driverMappingsLock)
            {
                return _driverPackageMappings.ToList();
            }
        }

        /// <summary>
        /// Checks whether a driver class was installed from a NuGet package.
        /// </summary>
        public bool IsDriverFromNuGet(string driverClassName)
        {
            return GetDriverPackageMapping(driverClassName) != null;
        }

        #endregion

        #region Driver Mapping Persistence

        private void SaveDriverMappings()
        {
            try
            {
                EnsureDriverMappingsFilePath();
                lock (_driverMappingsLock)
                {
                    var json = JsonConvert.SerializeObject(_driverPackageMappings, Formatting.Indented);
                    File.WriteAllText(_driverMappingsFilePath, json);
                }
                Logger?.WriteLog($"SaveDriverMappings: Saved {_driverPackageMappings.Count} mappings to {_driverMappingsFilePath}");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"SaveDriverMappings: Error - {ex.Message}");
            }
        }

        private void LoadDriverMappings()
        {
            try
            {
                EnsureDriverMappingsFilePath();
                if (File.Exists(_driverMappingsFilePath))
                {
                    var json = File.ReadAllText(_driverMappingsFilePath);
                    _driverPackageMappings = JsonConvert.DeserializeObject<List<DriverPackageMapping>>(json) ?? new List<DriverPackageMapping>();
                }
                else
                {
                    _driverPackageMappings = new List<DriverPackageMapping>();
                }
                Logger?.WriteLog($"LoadDriverMappings: Loaded {_driverPackageMappings.Count} mappings");
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LoadDriverMappings: Error - {ex.Message}");
                _driverPackageMappings = new List<DriverPackageMapping>();
            }
        }

        private void EnsureDriverMappingsLoaded()
        {
            if (_driverPackageMappings == null)
            {
                LoadDriverMappings();
            }
        }

        private void EnsureDriverMappingsFilePath()
        {
            if (string.IsNullOrEmpty(_driverMappingsFilePath))
            {
                var configPath = ConfigEditor?.ExePath ?? AppContext.BaseDirectory;
                _driverMappingsFilePath = Path.Combine(configPath, DriverMappingsFileName);
            }
        }

        #endregion
    }
}
