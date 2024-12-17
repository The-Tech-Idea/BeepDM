using TheTechIdea.Beep.Workflow.Interfaces;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Addin;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    [Addin(Caption = "Validate Email", Name = "ValidateEmail", addinType = AddinType.Class)]
    public class ValidateEmail : BaseWorkFlowRule
    {
        public ValidateEmail(IDMEEditor pDMEEditor) : base(pDMEEditor, "ValidateEmail") { }

        public override PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule)
        {
            InitializePassedArguments(args);

            string email = args.ParameterString1;
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                DMEEditor.AddLogMessage($"Validation Failed: {rule.Rulename} - Invalid email format");
                args.Messege = "Invalid email format";
            }
            return args;
        }
    }
}
