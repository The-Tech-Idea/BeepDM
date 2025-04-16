using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Rules
{
    public interface IRuleParser
    {
        List<IRuleStructure> RuleStructures { get; set; } 
        IRuleStructure ParseRule(string Rule);
        IRuleStructure ParseRule(IRule rule);
        void Clear();


    }
}
