using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;
using TheTechIdea.Beep.Editor.Forms.Models;

namespace TheTechIdea.Beep.Editor.UOWManager
{
    public partial class FormsManager
    {
        #region Validation (Required by Interface)

        /// <summary>
        /// Validates a specific field in a block. Routes through both the event manager and
        /// ValidationManager so that registered validation rules are also evaluated.
        /// </summary>
        public bool ValidateField(string blockName, string FieldName, object value)
        {
            try
            {
                // Fire event-based validation (existing behaviour)
                bool eventValid = _eventManager.TriggerFieldValidation(blockName, FieldName, value);

                // Also run registered validation rules via ValidationManager
                PrepareValidationContext(blockName);
                var ruleResult = _validationManager.ValidateItem(blockName, FieldName, value, ValidationTiming.OnChange);
                bool rulesValid = ruleResult?.IsValid != false;

                return eventValid && rulesValid;
            }
            catch (Exception ex)
            {
                LogError($"Error validating field '{FieldName}' in block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return false;
            }
        }

        /// <summary>
        /// Validates all records in a block. Routes through both the event manager and
        /// ValidationManager so that registered validation rules are also evaluated.
        /// </summary>
        public bool ValidateBlock(string blockName)
        {
            try
            {
                var blockInfo = GetBlock(blockName);
                if (blockInfo?.UnitOfWork == null)
                    return true; // No block to validate

                object currentRecord = blockInfo.UnitOfWork.CurrentItem;

                // Fire event-based validation (existing behaviour)
                bool eventValid = _eventManager.TriggerRecordValidation(blockName, currentRecord);

                // Build a flat dictionary from current record for ValidationManager
                bool rulesValid = true;
                if (currentRecord != null)
                {
                    PrepareValidationContext(blockName);
                    var recordDict = currentRecord is IDictionary<string, object> dict
                        ? dict
                        : currentRecord.GetType()
                            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                            .ToDictionary(p => p.Name, p => p.GetValue(currentRecord) as object, StringComparer.OrdinalIgnoreCase);

                    var ruleResult = _validationManager.ValidateRecord(blockName, recordDict, ValidationTiming.Manual);
                    rulesValid = ruleResult?.IsValid != false;
                }

                return eventValid && rulesValid;
            }
            catch (Exception ex)
            {
                LogError($"Error validating block '{blockName}'", ex, blockName);
                _eventManager.TriggerError(blockName, ex);
                return false;
            }
        }

        #endregion

        private void PrepareValidationContext(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                return;

            IDataSource dataSource = null;

            if (_blocks.TryGetValue(blockName, out var blockInfo))
            {
                dataSource = blockInfo.UnitOfWork?.DataSource;

                if (dataSource == null && !string.IsNullOrWhiteSpace(blockInfo.DataSourceName) && _dmeEditor != null)
                {
                    dataSource = _dmeEditor.GetDataSource(blockInfo.DataSourceName);
                }
            }

            if (dataSource != null)
            {
                _validationManager.SetDataSource(dataSource);
            }
        }
    }
}
