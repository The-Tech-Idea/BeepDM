using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Assistant for discovering and managing database drivers
    /// </summary>
    public class DriverDiscoveryAssistant : IDisposable
    {
        private readonly SharedContextManager _sharedContextManager;
        private readonly IDMLogger _logger;
        private readonly IConfigEditor _configEditor;
        private bool _disposed = false;

        public DriverDiscoveryAssistant(SharedContextManager sharedContextManager, IConfigEditor configEditor, IDMLogger logger)
        {
            _sharedContextManager = sharedContextManager;
            _configEditor = configEditor;
            _logger = logger;
        }

        /// <summary>
        /// Discovers drivers from an assembly
        /// </summary>
        public List<ConnectionDriversConfig> GetDrivers(Assembly assembly)
        {
            var driversFound = new List<ConnectionDriversConfig>();

            try
            {
                if (assembly?.GetType() != null)
                {
                    var adoDrivers = GetADOTypeDrivers(assembly);
                    driversFound.AddRange(adoDrivers);
                    
                    // Store discovered drivers in SharedContextManager instead of locally
                    _sharedContextManager.AddDiscoveredDrivers(adoDrivers);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to discover drivers from assembly: {assembly?.GetName()}", ex);
            }

            return driversFound;
        }

        /// <summary>
        /// Gets ADO.NET type drivers from assembly
        /// </summary>
        private List<ConnectionDriversConfig> GetADOTypeDrivers(Assembly assembly)
        {
            var drivers = new List<ConnectionDriversConfig>();

            if (assembly == null) return drivers;

            try
            {
                var driversConfigDict = new ConcurrentDictionary<string, ConnectionDriversConfig>();
                bool foundAnyDriverTypes = false;

                string[] assemblyNameParts = assembly.FullName.Split(',');
                string driverVersion = assemblyNameParts.Length > 1 ?
                    assemblyNameParts[1].Substring(assemblyNameParts[1].IndexOf("=") + 1) : "1.0";
                string packageName = assemblyNameParts[0];

                // Get all relevant driver types
                List<Type> relevantTypes;
                try
                {
                    relevantTypes = assembly.ExportedTypes?.ToList() ??
                        assembly.GetTypes()
                           .Where(type =>
                                typeof(IDbDataAdapter).IsAssignableFrom(type) ||
                                typeof(IDbConnection).IsAssignableFrom(type) ||
                                (type.BaseType != null && type.BaseType.ToString().Contains("DbCommandBuilder")) ||
                                typeof(IDbTransaction).IsAssignableFrom(type) ||
                                type.IsSubclassOf(typeof(DbConnection)) ||
                                type.IsSubclassOf(typeof(DbCommand)) ||
                                type.IsSubclassOf(typeof(DbDataReader)) ||
                                type.IsSubclassOf(typeof(DbParameter)) ||
                                type.IsSubclassOf(typeof(DbTransaction)))
                           .ToList();
                }
                catch
                {
                    return drivers;
                }

                // Handle special case for SqlClient
                if (relevantTypes == null || relevantTypes.Count == 0)
                {
                    if (packageName == "System.Data.SqlClient")
                    {
                        var sqlDriver = new ConnectionDriversConfig
                        {
                            version = driverVersion,
                            AdapterType = packageName + "." + "SqlDataAdapter",
                            DbConnectionType = packageName + "." + "SqlConnection",
                            CommandBuilderType = packageName + "." + "SqlCommandBuilder",
                            DbTransactionType = packageName + "." + "SqlTransaction",
                            PackageName = packageName,
                            DriverClass = packageName,
                            dllname = assembly.ManifestModule.Name,
                            ADOType = true
                        };
                        drivers.Add(sqlDriver);
                        return drivers;
                    }
                    return drivers;
                }

                // Process types in parallel for large sets
                if (relevantTypes.Count > 50)
                {
                    Parallel.ForEach(relevantTypes, type =>
                    {
                        if (type.BaseType == null) return;

                        TypeInfo typeInfo = type.GetTypeInfo();
                        if (ProcessDriverType(typeInfo, packageName, driverVersion, type.Module.Name, driversConfigDict))
                        {
                            foundAnyDriverTypes = true;
                        }
                    });
                }
                else
                {
                    foreach (var type in relevantTypes)
                    {
                        if (type.BaseType == null) continue;

                        TypeInfo typeInfo = type.GetTypeInfo();
                        if (ProcessDriverType(typeInfo, packageName, driverVersion, type.Module.Name, driversConfigDict))
                        {
                            foundAnyDriverTypes = true;
                        }
                    }
                }

                if (foundAnyDriverTypes)
                {
                    drivers.AddRange(driversConfigDict.Values);
                }

                return drivers;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to get ADO drivers from {assembly.GetName()}", ex);
                return drivers;
            }
        }

        /// <summary>
        /// Processes a driver type and updates the driver configuration
        /// </summary>
        private bool ProcessDriverType(TypeInfo typeInfo, string packageName, string version, string moduleName,
                              ConcurrentDictionary<string, ConnectionDriversConfig> driversConfigDict)
        {
            bool foundDriverComponent = false;

            var driverConfig = driversConfigDict.GetOrAdd(packageName, key => new ConnectionDriversConfig
            {
                PackageName = key,
                DriverClass = key,
                version = version,
                dllname = moduleName,
                ADOType = true
            });

            if (typeInfo.ImplementedInterfaces.Contains(typeof(IDbDataAdapter)))
            {
                driverConfig.AdapterType = typeInfo.FullName;
                foundDriverComponent = true;
            }
            else if (typeInfo.BaseType.ToString().Contains("DbCommandBuilder"))
            {
                driverConfig.CommandBuilderType = typeInfo.FullName;
                foundDriverComponent = true;
            }
            else if (typeInfo.ImplementedInterfaces.Contains(typeof(IDbConnection)) || typeof(DbConnection).IsAssignableFrom(typeInfo))
            {
                driverConfig.DbConnectionType = typeInfo.FullName;
                foundDriverComponent = true;
            }
            else if (typeInfo.ImplementedInterfaces.Contains(typeof(IDbTransaction)) || typeof(DbTransaction).IsAssignableFrom(typeInfo))
            {
                driverConfig.DbTransactionType = typeInfo.FullName;
                foundDriverComponent = true;
            }

            return foundDriverComponent;
        }

        /// <summary>
        /// Adds default engine drivers
        /// </summary>
        public bool AddEngineDefaultDrivers(List<AssemblyClassDefinition> dataSourcesClasses)
        {
            try
            {
                var defaultDrivers = new List<ConnectionDriversConfig>();
                
                var dataviewDriver = new ConnectionDriversConfig
                {
                    AdapterType = "DEFAULT",
                    dllname = "DataManagementEngine",
                    PackageName = "DataViewReader",
                    DriverClass = "DataViewReader",
                    version = "1"
                };
                defaultDrivers.Add(dataviewDriver);

                // Get File extensions
                var cls = dataSourcesClasses?.Where(o => o.classProperties != null)
                    .Where(p => p.classProperties.Category == DatasourceCategory.FILE).ToList();

                if (cls != null)
                {
                    foreach (var item in cls)
                    {
                        foreach (string extension in item.classProperties.FileType.Split(','))
                        {
                            var fileDriver = new ConnectionDriversConfig
                            {
                                AdapterType = "DEFAULT",
                                dllname = "DataManagementEngine",
                                PackageName = item.className,
                                DriverClass = item.className,
                                classHandler = item.className,
                                iconname = extension + ".ico",
                                extensionstoHandle = item.classProperties.FileType,
                                DatasourceCategory = DatasourceCategory.FILE,
                                version = "1"
                            };
                            defaultDrivers.Add(fileDriver);
                        }
                    }
                }

                // Store all default drivers in SharedContextManager
                _sharedContextManager.AddDiscoveredDrivers(defaultDrivers);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext("Failed to add engine default drivers", ex);
                return false;
            }
        }

        /// <summary>
        /// Creates file extension string from data sources
        /// </summary>
        public List<string> CreateFileExtensionString(List<AssemblyClassDefinition> dataSourcesClasses)
        {
            var cls = dataSourcesClasses?.Where(o => o.classProperties != null).ToList();
            var extensionsList = cls?.Where(o => o.classProperties.Category == DatasourceCategory.FILE)
                .Select(p => p.classProperties.FileType);
            
            if (extensionsList != null)
            {
                string extString = string.Join(",", extensionsList);
                return extString.Split(',').ToList();
            }

            return new List<string>();
        }

        /// <summary>
        /// Checks and adds drivers to configuration, avoiding duplicates
        /// </summary>
        public void CheckDriverAlreadyExistInList()
        {
            // Get all discovered drivers from SharedContextManager instead of local storage
            var allDiscoveredDrivers = _sharedContextManager.DiscoveredDrivers;
            
            foreach (var driver in allDiscoveredDrivers)
            {
                _configEditor?.AddDriver(driver);
            }
        }

        /// <summary>
        /// Gets all discovered drivers from SharedContextManager
        /// </summary>
        public List<ConnectionDriversConfig> GetDiscoveredDrivers()
        {
            return _sharedContextManager.DiscoveredDrivers;
        }

        /// <summary>
        /// Gets driver discovery statistics from SharedContextManager
        /// </summary>
        public Dictionary<string, object> GetDriverDiscoveryStatistics()
        {
            var allDrivers = _sharedContextManager.DiscoveredDrivers;
            var adoDrivers = allDrivers.Count(d => d.ADOType);
            var fileDrivers = allDrivers.Count(d => d.DatasourceCategory == DatasourceCategory.FILE);
            var customDrivers = allDrivers.Count(d => !d.ADOType && d.DatasourceCategory != DatasourceCategory.FILE);

            return new Dictionary<string, object>
            {
                ["TotalDrivers"] = allDrivers.Count,
                ["ADODrivers"] = adoDrivers,
                ["FileDrivers"] = fileDrivers,
                ["CustomDrivers"] = customDrivers,
                ["DriversByPackage"] = allDrivers.GroupBy(d => d.PackageName)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        /// <summary>
        /// Clears discovered drivers - now delegates to SharedContextManager
        /// </summary>
        public void ClearDiscoveredDrivers()
        {
            // Note: Individual assistants should not clear shared storage
            // This would be done by SharedContextManager when nuggets are unloaded
            _logger?.LogWithContext("ClearDiscoveredDrivers called - individual drivers are managed by SharedContextManager during nugget unloading", null);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // No need to clear discovered drivers - they're managed by SharedContextManager
                _disposed = true;
            }
        }
    }
}