using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

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
        public DateTime rundate { get; set; }
        public int parentscriptid { get; set; }
        public string sourceDataSourceName { get; set; }
        public int currenrecordindex { get; set; }
        public string sourceEntityName { get; set; }
        public string errormessage { get; set; }
        public string script { get; set; }
        public IErrorsInfo errorsInfo { get; set; }


    }
}
