using System;
using System.Collections.Generic;
using System.Linq;

namespace TheTechIdea.Beep.Rules
{
    /// <summary>
    /// In-memory rule catalog.  Provides deterministic lookup and lifecycle management
    /// for all registered <see cref="IRuleStructure"/> instances.
    /// </summary>
    public sealed class RuleCatalog : IRuleCatalog
    {
        private readonly Dictionary<string, IRuleStructure> _byGuid =
            new Dictionary<string, IRuleStructure>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, IRuleStructure> _byName =
            new Dictionary<string, IRuleStructure>(StringComparer.OrdinalIgnoreCase);

        public void Register(IRuleStructure structure)
        {
            if (structure == null) throw new ArgumentNullException(nameof(structure));

            if (string.IsNullOrWhiteSpace(structure.GuidID))
                throw new RuleCatalogException(DiagnosticCode.CatalogKeyInvalid,
                    "Rule structure must have a non-empty GuidID.");

            if (_byGuid.ContainsKey(structure.GuidID))
                throw new RuleCatalogException(DiagnosticCode.DuplicateRuleRegistration,
                    $"A rule with GuidID '{structure.GuidID}' is already in the catalog.");

            if (!string.IsNullOrWhiteSpace(structure.Rulename) && _byName.ContainsKey(structure.Rulename))
                throw new RuleCatalogException(DiagnosticCode.DuplicateRuleRegistration,
                    $"A rule named '{structure.Rulename}' is already in the catalog.");

            _byGuid[structure.GuidID] = structure;

            if (!string.IsNullOrWhiteSpace(structure.Rulename))
                _byName[structure.Rulename] = structure;
        }

        public void Unregister(string guidId)
        {
            if (!_byGuid.TryGetValue(guidId, out var s)) return;
            _byGuid.Remove(guidId);
            if (!string.IsNullOrWhiteSpace(s.Rulename))
                _byName.Remove(s.Rulename);
        }

        public IRuleStructure GetByGuid(string guidId) =>
            _byGuid.TryGetValue(guidId, out var s) ? s : null;

        public IRuleStructure GetByName(string ruleName) =>
            _byName.TryGetValue(ruleName, out var s) ? s : null;

        public IEnumerable<IRuleStructure> GetAll() => _byGuid.Values.ToList();

        public IEnumerable<IRuleStructure> GetByModule(string module) =>
            _byGuid.Values.Where(s => string.Equals(s.Module, module, StringComparison.OrdinalIgnoreCase)).ToList();

        public IEnumerable<IRuleStructure> GetByTag(string tag) =>
            _byGuid.Values.Where(s => s.Tags != null &&
                s.Tags.Split(',').Any(t => t.Trim().Equals(tag, StringComparison.OrdinalIgnoreCase))).ToList();

        public IEnumerable<IRuleStructure> GetByLifecycleState(RuleLifecycleState state) =>
            _byGuid.Values.Where(s => s.LifecycleState == state).ToList();

        public void Promote(string guidId, RuleLifecycleState newState)
        {
            if (!_byGuid.TryGetValue(guidId, out var s))
                throw new RuleCatalogException(DiagnosticCode.CatalogKeyInvalid,
                    $"Rule with GuidID '{guidId}' not found in catalog.");

            if ((int)newState < (int)s.LifecycleState)
                throw new RuleCatalogException(DiagnosticCode.LifecycleStateViolation,
                    $"Cannot downgrade lifecycle state from '{s.LifecycleState}' to '{newState}'.");

            s.LifecycleState = newState;
        }
    }
}
