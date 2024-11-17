using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;

using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow.Interfaces;


namespace TheTechIdea.Beep.Workflow
{
    public class RuleParser:IRuleParser
    {
       
        public List<IRuleStructure> RuleStructures { get; set; }=new List<IRuleStructure>(){ };
        public  RuleParser(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;
        }
        public IDMEEditor DMEEditor { get; set; }
        public IRuleStructure ParseRule(string Rule)
        {
            RuleStructure r = new RuleStructure();
            string[] vs = Rule.Split('.');
            if (Rule.Length > 0)
            {
                if (vs.Count() < 2)
                {
                    DMEEditor.AddLogMessage("Run Rule", "Rule Syntax is invalid", DateTime.Now, -1, Rule, Errors.Failed);
                    return null;
                }
            }
            else
            {
                DMEEditor.AddLogMessage("Run Rule", "Rule Syntax is invalid", DateTime.Now, -1, Rule, Errors.Failed);
                return null;
            }
            if (vs[0].StartsWith(":"))
            {
                vs[0] = vs[0].Remove(0, 1);

            }
            r.Rulename = vs[0];
            r.Fieldname = vs[1];
            r.Expression = vs[2];
            return r;
        }
    }
}
