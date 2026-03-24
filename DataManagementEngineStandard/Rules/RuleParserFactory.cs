using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules
{
    /// <summary>
    /// Thread-safe registry of <see cref="IRuleParser"/> implementations keyed by rule type.
    /// </summary>
    public sealed class RuleParserFactory : IRuleParserFactory
    {
        private readonly Dictionary<string, IRuleParser> _parsers =
            new Dictionary<string, IRuleParser>(StringComparer.OrdinalIgnoreCase);

        public void RegisterParser(string ruleType, IRuleParser parser)
        {
            if (string.IsNullOrWhiteSpace(ruleType)) throw new ArgumentNullException(nameof(ruleType));
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            if (_parsers.ContainsKey(ruleType))
                throw new RuleCatalogException(DiagnosticCode.DuplicateParserRegistration,
                    $"A parser for rule type '{ruleType}' is already registered.");

            _parsers[ruleType] = parser;
        }

        public IRuleParser GetParser(string ruleType)
        {
            if (string.IsNullOrWhiteSpace(ruleType))
                throw new ArgumentNullException(nameof(ruleType));

            if (_parsers.TryGetValue(ruleType, out var parser))
                return parser;

            throw new RuleCatalogException(DiagnosticCode.CatalogKeyInvalid,
                $"No parser registered for rule type '{ruleType}'.");
        }

        public bool HasParser(string ruleType) =>
            !string.IsNullOrWhiteSpace(ruleType) && _parsers.ContainsKey(ruleType);

        public IEnumerable<string> GetRegisteredTypes() => _parsers.Keys;
    }
}

