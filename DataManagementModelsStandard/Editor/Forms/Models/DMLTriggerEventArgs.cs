using System;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Event arguments for DML (Data Manipulation Language) triggers.
    /// Carry block name, operation, current record, and cancellation state.
    /// Field-value accessors are convenience wrappers for trigger handlers.
    /// Prefer <c>FormsManager.SetFieldValue</c> / <c>FormsManager.GetFieldValue</c>
    /// or <c>TriggerContext.FormsManager</c> for new code — the context gives
    /// the trigger handler full engine access including validation and LOVs.
    /// </summary>
    public class DMLTriggerEventArgs : EventArgs
    {
        public DMLTriggerEventArgs(string blockName, DMLOperation operation, UnitofWorkParams unitOfWorkParams = null)
        {
            BlockName = blockName;
            Operation = operation;
            UnitOfWorkParams = unitOfWorkParams;

            if (unitOfWorkParams?.Record != null)
                CurrentRecord = unitOfWorkParams.Record;
        }

        public string BlockName { get; }
        public DMLOperation Operation { get; }
        public UnitofWorkParams UnitOfWorkParams { get; }
        public object CurrentRecord { get; set; }
        public string Message { get; set; }
        public bool Cancel { get; set; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public int RecordsAffected { get; set; }

        [Obsolete("Use FormsManager.SetFieldValue or TriggerContext.FormsManager.SetFieldValue from a trigger handler instead.")]
        public void SetFieldValue(string fieldName, object value)
        {
            if (CurrentRecord == null || string.IsNullOrWhiteSpace(fieldName)) return;
            try
            {
                var pi = CurrentRecord.GetType().GetProperty(fieldName,
                    System.Reflection.BindingFlags.IgnoreCase |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);
                if (pi != null && pi.CanWrite)
                    pi.SetValue(CurrentRecord, ConvertValue(value, pi.PropertyType));
            }
            catch { /* best-effort — trigger chain must not break */ }
        }

        [Obsolete("Use FormsManager.GetFieldValue or TriggerContext.FormsManager.GetFieldValue from a trigger handler instead.")]
        public object GetFieldValue(string fieldName)
        {
            if (CurrentRecord == null || string.IsNullOrWhiteSpace(fieldName)) return null;
            try
            {
                return CurrentRecord.GetType().GetProperty(fieldName,
                    System.Reflection.BindingFlags.IgnoreCase |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance)?.GetValue(CurrentRecord);
            }
            catch { return null; }
        }

        [Obsolete("Use FormsManager.SetFieldValue(record, fieldName, DateTime.UtcNow) from a trigger handler instead.")]
        public void SetCurrentDateTime(string fieldName)
            => SetFieldValue(fieldName, DateTime.UtcNow);

        [Obsolete("Use FormsManager.SetFieldValue(record, fieldName, currentUser) from a trigger handler instead.")]
        public void SetCurrentUser(string fieldName, string currentUser)
            => SetFieldValue(fieldName, currentUser);

        [Obsolete("Use FormsManager.GetFieldValue and check for null/empty from a trigger handler instead.")]
        public bool IsFieldNullOrEmpty(string fieldName)
        {
            var value = GetFieldValue(fieldName);
            return value == null || (value is string s && string.IsNullOrWhiteSpace(s));
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null || targetType.IsInstanceOfType(value))
                return value;
            try
            {
                var effective = Nullable.GetUnderlyingType(targetType) ?? targetType;
                return Convert.ChangeType(value, effective);
            }
            catch
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }
    }
}
