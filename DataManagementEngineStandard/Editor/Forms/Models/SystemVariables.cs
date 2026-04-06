using System;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Oracle Forms :SYSTEM.* equivalent system variables
    /// UI-agnostic implementation for FormsManager
    /// </summary>
    public class SystemVariables
    {
        #region Block Information
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.CURRENT_BLOCK
        /// Name of the block that currently has focus
        /// </summary>
        public string CURRENT_BLOCK { get; set; }
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.MASTER_BLOCK
        /// Name of the master block in a master-detail relationship
        /// </summary>
        public string MASTER_BLOCK { get; set; }
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.TRIGGER_BLOCK
        /// Name of the block from which the current trigger fired
        /// </summary>
        public string TRIGGER_BLOCK { get; set; }
        
        #endregion
        
        #region Record Information
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.CURSOR_RECORD
        /// Current record number (1-based) in the current block
        /// </summary>
        public int CURSOR_RECORD { get; set; }
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.LAST_RECORD
        /// Total number of records in current block
        /// </summary>
        public int LAST_RECORD { get; set; }
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.RECORDS_DISPLAYED
        /// Number of records currently displayed/loaded
        /// </summary>
        public int RECORDS_DISPLAYED { get; set; }
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.RECORD_STATUS
        /// Status of current record: "NEW", "INSERT", "QUERY", "CHANGED"
        /// </summary>
        public string RECORD_STATUS { get; set; }
        
        #endregion
        
        #region Item/Field Information
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.CURSOR_ITEM
        /// Full name of the item that currently has focus (block.item)
        /// </summary>
        public string CURSOR_ITEM { get; set; }
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.TRIGGER_ITEM
        /// Full name of the item that caused the trigger to fire
        /// </summary>
        public string TRIGGER_ITEM { get; set; }

        /// <summary>
        /// Oracle Forms: :SYSTEM.TRIGGER_FIELD
        /// Field name that caused the trigger to fire (alias for TRIGGER_ITEM)
        /// </summary>
        public string TRIGGER_FIELD { get; set; }
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.CURRENT_ITEM
        /// Name of the current item (without block prefix)
        /// </summary>
        public string CURRENT_ITEM { get; set; }
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.CURSOR_VALUE
        /// Value of the item that currently has focus
        /// </summary>
        public object CURSOR_VALUE { get; set; }
        
        #endregion
        
        #region Form Information
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.CURRENT_FORM
        /// Name of the current form
        /// </summary>
        public string CURRENT_FORM { get; set; }
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.TRIGGER_FORM
        /// Name of the form from which the current trigger fired
        /// </summary>
        public string TRIGGER_FORM { get; set; }
        
        #endregion
        
        #region Mode & Status
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.MODE
        /// Current mode: "NORMAL", "ENTER-QUERY", "QUERY"
        /// </summary>
        public string MODE { get; set; } = "NORMAL";
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.BLOCK_STATUS
        /// Status of current block: "NEW", "QUERY", "CHANGED"
        /// </summary>
        public string BLOCK_STATUS { get; set; } = "NEW";
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.FORM_STATUS
        /// Status of form: "NEW", "QUERY", "CHANGED"
        /// </summary>
        public string FORM_STATUS { get; set; } = "NEW";
        
        #endregion
        
        #region Date/Time Variables
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.CURRENT_DATETIME
        /// Current date and time
        /// </summary>
        public DateTime CURRENT_DATETIME => DateTime.Now;
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.EFFECTIVE_DATE
        /// Effective date for database operations
        /// </summary>
        public DateTime EFFECTIVE_DATE { get; set; } = DateTime.Today;
        
        /// <summary>
        /// Last operation timestamp
        /// </summary>
        public DateTime LAST_OPERATION_TIME { get; set; }
        
        #endregion
        
        #region Message & Error Variables
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.MESSAGE_LEVEL
        /// Message suppression level (0-25)
        /// </summary>
        public int MESSAGE_LEVEL { get; set; }
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.SUPPRESS_WORKING
        /// Whether to suppress "Working..." message
        /// </summary>
        public bool SUPPRESS_WORKING { get; set; }
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.LAST_QUERY
        /// The last executed query string
        /// </summary>
        public string LAST_QUERY { get; set; }
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.LAST_ERROR
        /// Last error message
        /// </summary>
        public string LAST_ERROR { get; set; }
        
        /// <summary>
        /// Last error code
        /// </summary>
        public int LAST_ERROR_CODE { get; set; }
        
        #endregion
        
        #region Coordination Variables
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.COORDINATION_OPERATION
        /// Current coordination operation being performed
        /// </summary>
        public string COORDINATION_OPERATION { get; set; }
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.EVENT_WINDOW
        /// Window where the last event occurred
        /// </summary>
        public string EVENT_WINDOW { get; set; }
        
        #endregion
        
        #region Trigger Information
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.TRIGGER_TYPE
        /// Type of the current trigger being executed
        /// </summary>
        public string TRIGGER_TYPE { get; set; }
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.TRIGGER_RECORD
        /// Record number that triggered the event
        /// </summary>
        public int TRIGGER_RECORD { get; set; }
        
        #endregion
        
        #region Tab/Canvas Information
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.TAB_NEW_PAGE
        /// Name of tab page being navigated to
        /// </summary>
        public string TAB_NEW_PAGE { get; set; }
        
        /// <summary>
        /// Oracle Forms: :SYSTEM.TAB_PREVIOUS_PAGE
        /// Name of tab page being navigated from
        /// </summary>
        public string TAB_PREVIOUS_PAGE { get; set; }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Reset all system variables to default state
        /// </summary>
        public void Reset()
        {
            CURRENT_BLOCK = null;
            MASTER_BLOCK = null;
            TRIGGER_BLOCK = null;
            CURSOR_RECORD = 0;
            LAST_RECORD = 0;
            RECORDS_DISPLAYED = 0;
            RECORD_STATUS = "NEW";
            CURSOR_ITEM = null;
            TRIGGER_ITEM = null;
            TRIGGER_FIELD = null;
            CURRENT_ITEM = null;
            CURSOR_VALUE = null;
            MODE = "NORMAL";
            BLOCK_STATUS = "NEW";
            FORM_STATUS = "NEW";
            LAST_QUERY = null;
            LAST_ERROR = null;
            LAST_ERROR_CODE = 0;
            LAST_OPERATION_TIME = DateTime.Now;
        }
        
        /// <summary>
        /// Create a snapshot of current system variables
        /// </summary>
        public SystemVariables Clone()
        {
            return new SystemVariables
            {
                CURRENT_BLOCK = CURRENT_BLOCK,
                MASTER_BLOCK = MASTER_BLOCK,
                TRIGGER_BLOCK = TRIGGER_BLOCK,
                CURSOR_RECORD = CURSOR_RECORD,
                LAST_RECORD = LAST_RECORD,
                RECORDS_DISPLAYED = RECORDS_DISPLAYED,
                RECORD_STATUS = RECORD_STATUS,
                CURSOR_ITEM = CURSOR_ITEM,
                TRIGGER_ITEM = TRIGGER_ITEM,
                CURRENT_ITEM = CURRENT_ITEM,
                CURSOR_VALUE = CURSOR_VALUE,
                CURRENT_FORM = CURRENT_FORM,
                TRIGGER_FORM = TRIGGER_FORM,
                MODE = MODE,
                BLOCK_STATUS = BLOCK_STATUS,
                FORM_STATUS = FORM_STATUS,
                EFFECTIVE_DATE = EFFECTIVE_DATE,
                LAST_OPERATION_TIME = LAST_OPERATION_TIME,
                MESSAGE_LEVEL = MESSAGE_LEVEL,
                SUPPRESS_WORKING = SUPPRESS_WORKING,
                LAST_QUERY = LAST_QUERY,
                LAST_ERROR = LAST_ERROR,
                LAST_ERROR_CODE = LAST_ERROR_CODE,
                COORDINATION_OPERATION = COORDINATION_OPERATION,
                EVENT_WINDOW = EVENT_WINDOW,
                TRIGGER_TYPE = TRIGGER_TYPE,
                TRIGGER_RECORD = TRIGGER_RECORD,
                TAB_NEW_PAGE = TAB_NEW_PAGE,
                TAB_PREVIOUS_PAGE = TAB_PREVIOUS_PAGE
            };
        }
        
        #endregion
    }
}
