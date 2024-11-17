using System;
using System.Collections.Generic;
using System.Text;
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
            id += 1;
            sourceEntityName = pentityname;
            rundate = dateTime;
            script = pscript;
        }
        public static int id { get; set; }
      
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public DateTime rundate { get; set; }
        public int parentscriptid { get; set; }
        public string sourceDataSourceName { get; set; }
        public int currenrecordindex { get; set; }
        public string sourceEntityName { get; set; }
        public string errormessage { get; set; }
        public string script { get; set; }
       


    }
}
