using TheTechIdea.Beep.DataBase;
using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Utilities;
using System.Linq;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers.DataTypesHelpers;
using TheTechIdea.Beep.ConfigUtil;
using System.Collections.Concurrent;
using System.Threading;

namespace TheTechIdea.Beep.Helpers
{
    /// <summary>
    /// Optimized helper class for mapping data types to field names with caching and enhanced functionality.
    /// </summary>
    public class DataTypesHelper : IDataTypesHelper
    {
        #region Private Fields
        private readonly ConcurrentDictionary<string, List<DatatypeMapping>> _mappingCache;
        private readonly ConcurrentDictionary<string, string[]> _netTypesCache;
        private readonly object _lockObject = new object();
        private volatile bool _isInitialized = false;
        private bool _disposedValue;
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance of the DataTypesHelper class.</summary>
        /// <param name="pDMEEditor">The IDMEEditor instance to be associated with the helper.</param>
        public DataTypesHelper(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor ?? throw new ArgumentNullException(nameof(pDMEEditor));
            _mappingCache = new ConcurrentDictionary<string, List<DatatypeMapping>>();
            _netTypesCache = new ConcurrentDictionary<string, string[]>();
            DefaultStringSize = 250;
            
            InitializeAsync();
        }
        #endregion

        #region Public Properties
        /// <summary>Gets or sets the DME editor.</summary>
        /// <value>The DME editor.</value>
        public IDMEEditor DMEEditor { get; set; }

        /// <summary>Gets or sets the list of datatype mappings.</summary>
        /// <value>The list of datatype mappings.</value>
        public List<DatatypeMapping> mapping { get; set; }

