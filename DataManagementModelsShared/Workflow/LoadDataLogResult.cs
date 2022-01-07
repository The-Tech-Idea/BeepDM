using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow
{
    public class LoadDataLogResult
    {
        public string RunID { get; set; }
        public string WorkFlowID { get; set; }
        public string StepID { get; set; }
        public DateTime Date { get; set; }
        public IErrorsInfo ErrorInfo { get; set; }
        public int Rowindex { get; set; }
        public string RowID { get; set; }
        public string InputLine { get; set; }
        

        public LoadDataLogResult()
        {

        }
    }
}
