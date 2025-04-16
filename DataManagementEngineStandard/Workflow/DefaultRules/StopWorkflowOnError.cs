using TheTechIdea.Beep.Rules;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    [Addin(Caption = "Stop Workflow On Error", Name = "StopWorkflowOnError", addinType = AddinType.Class)]
    public class StopWorkflowOnError : BaseWorkFlowRule
    {
        public StopWorkflowOnError(IDMEEditor pDMEEditor) : base(pDMEEditor, "StopWorkflowOnError") { }

        public override PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule)
        {
            if (args.IsError )
            {
                DMEEditor.AddLogMessage("Workflow execution halted due to an error.");
            }
            return args;
        }
    }
}
