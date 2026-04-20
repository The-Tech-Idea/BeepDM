namespace TheTechIdea.Beep.Services.Audit.Models
{
    /// <summary>
    /// Result of the audited operation. Keeping this an enum (rather than
    /// a free-form string) lets sinks index outcomes cheaply and lets
    /// dashboards group failures without lossy parsing.
    /// </summary>
    public enum AuditOutcome
    {
        /// <summary>Operation completed successfully.</summary>
        Success = 0,

        /// <summary>Operation attempted but failed (system error, validation, etc.).</summary>
        Failure = 1,

        /// <summary>Operation rejected by authorization / policy.</summary>
        Denied = 2,

        /// <summary>Operation accepted but not yet completed (asynchronous flow).</summary>
        Pending = 3,

        /// <summary>Operation was rolled back or compensated after the fact.</summary>
        Compensated = 4
    }
}
