using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Editor
{
    public interface IETLScriptDTL
    {
         int ID { get; set; }
         string GuidID { get; set; } 
        string ddl { get; set; }
        string sourceentityname { get; set; }
        string sourceDatasourceEntityName { get; set; }
        string destinationdatasourcename { get; set; }
        string sourcedatasourcename { get; set; }
         string destinationentityname { get; set; }
         string destinationDatasourceEntityName { get; set; }
        string errormessage { get; set; }
        bool Active { get; set; }
        bool CopyData { get; set; } 
        List<AppFilter> FilterConditions { get; set; }  
        IErrorsInfo errorsInfo { get; set; }
        DDLScriptType scriptType { get; set; }
        EntityDataMap_DTL Mapping { get; set; }
        List<SyncErrorsandTracking> Tracking { get; set; }
      
    }
    public enum DDLScriptType
    {
        CopyEntities,SyncEntity,CompareEntity,CreateEntity,AlterPrimaryKey,AlterFor,AlterUni,DropTable,EnableCons,DisableCons,CopyData
    }
}
