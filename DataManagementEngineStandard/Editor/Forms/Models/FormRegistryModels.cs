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
        /// <summary>The form was registered.</summary>
        Registered,

        /// <summary>The form was unregistered.</summary>
        Unregistered,

        /// <summary>The form became active.</summary>
        Activated,

        /// <summary>The form became inactive.</summary>
        Deactivated,

        /// <summary>The form was suspended.</summary>
        Suspended,

        /// <summary>The form was resumed.</summary>
        Resumed
    }

    /// <summary>One entry in the nested form call stack</summary>
    public class FormCallStackEntry
    {
        /// <summary>Gets or sets the called form name.</summary>
        public string FormName { get; set; }

        /// <summary>Gets or sets the caller form name.</summary>
        public string CallerFormName { get; set; }

        /// <summary>Gets or sets how the form was opened.</summary>
        public FormCallMode CallMode { get; set; }

        /// <summary>Gets or sets parameters passed to the form.</summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>Gets or sets when the call was made.</summary>
        public DateTime CalledAt { get; set; } = DateTime.Now;
    }

    /// <summary>A message sent between forms via IFormMessageBus</summary>
    public class FormMessage
    {
        /// <summary>Gets or sets the target form name.</summary>
        public string TargetForm { get; set; }

        /// <summary>Gets or sets the sender form name.</summary>
        public string SenderForm { get; set; }

        /// <summary>Gets or sets the message type identifier.</summary>
        public string MessageType { get; set; }

        /// <summary>Gets or sets the message payload.</summary>
        public object Payload { get; set; }

        /// <summary>Gets or sets when the message was sent.</summary>
        public DateTime SentAt { get; set; } = DateTime.Now;
    }

    /// <summary>Event args for form message bus events</summary>
    public class FormMessageEventArgs : EventArgs
    {
        /// <summary>Gets the form message associated with the event.</summary>
        public FormMessage Message { get; init; }
    }

    /// <summary>Event args for form registry lifecycle events</summary>
    public class FormLifecycleEventArgs : EventArgs
    {
        /// <summary>Gets the form name associated with the lifecycle event.</summary>
        public string FormName { get; init; }

        /// <summary>Gets the lifecycle event type.</summary>
        public FormLifecycleEvent Event { get; init; }
    }

    /// <summary>Event args for shared block change notifications</summary>
    public class SharedBlockChangedEventArgs : EventArgs
    {
        /// <summary>Gets or sets the changed block name.</summary>
        public string BlockName { get; set; }

        /// <summary>Gets or sets the form or actor that made the change.</summary>
        public string ChangedBy { get; set; }

        /// <summary>Gets or sets the changed record payload.</summary>
        public object ChangedRecord { get; set; }

        /// <summary>Gets or sets when the change occurred.</summary>
        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }
}
