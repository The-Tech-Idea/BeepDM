# Phase A3: LOV Manager Migration

## Overview
Migrate the List of Values (LOV) system from `BeepDataBlock` to `FormsManager` in BeepDM.

## Current State

### Source Files (Beep.Winform)
- `TheTechIdea.Beep.Winform.Controls.Integrated/DataBlocks/BeepDataBlock.LOV.cs`
- `TheTechIdea.Beep.Winform.Controls.Integrated/DataBlocks/Models/BeepDataBlockLOV.cs`
- `TheTechIdea.Beep.Winform.Controls.Integrated/Dialogs/BeepLOVDialog.cs` (UI-only, stays)

### Features to Migrate

#### LOV Definition Properties
| Property | Type | Description |
|----------|------|-------------|
| `LOVName` | string | Unique LOV identifier |
| `Title` | string | LOV dialog title |
| `DataSourceName` | string | Data source to query |
| `EntityName` | string | Entity/table name |
| `DisplayField` | string | Field to display |
| `ReturnField` | string | Field to return as value |
| `Columns` | List<LOVColumn> | Column definitions |
| `Filters` | List<AppFilter> | Query filters |
| `WhereClause` | string | WHERE clause |
| `OrderByClause` | string | ORDER BY clause |
| `AllowSearch` | bool | Enable search |
| `SearchMode` | LOVSearchMode | Search mode |
| `Width` | int | Dialog width |
| `Height` | int | Dialog height |
| `AllowMultiSelect` | bool | Multi-select mode |
| `AutoRefresh` | bool | Refresh each open |
| `ValidationType` | LOVValidationType | Validation type |
| `AutoDisplay` | bool | Auto-show on entry |
| `AutoDisplayMinChars` | int | Min chars for auto |
| `AutoPopulateRelatedFields` | bool | Auto-populate related |
| `RelatedFieldMappings` | Dictionary | Field mappings |
| `UseCache` | bool | Cache LOV data |
| `CacheDurationMinutes` | int | Cache expiration |

#### LOVColumn Properties
| Property | Type | Description |
|----------|------|-------------|
| `FieldName` | string | Entity field name |
| `DisplayName` | string | Column header |
| `Width` | int | Column width |
| `Visible` | bool | Is visible |
| `Searchable` | bool | Can search |
| `Format` | string | Display format |
| `Alignment` | LOVColumnAlignment | Text alignment |

#### Enums
| Enum | Values |
|------|--------|
| `LOVValidationType` | ListOnly, Unrestricted, Validated |
| `LOVSearchMode` | Contains, StartsWith, EndsWith, Exact |
| `LOVColumnAlignment` | Left, Center, Right |

#### Methods to Migrate (Data/Logic Layer)
| Method | Description |
|--------|-------------|
| `RegisterLOV()` | Register LOV for item |
| `UnregisterLOV()` | Remove LOV |
| `HasLOV()` | Check if item has LOV |
| `GetLOV()` | Get LOV definition |
| `LoadLOVData()` | Query data from source |
| `FilterLOVData()` | Filter cached data |
| `ValidateLOVValue()` | Validate against LOV |

#### Methods to Keep in BeepDataBlock (UI Layer)
| Method | Description |
|--------|-------------|
| `ShowLOV()` | Display LOV dialog |
| `AttachLOVToComponent()` | Attach F9/DoubleClick handlers |
| `DetachLOVFromComponent()` | Remove handlers |
| `PopulateRelatedFields()` | UI field population |

---

## Target Files (BeepDM)

### File 1: LOVSearchMode Enum
**Path**: `DataManagementModelsStandard/Editor/UOWManager/Models/LOVSearchMode.cs`

```csharp
namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// LOV search mode
    /// </summary>
    public enum LOVSearchMode
    {
        /// <summary>Search anywhere in the string</summary>
        Contains,
        
        /// <summary>Search at start of string</summary>
        StartsWith,
        
        /// <summary>Search at end of string</summary>
        EndsWith,
        
        /// <summary>Exact match</summary>
        Exact
    }
}
```

