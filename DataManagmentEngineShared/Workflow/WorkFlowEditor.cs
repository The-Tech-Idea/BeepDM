using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Workflow
{
    public class WorkFlowEditor : IWorkFlowEditor
    {
        public WorkFlowEditor()
        {

        }

        public IDMEEditor DMEEditor { get ; set ; }
        public List<IWorkFlow> WorkFlows { get ; set ; }
        public List<IWorkFlowAction> Actions { get ; set ; }
        public List<IWorkFlowRule> Rules { get ; set ; }

        public IErrorsInfo RunWorkFlow(string WorkFlowName)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo StopWorkFlow()
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo SyncDatabase(IDataSource src, IRDBSource dest)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo SyncEntity(IDataSource src, string SourceEntityName, IDataSource dest, string DestEntityName)
        {
            throw new NotImplementedException();
        }
    }
}
