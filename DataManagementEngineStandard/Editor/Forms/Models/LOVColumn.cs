namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// LOV column definition (UI-agnostic)
    /// Defines a column to display in LOV grid
    /// </summary>
    public class LOVColumn
    {
        #region Properties
        
        /// <summary>Field name in the source entity</summary>
        public string FieldName { get; set; }
        
        /// <summary>Display name shown in LOV grid header</summary>
        public string DisplayName { get; set; }
        
        /// <summary>Column width in pixels</summary>
        public int Width { get; set; } = 100;
        
        /// <summary>Whether column is visible in the LOV grid</summary>
        public bool Visible { get; set; } = true;
        
        /// <summary>Whether this column is searchable</summary>
        public bool Searchable { get; set; } = true;
        
        /// <summary>Column format (for dates, numbers, etc.)</summary>
        public string Format { get; set; }
        
        /// <summary>Column alignment</summary>
        public LOVColumnAlignment Alignment { get; set; } = LOVColumnAlignment.Left;
        
        /// <summary>Sort order (0 = not sorted, positive = sort priority)</summary>
        public int SortOrder { get; set; }
        
        /// <summary>Sort direction (true = ascending, false = descending)</summary>
        public bool SortAscending { get; set; } = true;
        
        #endregion
        
        #region Factory Methods
        
        /// <summary>Create a visible column</summary>
        public static LOVColumn Create(string fieldName, string displayName = null, int width = 100)
        {
            return new LOVColumn
            {
                FieldName = fieldName,
                DisplayName = displayName ?? fieldName,
                Width = width,
                Visible = true,
                Searchable = true
            };
        }
        
        /// <summary>Create a hidden column (still part of data, but not shown)</summary>
        public static LOVColumn Hidden(string fieldName)
        {
            return new LOVColumn
            {
                FieldName = fieldName,
                DisplayName = fieldName,
                Visible = false,
                Searchable = false
            };
        }
        
        /// <summary>Create a numeric column (right-aligned)</summary>
        public static LOVColumn Numeric(string fieldName, string displayName = null, int width = 80, string format = null)
        {
            return new LOVColumn
            {
                FieldName = fieldName,
                DisplayName = displayName ?? fieldName,
                Width = width,
                Visible = true,
                Searchable = true,
                Alignment = LOVColumnAlignment.Right,
                Format = format ?? "N2"
            };
        }
        
        /// <summary>Create a date column</summary>
        public static LOVColumn Date(string fieldName, string displayName = null, int width = 100, string format = null)
        {
            return new LOVColumn
            {
                FieldName = fieldName,
                DisplayName = displayName ?? fieldName,
                Width = width,
                Visible = true,
                Searchable = true,
                Alignment = LOVColumnAlignment.Center,
                Format = format ?? "d"
            };
        }
        
        #endregion
    }
}
