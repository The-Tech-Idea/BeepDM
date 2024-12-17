using TheTechIdea.Beep.Workflow.Interfaces;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Addin;
using System.Threading;


namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    [Addin(Caption = "Pause Workflow", Name = "PauseWorkflow", addinType = AddinType.Class)]
    public class PauseWorkflow : BaseWorkFlowRule
    {
        public PauseWorkflow(IDMEEditor pDMEEditor) : base(pDMEEditor, "PauseWorkflow") { }

        public override PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule)
        {
            if (int.TryParse(rule.Expression, out int duration))
            {
                Thread.Sleep(duration);
            }
            return args;
        }
    }
}
