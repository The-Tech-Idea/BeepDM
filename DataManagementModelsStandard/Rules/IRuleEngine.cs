using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules
{
    public interface IRuleEngine
    {
        object EvaluateExpression(IList<Token> tokens, Dictionary<string, object> parameters);
        void RegisterRule(IRule rule);
        (Dictionary<string, object> outputs, object result) SolveRule(string ruleKey, Dictionary<string, object> parameters);
    }
}