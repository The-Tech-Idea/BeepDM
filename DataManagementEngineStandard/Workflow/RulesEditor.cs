using System;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Rules;

namespace TheTechIdea.Beep.Workflow
{
    public class RulesEditor : IRulesEditor
    {
        public RulesEditor(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor ?? throw new ArgumentNullException(nameof(pDMEEditor));
            Parser = new RuleParser();
            Rules = new List<IWorkFlowRule>();
        }

        public IDMEEditor DMEEditor { get; set; }
        public List<IWorkFlowRule> Rules { get; set; }
        public IRuleParser Parser { get; set; }
        private IPassedArgs passedArgs;

        /// <summary>
        /// Solves a rule using the provided workflow rule and arguments.
        /// </summary>
        /// <param name="rule">The workflow rule to execute.</param>
        /// <param name="args">The arguments required for the rule execution.</param>
        /// <returns>A tuple containing the updated arguments and the result of the rule execution.</returns>
        public Tuple<IPassedArgs, object> SolveRule(IWorkFlowRule rule, IPassedArgs args)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            try
            {
                DMEEditor.AddLogMessage("RulesEditor", $"Executing rule: {rule.RuleName}", DateTime.Now, 0, null, Errors.Ok);
                var result = rule.ExecuteRule((PassedArgs)args, new RuleStructure { Rulename = rule.RuleName });
                return new Tuple<IPassedArgs, object>(args, result.SentData);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("RulesEditor", $"Error solving rule '{rule.RuleName}': {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Solves a rule based on its name using the provided arguments.
        /// </summary>
        /// <param name="rulename">The name of the rule to execute.</param>
        /// <param name="args">The arguments required for the rule execution.</param>
        /// <returns>A tuple containing the updated arguments and the result of the rule execution.</returns>
        public Tuple<IPassedArgs, object> SolveRule(string rulename, IPassedArgs args)
        {
            if (string.IsNullOrEmpty(rulename))
                throw new ArgumentException("Rule name cannot be null or empty.", nameof(rulename));
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            var ruleStructure = (RuleStructure)Parser.ParseRule(rulename);
            if (ruleStructure == null)
                throw new KeyNotFoundException($"Rule '{rulename}' could not be parsed.");

            var assemblyDef = DMEEditor.ConfigEditor.Rules
                .FirstOrDefault(x => x.classProperties.Name.Equals(ruleStructure.Rulename, StringComparison.OrdinalIgnoreCase));

            if (assemblyDef == null)
                throw new KeyNotFoundException($"Rule definition for '{rulename}' not found in configuration.");

            var ruleInstance = Activator.CreateInstance(assemblyDef.type, new object[] { DMEEditor }) as IWorkFlowRule;
            if (ruleInstance == null)
                throw new InvalidOperationException($"Failed to create an instance of the rule '{rulename}'.");

            return SolveRule(ruleInstance, args);
        }

        /// <summary>
        /// Executes a rule based on the arguments provided in <see cref="IPassedArgs"/>.
        /// </summary>
        /// <param name="args">The arguments that contain the rule name and required data.</param>
        /// <returns>The result of the rule execution, if successful.</returns>
        public object SolveRule(IPassedArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            passedArgs = args;

            if (!string.IsNullOrEmpty(args.ParameterString1))
            {
                var result = SolveRule(args.ParameterString1, args);
                return result?.Item2;
            }

            return null;
        }
    }
}
