using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Workflow.Interfaces;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    [Addin(Caption = "Default Now", Name = "Now", misc = "Defaults", addinType = AddinType.Class, returndataTypename = "string")]
    public class GetNow : IWorkFlowRule
    {
        public GetNow(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;
            RuleName = "GetNow";
            Rule = "Now";
        }
        public IDMEEditor DMEEditor { get; set; }
        public string RuleName { get; set; } = "GetNow";
        public string Rule { get; set; } = "Now";
        private List<string> _Tokens = new List<string>();

        public event EventHandler<IWorkFlowEventArgs> WorkFlowRuleStarted;
        public event EventHandler<IWorkFlowEventArgs> WorkFlowRuleEnded;
        public event EventHandler<IWorkFlowEventArgs> WorkFlowRuleRunning;

        public PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule)
        {
            if (args != null)
            {
                DMEEditor.Passedarguments.ParameterString2 = null;
                DMEEditor.Passedarguments.ReturnData = null;
                DMEEditor.Passedarguments.ReturnType = null;
                if (!string.IsNullOrEmpty(args.DatasourceName))
                {
                    List<DefaultValue> defaults = DMEEditor.ConfigEditor.DataConnections[DMEEditor.ConfigEditor.DataConnections.FindIndex(i => i.ConnectionName == args.DatasourceName)].DatasourceDefaults;
                    if (defaults != null)
                    {
                        if (rule != null)
                        {
                            DefaultValue defaultValue = defaults.Where(p => string.Equals(p.Rule, rule.Rulename, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                            if (defaultValue != null)
                            {
                                if (rule.Expression != null)
                                {
                                    args.ParameterString2 = DateTime.Now.ToString(args.ParameterString3);
                                }
                                else
                                    args.ParameterDate1 = DateTime.Parse(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                                DMEEditor.Passedarguments.ReturnData = args.ParameterDate1;
                                DMEEditor.Passedarguments.ReturnType = args.ParameterDate1.GetType();
                            }
                        }
                    }
                }
            }
            return (PassedArgs)DMEEditor.Passedarguments;
        }

    }
}