### File 2: LOVValidationType Enum
**Path**: `DataManagementModelsStandard/Editor/UOWManager/Models/LOVValidationType.cs`

```csharp
namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// LOV validation type (how user input is validated)
    /// </summary>
    public enum LOVValidationType
    {
        /// <summary>
        /// Oracle Forms: Validate From List = Yes
        /// User MUST select from LOV, cannot type custom value
        /// </summary>
        ListOnly,
        
        /// <summary>
        /// Oracle Forms: Validate From List = No
        /// User can type any value, LOV is optional
        /// </summary>
        Unrestricted,
        
        /// <summary>
        /// User can type value, but it must match a value in the LOV
        /// </summary>
        Validated
    }
}
```

### File 3: LOVColumnAlignment Enum
**Path**: `DataManagementModelsStandard/Editor/UOWManager/Models/LOVColumnAlignment.cs`

```csharp
namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// LOV column alignment
    /// </summary>
    public enum LOVColumnAlignment
    {
        Left,
        Center,
        Right
    }
}
```

### File 4: LOVColumn Model
**Path**: `DataManagementModelsStandard/Editor/UOWManager/Models/LOVColumn.cs`

```csharp
namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// LOV column definition (UI-agnostic)
    /// </summary>
    public class LOVColumn
    {
        /// <summary>Field name in the entity</summary>
        public string FieldName { get; set; }
        
        /// <summary>Display name in LOV grid header</summary>
        public string DisplayName { get; set; }
        
        /// <summary>Column width in pixels</summary>
        public int Width { get; set; } = 100;
        
        /// <summary>Whether column is visible</summary>
        public bool Visible { get; set; } = true;
        
        /// <summary>Whether this column is searchable</summary>
        public bool Searchable { get; set; } = true;
        
        /// <summary>Column format (for dates, numbers, etc.)</summary>
        public string Format { get; set; }
        
        /// <summary>Column alignment</summary>
        public LOVColumnAlignment Alignment { get; set; } = LOVColumnAlignment.Left;
    }
}
```

### File 5: LOVDefinition Model
**Path**: `DataManagementModelsStandard/Editor/UOWManager/Models/LOVDefinition.cs`

