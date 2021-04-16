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
        string valueType { get; set; }
         string FilterValue1 { get; set; }
         
    }

    public class ReportFilter : IReportFilter
    {
        public ReportFilter()
        {

        }
        public string FieldName { get; set; }
        public string Operator { get; set; }
        public string FilterValue { get; set; }
        public string valueType { get; set; }
        public string FilterValue1 { get; set; }
        


    }
    public class QueryBuild
    {
        public QueryBuild()
        {


        }
        public List<string> Fields { get; set; } = new List<string>();
        public List<string> Entities { get; set; } = new List<string>();
        public string FieldsString { get; set; }
        public string EntitiesString { get; set; }
        public string WhereCondition { get; set; }
        public string OrderbyCondition { get; set; }
        public string HavingCondition { get; set; }
        public string GroupbyCondition { get; set; }
    }
    public class FilterType
    {
        public FilterType(string pfiltertype)
        {
            FilterDisplay = pfiltertype;
            FilterValue = pfiltertype;
        }
        public string FilterDisplay { get; set; }
        public  string FilterValue { get; set; }
    }
}