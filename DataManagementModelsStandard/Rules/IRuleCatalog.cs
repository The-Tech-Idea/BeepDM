using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules
{
    /// <summary>
    /// Centralized catalog for rule lifecycle management and discovery.
    /// </summary>
    public interface IRuleCatalog
    {
        // ── Registration ──────────────────────────────────────────────────────────
        void Register(IRuleStructure structure);
        void Unregister(string guidId);

        // ── Lookup ────────────────────────────────────────────────────────────────
        IRuleStructure GetByGuid(string guidId);
        IRuleStructure GetByName(string ruleName);
        IEnumerable<IRuleStructure> GetAll();
        IEnumerable<IRuleStructure> GetByModule(string module);
        IEnumerable<IRuleStructure> GetByTag(string tag);
        IEnumerable<IRuleStructure> GetByLifecycleState(RuleLifecycleState state);

        // ── Lifecycle transitions ─────────────────────────────────────────────────
        void Promote(string guidId, RuleLifecycleState newState);
    }
}
