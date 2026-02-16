using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Editor
{
    public partial class ObservableBindingList<T>
    {
        #region "Logging"

        private Dictionary<string, object> TrackChanges(T original, T current)
        {
            var changedFields = new Dictionary<string, object>();

            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(current))
            {
                var originalValue = prop.GetValue(original);
                var currentValue = prop.GetValue(current);
                if (!Equals(originalValue, currentValue))
                {
                    if (!ChangedValues.ContainsKey(current))
                    {
                        ChangedValues[current] = new Dictionary<string, object>();
                    }
                    changedFields[prop.Name] = currentValue;
                    ChangedValues[current][prop.Name] = originalValue;
                }
            }

            return changedFields;
        }
        private void CreateLogEntry(T item, LogAction action, Tracking tracking, Dictionary<string, object> changedFields = null)
        {
            if (tracking == null)
            {
                // Create a temporary tracking for logging purposes
                int originalIndex = originalList.IndexOf(item);
                tracking = new Tracking(Guid.NewGuid(), originalIndex >= 0 ? originalIndex : 0, Items.IndexOf(item))
                {
                    EntityState = action == LogAction.Insert ? EntityState.Added : 
                                 action == LogAction.Delete ? EntityState.Deleted : EntityState.Modified
                };
            }
            
            // Check if an entry for this tracking record already exists
            var existingLogEntry = UpdateLog.Values.FirstOrDefault(log =>
                log.TrackingRecord != null && log.TrackingRecord.UniqueId == tracking.UniqueId);

            if (existingLogEntry != null)
            {
                // Update the existing log entry
                existingLogEntry.LogDateandTime = DateTime.Now;
                existingLogEntry.LogAction = action;
                existingLogEntry.UpdatedFields = changedFields ?? new Dictionary<string, object>();
            }
            else
            {
                // BUG 12 fix: Use Guid key instead of DateTime to avoid collision
                var logId = Guid.NewGuid();
                var logEntry = new EntityUpdateInsertLog
                {
                    LogId = logId,
                    LogDateandTime = DateTime.Now,
                    LogUser = CurrentUser,
                    LogAction = action,
                    LogEntity = typeof(T).Name,
                    UpdatedFields = changedFields ?? new Dictionary<string, object>(),
                    TrackingRecord = tracking
                };

                UpdateLog[logId] = logEntry;
            }
        }

        [Obsolete("Use IsLogging instead. This property name contains a typo.")]
        public bool IsLoggin { get => IsLogging; set => IsLogging = value; }

        private Dictionary<string, object> GetChangedFields(T oldItem, T newItem)
        {
            var changedFields = new Dictionary<string, object>();

            foreach (var property in GetCachedProperties())
            {
                var oldValue = property.GetValue(oldItem);
                var newValue = property.GetValue(newItem);

                if (!Equals(oldValue, newValue))
                {
                    changedFields[property.Name] = newValue;
                }
            }

            return changedFields;
        }
        #endregion
    }
}
