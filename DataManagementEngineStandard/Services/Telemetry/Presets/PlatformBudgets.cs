namespace TheTechIdea.Beep.Services.Telemetry.Presets
{
    /// <summary>
    /// Locked v1 default storage budgets per host platform. Centralized
    /// here so the Phase 12 registration helpers all pick from a single
    /// source of truth and operators have one place to look up the
    /// recommended caps.
    /// </summary>
    /// <remarks>
    /// Logs and audit have separate caps because audit is lossless by
    /// policy and typically needs a longer retention window. The Blazor
    /// targets are deliberately small because IndexedDB quota is shared
    /// with the page; the MAUI targets are small because mobile storage
    /// is constrained and apps that exceed user-visible caps may be
    /// killed by the OS.
    /// </remarks>
    public static class PlatformBudgets
    {
        // ---- Desktop (WinForms / WPF / Console / Avalonia) ------------------
        public const long DesktopLogBytes = 50L * 1024 * 1024;
        public const long DesktopAuditBytes = 200L * 1024 * 1024;

        // ---- Web (ASP.NET Core / Kestrel) -----------------------------------
        public const long WebLogBytes = 500L * 1024 * 1024;
        public const long WebAuditBytes = 2000L * 1024 * 1024;

        // ---- Blazor WebAssembly --------------------------------------------
        public const long BlazorLogBytes = 5L * 1024 * 1024;
        public const long BlazorAuditBytes = 5L * 1024 * 1024;

        // ---- MAUI (Android / iOS / Mac Catalyst / Windows) ------------------
        public const long MauiLogBytes = 20L * 1024 * 1024;
        public const long MauiAuditBytes = 40L * 1024 * 1024;

        // ---- Server queue capacities ---------------------------------------
        // Web/Server hosts run higher-throughput pipelines, so they get a
        // bigger queue than the per-pipeline default. Mobile hosts get a
        // smaller queue because RAM is tighter.
        public const int DesktopQueueCapacity = 10_000;
        public const int WebQueueCapacity = 50_000;
        public const int BlazorQueueCapacity = 1_000;
        public const int MauiQueueCapacity = 2_000;

        // ---- Default file rotation caps (bytes per file) -------------------
        public const long DesktopMaxFileBytes = 10L * 1024 * 1024;
        public const long WebMaxFileBytes = 20L * 1024 * 1024;
        public const long MauiMaxFileBytes = 1L * 1024 * 1024;
    }
}
