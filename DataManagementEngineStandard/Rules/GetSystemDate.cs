using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Rules.BuiltinRules
{
    [Rule(ruleKey: "GetSystemDate", ParserKey ="RulesParser",RuleName ="GetSystemDate")]
    public class GetSystemDate : IRule

    {/// <summary>
     /// The textual expression that defines the rule's logic.
     /// For example: ":Entity1.LastName == 'Smith'" or other valid expressions.
     /// </summary>
       public string RuleText { get; set; }= "GetSystemDate";
        public RuleStructure Structure { get; set; } = new RuleStructure();
        // Constructor to initialize the rule text.
        public GetSystemDate(string ruleText)
        {
            RuleText = ruleText;
        }

        public (Dictionary<string, object> outputs, object result) SolveRule( Dictionary<string, object> parameters)
        {
           
            return (parameters, DateTime.Now);
        }
    }
}
