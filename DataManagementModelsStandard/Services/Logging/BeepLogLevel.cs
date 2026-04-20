namespace TheTechIdea.Beep.Services.Logging
{
    /// <summary>
    /// Severity classification for <see cref="IBeepLog"/> entries.
    /// Mirrors <c>Microsoft.Extensions.Logging.LogLevel</c> so the values can be
    /// projected one-to-one when bridging through the Microsoft logger provider.
    /// Defined in the Models project so the contract stays free of any
    /// <c>Microsoft.Extensions.Logging</c> dependency.
    /// </summary>
    public enum BeepLogLevel
    {
        /// <summary>Most detailed messages; usually disabled in production.</summary>
        Trace = 0,

        /// <summary>Messages used for interactive investigation during development.</summary>
        Debug = 1,

        /// <summary>Tracks the general flow of the application.</summary>
        Information = 2,

        /// <summary>Highlights an abnormal or unexpected event in the application flow.</summary>
        Warning = 3,

        /// <summary>The application has hit a current flow stop because of a failure.</summary>
        Error = 4,

        /// <summary>An unrecoverable application or system crash, or a catastrophic failure.</summary>
        Critical = 5,

        /// <summary>No messages are written. Used to disable logging entirely.</summary>
        None = 6,
    }
}
