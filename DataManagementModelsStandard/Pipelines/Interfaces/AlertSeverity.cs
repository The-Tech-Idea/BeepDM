namespace TheTechIdea.Beep.Pipelines.Interfaces
{
    /// <summary>
    /// Severity levels for pipeline alert events.
    /// </summary>
    public enum AlertSeverity
    {
        /// <summary>Informational — run started, completed, etc.</summary>
        Info,

        /// <summary>Non-critical issue that may need attention.</summary>
        Warning,

        /// <summary>Serious failure requiring immediate attention.</summary>
        Error,

        /// <summary>Run-stopping failure.</summary>
        Critical
    }
}
