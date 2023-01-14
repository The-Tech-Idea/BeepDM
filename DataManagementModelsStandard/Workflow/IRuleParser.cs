using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Workflow.Interfaces;

namespace TheTechIdea.Beep.Workflow
{
    public interface IRuleParser
    {
        List<IRuleStructure> RuleStructures { get; set; } 
        IDMEEditor DMEEditor { get; set; }
        IRuleStructure ParseRule(string Rule);
        
    }
}
