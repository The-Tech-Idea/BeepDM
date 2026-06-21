using System;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Message severity levels for block messages.
    /// </summary>
    public enum MessageLevel
    {
        /// <summary>Informational message.</summary>
        Info,

        /// <summary>Success message.</summary>
        Success,

        /// <summary>Warning message.</summary>
        Warning,

        /// <summary>Error message.</summary>
        Error
    }

    /// <summary>
    /// A platform-agnostic message queued for display on a data block.
    /// </summary>
    public class BlockMessage
    {
        /// <summary>Gets or sets the owning block name</summary>
        public string BlockName { get; set; }

        /// <summary>Gets or sets the message text</summary>
        public string Text { get; set; }

        /// <summary>Gets or sets the severity level</summary>
        public MessageLevel Level { get; set; }

        /// <summary>Gets or sets when the message was created (UTC)</summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Event args for message queue events.
    /// </summary>
    public class BlockMessageEventArgs : EventArgs
    {
        /// <summary>Gets or sets the message</summary>
        public BlockMessage Message { get; set; }

        /// <summary>Gets or sets whether this is a clear event</summary>
        public bool IsClear { get; set; }
    }
}
