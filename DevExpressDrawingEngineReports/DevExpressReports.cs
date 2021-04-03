using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Report
{
    public class DevExpressReports : IReportDMWriter
    {
        public IReportDefinition Definition { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
    
        public bool Html { get; set; } = true;
        public bool Text { get; set; } = true;
        public bool Csv { get; set; } = true;
        public bool PDF { get; set; } = true;
        public bool Excel { get; set; } = true;
        public IErrorsInfo RunReport(ReportType reportType, string outputFile)
        {
           
            throw new NotImplementedException();
        }

       
    }
}
