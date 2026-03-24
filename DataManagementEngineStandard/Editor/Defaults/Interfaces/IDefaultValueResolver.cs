using System.Collections.Generic;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Editor.Defaults.Interfaces
{
    /// <summary>
    /// Contract every default-value resolver must satisfy.
    /// Resolvers are self-describing: they declare which rule types they own
    /// and whether they can handle a specific rule string.
    /// </summary>
    public interface IDefaultValueResolver
    {
        /// <summary>Unique name used for registration and logging (e.g. "DateTimeResolver").</summary>
        string ResolverName { get; }

        /// <summary>
        /// Rule-type tokens this resolver handles, e.g. {"NOW", "TODAY", "DATETIME"}.
        /// Used by the manager to build a fast lookup table.
        /// </summary>
        IEnumerable<string> SupportedRuleTypes { get; }

        /// <summary>
        /// Produce a concrete value for <paramref name="rule"/> given the runtime context.
        /// Returns null when no value can be produced rather than throwing.
        /// </summary>
        object ResolveValue(string rule, IPassedArgs parameters);

        /// <summary>
        /// Fast pre-check — returns true when this resolver is willing to handle the rule.
        /// Called before <see cref="ResolveValue"/> to avoid unnecessary resolver invocations.
        /// </summary>
        bool CanHandle(string rule);

        /// <summary>
        /// Returns example rule strings for tooling and documentation purposes.
        /// </summary>
        IEnumerable<string> GetExamples();
    }
}
