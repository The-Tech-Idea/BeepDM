namespace TheTechIdea.Beep.Services.Telemetry.Retention
{
    /// <summary>
    /// Action taken when a directory exceeds its
    /// <see cref="StorageBudget.MaxTotalBytes"/> cap.
    /// </summary>
    /// <remarks>
    /// Logs default to <see cref="DeleteOldest"/> because individual log
    /// lines are disposable. Audit defaults to <see cref="BlockNewWrites"/>
    /// because deleting old audit records may breach compliance and the
    /// operator must explicitly opt into purges.
    /// </remarks>
    public enum BudgetBreachAction
    {
        /// <summary>
        /// Sweeper deletes oldest files first until the directory is
        /// back under budget. Default for logs.
        /// </summary>
        DeleteOldest = 0,

        /// <summary>
        /// Producers begin failing fast (audit) until the directory
        /// drops back under budget through external means. Default for
        /// audit; safe for compliance scenarios.
        /// </summary>
        BlockNewWrites = 1,

        /// <summary>
        /// No mutation, just emit the breach event for self-observability
        /// (Phase 11) so operators can react manually.
        /// </summary>
        EmitOnly = 2
    }
}
