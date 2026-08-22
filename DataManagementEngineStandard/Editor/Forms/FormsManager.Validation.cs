using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.UOWManager.Helpers;
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

                // SetItemError/ClearItemError had no caller for this path
                // before 2026-08-22 (see ItemChanged's own remark on the same
                // date). Only clear when a rule actually ran and passed — a
                // field with zero registered rules is vacuously "valid" here
                // and must not silently wipe an error some other check (e.g.
                // LOV) set on it.
                if (ruleResult != null && ruleResult.RuleResults.Count > 0)
                {
                    if (rulesValid)
                        _itemPropertyManager?.ClearItemError(blockName, FieldName);
                    else
                        _itemPropertyManager?.SetItemError(blockName, FieldName, ruleResult.FirstError ?? "Validation failed");
                }

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
                    // RecordPropertyAccessor.GetAllReadable returns a
                    // case-insensitive Dictionary<string, object>, which
                    // is the same shape ValidationManager.ValidateRecord
                    // wants. Replaces the GetProperties() + ToDictionary
                    // reflection path that re-scanned the record type on
                    // every validation.
                    var recordDict = currentRecord is IDictionary<string, object> dict
                        ? dict
                        : RecordPropertyAccessor.GetAllReadable(currentRecord, _dmeEditor);

                    var ruleResult = _validationManager.ValidateRecord(blockName, recordDict, ValidationTiming.Manual);
                    rulesValid = ruleResult?.IsValid != false;

                    // Same 2026-08-22 fix as ValidateField, per item in the
                    // record: only touch a field's error state when this pass
                    // actually evaluated a rule against it (RuleResults.Count
                    // > 0) — a field with no registered rules is vacuously
                    // "valid" per ItemValidationResult.IsValid and must not
                    // silently clear an error a different check (e.g. LOV) set.
                    if (ruleResult != null)
                    {
                        foreach (var kvp in ruleResult.ItemResults)
                        {
                            if (kvp.Value.RuleResults.Count == 0) continue;

                            if (kvp.Value.IsValid)
                                _itemPropertyManager?.ClearItemError(blockName, kvp.Key);
                            else
                                _itemPropertyManager?.SetItemError(
                                    blockName, kvp.Key, kvp.Value.FirstError ?? "Validation failed");
                        }
                    }
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
