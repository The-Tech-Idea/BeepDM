using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Rules
{
    /// <summary>
    /// Implementation of IRuleParser that tokenizes rule text and builds a corresponding RuleStructure.
    /// </summary>
    public class RuleParser : IRuleParser, IDisposable
    {
        public List<IRuleStructure> RuleStructures { get; set; } = new List<IRuleStructure>();

        /// <summary>
        /// Parses the provided rule expression (as a string) into a RuleStructure.
        /// </summary>
        public IRuleStructure ParseRule(string rule)
        {
            // Create an instance of the tokenizer to process the rule text.
            Tokenizer tokenizer = new Tokenizer(rule);
            List<Token> tokens = tokenizer.Tokenize();

            // Create a new RuleStructure with default metadata.
            var ruleStructure = new RuleStructure
            {
                Expression = rule,
                Tokens = tokens,
                Rulename = "DefaultRule",
              
                RuleType = "Advanced"
            };

            RuleStructures.Add(ruleStructure);
            return ruleStructure;
        }

        /// <summary>
        /// Parses the rule by reading its RuleText and extracting metadata from the rule’s attribute.
        /// </summary>
        public IRuleStructure ParseRule(IRule rule)
        {
            // Use the rule's RuleText for tokenization.
            Tokenizer tokenizer = new Tokenizer(rule.RuleText);
            List<Token> tokens = tokenizer.Tokenize();

            // Try to extract metadata from the rule's [Rule] attribute.
            string ruleName = rule.RuleText;
            string ruleType = "Advanced";
            string FieldName = "";

            var ruleTypeInfo = rule.GetType();
            var attr = (RuleAttribute)ruleTypeInfo.GetCustomAttributes(typeof(RuleAttribute), false).FirstOrDefault();
            if (attr != null)
            {
                ruleName = attr.RuleName;     // Use the attribute's rule name.
                ruleType = attr.RuleType;     // Use the attribute's rule type.
                // Optionally, if you include FieldName in your attribute, extract it here.
                //FieldName = attr.RuleField;  // For example, if you had this property.
            }
            else
            {
                // Optionally, log or handle the absence of the attribute.
            }

            var ruleStructure = new RuleStructure
            {
                Expression = rule.RuleText,
                Tokens = tokens,
                Rulename = ruleName,
            
                RuleType = ruleType
            };

            RuleStructures.Add(ruleStructure);
            return ruleStructure;
        }

        public void Clear()
        {
            RuleStructures.Clear();
        }

        public void Dispose()
        {
            RuleStructures.Clear();
        }
    }
}
