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

    /// <summary>
    /// A named, reusable Alert object (Oracle Forms ALERT). Unlike the
    /// ad-hoc <c>ShowAlertAsync(title, message, ...)</c> overload, which takes
    /// its content literally on every call, a definition is authored once —
    /// with a name — and invoked by that name from any trigger, matching
    /// Oracle's own SHOW_ALERT('alert_name') built-in.
    /// </summary>
    public class AlertDefinition
    {
        /// <summary>Gets or sets the alert's unique name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the alert's title bar text.</summary>
        public string Title { get; set; }

        /// <summary>Gets or sets the alert's message body.</summary>
        public string Message { get; set; }

        /// <summary>Gets or sets the alert's icon/severity style.</summary>
        public AlertStyle Style { get; set; } = AlertStyle.None;

        /// <summary>Gets or sets the first (always present) button's label.</summary>
        public string Button1Text { get; set; } = "OK";

        /// <summary>Gets or sets the second button's label, or null to omit it.</summary>
        public string Button2Text { get; set; }

        /// <summary>Gets or sets the third button's label, or null to omit it.</summary>
        public string Button3Text { get; set; }

        /// <summary>Gets or sets when this alert definition was created.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public AlertDefinition() { }

        public AlertDefinition(
            string name, string title, string message,
            AlertStyle style = AlertStyle.None,
            string button1Text = "OK", string button2Text = null, string button3Text = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Title = title;
            Message = message;
            Style = style;
            Button1Text = button1Text;
            Button2Text = button2Text;
            Button3Text = button3Text;
        }
    }
}
