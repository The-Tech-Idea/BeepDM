using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow.Interfaces;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    public abstract class BaseWorkFlowRule : IWorkFlowRule
    {
        public IDMEEditor DMEEditor { get; set; }
        protected BaseWorkFlowRule(IDMEEditor pDMEEditor, string ruleName)
        {
            DMEEditor = pDMEEditor;
            RuleName = ruleName;
            Rule = ruleName;
        }

        public string RuleName { get; set; }
        public string Rule { get; set; }
       
        public event EventHandler<WorkFlowEventArgs> WorkFlowRuleStarted;
        public event EventHandler<WorkFlowEventArgs> WorkFlowRuleEnded;
        public event EventHandler<WorkFlowEventArgs> WorkFlowRuleRunning;

        public abstract PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule);

        protected DefaultValue GetDefaultValue(PassedArgs args, IRuleStructure rule)
        {
            if (string.IsNullOrEmpty(args.DatasourceName))
                return null;

            var dataConnection = DMEEditor.ConfigEditor.DataConnections
                .FirstOrDefault(c => c.ConnectionName == args.DatasourceName);

            return dataConnection?.DatasourceDefaults?.FirstOrDefault(p =>
                string.Equals(p.Rule, rule.Rulename, StringComparison.InvariantCultureIgnoreCase));
        }

        protected void InitializePassedArguments(PassedArgs args)
        {
            DMEEditor.Passedarguments.ParameterString2 = null;
            DMEEditor.Passedarguments.ReturnData = null;
            DMEEditor.Passedarguments.ReturnType = null;
        }
    }
}
