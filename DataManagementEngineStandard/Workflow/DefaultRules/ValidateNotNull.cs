using TheTechIdea.Beep.Rules;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    [Addin(Caption = "Validate Not Null", Name = "ValidateNotNull", addinType = AddinType.Class)]
    public class ValidateNotNull : BaseWorkFlowRule
    {
        public ValidateNotNull(IDMEEditor pDMEEditor) : base(pDMEEditor, "ValidateNotNull") { }

        public override PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule)
        {
            InitializePassedArguments(args);

            if (string.IsNullOrEmpty(args.ParameterString1))
            {
                DMEEditor.AddLogMessage($"Validation Failed: {rule.Rulename} - Field cannot be null or empty");
              
                args.Messege = "Field cannot be null or empty";
            }
            return args;
        }
    }
}
