using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;

namespace TheTechIdea.Beep.Workflow
{
    public interface IWorkFlowAction
    {
       // IDMEEditor DMEEditor { get; set; }
        IWorkFlowAction PrevAction { get; set; }
        List<IWorkFlowAction> NextAction { get; set; }
        List<IPassedArgs> InParameters { get; set; }
        List<IPassedArgs> OutParameters { get; set; }
        List<IWorkFlowRule> Rules { get; set; } 
        string Id { get; set; }
        string ActionTypeName { get; set; }
        string Code { get; set; }
        bool IsFinish { get; set; }
        bool IsRunning { get; set; }
        string ClassName { get; set; }
        string Name { get; set; }
        event EventHandler<WorkFlowEventArgs> WorkFlowActionStarted;
        event EventHandler<WorkFlowEventArgs> WorkFlowActionEnded;
        event EventHandler<WorkFlowEventArgs> WorkFlowActionRunning;


        PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token);
        PassedArgs StopAction();

       

    }
}
