using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Report
{
    public interface IReportDefinition
    {
        string ID { get; set; }
        List<ReportBlock> Blocks { get; set; }
        string DataSourceName { get; set; }
        TextBlock Description { get; set; }
     
     
        string Name { get; set; }
        string ReportEndText { get; set; }
        TextBlock SubTitle { get; set; }
        ReportOrientation Orientation { get; set; }
        TextBlock Title { get; set; }
        TextBlock Header { get; set; }
        TextBlock Footer { get; set; }
        string CSS { get; set; }
        int ViewID { get; set; }
       
    }

    
   

}
