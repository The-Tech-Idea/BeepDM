using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Rules
{
    public class RuleParserFactory : IRuleParserFactory
    {
        private readonly IDictionary<string, IRuleParser> _parsers;

        public RuleParserFactory(IDictionary<string, IRuleParser> parsers)
        {
            _parsers = parsers;
        }

        public IRuleParser GetParser(IRuleStructure ruleStructure)
        {
            if (ruleStructure == null || string.IsNullOrWhiteSpace(ruleStructure.RuleType))
                throw new ArgumentException("Rule structure must have a valid RuleType");

            if (_parsers.TryGetValue(ruleStructure.RuleType, out var parser))
                return parser;
            else
                throw new KeyNotFoundException($"No parser registered for rule type: {ruleStructure.RuleType}");
        }
    }

}
