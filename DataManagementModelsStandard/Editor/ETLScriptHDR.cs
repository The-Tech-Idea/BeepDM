using System;
using System.Collections.Generic;
using Newtonsoft.Json;


namespace TheTechIdea.Beep.Editor
{
    public class ETLScriptHDR
    {
        public ETLScriptHDR()
        {
            Id = 1;
            GuidId = Guid.NewGuid().ToString();
        }
      //  [JsonProperty("ScriptDTL")]
        public List<ETLScriptDet> ScriptDetails { get; set; } = new List<ETLScriptDet>();
      //  [JsonProperty("workflowFileId")]
        public string WorkflowFileId { get; set; }
      //  [JsonProperty("scriptSource")]
        public string ScriptSource { get; set; }
      //  [JsonProperty("scriptDestination")]
        public string ScriptDestination { get; set; }
      //  [JsonProperty("scriptName")]
        public string ScriptName { get; set; }
      //  [JsonProperty("scriptDescription")]
        public string ScriptDescription { get; set; }
       
        public string ScriptType { get; set; }
      //  [JsonProperty("scriptStatus")]
        public string ScriptStatus { get; set; }
        public DateTime LastRunDateTime { get; set; }
      //  [JsonProperty("OwnerGuidID")]
        public string OwnerGuidId { get; set; }
        public string OwnerName { get; set; }
      //  [JsonProperty("GuidID")]
        public string GuidId { get; set; }
      //  [JsonProperty("id")]
        public int Id { get; set; }


    }
}
