using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Workflow.Mapping;

namespace TheTechIdea.Beep.Workflow
{
    public class RulesEditor
    {
        public RulesEditor(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;
        }
        public IDMEEditor DMEEditor { get; set; }
        public IDataSource DataSource { get; set; }
        public EntityStructure Entity { get; set; }
        public EntityDataMap DataMap { get; set; }
        public List<IWorkFlowStepRules> Rules { get; set; }
       
        public object SolveRule(EntityDataMap pDataMap)
        {
            return null;
        }

    
    }
}
