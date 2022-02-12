using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep;
using TheTechIdea.DataManagment_Engine.Workflow.Interfaces;

namespace TheTechIdea.DataManagment_Engine.Workflow
{
    public interface IRuleParser
    {
        List<IRuleStructure> RuleStructures { get; set; } 
        IDMEEditor DMEEditor { get; set; }
        IRuleStructure ParseRule(string Rule);
        
    }
}
