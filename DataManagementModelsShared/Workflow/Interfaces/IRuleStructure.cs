using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Workflow.Interfaces
{
    public interface IRuleStructure
    {
         string Rulename { get; set; }
         string Fieldname { get; set; }
         string Expression { get; set; }
    }
}
