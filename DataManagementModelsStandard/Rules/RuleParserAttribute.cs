using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Rules
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RuleParserAttribute : Attribute
    {
        public string ParserKey { get; }
        public string ParserType { get; set; } = "RuleParser";
        public string ParserName { get; set; } = "RuleParser";
        public string ParserDescription { get; set; } = "Rule Parser";
        public string ParserVersion { get; set; } = "1.0.0";
        public string ParserAuthor { get; set; } = "TheTechIdea";
        public string ParserCompany { get; set; } = "TheTechIdea";
        public string ParserCopyright { get; set; } = "Copyright © 2023 TheTechIdea";
        public RuleParserAttribute(string parserKey)
        {
            ParserKey = parserKey;
        }
    }
   
}
