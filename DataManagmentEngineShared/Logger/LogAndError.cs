using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TheTechIdea.DataManagment_Engine.Logger
{
    public class LogAndError : ILogAndError
    {
        private static int seq;
        public   int Id { get; set; }
        public string LogType { get; set; }
        public string LogMessage { get; set; }
        public DateTime LogData { get; set; }
        public int RecordID { get; set; }
        public string MiscData { get; set; }
        public LogAndError()
        {
        
         


        }
        public LogAndError(string pLogType, string pLogMessage, DateTime pLogData, int pRecordID, string pMiscData)
        {
            seq += 1;
            Id = seq;
            LogType = pLogType;
            LogMessage = pLogMessage;
            LogData = pLogData;
            RecordID = pRecordID;
            MiscData = pMiscData;


    }
    }
}
