namespace TheTechIdea.Beep.Services.Audit.Integrity
{
    /// <summary>
    /// Outcome of a chain verification call. <see cref="Issue"/> is
    /// <c>null</c> when the chain is intact; otherwise it carries a
    /// detailed description of the first detected divergence.
    /// </summary>
    public sealed class IntegrityCheckResult
    {
        /// <summary>Total events inspected by the verifier.</summary>
        public long EventsInspected { get; }

        /// <summary>First detected divergence, or <c>null</c> when the chain is intact.</summary>
        public IntegrityIssue Issue { get; }

        /// <summary>Convenience flag mirroring <c>Issue is null</c>.</summary>
        public bool IsValid => Issue is null;

        /// <summary>Creates a valid (no-issue) result.</summary>
        public static IntegrityCheckResult Ok(long inspected) => new IntegrityCheckResult(inspected, null);

        /// <summary>Creates a failed result.</summary>
        public static IntegrityCheckResult Fail(long inspected, IntegrityIssue issue) =>
            new IntegrityCheckResult(inspected, issue);

        private IntegrityCheckResult(long inspected, IntegrityIssue issue)
        {
            EventsInspected = inspected;
            Issue = issue;
        }
    }
}
