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
            ID= Guid.NewGuid().ToString();
        }
        public int Seq { get; set; }
        public string Description { get; set; }
        public string ID { get; set; }
        public string Name { get; set; }
        public bool IsFinish { get; set; }=false;   
        public bool IsRunning { get; set; } = false;
        public IWorkFlowStep PrevStep { get; set; }=null;
        public List<IWorkFlowStep> NextStep { get; set; }
        public List<IWorkFlowAction> Actions { get; set; }
        public List<IPassedArgs> InParameters { get; set; } 
        public List<IPassedArgs> OutParameters { get; set; } 
        public List<IWorkFlowRule> Rules { get; set ; }
        public string StepType { get; set; }
        public string Code { get; set; }

        public event EventHandler<WorkFlowEventArgs> WorkFlowStepStarted;
        public event EventHandler<WorkFlowEventArgs> WorkFlowStepEnded;
        public event EventHandler<WorkFlowEventArgs> WorkFlowStepRunning;
    }
}
