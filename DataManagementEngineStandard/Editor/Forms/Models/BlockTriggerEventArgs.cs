using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Event arguments for block-level triggers in Oracle Forms simulation
    /// </summary>
    public class BlockTriggerEventArgs : EventArgs
    {
        /// <summary>Gets the name of the block</summary>
        public string BlockName { get; }
        
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
        
        /// <summary>Initializes a new instance of the BlockTriggerEventArgs class</summary>
        /// <param name="blockName">The name of the block</param>
        /// <param name="message">Optional message</param>
        public BlockTriggerEventArgs(string blockName, string message = null)
        {
            BlockName = blockName;
            Message = message;
        }
    }
}