using System;

namespace TheTechIdea.Beep.Services.Telemetry.Retention
{
    /// <summary>
    /// One unit of work for <see cref="IBudgetEnforcer"/> — a single
    /// directory whose telemetry files are governed by the supplied
    /// rotation, retention, and budget policies.
    /// </summary>
    /// <remarks>
    /// A scope is the contract between a sink (which writes files) and
    /// the enforcer (which sweeps them). The enforcer never invents
    /// scopes; callers register one per directory they care about.
    /// </remarks>
    public sealed class EnforcerScope
    {
        /// <summary>
        /// Logical name used in diagnostics events. Defaults to the
        /// last directory segment if the caller does not supply one.
        /// </summary>
        public string Name { get; set; }

        /// <summary>Absolute directory the scope governs.</summary>
        public string Directory { get; set; }

        /// <summary>
        /// File-name search pattern (e.g. <c>"beep-*.ndjson*"</c>) that
        /// matches all files this scope should consider. The trailing
        /// <c>*</c> covers both raw and gzipped siblings.
        /// </summary>
        public string FilePattern { get; set; } = "*.ndjson*";

        /// <summary>Rotation policy hint (used by sinks, not the sweeper).</summary>
        public RotationPolicy Rotation { get; set; } = new RotationPolicy();

        /// <summary>Retention policy applied by the sweeper.</summary>
        public RetentionPolicy Retention { get; set; } = new RetentionPolicy();

        /// <summary>Storage budget enforced by the sweeper.</summary>
        public StorageBudget Budget { get; set; } = new StorageBudget();

        /// <summary>Last time this scope was successfully swept (UTC).</summary>
        public DateTime? LastSweptUtc { get; set; }

        /// <summary>Returns the resolved display name (falls back to directory leaf).</summary>
        public string ResolveName()
        {
            if (!string.IsNullOrWhiteSpace(Name))
            {
                return Name;
            }
            if (string.IsNullOrWhiteSpace(Directory))
            {
                return "(unnamed)";
            }
            return System.IO.Path.GetFileName(Directory.TrimEnd(System.IO.Path.DirectorySeparatorChar));
        }
    }
}
