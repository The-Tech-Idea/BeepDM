using System;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Event arguments for DML (Data Manipulation Language) triggers
    /// </summary>
    public class DMLTriggerEventArgs : EventArgs
    {
        /// <summary>Gets the name of the block</summary>
        public string BlockName { get; }
        
        /// <summary>Gets the DML operation being performed</summary>
        public DMLOperation Operation { get; }
        
        /// <summary>Gets the unit of work parameters</summary>
        public UnitofWorkParams UnitOfWorkParams { get; }
        
        /// <summary>Gets or sets the current record being processed</summary>
        public object CurrentRecord { get; set; }
        
        /// <summary>Gets or sets a message associated with the trigger</summary>
        public string Message { get; set; }
        
        /// <summary>Gets or sets whether the operation should be cancelled</summary>
        public bool Cancel { get; set; }
        
        /// <summary>Gets the timestamp when the event was created</summary>
        public DateTime Timestamp { get; } = DateTime.Now;
        
        /// <summary>Gets or sets the number of records affected</summary>
        public int RecordsAffected { get; set; }
        
        /// <summary>Initializes a new instance of the DMLTriggerEventArgs class</summary>
        /// <param name="blockName">The name of the block</param>
        /// <param name="operation">The DML operation being performed</param>
        /// <param name="unitOfWorkParams">Optional unit of work parameters</param>
        public DMLTriggerEventArgs(string blockName, DMLOperation operation, UnitofWorkParams unitOfWorkParams = null)
        {
            BlockName = blockName;
            Operation = operation;
            UnitOfWorkParams = unitOfWorkParams;
            
            // Extract the current record from UnitofWorkParams if available
            if (unitOfWorkParams?.Record != null)
            {
                CurrentRecord = unitOfWorkParams.Record;
            }
        }

        /// <summary>
        /// Sets a field value in the current record using reflection
        /// </summary>
        /// <param name="FieldName">Name of the field to set</param>
        /// <param name="value">Value to assign</param>
        public void SetFieldValue(string FieldName, object value)
        {
            if (CurrentRecord == null || string.IsNullOrWhiteSpace(FieldName)) return;
            
            try
            {
                var property = CurrentRecord.GetType().GetProperty(FieldName, 
                    System.Reflection.BindingFlags.IgnoreCase | 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance);
                
                if (property != null && property.CanWrite)
                {
                    var convertedValue = ConvertValue(value, property.PropertyType);
                    property.SetValue(CurrentRecord, convertedValue);
                }
            }
            catch (Exception)
            {
                // Log error but don't throw to avoid breaking the trigger chain
                Message += $"Error setting field '{FieldName}'; ";
            }
        }

        /// <summary>
        /// Gets a field value from the current record using reflection
        /// </summary>
        /// <param name="FieldName">Name of the field to get</param>
        /// <returns>Field value or null if not found</returns>
        public object GetFieldValue(string FieldName)
        {
            if (CurrentRecord == null || string.IsNullOrWhiteSpace(FieldName)) return null;
            
            try
            {
                var property = CurrentRecord.GetType().GetProperty(FieldName, 
                    System.Reflection.BindingFlags.IgnoreCase | 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance);
                
                return property?.GetValue(CurrentRecord);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the current date/time to a field - common Oracle Forms pattern
        /// </summary>
        /// <param name="FieldName">Name of the date field</param>
        public void SetCurrentDateTime(string FieldName)
        {
            SetFieldValue(FieldName, DateTime.Now);
        }

        /// <summary>
        /// Sets the current user to a field - common Oracle Forms pattern
        /// </summary>
        /// <param name="FieldName">Name of the user field</param>
        /// <param name="currentUser">Current user name</param>
        public void SetCurrentUser(string FieldName, string currentUser)
        {
            SetFieldValue(FieldName, currentUser);
        }

        /// <summary>
        /// Checks if a field is null or empty
        /// </summary>
        /// <param name="FieldName">Name of the field to check</param>
        /// <returns>True if field is null or empty</returns>
        public bool IsFieldNullOrEmpty(string FieldName)
        {
            var value = GetFieldValue(FieldName);
            return value == null || value is string str && string.IsNullOrWhiteSpace(str);
        }

        private object ConvertValue(object value, Type targetType)
        {
            if (value == null || targetType.IsAssignableFrom(value.GetType()))
                return value;

            try
            {
                // Handle nullable types
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    targetType = Nullable.GetUnderlyingType(targetType);
                }
                
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }
    }
}