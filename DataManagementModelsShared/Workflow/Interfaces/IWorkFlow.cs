using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow
{
    public interface IWorkFlow
    {
        event EventHandler<IWorkFlowEventArgs> WorkFlowStarted;
        event EventHandler<IWorkFlowEventArgs> WorkFlowEnded;
        event EventHandler<IWorkFlowEventArgs> WorkFlowRunning;
        List<IWorkFlowStep> Datasteps { get; set; }
        List<string>        DataSources { get; set; }
        string              WorkSpaceDatabase { get; set; }
        string              WorkSpaceFolder { get; set; }
        string              DataWorkFlowName { get; set; }
        string              Description { get; set; }
        string              Id { get; set; }
        List<WorkFlow_Run_Result> workFlow_Run_Results { get; set; }


    }
  
}