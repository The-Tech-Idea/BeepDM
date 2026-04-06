using System;

namespace TheTechIdea.Beep.Editor.Forms.Models
{
    /// <summary>
    /// Severity levels for block error log entries.
    /// </summary>
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Represents a single error or warning logged against a block.
    /// </summary>
    public class BlockErrorInfo
    {
        /// <summary>Gets or sets the block that produced the error</summary>
        public string BlockName { get; set; }

        /// <summary>Gets or sets the operation context (e.g. "InsertRecord", "Validation")</summary>
        public string Context { get; set; }

        /// <summary>Gets or sets the severity</summary>
        public ErrorSeverity Severity { get; set; }

        /// <summary>Gets or sets the human-readable message</summary>
        public string Message { get; set; }

        /// <summary>Gets or sets the original exception, if any</summary>
        public Exception Exception { get; set; }

        /// <summary>Gets or sets when the error was logged (UTC)</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Gets the age of this error entry</summary>
        public TimeSpan Age => DateTime.UtcNow - Timestamp;
    }

    /// <summary>
    /// Event args raised by IBlockErrorLog when an error or warning is logged.
    /// </summary>
    public class BlockErrorEventArgs : EventArgs
    {
        /// <summary>Gets or sets the error info</summary>
        public BlockErrorInfo ErrorInfo { get; set; }

        /// <summary>Gets or sets whether the error was handled by a subscriber</summary>
        public bool Handled { get; set; }
    }
}
