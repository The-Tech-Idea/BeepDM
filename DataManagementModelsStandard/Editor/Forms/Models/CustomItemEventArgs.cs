using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.UOWManager.Models
{
    /// <summary>
    /// Event arguments for host-defined custom item events (WHEN-CUSTOM-ITEM-EVENT).
    /// Carries the event type identifier, block/item names, and an arbitrary payload
    /// dictionary for extensibility without subclassing.
    /// </summary>
    public class CustomItemEventArgs : EventArgs
    {
        public string EventType { get; init; }
        public string BlockName { get; init; }
        public string ItemName { get; init; }
        public object Payload { get; set; }
        public Dictionary<string, object> Properties { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public bool Cancel { get; set; }

        public CustomItemEventArgs(string eventType, string blockName, string itemName, object payload = null)
        {
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
            BlockName = blockName;
            ItemName = itemName;
            Payload = payload;
        }
    }
}