```csharp
using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// List of Values (LOV) definition (UI-agnostic)
    /// Oracle Forms-compatible LOV system
    /// </summary>
    public class LOVDefinition
    {
        #region Basic Properties
        
        /// <summary>Unique LOV name</summary>
        public string LOVName { get; set; }
        
        /// <summary>LOV dialog title</summary>
        public string Title { get; set; }
        
        /// <summary>Data source name to query LOV data from</summary>
        public string DataSourceName { get; set; }
        
        /// <summary>Entity/table name to query</summary>
        public string EntityName { get; set; }
        
        /// <summary>Field to display in the LOV (visible to user)</summary>
        public string DisplayField { get; set; }
        
        /// <summary>Field to return to the calling item (actual value)</summary>
        public string ReturnField { get; set; }
        
        #endregion
        
        #region Column Configuration
        
        /// <summary>Columns to display in the LOV grid</summary>
        public List<LOVColumn> Columns { get; set; } = new List<LOVColumn>();
        
        #endregion
        
        #region Filtering & Sorting
        
        /// <summary>Additional filters to apply to LOV query</summary>
        public List<AppFilter> Filters { get; set; } = new List<AppFilter>();
        
        /// <summary>WHERE clause for LOV query</summary>
        public string WhereClause { get; set; }
        
        /// <summary>ORDER BY clause for LOV query</summary>
        public string OrderByClause { get; set; }
        
        /// <summary>Whether to allow user to filter/search LOV data</summary>
        public bool AllowSearch { get; set; } = true;
        
        /// <summary>Search mode</summary>
        public LOVSearchMode SearchMode { get; set; } = LOVSearchMode.Contains;
        
        #endregion
        
        #region Display Properties
        
        /// <summary>LOV dialog width</summary>
        public int Width { get; set; } = 600;
        
        /// <summary>LOV dialog height</summary>
        public int Height { get; set; } = 400;
        
        /// <summary>Whether to allow multiple row selection</summary>
        public bool AllowMultiSelect { get; set; }
        
        /// <summary>Whether to show row numbers in LOV grid</summary>
        public bool ShowRowNumbers { get; set; } = true;
        
        /// <summary>Whether to auto-size columns</summary>
        public bool AutoSizeColumns { get; set; } = true;
        
        #endregion
        
        #region Behavior Properties
        
        /// <summary>Whether to refresh LOV data each time it's opened</summary>
        public bool AutoRefresh { get; set; } = true;
        
        /// <summary>Validation type for the LOV</summary>
        public LOVValidationType ValidationType { get; set; } = LOVValidationType.ListOnly;
        
        /// <summary>Whether to auto-drop down LOV on field entry</summary>
        public bool AutoDisplay { get; set; }
        
        /// <summary>Minimum characters before auto-display</summary>
        public int AutoDisplayMinChars { get; set; } = 2;
        
        /// <summary>Whether to automatically populate related fields</summary>
        public bool AutoPopulateRelatedFields { get; set; } = true;
        
        /// <summary>Related field mappings (LOV field → Block field)</summary>
        public Dictionary<string, string> RelatedFieldMappings { get; set; } = new Dictionary<string, string>();
        
        #endregion
        
        #region Cache Properties
        
        /// <summary>Whether to cache LOV data in memory</summary>
        public bool UseCache { get; set; } = true;
        
        /// <summary>Cache duration (minutes, 0 = no expiration)</summary>
        public int CacheDurationMinutes { get; set; } = 30;
        
        /// <summary>Cached data (internal use)</summary>
        internal List<object> CachedData { get; set; }
        
        /// <summary>Cache timestamp (internal use)</summary>
        internal DateTime? CacheTimestamp { get; set; }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>Check if cache is valid</summary>
        public bool IsCacheValid()
        {
            if (!UseCache || CachedData == null || !CacheTimestamp.HasValue)
                return false;
                
            if (CacheDurationMinutes == 0)
                return true;
                
            return (DateTime.Now - CacheTimestamp.Value).TotalMinutes < CacheDurationMinutes;
        }
        
        /// <summary>Clear cached data</summary>
        public void ClearCache()
        {
            CachedData = null;
            CacheTimestamp = null;
        }
        
        #endregion
    }
}
```

### File 6: LOVResult Model
**Path**: `DataManagementModelsStandard/Editor/UOWManager/Models/LOVResult.cs`

```csharp
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Result from LOV data load
    /// </summary>
    public class LOVResult
    {
        /// <summary>Whether load was successful</summary>
        public bool Success { get; set; } = true;
        
        /// <summary>Error message if failed</summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>Loaded data records</summary>
        public List<object> Records { get; set; } = new List<object>();
        
        /// <summary>Total record count (before pagination)</summary>
        public int TotalCount { get; set; }
        
        /// <summary>Whether data was loaded from cache</summary>
        public bool FromCache { get; set; }
    }
    
    /// <summary>
    /// LOV selection result (returned from LOV dialog)
    /// </summary>
    public class LOVSelectionResult
    {
        /// <summary>Whether selection was made</summary>
        public bool Selected { get; set; }
        
        /// <summary>Selected value (return field value)</summary>
        public object SelectedValue { get; set; }
        
        /// <summary>Selected values (if multi-select)</summary>
        public List<object> SelectedValues { get; set; } = new List<object>();
        
        /// <summary>Selected record (full object)</summary>
        public object SelectedRecord { get; set; }
        
        /// <summary>Selected records (if multi-select)</summary>
        public List<object> SelectedRecords { get; set; } = new List<object>();
        
        /// <summary>Related field values to populate</summary>
        public Dictionary<string, object> RelatedFieldValues { get; set; } = new Dictionary<string, object>();
    }
}
```

