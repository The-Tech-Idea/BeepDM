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
        bool IsFinish { get; set; }
        bool IsRunning { get; set; }
        string ClassName { get; set; }
        string Name { get; set; }
        event EventHandler<IWorkFlowEventArgs> WorkFlowActionStarted;
        event EventHandler<IWorkFlowEventArgs> WorkFlowActionEnded;
        event EventHandler<IWorkFlowEventArgs> WorkFlowActionRunning;
        PassedArgs PerformAction();
        PassedArgs StopAction();

       

    }
}
