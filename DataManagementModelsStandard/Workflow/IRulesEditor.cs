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

        // args.SentObject has data object needed for rule to run
        Tuple<IPassedArgs,object> SolveRule(IWorkFlowRule rule,IPassedArgs args);
        Tuple<IPassedArgs, object> SolveRule(string rulename, IPassedArgs args);

    }
}