### File 7: ILOVManager Interface
**Path**: `DataManagementModelsStandard/Editor/UOWManager/Interfaces/ILOVManager.cs`

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>
    /// Manages List of Values (LOV) for blocks
    /// Oracle Forms-compatible LOV engine
    /// </summary>
    public interface ILOVManager
    {
        #region LOV Registration
        
        /// <summary>Register a LOV for a specific block/field</summary>
        void RegisterLOV(string blockName, string fieldName, LOVDefinition lov);
        
        /// <summary>Unregister a LOV</summary>
        void UnregisterLOV(string blockName, string fieldName);
        
        /// <summary>Check if a field has LOV</summary>
        bool HasLOV(string blockName, string fieldName);
        
        /// <summary>Get LOV definition for a field</summary>
        LOVDefinition GetLOV(string blockName, string fieldName);
        
        /// <summary>Get all LOVs for a block</summary>
        Dictionary<string, LOVDefinition> GetBlockLOVs(string blockName);
        
        #endregion
        
        #region LOV Data Operations (UI-Agnostic)
        
        /// <summary>Load LOV data from data source</summary>
        Task<LOVResult> LoadLOVDataAsync(string blockName, string fieldName, string searchText = null);
        
        /// <summary>Load LOV data using specific LOV definition</summary>
        Task<LOVResult> LoadLOVDataAsync(LOVDefinition lov, string searchText = null);
        
        /// <summary>Filter cached LOV data</summary>
        LOVResult FilterLOVData(string blockName, string fieldName, string searchText);
        
        /// <summary>Validate a value against LOV</summary>
        Task<IErrorsInfo> ValidateLOVValueAsync(string blockName, string fieldName, object value);
        
        /// <summary>Get related field values for a selected record</summary>
        Dictionary<string, object> GetRelatedFieldValues(LOVDefinition lov, object selectedRecord);
        
        #endregion
        
        #region Cache Management
        
        /// <summary>Clear LOV cache for a field</summary>
        void ClearLOVCache(string blockName, string fieldName);
        
        /// <summary>Clear all LOV caches for a block</summary>
        void ClearBlockLOVCache(string blockName);
        
        /// <summary>Clear all LOV caches</summary>
        void ClearAllLOVCaches();
        
        /// <summary>Refresh LOV cache for a field</summary>
        Task RefreshLOVCacheAsync(string blockName, string fieldName);
        
        #endregion
    }
}
```

### File 8: LOVManager Implementation
**Path**: `DataManagementEngineStandard/Editor/Forms/Helpers/LOVManager.cs`

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Manages List of Values (LOV) for Oracle Forms-compatible LOV system
    /// </summary>
    public class LOVManager : ILOVManager
    {
        #region Fields
        
        private readonly IDMEEditor _dmeEditor;
        private readonly ConcurrentDictionary<string, DataBlockInfo> _blocks;
        
        // Key: "blockName:fieldName", Value: LOV definition
        private readonly ConcurrentDictionary<string, LOVDefinition> _lovs = new();
        
        #endregion
        
        #region Constructor
        
        public LOVManager(
            IDMEEditor dmeEditor,
            ConcurrentDictionary<string, DataBlockInfo> blocks)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        }
        
        #endregion
        
        #region LOV Registration
        
        public void RegisterLOV(string blockName, string fieldName, LOVDefinition lov)
        {
            if (string.IsNullOrEmpty(blockName))
                throw new ArgumentException("Block name required", nameof(blockName));
            if (string.IsNullOrEmpty(fieldName))
                throw new ArgumentException("Field name required", nameof(fieldName));
            if (lov == null)
                throw new ArgumentNullException(nameof(lov));
                
            var key = GetLOVKey(blockName, fieldName);
            _lovs[key] = lov;
        }
        
        public void UnregisterLOV(string blockName, string fieldName)
        {
            var key = GetLOVKey(blockName, fieldName);
            _lovs.TryRemove(key, out _);
        }
        
        public bool HasLOV(string blockName, string fieldName)
        {
            var key = GetLOVKey(blockName, fieldName);
            return _lovs.ContainsKey(key);
        }
        
        public LOVDefinition GetLOV(string blockName, string fieldName)
        {
            var key = GetLOVKey(blockName, fieldName);
            return _lovs.TryGetValue(key, out var lov) ? lov : null;
        }
        
        public Dictionary<string, LOVDefinition> GetBlockLOVs(string blockName)
        {
            var prefix = $"{blockName}:";
            return _lovs
                .Where(kvp => kvp.Key.StartsWith(prefix))
                .ToDictionary(
                    kvp => kvp.Key.Substring(prefix.Length),
                    kvp => kvp.Value);
        }
        
        #endregion
        
        #region LOV Data Operations
        
        public async Task<LOVResult> LoadLOVDataAsync(string blockName, string fieldName, string searchText = null)
        {
            var lov = GetLOV(blockName, fieldName);
            if (lov == null)
            {
                return new LOVResult
                {
                    Success = false,
                    ErrorMessage = $"No LOV registered for {blockName}.{fieldName}"
                };
            }
            
            return await LoadLOVDataAsync(lov, searchText);
        }
        
        public async Task<LOVResult> LoadLOVDataAsync(LOVDefinition lov, string searchText = null)
        {
            var result = new LOVResult();
            
            try
            {
                // Check cache first
                if (lov.IsCacheValid() && string.IsNullOrEmpty(searchText))
                {
                    result.Records = lov.CachedData;
                    result.TotalCount = lov.CachedData.Count;
                    result.FromCache = true;
                    return result;
                }
                
                // Get data source
                var dataSource = _dmeEditor.GetDataSource(lov.DataSourceName);
                if (dataSource == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Data source '{lov.DataSourceName}' not found";
                    return result;
                }
                
                // Open connection if needed
                if (!dataSource.ConnectionStatus.Equals(ConnectionState.Open))
                {
                    dataSource.Openconnection();
                }
                
                // Build filters
                var filters = new List<AppFilter>(lov.Filters ?? new List<AppFilter>());
                
                // Add search filter
                if (!string.IsNullOrEmpty(searchText))
                {
                    foreach (var col in lov.Columns.Where(c => c.Searchable))
                    {
                        var searchFilter = CreateSearchFilter(col.FieldName, searchText, lov.SearchMode);
                        filters.Add(searchFilter);
                    }
                }
                
                // Query data
                object data;
                if (filters.Count > 0)
                {
                    data = dataSource.GetEntity(lov.EntityName, filters);
                }
                else
                {
                    data = dataSource.GetEntity(lov.EntityName, null);
                }
                
                // Convert to list
                if (data is IEnumerable<object> enumerable)
                {
                    result.Records = enumerable.ToList();
                }
                else if (data != null)
                {
                    result.Records = new List<object> { data };
                }
                
                result.TotalCount = result.Records.Count;
                
                // Cache if not searching and caching enabled
                if (string.IsNullOrEmpty(searchText) && lov.UseCache)
                {
                    lov.CachedData = result.Records;
                    lov.CacheTimestamp = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Error loading LOV data: {ex.Message}";
            }
            
            return result;
        }
        
        public LOVResult FilterLOVData(string blockName, string fieldName, string searchText)
        {
            var lov = GetLOV(blockName, fieldName);
            if (lov == null || !lov.IsCacheValid())
            {
                return new LOVResult { Success = false, ErrorMessage = "No cached data available" };
            }
            
            var result = new LOVResult { FromCache = true };
            
            if (string.IsNullOrEmpty(searchText))
            {
                result.Records = lov.CachedData;
            }
            else
            {
                result.Records = lov.CachedData
                    .Where(r => MatchesSearch(r, lov, searchText))
                    .ToList();
            }
            
            result.TotalCount = result.Records.Count;
            return result;
        }
        
        public async Task<IErrorsInfo> ValidateLOVValueAsync(string blockName, string fieldName, object value)
        {
            var errors = new ErrorsInfo { Flag = Errors.Ok };
            
            var lov = GetLOV(blockName, fieldName);
            if (lov == null)
                return errors;  // No LOV = no validation required
                
            if (lov.ValidationType == LOVValidationType.Unrestricted)
                return errors;  // No validation required
                
            if (value == null)
                return errors;  // Null check handled by required validation
                
            // Load LOV data
            var lovResult = await LoadLOVDataAsync(lov, null);
            if (!lovResult.Success)
            {
                errors.Flag = Errors.Failed;
                errors.Message = lovResult.ErrorMessage;
                return errors;
            }
            
            // Check if value exists in LOV
            var valueStr = value.ToString();
            var returnField = lov.ReturnField ?? lov.DisplayField;
            
            var exists = lovResult.Records.Any(r =>
            {
                var fieldValue = GetPropertyValue(r, returnField);
                return fieldValue?.ToString() == valueStr;
            });
            
            if (!exists)
            {
                errors.Flag = Errors.Failed;
                errors.Message = $"Value '{value}' not found in List of Values";
            }
            
            return errors;
        }
        
        public Dictionary<string, object> GetRelatedFieldValues(LOVDefinition lov, object selectedRecord)
        {
            var values = new Dictionary<string, object>();
            
            if (lov?.RelatedFieldMappings == null || selectedRecord == null)
                return values;
                
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
        
        #endregion
        
        #region Cache Management
        
        public void ClearLOVCache(string blockName, string fieldName)
        {
            var lov = GetLOV(blockName, fieldName);
            lov?.ClearCache();
        }
        
        public void ClearBlockLOVCache(string blockName)
        {
            foreach (var lov in GetBlockLOVs(blockName).Values)
            {
                lov.ClearCache();
            }
        }
        
        public void ClearAllLOVCaches()
        {
            foreach (var lov in _lovs.Values)
            {
                lov.ClearCache();
            }
        }
        
        public async Task RefreshLOVCacheAsync(string blockName, string fieldName)
        {
            var lov = GetLOV(blockName, fieldName);
            if (lov == null)
                return;
                
            lov.ClearCache();
            await LoadLOVDataAsync(lov, null);
        }
        
        #endregion
        
        #region Private Methods
        
        private static string GetLOVKey(string blockName, string fieldName)
        {
            return $"{blockName}:{fieldName}";
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
                    filter.Operator = "=";
                    break;
            }
            
            return filter;
        }
        
        private bool MatchesSearch(object record, LOVDefinition lov, string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
                return true;
                
            searchText = searchText.ToLower();
            
            foreach (var col in lov.Columns.Where(c => c.Searchable))
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
        
        private static object GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName))
                return null;
                
            try
            {
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
```

