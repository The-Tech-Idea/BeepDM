using TheTechIdea.Beep.Workflow.Interfaces;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Addin;
using System;
using System.Text.RegularExpressions;


namespace TheTechIdea.Beep.Workflow.DefaultRules
{
    [Addin(Caption = "Strip Special Characters", Name = "StripSpecialCharacters", addinType = AddinType.Class)]
    public class StripSpecialCharacters : BaseWorkFlowRule
    {
        public StripSpecialCharacters(IDMEEditor pDMEEditor) : base(pDMEEditor, "StripSpecialCharacters") { }

        public override PassedArgs ExecuteRule(PassedArgs args, IRuleStructure rule)
        {
            InitializePassedArguments(args);

            // Check if input string is provided
            if (string.IsNullOrEmpty(args.ParameterString1))
            {
                DMEEditor.AddLogMessage("Strip Failed: Input value is missing.");
                args.Messege = "Strip Failed: Input value is missing.";
                return args;
            }

            try
            {
                // Perform stripping of special characters
                string result = Regex.Replace(args.ParameterString1, @"[^a-zA-Z0-9\s]", "");

                args.Messege = $"Special characters removed successfully.";
                args.ParameterString2 = result; // Store cleaned string
                DMEEditor.Passedarguments.ReturnData = result;

                DMEEditor.AddLogMessage($"Special Characters Stripped: '{args.ParameterString1}' → '{result}'");
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
