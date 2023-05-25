using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Report
{
    public class ReportsList
    {
        public ReportsList()
        {
           
        }
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string ReportName { get; set; }
        public string ReportDefinition { get; set; }
        public string ReportEngine { get; set; }

    }
}
