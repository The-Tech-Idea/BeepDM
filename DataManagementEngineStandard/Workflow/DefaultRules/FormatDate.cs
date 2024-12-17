using TheTechIdea.Beep.Workflow.Interfaces;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Addin;
using System;

namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    [Addin(Caption = "Format Date", Name = "FormatDate", addinType = AddinType.Class)]
    public class FormatDate : BaseWorkFlowRule
    {
        public FormatDate(IDMEEditor pDMEEditor) : base(pDMEEditor, "FormatDate") { }

        public override PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule)
        {
            InitializePassedArguments(args);

            if (DateTime.TryParse(args.ParameterString1, out DateTime date))
            {
                args.ParameterString1 = date.ToString(rule.Expression); // Use the format provided in the rule's expression.
            }
            else
            {
                DMEEditor.AddLogMessage($"Validation Failed: {rule.Rulename} - Invalid date format");
                args.Messege = "Invalid date format";
            }
            return args;
        }
    }
}
