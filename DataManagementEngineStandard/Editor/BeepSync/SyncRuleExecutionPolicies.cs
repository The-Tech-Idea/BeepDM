using TheTechIdea.Beep.Rules;

namespace TheTechIdea.Beep.Editor.BeepSync
{
    /// <summary>
    /// Pre-built <see cref="RuleExecutionPolicy"/> profiles for BeepSync rule evaluations.
    /// Use <see cref="Resolve"/> to select the appropriate profile from a
    /// <see cref="SyncPerformanceProfile.RulePolicyMode"/> string.
    /// </summary>
    public static class SyncRuleExecutionPolicies
    {
        /// <summary>
        /// Safe profile for critical/standard sync paths.
        /// Full depth (10) and deprecation-blocked execution.
        /// </summary>
        public static readonly RuleExecutionPolicy DefaultSafe = new RuleExecutionPolicy
        {
            MaxDepth           = 10,
            MaxExecutionMs     = 5000,
            AllowDeprecatedExecution = false
        };

        /// <summary>
        /// Fast profile for high-volume, non-critical DQ checks.
        /// Reduced depth (3) and shorter timeout for minimal evaluation overhead.
        /// </summary>
        public static readonly RuleExecutionPolicy FastPath = new RuleExecutionPolicy
        {
            MaxDepth           = 3,
            MaxExecutionMs     = 2000,
            AllowDeprecatedExecution = false
        };

        /// <summary>
        /// Returns <see cref="FastPath"/> when <paramref name="mode"/> is
        /// <c>"FastPath"</c> (case-insensitive); otherwise returns <see cref="DefaultSafe"/>.
        /// </summary>
        public static RuleExecutionPolicy Resolve(string mode) =>
            string.Equals(mode, "FastPath", System.StringComparison.OrdinalIgnoreCase)
                ? FastPath
                : DefaultSafe;
    }
}
