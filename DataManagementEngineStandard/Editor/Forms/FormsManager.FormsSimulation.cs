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

        /// <summary>
        /// Sets common Oracle Forms system variables on the record
        /// (SYSTEM_DATE, SYSTEM_DATETIME, SYSTEM_USER, RECORD_STATUS).
        /// </summary>
        /// <remarks>
        /// B6 (audit pass 3, 2026-06): the helper had this
        /// method but the FormsManager wrapper didn't expose
        /// it. Hosts that wanted to set the Oracle Forms
        /// system variables on a record had to call the
        /// helper directly. Now exposed through the engine
        /// for consistency with the other FormsSimulation
        /// wrappers.
        /// </remarks>
        public void SetSystemVariables(object record, SystemVariableType variableType, object value = null)
        {
            _formsSimulationHelper.SetSystemVariables(record, variableType, value);
        }

        /// <summary>
        /// Validates a field's value against a set of field constraints.
        /// </summary>
        /// <remarks>
        /// B6 (audit pass 3, 2026-06): the helper had this
        /// method but the FormsManager wrapper didn't expose
        /// it. Note: per the helper's <see cref="FormsSimulationHelper.ValidateField"/>
        /// remarks, this is effectively a no-op when the
        /// host does not supply explicit
        /// <see cref="FieldConstraints"/> — the engine
        /// doesn't read entity annotations to populate
        /// constraints automatically. Callers should
        /// supply constraints explicitly for any
        /// non-trivial validation.
        /// </remarks>
        public ValidationResult ValidateField(object record, string FieldName, object value, FieldConstraints constraints = null)
        {
            return _formsSimulationHelper.ValidateField(record, FieldName, value, constraints);
        }

        /// <summary>
        /// Clears the current record's field value (sets it to null/empty).
        /// Oracle Forms KEY-CLRITM equivalent. Fire KEY-CLRITM triggers first,
        /// then clear the field value and reset associated item state.
        /// </summary>
        public bool ClearItem(string blockName, string itemName)
        {
            var block = GetBlock(blockName);
            if (block == null) return false;
            var uow = block.UnitOfWork;
            if (uow == null) return false;
            var current = uow.CurrentItem;
            if (current == null) return false;
            if (_itemPropertyManager == null) return false;

            try
            {
                var oldValue = GetFieldValue(current, itemName);
                SetFieldValue(current, itemName, null);

                _itemPropertyManager.ClearItemDirty(blockName, itemName);
                _itemPropertyManager.ClearItemError(blockName, itemName);

                LogOperation($"Cleared item '{itemName}' in block '{blockName}' (was: {oldValue})", blockName);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"ClearItem failed for '{itemName}' in block '{blockName}'", ex, blockName);
                return false;
            }
        }

        #endregion
    }
}
