using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.DataManagment_Engine.Workflow.Interfaces;

namespace TheTechIdea.DataManagment_Engine.Workflow
{
    public class RuleStructure: IRuleStructure
    {
        public string Rulename { get; set; }
        public string Fieldname { get; set; }
        public string Expression { get; set; }
    }
}
