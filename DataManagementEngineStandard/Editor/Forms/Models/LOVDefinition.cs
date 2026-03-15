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
        
        /// <summary>Minimum characters before auto-display triggers</summary>
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
                return true; // No expiration
                
            return (DateTime.Now - CacheTimestamp.Value).TotalMinutes < CacheDurationMinutes;
        }
        
        /// <summary>Clear cached data</summary>
        public void ClearCache()
        {
            CachedData = null;
            CacheTimestamp = null;
        }
        
        /// <summary>Add a column to the LOV</summary>
        public LOVDefinition AddColumn(LOVColumn column)
        {
            Columns.Add(column);
            return this;
        }
        
        /// <summary>Add a related field mapping</summary>
        public LOVDefinition MapField(string lovFieldName, string blockFieldName)
        {
            RelatedFieldMappings[lovFieldName] = blockFieldName;
            return this;
        }
        
        /// <summary>Add a filter</summary>
        public LOVDefinition AddFilter(AppFilter filter)
        {
            Filters.Add(filter);
            return this;
        }
        
        #endregion
        
        #region Factory Methods
        
        /// <summary>Create a simple LOV</summary>
        public static LOVDefinition Create(
            string lovName,
            string dataSourceName,
            string entityName,
            string displayField,
            string returnField = null)
        {
            var lov = new LOVDefinition
            {
                LOVName = lovName,
                DataSourceName = dataSourceName,
                EntityName = entityName,
                DisplayField = displayField,
                ReturnField = returnField ?? displayField,
                Title = $"Select {entityName}"
            };
            
            // Auto-add display and return columns
            lov.Columns.Add(LOVColumn.Create(displayField));
            if (returnField != null && returnField != displayField)
            {
                lov.Columns.Add(LOVColumn.Create(returnField));
            }
            
            return lov;
        }
        
        /// <summary>Create a lookup LOV (code/description style)</summary>
        public static LOVDefinition CreateLookup(
            string lovName,
            string dataSourceName,
            string entityName,
            string codeField,
            string descriptionField)
        {
            return new LOVDefinition
            {
                LOVName = lovName,
                DataSourceName = dataSourceName,
                EntityName = entityName,
                DisplayField = descriptionField,
                ReturnField = codeField,
                Title = $"Select {entityName}",
                Columns = new List<LOVColumn>
                {
                    LOVColumn.Create(codeField, "Code", 80),
                    LOVColumn.Create(descriptionField, "Description", 200)
                }
            };
        }
        
        #endregion
    }
}
