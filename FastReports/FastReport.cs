using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine.Report
{
    public class FastReport : IReportDMWriter
    {
        public IReportDefinition Definition { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public bool Html { get ; set ; }
        public bool Text { get ; set ; }
        public bool Csv { get ; set ; }
        public bool PDF { get ; set ; }
        public bool Excel { get ; set ; }

   
        public string OutputFile { get; set; }

        public IErrorsInfo RunReport(ReportType reportType, string outputFile)
        {
            throw new NotImplementedException();
        }
    }
}
