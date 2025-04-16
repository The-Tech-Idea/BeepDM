using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.Rules.BuiltinRules
{
    [Rule(ruleKey: "GetRecordCount", ParserKey = "RulesParser", RuleName = "GetRecordCount")]
    public class GetRecordCount : IRule

    {/// <summary>
     /// The textual expression that defines the rule's logic.
     /// For example:Rule Test Should be 
     /// </summary>
        public string RuleText { get; set; } = "GetRecordCount";
        public RuleStructure Structure { get; set; } = new RuleStructure();
        // Constructor to initialize the rule text.
        public GetRecordCount(string ruleText) 
        {
            RuleText = ruleText;
        }

        public (Dictionary<string, object> outputs, object result) SolveRule(Dictionary<string, object> parameters)
        {
            int recordCount = -1;
            string DatasourceName = string.Empty;
            string EntityName = string.Empty;
            List<AppFilter> filters = new List<AppFilter>();
            string query = string.Empty;
            bool itsQuery = false;
            Dictionary<string, object> output = new Dictionary<string, object>();
            if (parameters != null && parameters.ContainsKey("DataSourceName"))
            {
                DatasourceName = parameters["DataSourceName"].ToString();
            }
            if (parameters != null && parameters.ContainsKey("EntityName"))
            {
                EntityName = parameters["EntityName"].ToString();
            }
            if (parameters != null && parameters.ContainsKey("Filters"))
            {
                filters = (List<AppFilter>)parameters["Filters"];
            }
            if(string.IsNullOrEmpty(DatasourceName) || string.IsNullOrEmpty(EntityName))
            {
                output.Add("Error", "DatasourceName and EntityName cannot be null or empty.");
                
            }
            if (parameters != null && parameters.ContainsKey("Query"))
            {
                query = parameters["Query"].ToString();
                itsQuery = true;
            }
            IDMEEditor dm=null;
            if(parameters != null && parameters.ContainsKey("IDMEEditor"))
            {
                dm = (IDMEEditor)parameters["IDMEEditor"];
            }
            else
            {
                output.Add("Error", "IDMEEditor cannot be null or empty."); 
            }
        
            IDataSource ds = null;
            ds=dm.GetDataSource(DatasourceName);
            if(ds == null)
            {
               output.Add("Error", "Datasource not found.");
            }
            if (itsQuery)
            {
                recordCount = (int)ds.GetScalar(query);
            }
            else
            {
                try
                {
                    var x = ds.GetEntityAsync(EntityName, filters);
                    x.Wait();
                    var y = (IList<object>)x.Result;
                    if (y != null)
                    {
                        recordCount = y.Count();
                    }
                    output.Add("RecordCount", recordCount.ToString());
                }
                catch (Exception ex)
                {
                    output.Add("Error", ex.Message);
                }
                
            }
            return (output, recordCount);
        }
    }
}
