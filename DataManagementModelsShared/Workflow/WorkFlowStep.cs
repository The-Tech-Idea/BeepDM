using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.DataManagment_Engine.Workflow
{
    public class WorkFlowStep : IDataWorkFlowStep
    {
        public WorkFlowStep()
        {
            InParameters = new List<PassedArgs>();
            OutParameters = new List<PassedArgs>();
            NextStep = new List<string>();
        }
        public int Seq { get; set; }
        public string Description { get; set; }
        public string ID { get; set; }
        public string StepName { get; set; }
        public string ActionName { get; set; }
        public List<WorkFlowStepRules> Rules { get; set; }
        public string PrevStep { get; set; }
        public List<string> NextStep { get; set; }
        public List<PassedArgs> InParameters { get; set; } 
        public List<PassedArgs> OutParameters { get; set; } 
        public string Mapping { get; set; }
        public bool Finish { get; set; }
    }
}
