using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Util;

namespace TheTechIdea.Beep
{
    public interface IDataWorkFlow
    {
        List<IDataWorkFlowStep> Datasteps { get; set; }
        List<string> DataSources { get; set; }
        string              WorkSpaceDatabase { get; set; }
        string WorkSpaceFolder { get; set; }
        string DataWorkFlowName { get; set; }
        string Description { get; set; }
        string Id { get; set; }
       


    }
  
}