using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Event arguments for record-level triggers in Oracle Forms simulation
    /// </summary>
    public class RecordTriggerEventArgs : EventArgs
    {
        /// <summary>Gets the name of the block</summary>
        public string BlockName { get; }
        
        /// <summary>Gets the current record being processed</summary>
        public object CurrentRecord { get; }
        
        /// <summary>Gets or sets the trigger message</summary>
        public string Message { get; set; }
        
        /// <summary>Gets or sets whether the operation should be cancelled</summary>
        public bool Cancel { get; set; }
        
        /// <summary>Gets or sets additional data associated with the trigger</summary>
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        
        /// <summary>Gets the timestamp when the event was created</summary>
        public DateTime Timestamp { get; } = DateTime.Now;
        
        /// <summary>Gets or sets the source of the trigger</summary>
        public string TriggerSource { get; set; }
        
        /// <summary>Gets or sets the record index if applicable</summary>
        public int? RecordIndex { get; set; }
        
        /// <summary>Initializes a new instance of the RecordTriggerEventArgs class</summary>
        /// <param name="blockName">The name of the block</param>
        /// <param name="currentRecord">The current record being processed</param>
        /// <param name="message">Optional message</param>
        public RecordTriggerEventArgs(string blockName, object currentRecord, string message = null)
        {
            BlockName = blockName;
            CurrentRecord = currentRecord;
            Message = message;
        }
    }
}