using System;

namespace TheTechIdea.Beep.Services.Telemetry
{
    /// <summary>
    /// Cross-cutting helpers shared by the logging and audit registration
    /// extensions. Centralizes default constants so both features pick
    /// consistent values.
    /// </summary>
    public static class TelemetryFeature
    {
        /// <summary>Default queue capacity per pipeline.</summary>
        public const int DefaultQueueCapacity = 10_000;

        /// <summary>Default flush interval (drain cadence) for batch writers.</summary>
        public static readonly TimeSpan DefaultFlushInterval = TimeSpan.FromSeconds(2);

        /// <summary>Default cooperative shutdown timeout when <c>FlushAsync</c> is invoked.</summary>
        public static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(5);

        /// <summary>Default storage budget (bytes) for the desktop preset.</summary>
        public const long DefaultDesktopStorageBudgetBytes = 50L * 1024 * 1024;

        /// <summary>Default storage budget (bytes) for the audit feature on desktop.</summary>
        public const long DefaultDesktopAuditBudgetBytes = 200L * 1024 * 1024;

        /// <summary>Default audit retention in days.</summary>
        public const int DefaultAuditRetentionDays = 365;
    }
}
