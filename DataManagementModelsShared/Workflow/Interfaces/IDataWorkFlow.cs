using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TheTechIdea.DataManagment_Engine.Workflow;
using TheTechIdea.Util;

namespace TheTechIdea.DataManagment_Engine
{
    public interface IDataWorkFlow
    {
        List<WorkFlowStep> Datasteps { get; set; }
        List<string> DataSources { get; set; }
        string              WorkSpaceDatabase { get; set; }
        string WorkSpaceFolder { get; set; }
        string DataWorkFlowName { get; set; }
        string Description { get; set; }
        string Id { get; set; }
       


    }
  
}