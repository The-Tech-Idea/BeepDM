using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Rules
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RuleAttribute : Attribute
    {
       public string RuleKey { get; }
        public string RuleType { get; set; } = "Rule";
        public string RuleName { get; set; } = "Rule";
        public string RuleDescription { get; set; } = "Rule";
        public string RuleVersion { get; set; } = "1.0.0";
        public string RuleAuthor { get; set; } = "TheTechIdea";
        public string RuleCompany { get; set; } = "TheTechIdea";
        public string RuleCopyright { get; set; } = "Copyright © 2023 TheTechIdea";
        public string RuleLicense { get; set; } = "MIT";
        public string RuleLicenseUrl { get; set; } = "https://opensource.org/licenses/MIT";
        public string ParserKey { get; set; } = "RuleParser";
        public string ParserVersion { get; set; }


        public RuleAttribute(string ruleKey)
        {
            RuleKey = ruleKey;
        }
    }
}
