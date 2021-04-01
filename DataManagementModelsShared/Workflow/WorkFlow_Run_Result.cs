using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.DataManagment_Engine.Workflow
{
    public class WorkFlow_Run_Result
    {
        public WorkFlow_Run_Result()
        {

        }
        public string Workflow_Name { get; set; }
        public DateTime RunTime { get; set; }
        public List<Workflow_Step_Run_result> StepsResult { get; set; } = new List<Workflow_Step_Run_result>();
    }
    public class Workflow_Step_Run_result
    {
        public Workflow_Step_Run_result()
        {

        }
        public string ID { get; set; }
        public string StepName { get; set; }
        public DateTime RunTime { get; set; }
        public string ActionName { get; set; }
        public string StepDescription { get; set; }
        public string ErrorDescription { get; set; }
        public string ActionLogFile { get; set; }
         
        public bool Ok { get; set; } = false;

    }
}

