using System.Collections.Generic;
using System.ComponentModel;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow
{
    public interface IWorkFlowEditor
    {
        IDMEEditor DMEEditor { get; set; }
       
        List<DataWorkFlow> WorkFlows { get; set; }

        IErrorsInfo CopyEntity(IDataSource src, string SourceEntityName, IDataSource dest, string DestEntityName);
    
        IMapping_rep CreateMapping(IMapping_rep x);
        IMapping_rep CreateMapping(string src1, string entity1, string src2, string entity2);
        IErrorsInfo RunWorkFlow(string WorkFlowName);
        IErrorsInfo StopWorkFlow();
        IErrorsInfo SyncDatabase(IDataSource src, IRDBSource dest);
        IErrorsInfo SyncEntity(IDataSource src, string SourceEntityName, IDataSource dest, string DestEntityName);
    }
}