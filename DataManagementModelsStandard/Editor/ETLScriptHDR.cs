using System;
using System.Collections.Generic;


namespace TheTechIdea.Beep.Editor
{
    public class ETLScriptHDR
    {
        public ETLScriptHDR()
        {
            id = 1;
            GuidID = Guid.NewGuid().ToString();
        }
        public List<ETLScriptDet> ScriptDTL { get; set; } = new List<ETLScriptDet>();
        public string workflowFileId { get; set; }
        public string scriptSource { get; set; }
        public string scriptDestination { get; set; }
        public string scriptName { get; set; }
        public string scriptDescription { get; set; }
        public string scriptType { get; set; }
        public string scriptStatus { get; set; }
        public DateTime LastRunDateTime { get; set; }
        public string OwnerGuidID { get; set; }
        public string OwnerName { get; set; }
        public string GuidID { get; set; }
        public  int id { get; set; }


    }
}