        /// <summary>Gets or sets the default string size for database fields.</summary>
        /// <value>The default string size.</value>
        public int DefaultStringSize { get; set; }
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the helper asynchronously to avoid blocking constructor.
        /// </summary>
        private void InitializeAsync()
        {
            if (_isInitialized) return;

            lock (_lockObject)
            {
                if (_isInitialized) return;

                try
                {
                    // Initialize mapping cache with common data source types
              //      InitializeMappingCache();
                    
                    // Pre-load .NET data types
                    InitializeNetTypesCache();
                    
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    DMEEditor?.AddLogMessage("Beep", $"Error initializing DataTypesHelper: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                }
            }
        }

        /// <summary>
        /// Initializes the mapping cache with common data source mappings.
        /// Only loads mappings for data sources that have drivers installed.
        /// </summary>
        private void InitializeMappingCache()
        {
            var commonDataSources = new[]
            {
                "SQLSERVER", "MYSQL", "POSTGRESQL", "ORACLE", "SQLITE",
                "MONGODB", "CASSANDRA", "REDIS", "FIREBASE", "COSMOSDB"
            };

            foreach (var dsName in commonDataSources)
            {
                try
                {
                    // Check if driver is installed before attempting to load mappings
                    var hasDriver = DMEEditor?.ConfigEditor?.DataDriversClasses?
                        .Any(d => d.classHandler?.Equals(dsName, StringComparison.OrdinalIgnoreCase) == true 
                               && !d.NuggetMissing) ?? false;
                    
                    if (!hasDriver)
                    {
                        continue; // Skip databases without installed drivers
                    }

                    var mappings = DataTypeFieldMappingHelper.GetDataTypes(dsName, DMEEditor);
                    if (mappings?.Any() == true)
                    {
                        _mappingCache.TryAdd(dsName.ToUpperInvariant(), mappings);
                    }
                }
                catch (Exception ex)
                {
                    DMEEditor?.AddLogMessage("Beep", $"Warning: Could not load mappings for {dsName}: {ex.Message}", DateTime.Now, 0, null, Errors.Warning);
                }
            }
        }

        /// <summary>
        /// Initializes the .NET types cache.
        /// </summary>
        private void InitializeNetTypesCache()
        {
            try
            {
                _netTypesCache.TryAdd("NetDataTypes", DataTypeFieldMappingHelper.GetNetDataTypes());
                _netTypesCache.TryAdd("NetDataTypes2", DataTypeFieldMappingHelper.GetNetDataTypes2());
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error initializing .NET types cache: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>Gets a list of data classes from the configuration editor.</summary>
        /// <returns>A list of data classes.</returns>
        public List<string> GetDataClasses()
        {
            try
            {
                return DMEEditor?.ConfigEditor?.DataSourcesClasses?.Select(p => p.className)?.ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting data classes: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return new List<string>();
            }
        }

        /// <summary>Gets the data type of a field in a specific data source with caching.</summary>
        /// <param name="DSname">The name of the data source.</param>
        /// <param name="fld">The field for which to retrieve the data type.</param>
        /// <returns>The data type of the specified field.</returns>
        public string GetDataType(string DSname, EntityField fld)
        {
            if (string.IsNullOrWhiteSpace(DSname) || fld == null)
            {
                DMEEditor?.AddLogMessage("Beep", "Invalid parameters for GetDataType", DateTime.Now, 0, null, Errors.Warning);
                return "System.String"; // Default fallback
            }

            try
            {
                return DataTypeFieldMappingHelper.GetDataType(DSname, fld, DMEEditor);
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting data type for {DSname}.{fld.FieldName}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return GetFallbackDataType(fld);
            }
        }
        /// <summary>Gets the field type without conversion with enhanced error handling.</summary>
        /// <param name="DSname">The name of the data source.</param>
        /// <param name="fld">The entity field.</param>
        /// <returns>The field type without conversion.</returns>
        public string GetFieldTypeWoConversion(string DSname, EntityField fld)
        {
            if (string.IsNullOrWhiteSpace(DSname) || fld == null)
            {
                DMEEditor?.AddLogMessage("Beep", "Invalid parameters for GetFieldtypeWoConversion", DateTime.Now, 0, null, Errors.Warning);
                return "varchar"; // Default fallback
            }

            try
            {
                return DataTypeFieldMappingHelper.GetFieldtypeWoConversion(DSname, fld, DMEEditor);
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting field type without conversion for {DSname}.{fld.FieldName}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return GetFallbackFieldtype(fld);
            }
        }
       
        //public string GetFieldtypeWoConversion(string DSname, EntityField fld)
        //{
           
        //}

        /// <summary>Returns an array of .NET data types with caching.</summary>
        /// <returns>An array of .NET data types.</returns>
        public string[] GetNetDataTypes()
        {
            return _netTypesCache.GetOrAdd("NetDataTypes", _ => DataTypeFieldMappingHelper.GetNetDataTypes());
        }

        /// <summary>Returns an array of extended .NET data types with caching.</summary>
        /// <returns>An array of extended .NET data types.</returns>
        public string[] GetNetDataTypes2()
        {
            return _netTypesCache.GetOrAdd("NetDataTypes2", _ => DataTypeFieldMappingHelper.GetNetDataTypes2());
        }

        /// <summary>Gets cached data type mappings for a specific data source.</summary>
        /// <param name="DSname">The name of the data source.</param>
        /// <returns>The list of datatype mappings for the data source.</returns>
        public List<DatatypeMapping> GetDataTypeMappings(string DSname)
        {
            if (string.IsNullOrWhiteSpace(DSname))
            {
                return new List<DatatypeMapping>();
            }

            var key = DSname.ToUpperInvariant();
            return _mappingCache.GetOrAdd(key, _ => 
            {
                try
                {
                    return DataTypeFieldMappingHelper.GetDataTypes(DSname, DMEEditor) ?? new List<DatatypeMapping>();
                }
                catch (Exception ex)
                {
                    DMEEditor?.AddLogMessage("Beep", $"Error loading data type mappings for {DSname}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                    return new List<DatatypeMapping>();
                }
            });
        }

        /// <summary>Validates if a data type mapping exists for the given parameters.</summary>
        /// <param name="DSname">The data source name.</param>
        /// <param name="fld">The entity field.</param>
        /// <returns>True if a valid mapping exists.</returns>
        public bool IsValidDataTypeMapping(string DSname, EntityField fld)
        {
            if (string.IsNullOrWhiteSpace(DSname) || fld == null)
            {
                return false;
            }

            try
            {
                return DataTypeBasicOperations.IsValidFieldMapping(DSname, fld, DMEEditor);
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error validating data type mapping: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>Gets a custom data type using a converter function.</summary>
        /// <param name="DSname">The data source name.</param>
        /// <param name="fld">The entity field.</param>
        /// <param name="customConverter">The custom type converter function.</param>
        /// <returns>The custom converted data type.</returns>
        public string GetCustomDataType(string DSname, EntityField fld, Func<string, string> customConverter)
        {
            if (customConverter == null)
            {
                return GetDataType(DSname, fld);
            }

            try
            {
                return DataTypeBasicOperations.GetCustomDataType(DSname, fld, DMEEditor, customConverter);
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error getting custom data type: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return GetDataType(DSname, fld);
            }
        }

        /// <summary>Clears the internal caches to free memory and force refresh.</summary>
        public void ClearCache()
        {
            try
            {
                _mappingCache.Clear();
                _netTypesCache.Clear();
                DMEEditor?.AddLogMessage("Beep", "DataTypesHelper cache cleared successfully", DateTime.Now, 1, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor?.AddLogMessage("Beep", $"Error clearing cache: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
        }

        /// <summary>Gets comprehensive statistics about the cached data.</summary>
        /// <returns>A dictionary containing cache statistics.</returns>
        public Dictionary<string, object> GetCacheStatistics()
        {
            return new Dictionary<string, object>
            {
                ["MappingCacheCount"] = _mappingCache.Count,
                ["NetTypesCacheCount"] = _netTypesCache.Count,
                ["IsInitialized"] = _isInitialized,
                ["DefaultStringSize"] = DefaultStringSize,
                ["CachedDataSources"] = _mappingCache.Keys.ToList()
            };
        }
        #endregion

        #region Private Helper Methods
        /// <summary>
        /// Gets a fallback data type based on field characteristics.
        /// </summary>
        /// <param name="fld">The entity field.</param>
        /// <returns>A fallback .NET data type.</returns>
        private string GetFallbackDataType(EntityField fld)
        {
            if (fld == null) return "System.String";

            try
            {
                // Analyze field properties to determine best fallback
                if (fld.Fieldtype?.ToLowerInvariant().Contains("int") == true)
                {
                    return fld.Size1 > 10 ? "System.Int64" : "System.Int32";
                }
                
                if (fld.Fieldtype?.ToLowerInvariant().Contains("decimal") == true || 
                    fld.Fieldtype?.ToLowerInvariant().Contains("numeric") == true)
                {
                    return "System.Decimal";
                }
                
                if (fld.Fieldtype?.ToLowerInvariant().Contains("float") == true || 
                    fld.Fieldtype?.ToLowerInvariant().Contains("double") == true)
                {
                    return "System.Double";
                }
                
                if (fld.Fieldtype?.ToLowerInvariant().Contains("bool") == true)
                {
                    return "System.Boolean";
                }
                
                if (fld.Fieldtype?.ToLowerInvariant().Contains("date") == true || 
                    fld.Fieldtype?.ToLowerInvariant().Contains("time") == true)
                {
                    return "System.DateTime";
                }

                return "System.String";
            }
            catch
            {
                return "System.String";
            }
        }

        /// <summary>
        /// Gets a fallback field type for database schema.
        /// </summary>
        /// <param name="fld">The entity field.</param>
        /// <returns>A fallback database field type.</returns>
        private string GetFallbackFieldtype(EntityField fld)
        {
            if (fld == null) return "varchar";

            try
            {
                var netType = GetFallbackDataType(fld);
                
                return netType switch
                {
                    "System.Int32" => "int",
                    "System.Int64" => "bigint",
                    "System.Decimal" => "decimal",
                    "System.Double" => "float",
                    "System.Boolean" => "bit",
                    "System.DateTime" => "datetime",
                    _ => $"varchar({Math.Max(fld.Size1, DefaultStringSize)})"
                };
            }
            catch
            {
                return $"varchar({DefaultStringSize})";
            }
        }
        #endregion

        #region IDisposable Implementation
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        // Clear caches
                        _mappingCache?.Clear();
                        _netTypesCache?.Clear();
                        
                        // Clear mapping reference
                        mapping?.Clear();
                        mapping = null;
                        
                        DMEEditor?.AddLogMessage("Beep", "DataTypesHelper disposed successfully", DateTime.Now, 1, null, Errors.Ok);
                    }
                    catch (Exception ex)
                    {
                        // Log disposal error but don't throw
                        DMEEditor?.AddLogMessage("Beep", $"Error during DataTypesHelper disposal: {ex.Message}", DateTime.Now, 0, null, Errors.Warning);
                    }
                }

                _disposedValue = true;
            }
        }

        /// <summary>
        /// Releases all resources used by the DataTypesHelper.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

      

        #endregion
    }
}
