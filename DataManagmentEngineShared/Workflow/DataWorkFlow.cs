using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Workflow
{
    public class DataWorkFlow : IDataWorkFlow
    {
        public DataWorkFlow()
        {
            Id = Guid.NewGuid().ToString();
        }
        public List<WorkFlowStep> Datasteps { get; set; } 
        public List<string> DataSources { get; set; } = new List<string>();
        public string WorkSpaceDatabase { get; set; }
        public string WorkSpaceFolder { get; set; }
        public string DataWorkFlowName { get; set; }
        public string Description { get; set; }
        public string Id { get; set; }
    }
}
