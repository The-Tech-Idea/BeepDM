using System;

namespace TheTechIdea.Beep.Services.Telemetry.Retention
{
    /// <summary>
    /// Outcome of one sweep of a single <see cref="EnforcerScope"/>.
    /// Returned by <see cref="IBudgetEnforcer.EnforceAsync"/> and raised
    /// through <see cref="IBudgetEnforcer.Swept"/> for self-observability.
    /// </summary>
    public sealed class BudgetSweepResult
    {
        /// <summary>Scope name (mirrors <see cref="EnforcerScope.ResolveName"/>).</summary>
        public string ScopeName { get; set; }

        /// <summary>Directory that was swept.</summary>
        public string Directory { get; set; }

        /// <summary>Total bytes in the directory before the sweep.</summary>
        public long TotalBytesBefore { get; set; }

        /// <summary>Total bytes in the directory after the sweep.</summary>
        public long TotalBytesAfter { get; set; }

        /// <summary>Files deleted (by age, count, or budget).</summary>
        public int FilesDeleted { get; set; }

        /// <summary>Files compressed (raw -&gt; .gz) during the sweep.</summary>
        public int FilesCompressed { get; set; }

        /// <summary>True if the budget cap had to be enforced.</summary>
        public bool BudgetBreachTriggered { get; set; }

        /// <summary>Action that was actually applied at the moment of breach.</summary>
        public BudgetBreachAction ActionTaken { get; set; } = BudgetBreachAction.EmitOnly;

        /// <summary>UTC timestamp at the end of the sweep.</summary>
        public DateTime SweptUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// True if the scope is currently in <c>BlockNewWrites</c>
        /// state — producers should fail-fast until the next successful
        /// sweep clears the flag.
        /// </summary>
        public bool BlockingNewWrites { get; set; }

        /// <summary>Most recent error message, or <c>null</c>.</summary>
        public string LastError { get; set; }
    }
}
