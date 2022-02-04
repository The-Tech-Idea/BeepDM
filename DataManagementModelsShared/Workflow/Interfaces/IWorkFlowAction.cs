using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Workflow
{
    public interface IWorkFlowAction
    {
        IDMEEditor DMEEditor { get; set; }
        IWorkFlowAction PrevAction { get; set; }
        List<IWorkFlowAction> NextAction { get; set; }
        List<IPassedArgs> InParameters { get; set; }
        List<IPassedArgs> OutParameters { get; set; }
        List<IWorkFlowRule> Rules { get; set; } 
        bool Finish { get; set; }
        string ClassName { get; set; }
        string FullName { get; set; }
        event EventHandler<IWorkFlowEventArgs> WorkFlowStepStarted;
        event EventHandler<IWorkFlowEventArgs> WorkFlowStepEnded;
        event EventHandler<IWorkFlowEventArgs> WorkFlowStepRunning;
        PassedArgs PerformAction();
        PassedArgs StopAction();

       

    }
}
