using System;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Rules;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    [Addin(Caption = "Validate Value Range", Name = "ValidateValueRange", addinType = AddinType.Class)]
    public class ValidateDateValueRange : BaseWorkFlowRule
    {
        public ValidateDateValueRange(IDMEEditor pDMEEditor) : base(pDMEEditor, "ValidateValueRange") { }

        public override PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule)
        {
            InitializePassedArguments(args);

            if (string.IsNullOrEmpty(args.ParameterString1) || string.IsNullOrEmpty(rule.Expression))
            {
                DMEEditor.AddLogMessage("Validation Failed: Missing required arguments or range expression.");
             
                args.Messege = "Validation Failed: Missing arguments or range expression.";
                return args;
            }

            try
            {
                // Parse the expression of the format ":date.value1 :to :date.value2"
                var match = Regex.Match(rule.Expression, @":date\.(?<value1>[\d\-/:.]+)\s*:to\s*:date\.(?<value2>[\d\-/:.]+)");

                if (!match.Success)
                {
                    DMEEditor.AddLogMessage("Validation Failed: Invalid range expression format.");
                  
                    args.Messege = "Validation Failed: Invalid range expression format. Use ':date.value1 :to :date.value2'.";
                    return args;
                }

                // Extract the values as strings
                string value1String = match.Groups["value1"].Value;
                string value2String = match.Groups["value2"].Value;

                // Convert the input and range values into DateTime
                if (DateTime.TryParse(args.ParameterString1, out DateTime inputDate) &&
                    DateTime.TryParse(value1String, out DateTime minValue) &&
                    DateTime.TryParse(value2String, out DateTime maxValue))
                {
                    if (inputDate < minValue || inputDate > maxValue)
                    {
                        DMEEditor.AddLogMessage($"Validation Failed: Date {inputDate} is out of range ({minValue} to {maxValue}).");
                     
                        args.Messege = $"Date {inputDate} is out of range ({minValue} to {maxValue}).";
                    }
                    else
                    {
                      
                        args.Messege = $"Validation Passed: Date {inputDate} is within range ({minValue} to {maxValue}).";
                    }
                }
                else
                {
                    DMEEditor.AddLogMessage("Validation Failed: Invalid date values in expression or input.");
                
                    args.Messege = "Validation Failed: Invalid date values in expression or input.";
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage($"Error: {ex.Message}");
              
                args.Messege = $"Error: {ex.Message}";
            }

            return args;
        }
    }
}
