using TheTechIdea.Beep.Rules;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Addin;
using System;


namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    [Addin(Caption = "To Upper Case", Name = "ToUpperCase", addinType = AddinType.Class)]
    public class ToLowerCase : BaseWorkFlowRule
    {
        public ToLowerCase(IDMEEditor pDMEEditor) : base(pDMEEditor, "ToUpperCase") { }

        public override PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule)
        {
            InitializePassedArguments(args);
            args.ParameterString1 = args.ParameterString1?.ToUpper();
            return args;
        }
    }
}
