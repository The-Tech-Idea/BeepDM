using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.Forms.Models;
using TheTechIdea.Beep.Editor.UOWManager.Interfaces;
using TheTechIdea.Beep.Editor.UOWManager.Models;

namespace TheTechIdea.Beep.Editor.UOWManager.Helpers
{
    /// <summary>
    /// Manages Oracle Forms :SYSTEM.* equivalent system variables
    /// UI-agnostic implementation for FormsManager
    /// </summary>
    public class SystemVariablesManager : ISystemVariablesManager
    {
        #region Fields
        
        private readonly IDMEEditor _dmeEditor;
        private readonly ConcurrentDictionary<string, DataBlockInfo> _blocks;
        private readonly SystemVariables _formSystemVariables;
        private readonly ConcurrentDictionary<string, SystemVariables> _blockSystemVariables;
        private readonly object _lockObject = new object();
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Creates a new SystemVariablesManager
        /// </summary>
        /// <param name="dmeEditor">Data management editor</param>
        /// <param name="blocks">Reference to registered blocks</param>
        public SystemVariablesManager(
            IDMEEditor dmeEditor,
            ConcurrentDictionary<string, DataBlockInfo> blocks)
        {
            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
            _formSystemVariables = new SystemVariables();
            _blockSystemVariables = new ConcurrentDictionary<string, SystemVariables>(StringComparer.OrdinalIgnoreCase);
        }
        
        #endregion
        
        #region System Variables Access
        
        /// <summary>
        /// Get system variables for a specific block
        /// Creates new if doesn't exist
        /// </summary>
        public SystemVariables GetSystemVariables(string blockName)
        {
            if (string.IsNullOrEmpty(blockName))
                return _formSystemVariables;
            
            return _blockSystemVariables.GetOrAdd(blockName, name =>
            {
                var sysVars = new SystemVariables();
                sysVars.CURRENT_BLOCK = name;
                return sysVars;
            });
        }
        
        /// <summary>
        /// Get form-level system variables
        /// </summary>
        public SystemVariables GetFormSystemVariables()
        {
            return _formSystemVariables;
        }
        
        #endregion
        
        #region Block-Level Updates
        
