using System;
using TheTechIdea.Beep.Workflow.Interfaces;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    [Addin(Caption = "Default Now", Name = "Now", misc = "Defaults", addinType = AddinType.Class, returndataTypename = "DateTime")]
    public class GetNow : BaseWorkFlowRule
    {
        public GetNow(IDMEEditor pDMEEditor) : base(pDMEEditor, "GetNow") { }

        public override PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule)
        {
            InitializePassedArguments(args);

            var defaultValue = GetDefaultValue(args, rule);
            if (defaultValue != null)
            {
                args.ParameterDate1 = string.IsNullOrEmpty(rule.Expression)
                    ? DateTime.Now
                    : DateTime.Parse(DateTime.Now.ToString(rule.Expression));

                DMEEditor.Passedarguments.ReturnData = args.ParameterDate1;
                DMEEditor.Passedarguments.ReturnType = typeof(DateTime);
            }

            return (PassedArgs)DMEEditor.Passedarguments;
        }
    }
}
