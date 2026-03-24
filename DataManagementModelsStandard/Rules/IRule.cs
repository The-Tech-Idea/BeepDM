using System.Collections.Generic;

namespace TheTechIdea.Beep.Rules
{
    public interface IRule
    {
        string RuleText { get; set; }
        IRuleStructure Structure { get; set; }
        (Dictionary<string, object> outputs, object result) SolveRule(Dictionary<string, object> parameters = null);
    }
}
