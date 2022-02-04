using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Workflow
{
    public class WorkFlowAction : IWorkFlowAction
    {
        public WorkFlowAction()
        {

        }

        public IDMEEditor DMEEditor { get ; set ; }
        public IWorkFlowAction PrevAction { get ; set ; }
        public List<IWorkFlowAction> NextAction { get ; set ; }
        public List<IPassedArgs> InParameters { get ; set ; }
        public List<IPassedArgs> OutParameters { get ; set ; }
        public List<IWorkFlowRule> Rules { get ; set ; }
        public bool Finish { get ; set ; }
        public string ClassName { get ; set ; }
        public string FullName { get ; set ; }

        public event EventHandler<IWorkFlowEventArgs> WorkFlowStepStarted;
        public event EventHandler<IWorkFlowEventArgs> WorkFlowStepEnded;
        public event EventHandler<IWorkFlowEventArgs> WorkFlowStepRunning;

        public PassedArgs PerformAction()
        {
            throw new NotImplementedException();
        }

        public PassedArgs StopAction()
        {
            throw new NotImplementedException();
        }
    }
}
