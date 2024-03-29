﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Workflow
{
    public class WorkFlowAction : IWorkFlowAction
    {
        public WorkFlowAction()
        {
            Id=new Guid().ToString();
        }
     
        public string ActionTypeName { get; set; }
        public string Code { get; set; }
        public IDMEEditor DMEEditor { get ; set ; }
        public IWorkFlowAction PrevAction { get ; set ; }
        public List<IWorkFlowAction> NextAction { get ; set ; }
        public List<IPassedArgs> InParameters { get ; set ; }
        public List<IPassedArgs> OutParameters { get ; set ; }
        public List<IWorkFlowRule> Rules { get ; set ; }
        public bool IsFinish { get ; set ; }
        public bool IsRunning { get; set; }
        public string ClassName { get ; set ; }
        public string Name { get ; set ; }
        public string Id { get  ; set  ; }
        public string FullName { get  ; set  ; }
        public string Description { get  ; set  ; }

        public event EventHandler<WorkFlowEventArgs> WorkFlowActionStarted;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionEnded;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionRunning;

        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public PassedArgs StopAction()
        {
            throw new NotImplementedException();
        }
    }
}
