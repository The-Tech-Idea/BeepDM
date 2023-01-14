using System.Collections.Generic;
using System.ComponentModel;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow
{
    public interface IWorkFlowEditor
    {
        IDMEEditor DMEEditor { get; set; }
        IWorkFlowStepEditor StepEditor { get; set; }
        List<IWorkFlow> WorkFlows { get; set; }
        List<IWorkFlowAction> Actions { get; set; }
        List<IWorkFlowRule> Rules { get; set; }
        IErrorsInfo RunWorkFlow(string WorkFlowName);
        IErrorsInfo StopWorkFlow();
        IErrorsInfo SaveWorkFlow(string WorkFlowName);
        IErrorsInfo LoadWorkFlow(string WorkFlowName);
    }
}