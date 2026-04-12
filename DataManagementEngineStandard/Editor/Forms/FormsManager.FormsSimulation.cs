using System;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        #region Oracle Forms Simulation (Delegated)

        /// <summary>
        /// Sets default values for common audit fields when a new record is created
        /// </summary>
        public void SetAuditDefaults(object record, string currentUser = null)
        {
            _formsSimulationHelper.SetAuditDefaults(record, currentUser);
        }

        /// <summary>
        /// Sets a field value on a record using reflection
        /// </summary>
        public bool SetFieldValue(object record, string FieldName, object value)
        {
            return _formsSimulationHelper.SetFieldValue(record, FieldName, value);
        }

        /// <summary>
        /// Gets a field value from a record using reflection
        /// </summary>
        public object GetFieldValue(object record, string FieldName)
        {
            return _formsSimulationHelper.GetFieldValue(record, FieldName);
        }

        /// <summary>
        /// Executes a sequence generator for a field
        /// </summary>
        public bool ExecuteSequence(string blockName, object record, string FieldName, string sequenceName)
        {
            return _formsSimulationHelper.ExecuteSequence(blockName, record, FieldName, sequenceName);
        }

        #endregion
    }
}
