using TheTechIdea.Beep.Rules;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Addin;
using System;
using System.Text.RegularExpressions;


namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    [Addin(Caption = "Validate Numeric Range", Name = "ValidateNumericRange", addinType = AddinType.Class)]
    public class ValidateNumericRange : BaseWorkFlowRule
    {
        public ValidateNumericRange(IDMEEditor pDMEEditor) : base(pDMEEditor, "ValidateNumericRange") { }

        public override PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule)
        {
            InitializePassedArguments(args);

            if (string.IsNullOrEmpty(args.ParameterString1) || string.IsNullOrEmpty(rule.Expression))
            {
                DMEEditor.AddLogMessage("Validation Failed: Missing arguments or range expression.");
                args.Messege = "Validation Failed: Missing arguments or range expression.";
                return args;
            }

            try
            {
                var match = Regex.Match(rule.Expression, @":number\.(?<min>[\d.]+)\s*:to\s*:number\.(?<max>[\d.]+)");

                if (!match.Success)
                {
                    DMEEditor.AddLogMessage("Validation Failed: Invalid range expression format.");
                    args.Messege = "Validation Failed: Use ':number.minvalue :to :number.maxvalue'.";
                    return args;
                }

                if (double.TryParse(args.ParameterString1, out double input) &&
                    double.TryParse(match.Groups["min"].Value, out double min) &&
                    double.TryParse(match.Groups["max"].Value, out double max))
                {
                    args.Messege = input >= min && input <= max
                        ? $"Validation Passed: {input} is within range ({min} to {max})."
                        : $"Validation Failed: {input} is out of range ({min} to {max}).";
                }
                else
                {
                    args.Messege = "Invalid numeric values provided.";
                }
            }
            catch (Exception ex)
            {
                args.Messege = $"Error: {ex.Message}";
            }

            return args;
        }
    }
}
