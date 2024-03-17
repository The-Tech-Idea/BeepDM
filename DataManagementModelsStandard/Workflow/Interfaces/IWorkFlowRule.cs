using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Workflow.Interfaces;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow
{
    public interface IWorkFlowRule
    {
        event EventHandler<WorkFlowEventArgs> WorkFlowRuleStarted;
        event EventHandler<WorkFlowEventArgs> WorkFlowRuleEnded;
        event EventHandler<WorkFlowEventArgs> WorkFlowRuleRunning;
        IDMEEditor DMEEditor { get; set; }
        string RuleName { get; set; }
        string Rule { get; set; }
        PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule);
    }
}