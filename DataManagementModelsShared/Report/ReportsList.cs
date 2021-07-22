using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Report
{
    public class ReportsList
    {
        public ReportsList()
        {
            id = Guid.NewGuid().ToString();
        }
        public string id { get;  }
        public string ReportName { get; set; }
        public string ReportDefinition { get; set; }
        public string ReportEngine { get; set; }

    }
}
