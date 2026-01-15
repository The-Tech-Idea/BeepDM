using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor.UOW
{
    /// <summary>
    /// Extension methods for UnitOfWorkWrapper to provide additional functionality
    /// and Oracle Forms-like operations
    /// </summary>
    public static class UnitOfWorkWrapperExtensions
    {
        /// <summary>
        /// Executes a function for each record in the unit of work
        /// Oracle Forms equivalent of processing all records in a block
        /// </summary>
        public static async Task<int> ForEachRecordAsync<T>(this IUnitOfWorkWrapper wrapper, 
            Func<dynamic, Task<bool>> processor)
        {
            if (wrapper == null) throw new ArgumentNullException(nameof(wrapper));
            if (processor == null) throw new ArgumentNullException(nameof(processor));

            int processedCount = 0;
            int originalIndex = wrapper.CurrentIndex;

            try
            {
                // Move to first record
                wrapper.MoveFirst();

                // Process all records
                for (int i = 0; i < wrapper.Count; i++)
                {
                    wrapper.MoveTo(i);
                    var currentRecord = wrapper.CurrentItem;
                    
                    if (currentRecord != null)
                    {
                        bool continueProcessing = await processor(currentRecord);
                        processedCount++;
                        
                        if (!continueProcessing)
                            break;
                    }
                }
            }
            finally
            {
                // Restore original position
                if (originalIndex >= 0 && originalIndex < wrapper.Count)
                {
                    wrapper.MoveTo(originalIndex);
                }
            }

            return processedCount;
        }

        /// <summary>
        /// Finds the first record matching the specified predicate
        /// Oracle Forms equivalent of searching for a specific record
        /// </summary>
        public static dynamic FindRecord(this IUnitOfWorkWrapper wrapper, Func<dynamic, bool> predicate)
        {
            if (wrapper == null) throw new ArgumentNullException(nameof(wrapper));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            int originalIndex = wrapper.CurrentIndex;

            try
            {
                // Search through all records
                for (int i = 0; i < wrapper.Count; i++)
                {
                    wrapper.MoveTo(i);
                    var currentRecord = wrapper.CurrentItem;
                    
                    if (currentRecord != null && predicate(currentRecord))
                    {
                        return currentRecord; // Leave cursor at found record
                    }
                }

                return null; // Not found
            }
            catch
            {
                // Restore original position on error
                if (originalIndex >= 0 && originalIndex < wrapper.Count)
                {
                    wrapper.MoveTo(originalIndex);
                }
                throw;
            }
        }

        /// <summary>
        /// Gets all records as a list
        /// </summary>
        public static List<dynamic> GetAllRecords(this IUnitOfWorkWrapper wrapper)
        {
            if (wrapper == null) throw new ArgumentNullException(nameof(wrapper));

            var records = new List<dynamic>();
            int originalIndex = wrapper.CurrentIndex;

            try
            {
                for (int i = 0; i < wrapper.Count; i++)
                {
                    wrapper.MoveTo(i);
                    var currentRecord = wrapper.CurrentItem;
                    if (currentRecord != null)
                    {
                        records.Add(currentRecord);
                    }
                }
            }
            finally
            {
                // Restore original position
                if (originalIndex >= 0 && originalIndex < wrapper.Count)
                {
                    wrapper.MoveTo(originalIndex);
                }
            }

            return records;
        }

        /// <summary>
        /// Safely navigates to a record by primary key value
        /// </summary>
        public static bool NavigateToRecord(this IUnitOfWorkWrapper wrapper, string primaryKeyValue)
        {
            if (wrapper == null) throw new ArgumentNullException(nameof(wrapper));
            if (string.IsNullOrWhiteSpace(primaryKeyValue)) return false;

            try
            {
                var record = wrapper.Get(primaryKeyValue);
                if (record != null)
                {
                    int index = wrapper.Getindex(record);
                    if (index >= 0)
                    {
                        wrapper.MoveTo(index);
                        return true;
                    }
                }
            }
            catch
            {
                // Navigation failed
            }

            return false;
        }

        /// <summary>
        /// Gets the current record position information
        /// Oracle Forms equivalent of getting cursor position
        /// </summary>
        public static RecordPositionInfo GetPositionInfo(this IUnitOfWorkWrapper wrapper)
        {
            if (wrapper == null) throw new ArgumentNullException(nameof(wrapper));

            return new RecordPositionInfo
            {
                CurrentIndex = wrapper.CurrentIndex,
                TotalCount = wrapper.Count,
                IsAtFirst = wrapper.CurrentIndex <= 0,
                IsAtLast = wrapper.CurrentIndex >= wrapper.Count - 1,
                HasRecords = wrapper.Count > 0,
                CurrentRecord = wrapper.CurrentItem
            };
        }

        /// <summary>
        /// Batch insert multiple records with progress reporting
        /// </summary>
        public static async Task<BatchOperationResult> BatchInsertAsync(this IUnitOfWorkWrapper wrapper, 
            IEnumerable<dynamic> records, IProgress<BatchProgress> progress = null)
        {
            if (wrapper == null) throw new ArgumentNullException(nameof(wrapper));
            if (records == null) throw new ArgumentNullException(nameof(records));

            var result = new BatchOperationResult();
            var recordList = records.ToList();
            result.TotalRecords = recordList.Count;

            for (int i = 0; i < recordList.Count; i++)
            {
                try
                {
                    var insertResult = await wrapper.InsertAsync(recordList[i]);
                    if (insertResult.Flag == Errors.Ok)
                    {
                        result.SuccessfulRecords++;
                    }
                    else
                    {
                        result.FailedRecords++;
                        result.Errors.Add($"Record {i + 1}: {insertResult.Message}");
                    }
                }
                catch (Exception ex)
                {
                    result.FailedRecords++;
                    result.Errors.Add($"Record {i + 1}: {ex.Message}");
                }

                // Report progress
                progress?.Report(new BatchProgress
                {
                    CurrentRecord = i + 1,
                    TotalRecords = recordList.Count,
                    SuccessfulRecords = result.SuccessfulRecords,
                    FailedRecords = result.FailedRecords
                });
            }

            result.ProcessedRecords = result.SuccessfulRecords + result.FailedRecords;
            return result;
        }

        /// <summary>
        /// Checks if the wrapper contains any records
        /// </summary>
        public static bool HasRecords(this IUnitOfWorkWrapper wrapper)
        {
            return wrapper?.Count > 0;
        }

        /// <summary>
        /// Safely gets a field value from the current record
        /// </summary>
        public static T GetCurrentFieldValue<T>(this IUnitOfWorkWrapper wrapper, string FieldName, T defaultValue = default(T))
        {
            if (wrapper == null || string.IsNullOrWhiteSpace(FieldName))
                return defaultValue;

            try
            {
                var currentRecord = wrapper.CurrentItem;
                if (currentRecord != null)
                {
                    var value = GetFieldValue(currentRecord, FieldName);
                    if (value != null && value is T)
                        return (T)value;
                }
            }
            catch
            {
                // Return default value on error
            }

            return defaultValue;
        }

        /// <summary>
        /// Safely sets a field value on the current record
        /// </summary>
        public static bool SetCurrentFieldValue(this IUnitOfWorkWrapper wrapper, string FieldName, object value)
        {
            if (wrapper == null || string.IsNullOrWhiteSpace(FieldName))
                return false;

            try
            {
                var currentRecord = wrapper.CurrentItem;
                if (currentRecord != null)
                {
                    return SetFieldValue(currentRecord, FieldName, value);
                }
            }
            catch
            {
                // Field setting failed
            }

            return false;
        }

        #region Private Helper Methods

        private static object GetFieldValue(dynamic record, string FieldName)
        {
            try
            {
                // Try as dynamic object first
                return ((IDictionary<string, object>)record)[FieldName];
            }
            catch
            {
                try
                {
                    // Try reflection
                    var type = record.GetType();
                    var property = type.GetProperty(FieldName);
                    return property?.GetValue(record);
                }
                catch
                {
                    return null;
                }
            }
        }

        private static bool SetFieldValue(dynamic record, string FieldName, object value)
        {
            try
            {
                // Try as dynamic object first
                ((IDictionary<string, object>)record)[FieldName] = value;
                return true;
            }
            catch
            {
                try
                {
                    // Try reflection
                    var type = record.GetType();
                    var property = type.GetProperty(FieldName);
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(record, value);
                        return true;
                    }
                }
                catch
                {
                    // Failed to set value
                }
            }

            return false;
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Information about current record position
    /// </summary>
    public class RecordPositionInfo
    {
        public int CurrentIndex { get; set; }
        public int TotalCount { get; set; }
        public bool IsAtFirst { get; set; }
        public bool IsAtLast { get; set; }
        public bool HasRecords { get; set; }
        public dynamic CurrentRecord { get; set; }

        public string Summary => HasRecords
            ? $"Record {CurrentIndex + 1} of {TotalCount}"
            : "No records";
    }

    /// <summary>
    /// Result of batch operations
    /// </summary>
    public class BatchOperationResult
    {
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan Duration { get; set; }

        public bool IsSuccess => FailedRecords == 0;
        public double SuccessRate => TotalRecords > 0 ? (double)SuccessfulRecords / TotalRecords : 0;
    }

    /// <summary>
    /// Progress information for batch operations
    /// </summary>
    public class BatchProgress
    {
        public int CurrentRecord { get; set; }
        public int TotalRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }

        public double ProgressPercentage => TotalRecords > 0 
            ? (double)CurrentRecord / TotalRecords * 100 
            : 0;
    }

    #endregion
}