### File 9: FormsManager.LOV.cs Partial
**Path**: `DataManagementEngineStandard/Editor/Forms/FormsManager.LOV.cs`

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    /// <summary>
    /// FormsManager partial - LOV support
    /// </summary>
    public partial class FormsManager
    {
        #region Fields
        
        private ILOVManager _lovManager;
        
        #endregion
        
        #region Properties
        
        /// <summary>LOV manager</summary>
        public ILOVManager LOVManager => _lovManager;
        
        #endregion
        
        #region LOV Methods
        
        /// <summary>Register a LOV for current block</summary>
        public void RegisterLOV(string fieldName, LOVDefinition lov)
        {
            _lovManager?.RegisterLOV(CurrentBlockName, fieldName, lov);
        }
        
        /// <summary>Register a LOV for specific block</summary>
        public void RegisterLOV(string blockName, string fieldName, LOVDefinition lov)
        {
            _lovManager?.RegisterLOV(blockName, fieldName, lov);
        }
        
        /// <summary>Check if field has LOV</summary>
        public bool HasLOV(string fieldName)
        {
            return _lovManager?.HasLOV(CurrentBlockName, fieldName) ?? false;
        }
        
        /// <summary>Get LOV definition</summary>
        public LOVDefinition GetLOV(string fieldName)
        {
            return _lovManager?.GetLOV(CurrentBlockName, fieldName);
        }
        
        /// <summary>Load LOV data</summary>
        public async Task<LOVResult> LoadLOVDataAsync(string fieldName, string searchText = null)
        {
            return await (_lovManager?.LoadLOVDataAsync(CurrentBlockName, fieldName, searchText)
                ?? Task.FromResult(new LOVResult { Success = false, ErrorMessage = "No LOV manager" }));
        }
        
        /// <summary>Validate value against LOV</summary>
        public async Task<IErrorsInfo> ValidateLOVValueAsync(string fieldName, object value)
        {
            return await (_lovManager?.ValidateLOVValueAsync(CurrentBlockName, fieldName, value)
                ?? Task.FromResult<IErrorsInfo>(new ErrorsInfo { Flag = Errors.Ok }));
        }
        
        /// <summary>Get related field values for selected LOV record</summary>
        public Dictionary<string, object> GetLOVRelatedFieldValues(string fieldName, object selectedRecord)
        {
            var lov = _lovManager?.GetLOV(CurrentBlockName, fieldName);
            return _lovManager?.GetRelatedFieldValues(lov, selectedRecord) 
                ?? new Dictionary<string, object>();
        }
        
        /// <summary>Clear LOV cache</summary>
        public void ClearLOVCache(string fieldName)
        {
            _lovManager?.ClearLOVCache(CurrentBlockName, fieldName);
        }
        
        /// <summary>Clear all LOV caches for current block</summary>
        public void ClearAllLOVCaches()
        {
            _lovManager?.ClearBlockLOVCache(CurrentBlockName);
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>Initialize LOV manager</summary>
        private void InitializeLOVManager()
        {
            _lovManager = new LOVManager(_dmeEditor, _blocks);
        }
        
        #endregion
    }
}
```

---

## Modifications to Existing Files

### File 10: Update FormsManager.cs Constructor
**Path**: `DataManagementEngineStandard/Editor/Forms/FormsManager.cs`

**Add to constructor**:
```csharp
// Initialize LOV manager
InitializeLOVManager();
```

---

## BeepDataBlock Refactoring (Beep.Winform)

After migration, update `BeepDataBlock.LOV.cs` to delegate data logic to FormsManager:

### Updated BeepDataBlock.LOV.cs

```csharp
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Winform.Controls.Integrated.Dialogs;

