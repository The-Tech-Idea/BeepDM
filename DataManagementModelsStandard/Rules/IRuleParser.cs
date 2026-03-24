using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules
{
    public interface IRuleParser
    {
        List<IRuleStructure> RuleStructures { get; }

        ParseResult ParseRule(string expression);
        ParseResult ParseRule(IRule rule);

        void Clear();
    }
}
