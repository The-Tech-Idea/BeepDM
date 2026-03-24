using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules
{
    public interface IRuleParserFactory
    {
        IRuleParser GetParser(string ruleType);
        void RegisterParser(string ruleType, IRuleParser parser);
        bool HasParser(string ruleType);
        IEnumerable<string> GetRegisteredTypes();
    }
}

