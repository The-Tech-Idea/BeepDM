using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow.Interfaces;

namespace TheTechIdea.Beep.Workflow
{
    public class RuleParser : IRuleParser
    {
        public RuleParser(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;
        }

        public IDMEEditor DMEEditor { get; set; }
        public List<IRuleStructure> RuleStructures { get; set; } = new List<IRuleStructure>();

        public IRuleStructure ParseRule(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
            {
                DMEEditor.AddLogMessage("Parse Rule", "Rule is empty or null", DateTime.Now, -1, rule, Errors.Failed);
                return null;
            }

            var parts = rule.Split('.');
            if (parts.Length < 3)
            {
                DMEEditor.AddLogMessage("Parse Rule", "Rule syntax is invalid", DateTime.Now, -1, rule, Errors.Failed);
                return null;
            }

            return new RuleStructure
            {
                Rulename = parts[0].TrimStart(':'),
                Fieldname = parts[1],
                Expression = parts[2]
            };
        }
    }
}
