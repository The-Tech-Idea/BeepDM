using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Editor
{
   
    public class SyncEntity : ISyncEntity
    {
        public SyncEntity()
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
        public IErrorsInfo errorsInfo { get; set; }
        public DDLScriptType scriptType { get; set; }
        public List<SyncErrorsandTracking> Tracking { get; set; } = new List<SyncErrorsandTracking>();
        public List<SyncEntity> CopyDataScripts { get; set; } = new List<SyncEntity>();
    }
   
   
}
