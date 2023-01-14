using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow
{
    public class WorkFlowEditor : IWorkFlowEditor
    {
        public WorkFlowEditor(IDMEEditor pDMEEditor)
        {
            DMEEditor= pDMEEditor;
            StepEditor = new WorkFlowStepEditor(DMEEditor, this);
        }

        public IDMEEditor DMEEditor { get; set; }
        public List<IWorkFlow> WorkFlows { get; set; }
        public List<IWorkFlowAction> Actions { get; set; }
        public List<IWorkFlowRule> Rules { get; set; }
        public IWorkFlowStepEditor StepEditor { get; set; }

        public IErrorsInfo LoadWorkFlow(string WorkFlowName)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo RunWorkFlow(string WorkFlowName)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo SaveWorkFlow(string WorkFlowName)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo StopWorkFlow()
        {
            throw new NotImplementedException();
        }

      
    }
}