namespace TheTechIdea.Beep.Winform.Controls
{
    /// <summary>
    /// BeepDataBlock partial - LOV (thin UI wrapper)
    /// Data logic delegated to FormsManager.LOVManager
    /// </summary>
    public partial class BeepDataBlock
    {
        #region LOV Registration (Delegates to FormsManager)
        
        /// <summary>Register a LOV (delegates to FormsManager)</summary>
        public void RegisterLOV(string itemName, LOVDefinition lov)
        {
            _formsManager?.LOVManager?.RegisterLOV(Name, itemName, lov);
            
            // UI-specific: Attach handlers to component
            if (UIComponents.ContainsKey(itemName))
            {
                AttachLOVToComponent(itemName, lov);
            }
        }
        
        /// <summary>Check if item has LOV</summary>
        public bool HasLOV(string itemName)
        {
            return _formsManager?.LOVManager?.HasLOV(Name, itemName) ?? false;
        }
        
        /// <summary>Get LOV definition</summary>
        public LOVDefinition GetLOV(string itemName)
        {
            return _formsManager?.LOVManager?.GetLOV(Name, itemName);
        }
        
        #endregion
        
        #region UI-Specific Methods (Keep in BeepDataBlock)
        
        /// <summary>Attach F9 and DoubleClick handlers to control</summary>
        private void AttachLOVToComponent(string itemName, LOVDefinition lov)
        {
            if (!UIComponents.ContainsKey(itemName))
                return;
                
            var component = UIComponents[itemName];
            
            if (component is Control control)
            {
                // Double-click handler (Oracle Forms standard)
                control.DoubleClick -= LOVDoubleClick;
                control.DoubleClick += LOVDoubleClick;
                
                // F9 key handler (Oracle Forms LOV key)
                control.KeyDown -= LOVKeyDown;
                control.KeyDown += LOVKeyDown;
            }
        }
        
