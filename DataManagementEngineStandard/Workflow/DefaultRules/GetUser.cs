using System;
using TheTechIdea.Beep.Rules;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    [Addin(Caption = "Default User", Name = "User", misc = "Defaults", addinType = AddinType.Class, returndataTypename = "string")]
    public class GetUser : BaseWorkFlowRule
    {
        public GetUser(IDMEEditor pDMEEditor) : base(pDMEEditor, "GetUser") { }

        public override PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule)
        {
            InitializePassedArguments(args);

            var defaultValue = GetDefaultValue(args, rule);
            if (defaultValue != null)
            {
                args.ParameterString2 = Environment.UserName;
                DMEEditor.Passedarguments.ReturnData = args.ParameterString2;
                DMEEditor.Passedarguments.ReturnType = typeof(string);
            }

            return (PassedArgs)DMEEditor.Passedarguments;
        }
    }
}
