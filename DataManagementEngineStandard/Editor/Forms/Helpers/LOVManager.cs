using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Manages List of Values (LOV) for Oracle Forms-compatible LOV system.
    /// Handles LOV registration, data loading, caching, validation, and related field population.
    /// Thread-safe implementation using ConcurrentDictionary.
    /// </summary>
    public class LOVManager : ILOVManager
    {
        #region Fields
        
        private readonly IDMEEditor _dmeEditor;
        private readonly ConcurrentDictionary<string, DataBlockInfo> _blocks;
        
        // Key: "blockName:fieldName", Value: LOV definition
        private readonly ConcurrentDictionary<string, LOVDefinition> _lovs = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lockObject = new object();
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of LOVManager
        /// </summary>
        /// <param name="dmeEditor">DME Editor instance</param>
        /// <param name="blocks">Blocks collection reference</param>
        public LOVManager(
            IDMEEditor dmeEditor,
            ConcurrentDictionary<string, DataBlockInfo> blocks)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        }
        
        #endregion
        
        #region Events
        
        /// <inheritdoc />
        public event EventHandler<LOVDataLoadedEventArgs> LOVDataLoaded;
        
        /// <inheritdoc />
        public event EventHandler<LOVValidationEventArgs> LOVValidationFailed;
        
        #endregion
        
        #region LOV Registration
        
        /// <inheritdoc />
        public void RegisterLOV(string blockName, string fieldName, LOVDefinition lov)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                throw new ArgumentException("Block name required", nameof(blockName));
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field name required", nameof(fieldName));
            if (lov == null)
                throw new ArgumentNullException(nameof(lov));
            
            // Set LOV name if not specified
            if (string.IsNullOrWhiteSpace(lov.LOVName))
            {
                lov.LOVName = $"{blockName}_{fieldName}_LOV";
            }
                
            var key = GetLOVKey(blockName, fieldName);
            _lovs[key] = lov;
        }
        
        /// <inheritdoc />
        public void UnregisterLOV(string blockName, string fieldName)
        {
            var key = GetLOVKey(blockName, fieldName);
            _lovs.TryRemove(key, out _);
        }
        
        /// <inheritdoc />
        public bool HasLOV(string blockName, string fieldName)
        {
            var key = GetLOVKey(blockName, fieldName);
            return _lovs.ContainsKey(key);
        }
        
        /// <inheritdoc />
        public LOVDefinition GetLOV(string blockName, string fieldName)
        {
            var key = GetLOVKey(blockName, fieldName);
            return _lovs.TryGetValue(key, out var lov) ? lov : null;
        }
        
        /// <inheritdoc />
        public Dictionary<string, LOVDefinition> GetBlockLOVs(string blockName)
        {
            var prefix = $"{blockName}:";
            return _lovs
                .Where(kvp => kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    kvp => kvp.Key.Substring(prefix.Length),
                    kvp => kvp.Value,
                    StringComparer.OrdinalIgnoreCase);
        }
        
        /// <inheritdoc />
        public IReadOnlyList<string> GetAllLOVKeys()
        {
            return _lovs.Keys.ToList();
        }
        
        #endregion
        
        #region LOV Data Operations
        
        /// <inheritdoc />
        public async Task<LOVResult> LoadLOVDataAsync(string blockName, string fieldName, string searchText = null)
        {
            var lov = GetLOV(blockName, fieldName);
            if (lov == null)
            {
                return LOVResult.Fail($"No LOV registered for {blockName}.{fieldName}");
            }
            
            var result = await LoadLOVDataAsync(lov, searchText);
            
            // Raise event
            LOVDataLoaded?.Invoke(this, new LOVDataLoadedEventArgs
            {
                BlockName = blockName,
                FieldName = fieldName,
                LOV = lov,
                RecordCount = result.TotalCount,
                FromCache = result.FromCache,
                LoadTimeMs = result.LoadTimeMs
            });
            
            return result;
        }
        
        /// <inheritdoc />
        public async Task<LOVResult> LoadLOVDataAsync(LOVDefinition lov, string searchText = null)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Check cache first (only if no search text)
                if (string.IsNullOrEmpty(searchText) && lov.IsCacheValid())
                {
                    stopwatch.Stop();
                    return new LOVResult
                    {
                        Success = true,
                        Records = lov.CachedData,
                        TotalCount = lov.CachedData.Count,
                        FromCache = true,
                        LoadTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }
                
                // Get data source
                var dataSource = _dmeEditor.GetDataSource(lov.DataSourceName);
                if (dataSource == null)
                {
                    return LOVResult.Fail($"Data source '{lov.DataSourceName}' not found");
                }
                
                // Open connection if needed
                if (dataSource.ConnectionStatus != ConnectionState.Open)
                {
                    dataSource.Openconnection();
                }
                
                // Build filters
                var filters = BuildFilters(lov, searchText);
                
                // Query data
                var data = await Task.Run(() => 
                {
                    if (filters.Count > 0)
                    {
                        return dataSource.GetEntity(lov.EntityName, filters);
                    }
                    return dataSource.GetEntity(lov.EntityName, null);
                });
                
                // Convert to list
                var records = ConvertToList(data);
                
                stopwatch.Stop();
                
                var result = new LOVResult
                {
                    Success = true,
                    Records = records,
                    TotalCount = records.Count,
                    FromCache = false,
                    LoadTimeMs = stopwatch.ElapsedMilliseconds
                };
                
                // Cache if not searching and caching enabled
                if (string.IsNullOrEmpty(searchText) && lov.UseCache)
                {
                    lov.CachedData = records;
                    lov.CacheTimestamp = DateTime.Now;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return LOVResult.Fail($"Error loading LOV data: {ex.Message}");
            }
        }
        
        /// <inheritdoc />
        public LOVResult FilterLOVData(string blockName, string fieldName, string searchText)
        {
            var lov = GetLOV(blockName, fieldName);
            if (lov == null || !lov.IsCacheValid())
            {
                return LOVResult.Fail("No cached data available");
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            List<object> filteredRecords;
            if (string.IsNullOrEmpty(searchText))
            {
                filteredRecords = lov.CachedData;
            }
            else
            {
                filteredRecords = lov.CachedData
                    .Where(r => MatchesSearch(r, lov, searchText))
                    .ToList();
            }
            
            stopwatch.Stop();
            
            return new LOVResult
            {
                Success = true,
                Records = filteredRecords,
                TotalCount = filteredRecords.Count,
                FromCache = true,
                LoadTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        
        /// <inheritdoc />
        public async Task<LOVValidationResult> ValidateLOVValueAsync(string blockName, string fieldName, object value)
        {
            var lov = GetLOV(blockName, fieldName);
            if (lov == null)
                return LOVValidationResult.Valid(); // No LOV = no validation required
                
            if (lov.ValidationType == LOVValidationType.Unrestricted)
                return LOVValidationResult.Valid(); // No validation required
                
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return LOVValidationResult.Valid(); // Null check handled by required validation
                
            // Load LOV data
            var lovResult = await LoadLOVDataAsync(lov, null);
            if (!lovResult.Success)
            {
                return LOVValidationResult.Invalid($"Cannot validate: {lovResult.ErrorMessage}");
            }
            
            // Check if value exists in LOV
            var valueStr = value.ToString();
            var returnField = lov.ReturnField ?? lov.DisplayField;
            
            object matchedRecord = null;
            foreach (var record in lovResult.Records)
            {
                var fieldValue = GetPropertyValue(record, returnField);
                if (fieldValue?.ToString()?.Equals(valueStr, StringComparison.OrdinalIgnoreCase) == true)
                {
                    matchedRecord = record;
                    break;
                }
            }
            
            if (matchedRecord != null)
            {
                return LOVValidationResult.Valid(matchedRecord);
            }
            
            // Value not found - get suggestions
            var suggestions = GetSuggestions(lovResult.Records, lov, valueStr, 5);
            
            var result = LOVValidationResult.Invalid($"Value '{value}' not found in List of Values", suggestions);
            
            // Raise event
            LOVValidationFailed?.Invoke(this, new LOVValidationEventArgs
            {
                BlockName = blockName,
                FieldName = fieldName,
                Value = value,
                ErrorMessage = result.ErrorMessage,
                Suggestions = suggestions
            });
            
            return result;
        }
        
        /// <inheritdoc />
        public Dictionary<string, object> GetRelatedFieldValues(LOVDefinition lov, object selectedRecord)
        {
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            
            if (lov?.RelatedFieldMappings == null || selectedRecord == null)
                return values;
            
            // First, add the return field value
            if (!string.IsNullOrWhiteSpace(lov.ReturnField))
            {
                var returnValue = GetPropertyValue(selectedRecord, lov.ReturnField);
                if (returnValue != null)
                {
                    values["__RETURN_VALUE__"] = returnValue;
                }
            }
            
            // Then add related field mappings
            foreach (var mapping in lov.RelatedFieldMappings)
            {
                var lovFieldName = mapping.Key;      // Field in LOV record
                var blockFieldName = mapping.Value;  // Target field in block
                
                var fieldValue = GetPropertyValue(selectedRecord, lovFieldName);
                if (fieldValue != null)
                {
                    values[blockFieldName] = fieldValue;
                }
            }
            
            return values;
        }
        
        /// <inheritdoc />
        public async Task<object> FindRecordByValueAsync(string blockName, string fieldName, object value)
        {
            if (value == null)
                return null;
                
            var lov = GetLOV(blockName, fieldName);
            if (lov == null)
                return null;
                
            var lovResult = await LoadLOVDataAsync(lov, null);
            if (!lovResult.Success)
                return null;
                
            var returnField = lov.ReturnField ?? lov.DisplayField;
            var valueStr = value.ToString();
            
            return lovResult.Records.FirstOrDefault(r =>
            {
                var fieldValue = GetPropertyValue(r, returnField);
                return fieldValue?.ToString()?.Equals(valueStr, StringComparison.OrdinalIgnoreCase) == true;
            });
        }
        
        #endregion
        
        #region Cache Management
        
        /// <inheritdoc />
        public void ClearLOVCache(string blockName, string fieldName)
        {
            var lov = GetLOV(blockName, fieldName);
            lov?.ClearCache();
        }
        
        /// <inheritdoc />
        public void ClearBlockLOVCache(string blockName)
        {
            foreach (var lov in GetBlockLOVs(blockName).Values)
            {
                lov.ClearCache();
            }
        }
        
        /// <inheritdoc />
        public void ClearAllLOVCaches()
        {
            foreach (var lov in _lovs.Values)
            {
                lov.ClearCache();
            }
        }
        
        /// <inheritdoc />
        public async Task RefreshLOVCacheAsync(string blockName, string fieldName)
        {
            var lov = GetLOV(blockName, fieldName);
            if (lov == null)
                return;
                
            lov.ClearCache();
            await LoadLOVDataAsync(lov, null);
        }
        
        /// <inheritdoc />
        public async Task PreloadLOVAsync(string blockName, string fieldName)
        {
            await LoadLOVDataAsync(blockName, fieldName, null);
        }
        
        /// <inheritdoc />
        public async Task PreloadBlockLOVsAsync(string blockName)
        {
            var lovs = GetBlockLOVs(blockName);
            var tasks = lovs.Select(kvp => LoadLOVDataAsync(blockName, kvp.Key, null));
            await Task.WhenAll(tasks);
        }
        
        #endregion
        
        #region Private Methods
        
        private static string GetLOVKey(string blockName, string fieldName)
        {
            return $"{blockName}:{fieldName}";
        }
        
        private List<AppFilter> BuildFilters(LOVDefinition lov, string searchText)
        {
            var filters = new List<AppFilter>();
            
            // Add existing filters
            if (lov.Filters != null)
            {
                filters.AddRange(lov.Filters);
            }
            
            // Add search filters if search text provided
            if (!string.IsNullOrEmpty(searchText))
            {
                var searchableColumns = lov.Columns.Where(c => c.Searchable).ToList();
                
                // If no columns defined, use display field
                if (!searchableColumns.Any() && !string.IsNullOrWhiteSpace(lov.DisplayField))
                {
                    searchableColumns.Add(new LOVColumn { FieldName = lov.DisplayField });
                }
                
                foreach (var col in searchableColumns)
                {
                    var searchFilter = CreateSearchFilter(col.FieldName, searchText, lov.SearchMode);
                    filters.Add(searchFilter);
                }
            }
            
            return filters;
        }
        
        private static AppFilter CreateSearchFilter(string fieldName, string searchText, LOVSearchMode searchMode)
        {
            var filter = new AppFilter
            {
                FieldName = fieldName,
                FilterValue = searchText
            };
            
            switch (searchMode)
            {
                case LOVSearchMode.StartsWith:
                    filter.Operator = "LIKE";
                    filter.FilterValue = $"{searchText}%";
                    break;
                case LOVSearchMode.EndsWith:
                    filter.Operator = "LIKE";
                    filter.FilterValue = $"%{searchText}";
                    break;
                case LOVSearchMode.Contains:
                    filter.Operator = "LIKE";
                    filter.FilterValue = $"%{searchText}%";
                    break;
                case LOVSearchMode.Exact:
                default:
                    filter.Operator = "=";
                    break;
            }
            
            return filter;
        }
        
        private List<object> ConvertToList(object data)
        {
            if (data == null)
                return new List<object>();
            
            // Handle DataTable
            if (data is DataTable dt)
            {
                return dt.AsEnumerable()
                    .Select(r => r.ItemArray.Select((val, i) => new { Key = dt.Columns[i].ColumnName, Value = val })
                        .ToDictionary(x => x.Key, x => x.Value) as object)
                    .ToList();
            }
            
            // Handle IEnumerable
            if (data is IEnumerable<object> enumerable)
            {
                return enumerable.ToList();
            }
            
            // Handle non-generic IEnumerable
            if (data is System.Collections.IEnumerable nonGenericEnum)
            {
                var list = new List<object>();
                foreach (var item in nonGenericEnum)
                {
                    list.Add(item);
                }
                return list;
            }
            
            // Single object
            return new List<object> { data };
        }
        
        private bool MatchesSearch(object record, LOVDefinition lov, string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
                return true;
                
            searchText = searchText.ToLower();
            
            var searchableColumns = lov.Columns.Where(c => c.Searchable).ToList();
            
            // If no columns defined, use display field
            if (!searchableColumns.Any() && !string.IsNullOrWhiteSpace(lov.DisplayField))
            {
                searchableColumns.Add(new LOVColumn { FieldName = lov.DisplayField });
            }
            
            foreach (var col in searchableColumns)
            {
                var value = GetPropertyValue(record, col.FieldName)?.ToString()?.ToLower();
                if (value == null)
                    continue;
                    
                bool matches = lov.SearchMode switch
                {
                    LOVSearchMode.StartsWith => value.StartsWith(searchText),
                    LOVSearchMode.EndsWith => value.EndsWith(searchText),
                    LOVSearchMode.Exact => value == searchText,
                    _ => value.Contains(searchText)
                };
                
                if (matches)
                    return true;
            }
            
            return false;
        }
        
        private List<object> GetSuggestions(List<object> records, LOVDefinition lov, string searchText, int maxSuggestions)
        {
            if (records == null || !records.Any())
                return new List<object>();
                
            var displayField = lov.DisplayField ?? lov.ReturnField;
            if (string.IsNullOrWhiteSpace(displayField) && lov.Columns.Any())
            {
                displayField = lov.Columns.First().FieldName;
            }
            
            // Find records that partially match
            return records
                .Where(r =>
                {
                    var value = GetPropertyValue(r, displayField)?.ToString();
                    return value != null && 
                           value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                })
                .Take(maxSuggestions)
                .ToList();
        }
        
        private static object GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName))
                return null;
            
            try
            {
                // Handle Dictionary
                if (obj is IDictionary<string, object> dict)
                {
                    return dict.TryGetValue(propertyName, out var val) ? val : null;
                }
                
                // Handle DataRow
                if (obj is DataRow row)
                {
                    return row.Table.Columns.Contains(propertyName) ? row[propertyName] : null;
                }
                
                // Handle regular objects via reflection
                var prop = obj.GetType().GetProperty(propertyName, 
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                return prop?.GetValue(obj);
            }
            catch
            {
                return null;
            }
        }
        
        #endregion
    }
}
