using TheTechIdea.Beep.Workflow.Interfaces;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Addin;
using System;
using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    [Addin(Caption = "Validate Regex Match", Name = "ValidateRegexMatch", addinType = AddinType.Class)]
    public class ValidateRegexMatch : BaseWorkFlowRule
    {
        public ValidateRegexMatch(IDMEEditor pDMEEditor) : base(pDMEEditor, "ValidateRegexMatch") { }

        public override PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule)
        {
            InitializePassedArguments(args);

            // Check if the input and rule expression are valid
            if (string.IsNullOrEmpty(args.ParameterString1) || string.IsNullOrEmpty(rule.Expression))
            {
                DMEEditor.AddLogMessage("Validation Failed: Input or Regex pattern is missing.");
                args.Messege = "Validation Failed: Input value or Regex pattern is missing.";
                return args;
            }

            try
            {
                // Extract the Regex pattern from the expression
                string pattern = ExtractRegexPattern(rule.Expression);

                if (string.IsNullOrEmpty(pattern))
                {
                    DMEEditor.AddLogMessage("Validation Failed: Invalid Regex pattern.");
                    args.Messege = "Validation Failed: Invalid Regex pattern.";
                    return args;
                }

                // Perform Regex match
                if (Regex.IsMatch(args.ParameterString1, pattern))
                {
                    args.Messege = $"Validation Passed: Value '{args.ParameterString1}' matches the Regex pattern '{pattern}'.";
                }
                else
                {
                    args.Messege = $"Validation Failed: Value '{args.ParameterString1}' does not match the Regex pattern '{pattern}'.";
                    DMEEditor.AddLogMessage(args.Messege);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage($"Error: {ex.Message}");
                args.Messege = $"Error: {ex.Message}";
            }

            return args;
        }

        /// <summary>
        /// Extracts the Regex pattern from the rule expression.
        /// Expected format: ":regex.<pattern>"
        /// </summary>
        /// <param name="expression"></param>
        /// <returns>Regex pattern</returns>
        private string ExtractRegexPattern(string expression)
        {
            var match = Regex.Match(expression, @":regex\.(?<pattern>.+)");
            return match.Success ? match.Groups["pattern"].Value : null;
        }
    }
}
