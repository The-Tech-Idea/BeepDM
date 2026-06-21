using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Result from LOV data load operation
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
        
        /// <summary>Time taken to load data (milliseconds)</summary>
        public long LoadTimeMs { get; set; }
        
        #region Factory Methods
        
        /// <summary>Create a success result with records</summary>
        public static LOVResult Ok(List<object> records, bool fromCache = false)
        {
            return new LOVResult
            {
                Success = true,
                Records = records ?? new List<object>(),
                TotalCount = records?.Count ?? 0,
                FromCache = fromCache
            };
        }
        
        /// <summary>Create a failed result with error message</summary>
        public static LOVResult Fail(string errorMessage)
        {
            return new LOVResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
        
        #endregion
    }
    
    /// <summary>
    /// LOV selection result (returned from LOV dialog)
    /// </summary>
    public class LOVSelectionResult
    {
        /// <summary>Whether user made a selection (vs cancelled)</summary>
        public bool Selected { get; set; }
        
        /// <summary>Selected value (return field value)</summary>
        public object SelectedValue { get; set; }
        
        /// <summary>Selected display value</summary>
        public object DisplayValue { get; set; }
        
        /// <summary>Selected values (if multi-select enabled)</summary>
        public List<object> SelectedValues { get; set; } = new List<object>();
        
        /// <summary>Selected record (full object)</summary>
        public object SelectedRecord { get; set; }
        
        /// <summary>Selected records (if multi-select enabled)</summary>
        public List<object> SelectedRecords { get; set; } = new List<object>();
        
        /// <summary>Related field values to populate in the block</summary>
        public Dictionary<string, object> RelatedFieldValues { get; set; } = new Dictionary<string, object>();
        
        #region Factory Methods
        
        /// <summary>Create a selected result</summary>
        public static LOVSelectionResult Select(object value, object record = null, Dictionary<string, object> relatedValues = null)
        {
            return new LOVSelectionResult
            {
                Selected = true,
                SelectedValue = value,
                SelectedRecord = record,
                RelatedFieldValues = relatedValues ?? new Dictionary<string, object>()
            };
        }
        
        /// <summary>Create a cancelled result</summary>
        public static LOVSelectionResult Cancelled()
        {
            return new LOVSelectionResult { Selected = false };
        }
        
        #endregion
    }
    
    /// <summary>
    /// LOV validation result
    /// </summary>
    public class LOVValidationResult
    {
        /// <summary>Whether the value is valid</summary>
        public bool IsValid { get; set; }
        
        /// <summary>Error message if invalid</summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>The matched record (if value exists in LOV)</summary>
        public object MatchedRecord { get; set; }
        
        /// <summary>Suggested values (partial matches)</summary>
        public List<object> Suggestions { get; set; } = new List<object>();
        
        #region Factory Methods
        
        /// <summary>Create a valid result</summary>
        public static LOVValidationResult Valid(object matchedRecord = null)
        {
            return new LOVValidationResult
            {
                IsValid = true,
                MatchedRecord = matchedRecord
            };
        }
        
        /// <summary>Create an invalid result</summary>
        public static LOVValidationResult Invalid(string errorMessage, List<object> suggestions = null)
        {
            return new LOVValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage,
                Suggestions = suggestions ?? new List<object>()
            };
        }
        
        #endregion
    }
}
