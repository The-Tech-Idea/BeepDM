using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

using TheTechIdea.Beep.Workflow.Mapping;
using TheTechIdea.Beep.Workflow;

using TheTechIdea.Util;
using TheTechIdea.Beep.Workflow.Interfaces;

namespace TheTechIdea.Beep.Workflow
{
    public class RulesEditor:IRulesEditor
    {
      

        public RulesEditor(IDMEEditor pDMEEditor)
        {
            DMEEditor= pDMEEditor;
            Parser = new RuleParser(DMEEditor);
        }
      
        public IDMEEditor DMEEditor { get; set; }
        public List<IWorkFlowRule> Rules { get; set; }
        public IRuleParser Parser { get; set; }
        private IDataSource DataSource;
        private EntityStructure Entity;
        private EntityDataMap DataMap;
        private IPassedArgs passedArgs;
        private IRuleStructure ruleStructure;
        public object SolveRule(IPassedArgs args)
        {
            if (args != null)
            {
                passedArgs = args;
                if (!string.IsNullOrEmpty(args.ParameterString1))
                {
                    RunMethod(args.ParameterString1);
                    if(DMEEditor.ErrorObject.Flag== Errors.Ok)
                    {
                        return DMEEditor.Passedarguments.ReturnData;
                    }
                }
            }
            return null;
        }
        private IErrorsInfo RunMethod(string Rule)
        {
            try
            {
                RuleStructure r = (RuleStructure)Parser.ParseRule(Rule);
                if(r != null)
                {
                    AssemblyClassDefinition assemblydef = (AssemblyClassDefinition)DMEEditor.ConfigEditor.Rules.Where(x => x.classProperties.Name.Equals(r.Rulename, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    var fc = Activator.CreateInstance(assemblydef.type, new object[] { DMEEditor });
                    IWorkFlowRule rule = (IWorkFlowRule)fc;
                  
                    if (rule != null)
                    {
                        rule.ExecuteRule((PassedArgs)DMEEditor.Passedarguments, (IRuleStructure)r);

                        if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                        {
                           // DMEEditor.AddLogMessage("Run Rule", "Success Run Rule : " + Rule, DateTime.Now, 0, null, Errors.Ok);
                        }
                        else
                            DMEEditor.AddLogMessage("Run Rule", "Failed running Rule : " + Rule, DateTime.Now, -1, Rule, Errors.Failed);
                    }
                }

            }
            catch (Exception ex)
            {
                string mes = "Could not Run rule " + Rule;
                DMEEditor.AddLogMessage("Run Rule", mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }

      

    }
    
}
