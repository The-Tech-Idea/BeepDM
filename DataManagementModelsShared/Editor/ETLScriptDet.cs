using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Editor
{
   
    public class ETLScriptDet : IETLScriptDTL
    {
        public ETLScriptDet()
        {
            id = 1;
        }
        public  int id { get; set; }
        public string ddl { get ; set ; }

        public string sourcedatasourcename { get; set; }
        public string sourceentityname { get ; set ; }
        public string sourceDatasourceEntityName { get; set; }

        public string destinationentityname { get; set; }
        public string destinationDatasourceEntityName { get; set; }
        public string destinationdatasourcename { get ; set ; }
        public string errormessage { get ; set ; }
        public bool Active { get; set; } = false;
        public IErrorsInfo errorsInfo { get; set; }=new ErrorsInfo();
        public DDLScriptType scriptType { get; set; }=new DDLScriptType();
        public EntityDataMap_DTL Mapping { get; set; }
        public List<SyncErrorsandTracking> Tracking { get; set; } = new List<SyncErrorsandTracking>();
        public List<ETLScriptDet> CopyDataScripts { get; set; } = new List<ETLScriptDet>();
    }
   
   
}
