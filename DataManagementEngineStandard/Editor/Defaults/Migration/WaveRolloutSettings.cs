using System.Collections.Generic;

namespace TheTechIdea.Beep.Editor.Defaults.Migration
{
    /// <summary>
    /// Per-resolver wave-rollout policy. Each entry disables / enables a resolver family
    /// for the named rollout environment, allowing phased adoption of the new defaults
    /// system without breaking existing pipelines.
    /// </summary>
    public sealed class WaveRolloutSettings
    {
        public string Environment { get; init; } = "Development";

        /// <summary>Map of resolver name → enabled flag. Missing entries default to enabled.</summary>
        public IReadOnlyDictionary<string, ResolverWaveSettings> Resolvers { get; init; }
            = new Dictionary<string, ResolverWaveSettings>();
    }

    public sealed class ResolverWaveSettings
    {
        public bool Enabled { get; init; } = true;
        public string Wave { get; init; } = "Wave1";
    }
}
