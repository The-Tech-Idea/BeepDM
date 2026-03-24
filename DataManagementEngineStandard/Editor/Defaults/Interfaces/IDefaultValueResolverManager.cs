using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor.Defaults.RuleParsing;

namespace TheTechIdea.Beep.Editor.Defaults.Interfaces
{
    /// <summary>
    /// Central resolver pipeline. Manages a registry of <see cref="IDefaultValueResolver"/>
    /// instances and routes rule strings to the correct resolver.
    ///
    /// Phase notes embedded in summary tags show when each member was added to the plan.
    /// </summary>
    public interface IDefaultValueResolverManager
    {
        // ── Registration ────────────────────────────────────────────────────

        /// <summary>
        /// Registers a resolver at the default priority (100).
        /// If a resolver with the same <see cref="IDefaultValueResolver.ResolverName"/> is already
        /// registered it is replaced.
        /// </summary>
        void RegisterResolver(IDefaultValueResolver resolver);

        /// <summary>
        /// Registers a resolver at an explicit priority.
        /// Lower values are tried first — use 0–49 for overrides, 50–99 for custom,
        /// 100 for standard (default), 200+ for fallback resolvers.
        /// [Phase 4]
        /// </summary>
        void RegisterResolver(IDefaultValueResolver resolver, int priority);

        /// <summary>Removes a resolver by name. No-op when the name does not exist.</summary>
        void UnregisterResolver(string resolverName);

        // ── Resolution ──────────────────────────────────────────────────────

        /// <summary>
        /// Routes <paramref name="rule"/> to the first resolver that claims it and returns
        /// the resolved value, or null when no resolver matches.
        /// </summary>
        object ResolveValue(string rule, IPassedArgs parameters);

        /// <summary>
        /// Same as <see cref="ResolveValue"/> but passes the rule through
        /// <c>RuleNormalizer</c> first so dot-style syntax is accepted.
        /// [Phase 1]
        /// </summary>
        object ResolveValueWithNormalization(string rule, IPassedArgs parameters);

        /// <summary>
        /// Resolves the rule and returns a full <see cref="ResolverExecutionResult"/>
        /// with timing, resolver name, fingerprint, and fallback flags.
        /// [Phase 5]
        /// </summary>
        ResolverExecutionResult ResolveWithTelemetry(string rule, IPassedArgs parameters);

        // ── Introspection ───────────────────────────────────────────────────

        /// <summary>Returns a snapshot of all registered resolvers keyed by resolver name.</summary>
        IReadOnlyDictionary<string, IDefaultValueResolver> GetResolvers();

        /// <summary>
        /// Returns the resolver that would handle <paramref name="rule"/>, or null when none matches.
        /// </summary>
        IDefaultValueResolver GetResolverForRule(string rule);

        // ── Rule parsing ────────────────────────────────────────────────────

        /// <summary>
        /// Parses and normalizes a rule string into a structured <see cref="ParsedRule"/>.
        /// Results are cached by the implementation (see <see cref="CompileRule"/>).
        /// [Phase 1]
        /// </summary>
        ParsedRule ParseRule(string rule);

        /// <summary>
        /// Pre-parses and caches a rule so the first live resolution has zero parse overhead.
        /// Safe to call at app startup for all known rules.
        /// [Phase 6]
        /// </summary>
        void CompileRule(string rule);

        /// <summary>
        /// [Phase 6] Clears the value result cache.  Call this when a data-source connection or
        /// configuration changes to prevent stale values from being returned.
        /// </summary>
        void InvalidateValueCache();
    }
}