        private async void LOVDoubleClick(object sender, EventArgs e)
        {
            var itemName = GetItemNameFromControl(sender as Control);
            if (!string.IsNullOrEmpty(itemName))
            {
                await ShowLOV(itemName);
            }
        }
        
        private async void LOVKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F9)
            {
                var itemName = GetItemNameFromControl(sender as Control);
                if (!string.IsNullOrEmpty(itemName))
                {
                    await ShowLOV(itemName);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }
        
        /// <summary>Show LOV dialog (UI-specific)</summary>
        public async Task<bool> ShowLOV(string itemName)
        {
            var lov = GetLOV(itemName);
            if (lov == null)
            {
                MessageBox.Show($"No LOV registered for item '{itemName}'", "LOV Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            
            try
            {
                // Load data via FormsManager (business logic)
                var lovResult = await _formsManager.LOVManager.LoadLOVDataAsync(Name, itemName);
                
                if (!lovResult.Success)
                {
                    MessageBox.Show(lovResult.ErrorMessage, "LOV Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                
                if (lovResult.Records.Count == 0)
                {
                    MessageBox.Show("No data available", "LOV Empty",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
                
                // Show dialog (UI-specific)
                using (var dialog = new BeepLOVDialog(lov, lovResult.Records))
                {
                    dialog.InitialValue = GetItemValue(itemName);
                    
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        // Set selected value
                        SetItemValue(itemName, dialog.SelectedValue);
                        
                        // Auto-populate related fields via FormsManager
                        if (lov.AutoPopulateRelatedFields && dialog.SelectedRecord != null)
                        {
                            var relatedValues = _formsManager.LOVManager
                                .GetRelatedFieldValues(lov, dialog.SelectedRecord);
                                
                            foreach (var kvp in relatedValues)
                            {
                                SetItemValue(kvp.Key, kvp.Value);
                            }
                        }
                        
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing LOV: {ex.Message}", "LOV Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            return false;
        }
        
        #endregion
    }
}
```

---

## Verification Steps

1. **Build BeepDM** - Verify no compile errors
2. **Check enums** - Ensure `LOVSearchMode`, `LOVValidationType`, `LOVColumnAlignment` accessible
3. **Check models** - Ensure `LOVDefinition`, `LOVColumn`, `LOVResult` accessible
4. **Check interface** - Ensure `ILOVManager` accessible
5. **Update BeepDataBlock** - Reference new models, delegate data logic
6. **Test LOV** - Verify F9 handler and dialog work via delegation

---

## Dependencies

- **Phase A3 depends on**: None
- **Can be implemented independently**

---

## Files Summary

| File | Action | Location |
|------|--------|----------|
| `LOVSearchMode.cs` | CREATE | `DataManagementModelsStandard/Editor/UOWManager/Models/` |
| `LOVValidationType.cs` | CREATE | `DataManagementModelsStandard/Editor/UOWManager/Models/` |
| `LOVColumnAlignment.cs` | CREATE | `DataManagementModelsStandard/Editor/UOWManager/Models/` |
| `LOVColumn.cs` | CREATE | `DataManagementModelsStandard/Editor/UOWManager/Models/` |
| `LOVDefinition.cs` | CREATE | `DataManagementModelsStandard/Editor/UOWManager/Models/` |
| `LOVResult.cs` | CREATE | `DataManagementModelsStandard/Editor/UOWManager/Models/` |
| `ILOVManager.cs` | CREATE | `DataManagementModelsStandard/Editor/UOWManager/Interfaces/` |
| `LOVManager.cs` | CREATE | `DataManagementEngineStandard/Editor/Forms/Helpers/` |
| `FormsManager.LOV.cs` | CREATE | `DataManagementEngineStandard/Editor/Forms/` |
| `FormsManager.cs` | MODIFY | Add initialization call |
| `BeepDataBlock.LOV.cs` | MODIFY | Thin UI wrapper (Beep.Winform) |
