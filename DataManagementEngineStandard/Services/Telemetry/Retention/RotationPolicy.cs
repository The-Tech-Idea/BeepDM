using System;

namespace TheTechIdea.Beep.Services.Telemetry.Retention
{
    /// <summary>
    /// Operator-facing knobs that tell a rolling sink when to close the
    /// current file and start a new one. The sink itself owns the decision;
    /// this policy is a data-only POCO so it can be persisted, diffed, and
    /// shipped through configuration.
    /// </summary>
    /// <remarks>
    /// Phase 03 hard-coded equivalent values inline in
    /// <see cref="Sinks.FileRollingSink"/>; Phase 04 surfaces them through
    /// this policy so callers can configure them at registration time.
    /// </remarks>
    public sealed class RotationPolicy
    {
        /// <summary>Default rolling threshold (5 MB).</summary>
        public const long DefaultMaxFileBytes = 5L * 1024 * 1024;

        /// <summary>Default wall-clock roll interval (24 hours).</summary>
        public static readonly TimeSpan DefaultRollInterval = TimeSpan.FromHours(24);

        /// <summary>
        /// Maximum bytes per file before rotation. Set to a non-positive
        /// value to disable size-based rotation.
        /// </summary>
        public long MaxFileBytes { get; set; } = DefaultMaxFileBytes;

        /// <summary>
        /// Roll the file whenever wall-clock crosses this interval. Set
        /// to <c>null</c> to disable time-based rotation.
        /// </summary>
        public TimeSpan? RollInterval { get; set; } = DefaultRollInterval;

        /// <summary>
        /// Optional cap on envelope count per file. Sinks that do not
        /// track per-file counts (SQLite) ignore this knob.
        /// </summary>
        public int? MaxEventsPerFile { get; set; }

        /// <summary>Convenience copy used by the registration helpers.</summary>
        public RotationPolicy Clone()
            => new RotationPolicy
            {
                MaxFileBytes = MaxFileBytes,
                RollInterval = RollInterval,
                MaxEventsPerFile = MaxEventsPerFile
            };
    }
}
