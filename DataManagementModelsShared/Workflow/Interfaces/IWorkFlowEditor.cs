using System.Collections.Generic;
using System.ComponentModel;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.DataManagment_Engine.Workflow;
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
        IErrorsInfo SyncDatabase(IDataSource src, IRDBSource dest);
        IErrorsInfo SyncEntity(IDataSource src, string SourceEntityName, IDataSource dest, string DestEntityName);
    }
}