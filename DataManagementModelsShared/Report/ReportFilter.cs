using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.DataManagment_Engine.Report
{
    public interface IReportFilter
    {
        string FieldName { get; set; }
        string FilterValue { get; set; }
        string Operator { get; set; }
    }

    public class ReportFilter : IReportFilter
    {
        public ReportFilter()
        {

        }
        public string FieldName { get; set; }
        public string Operator { get; set; }
        public string FilterValue { get; set; }


    }
}