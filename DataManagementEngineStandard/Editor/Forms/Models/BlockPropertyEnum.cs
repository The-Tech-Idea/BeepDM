using System;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Block properties corresponding to Oracle Forms built-in block properties.
    /// Used with SetBlockProperty / GetBlockProperty.
    /// </summary>
    public enum BlockProperty
    {
        /// <summary>Controls whether INSERT is allowed on the block (Oracle: INSERT_ALLOWED)</summary>
        InsertAllowed,

        /// <summary>Controls whether UPDATE is allowed on the block (Oracle: UPDATE_ALLOWED)</summary>
        UpdateAllowed,

        /// <summary>Controls whether DELETE is allowed on the block (Oracle: DELETE_ALLOWED)</summary>
        DeleteAllowed,

        /// <summary>Controls whether QUERY is allowed on the block (Oracle: QUERY_ALLOWED)</summary>
        QueryAllowed,

        /// <summary>Additional WHERE clause appended to every query (Oracle: DEFAULT_WHERE)</summary>
        DefaultWhere,

        /// <summary>Additional ORDER BY clause appended to every query (Oracle: ORDER_BY)</summary>
        OrderBy,

        /// <summary>Whether the UI for the block is enabled for user interaction</summary>
        Enabled,

        /// <summary>Whether the block UI is visible</summary>
        Visible,

        /// <summary>Maximum number of records to display at a time</summary>
        RecordsDisplayed,

        /// <summary>Index of the current record (0-based)</summary>
        CurrentRecordIndex,

        /// <summary>Current operational status / mode label</summary>
        Status
    }
}
