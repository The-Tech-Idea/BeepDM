using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Rules;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Rules;

namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    public abstract class BaseWorkFlowRule : IWorkFlowRule
    {
        public IDMEEditor DMEEditor { get; set; }

        protected BaseWorkFlowRule(IDMEEditor pDMEEditor, string ruleName)
        {
            DMEEditor = pDMEEditor ?? throw new ArgumentNullException(nameof(pDMEEditor));
            RuleName = ruleName ?? throw new ArgumentNullException(nameof(ruleName));
            Rule = ruleName;
        }

        public string RuleName { get; set; }
        public string Rule { get; set; }

        public event EventHandler<WorkFlowEventArgs> WorkFlowRuleStarted;
        public event EventHandler<WorkFlowEventArgs> WorkFlowRuleEnded;
        public event EventHandler<WorkFlowEventArgs> WorkFlowRuleRunning;

        /// <summary>
        /// Executes the rule logic. Must be implemented by derived classes.
        /// </summary>
        /// <param name="args">The input arguments for the rule execution.</param>
        /// <param name="rule">The rule structure defining the rule logic.</param>
        /// <returns>A <see cref="PassedArgs"/> object containing the results of the execution.</returns>
        public abstract PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule);

        /// <summary>
        /// Retrieves the default value for the rule based on the provided arguments and rule structure.
        /// </summary>
        /// <param name="args">The input arguments containing data source details.</param>
        /// <param name="rule">The rule structure for the current execution.</param>
        /// <returns>The default value for the rule, if applicable.</returns>
        protected DefaultValue GetDefaultValue(PassedArgs args, IRuleStructure rule)
        {
            if (string.IsNullOrEmpty(args.DatasourceName))
                return null;

            var dataConnection = DMEEditor.ConfigEditor.DataConnections
                .FirstOrDefault(c => c.ConnectionName.Equals(args.DatasourceName, StringComparison.OrdinalIgnoreCase));

            return dataConnection?.DatasourceDefaults?.FirstOrDefault(p =>
                string.Equals(p.Rule, rule.Rulename, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Prepares the <see cref="PassedArgs"/> object for rule execution.
        /// Resets parameters and clears return data.
        /// </summary>
        /// <param name="args">The arguments to initialize.</param>
        protected void InitializePassedArguments(PassedArgs args)
        {
            args.ParameterString2 = null;
            args.ReturnData = null;
            args.ReturnType = null;
        }

        /// <summary>
        /// Triggers the event for the start of the rule execution.
        /// </summary>
        /// <param name="message">A message describing the start event.</param>
        protected void OnWorkFlowRuleStarted(string message)
        {
            WorkFlowRuleStarted?.Invoke(this, new WorkFlowEventArgs
            {
                ActionName = RuleName,
                Message = message,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Triggers the event for the end of the rule execution.
        /// </summary>
        /// <param name="message">A message describing the end event.</param>
        protected void OnWorkFlowRuleEnded(string message)
        {
            WorkFlowRuleEnded?.Invoke(this, new WorkFlowEventArgs
            {
                ActionName = RuleName,
                Message = message,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Triggers the event for the rule running state.
        /// </summary>
        /// <param name="message">A message describing the running event.</param>
        protected void OnWorkFlowRuleRunning(string message)
        {
            WorkFlowRuleRunning?.Invoke(this, new WorkFlowEventArgs
            {
                ActionName = RuleName,
                Message = message,
                Timestamp = DateTime.Now
            });
        }
    }
}
