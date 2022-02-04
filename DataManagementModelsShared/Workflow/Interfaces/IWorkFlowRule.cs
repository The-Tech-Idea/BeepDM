using System;
using System.Collections.Generic;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow
{
    public interface IWorkFlowRule
    {
        event EventHandler<IWorkFlowEventArgs> WorkFlowRuleStarted;
        event EventHandler<IWorkFlowEventArgs> WorkFlowRuleEnded;
        event EventHandler<IWorkFlowEventArgs> WorkFlowRuleRunning;
        IDMEEditor DMEEditor { get; set; }
        string RuleName { get; set; }
        string Rule { get; set; }
        PassedArgs ExecuteRule(PassedArgs args);
    }
}