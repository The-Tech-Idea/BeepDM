using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Workflow
{
    public class WorkFlow : IWorkFlow
    {
        public WorkFlow()
        {
           
        }
        public List<IWorkFlowStep> Datasteps { get; set; } 
        public List<string> DataSources { get; set; } = new List<string>();
        public string WorkSpaceDatabase { get; set; }
        public string WorkSpaceFolder { get; set; }
        public string DataWorkFlowName { get; set; }
        public string Description { get; set; }
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public List<WorkFlow_Run_Result> workFlow_Run_Results { get; set; }=new List<WorkFlow_Run_Result>();

        public event EventHandler<IWorkFlowEventArgs> WorkFlowStarted;
        public event EventHandler<IWorkFlowEventArgs> WorkFlowEnded;
        public event EventHandler<IWorkFlowEventArgs> WorkFlowRunning;
    }
}
