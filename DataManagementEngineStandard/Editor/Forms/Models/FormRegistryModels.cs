using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>How a form was opened (Oracle Forms: CALL_FORM mode)</summary>
    public enum FormCallMode
    {
        /// <summary>Caller is suspended until callee returns (like Oracle CALL_FORM)</summary>
        Modal,
        /// <summary>Both forms run concurrently (like Oracle OPEN_FORM)</summary>
        Modeless,
        /// <summary>Caller is closed, callee takes over (like Oracle NEW_FORM)</summary>
        Replace
    }

    /// <summary>Form registry lifecycle event types</summary>
    public enum FormLifecycleEvent
    {
        Registered,
        Unregistered,
        Activated,
        Deactivated,
        Suspended,
        Resumed
    }

    /// <summary>One entry in the nested form call stack</summary>
    public class FormCallStackEntry
    {
        public string FormName { get; set; }
        public string CallerFormName { get; set; }
        public FormCallMode CallMode { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public DateTime CalledAt { get; set; } = DateTime.Now;
    }

    /// <summary>A message sent between forms via IFormMessageBus</summary>
    public class FormMessage
    {
        public string TargetForm { get; set; }
        public string SenderForm { get; set; }
        public string MessageType { get; set; }
        public object Payload { get; set; }
        public DateTime SentAt { get; set; } = DateTime.Now;
    }

    /// <summary>Event args for form message bus events</summary>
    public class FormMessageEventArgs : EventArgs
    {
        public FormMessage Message { get; init; }
    }

    /// <summary>Event args for form registry lifecycle events</summary>
    public class FormLifecycleEventArgs : EventArgs
    {
        public string FormName { get; init; }
        public FormLifecycleEvent Event { get; init; }
    }

    /// <summary>Event args for shared block change notifications</summary>
    public class SharedBlockChangedEventArgs : EventArgs
    {
        public string BlockName { get; set; }
        public string ChangedBy { get; set; }
        public object ChangedRecord { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }
}
