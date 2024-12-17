using TheTechIdea.Beep.Workflow.Interfaces;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    [Addin(Caption = "Trim Whitespace", Name = "TrimWhitespace", addinType = AddinType.Class)]
    public class TrimWhitespace : BaseWorkFlowRule
    {
        public TrimWhitespace(IDMEEditor pDMEEditor) : base(pDMEEditor, "TrimWhitespace") { }

        public override PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule)
        {
            InitializePassedArguments(args);
            args.ParameterString1 = args.ParameterString1?.Trim();
            return args;
        }
    }
}
