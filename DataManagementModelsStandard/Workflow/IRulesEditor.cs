using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Workflow.Interfaces;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow
{
    public interface IRulesEditor
    {
        IDMEEditor DMEEditor { get; set; }
        List<IWorkFlowRule> Rules { get; set; }
        IRuleParser Parser { get; set; }

        object SolveRule(IPassedArgs args);

    }
}
