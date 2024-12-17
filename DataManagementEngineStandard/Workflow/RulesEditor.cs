using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Workflow.Interfaces;

namespace TheTechIdea.Beep.Workflow
{
    public class RulesEditor : IRulesEditor
    {
        public RulesEditor(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;
            Parser = new RuleParser(DMEEditor);
        }

        public IDMEEditor DMEEditor { get; set; }
        public List<IWorkFlowRule> Rules { get; set; }
        public IRuleParser Parser { get; set; }
        private IPassedArgs passedArgs;

        public object SolveRule(IPassedArgs args)
        {
            if (args != null)
            {
                passedArgs = args;
                if (!string.IsNullOrEmpty(args.ParameterString1))
                {
                    RunMethod(args.ParameterString1);
                    if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                        return DMEEditor.Passedarguments.ReturnData;
                }
            }
            return null;
        }

        private IErrorsInfo RunMethod(string ruleName)
        {
            try
            {
                var ruleStructure = (RuleStructure)Parser.ParseRule(ruleName);
                if (ruleStructure != null)
                {
                    var assemblyDef = DMEEditor.ConfigEditor.Rules
                        .FirstOrDefault(x => x.classProperties.Name.Equals(ruleStructure.Rulename, StringComparison.InvariantCultureIgnoreCase));

                    if (assemblyDef != null)
                    {
                        var ruleInstance = Activator.CreateInstance(assemblyDef.type, new object[] { DMEEditor }) as IWorkFlowRule;
                        ruleInstance?.ExecuteRule((PassedArgs)DMEEditor.Passedarguments, ruleStructure);
                    }
                }
            }
            catch (Exception ex)
            {
                string message = $"Failed to run rule: {ruleName}. Error: {ex.Message}";
                DMEEditor.AddLogMessage("Run Rule", message, DateTime.Now, -1, message, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
    }
}
