using System;
using System.Collections.Generic;

using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Addin;

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
