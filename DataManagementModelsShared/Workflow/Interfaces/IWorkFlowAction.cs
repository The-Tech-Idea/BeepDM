using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.DataManagment_Engine.Workflow
{
    public interface IWorkFlowAction
    {
        string Id { get; set; }
        string ClassName { get; set; }
        string FullName { get; set; }
        string Description { get; set; }

    }
}
