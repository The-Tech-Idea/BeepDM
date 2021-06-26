using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Editor
{
    public class SyncErrorsandTracking
    {
        public SyncErrorsandTracking()
        {

        }
        public SyncErrorsandTracking(string pentityname, DateTime dateTime, string pscript)
        {
            id = Guid.NewGuid().ToString();
            sourceEntityName = pentityname;
            rundate = dateTime;
            script = pscript;
        }
        public string id { get; set; }
        public DateTime rundate { get; set; }
        public string parentscriptid { get; set; }
        public string sourceDataSourceName { get; set; }
        public int currenrecordindex { get; set; }
        public string sourceEntityName { get; set; }
        public string errormessage { get; set; }
        public string script { get; set; }
        public IErrorsInfo errorsInfo { get; set; }


    }
}
