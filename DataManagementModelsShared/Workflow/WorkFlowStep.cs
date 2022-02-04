using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Workflow
{
    public class WorkFlowStep : IWorkFlowStep
    {
        public WorkFlowStep()
        {
            InParameters = new List<IPassedArgs>();
            OutParameters = new List<IPassedArgs>();
            Rules = new List<IWorkFlowRule>();
            NextStep = new List<IWorkFlowStep>();
        }
        public int Seq { get; set; }
        public string Description { get; set; }
        public string ID { get; set; }
        public string StepName { get; set; }
        public bool IsFinish { get; set; }=false;   
        public Boolean IsRunning { get; set; } = false;
        public IWorkFlowStep PrevStep { get; set; }=null;
        public List<IWorkFlowStep> NextStep { get; set; }
        public List<IPassedArgs> InParameters { get; set; } 
        public List<IPassedArgs> OutParameters { get; set; } 
        public List<IWorkFlowRule> Rules { get; set ; }

        public event EventHandler<IWorkFlowEventArgs> WorkFlowStepStarted;
        public event EventHandler<IWorkFlowEventArgs> WorkFlowStepEnded;
        public event EventHandler<IWorkFlowEventArgs> WorkFlowStepRunning;
    }
}
