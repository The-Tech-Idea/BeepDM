using System;
using System.Collections.Generic;
using System.Text;
using TheTechIdea.Beep.Workflow.Interfaces;

namespace TheTechIdea.Beep.Workflow
{
    public class RuleStructure: IRuleStructure
    {
        public string Rulename { get; set; }
        public string Fieldname { get; set; }
        public string Expression { get; set; }
    }
}
