using System;
using System.Collections.Generic;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Workflow
{
    public class WorkFlowAction : IWorkFlowAction
    {
        public WorkFlowAction()
        {
            Id = Guid.NewGuid().ToString();
            NextAction = new List<IWorkFlowAction>();
            InParameters = new List<IPassedArgs>();
            OutParameters = new List<IPassedArgs>();
            Rules = new List<IWorkFlowRule>();
        }

        #region Properties
        public string ActionTypeName { get; set; }
        public string Code { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public IWorkFlowAction PrevAction { get; set; }
        public List<IWorkFlowAction> NextAction { get; set; }
        public List<IPassedArgs> InParameters { get; set; }
        public List<IPassedArgs> OutParameters { get; set; }
        public List<IWorkFlowRule> Rules { get; set; }
        public bool IsFinish { get; set; }
        public bool IsRunning { get; set; }
        public string ClassName { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        #endregion

        #region Events
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionStarted;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionEnded;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionRunning;
        #endregion

        #region PerformAction
        // Implements the interface method
        public virtual PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token)
        {
            return PerformAction(progress, token, null); // Calls the overloaded method without custom execution
        }

        // Overloaded method with a function to execute
        public virtual PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token, Func<PassedArgs,object> actionToExecute)
        {
            var args = new PassedArgs { Messege = "Action Started", ParameterInt1 = 0 };

            try
            {
                // Invoke the 'Started' event
                WorkFlowActionStarted?.Invoke(this, new WorkFlowEventArgs { Message = "Action Started", ActionName = Name });

                IsRunning = true;
                args.ParameterInt1 = 50;
                WorkFlowActionRunning?.Invoke(this, new WorkFlowEventArgs { Message = "Action Running", ActionName = Name });

                // Check for cancellation
                if (token.IsCancellationRequested)
                {
                    args.Messege = "Action Canceled";
                    return args;
                }

                // Execute the custom function if provided
                if (actionToExecute != null)
                {

                    PassedArgs result = (PassedArgs)actionToExecute.Invoke(args);
                    args = result;
                }

                // Finalize
                IsRunning = false;
                IsFinish = true;

                args.Messege = "Action Completed Successfully";
                args.ParameterInt1 = 100;

                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { Message = "Action Completed", ActionName = Name });

                return args;
            }
            catch (Exception ex)
            {
                args.Messege = $"Error executing action: {ex.Message}";
                IsRunning = false;
                IsFinish = true;

                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { Message = $"Action Failed: {ex.Message}", ActionName = Name });
                return args;
            }
        }
        #endregion

        public virtual PassedArgs StopAction()
        {
            IsRunning = false;
            IsFinish = true;
            return new PassedArgs { Messege = "Action Stopped" };
        }
    }
}
