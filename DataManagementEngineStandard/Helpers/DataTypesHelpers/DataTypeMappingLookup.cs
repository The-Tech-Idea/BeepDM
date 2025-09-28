using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.Helpers.DataTypesHelpers
{
    /// <summary>
    /// Optimized helper class for data type mapping lookups and conversions between database and .NET types.
    /// Features caching, improved error handling, and modern C# patterns.
    /// </summary>
    public static class DataTypeMappingLookup
    {
        #region Constants and Cache
        private const string NUMERIC_PRECISION_PLACEHOLDER = "(P,S)";
        private const string STRING_LENGTH_PLACEHOLDER = "(N)";
        private const string PRECISION_SCALE_PLACEHOLDER = "(N,S)";
        
        // Thread-safe caches for improved performance
        private static readonly ConcurrentDictionary<string, DatatypeMapping> _mappingCache = new();
        private static readonly ConcurrentDictionary<string, string> _dataTypeCache = new();
        private static readonly ConcurrentDictionary<string, List<DatatypeMapping>> _classTypeMappingsCache = new();
        
        // Regex patterns for better performance
        private static readonly Regex _parenthesesPattern = new(@"\([^)]*\)", RegexOptions.Compiled);
        private static readonly Regex _precisionScalePattern = new(@"\((\d+),(\d+)\)", RegexOptions.Compiled);
        private static readonly Regex _lengthPattern = new(@"\((\d+)\)", RegexOptions.Compiled);
        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the datatype mapping for a given class name, field type, entity field, and DME editor with caching.
        /// </summary>
        /// <param name="className">The name of the class.</param>
        /// <param name="fieldType">The type of the field.</param>
        /// <param name="fld">The entity field.</param>
        /// <param name="DMEEditor">The DME editor.</param>
        /// <returns>The datatype mapping for the given parameters.</returns>
        public static DatatypeMapping GetDataTypeMappingForString(string className, string fieldType, EntityField fld, IDMEEditor DMEEditor)
        {
            if (string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(fieldType) || fld == null || DMEEditor == null)
            {
                return null;
            }

            var cacheKey = $"{className}|{fieldType}|{fld.Size1}";
            
            return _mappingCache.GetOrAdd(cacheKey, _ =>
            {
                try
                {
                    EnsureDataTypesMapLoaded(DMEEditor);
                    
                    var mappings = GetCachedClassMappings(className, DMEEditor);
                    
                    // First, try to find exact match with preferred mapping
                    var mapping = mappings.FirstOrDefault(x => 
                        x.NetDataType.Equals(fieldType, StringComparison.InvariantCultureIgnoreCase) && x.Fav);
                    
                    if (mapping == null)
                    {
                        // Fall back to any matching mapping
                        mapping = mappings.FirstOrDefault(x => 
                            x.NetDataType.Equals(fieldType, StringComparison.InvariantCultureIgnoreCase));
                    }

                    // Handle size-specific mappings
                    if (fld.Size1 > 0 && mapping != null)
                    {
                        mapping = ProcessSizeMapping(mapping, mappings, fieldType, fld.Size1);
                    }

                    return mapping;
                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("DataTypeMappingLookup", 
                        $"Error getting data type mapping for {className}.{fieldType}: {ex.Message}", 
                        DateTime.Now, -1, null, Errors.Failed);
                    return null;
                }
            });
        }

        /// <summary>
        /// Gets the data type of a field in a specific data source with enhanced error handling and caching.
        /// </summary>
        /// <param name="DSname">The name of the data source.</param>
        /// <param name="providerfldtype">The provider field type.</param>
        /// <param name="DMEEditor">The IDMEEditor instance used for accessing the data source.</param>
        /// <returns>The data type of the specified field.</returns>
        public static string GetDataType(string DSname, string providerfldtype, IDMEEditor DMEEditor)
        {
            if (string.IsNullOrWhiteSpace(DSname) || string.IsNullOrWhiteSpace(providerfldtype) || DMEEditor == null)
            {
                return null;
            }

            var cacheKey = $"{DSname}|{providerfldtype}";
            
            return _dataTypeCache.GetOrAdd(cacheKey, _ =>
            {
                try
                {
                    var ds = DMEEditor.GetDataSource(DSname);
                    if (ds == null)
                    {
                        DMEEditor.AddLogMessage("DataTypeMappingLookup", 
                            $"Data source '{DSname}' not found", 
                            DateTime.Now, 0, null, Errors.Warning);
                        return null;
                    }

                    EnsureDataTypesMapLoaded(DMEEditor);

                    // Clean the provider field type
                    var cleanFieldType = CleanFieldType(providerfldtype);
                    
                    // Get the class handler for the data source
                    var classHandler = DMEEditor.GetDataSourceClass(DSname);
                    if (classHandler == null)
                    {
                        DMEEditor.AddLogMessage("DataTypeMappingLookup", 
                            $"No class handler found for data source '{DSname}'", 
                            DateTime.Now, 0, null, Errors.Warning);
                        return null;
                    }

                    // Look up the mapping
                    var mappings = GetCachedClassMappings(classHandler.className, DMEEditor);
                    var mapping = mappings.FirstOrDefault(m => 
                        m.DataType.Equals(cleanFieldType, StringComparison.InvariantCultureIgnoreCase));

                    return mapping?.NetDataType;
                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("DataTypeMappingLookup", 
                        $"Error converting field type '{providerfldtype}' for data source '{DSname}': {ex.Message}", 
                        DateTime.Now, -1, null, Errors.Failed);
                    return null;
                }
            });
        }

        /// <summary>
        /// Gets the data type of a field in a specific data source with comprehensive mapping logic.
        /// </summary>
        /// <param name="DSname">The name of the data source.</param>
        /// <param name="fld">The field for which to retrieve the data type.</param>
        /// <param name="DMEEditor">The IDMEEditor instance used for accessing the data source.</param>
        /// <returns>The data type of the specified field.</returns>
        public static string GetDataType(string DSname, EntityField fld, IDMEEditor DMEEditor)
        {
            if (string.IsNullOrWhiteSpace(DSname) || fld == null || DMEEditor == null)
            {
                return GetFallbackDataType(fld);
            }

            var cacheKey = $"{DSname}|{fld.EntityName}|{fld.fieldname}|{fld.fieldtype}|{fld.Size1}|{fld.NumericPrecision}|{fld.NumericScale}";
            
            return _dataTypeCache.GetOrAdd(cacheKey, _ =>
            {
                try
                {
                    var ds = DMEEditor.GetDataSource(DSname);
                    if (ds == null)
                    {
                        DMEEditor.AddLogMessage("DataTypeMappingLookup", 
                            $"Data source '{DSname}' not found for field '{fld.EntityName}.{fld.fieldname}'", 
                            DateTime.Now, 0, null, Errors.Warning);
                        return GetFallbackDataType(fld);
                    }

                    EnsureDataTypesMapLoaded(DMEEditor);

                    string retval;

                    if (!IsSystemType(fld.fieldtype))
                    {
                        retval = GetFieldTypeWoConversion(DSname, fld, DMEEditor);
                    }
                    else
                    {
                        var classHandler = DMEEditor.GetDataSourceClass(DSname);
                        if (classHandler != null)
                        {
                            retval = ProcessSystemFieldType(classHandler.className, fld, DMEEditor);
                        }
                        else
                        {
                            DMEEditor.AddLogMessage("DataTypeMappingLookup", 
                                $"Class handler not found for data source '{DSname}', field '{fld.EntityName}.{fld.fieldname}'", 
                                DateTime.Now, 0, null, Errors.Warning);
                            return GetFallbackDataType(fld);
                        }
                    }

                    if (!string.IsNullOrEmpty(retval))
                    {
                        retval = ApplyPlaceholderReplacements(retval, fld, DMEEditor);
                    }

                    return retval ?? GetFallbackDataType(fld);
                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("DataTypeMappingLookup", 
                        $"Error processing field '{fld.EntityName}.{fld.fieldname}': {ex.Message}", 
                        DateTime.Now, -1, null, Errors.Failed);
                    return GetFallbackDataType(fld);
                }
            });
        }

        /// <summary>
        /// Gets the data type of a field from a specific data source class name.
        /// </summary>
        /// <param name="className">The name of the data source class</param>
        /// <param name="fld">The field for which to retrieve the data type.</param>
        /// <param name="DMEEditor">The IDMEEditor instance used for accessing the data source.</param>
        /// <returns>The data type of the specified field.</returns>
        public static string GetDataTypeFromDataSourceClassName(string className, EntityField fld, IDMEEditor DMEEditor)
        {
            if (string.IsNullOrWhiteSpace(className) || fld == null || DMEEditor == null)
            {
                return GetFallbackDataType(fld);
            }

            var cacheKey = $"class|{className}|{fld.fieldtype}|{fld.Size1}|{fld.NumericPrecision}|{fld.NumericScale}";
            
            return _dataTypeCache.GetOrAdd(cacheKey, _ =>
            {
                try
                {
                    EnsureDataTypesMapLoaded(DMEEditor);
                    return ProcessSystemFieldType(className, fld, DMEEditor) ?? GetFallbackDataType(fld);
                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("DataTypeMappingLookup", 
                        $"Error getting data type from class '{className}' for field type '{fld.fieldtype}': {ex.Message}", 
                        DateTime.Now, -1, null, Errors.Failed);
                    return GetFallbackDataType(fld);
                }
            });
        }

        /// <summary>
        /// Gets the field type without conversion with enhanced caching and error handling.
        /// </summary>
        /// <param name="DSname">The name of the data source.</param>
        /// <param name="fld">The entity field.</param>
        /// <param name="DMEEditor">The DME editor.</param>
        /// <returns>The field type without conversion.</returns>
        public static string GetFieldTypeWoConversion(string DSname, EntityField fld, IDMEEditor DMEEditor)
        {
            if (string.IsNullOrWhiteSpace(DSname) || fld == null || DMEEditor == null)
            {
                return GetFallbackFieldType(fld);
            }

            var cacheKey = $"wo_conv|{DSname}|{fld.fieldtype}|{fld.Size1}|{fld.NumericPrecision}|{fld.NumericScale}";
            
            return _dataTypeCache.GetOrAdd(cacheKey, _ =>
            {
                try
                {
                    EnsureDataTypesMapLoaded(DMEEditor);
                    
                    var classHandler = DMEEditor.GetDataSourceClass(DSname);
                    if (classHandler == null)
                    {
                        DMEEditor.AddLogMessage("DataTypeMappingLookup", 
                            $"Class handler not found for data source '{DSname}', field '{fld.EntityName}.{fld.fieldname}'", 
                            DateTime.Now, 0, null, Errors.Warning);
                        return GetFallbackFieldType(fld);
                    }

                    var mappings = GetCachedClassMappings(classHandler.className, DMEEditor);
                    return ProcessFieldTypeMapping(mappings, classHandler.className, fld) ?? GetFallbackFieldType(fld);
                }
                catch (Exception ex)
                {
                    DMEEditor.AddLogMessage("DataTypeMappingLookup", 
                        $"Error getting field type without conversion for '{fld.EntityName}.{fld.fieldname}': {ex.Message}", 
                        DateTime.Now, -1, null, Errors.Failed);
                    return GetFallbackFieldType(fld);
                }
            });
        }

        /// <summary>
        /// Clears all internal caches for memory management and fresh data loading.
        /// </summary>
        public static void ClearCache()
        {
            _mappingCache.Clear();
            _dataTypeCache.Clear();
            _classTypeMappingsCache.Clear();
        }

        /// <summary>
        /// Gets cache statistics for monitoring and diagnostics.
        /// </summary>
        /// <returns>Dictionary containing cache statistics.</returns>
        public static Dictionary<string, object> GetCacheStatistics()
        {
            return new Dictionary<string, object>
            {
                ["MappingCacheCount"] = _mappingCache.Count,
                ["DataTypeCacheCount"] = _dataTypeCache.Count,
                ["ClassMappingsCacheCount"] = _classTypeMappingsCache.Count,
                ["TotalCachedItems"] = _mappingCache.Count + _dataTypeCache.Count + _classTypeMappingsCache.Count
            };
        }

        #endregion

        #region Private Helper Methods

        private static void EnsureDataTypesMapLoaded(IDMEEditor DMEEditor)
        {
            if (DMEEditor.ConfigEditor.DataTypesMap == null || !DMEEditor.ConfigEditor.DataTypesMap.Any())
            {
                DMEEditor.ConfigEditor.ReadDataTypeFile();
            }
        }

        private static List<DatatypeMapping> GetCachedClassMappings(string className, IDMEEditor DMEEditor)
        {
            return _classTypeMappingsCache.GetOrAdd(className, _ =>
            {
                return DMEEditor.ConfigEditor.DataTypesMap?
                    .Where(x => x.DataSourceName.Equals(className, StringComparison.InvariantCultureIgnoreCase))
                    .ToList() ?? new List<DatatypeMapping>();
            });
        }

        private static string CleanFieldType(string fieldType)
        {
            if (string.IsNullOrWhiteSpace(fieldType))
                return fieldType;

            return _parenthesesPattern.Replace(fieldType, "").Trim();
        }

        private static bool IsSystemType(string fieldType)
        {
            return !string.IsNullOrWhiteSpace(fieldType) && fieldType.Contains("System.", StringComparison.InvariantCultureIgnoreCase);
        }

        private static DatatypeMapping ProcessSizeMapping(DatatypeMapping baseMapping, List<DatatypeMapping> mappings, string fieldType, int size)
        {
            if (baseMapping == null || size <= 0)
                return baseMapping;

            var sizeMapping = mappings.FirstOrDefault(x => 
                x.NetDataType.Equals(fieldType, StringComparison.InvariantCultureIgnoreCase) && 
                x.DataType.Contains("N"));

            if (sizeMapping != null)
            {
                // Create a copy to avoid modifying the cached original
                var result = new DatatypeMapping
                {
                    DataSourceName = sizeMapping.DataSourceName,
                    NetDataType = sizeMapping.NetDataType,
                    DataType = sizeMapping.DataType.Replace("(N)", $"({size})")
                };
                return result;
            }

            return baseMapping;
        }

        private static string ProcessSystemFieldType(string className, EntityField fld, IDMEEditor DMEEditor)
        {
            if (fld == null || string.IsNullOrWhiteSpace(fld.fieldtype))
                return null;

            var mappings = GetCachedClassMappings(className, DMEEditor);

            string retval = null;

            if (fld.fieldtype.Equals("System.String", StringComparison.InvariantCultureIgnoreCase))
            {
                retval = ProcessStringFieldType(mappings, className, fld);
            }
            else if (!fld.fieldtype.Equals("System.DateTime", StringComparison.InvariantCultureIgnoreCase))
            {
                retval = ProcessNumericFieldType(mappings, className, fld);
            }

            if (retval == null)
            {
                var dt = mappings.FirstOrDefault(x => 
                    x.NetDataType.Equals(fld.fieldtype, StringComparison.InvariantCultureIgnoreCase));
                retval = dt?.DataType;
            }

            return retval;
        }

        private static string ProcessStringFieldType(List<DatatypeMapping> mappings, string className, EntityField fld)
        {
            if (fld.Size1 > 0)
            {
                // Try to find preferred mapping with size placeholder
                var dt = mappings.FirstOrDefault(x => 
                    x.NetDataType.Equals(fld.fieldtype, StringComparison.InvariantCultureIgnoreCase) && 
                    x.Fav && 
                    x.DataType.Contains("N"));

                if (dt == null)
                {
                    // Fall back to any mapping with size placeholder
                    dt = mappings.FirstOrDefault(x => 
                        x.NetDataType.Equals(fld.fieldtype, StringComparison.InvariantCultureIgnoreCase) && 
                        x.DataType.Contains("N"));
                }

                if (dt != null)
                {
                    return dt.DataType.Replace("(N)", $"({fld.Size1})");
                }
            }

            // No size specified, get default string mapping
            var defaultMapping = mappings.FirstOrDefault(x => 
                x.NetDataType.Equals(fld.fieldtype, StringComparison.InvariantCultureIgnoreCase));
            
            return defaultMapping?.DataType;
        }

        private static string ProcessNumericFieldType(List<DatatypeMapping> mappings, string className, EntityField fld)
        {
            // Set default precision and scale for decimals
            if (fld.fieldtype.Equals("System.Decimal", StringComparison.InvariantCultureIgnoreCase))
            {
                if (fld.NumericPrecision == 0) fld.NumericPrecision = 28;
                if (fld.NumericScale == 0) fld.NumericScale = 8;
            }

            if (fld.NumericPrecision > 0)
            {
                if (fld.NumericScale > 0)
                {
                    // Look for precision,scale pattern
                    var dt = FindBestMapping(mappings, fld.fieldtype, "P,S", true) ??
                            FindBestMapping(mappings, fld.fieldtype, "P,S", false);
                    
                    if (dt != null)
                    {
                        return dt.DataType.Replace("(P,S)", $"({fld.NumericPrecision},{fld.NumericScale})");
                    }
                }
                else
                {
                    // Look for precision only pattern
                    var dt = FindBestMapping(mappings, fld.fieldtype, "(N)", true) ??
                            FindBestMapping(mappings, fld.fieldtype, "(N)", false);
                    
                    if (dt != null)
                    {
                        return dt.DataType.Replace("(N)", $"({fld.NumericPrecision})");
                    }

                    // Fall back to precision,scale with 0 scale
                    dt = FindBestMapping(mappings, fld.fieldtype, "P,S", true) ??
                         FindBestMapping(mappings, fld.fieldtype, "P,S", false);
                    
                    if (dt != null)
                    {
                        return dt.DataType.Replace("(P,S)", $"({fld.NumericPrecision},0)");
                    }
                }
            }

            return null;
        }

        private static DatatypeMapping FindBestMapping(List<DatatypeMapping> mappings, string fieldType, string pattern, bool preferFavorite)
        {
            return mappings.FirstOrDefault(x => 
                x.NetDataType.Equals(fieldType, StringComparison.InvariantCultureIgnoreCase) && 
                x.DataType.Contains(pattern) && 
                (!preferFavorite || x.Fav));
        }

        private static string ProcessFieldTypeMapping(List<DatatypeMapping> mappings, string className, EntityField fld)
        {
            DatatypeMapping dt = null;

            // Try size-based mapping first
            if (fld.Size1 > 0)
            {
                dt = mappings.FirstOrDefault(x => 
                    x.DataType == fld.fieldtype && 
                    x.DataType.Contains("N"));
                
                if (dt != null)
                {
                    return dt.DataType.Replace("(N)", $"({fld.Size1})");
                }
            }

            // Try precision/scale mapping
            if (fld.NumericPrecision > 0)
            {
                if (fld.NumericScale > 0)
                {
                    dt = mappings.FirstOrDefault(x => 
                        x.DataType == fld.fieldtype && 
                        x.DataType.Contains("N,S"));
                    
                    if (dt != null)
                    {
                        return dt.DataType.Replace("(N,S)", $"({fld.NumericPrecision},{fld.NumericScale})");
                    }
                }
                else
                {
                    dt = mappings.FirstOrDefault(x => 
                        x.DataType == fld.fieldtype && 
                        x.DataType.Contains("(N)"));
                    
                    if (dt != null)
                    {
                        return dt.DataType.Replace("(N)", $"({fld.NumericPrecision})");
                    }

                    // Fall back to precision,scale pattern
                    dt = mappings.FirstOrDefault(x => 
                        x.DataType == fld.fieldtype && 
                        x.DataType.Contains("(N,S)"));
                    
                    if (dt != null)
                    {
                        return dt.DataType.Replace("(N,S)", $"({fld.NumericPrecision},0)");
                    }
                }
            }

            // Default mapping
            dt = mappings.FirstOrDefault(x => x.DataType == fld.fieldtype);
            return dt?.DataType;
        }

        private static string ApplyPlaceholderReplacements(string retval, EntityField fld, IDMEEditor DMEEditor)
        {
            if (string.IsNullOrWhiteSpace(retval) || fld == null)
                return retval;

            // Apply default size for string types if needed
            if (fld.NumericPrecision == 0 && retval.Contains("N"))
            {
                var defaultSize = DMEEditor?.typesHelper?.DefaultStringSize ?? 255;
                fld.NumericPrecision = (short)defaultSize;
            }

            // Replace placeholders
            retval = retval.Replace("(N)", $"({fld.NumericPrecision})");
            retval = retval.Replace("(P,S)", $"({fld.NumericPrecision},{fld.NumericScale})");

            return retval;
        }

        private static string GetFallbackDataType(EntityField fld)
        {
            if (fld == null || string.IsNullOrWhiteSpace(fld.fieldtype))
                return "System.String";

            // Analyze field type to provide intelligent fallback
            var fieldTypeLower = fld.fieldtype.ToLowerInvariant();

            return fieldTypeLower switch
            {
                var type when type.Contains("int") => fld.Size1 > 10 ? "System.Int64" : "System.Int32",
                var type when type.Contains("decimal") || type.Contains("numeric") => "System.Decimal",
                var type when type.Contains("float") || type.Contains("double") => "System.Double",
                var type when type.Contains("bool") => "System.Boolean",
                var type when type.Contains("date") || type.Contains("time") => "System.DateTime",
                var type when type.Contains("guid") => "System.Guid",
                var type when type.Contains("byte") => "System.Byte[]",
                _ => "System.String"
            };
        }

        private static string GetFallbackFieldType(EntityField fld)
        {
            if (fld == null)
                return "varchar(255)";

            var fallbackDataType = GetFallbackDataType(fld);
            var size = Math.Max(fld.Size1, 255);

            return fallbackDataType switch
            {
                "System.Int32" => "int",
                "System.Int64" => "bigint",
                "System.Decimal" => fld.NumericScale > 0 ? $"decimal({fld.NumericPrecision},{fld.NumericScale})" : "decimal(18,2)",
                "System.Double" => "float",
                "System.Boolean" => "bit",
                "System.DateTime" => "datetime",
                "System.Guid" => "uniqueidentifier",
                "System.Byte[]" => "varbinary(max)",
                _ => $"varchar({size})"
            };
        }

        #endregion
    }
}