        /// <summary>
        /// Update system variables when switching to a block
        /// </summary>
        public void UpdateForBlockChange(string blockName)
        {
            if (string.IsNullOrEmpty(blockName))
                return;
            
            lock (_lockObject)
            {
                // Update form-level current block
                _formSystemVariables.CURRENT_BLOCK = blockName;
                _formSystemVariables.LAST_OPERATION_TIME = DateTime.Now;
                
                // Get or create block-specific system variables
                var sysVars = GetSystemVariables(blockName);
                sysVars.CURRENT_BLOCK = blockName;
                
                // Update with block info if available
                if (_blocks.TryGetValue(blockName, out var blockInfo))
                {
                    var unitOfWork = blockInfo.UnitOfWork;
                    if (unitOfWork != null)
                    {
                        // Try to get record count from unit of work
                        try
                        {
                            var unitsProperty = unitOfWork.GetType().GetProperty("Units");
                            if (unitsProperty != null)
                            {
                                var units = unitsProperty.GetValue(unitOfWork);
                                if (units != null)
                                {
                                    var countProperty = units.GetType().GetProperty("Count");
                                    var currentIndexProperty = units.GetType().GetProperty("CurrentIndex");
                                    
                                    if (countProperty != null)
                                    {
                                        sysVars.LAST_RECORD = (int)countProperty.GetValue(units);
                                        sysVars.RECORDS_DISPLAYED = sysVars.LAST_RECORD;
                                    }
                                    
                                    if (currentIndexProperty != null)
                                    {
                                        sysVars.CURSOR_RECORD = (int)currentIndexProperty.GetValue(units) + 1; // 1-based
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Ignore reflection errors
                        }
                    }
                    
                    // Check if this block has a master
                    if (!string.IsNullOrEmpty(blockInfo.MasterBlockName))
                    {
                        sysVars.MASTER_BLOCK = blockInfo.MasterBlockName;
                    }
                }
                
                LogOperation($"Block changed to: {blockName}");
            }
        }
        
        /// <summary>
        /// Update system variables when record changes
        /// </summary>
        public void UpdateForRecordChange(string blockName, int recordIndex, int totalRecords)
        {
            if (string.IsNullOrEmpty(blockName))
                return;
            
            lock (_lockObject)
            {
                var sysVars = GetSystemVariables(blockName);
                sysVars.CURSOR_RECORD = recordIndex + 1; // 1-based
                sysVars.LAST_RECORD = totalRecords;
                sysVars.RECORDS_DISPLAYED = totalRecords;
                sysVars.LAST_OPERATION_TIME = DateTime.Now;
                
                // Update form-level
                _formSystemVariables.CURSOR_RECORD = recordIndex + 1;
                _formSystemVariables.LAST_RECORD = totalRecords;
                
                LogOperation($"Record changed in {blockName}: {recordIndex + 1}/{totalRecords}");
            }
        }
        
        /// <summary>
        /// Update system variables when item focus changes
        /// </summary>
        public void UpdateForItemChange(string blockName, string itemName, object itemValue)
        {
            if (string.IsNullOrEmpty(blockName))
                return;
            
            lock (_lockObject)
            {
                var sysVars = GetSystemVariables(blockName);
                sysVars.CURRENT_ITEM = itemName;
                sysVars.CURSOR_ITEM = !string.IsNullOrEmpty(itemName) 
                    ? $"{blockName}.{itemName}" 
                    : null;
                sysVars.CURSOR_VALUE = itemValue;
                sysVars.LAST_OPERATION_TIME = DateTime.Now;
                
                // Update form-level
                _formSystemVariables.CURRENT_ITEM = itemName;
                _formSystemVariables.CURSOR_ITEM = sysVars.CURSOR_ITEM;
                _formSystemVariables.CURSOR_VALUE = itemValue;
            }
        }
        
        #endregion
        
        #region Mode Updates
        
        /// <summary>
        /// Update MODE system variable
        /// </summary>
        public void SetMode(string mode)
        {
            lock (_lockObject)
            {
                _formSystemVariables.MODE = mode ?? "NORMAL";
                _formSystemVariables.LAST_OPERATION_TIME = DateTime.Now;
                
                // Also update current block's mode
                if (!string.IsNullOrEmpty(_formSystemVariables.CURRENT_BLOCK))
                {
                    var sysVars = GetSystemVariables(_formSystemVariables.CURRENT_BLOCK);
                    sysVars.MODE = mode ?? "NORMAL";
                }
                
                LogOperation($"Mode changed to: {mode}");
            }
        }
        
        /// <summary>
        /// Update BLOCK_STATUS system variable
        /// </summary>
        public void SetBlockStatus(string blockName, string status)
        {
            if (string.IsNullOrEmpty(blockName))
                return;
            
            lock (_lockObject)
            {
                var sysVars = GetSystemVariables(blockName);
                sysVars.BLOCK_STATUS = status ?? "NEW";
                sysVars.LAST_OPERATION_TIME = DateTime.Now;
                
                // Update form status if any block has changes
                if (status == "CHANGED")
                {
                    _formSystemVariables.FORM_STATUS = "CHANGED";
                }
            }
        }
        
        /// <summary>
        /// Update FORM_STATUS system variable
        /// </summary>
        public void SetFormStatus(string status)
        {
            lock (_lockObject)
            {
                _formSystemVariables.FORM_STATUS = status ?? "NEW";
                _formSystemVariables.LAST_OPERATION_TIME = DateTime.Now;
            }
        }
        
        /// <summary>
        /// Update RECORD_STATUS system variable
        /// </summary>
        public void SetRecordStatus(string blockName, string status)
        {
            if (string.IsNullOrEmpty(blockName))
                return;
            
            lock (_lockObject)
            {
                var sysVars = GetSystemVariables(blockName);
                sysVars.RECORD_STATUS = status ?? "NEW";
                sysVars.LAST_OPERATION_TIME = DateTime.Now;
                
                _formSystemVariables.RECORD_STATUS = status ?? "NEW";
            }
        }
        
        #endregion
        
        #region Trigger Context Updates
        
        /// <summary>
        /// Set trigger context before trigger execution
        /// </summary>
        public void SetTriggerContext(string triggerType, string blockName, string itemName = null, int recordIndex = 0)
        {
            lock (_lockObject)
            {
                _formSystemVariables.TRIGGER_TYPE = triggerType;
                _formSystemVariables.TRIGGER_FORM = _formSystemVariables.CURRENT_FORM;
                _formSystemVariables.TRIGGER_BLOCK = blockName;
                _formSystemVariables.TRIGGER_RECORD = recordIndex + 1; // 1-based
                
                if (!string.IsNullOrEmpty(itemName))
                {
                    _formSystemVariables.TRIGGER_ITEM = !string.IsNullOrEmpty(blockName)
                        ? $"{blockName}.{itemName}"
                        : itemName;
                }
                
                if (!string.IsNullOrEmpty(blockName))
                {
                    var sysVars = GetSystemVariables(blockName);
                    sysVars.TRIGGER_TYPE = triggerType;
                    sysVars.TRIGGER_BLOCK = blockName;
                    sysVars.TRIGGER_ITEM = _formSystemVariables.TRIGGER_ITEM;
                    sysVars.TRIGGER_RECORD = recordIndex + 1;
                }
            }
        }
        
        /// <summary>
        /// Clear trigger context after trigger execution
        /// </summary>
        public void ClearTriggerContext()
        {
            lock (_lockObject)
            {
                _formSystemVariables.TRIGGER_TYPE = null;
                _formSystemVariables.TRIGGER_BLOCK = null;
                _formSystemVariables.TRIGGER_ITEM = null;
                _formSystemVariables.TRIGGER_RECORD = 0;
                _formSystemVariables.TRIGGER_FORM = null;
            }
        }
        
        #endregion
        
        #region Error Handling
        
        /// <summary>
        /// Set last error information
        /// </summary>
        public void SetLastError(string errorMessage, int errorCode = 0)
        {
            lock (_lockObject)
            {
                _formSystemVariables.LAST_ERROR = errorMessage;
                _formSystemVariables.LAST_ERROR_CODE = errorCode;
                _formSystemVariables.LAST_OPERATION_TIME = DateTime.Now;
                
                if (!string.IsNullOrEmpty(_formSystemVariables.CURRENT_BLOCK))
                {
                    var sysVars = GetSystemVariables(_formSystemVariables.CURRENT_BLOCK);
                    sysVars.LAST_ERROR = errorMessage;
                    sysVars.LAST_ERROR_CODE = errorCode;
                }
                
                LogOperation($"Error set: {errorMessage} (Code: {errorCode})");
            }
        }
        
        /// <summary>
        /// Clear last error
        /// </summary>
        public void ClearLastError()
        {
            lock (_lockObject)
            {
                _formSystemVariables.LAST_ERROR = null;
                _formSystemVariables.LAST_ERROR_CODE = 0;
                
                if (!string.IsNullOrEmpty(_formSystemVariables.CURRENT_BLOCK))
                {
                    var sysVars = GetSystemVariables(_formSystemVariables.CURRENT_BLOCK);
                    sysVars.LAST_ERROR = null;
                    sysVars.LAST_ERROR_CODE = 0;
                }
            }
        }
        
        #endregion
        
        #region Query Information
        
        /// <summary>
        /// Set last query string
        /// </summary>
        public void SetLastQuery(string queryString)
        {
            lock (_lockObject)
            {
                _formSystemVariables.LAST_QUERY = queryString;
                _formSystemVariables.LAST_OPERATION_TIME = DateTime.Now;
                
                if (!string.IsNullOrEmpty(_formSystemVariables.CURRENT_BLOCK))
                {
                    var sysVars = GetSystemVariables(_formSystemVariables.CURRENT_BLOCK);
                    sysVars.LAST_QUERY = queryString;
                }
            }
        }
        
        #endregion
        
        #region Form Context
        
        /// <summary>
        /// Set current form name
        /// </summary>
        public void SetCurrentForm(string formName)
        {
            lock (_lockObject)
            {
                _formSystemVariables.CURRENT_FORM = formName;
                _formSystemVariables.LAST_OPERATION_TIME = DateTime.Now;
                LogOperation($"Form set to: {formName}");
            }
        }
        
        /// <summary>
        /// Reset all system variables
        /// </summary>
        public void Reset()
        {
            lock (_lockObject)
            {
                _formSystemVariables.Reset();
                _blockSystemVariables.Clear();
                LogOperation("System variables reset");
            }
        }
        
        #endregion

        #region Per-Block Snapshot (Phase 8)

        // Dedicated per-block snapshot store (separate from _blockSystemVariables which
        // is lazily-created on any access; this one is only written via UpdateBlockVariables).
        private readonly Dictionary<string, SystemVariables> _blockVars
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Store a rich per-block snapshot. Called by FormsManager after every significant
        /// operation so BeepDataBlock can read system variables without calling FormsManager directly.
        /// </summary>
        public void UpdateBlockVariables(
            string blockName,
            string masterBlockName,
            string mode,
            int cursorRecord,
            int lastRecord,
            int recordsDisplayed,
            bool isQueryMode,
            bool isDirty,
            string triggerItem = null,
            TriggerType? activeTrigger = null)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return;

            lock (_lockObject)
            {
                if (!_blockVars.TryGetValue(blockName, out var sv))
                {
                    sv = new SystemVariables { CURRENT_BLOCK = blockName };
                    _blockVars[blockName] = sv;
                }

                sv.MASTER_BLOCK      = masterBlockName;
                sv.MODE              = mode;
                sv.CURSOR_RECORD     = cursorRecord;
                sv.LAST_RECORD       = lastRecord;
                sv.RECORDS_DISPLAYED = recordsDisplayed;
                sv.BLOCK_STATUS      = isQueryMode ? "Query" : (isDirty ? "Changed" : "Normal");
                sv.LAST_OPERATION_TIME = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(triggerItem))
                {
                    sv.TRIGGER_BLOCK  = blockName;
                    sv.TRIGGER_ITEM   = triggerItem;
                    sv.TRIGGER_FIELD  = triggerItem;
                    sv.TRIGGER_RECORD = cursorRecord;
                }
            }
        }

        /// <summary>
        /// Retrieve the latest per-block snapshot. Returns a new empty instance when none exists.
        /// </summary>
        public SystemVariables GetBlockVariables(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                return new SystemVariables();

            lock (_lockObject)
            {
                return _blockVars.TryGetValue(blockName, out var sv) ? sv : new SystemVariables();
            }
        }

        #endregion
        
        #region Private Helpers
        
        private void LogOperation(string message)
        {
            try
            {
                _dmeEditor?.AddLogMessage("SystemVariablesManager", message, DateTime.Now, -1, null, Errors.Ok);
            }
            catch
            {
                // Ignore logging errors
            }
        }
        
        #endregion
    }
}
