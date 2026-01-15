using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor
{
    public interface IETLScriptDTL
    {
        int Id { get; set; }
        string GuidId { get; set; }
        string Ddl { get; set; }
        string SourceEntityName { get; set; }
        string SourceDataSourceEntityName { get; set; }
        string DestinationDataSourceName { get; set; }
        string SourceDataSourceName { get; set; }
        string DestinationEntityName { get; set; }
        string DestinationDataSourceEntityName { get; set; }
        string ErrorMessage { get; set; }
        bool Active { get; set; }
        bool CopyData { get; set; }
        List<AppFilter> FilterConditions { get; set; }
        DDLScriptType ScriptType{ get; set; }
        EntityDataMap_DTL Mapping { get; set; }
        List<SyncErrorsandTracking> Tracking { get; set; }
    }
    public enum DDLScriptType
    {
        CopyEntities,SyncEntity,CompareEntity,CreateEntity,AlterPrimaryKey,AlterFor,AlterUni,DropTable,EnableCons,DisableCons,CopyData
    }
}
