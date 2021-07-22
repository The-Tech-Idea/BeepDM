using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Workflow
{
    public class WorkFlowAction : IWorkFlowAction
    {
        public WorkFlowAction()
        {

        }
        public string Id { get ; set ; }
        public string ClassName { get ; set ; }
        public string FullName { get ; set ; }
        public string Description { get; set; } 
    }
}
