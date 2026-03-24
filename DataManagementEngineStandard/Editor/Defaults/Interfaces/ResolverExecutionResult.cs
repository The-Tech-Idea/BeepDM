using System;

namespace TheTechIdea.Beep.Editor.Defaults.Interfaces
{
    /// <summary>
    /// Immutable record produced by <c>IDefaultValueResolverManager.ResolveWithTelemetry</c>.
    /// Carries full diagnostics for a single resolution attempt.
    /// </summary>
    public sealed class ResolverExecutionResult
    {
        /// <summary>Name of the resolver that handled the rule, or &quot;none&quot; on miss.</summary>
        public string ResolverName { get; init; }

        /// <summary>Rule string exactly as supplied by the caller.</summary>
        public string OriginalRule { get; init; }

        /// <summary>Rule after dot-style normalization by <c>RuleNormalizer</c>.</summary>
        public string NormalizedRule { get; init; }

        /// <summary>Value returned by the winning resolver, or null on failure.</summary>
        public object ResolvedValue { get; init; }

        /// <summary>True when a resolver produced a value without throwing.</summary>
        public bool Succeeded { get; init; }

        /// <summary>True when the primary resolver failed and a fallback value was used.</summary>
        public bool FallbackUsed { get; init; }

        /// <summary>Wall-clock time spent resolving.</summary>
        public TimeSpan Duration { get; init; }

        /// <summary>Exception message when <see cref="Succeeded"/> is false.</summary>
        public string ErrorMessage { get; init; }

        /// <summary>
        /// Short hex fingerprint of the normalized rule, used as a cache/log key.
        /// Computed from the FNV-1a hash of <see cref="NormalizedRule"/>.
        /// </summary>
        public string RuleFingerprint { get; init; }

        // ── static factory helpers ───────────────────────────────────────────

        internal static string ComputeFingerprint(string normalizedRule)
        {
            if (normalizedRule is null) return "00000000";
            // FNV-1a 32-bit
            uint hash = 2166136261u;
            foreach (char c in normalizedRule)
            {
                hash ^= (uint)c;
                hash *= 16777619u;
            }
            return hash.ToString("x8");
        }
    }
}
