using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor
{
   
    public class ETLScriptDet : IETLScriptDTL
    {
        public ETLScriptDet()
        {
            Id = 1;
        }
      //  [JsonProperty("ID")]
        public int Id { get; set; }
      //  [JsonProperty("GuidID")]
        public string GuidId { get; set; } = Guid.NewGuid().ToString();
      //  [JsonProperty("ddl")]
        public string Ddl { get ; set ; }

      //  [JsonProperty("sourcedatasourcename")]
        public string SourceDataSourceName { get; set; }
      //  [JsonProperty("sourceentityname")]
        public string SourceEntityName { get; set; }
      //  [JsonProperty("sourceDatasourceEntityName")]
        public string SourceDataSourceEntityName { get; set; }
        public EntityStructure SourceEntity { get; set; }
      //  [JsonProperty("destinationentityname")]
        public string DestinationEntityName { get; set; }
      //  [JsonProperty("destinationDatasourceEntityName")]
        public string DestinationDataSourceEntityName { get; set; }
      //  [JsonProperty("destinationdatasourcename")]
        public string DestinationDataSourceName { get ; set ; }
      //  [JsonProperty("errormessage")]
        public string ErrorMessage { get ; set ; }
        public bool IsCreated { get; set; }=false;
        public bool IsModified { get; set; }=false ;
        public bool IsDataCopied { get; set; }=false ;
        public bool Failed { get; set; } = false;
        public bool Active { get; set; } = false;
        public bool CopyData { get; set; }= false;
        public List<AppFilter> FilterConditions { get; set; }=new List<AppFilter>();
       
        public DDLScriptType ScriptType  { get; set; }=new DDLScriptType();
        public EntityDataMap_DTL Mapping { get; set; }
        public List<SyncErrorsandTracking> Tracking { get; set; } = new List<SyncErrorsandTracking>();
        public List<ETLScriptDet> CopyDataScripts { get; set; } = new List<ETLScriptDet>();
    }
   
   
}
