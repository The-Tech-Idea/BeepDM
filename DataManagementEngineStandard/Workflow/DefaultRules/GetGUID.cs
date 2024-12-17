using System;
using TheTechIdea.Beep.Workflow.Interfaces;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    [Addin(Caption = "Default GUID", Name = "GUID", misc = "Defaults", addinType = AddinType.Class, returndataTypename = "string")]
    public class GetGUID : BaseWorkFlowRule
    {
        public GetGUID(IDMEEditor pDMEEditor) : base(pDMEEditor, "GetGUID") { }

        public override PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule)
        {
            InitializePassedArguments(args);

            var defaultValue = GetDefaultValue(args, rule);
            if (defaultValue != null)
            {
                args.ParameterString2 = Guid.NewGuid().ToString("N");
                DMEEditor.Passedarguments.ReturnData = args.ParameterString2;
                DMEEditor.Passedarguments.ReturnType = typeof(string);
            }

            return (PassedArgs)DMEEditor.Passedarguments;
        }
    }
}
