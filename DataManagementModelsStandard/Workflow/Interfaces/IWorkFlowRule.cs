﻿using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow.Interfaces;
using TheTechIdea.Beep.Utilities;

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
        // args.SentObject has data object needed for rule to run
        PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule);
    }
}