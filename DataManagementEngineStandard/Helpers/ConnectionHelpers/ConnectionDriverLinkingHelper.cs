using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Helpers.ConnectionHelpers
{
    /// <summary>
    /// Helper class for linking connections to their corresponding drivers and managing driver configurations.
    /// </summary>
    public static class ConnectionDriverLinkingHelper
    {
        /// <summary>
        /// Links a connection to its corresponding drivers in the configuration editor.
        /// </summary>
        /// <param name="cn">The connection properties.</param>
        /// <param name="configEditor">The configuration editor.</param>
        /// <returns>The connection drivers configuration.</returns>
        public static ConnectionDriversConfig LinkConnection2Drivers(IConnectionProperties cn, IConfigEditor configEditor)
        {
            if (cn == null || configEditor == null)
                return null;

            // First, try to match by exact package name and version
            var result = FindDriverByPackageNameAndVersion(cn, configEditor);
            if (result != null) return result;

            // Then, try to match by package name only
            result = FindDriverByPackageName(cn, configEditor);
            if (result != null) return result;

            // Next, try to match by data source type
            result = FindDriverByDataSourceType(cn, configEditor);
            if (result != null) return result;

            // Finally, for file-based connections, try to match by file extension
            if (cn.Category == DatasourceCategory.FILE)
            {
                result = FindDriverByFileExtension(cn, configEditor);
            }

            return result;
        }

        /// <summary>
        /// Finds a driver configuration by exact package name and version match.
        /// </summary>
        /// <param name="cn">The connection properties.</param>
        /// <param name="configEditor">The configuration editor.</param>
        /// <returns>The matching driver configuration or null.</returns>
        private static ConnectionDriversConfig FindDriverByPackageNameAndVersion(IConnectionProperties cn, IConfigEditor configEditor)
        {
            if (string.IsNullOrEmpty(cn.DriverName) || string.IsNullOrEmpty(cn.DriverVersion))
                return null;

            return configEditor.DataDriversClasses
                .FirstOrDefault(c => c.PackageName.Equals(cn.DriverName, StringComparison.InvariantCultureIgnoreCase) 
                                    && c.version == cn.DriverVersion);
        }

        /// <summary>
        /// Finds a driver configuration by package name only.
        /// </summary>
        /// <param name="cn">The connection properties.</param>
        /// <param name="configEditor">The configuration editor.</param>
        /// <returns>The matching driver configuration or null.</returns>
        private static ConnectionDriversConfig FindDriverByPackageName(IConnectionProperties cn, IConfigEditor configEditor)
        {
            if (string.IsNullOrEmpty(cn.DriverName))
                return null;

            return configEditor.DataDriversClasses
                .FirstOrDefault(c => c.PackageName.Equals(cn.DriverName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Finds a driver configuration by data source type.
        /// </summary>
        /// <param name="cn">The connection properties.</param>
        /// <param name="configEditor">The configuration editor.</param>
        /// <returns>The matching driver configuration or null.</returns>
        private static ConnectionDriversConfig FindDriverByDataSourceType(IConnectionProperties cn, IConfigEditor configEditor)
        {
            return configEditor.DataDriversClasses
                .FirstOrDefault(c => c.DatasourceType == cn.DatabaseType);
        }

        /// <summary>
        /// Finds a driver configuration by file extension for file-based connections.
        /// </summary>
        /// <param name="cn">The connection properties.</param>
        /// <param name="configEditor">The configuration editor.</param>
        /// <returns>The matching driver configuration or null.</returns>
        private static ConnectionDriversConfig FindDriverByFileExtension(IConnectionProperties cn, IConfigEditor configEditor)
        {
            if (string.IsNullOrEmpty(cn.FileName))
                return null;

            var driversWithExtensions = configEditor.DataDriversClasses
                .Where(p => !string.IsNullOrEmpty(p.extensionstoHandle))
                .ToList();

            if (!driversWithExtensions.Any())
                return null;

            string fileExtension = Path.GetExtension(cn.FileName).Replace(".", "").ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExtension))
                return null;

            return driversWithExtensions
                .FirstOrDefault(c => c.extensionstoHandle.ToLowerInvariant().Contains(fileExtension));
        }

        /// <summary>
        /// Gets all available driver configurations for a specific data source type.
        /// </summary>
        /// <param name="dataSourceType">The data source type to filter by.</param>
        /// <param name="configEditor">The configuration editor.</param>
        /// <returns>A list of matching driver configurations.</returns>
        public static List<ConnectionDriversConfig> GetDriversForDataSourceType(DataSourceType dataSourceType, IConfigEditor configEditor)
        {
            if (configEditor == null)
                return new List<ConnectionDriversConfig>();

            return configEditor.DataDriversClasses
                .Where(c => c.DatasourceType == dataSourceType)
                .ToList();
        }

        /// <summary>
        /// Gets all available driver configurations that support a specific file extension.
        /// </summary>
        /// <param name="fileExtension">The file extension (without the dot).</param>
        /// <param name="configEditor">The configuration editor.</param>
        /// <returns>A list of matching driver configurations.</returns>
        public static List<ConnectionDriversConfig> GetDriversForFileExtension(string fileExtension, IConfigEditor configEditor)
        {
            if (string.IsNullOrEmpty(fileExtension) || configEditor == null)
                return new List<ConnectionDriversConfig>();

            string normalizedExtension = fileExtension.Replace(".", "").ToLowerInvariant();

            return configEditor.DataDriversClasses
                .Where(c => !string.IsNullOrEmpty(c.extensionstoHandle) && 
                           c.extensionstoHandle.ToLowerInvariant().Contains(normalizedExtension))
                .ToList();
        }

        /// <summary>
        /// Gets all available driver configurations for a specific datasource category.
        /// </summary>
        /// <param name="category">The datasource category to filter by.</param>
        /// <param name="configEditor">The configuration editor.</param>
        /// <returns>A list of matching driver configurations.</returns>
        public static List<ConnectionDriversConfig> GetDriversForCategory(DatasourceCategory category, IConfigEditor configEditor)
        {
            if (configEditor == null)
                return new List<ConnectionDriversConfig>();

            return configEditor.DataDriversClasses
                .Where(c => c.DatasourceCategory == category)
                .ToList();
        }

        /// <summary>
        /// Validates if a driver configuration is compatible with the given connection properties.
        /// </summary>
        /// <param name="driverConfig">The driver configuration to validate.</param>
        /// <param name="connectionProperties">The connection properties to check against.</param>
        /// <returns>True if compatible, false otherwise.</returns>
        public static bool IsDriverCompatible(ConnectionDriversConfig driverConfig, IConnectionProperties connectionProperties)
        {
            if (driverConfig == null || connectionProperties == null)
                return false;

            // Check data source type compatibility
            if (driverConfig.DatasourceType != connectionProperties.DatabaseType)
                return false;

            // Check category compatibility
            if (driverConfig.DatasourceCategory != connectionProperties.Category)
                return false;

            // For file-based connections, check extension compatibility
            if (connectionProperties.Category == DatasourceCategory.FILE && 
                !string.IsNullOrEmpty(connectionProperties.FileName) &&
                !string.IsNullOrEmpty(driverConfig.extensionstoHandle))
            {
                string fileExtension = Path.GetExtension(connectionProperties.FileName).Replace(".", "").ToLowerInvariant();
                if (!string.IsNullOrEmpty(fileExtension) && 
                    !driverConfig.extensionstoHandle.ToLowerInvariant().Contains(fileExtension))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the best matching driver for the given connection properties.
        /// </summary>
        /// <param name="connectionProperties">The connection properties.</param>
        /// <param name="configEditor">The configuration editor.</param>
        /// <returns>The best matching driver configuration or null if none found.</returns>
        public static ConnectionDriversConfig GetBestMatchingDriver(IConnectionProperties connectionProperties, IConfigEditor configEditor)
        {
            if (connectionProperties == null || configEditor == null)
                return null;

            var allDrivers = configEditor.DataDriversClasses.ToList();
            var compatibleDrivers = allDrivers.Where(d => IsDriverCompatible(d, connectionProperties)).ToList();

            if (!compatibleDrivers.Any())
                return null;

            // Prioritize exact package name and version match
            if (!string.IsNullOrEmpty(connectionProperties.DriverName) && !string.IsNullOrEmpty(connectionProperties.DriverVersion))
            {
                var exactMatch = compatibleDrivers.FirstOrDefault(d => 
                    d.PackageName.Equals(connectionProperties.DriverName, StringComparison.InvariantCultureIgnoreCase) &&
                    d.version == connectionProperties.DriverVersion);
                if (exactMatch != null) return exactMatch;
            }

            // Then prioritize package name match
            if (!string.IsNullOrEmpty(connectionProperties.DriverName))
            {
                var packageMatch = compatibleDrivers.FirstOrDefault(d => 
                    d.PackageName.Equals(connectionProperties.DriverName, StringComparison.InvariantCultureIgnoreCase));
                if (packageMatch != null) return packageMatch;
            }

            // Finally, return the first compatible driver
            return compatibleDrivers.First();
        }
    }
}