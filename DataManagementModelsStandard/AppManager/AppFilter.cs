using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Report
{
    public interface IAppFilter
    {
         int ID { get; set; }
         string GuidID { get; set; }
        string FieldName { get; set; }
        string FilterValue { get; set; }
        string Operator { get; set; }
        string valueType { get; set; }
         string FilterValue1 { get; set; }
         
    }

    public class AppFilter : IAppFilter
    {
        public AppFilter()
        {

        }
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
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
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
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
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string FilterDisplay { get; set; }
        public  string FilterValue { get; set; }
    }
}