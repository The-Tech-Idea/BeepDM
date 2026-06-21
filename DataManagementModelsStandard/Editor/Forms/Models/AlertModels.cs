using System;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Alert icon / severity style.
    /// Mirrors Oracle Forms Alert_Style property.
    /// </summary>
    public enum AlertStyle
    {
        /// <summary>Informational alert — no decision required</summary>
        Information,

        /// <summary>Caution / warning — proceed with care</summary>
        Caution,

        /// <summary>Stop / critical error</summary>
        Stop,

        /// <summary>Question requiring a user decision</summary>
        Question,

        /// <summary>No icon</summary>
        None
    }

    /// <summary>
    /// Result of an alert dialog — which button the user pressed.
    /// Mirrors Oracle Forms SHOW_ALERT return value.
    /// </summary>
    public enum AlertResult
    {
        /// <summary>User pressed the first (default) button</summary>
        Button1,

        /// <summary>User pressed the second button</summary>
        Button2,

        /// <summary>User pressed the third button</summary>
        Button3,

        /// <summary>Dialog was dismissed without a selection (e.g. timeout or no UI provider)</summary>
        None
    }

    /// <summary>
    /// A message displayed in the form status area.
    /// </summary>
    public class StatusMessage
    {
        /// <summary>Gets or sets the status text.</summary>
        public string Text { get; set; }

        /// <summary>Gets or sets the status severity level.</summary>
        public MessageLevel Level { get; set; }

        /// <summary>Gets or sets when the status message was created.</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
