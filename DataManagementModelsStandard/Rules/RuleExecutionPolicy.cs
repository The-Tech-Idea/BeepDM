using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules
{
    /// <summary>
    /// Policy profile that governs how a rule is allowed to execute.
    /// Pass one to <see cref="IRuleEngine"/> to restrict runtime behavior.
    /// </summary>
    public class RuleExecutionPolicy
    {
        /// <summary>Default permissive policy (no restrictions).</summary>
        public static readonly RuleExecutionPolicy Default = new RuleExecutionPolicy();

        /// <summary>
        /// Maximum evaluation recursion depth for rule-reference chains.
        /// 0 = unlimited (not recommended in production).
        /// </summary>
        public int MaxDepth { get; set; } = 20;

        /// <summary>
        /// Maximum allowed wall-clock time in milliseconds for a single evaluation.
        /// 0 = no timeout.
        /// </summary>
        public int MaxExecutionMs { get; set; } = 5000;

        /// <summary>
        /// When non-null, only the <see cref="TokenType"/> values in this set are allowed.
        /// Any other operator/token type triggers a <see cref="DiagnosticCode.OperatorNotAllowed"/> fault.
        /// Null means all operators are permitted.
        /// </summary>
        public HashSet<TokenType> AllowedTokenTypes { get; set; } = null;

        /// <summary>
        /// Minimum lifecycle state a rule must be in before it may execute.
        /// Defaults to <see cref="RuleLifecycleState.Draft"/> (all states allowed).
        /// </summary>
        public RuleLifecycleState MinimumLifecycleState { get; set; } = RuleLifecycleState.Draft;

        /// <summary>
        /// When true, a rule in <see cref="RuleLifecycleState.Deprecated"/> state will
        /// raise a <see cref="DiagnosticCode.LifecycleStateViolation"/> warning but still execute.
        /// When false, deprecated rules are fully blocked.
        /// </summary>
        public bool AllowDeprecatedExecution { get; set; } = false;

        /// <summary>Checks whether the given token type is permitted under this policy.</summary>
        public bool IsTokenTypeAllowed(TokenType type)
        {
            return AllowedTokenTypes == null || AllowedTokenTypes.Contains(type);
        }
    }
}
