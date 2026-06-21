using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Plain-data container that mirrors the Oracle Forms <c>:SYSTEM</c>
    /// variables. Held by <see cref="TheTechIdea.Beep.Editor.UOWManager.Helpers.SystemVariablesManager"/>
    /// and the wider BeepForms runtime. Two instances are typical per host:
    /// a form-level singleton and one per registered block.
    /// </summary>
    /// <remarks>
    /// The class uses <c>UPPER_SNAKE_CASE</c> public fields rather than
    /// C#-style properties to match the Oracle Forms naming convention that
    /// trigger authors expect when reading <c>:SYSTEM.CURSOR_RECORD</c> etc.
    /// All fields are public so reflection-style access from generated
    /// trigger code remains straightforward.
    /// </remarks>
    public class SystemVariables
    {
        public SystemVariables()
        {
            CURRENT_BLOCK = string.Empty;
            CURRENT_ITEM = string.Empty;
            CURRENT_FORM = string.Empty;
            LAST_OPERATION_TIME = DateTime.MinValue;
            LAST_QUERY = string.Empty;
            LAST_ERROR = string.Empty;
            LAST_ERROR_CODE = 0;
            MODE = "NORMAL";
            BLOCK_STATUS = "NEW";
            FORM_STATUS = "NEW";
            RECORD_STATUS = "NEW";
            MASTER_BLOCK = string.Empty;
            CURSOR_ITEM = string.Empty;
            TRIGGER_FORM = string.Empty;
            TRIGGER_BLOCK = string.Empty;
            TRIGGER_ITEM = string.Empty;
            TRIGGER_FIELD = string.Empty;
            TRIGGER_TYPE = string.Empty;
            CURSOR_VALUE = null;
        }

        // ── Cursor / position ──────────────────────────────────────────
        public string CURRENT_BLOCK { get; set; }
        public string CURRENT_ITEM { get; set; }
        public string CURRENT_FORM { get; set; }
        public string CURSOR_ITEM { get; set; }
        public object CURSOR_VALUE { get; set; }
        public int CURSOR_RECORD { get; set; }

        // ── Counts ──────────────────────────────────────────────────────
        public int LAST_RECORD { get; set; }
        public int RECORDS_DISPLAYED { get; set; }

        // ── Mode / status ──────────────────────────────────────────────
        public string MODE { get; set; }
        public string BLOCK_STATUS { get; set; }
        public string FORM_STATUS { get; set; }
        public string RECORD_STATUS { get; set; }

        // ── Master-detail ──────────────────────────────────────────────
        public string MASTER_BLOCK { get; set; }

        // ── Trigger context ────────────────────────────────────────────
        public string TRIGGER_TYPE { get; set; }
        public string TRIGGER_FORM { get; set; }
        public string TRIGGER_BLOCK { get; set; }
        public string TRIGGER_ITEM { get; set; }
        public string TRIGGER_FIELD { get; set; }
        public int TRIGGER_RECORD { get; set; }

        // ── Query / error ──────────────────────────────────────────────
        public string LAST_QUERY { get; set; }
        public string LAST_ERROR { get; set; }
        public int LAST_ERROR_CODE { get; set; }

        // ── Timestamps ─────────────────────────────────────────────────
        public DateTime LAST_OPERATION_TIME { get; set; }

        /// <summary>
        /// Reset the variables back to their initial form. Used by
        /// <c>SystemVariablesManager.Reset()</c> when a new form is opened
        /// or the host shuts down.
        /// </summary>
        public void Reset()
        {
            CURRENT_BLOCK = string.Empty;
            CURRENT_ITEM = string.Empty;
            CURRENT_FORM = string.Empty;
            LAST_OPERATION_TIME = DateTime.MinValue;
            LAST_QUERY = string.Empty;
            LAST_ERROR = string.Empty;
            LAST_ERROR_CODE = 0;
            MODE = "NORMAL";
            BLOCK_STATUS = "NEW";
            FORM_STATUS = "NEW";
            RECORD_STATUS = "NEW";
            MASTER_BLOCK = string.Empty;
            CURSOR_ITEM = string.Empty;
            CURSOR_VALUE = null;
            CURSOR_RECORD = 0;
            LAST_RECORD = 0;
            RECORDS_DISPLAYED = 0;
            TRIGGER_FORM = string.Empty;
            TRIGGER_BLOCK = string.Empty;
            TRIGGER_ITEM = string.Empty;
            TRIGGER_FIELD = string.Empty;
            TRIGGER_TYPE = string.Empty;
            TRIGGER_RECORD = 0;
        }

        /// <summary>
        /// Returns the snapshot of every supported variable as a
        /// <see cref="System.String"/>-keyed dictionary. Useful when
        /// passing state to trigger handlers on background threads or to
        /// logging surfaces.
        /// </summary>
        public IReadOnlyDictionary<string, object> ToSnapshot()
        {
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["CURRENT_BLOCK"] = CURRENT_BLOCK,
                ["CURRENT_ITEM"] = CURRENT_ITEM,
                ["CURRENT_FORM"] = CURRENT_FORM,
                ["CURSOR_ITEM"] = CURSOR_ITEM,
                ["CURSOR_VALUE"] = CURSOR_VALUE,
                ["CURSOR_RECORD"] = CURSOR_RECORD,
                ["LAST_RECORD"] = LAST_RECORD,
                ["RECORDS_DISPLAYED"] = RECORDS_DISPLAYED,
                ["MODE"] = MODE,
                ["BLOCK_STATUS"] = BLOCK_STATUS,
                ["FORM_STATUS"] = FORM_STATUS,
                ["RECORD_STATUS"] = RECORD_STATUS,
                ["MASTER_BLOCK"] = MASTER_BLOCK,
                ["TRIGGER_TYPE"] = TRIGGER_TYPE,
                ["TRIGGER_FORM"] = TRIGGER_FORM,
                ["TRIGGER_BLOCK"] = TRIGGER_BLOCK,
                ["TRIGGER_ITEM"] = TRIGGER_ITEM,
                ["TRIGGER_FIELD"] = TRIGGER_FIELD,
                ["TRIGGER_RECORD"] = TRIGGER_RECORD,
                ["LAST_QUERY"] = LAST_QUERY,
                ["LAST_ERROR"] = LAST_ERROR,
                ["LAST_ERROR_CODE"] = LAST_ERROR_CODE,
                ["LAST_OPERATION_TIME"] = LAST_OPERATION_TIME
            };
        }
    }
}
