using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.DataManagment_Engine.Logger
{
    public interface ILogAndError
    {
         int        Id { get; set; }
         string     LogType { get; set; }
         string     LogMessage { get; set; }
         DateTime LogData { get; set; }
         int         RecordID { get; set; }
         string     MiscData { get; set; }

    }
}
