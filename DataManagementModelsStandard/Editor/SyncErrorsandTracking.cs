using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Editor
{
    public class SyncErrorsandTracking
    {
        public SyncErrorsandTracking()
        {

        }
        public SyncErrorsandTracking(string pentityname, DateTime dateTime, string pscript)
        {
            Id += 1;
            SourceEntityName = pentityname;
            RunDate = dateTime;
            Script = pscript;
        }
        public static int Id { get; set; }

      //  [JsonProperty("GuidID")]
        public string GuidId { get; set; } = Guid.NewGuid().ToString();
      //  [JsonProperty("rundate")]
        public DateTime RunDate { get; set; }
      //  [JsonProperty("parentscriptid")]
        public int ParentScriptId { get; set; }
      //  [JsonProperty("sourceDataSourceName")]
        public string SourceDataSourceName { get; set; }
      //  [JsonProperty("currenrecordindex")]
        public int CurrentRecordIndex { get; set; }
      //  [JsonProperty("sourceEntityName")]
        public string SourceEntityName { get; set; }
      //  [JsonProperty("errormessage")]
        public string ErrorMessage { get; set; }
      //  [JsonProperty("script")]
        public string Script { get; set; }
       


    }
}
