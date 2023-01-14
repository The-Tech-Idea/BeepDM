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
    [Addin(Caption = "Default User", Name = "User", misc = "Defaults", addinType = AddinType.Class, returndataTypename = "string")]
    public class GetUser : IWorkFlowRule
    {
        public GetUser(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;
            RuleName = "GetUser";
            Rule = "User";
        }
        public IDMEEditor DMEEditor { get; set; }
        public string RuleName { get; set; } = "GetUser";
        public string Rule { get; set; } = "User";
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
                                args.ParameterString2 = Environment.UserName;
                                DMEEditor.Passedarguments.ReturnData = args.ParameterString2;
                                DMEEditor.Passedarguments.ReturnType = args.ParameterString2.GetType();
                            }
                        }
                    }
                }
            }
            return (PassedArgs)DMEEditor.Passedarguments;
        }

    }
}
